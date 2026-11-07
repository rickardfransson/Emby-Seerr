using Bestall.Domain;
using Xunit;

namespace Bestall.Tests
{
    public class PlatshallarnamnTester
    {
        [Fact]
        public void Visningstitel_har_tillagget_sist_sa_att_sok_traffar()
            => Assert.Equal("Dune (beställ)", Platshallarnamn.Visningstitel("Dune "));

        [Fact]
        public void Mappnamn_innehaller_ar_och_tmdb_id()
            => Assert.Equal("Dune (2021) [tmdbid-438631]", Platshallarnamn.Mappnamn("Dune", 2021, 438631));

        [Fact]
        public void Mappnamn_rensar_tecken_som_inte_far_finnas_i_filnamn()
            => Assert.Equal("Mission Impossible - Dead Reckoning (2023) [tmdbid-575264]",
                Platshallarnamn.Mappnamn("Mission: Impossible - Dead Reckoning", 2023, 575264));

        [Fact]
        public void Mappnamn_utan_giltiga_tecken_far_reservnamn()
            => Assert.Equal("Film (2020) [tmdbid-1]", Platshallarnamn.Mappnamn("???", 2020, 1));

        [Theory]
        [InlineData("/media/bestall/Dune (2021) [tmdbid-438631]/Dune (2021) [tmdbid-438631].strm", 438631)]
        [InlineData(@"D:\bestall\Dune (2021) [TMDBID-438631]", 438631)]
        [InlineData("/media/filmer/Dune (2021)/Dune.mkv", null)]
        [InlineData(null, null)]
        public void Tmdb_id_hittas_i_sokvag(string sokvag, int? forvantat)
            => Assert.Equal(forvantat, Platshallarnamn.TmdbIdFranSokvag(sokvag));

        [Theory]
        [InlineData("/media/bestall/Dune/Dune.strm", "/media/bestall", true)]
        [InlineData("/media/bestall/Dune/Dune.strm", "/media/bestall/", true)]
        [InlineData("/media/bestall-annat/Dune/Dune.strm", "/media/bestall", false)]
        [InlineData(@"D:\Bestall\Dune\Dune.strm", @"d:\bestall", true)]
        [InlineData("/media/filmer/Dune.mkv", "", false)]
        public void Avgor_om_sokvag_ligger_i_platshallarmappen(string sokvag, string mapp, bool forvantat)
            => Assert.Equal(forvantat, Platshallarnamn.LiggerI(sokvag, mapp));
    }
}
