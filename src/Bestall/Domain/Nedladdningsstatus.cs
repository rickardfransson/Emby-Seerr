using System;

namespace Bestall.Domain
{
    /// <summary>Hur långt en nedladdning kommit, som text efter "Laddas ner" i Mina beställningar.</summary>
    public static class Nedladdningsstatus
    {
        /// <summary>T.ex. "45 %, ca 12 min kvar". Null om storleken är okänd.</summary>
        public static string Text(long storlek, long kvar, DateTime? klarUtc, DateTime nuUtc)
        {
            if (storlek <= 0) return null;

            var procent = (int)Math.Floor(100.0 * Math.Max(0, storlek - Math.Max(0, kvar)) / storlek);
            procent = Math.Max(0, Math.Min(100, procent));

            var tid = klarUtc.HasValue ? Tid(klarUtc.Value - nuUtc) : null;
            return tid == null ? $"{procent} %" : $"{procent} %, {tid}";
        }

        private static string Tid(TimeSpan aterstar)
        {
            if (aterstar <= TimeSpan.Zero) return "snart klar";
            if (aterstar < TimeSpan.FromMinutes(1)) return "under 1 min kvar";
            if (aterstar < TimeSpan.FromHours(1)) return $"ca {(int)Math.Ceiling(aterstar.TotalMinutes)} min kvar";

            var timmar = (int)aterstar.TotalHours;
            var minuter = aterstar.Minutes;
            if (timmar >= 24) return $"ca {(int)Math.Round(aterstar.TotalDays)} dygn kvar";
            return minuter == 0 ? $"ca {timmar} tim kvar" : $"ca {timmar} tim {minuter} min kvar";
        }
    }
}
