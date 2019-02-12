
namespace LootMagnet {
    public class ModConfig {

        // If true, many logs will be printed
        public bool Debug = false;
        
        // The values used to define the base amounts for rollup
        public float[] RollupMRBValue = new float[] { 15000f, 20000f, 25000f, 30000f, 35000f, 40000f };

        // How much the rollup values houdl be multiplied based upon your faction rating
        public float[] RollupFactionMulti = new float[] { 0f, 0f, 0f, 1f, 6f, 12f, 20f };

        // The holdback percentage for any given item
        public float[] HoldbackFactionValue = new float[] { 60f, 40f, 20f, 10f, 5f, 2.5f, 0f };

        // How much your MRB reduces the holdback percentage
        public float[] HoldbackMRBMulti = new float[] { 1f, 0.875f, 0.75f, 0.625f, 0.5f, 0.375f };

        // The number of holdback picks an employer gets
        public int[] HoldbackPicks = new int[] { 4, 3, 2, 1, 1, 1, 0 };

        // TODO: Print multiplier values
        public override string ToString() {
            return $"Debug:{Debug} ";
        }
    }
}
