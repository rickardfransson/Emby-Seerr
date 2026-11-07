using System;

namespace Bestall.Domain
{
    /// <summary>Seerrs status för en film (media), som Seerr anger den.</summary>
    public enum MediaStatus
    {
        Okand = 1,
        Vantande = 2,
        Bearbetas = 3,
        DelvisTillganglig = 4,
        Tillganglig = 5
    }

    /// <summary>Seerrs status för en beställning (request).</summary>
    public enum Bestallningsstatus
    {
        VantarPaGodkannande = 1,
        Godkand = 2,
        Avslagen = 3,
        Misslyckad = 4,
        Klar = 5
    }

    /// <summary>Regler för vad som är beställningsbart och hur en beställning visas för tittaren.</summary>
    public static class Bestallningsregler
    {
        /// <summary>Hur länge en avslagen beställning syns i Mina beställningar.</summary>
        public static readonly TimeSpan AvslagenSynsI = TimeSpan.FromDays(7);

        /// <summary>
        /// En film är beställningsbar om Seerr inte känner till den alls, eller bara som okänd.
        /// Allt annat betyder att den redan är beställd, på väg eller finns.
        /// </summary>
        public static bool ArBestallningsbar(int? mediaStatus)
            => mediaStatus == null || mediaStatus == (int)MediaStatus.Okand;

        /// <summary>
        /// Texten som visas efter filmens titel i Mina beställningar,
        /// eller null om beställningen inte ska visas där.
        /// </summary>
        public static string Statustext(int bestallningsstatus, int? mediaStatus, DateTime uppdateradUtc, DateTime nuUtc)
        {
            if (mediaStatus == (int)MediaStatus.Tillganglig)
                return null;

            switch ((Bestallningsstatus)bestallningsstatus)
            {
                case Bestallningsstatus.VantarPaGodkannande:
                    return "Väntar på godkännande";
                case Bestallningsstatus.Godkand:
                    return mediaStatus == (int)MediaStatus.Bearbetas ? "Laddas ner" : "Godkänd";
                case Bestallningsstatus.Avslagen:
                    return nuUtc - uppdateradUtc <= AvslagenSynsI ? "Avslagen" : null;
                case Bestallningsstatus.Misslyckad:
                    return "Misslyckades";
                default:
                    return null;
            }
        }
    }
}
