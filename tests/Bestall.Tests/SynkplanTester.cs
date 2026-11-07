using System.Linq;
using Bestall.Domain;
using Xunit;

namespace Bestall.Tests
{
    public class SynkplanTester
    {
        [Fact]
        public void Lagger_till_nya_och_tar_bort_gamla()
        {
            var plan = Synkplan.Berakna(new[] { 1, 2, 3 }, new[] { 2, 3, 4 });
            Assert.Equal(new[] { 1 }, plan.LaggTill);
            Assert.Equal(new[] { 4 }, plan.TaBort);
            Assert.False(plan.BorttagningSparrad);
        }

        [Fact]
        public void Sparrar_borttagning_om_urvalet_plotsligt_krymper_kraftigt()
        {
            var befintliga = Enumerable.Range(1, 1000).ToArray();
            var plan = Synkplan.Berakna(new[] { 1, 2, 3 }, befintliga);
            Assert.True(plan.BorttagningSparrad);
            Assert.Empty(plan.TaBort);
        }

        [Fact]
        public void Litet_bibliotek_far_tommas_normalt()
        {
            var plan = Synkplan.Berakna(new int[0], new[] { 1, 2, 3 });
            Assert.False(plan.BorttagningSparrad);
            Assert.Equal(3, plan.TaBort.Count);
        }

        [Fact]
        public void Forsta_korningen_lagger_till_allt()
        {
            var plan = Synkplan.Berakna(Enumerable.Range(1, 5000).ToArray(), new int[0]);
            Assert.Equal(5000, plan.LaggTill.Count);
            Assert.Empty(plan.TaBort);
        }
    }
}
