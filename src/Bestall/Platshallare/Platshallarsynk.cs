using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bestall.Configuration;
using Bestall.Domain;
using Bestall.Kanal;
using Bestall.Seerr;
using MediaBrowser.Model.Logging;

namespace Bestall.Platshallare
{
    /// <summary>
    /// Håller urvalet aktuellt: skriver en mapp per beställningsbar film med .strm, .nfo och omslag,
    /// och tar bort mappar för filmer som inte längre är beställningsbara.
    /// </summary>
    public sealed class Platshallarsynk
    {
        /// <summary>Seerr (via TMDB) lämnar som mest 500 sidor à 20 filmer.</summary>
        private const int MaxSidor = 500;
        private static readonly HttpClient Bilder = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private readonly SeerrKlient _seerr;
        private readonly Forslagscache _forslag;
        private readonly Func<PluginConfiguration> _konfig;
        private readonly Omslagsmarkering _omslag;
        private readonly ILogger _log;

        public Platshallarsynk(SeerrKlient seerr, Forslagscache forslag, Func<PluginConfiguration> konfig, ILogger log)
        {
            _seerr = seerr;
            _forslag = forslag;
            _konfig = konfig;
            _omslag = new Omslagsmarkering(log);
            _log = log;
        }

        /// <summary>Kör synken. Returnerar sant om något på disk ändrades.</summary>
        public async Task<bool> KorAsync(IProgress<double> framsteg, CancellationToken ct)
        {
            var k = _konfig();
            if (string.IsNullOrWhiteSpace(k.PlatshallarMapp))
            {
                _log.Warn("Ingen mapp för platshållare är inställd; synken hoppas över.");
                return false;
            }
            if (!await _seerr.SvararAsync(ct).ConfigureAwait(false))
            {
                // Viktigt: utan svar från Seerr vet vi inget om urvalet, så ingenting tas bort.
                _log.Warn("Seerr svarar inte; synken hoppas över och befintliga platshållare lämnas orörda.");
                return false;
            }

            _log.Info("Platshållarsynk startar i {0}", k.PlatshallarMapp);
            Directory.CreateDirectory(k.PlatshallarMapp);
            foreach (var rest in Directory.EnumerateDirectories(k.PlatshallarMapp, ".skapas-*"))
                TaBort(rest);
            var ombyggda = ByggOmGamlaPlatshallare(k.PlatshallarMapp);

            var onskade = await HamtaUrvalAsync(k, ct).ConfigureAwait(false);
            framsteg.Report(20);

            var befintliga = BefintligaMappar(k.PlatshallarMapp);
            var plan = Synkplan.Berakna(onskade.Keys.ToList(), befintliga.Keys.ToList());

            if (plan.BorttagningSparrad)
                _log.Warn("Urvalet från Seerr ({0}) är misstänkt litet jämfört med befintliga platshållare ({1}); inget tas bort denna gång.",
                    onskade.Count, befintliga.Count);

            foreach (var tmdb in plan.TaBort)
            {
                ct.ThrowIfCancellationRequested();
                TaBort(befintliga[tmdb]);
            }

            var lander = (k.AldersgransLander ?? "US").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var klara = 0;
            foreach (var tmdb in plan.LaggTill)
            {
                ct.ThrowIfCancellationRequested();
                await SkapaAsync(k.PlatshallarMapp, k.EmbyAdress, onskade[tmdb], lander, ct).ConfigureAwait(false);
                klara++;
                framsteg.Report(20 + 80.0 * klara / Math.Max(1, plan.LaggTill.Count));
                await Task.Delay(150, ct).ConfigureAwait(false); // Skonsamt mot Seerr och TMDB vid första körningen.
            }

            _log.Info("Platshållarsynk klar: {0} tillagda, {1} borttagna, {2} totalt.",
                plan.LaggTill.Count, plan.TaBort.Count, befintliga.Count - plan.TaBort.Count + plan.LaggTill.Count);
            framsteg.Report(100);
            return ombyggda > 0 || plan.LaggTill.Count > 0 || plan.TaBort.Count > 0;
        }

        /// <summary>Tar bort platshållaren för en film, t.ex. direkt efter att den beställts.</summary>
        public void TaBortFilm(int tmdbId)
        {
            var mapp = _konfig().PlatshallarMapp;
            if (string.IsNullOrWhiteSpace(mapp) || !Directory.Exists(mapp)) return;
            if (BefintligaMappar(mapp).TryGetValue(tmdbId, out var sokvag)) TaBort(sokvag);
        }

        private async Task<Dictionary<int, DiscoverFilm>> HamtaUrvalAsync(PluginConfiguration k, CancellationToken ct)
        {
            var max = Math.Max(0, Math.Min(k.MaxAntalPlatshallare, MaxSidor * 20));
            var urval = new Dictionary<int, DiscoverFilm>();

            // Förslagslistorna först, så att det man ser i kanalen alltid också går att söka fram.
            foreach (Forslagslista lista in Enum.GetValues(typeof(Forslagslista)))
                foreach (var f in await _forslag.HamtaAsync(lista, ct).ConfigureAwait(false))
                    urval[f.Id] = f;
            var franListor = urval.Count;

            int sida = 0, totaltSidor = 0, traffar = 0;
            string stopp = "taket nått";
            while (urval.Count < max)
            {
                sida++;
                if (sida > MaxSidor) { stopp = $"max {MaxSidor} sidor"; break; }

                var svar = await _seerr.UrvalAsync(sida, Math.Max(0, k.MinAntalRoster), Math.Max(0, Math.Min(10, k.MinBetyg)), ct).ConfigureAwait(false);
                totaltSidor = svar.TotalPages;
                traffar += svar.Results.Count;
                foreach (var f in svar.Results.Where(Forslagscache.ArVisbar))
                    if (urval.Count < max && !urval.ContainsKey(f.Id)) urval[f.Id] = f;

                if (sida == 1)
                    _log.Info("Urval sida 1: {0} filmer, {1} beställningsbara, Seerr anger {2} sidor totalt.",
                        svar.Results.Count, svar.Results.Count(Forslagscache.ArVisbar), svar.TotalPages);

                if (svar.Results.Count == 0) { stopp = $"tom sida {sida}"; break; }
                if (svar.TotalPages > 0 && sida >= svar.TotalPages) { stopp = $"sista sidan ({svar.TotalPages})"; break; }
            }

            _log.Info("Urval: {0} från förslagslistor, {1} totalt efter {2} sidor ({3} filmer från Seerr). Stoppade: {4}. Tak {5}, minst {6} röster, lägst betyg {7}.",
                franListor, urval.Count, sida, traffar, stopp, max, k.MinAntalRoster, k.MinBetyg);
            return urval;
        }

        /// <summary>
        /// Platshållare från äldre versioner har [tmdbid-…] i namnet och TMDB-id i NFO-filen, vilket gör att Emby
        /// för över "sedd" till den riktiga filmen. Byt till pluginets eget märke och ta bort TMDB-id, utan att
        /// hämta något på nytt.
        /// </summary>
        private int ByggOmGamlaPlatshallare(string rot)
        {
            var antal = 0;
            foreach (var mapp in Directory.EnumerateDirectories(rot).Where(m => Platshallarnamn.ArGammaltFormat(Path.GetFileName(m))).ToList())
            {
                try
                {
                    foreach (var fil in Directory.EnumerateFiles(mapp).ToList())
                    {
                        var nyFil = Path.Combine(mapp, Platshallarnamn.TillNyttFormat(Path.GetFileName(fil)));
                        if (fil.EndsWith(".nfo", StringComparison.OrdinalIgnoreCase))
                            File.WriteAllText(fil, NfoSkrivare.UtanTmdbId(File.ReadAllText(fil)), new UTF8Encoding(false));
                        if (nyFil != fil) File.Move(fil, nyFil);
                    }

                    var nyMapp = Path.Combine(rot, Platshallarnamn.TillNyttFormat(Path.GetFileName(mapp)));
                    if (Directory.Exists(nyMapp)) TaBort(mapp);
                    else Directory.Move(mapp, nyMapp);
                    antal++;
                }
                catch (Exception ex)
                {
                    // Går den inte att bygga om, ta bort den; nästa synk skapar den på nytt i rätt format.
                    _log.Warn("Kunde inte bygga om platshållare {0}: {1}", mapp, ex.Message);
                    TaBort(mapp);
                }
            }

            if (antal > 0) _log.Info("Byggde om {0} platshållare till nytt format (utan TMDB-id).", antal);
            return antal;
        }

        private static Dictionary<int, string> BefintligaMappar(string rot)
        {
            var resultat = new Dictionary<int, string>();
            foreach (var mapp in Directory.EnumerateDirectories(rot))
            {
                if (Path.GetFileName(mapp).StartsWith(".")) continue;
                var id = Platshallarnamn.TmdbIdFranSokvag(Path.GetFileName(mapp));
                if (id.HasValue) resultat[id.Value] = mapp;
            }
            return resultat;
        }

        private async Task SkapaAsync(string rot, string embyAdress, DiscoverFilm film, string[] lander, CancellationToken ct)
        {
            FilmDetaljer detaljer = null;
            try { detaljer = await _seerr.FilmAsync(film.Id, ct).ConfigureAwait(false); }
            catch (Exception ex) when (!ct.IsCancellationRequested) { _log.Debug("Inga detaljer för tmdb {0}: {1}", film.Id, ex.Message); }

            var namn = Platshallarnamn.Mappnamn(film.Title, film.Ar ?? 0, film.Id);
            var mapp = Path.Combine(rot, namn);
            // Punkt först: dold mapp som Emby hoppar över tills den döps om.
            var tillfallig = Path.Combine(rot, ".skapas-" + film.Id);

            try
            {
                if (Directory.Exists(tillfallig)) Directory.Delete(tillfallig, true);
                Directory.CreateDirectory(tillfallig);

                var adress = $"{(embyAdress ?? "http://127.0.0.1:8096").TrimEnd('/')}/emby/Bestall/Video?Tmdb={film.Id}";
                File.WriteAllText(Path.Combine(tillfallig, namn + ".strm"), adress + "\n", new UTF8Encoding(false));
                File.WriteAllText(Path.Combine(tillfallig, namn + ".nfo"), NfoSkrivare.Skapa(film, detaljer, lander), new UTF8Encoding(false));

                var omslag = await HamtaOmslagAsync(detaljer?.PosterPath ?? film.PosterPath, ct).ConfigureAwait(false);
                if (omslag != null) File.WriteAllBytes(Path.Combine(tillfallig, "poster.jpg"), _omslag.Markera(omslag));

                // Mappen blir synlig för Emby först när den är komplett.
                Directory.Move(tillfallig, mapp);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _log.ErrorException("Kunde inte skapa platshållare för tmdb " + film.Id, ex);
                try { if (Directory.Exists(tillfallig)) Directory.Delete(tillfallig, true); } catch { /* städas nästa gång */ }
            }
        }

        private async Task<byte[]> HamtaOmslagAsync(string posterPath, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(posterPath)) return null;
            try
            {
                using (var svar = await Bilder.GetAsync("https://image.tmdb.org/t/p/w500" + posterPath, ct).ConfigureAwait(false))
                    return svar.IsSuccessStatusCode ? await svar.Content.ReadAsByteArrayAsync().ConfigureAwait(false) : null;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _log.Debug("Kunde inte hämta omslag {0}: {1}", posterPath, ex.Message);
                return null;
            }
        }

        private void TaBort(string mapp)
        {
            try { Directory.Delete(mapp, true); }
            catch (Exception ex) { _log.Warn("Kunde inte ta bort platshållare {0}: {1}", mapp, ex.Message); }
        }
    }
}
