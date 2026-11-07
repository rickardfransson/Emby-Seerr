using Bestall.Domain;
using Xunit;

namespace Bestall.Tests
{
    public class NotistextTester
    {
        [Fact]
        public void Fyller_i_titel_och_ar()
            => Assert.Equal("Dune (2021) finns nu att se i biblioteket.",
                Notistext.Formatera(Notistext.StandardText, "Dune", 2021, Notistext.StandardText));

        [Fact]
        public void Egen_mall_med_falten_i_annan_ordning()
            => Assert.Equal("Nu kan du se Dune från 2021!",
                Notistext.Formatera("Nu kan du se {titel} från {år}!", "Dune", 2021, Notistext.StandardText));

        [Fact]
        public void Falten_ar_okansliga_for_skiftlage()
            => Assert.Equal("Dune 2021", Notistext.Formatera("{Titel} {ÅR}", "Dune", 2021, ""));

        [Fact]
        public void Tom_mall_ger_reservtexten()
            => Assert.Equal("Din film har kommit", Notistext.Formatera("  ", "Dune", 2021, Notistext.StandardRubrik));

        [Fact]
        public void Saknat_ar_lamnar_inga_tomma_parenteser()
            => Assert.Equal("Dune finns nu att se i biblioteket.",
                Notistext.Formatera(Notistext.StandardText, "Dune", null, Notistext.StandardText));

        [Fact]
        public void Titel_som_innehaller_faltnamnet_ersatts_inte_igen()
            => Assert.Equal("Filmen {titel}", Notistext.Formatera("Filmen {titel}", "{titel}", null, ""));
    }
}
