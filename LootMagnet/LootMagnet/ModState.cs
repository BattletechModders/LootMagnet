using BattleTech;
using BattleTech.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LootMagnet {

    public static class ModState {
        public static FactionValue Employer = null;
        public static SimGameReputation EmployerRep = SimGameReputation.INDIFFERENT;
        public static int EmployerRepRaw = 0;
        public static bool IsEmployerAlly = false;
        public static int MRBRating = 0;

        public static double Compensation = 0;
        public static List<SalvageDef> PotentialSalvage = new List<SalvageDef>();
        public static List<SalvageDef> HeldbackParts = new List<SalvageDef>();
        public static List<SalvageDef> CompensationParts = new List<SalvageDef>();

        // State used for QuickSell
        public static Contract Contract;
        public static SimGameState SimGameState;
        public static AAR_SalvageScreen AAR_SalvageScreen;
        public static SGCurrencyDisplay SGCurrencyDisplay;
        public static GameObject HBSPopupRoot;
        public static TMP_FontAsset FloatieFont;

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

            Contract = null;
            SimGameState = null;
            AAR_SalvageScreen = null;
            SGCurrencyDisplay = null;
            HBSPopupRoot = null;
            FloatieFont = null;
        }
    }

}


