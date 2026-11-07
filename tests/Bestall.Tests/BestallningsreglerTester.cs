using System;
using Bestall.Domain;
using Xunit;

namespace Bestall.Tests
{
    public class BestallningsreglerTester
    {
        private static readonly DateTime Nu = new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData(null, true)]
        [InlineData(1, true)]   // okänd
        [InlineData(2, false)]  // väntande
        [InlineData(3, false)]  // bearbetas
        [InlineData(5, false)]  // finns
        public void Bara_okanda_filmer_ar_bestallningsbara(int? mediaStatus, bool forvantat)
            => Assert.Equal(forvantat, Bestallningsregler.ArBestallningsbar(mediaStatus));

        [Fact]
        public void Vantande_bestallning_visas_som_vantar_pa_godkannande()
            => Assert.Equal("Väntar på godkännande", Bestallningsregler.Statustext(1, 2, Nu, Nu));

        [Fact]
        public void Godkand_bestallning_som_laddas_ner()
            => Assert.Equal("Laddas ner", Bestallningsregler.Statustext(2, 3, Nu, Nu));

        [Fact]
        public void Godkand_bestallning_som_inte_borjat_laddas_ner()
            => Assert.Equal("Godkänd", Bestallningsregler.Statustext(2, 2, Nu, Nu));

        [Fact]
        public void Film_som_finns_visas_inte_i_mina_bestallningar()
        {
            Assert.Null(Bestallningsregler.Statustext(2, 5, Nu, Nu));
            Assert.Null(Bestallningsregler.Statustext(5, 5, Nu, Nu));
        }

        [Fact]
        public void Avslagen_syns_i_sju_dagar()
        {
            Assert.Equal("Avslagen", Bestallningsregler.Statustext(3, 1, Nu.AddDays(-7), Nu));
            Assert.Null(Bestallningsregler.Statustext(3, 1, Nu.AddDays(-7).AddMinutes(-1), Nu));
        }

        [Fact]
        public void Misslyckad_bestallning_visas()
            => Assert.Equal("Misslyckades", Bestallningsregler.Statustext(4, 2, Nu, Nu));
    }
}
