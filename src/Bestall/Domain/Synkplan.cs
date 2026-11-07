using System.Collections.Generic;
using System.Linq;

namespace Bestall.Domain
{
    /// <summary>Vad den dagliga synken ska lägga till och ta bort i urvalet.</summary>
    public sealed class Synkplan
    {
        public IReadOnlyList<int> LaggTill { get; }
        public IReadOnlyList<int> TaBort { get; }

        /// <summary>
        /// Sant om borttagning stoppats för att det önskade urvalet ser misstänkt litet ut,
        /// t.ex. för att Seerr svarade ofullständigt. Hellre för många platshållare än ett tömt bibliotek.
        /// </summary>
        public bool BorttagningSparrad { get; }

        private Synkplan(IReadOnlyList<int> laggTill, IReadOnlyList<int> taBort, bool sparrad)
        {
            LaggTill = laggTill;
            TaBort = taBort;
            BorttagningSparrad = sparrad;
        }

        public static Synkplan Berakna(ICollection<int> onskade, ICollection<int> befintliga)
        {
            var onskadeSet = new HashSet<int>(onskade);
            var befintligaSet = new HashSet<int>(befintliga);

            var laggTill = onskade.Where(id => !befintligaSet.Contains(id)).Distinct().ToList();
            var taBort = befintliga.Where(id => !onskadeSet.Contains(id)).Distinct().ToList();

            var sparrad = befintligaSet.Count >= 100 && onskadeSet.Count < befintligaSet.Count / 10;
            return new Synkplan(laggTill, sparrad ? new List<int>() : taBort, sparrad);
        }
    }
}
