using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bestall.Domain
{
    /// <summary>Namngivning av platshållare på disk och i Emby.</summary>
    public static class Platshallarnamn
    {
        private static readonly Regex TmdbIdMonster = new Regex(@"\[tmdbid-(\d+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly char[] OgiltigaTecken = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        public const string Tillagg = " (beställ)";

        /// <summary>Titeln som visas i Emby. Tillägget sist så att sök på filmens namn fortfarande träffar.</summary>
        public static string Visningstitel(string titel) => (titel ?? "").Trim() + Tillagg;

        /// <summary>Mappnamn med tmdb-id, t.ex. "Dune (2021) [tmdbid-438631]".</summary>
        public static string Mappnamn(string titel, int ar, int tmdbId)
        {
            var rensad = new string((titel ?? "").Where(c => !OgiltigaTecken.Contains(c) && !char.IsControl(c)).ToArray()).Trim().TrimEnd('.');
            if (rensad.Length == 0) rensad = "Film";
            if (rensad.Length > 120) rensad = rensad.Substring(0, 120).Trim();
            return $"{rensad} ({ar}) [tmdbid-{tmdbId}]";
        }

        /// <summary>Hittar tmdb-id i en sökväg till en platshållare, eller null.</summary>
        public static int? TmdbIdFranSokvag(string sokvag)
        {
            if (string.IsNullOrEmpty(sokvag)) return null;
            var traffar = TmdbIdMonster.Matches(sokvag);
            if (traffar.Count == 0) return null;
            return int.TryParse(traffar[traffar.Count - 1].Groups[1].Value, out var id) ? id : (int?)null;
        }

        /// <summary>Om sökvägen ligger i (eller är) platshållarmappen.</summary>
        public static bool LiggerI(string sokvag, string mapp)
        {
            if (string.IsNullOrWhiteSpace(sokvag) || string.IsNullOrWhiteSpace(mapp)) return false;
            var m = mapp.TrimEnd('/', '\\');
            var s = sokvag.Replace('\\', '/');
            m = m.Replace('\\', '/');
            return s.Equals(m, System.StringComparison.OrdinalIgnoreCase)
                || s.StartsWith(m + "/", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
