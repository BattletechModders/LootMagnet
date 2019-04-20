
using BattleTech;
using System.Collections.Generic;
using System.Linq;

namespace LootMagnet {
    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;

        // If true, short-cuts many checks for testing purposes. DO NOT SET TRUE FOR RELEASE
        public bool DeveloperMode = false;

        // The values used to define the base amounts for rollup
        public float[] RollupMRBValue = new float[] { 40000f, 60000f, 90000f, 130000f, 180000f, 240000f };

        // How much the rollup values should be multiplied for components
        // loathed, hated, disliked, indifferent, liked, friendly, allied
        public float[] RollupFactionComponentMulti = new float[] { 0f, 0f, 0f, 1f, 5f, 9f, 13f, 21f };

        // How much the rollup values should be multipled for mech components
        // loathed, hated, disliked, indifferent, liked, friendly, allied
        public float[] RollupFactionMechMulti = new float[] { 0f, 0f, 0f, 0f, 0f, 20f, 30f, 180f };

        // The holdback percentage for any given item. 
        // loathed, hated, disliked, indifferent, liked, friendly, allied
        //public float[] HoldbackTriggerChance = new float[] { 60f, 40f, 20f, 16f, 8f, 4f, 2f, 1f };
        public float[] HoldbackTriggerChance = new float[] { 99f, 99f, 99f, 99f, 99f, 99f, 99f, 99f };

        // The holdback percentage for any given item
        public int[] HoldbackPickRange = new int[] { 2, 6 };

        // Acceptance reputation gain is equal to the the max possible reputation of the contract times this value
        public float HoldbackAcceptRepMulti = 0.5f;

        // Refusal reputation loss is equal to the the max possible reputation of the contract times this value
        public float HoldbackRefuseRepMulti = -2.5f;

        public float DisputeSuccessBase = 40.0f;
        public float DisputeCritChance = 5.0f;

        public float DisputeMRBSuccessFactor = 10.0f;
        public int DisputeSuccessRandomBound = 10;
        public float DisputePayoutRandomBound = 0.7f;

        public float DisputeMRBFeeFactor = -0.1f;
        public float DisputeMRBRepPenalty = -0.3f;

        public float DisputeSuccessRepPenaltyFactor = -0.8f;

        public float DisputeFailRepPenaltyFactor = -0.3f;
        public float DisputeFailPayoutFactor = -0.5f;

        public float DisputeCritFailRepPenaltyFactor = -0.1f;
        public float DisputeCritFailPayoutFactor = -1.5f;


        // TODO: Print multiplier values
        public override string ToString() {
            string rollupMRBVal = string.Join(", ", RollupMRBValue.Select(v => v.ToString("0.00")).ToArray());
            string rollupFactComponentVal = string.Join(", ", RollupFactionComponentMulti.Select(v => v.ToString("0.00")).ToArray());
            string rollupFactMechVal = string.Join(", ", RollupFactionMechMulti.Select(v => v.ToString("0.00")).ToArray());
            string holdbackTrigger = string.Join(", ", HoldbackTriggerChance.Select(v => v.ToString("0.00")).ToArray());
            string holdbackPicks = string.Join(", ", HoldbackPickRange.Select(v => v.ToString("0")).ToArray());

            return $"Debug:{Debug} DeveloperMode:{DeveloperMode}\n " +
                $"Rollup\n" +
                $"  Components MRBValues:[{rollupMRBVal}] x FactMulti:[{rollupFactComponentVal}]\n " +
                $"  Mechs MRBValues:[{rollupMRBVal}] x FactMulti:[{rollupFactMechVal}]\n " +
                $"Holdback\n" +
                $" Chance:{holdbackTrigger} Picks:{holdbackPicks}\n" +
                $" AcceptMulti:{HoldbackAcceptRepMulti} RefusalMulti:{HoldbackRefuseRepMulti}";
        }
    }
}
