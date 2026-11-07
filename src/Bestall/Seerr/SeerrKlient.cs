using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bestall.Configuration;
using MediaBrowser.Model.Logging;

namespace Bestall.Seerr
{
    /// <summary>Anrop mot Seerrs API med pluginets API-nyckel (adminbehörighet).</summary>
    public sealed class SeerrKlient
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private readonly Func<PluginConfiguration> _konfig;
        private readonly ILogger _log;

        public SeerrKlient(Func<PluginConfiguration> konfig, ILogger log)
        {
            _konfig = konfig;
            _log = log;
        }

        public bool ArKonfigurerad
        {
            get
            {
                var k = _konfig();
                return !string.IsNullOrWhiteSpace(k.SeerrAdress) && !string.IsNullOrWhiteSpace(k.SeerrApiNyckel);
            }
        }

        public async Task<bool> SvararAsync(CancellationToken ct)
        {
            if (!ArKonfigurerad) return false;
            try
            {
                // /auth/me kräver giltig nyckel, så det kontrollerar både att Seerr är uppe och att nyckeln fungerar.
                using (var svar = await SkickaAsync(HttpMethod.Get, "/api/v1/auth/me", null, ct).ConfigureAwait(false))
                    return svar.IsSuccessStatusCode;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && ct.IsCancellationRequested))
            {
                _log.Debug("Seerr svarar inte: {0}", ex.Message);
                return false;
            }
        }

        public Task<DiscoverSida> PopularaAsync(int sida, CancellationToken ct)
            => HamtaAsync<DiscoverSida>($"/api/v1/discover/movies?page={sida}", ct);

        public Task<DiscoverSida> TrendandeAsync(int sida, CancellationToken ct)
            => HamtaAsync<DiscoverSida>($"/api/v1/discover/trending?page={sida}", ct);

        public Task<DiscoverSida> KommandeAsync(int sida, CancellationToken ct)
            => HamtaAsync<DiscoverSida>($"/api/v1/discover/movies/upcoming?page={sida}", ct);

        /// <summary>Filmer sorterade på popularitet, med minst angivet antal röster och betyg. Grunden för det stora urvalet.</summary>
        public Task<DiscoverSida> UrvalAsync(int sida, int minAntalRoster, double minBetyg, CancellationToken ct)
            => HamtaAsync<DiscoverSida>(
                $"/api/v1/discover/movies?page={sida}&sortBy=popularity.desc&voteCountGte={minAntalRoster}" +
                $"&voteAverageGte={minBetyg.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}", ct);

        public Task<FilmDetaljer> FilmAsync(int tmdbId, CancellationToken ct)
            => HamtaAsync<FilmDetaljer>($"/api/v1/movie/{tmdbId}?language={Sprak}", ct);

        public async Task<List<SeerrAnvandare>> AnvandareAsync(CancellationToken ct)
        {
            var alla = new List<SeerrAnvandare>();
            const int perSida = 100;
            for (var skip = 0; ; skip += perSida)
            {
                var sida = await HamtaAsync<SeerrAnvandarSida>($"/api/v1/user?take={perSida}&skip={skip}", ct).ConfigureAwait(false);
                alla.AddRange(sida.Results);
                if (sida.Results.Count < perSida) return alla;
            }
        }

        public async Task<List<SeerrBestallning>> BestallningarForAsync(int seerrAnvandarId, CancellationToken ct)
        {
            var sida = await HamtaAsync<BestallningsSida>(
                $"/api/v1/request?take=100&skip=0&filter=all&sort=modified&requestedBy={seerrAnvandarId}", ct).ConfigureAwait(false);
            return sida.Results;
        }

        /// <summary>De senast ändrade beställningarna vars film finns i biblioteket, för alla användare.</summary>
        public async Task<List<SeerrBestallning>> TillgangligaBestallningarAsync(CancellationToken ct)
        {
            var sida = await HamtaAsync<BestallningsSida>(
                "/api/v1/request?take=100&skip=0&filter=available&sort=modified", ct).ConfigureAwait(false);
            return sida.Results;
        }

        public async Task<Bestallningsresultat> BestallFilmAsync(int tmdbId, int seerrAnvandarId, CancellationToken ct)
        {
            var kropp = JsonSerializer.Serialize(new { mediaType = "movie", mediaId = tmdbId, userId = seerrAnvandarId });
            using (var svar = await SkickaAsync(HttpMethod.Post, "/api/v1/request", kropp, ct).ConfigureAwait(false))
            {
                if (svar.IsSuccessStatusCode) return Bestallningsresultat.Skapad;

                var text = await svar.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (svar.StatusCode == HttpStatusCode.Conflict || text.IndexOf("already", StringComparison.OrdinalIgnoreCase) >= 0)
                    return Bestallningsresultat.FannsRedan;

                _log.Error("Seerr avvisade beställning av tmdb {0} för Seerr-användare {1}: {2} {3}",
                    tmdbId, seerrAnvandarId, (int)svar.StatusCode, text);
                return Bestallningsresultat.Misslyckades;
            }
        }

        private string Sprak => Uri.EscapeDataString(string.IsNullOrWhiteSpace(_konfig().Sprak) ? "sv" : _konfig().Sprak);

        private async Task<T> HamtaAsync<T>(string sokvag, CancellationToken ct)
        {
            using (var svar = await SkickaAsync(HttpMethod.Get, sokvag, null, ct).ConfigureAwait(false))
            {
                var text = await svar.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!svar.IsSuccessStatusCode)
                    throw new HttpRequestException($"Seerr svarade {(int)svar.StatusCode} på {sokvag}");
                return JsonSerializer.Deserialize<T>(text, Json);
            }
        }

        private Task<HttpResponseMessage> SkickaAsync(HttpMethod metod, string sokvag, string jsonKropp, CancellationToken ct)
        {
            var k = _konfig();
            var anrop = new HttpRequestMessage(metod, k.SeerrAdress.TrimEnd('/') + sokvag);
            anrop.Headers.Add("X-Api-Key", k.SeerrApiNyckel);
            if (jsonKropp != null) anrop.Content = new StringContent(jsonKropp, Encoding.UTF8, "application/json");
            return Http.SendAsync(anrop, ct);
        }
    }
}
