using System;

namespace Bestall.Domain
{
    /// <summary>Fyller i notismallen som administratören skrivit i inställningarna.</summary>
    public static class Notistext
    {
        public const string StandardRubrik = "Din film har kommit";
        public const string StandardText = "{titel} ({år}) finns nu att se i biblioteket.";

        /// <summary>Ersätter {titel} och {år}. Tom mall ger standardtexten.</summary>
        public static string Formatera(string mall, string titel, int? ar, string reserv)
        {
            var text = string.IsNullOrWhiteSpace(mall) ? reserv : mall;
            text = Ersatt(text, "{titel}", titel ?? "");
            text = Ersatt(text, "{år}", ar?.ToString() ?? "");
            text = Ersatt(text, "{ar}", ar?.ToString() ?? "");

            // Utan år blir "Dune ()" kvar i standardtexten; ta bort tomma parenteser.
            return text.Replace(" ()", "").Replace("()", "").Trim();
        }

        private static string Ersatt(string text, string falt, string varde)
        {
            var i = text.IndexOf(falt, StringComparison.OrdinalIgnoreCase);
            while (i >= 0)
            {
                text = text.Substring(0, i) + varde + text.Substring(i + falt.Length);
                i = text.IndexOf(falt, i + varde.Length, StringComparison.OrdinalIgnoreCase);
            }
            return text;
        }
    }
}
