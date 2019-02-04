
namespace LootMagnet {
    public class ModConfig {
        public bool Debug = false;
        public bool LoreMode = false;

        public override string ToString() {
            return $"Debug:{Debug} LoreMode:{LoreMode}";
        }
    }
}
