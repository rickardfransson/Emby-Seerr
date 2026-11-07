using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Bestall.Domain;
using Bestall.Seerr;

namespace Bestall.Platshallare
{
    /// <summary>
    /// Skriver filmens information som NFO, så att Emby läser den lokalt i stället för att hämta den från nätet.
    /// lockdata hindrar Emby från att skriva över titeln med "(beställ)".
    /// </summary>
    public static class NfoSkrivare
    {
        public static string Skapa(DiscoverFilm film, FilmDetaljer detaljer, IEnumerable<string> aldersgransLander)
        {
            var titel = detaljer?.Title ?? film.Title;
            var datum = detaljer?.ReleaseDate ?? film.ReleaseDate;

            var movie = new XElement("movie",
                new XElement("title", Platshallarnamn.Visningstitel(titel)),
                new XElement("sorttitle", titel),
                new XElement("originaltitle", detaljer?.OriginalTitle ?? film.OriginalTitle ?? titel),
                new XElement("year", film.Ar),
                new XElement("plot", detaljer?.Overview ?? film.Overview ?? ""),
                new XElement("uniqueid", new XAttribute("type", "tmdb"), new XAttribute("default", "true"), film.Id),
                new XElement("tmdbid", film.Id),
                new XElement("lockdata", "true"));

            if (DateTime.TryParse(datum, out var d))
                movie.Add(new XElement("premiered", d.ToString("yyyy-MM-dd")));
            if (!string.IsNullOrWhiteSpace(detaljer?.Tagline))
                movie.Add(new XElement("tagline", detaljer.Tagline));
            if (detaljer?.Runtime is int minuter && minuter > 0)
                movie.Add(new XElement("runtime", minuter));
            var betyg = detaljer?.VoteAverage ?? film.VoteAverage;
            if (betyg.HasValue)
                movie.Add(new XElement("rating", betyg.Value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)));

            var aldersgrans = detaljer?.Aldersgrans(aldersgransLander);
            if (aldersgrans != null)
                movie.Add(new XElement("mpaa", aldersgrans));

            foreach (var genre in (detaljer?.Genres ?? new List<Genre>()).Select(g => g.Name).Where(n => !string.IsNullOrWhiteSpace(n)))
                movie.Add(new XElement("genre", genre));

            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), movie).ToString();
        }
    }
}
