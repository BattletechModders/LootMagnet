
using System.Linq;

namespace LootMagnet {
    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;

        // If true, short-cuts many checks for testing purposes. DO NOT SET TRUE FOR RELEASE
        public bool DeveloperMode = true;

        // If true, when allied to a faction mech parts will be rolled up as well
        public bool RollupMechsAtAllied = true;

        // The values used to define the base amounts for rollup
        public float[] RollupMRBValue = new float[] { 40000f, 60000f, 90000f, 130000f, 180000f, 240000f };

        // How much the rollup values houdl be multiplied based upon your faction rating
        public float[] RollupFactionMulti = new float[] { 0f, 0f, 0f, 1f, 5f, 11f, 18f };

        // If RollupMechsAtAllied = true, how much should be multiply the threshold by for mech parts
        public float RollupAlliedMultiForMechs = 10f;

        // If true, the employer will roll on each and every mech part
        public bool HoldbackAlwaysForMechs = false;

        // The holdback percentage for any given item
        public float[] HoldbackFactionValue = new float[] { 60f, 40f, 20f, 10f, 5f, 2.5f, 0f };

        // How much your MRB reduces the holdback percentage
        public float[] HoldbackMRBMulti = new float[] { 1f, 0.875f, 0.75f, 0.625f, 0.5f, 0.375f };

        // The number of holdback picks an employer gets
        public int[] HoldbackPicks = new int[] { 4, 3, 2, 1, 1, 1, 0 };

        // TODO: Print multiplier values
        public override string ToString() {
            string rollupMRBVal = string.Join(", ", RollupMRBValue.Select(v => v.ToString("0.00")).ToArray());
            string rollupFactVal = string.Join(", ", RollupFactionMulti.Select(v => v.ToString("0.00")).ToArray());
            string holdbackFactVal = string.Join(", ", HoldbackFactionValue.Select(v => v.ToString("0.00")).ToArray());
            string holdbackMRBVal = string.Join(", ", HoldbackMRBMulti.Select(v => v.ToString("0.00")).ToArray());
            string holdbackPickVal = string.Join(", ", HoldbackPicks.Select(v => v.ToString()).ToArray());

            return $"Debug:{Debug} DeveloperMode:{DeveloperMode}\n " +
                $"Rollup MechsAtAllied:{RollupMechsAtAllied} MRBValues:[{rollupMRBVal}] x FactMulti:[{rollupFactVal}]\n " +
                $"Holdback alwaysHoldback:{HoldbackAlwaysForMechs} FactPercent:[{holdbackFactVal}] x MRBMulti:[{holdbackMRBVal}] Picks:[{holdbackPickVal}]";
        }
    }
}
