using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Bestall.Seerr
{
    public sealed class DiscoverSida
    {
        [JsonPropertyName("page")] public int Page { get; set; }
        [JsonPropertyName("totalPages")] public int TotalPages { get; set; }
        [JsonPropertyName("results")] public List<DiscoverFilm> Results { get; set; } = new List<DiscoverFilm>();
    }

    public sealed class DiscoverFilm
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("mediaType")] public string MediaType { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("originalTitle")] public string OriginalTitle { get; set; }
        [JsonPropertyName("releaseDate")] public string ReleaseDate { get; set; }
        [JsonPropertyName("overview")] public string Overview { get; set; }
        [JsonPropertyName("posterPath")] public string PosterPath { get; set; }
        [JsonPropertyName("voteAverage")] public double? VoteAverage { get; set; }
        [JsonPropertyName("adult")] public bool Adult { get; set; }
        [JsonPropertyName("mediaInfo")] public MediaInfo MediaInfo { get; set; }

        public int? Ar => DateTime.TryParse(ReleaseDate, out var d) ? d.Year : (int?)null;

        /// <summary>Trendande-listan blandar filmer och serier; filmer saknar ibland mediaType i de andra listorna.</summary>
        public bool ArFilm => string.IsNullOrEmpty(MediaType) || MediaType == "movie";
    }

    public sealed class MediaInfo
    {
        [JsonPropertyName("tmdbId")] public int? TmdbId { get; set; }
        [JsonPropertyName("status")] public int? Status { get; set; }
        [JsonPropertyName("mediaType")] public string MediaType { get; set; }

        /// <summary>Pågående nedladdningar som Seerr hämtat från Radarr.</summary>
        [JsonPropertyName("downloadStatus")] public List<Nedladdning> DownloadStatus { get; set; } = new List<Nedladdning>();
    }

    public sealed class Nedladdning
    {
        [JsonPropertyName("size")] public long Size { get; set; }
        [JsonPropertyName("sizeLeft")] public long SizeLeft { get; set; }
        [JsonPropertyName("estimatedCompletionTime")] public DateTime? EstimatedCompletionTime { get; set; }
    }

    public sealed class FilmDetaljer
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; }
        [JsonPropertyName("originalTitle")] public string OriginalTitle { get; set; }
        [JsonPropertyName("releaseDate")] public string ReleaseDate { get; set; }
        [JsonPropertyName("overview")] public string Overview { get; set; }
        [JsonPropertyName("tagline")] public string Tagline { get; set; }
        [JsonPropertyName("posterPath")] public string PosterPath { get; set; }
        [JsonPropertyName("runtime")] public int? Runtime { get; set; }
        [JsonPropertyName("voteAverage")] public double? VoteAverage { get; set; }
        [JsonPropertyName("genres")] public List<Genre> Genres { get; set; } = new List<Genre>();
        [JsonPropertyName("releases")] public Releases Releases { get; set; }
        [JsonPropertyName("mediaInfo")] public MediaInfo MediaInfo { get; set; }

        /// <summary>Åldersgräns för angivna land, i den ordning de anges, eller null.</summary>
        public string Aldersgrans(IEnumerable<string> lander)
        {
            var resultat = Releases?.Results ?? new List<ReleaseLand>();
            foreach (var land in lander)
            {
                var r = resultat.FirstOrDefault(x => string.Equals(x.Land, land, StringComparison.OrdinalIgnoreCase));
                var cert = r?.ReleaseDates?.Select(d => d.Certification).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
                if (cert != null) return land.Equals("US", StringComparison.OrdinalIgnoreCase) ? cert : $"{land.ToUpperInvariant()}-{cert}";
            }
            return null;
        }
    }

    public sealed class Genre
    {
        [JsonPropertyName("name")] public string Name { get; set; }
    }

    public sealed class Releases
    {
        [JsonPropertyName("results")] public List<ReleaseLand> Results { get; set; } = new List<ReleaseLand>();
    }

    public sealed class ReleaseLand
    {
        [JsonPropertyName("iso_3166_1")] public string Land { get; set; }
        [JsonPropertyName("release_dates")] public List<ReleaseDatum> ReleaseDates { get; set; } = new List<ReleaseDatum>();
    }

    public sealed class ReleaseDatum
    {
        [JsonPropertyName("certification")] public string Certification { get; set; }
    }

    public sealed class SeerrAnvandarSida
    {
        [JsonPropertyName("results")] public List<SeerrAnvandare> Results { get; set; } = new List<SeerrAnvandare>();
    }

    public sealed class SeerrAnvandare
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("displayName")] public string DisplayName { get; set; }

        /// <summary>Seerr lagrar Emby-användarens id i samma fält som för Jellyfin.</summary>
        [JsonPropertyName("jellyfinUserId")] public string EmbyAnvandarId { get; set; }
    }

    public sealed class BestallningsSida
    {
        [JsonPropertyName("results")] public List<SeerrBestallning> Results { get; set; } = new List<SeerrBestallning>();
    }

    public sealed class SeerrBestallning
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("media")] public MediaInfo Media { get; set; }
        [JsonPropertyName("requestedBy")] public SeerrAnvandare RequestedBy { get; set; }
    }

    public enum Bestallningsresultat
    {
        Skapad,
        FannsRedan,
        Misslyckades
    }
}
