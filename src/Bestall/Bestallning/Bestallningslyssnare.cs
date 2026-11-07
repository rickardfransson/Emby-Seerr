using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bestall.Domain;
using Bestall.Kanal;
using Bestall.Platshallare;
using Bestall.Seerr;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;

namespace Bestall.Bestallning
{
    /// <summary>
    /// Gör beställningen när en tittare spelar en platshållare eller en film i beställningskanalen.
    /// Embys uppspelningshändelse är det enda ställe där vi vet både vilken film och vilken tittare det gäller
    /// (se docs/adr/0002).
    /// </summary>
    public class Bestallningslyssnare : IServerEntryPoint
    {
        private static readonly TimeSpan Dubblettfonster = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan BorttagningsFordrojning = TimeSpan.FromMinutes(2);

        private readonly ISessionManager _sessioner;
        private readonly ILibraryMonitor _bibliotek;
        private readonly ILogger _log;
        private readonly ConcurrentDictionary<string, DateTime> _senaste = new ConcurrentDictionary<string, DateTime>();

        public Bestallningslyssnare(ISessionManager sessioner, ILibraryMonitor bibliotek, ILogManager logManager)
        {
            _sessioner = sessioner;
            _bibliotek = bibliotek;
            _log = logManager.GetLogger("Bestall");
        }

        public void Run() => _sessioner.PlaybackStart += VidUppspelning;

        public void Dispose() => _sessioner.PlaybackStart -= VidUppspelning;

        private void VidUppspelning(object sender, PlaybackProgressEventArgs e)
        {
            var tmdb = TmdbIdForBestallning(e.Item);
            if (!tmdb.HasValue) return;

            var tittare = e.Users?.FirstOrDefault();
            if (tittare == null)
            {
                _log.Warn("Beställningsbar film {0} spelades utan känd användare; ingen beställning gjord.", tmdb);
                return;
            }

            // Händelsen kan komma flera gånger för samma uppspelning.
            var nyckel = $"{tittare.Id:N}:{tmdb}";
            var nu = DateTime.UtcNow;
            if (_senaste.TryGetValue(nyckel, out var forra) && nu - forra < Dubblettfonster) return;
            _senaste[nyckel] = nu;
            foreach (var gammal in _senaste.Where(kv => nu - kv.Value > Dubblettfonster).Select(kv => kv.Key).ToList())
                _senaste.TryRemove(gammal, out _);

            _ = Task.Run(() => BestallAsync(tmdb.Value, tittare, e.ClientName, e.DeviceName));
        }

        private async Task BestallAsync(int tmdb, User tittare, string klient, string enhet)
        {
            var p = Plugin.Instance;
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var seerrId = await p.Tittare.SeerrIdForAsync(tittare.Id, cts.Token).ConfigureAwait(false);
                    if (!seerrId.HasValue)
                    {
                        _log.Warn("{0} spelade en beställningsbar film men har inget Seerr-konto; ingen beställning gjord. " +
                                  "Begränsa platshållarbiblioteket till tittare i Embys användarinställningar.", tittare.Name);
                        return;
                    }

                    var resultat = await p.Seerr.BestallFilmAsync(tmdb, seerrId.Value, cts.Token).ConfigureAwait(false);
                    _log.Info("Beställning av tmdb {0} för {1} ({2}, {3}): {4}", tmdb, tittare.Name, klient, enhet, resultat);

                    if (resultat == Bestallningsresultat.Misslyckades) return;

                    p.Forslag.Glom();

                    // Vänta tills bekräftelsevideon spelats klart innan platshållaren tas bort.
                    await Task.Delay(BorttagningsFordrojning).ConfigureAwait(false);
                    new Platshallarsynk(p.Seerr, p.Forslag, () => p.Configuration, _log).TaBortFilm(tmdb);
                    _bibliotek.ReportFileSystemChanged(p.Configuration.PlatshallarMapp);
                }
            }
            catch (Exception ex)
            {
                _log.ErrorException($"Beställning av tmdb {tmdb} för {tittare.Name} misslyckades", ex);
            }
        }

        private static int? TmdbIdForBestallning(BaseItem objekt)
        {
            if (objekt == null) return null;

            // Film i beställningskanalen.
            var externtId = objekt.ExternalId;
            if (!string.IsNullOrEmpty(externtId) && externtId.StartsWith(Bestallningskanal.FilmPrefix, StringComparison.Ordinal)
                && int.TryParse(externtId.Substring(Bestallningskanal.FilmPrefix.Length), out var kanalId))
                return kanalId;

            // Platshållare i platshållarbiblioteket.
            var mapp = Plugin.Instance?.Configuration.PlatshallarMapp;
            if (Platshallarnamn.LiggerI(objekt.Path, mapp))
                return Platshallarnamn.TmdbIdFranSokvag(objekt.Path);

            return null;
        }
    }
}
