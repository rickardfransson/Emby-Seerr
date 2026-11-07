using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Bestall.Seerr
{
    /// <summary>
    /// Kopplar Emby-användare till Seerr-konton. En tittare är en Emby-användare som har ett Seerr-konto;
    /// andra Emby-användare kan inte beställa. Hämtas om var tionde minut.
    /// </summary>
    public sealed class Tittarregister
    {
        private static readonly TimeSpan Giltighet = TimeSpan.FromMinutes(10);

        private readonly SeerrKlient _seerr;
        private readonly ILogger _log;
        private readonly SemaphoreSlim _las = new SemaphoreSlim(1, 1);
        private Dictionary<Guid, int> _koppling = new Dictionary<Guid, int>();
        private DateTime _hamtadUtc = DateTime.MinValue;

        public Tittarregister(SeerrKlient seerr, ILogger log)
        {
            _seerr = seerr;
            _log = log;
        }

        /// <summary>Seerr-användarens id för en Emby-användare, eller null om hen inte är en tittare.</summary>
        public async Task<int?> SeerrIdForAsync(Guid embyAnvandarId, CancellationToken ct)
        {
            await UppdateraVidBehovAsync(ct).ConfigureAwait(false);
            return _koppling.TryGetValue(embyAnvandarId, out var id) ? id : (int?)null;
        }

        /// <summary>Synkron variant för Embys synkrona anrop (t.ex. om kanalen ska visas). Använder senast kända koppling om Seerr är långsamt.</summary>
        public int? SeerrIdFor(Guid embyAnvandarId)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    UppdateraVidBehovAsync(cts.Token).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.Debug("Kunde inte uppdatera tittarregistret: {0}", ex.Message);
            }
            return _koppling.TryGetValue(embyAnvandarId, out var id) ? id : (int?)null;
        }

        public void Glom() => _hamtadUtc = DateTime.MinValue;

        private async Task UppdateraVidBehovAsync(CancellationToken ct)
        {
            if (DateTime.UtcNow - _hamtadUtc < Giltighet) return;

            await _las.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (DateTime.UtcNow - _hamtadUtc < Giltighet) return;
                if (!_seerr.ArKonfigurerad) { _koppling = new Dictionary<Guid, int>(); return; }

                var anvandare = await _seerr.AnvandareAsync(ct).ConfigureAwait(false);
                _koppling = anvandare
                    .Where(a => Guid.TryParse(a.EmbyAnvandarId, out _))
                    .GroupBy(a => Guid.Parse(a.EmbyAnvandarId))
                    .ToDictionary(g => g.Key, g => g.First().Id);
                _hamtadUtc = DateTime.UtcNow;
                _log.Debug("Tittarregistret uppdaterat: {0} tittare", _koppling.Count);
            }
            finally
            {
                _las.Release();
            }
        }
    }
}
