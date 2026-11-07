using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bestall.Domain;
using Bestall.Seerr;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;

namespace Bestall.Kanal
{
    /// <summary>
    /// Beställningskanalen: förslagslistor att bläddra i och tittarens egna beställningar.
    /// Att spela en film här gör en beställning (se Bestallning/Bestallningslyssnare).
    /// </summary>
    public class Bestallningskanal : IChannel, IRequiresMediaInfoCallback, IHasCacheKey
    {
        public const string FilmPrefix = "bestall-film-";
        private const string StatusPrefix = "bestall-status-";
        private const string Mina = "bestall-mina";
        private const string Otillganglig = "bestall-otillganglig";

        private static readonly (string Id, string Namn, Forslagslista Lista)[] Listor =
        {
            ("bestall-lista-popular", "Populärt", Forslagslista.Popularast),
            ("bestall-lista-trendande", "Trendande", Forslagslista.Trendande),
            ("bestall-lista-kommande", "Kommande", Forslagslista.Kommande)
        };

        private readonly IUserManager _users;
        private readonly ILogger _log;

        public Bestallningskanal(IUserManager users, ILogManager logManager)
        {
            _users = users;
            _log = logManager.GetLogger("Bestall");
        }

        private static Plugin P => Plugin.Instance;

        public string Name => "Beställ film";
        public string Description => "Bläddra bland filmer och beställ. Tryck spela för att beställa.";
        public string DataVersion => "1";
        public string HomePageUrl => "";
        public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

        /// <summary>
        /// Nyckeln för Embys cache av kanalen. Den byter värde per tittare, när Seerr går upp eller ner,
        /// och varannan minut, så att nedladdningsstatus hänger med och Mina beställningar inte delas mellan tittare och inte blir inaktuell.
        /// </summary>
        public string GetCacheKey(string userId)
        {
            var uppe = P.SeerrStatus.ArUppeAsync(CancellationToken.None).GetAwaiter().GetResult();
            var period = DateTime.UtcNow.Ticks / TimeSpan.FromMinutes(2).Ticks;
            return $"{userId}-{(uppe ? "uppe" : "nere")}-{period}";
        }

        public InternalChannelFeatures GetChannelFeatures() => new InternalChannelFeatures
        {
            ContentTypes = new List<ChannelMediaContentType> { ChannelMediaContentType.Movie },
            MediaTypes = new List<ChannelMediaType> { ChannelMediaType.Video },
            MaxPageSize = 100
        };

        /// <summary>Kanalen syns bara för tittare, alltså Emby-användare med ett Seerr-konto.</summary>
        public bool IsEnabledFor(string userId)
            => Guid.TryParse(userId, out var id) && P.Tittare.SeerrIdFor(id).HasValue;

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken ct)
        {
            List<ChannelItemInfo> poster;
            try
            {
                if (!await P.SeerrStatus.ArUppeAsync(ct).ConfigureAwait(false))
                {
                    _log.Warn("Beställningskanalen: Seerr svarar inte på {0}; visar 'inte tillgängligt'.", P.Configuration.SeerrAdress);
                    poster = new List<ChannelItemInfo> { Mapp(Otillganglig, "Beställningar är inte tillgängliga just nu") };
                }
                else if (string.IsNullOrEmpty(query.FolderId))
                    poster = Rot();
                else if (query.FolderId == Mina)
                    poster = await MinaBestallningarAsync(query, ct).ConfigureAwait(false);
                else
                    poster = await ListaAsync(query.FolderId, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _log.ErrorException("Kunde inte hämta innehåll till beställningskanalen", ex);
                poster = new List<ChannelItemInfo> { Mapp(Otillganglig, "Beställningar är inte tillgängliga just nu") };
            }

            return new ChannelItemResult { Items = poster, TotalRecordCount = poster.Count };
        }

        public async Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaInfo(string id, CancellationToken ct)
        {
            var uppe = await P.SeerrStatus.ArUppeAsync(ct).ConfigureAwait(false);
            var video = uppe ? P.BestalldVideo : P.EjTillgangligVideo;
            return new[]
            {
                new MediaSourceInfo
                {
                    Id = id,
                    Path = video,
                    Protocol = MediaProtocol.File,
                    Container = "mp4",
                    IsRemote = false,
                    SupportsDirectPlay = true,
                    SupportsDirectStream = true,
                    SupportsTranscoding = true
                }
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken ct)
            => Task.FromResult(new DynamicImageResponse());

        public IEnumerable<ImageType> GetSupportedChannelImages() => Array.Empty<ImageType>();

        private static List<ChannelItemInfo> Rot()
        {
            var rot = new List<ChannelItemInfo> { Mapp(Mina, "Mina beställningar") };
            rot.AddRange(Listor.Select(l => Mapp(l.Id, l.Namn)));
            return rot;
        }

        private static async Task<List<ChannelItemInfo>> ListaAsync(string mappId, CancellationToken ct)
        {
            var lista = Listor.FirstOrDefault(l => l.Id == mappId);
            if (lista.Id == null) return new List<ChannelItemInfo>();

            var filmer = await P.Forslag.HamtaAsync(lista.Lista, ct).ConfigureAwait(false);
            return filmer.Select(f =>
            {
                return new ChannelItemInfo
            {
                Id = FilmPrefix + f.Id,
                Name = f.Title,
                Overview = f.Overview,
                ProductionYear = f.Ar,
                PremiereDate = DateTime.TryParse(f.ReleaseDate, out var d) ? d : (DateTime?)null,
                CommunityRating = f.VoteAverage.HasValue ? (float)f.VoteAverage.Value : (float?)null,
                ImageUrl = Poster(f.PosterPath),
                Type = ChannelItemType.Media,
                ContentType = ChannelMediaContentType.Movie,
                MediaType = ChannelMediaType.Video
            };
            }).ToList();
        }

        private async Task<List<ChannelItemInfo>> MinaBestallningarAsync(InternalChannelItemQuery query, CancellationToken ct)
        {
            var anvandare = _users.GetUserById(query.UserId);
            if (anvandare == null) return new List<ChannelItemInfo>();

            var seerrId = await P.Tittare.SeerrIdForAsync(anvandare.Id, ct).ConfigureAwait(false);
            if (!seerrId.HasValue) return new List<ChannelItemInfo>();

            var bestallningar = await P.Seerr.BestallningarForAsync(seerrId.Value, ct).ConfigureAwait(false);
            var nu = DateTime.UtcNow;
            var poster = new List<ChannelItemInfo>();

            foreach (var b in bestallningar.Where(b => b.Type == "movie" && b.Media?.TmdbId != null))
            {
                var status = Bestallningsregler.Statustext(b.Status, b.Media.Status, b.UpdatedAt.ToUniversalTime(), nu);
                if (status == null) continue;

                var nedladdning = b.Media.DownloadStatus?.FirstOrDefault(d => d.Size > 0);
                if (nedladdning != null)
                {
                    var forlopp = Nedladdningsstatus.Text(nedladdning.Size, nedladdning.SizeLeft,
                        nedladdning.EstimatedCompletionTime?.ToUniversalTime(), nu);
                    if (forlopp != null) status = $"Laddas ner, {forlopp}";
                }

                var tmdb = b.Media.TmdbId.Value;
                FilmDetaljer film = null;
                try { film = await P.Forslag.FilmAsync(tmdb, ct).ConfigureAwait(false); }
                catch (Exception ex) { _log.Debug("Kunde inte hämta titel för tmdb {0}: {1}", tmdb, ex.Message); }

                // Statusposter är mappar, inte filmer, så att de inte kan spelas och beställas igen.
                var post = Mapp(StatusPrefix + tmdb, $"{film?.Title ?? "Okänd film"} – {status}");
                post.ImageUrl = Poster(film?.PosterPath);
                post.Overview = film?.Overview;
                poster.Add(post);
            }

            if (poster.Count == 0)
                poster.Add(Mapp(StatusPrefix + "tom", "Du har inga pågående beställningar"));

            return poster;
        }

        private static ChannelItemInfo Mapp(string id, string namn) => new ChannelItemInfo
        {
            Id = id,
            Name = namn,
            Type = ChannelItemType.Folder,
            FolderType = ChannelFolderType.Container
        };

        private static string Poster(string posterPath)
            => string.IsNullOrEmpty(posterPath) ? null : "https://image.tmdb.org/t/p/w500" + posterPath;
    }
}
