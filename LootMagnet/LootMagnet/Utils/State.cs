using BattleTech;
using System.Collections.Generic;

namespace LootMagnet {

    public static class State {
        public static Faction Employer = Faction.INVALID_UNSET;
        public static SimGameReputation EmployerRep = SimGameReputation.INDIFFERENT;
        public static int EmployerRepRaw = 0;
        public static bool IsEmployerAlly = false;
        public static int MRBRating = 0;
        public static List<SalvageDef> HeldbackItems = new List<SalvageDef>();
    }

}


