using System.Threading;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace Bestall.Api
{
    /// <summary>
    /// Adressen platshållarnas .strm pekar på. Visar bekräftelsevideon, eller "inte tillgängligt" om Seerr är nere.
    /// Gör aldrig någon beställning själv: Emby kan anropa adressen vid skanning, och här vet vi inte vem som tittar.
    /// </summary>
    [Route("/Bestall/Video", "GET")]
    [Route("/Bestall/Video", "HEAD")]
    [Unauthenticated]
    public class VideoAnrop
    {
        public int Tmdb { get; set; }
    }

    public class Videotjanst : IService, IRequiresRequest
    {
        private readonly IHttpResultFactory _svar;

        public Videotjanst(IHttpResultFactory svar) => _svar = svar;

        public IRequest Request { get; set; }

        public object Get(VideoAnrop anrop) => Video();

        public object Head(VideoAnrop anrop) => Video();

        private object Video()
        {
            var p = Plugin.Instance;
            var uppe = p.SeerrStatus.ArUppeAsync(CancellationToken.None).GetAwaiter().GetResult();
            return _svar.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                Path = uppe ? p.BestalldVideo : p.EjTillgangligVideo,
                ContentType = "video/mp4"
            });
        }
    }
}
