using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Bestall.Notiser
{
    /// <summary>
    /// Sparar på disk vilka beställningar som redan fått en notis och vilka notiser som väntar på att tittaren
    /// ska öppna appen. Överlever omstarter av Emby.
    /// </summary>
    public sealed class Notislager
    {
        private static readonly TimeSpan VantarHogst = TimeSpan.FromDays(14);
        private const int SparaHogst = 5000;

        private readonly string _fil;
        private readonly object _las = new object();
        private Data _data;

        public Notislager(string fil)
        {
            _fil = fil;
            _data = Las();
        }

        public bool ArInitierad { get { lock (_las) return _data.Initierad; } }

        /// <summary>Första körningen: markera alla redan tillgängliga beställningar som klara, så att ingen får gamla notiser.</summary>
        public void Initiera(IEnumerable<int> befintligaBestallningar)
        {
            lock (_las)
            {
                _data.Notifierade = befintligaBestallningar.Distinct().ToList();
                _data.Initierad = true;
                Spara();
            }
        }

        public bool HarNotifierat(int bestallningsId) { lock (_las) return _data.Notifierade.Contains(bestallningsId); }

        public void LaggTill(int bestallningsId, Guid embyAnvandare, string titel, int? ar)
        {
            lock (_las)
            {
                if (_data.Notifierade.Contains(bestallningsId)) return;
                _data.Notifierade.Add(bestallningsId);
                if (_data.Notifierade.Count > SparaHogst)
                    _data.Notifierade.RemoveRange(0, _data.Notifierade.Count - SparaHogst);

                _data.Vantande.Add(new VantandeNotis
                {
                    EmbyAnvandarId = embyAnvandare.ToString("N"),
                    Titel = titel,
                    Ar = ar,
                    SkapadUtc = DateTime.UtcNow
                });
                Spara();
            }
        }

        /// <summary>Väntande notiser för en tittare. Gamla notiser rensas bort.</summary>
        public List<VantandeNotis> VantandeFor(Guid embyAnvandare)
        {
            lock (_las)
            {
                var gransen = DateTime.UtcNow - VantarHogst;
                if (_data.Vantande.RemoveAll(n => n.SkapadUtc < gransen) > 0) Spara();
                var id = embyAnvandare.ToString("N");
                return _data.Vantande.Where(n => n.EmbyAnvandarId == id).ToList();
            }
        }

        public List<Guid> AnvandareMedVantande()
        {
            lock (_las)
                return _data.Vantande.Select(n => Guid.TryParse(n.EmbyAnvandarId, out var g) ? g : Guid.Empty)
                    .Where(g => g != Guid.Empty).Distinct().ToList();
        }

        public void Levererad(VantandeNotis notis)
        {
            lock (_las)
            {
                _data.Vantande.RemoveAll(n => n.EmbyAnvandarId == notis.EmbyAnvandarId && n.Titel == notis.Titel && n.SkapadUtc == notis.SkapadUtc);
                Spara();
            }
        }

        private Data Las()
        {
            try
            {
                if (File.Exists(_fil)) return JsonSerializer.Deserialize<Data>(File.ReadAllText(_fil)) ?? new Data();
            }
            catch
            {
                // Trasig fil: börja om. Värsta fallet är att gamla notiser inte visas.
            }
            return new Data();
        }

        private void Spara()
        {
            var tmp = _fil + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_data));
            if (File.Exists(_fil)) File.Delete(_fil);
            File.Move(tmp, _fil);
        }

        private sealed class Data
        {
            public bool Initierad { get; set; }
            public List<int> Notifierade { get; set; } = new List<int>();
            public List<VantandeNotis> Vantande { get; set; } = new List<VantandeNotis>();
        }
    }

    public sealed class VantandeNotis
    {
        public string EmbyAnvandarId { get; set; }
        public string Titel { get; set; }
        public int? Ar { get; set; }
        public DateTime SkapadUtc { get; set; }
    }
}
