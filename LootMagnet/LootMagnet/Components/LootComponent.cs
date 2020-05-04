#if NO_CC
#else
using CustomComponents;
#endif

namespace LootMagnet.Components {

#if NO_CC
#else
    [CustomComponent("LootMagnetComp")]
    public class LootMagnetComp : SimpleCustomComponent {
        public bool Blacklisted = false;
    }
#endif
}

