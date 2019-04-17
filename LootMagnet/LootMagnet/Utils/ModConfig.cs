
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
        public float[] HoldbackTriggerChance = new float[] { 60f, 40f, 20f, 16f, 8f, 4f, 2f, 1f };

        // The percentage of the total salvage list AFTER GROUPING that will be held back (rounded up)
        public float HoldbackPicksGreed = 5.0f;

        // The holdback percentage for any given item
        // loathed, hated, disliked, indifferent, liked, friendly, allied
        public int[] HoldbackPicksModifier = new int[] { +3, +2, +1, 0, -1, -2, -3 };

        // If true, the employer will roll on each and every mech part
        public bool AlwaysHoldbackMechs = false;

        // TODO: Print multiplier values
        public override string ToString() {
            string rollupMRBVal = string.Join(", ", RollupMRBValue.Select(v => v.ToString("0.00")).ToArray());
            string rollupFactComponentVal = string.Join(", ", RollupFactionComponentMulti.Select(v => v.ToString("0.00")).ToArray());
            string rollupFactMechVal = string.Join(", ", RollupFactionMechMulti.Select(v => v.ToString("0.00")).ToArray());
            string holdbackTrigger = string.Join(", ", HoldbackTriggerChance.Select(v => v.ToString("0.00")).ToArray());
            string holdbackPicks = string.Join(", ", HoldbackPicksModifier.Select(v => v.ToString("0")).ToArray());

            return $"Debug:{Debug} DeveloperMode:{DeveloperMode}\n " +
                $"Rollup Components MRBValues:[{rollupMRBVal}] x FactMulti:[{rollupFactComponentVal}]\n " +
                $"Rollup Mechs MRBValues:[{rollupMRBVal}] x FactMulti:[{rollupFactMechVal}]\n " +
                $"Holdback Chance:{holdbackTrigger} PicksGreed:{HoldbackPicksGreed} AdditionalPicks:{holdbackPicks}";
        }
    }
}
