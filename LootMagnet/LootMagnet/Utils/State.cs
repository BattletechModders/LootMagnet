using BattleTech;
using System.Collections.Generic;

namespace LootMagnet {

    public static class State {
        public static FactionValue Employer = null;
        public static SimGameReputation EmployerRep = SimGameReputation.INDIFFERENT;
        public static int EmployerRepRaw = 0;
        public static bool IsEmployerAlly = false;
        public static int MRBRating = 0;

        public static double Compensation = 0;
        public static List<SalvageDef> PotentialSalvage = new List<SalvageDef>();
        public static List<SalvageDef> HeldbackParts = new List<SalvageDef>();
        public static List<SalvageDef> CompensationParts = new List<SalvageDef>();

        public static void Reset() {
            // Reinitialize state to known values
            Employer = null;
            EmployerRep = SimGameReputation.INDIFFERENT;
            EmployerRepRaw = 0;
            IsEmployerAlly = false;
            MRBRating = 0;

            Compensation = 0;

            PotentialSalvage.Clear();
            HeldbackParts.Clear();
            CompensationParts.Clear();
        }
    }

}


