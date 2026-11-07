using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bestall.Seerr
{
    /// <summary>Om Seerr går att nå just nu. Kontrolleras högst en gång per minut.</summary>
    public sealed class SeerrStatus
    {
        private static readonly TimeSpan Giltighet = TimeSpan.FromMinutes(1);

        private readonly SeerrKlient _seerr;
        private readonly SemaphoreSlim _las = new SemaphoreSlim(1, 1);
        private DateTime _kontrolleradUtc = DateTime.MinValue;
        private bool _uppe;

        public SeerrStatus(SeerrKlient seerr) => _seerr = seerr;

        public async Task<bool> ArUppeAsync(CancellationToken ct)
        {
            if (DateTime.UtcNow - _kontrolleradUtc < Giltighet) return _uppe;

            await _las.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (DateTime.UtcNow - _kontrolleradUtc < Giltighet) return _uppe;
                _uppe = await _seerr.SvararAsync(ct).ConfigureAwait(false);
                _kontrolleradUtc = DateTime.UtcNow;
                return _uppe;
            }
            finally
            {
                _las.Release();
            }
        }

        /// <summary>Tvingar en ny kontroll nästa gång, t.ex. efter att inställningarna ändrats.</summary>
        public void Glom() => _kontrolleradUtc = DateTime.MinValue;
    }
}
