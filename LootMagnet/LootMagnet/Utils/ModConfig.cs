
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
        // loathed, hated, disliked, indifferent, liked, friendly, allied
        public int[] HoldbackPicks = new int[] { 6, 4, 3, 2, 2, 1, 1 };

        // Acceptance reputation gain is equal to the the max possible reputation of the contract times this value
        public float HoldbackAcceptMulti = 0.5f;

        // Refusal reputation loss is equal to the the max possible reputation of the contract times this value
        public float HoldbackRefusalMulti = 2.5f;

        // Dispute reputation loss is equal to the max possible reputation of the contract times this value
        public float HoldbackDisputeMulti = 0.75f;

        // In a dispute, what's the base success chance you have.
        public float HoldbackDisputeBaseChance = 40.0f;

        // For each point of MRB rating, increase the success chance by this amount
        public float HoldbackDisputeMRBFactor = 10.0f;

        // In a dispute, what's the chance of a critical failure (lose all equipment) or success (gain the items automatically)
        public float HoldbackDisputeCriticalChance = 5.0f;

        // The percentage of the total items cost that must be paid in legal fees
        public float HoldbackDisputePayoutMulti = 1f;

        public float HoldbackDisputeCriticalPayoutMulti = 3f;

        // TODO: Print multiplier values
        public override string ToString() {
            string rollupMRBVal = string.Join(", ", RollupMRBValue.Select(v => v.ToString("0.00")).ToArray());
            string rollupFactComponentVal = string.Join(", ", RollupFactionComponentMulti.Select(v => v.ToString("0.00")).ToArray());
            string rollupFactMechVal = string.Join(", ", RollupFactionMechMulti.Select(v => v.ToString("0.00")).ToArray());
            string holdbackTrigger = string.Join(", ", HoldbackTriggerChance.Select(v => v.ToString("0.00")).ToArray());
            string holdbackPicks = string.Join(", ", HoldbackPicks.Select(v => v.ToString("0")).ToArray());

            return $"Debug:{Debug} DeveloperMode:{DeveloperMode}\n " +
                $"Rollup\n" +
                $"  Components MRBValues:[{rollupMRBVal}] x FactMulti:[{rollupFactComponentVal}]\n " +
                $"  Mechs MRBValues:[{rollupMRBVal}] x FactMulti:[{rollupFactMechVal}]\n " +
                $"Holdback\n" +
                $" Chance:{holdbackTrigger} Picks:{holdbackPicks}\n" +
                $" AcceptMulti:{HoldbackAcceptMulti} RefusalMulti:{HoldbackRefusalMulti} DisputeMulti:{HoldbackDisputeMulti}";
        }
    }
}
