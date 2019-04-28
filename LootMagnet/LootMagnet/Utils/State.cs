using BattleTech;
using System.Collections.Generic;

namespace LootMagnet {

    public static class State {
        public static Faction Employer = Faction.INVALID_UNSET;
        public static SimGameReputation EmployerRep = SimGameReputation.INDIFFERENT;
        public static int EmployerRepRaw = 0;
        public static bool IsEmployerAlly = false;
        public static int MRBRating = 0;

        public static double Compensation = 0;
        public static List<SalvageDef> HeldbackParts = new List<SalvageDef>();
        public static List<SalvageDef> CompensationParts = new List<SalvageDef>();

        public static void Reset() {
            // Reinitialize state
            State.Employer = Faction.INVALID_UNSET;
            State.EmployerRep = SimGameReputation.INDIFFERENT;
            State.EmployerRepRaw = 0;
            State.IsEmployerAlly = false;
            State.MRBRating = 0;

            State.Compensation = 0;
            State.HeldbackParts.Clear();
            State.CompensationParts.Clear();
        }
    }

}


