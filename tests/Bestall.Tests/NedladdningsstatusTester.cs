using System;
using Bestall.Domain;
using Xunit;

namespace Bestall.Tests
{
    public class NedladdningsstatusTester
    {
        private static readonly DateTime Nu = new DateTime(2026, 11, 7, 18, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Procent_och_minuter()
            => Assert.Equal("45 %, ca 12 min kvar", Nedladdningsstatus.Text(1000, 550, Nu.AddMinutes(11.5), Nu));

        [Fact]
        public void Timmar_och_minuter()
            => Assert.Equal("10 %, ca 2 tim 5 min kvar", Nedladdningsstatus.Text(100, 90, Nu.AddMinutes(125), Nu));

        [Fact]
        public void Jamna_timmar()
            => Assert.Equal("10 %, ca 3 tim kvar", Nedladdningsstatus.Text(100, 90, Nu.AddHours(3), Nu));

        [Fact]
        public void Mer_an_ett_dygn()
            => Assert.Equal("1 %, ca 2 dygn kvar", Nedladdningsstatus.Text(100, 99, Nu.AddHours(50), Nu));

        [Fact]
        public void Under_en_minut()
            => Assert.Equal("99 %, under 1 min kvar", Nedladdningsstatus.Text(100, 1, Nu.AddSeconds(20), Nu));

        [Fact]
        public void Passerad_tid_ar_snart_klar()
            => Assert.Equal("100 %, snart klar", Nedladdningsstatus.Text(100, 0, Nu.AddMinutes(-1), Nu));

        [Fact]
        public void Utan_tid_visas_bara_procent()
            => Assert.Equal("30 %", Nedladdningsstatus.Text(100, 70, null, Nu));

        [Fact]
        public void Okand_storlek_ger_ingen_text()
            => Assert.Null(Nedladdningsstatus.Text(0, 0, Nu, Nu));
    }
}
