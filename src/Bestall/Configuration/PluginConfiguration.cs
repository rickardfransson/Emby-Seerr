using MediaBrowser.Model.Plugins;

namespace Bestall.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>Seerrs adress sedd från Emby-servern, t.ex. http://192.168.1.10:5055.</summary>
        public string SeerrAdress { get; set; } = "";

        /// <summary>API-nyckel från Seerr (Inställningar → Allmänt).</summary>
        public string SeerrApiNyckel { get; set; } = "";

        /// <summary>Mapp där platshållarna skrivs. Läggs till som ett eget filmbibliotek i Emby.</summary>
        public string PlatshallarMapp { get; set; } = "";

        /// <summary>Embys egen adress sedd från servern själv. Platshållarnas videoadress pekar hit.</summary>
        public string EmbyAdress { get; set; } = "http://127.0.0.1:8096";

        /// <summary>Hur många röster en film minst ska ha på TMDB för att komma med i urvalet.</summary>
        public int MinAntalRoster { get; set; } = 50;

        /// <summary>Lägsta TMDB-betyg (0–10) för att en film ska komma med i urvalet.</summary>
        public double MinBetyg { get; set; } = 5.9;

        /// <summary>Tak för antalet platshållare.</summary>
        public int MaxAntalPlatshallare { get; set; } = 7500;

        /// <summary>Antal filmer per förslagslista i beställningskanalen.</summary>
        public int FilmerPerForslagslista { get; set; } = 40;

        /// <summary>Språk för titlar och handling.</summary>
        public string Sprak { get; set; } = "sv";

        /// <summary>Länder att hämta åldersgräns från, i prioritetsordning. Embys föräldrakontroll förstår US bäst.</summary>
        public string AldersgransLander { get; set; } = "US,SE";
    }
}
