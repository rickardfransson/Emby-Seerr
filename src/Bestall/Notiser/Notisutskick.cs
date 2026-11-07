using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bestall.Domain;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Session;

namespace Bestall.Notiser
{
    /// <summary>
    /// Visar en notis i Emby-appen när en tittares beställda film har kommit. Kontrollerar Seerr var femte minut.
    /// Är tittaren inte inloggad sparas notisen och visas nästa gång en av tittarens appar ansluter.
    /// </summary>
    public class Notisutskick : IServerEntryPoint
    {
        private static readonly TimeSpan Intervall = TimeSpan.FromMinutes(5);

        private readonly ISessionManager _sessioner;
        private readonly ILogger _log;
        private readonly Notislager _lager;
        private readonly SemaphoreSlim _las = new SemaphoreSlim(1, 1);
        private Timer _timer;

        public Notisutskick(ISessionManager sessioner, IApplicationPaths paths, ILogManager logManager)
        {
            _sessioner = sessioner;
            _log = logManager.GetLogger("Bestall");
            var mapp = Path.Combine(paths.PluginConfigurationsPath, "Bestall");
            Directory.CreateDirectory(mapp);
            _lager = new Notislager(Path.Combine(mapp, "notiser.json"));
        }

        public void Run()
        {
            _sessioner.SessionStarted += VidNySession;
            _timer = new Timer(_ => _ = KorAsync(), null, TimeSpan.FromMinutes(1), Intervall);
        }

        public void Dispose()
        {
            _sessioner.SessionStarted -= VidNySession;
            _timer?.Dispose();
        }

        private void VidNySession(object sender, SessionEventArgs e)
        {
            // Ge appen en stund att bli klar innan notisen skickas.
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
                await LevereraAsync().ConfigureAwait(false);
            });
        }

        private async Task KorAsync()
        {
            var p = Plugin.Instance;
            if (p == null || !p.Configuration.NotisAktiv) return;

            if (!await _las.WaitAsync(0).ConfigureAwait(false)) return;
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
                {
                    if (!await p.SeerrStatus.ArUppeAsync(cts.Token).ConfigureAwait(false)) return;

                    var tillgangliga = (await p.Seerr.TillgangligaBestallningarAsync(cts.Token).ConfigureAwait(false))
                        .Where(b => b.Type == "movie" && b.Media?.TmdbId != null)
                        .ToList();

                    if (!_lager.ArInitierad)
                    {
                        _lager.Initiera(tillgangliga.Select(b => b.Id));
                        _log.Info("Notiser startade; {0} redan tillgängliga beställningar får ingen notis.", tillgangliga.Count);
                        return;
                    }

                    foreach (var b in tillgangliga.Where(b => !_lager.HarNotifierat(b.Id)))
                    {
                        if (!Guid.TryParse(b.RequestedBy?.EmbyAnvandarId, out var tittare)) continue;

                        string titel = null;
                        int? ar = null;
                        try
                        {
                            var film = await p.Forslag.FilmAsync(b.Media.TmdbId.Value, cts.Token).ConfigureAwait(false);
                            titel = film?.Title;
                            if (DateTime.TryParse(film?.ReleaseDate, out var d)) ar = d.Year;
                        }
                        catch (Exception ex) { _log.Debug("Ingen titel för tmdb {0}: {1}", b.Media.TmdbId, ex.Message); }

                        _lager.LaggTill(b.Id, tittare, titel ?? "Din beställda film", ar);
                        _log.Info("Notis väntar för {0}: {1}", b.RequestedBy?.DisplayName, titel);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug("Kontroll av färdiga beställningar misslyckades: {0}", ex.Message);
            }
            finally
            {
                _las.Release();
            }

            await LevereraAsync().ConfigureAwait(false);
        }

        private async Task LevereraAsync()
        {
            var p = Plugin.Instance;
            if (p == null || !p.Configuration.NotisAktiv) return;
            var k = p.Configuration;

            foreach (var tittare in _lager.AnvandareMedVantande())
            {
                var sessioner = _sessioner.Sessions
                    .Where(s => Guid.TryParse(s.UserId, out var id) && id == tittare)
                    .ToList();
                if (sessioner.Count == 0) continue;

                foreach (var notis in _lager.VantandeFor(tittare))
                {
                    var meddelande = new MessageCommand
                    {
                        Header = Notistext.Formatera(k.NotisRubrik, notis.Titel, notis.Ar, Notistext.StandardRubrik),
                        Text = Notistext.Formatera(k.NotisText, notis.Titel, notis.Ar, Notistext.StandardText),
                        TimeoutMs = Math.Max(3, Math.Min(120, k.NotisSekunder)) * 1000L
                    };

                    var skickad = false;
                    foreach (var s in sessioner)
                    {
                        try
                        {
                            await _sessioner.SendMessageCommand(null, s.Id, meddelande, CancellationToken.None).ConfigureAwait(false);
                            skickad = true;
                            _log.Info("Notis visad för {0} i {1} ({2}): {3}", s.UserName, s.Client, s.DeviceName, meddelande.Text);
                        }
                        catch (Exception ex)
                        {
                            _log.Debug("Kunde inte visa notis i {0}: {1}", s.Client, ex.Message);
                        }
                    }

                    if (skickad) _lager.Levererad(notis);
                }
            }
        }
    }
}
