using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bestall.Configuration;
using Bestall.Domain;
using Bestall.Seerr;
using MediaBrowser.Model.Logging;

namespace Bestall.Kanal
{
    public enum Forslagslista
    {
        Popularast,
        Trendande,
        Kommande
    }

    /// <summary>Förslagslistor och filmtitlar för beställningskanalen, cachade så att kanalen känns snabb i TV-apparna.</summary>
    public sealed class Forslagscache
    {
        private static readonly TimeSpan ListGiltighet = TimeSpan.FromMinutes(15);
        private const int MaxSidor = 6;

        private readonly SeerrKlient _seerr;
        private readonly Func<PluginConfiguration> _konfig;
        private readonly ILogger _log;
        private readonly ConcurrentDictionary<Forslagslista, (DateTime Hamtad, List<DiscoverFilm> Filmer)> _listor
            = new ConcurrentDictionary<Forslagslista, (DateTime, List<DiscoverFilm>)>();
        private readonly ConcurrentDictionary<int, FilmDetaljer> _filmer = new ConcurrentDictionary<int, FilmDetaljer>();

        public Forslagscache(SeerrKlient seerr, Func<PluginConfiguration> konfig, ILogger log)
        {
            _seerr = seerr;
            _konfig = konfig;
            _log = log;
        }

        public async Task<List<DiscoverFilm>> HamtaAsync(Forslagslista lista, CancellationToken ct)
        {
            if (_listor.TryGetValue(lista, out var cachad) && DateTime.UtcNow - cachad.Hamtad < ListGiltighet)
                return cachad.Filmer;

            var antal = Math.Max(1, _konfig().FilmerPerForslagslista);
            var filmer = new List<DiscoverFilm>();
            var sedda = new HashSet<int>();

            for (var sida = 1; sida <= MaxSidor && filmer.Count < antal; sida++)
            {
                var svar = await HamtaSidaAsync(lista, sida, ct).ConfigureAwait(false);
                foreach (var f in svar.Results.Where(ArVisbar))
                    if (sedda.Add(f.Id) && filmer.Count < antal) filmer.Add(f);
                if (sida >= svar.TotalPages) break;
            }

            _listor[lista] = (DateTime.UtcNow, filmer);
            return filmer;
        }

        /// <summary>Filmens detaljer, cachade för hela körningen. Används för titlar i Mina beställningar.</summary>
        public async Task<FilmDetaljer> FilmAsync(int tmdbId, CancellationToken ct)
        {
            if (_filmer.TryGetValue(tmdbId, out var f)) return f;
            f = await _seerr.FilmAsync(tmdbId, ct).ConfigureAwait(false);
            _filmer[tmdbId] = f;
            return f;
        }

        /// <summary>Tömmer listorna, t.ex. efter en beställning, så att den beställda filmen försvinner direkt.</summary>
        public void Glom() => _listor.Clear();

        public static bool ArVisbar(DiscoverFilm f)
            => f.ArFilm && !f.Adult && f.Ar.HasValue && !string.IsNullOrEmpty(f.PosterPath)
               && Bestallningsregler.ArBestallningsbar(f.MediaInfo?.Status);

        private Task<DiscoverSida> HamtaSidaAsync(Forslagslista lista, int sida, CancellationToken ct)
        {
            switch (lista)
            {
                case Forslagslista.Trendande: return _seerr.TrendandeAsync(sida, ct);
                case Forslagslista.Kommande: return _seerr.KommandeAsync(sida, ct);
                default: return _seerr.PopularaAsync(sida, ct);
            }
        }
    }
}
