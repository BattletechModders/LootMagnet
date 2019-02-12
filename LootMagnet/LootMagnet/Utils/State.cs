using BattleTech;
using System.Collections.Generic;
using System.Linq;

namespace LootMagnet {

    public static class State {

        public static Dictionary<string, SalvageHolder> SalvageState = new Dictionary<string, SalvageHolder>();

        public static SimGameReputation EmployerReputation = SimGameReputation.INDIFFERENT;
        public static bool IsEmployerAlly = false;
        public static int MRBRating = 0;
    }

    public class SalvageHolder {
        public SalvageDef bucketDef;
        public SalvageDef remainderDef;
    }
}
