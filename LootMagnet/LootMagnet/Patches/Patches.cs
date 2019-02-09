using BattleTech;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LootMagnet.Patches {

    [HarmonyPatch]
    public static class Contract_GenerateSalvage {

        // Private method can't be patched by annotations, so use MethodInfo
        public static MethodInfo TargetMethod() {
            return AccessTools.Method(typeof(Contract), "GenerateSalvage");
        }

        public static void Postfix (Contract __instance, List<UnitResult> enemyMechs, List<VehicleDef> enemyVehicles, List<UnitResult> lostUnits, bool logResults,
            List<SalvageDef> ___finalPotentialSalvage) {
            
            LootMagnet.Logger.Log("Checking salvage results for contract.");

            //Dictionary<string, SalvageDef> salvagedComponents = new Dictionary<string, SalvageDef>();

            //List<SalvageDef> allComponents = new List<SalvageDef>();
            //List<SalvageDef> allMechParts = new List<SalvageDef>();
            //List<SalvageDef> allChassis = new List<SalvageDef>();
            //foreach (SalvageDef sDef in ___finalPotentialSalvage) {
            //    LootMagnet.Logger.Log($"Found sDef - UIName:{sDef?.Description?.UIName} count:{sDef?.Count} type:{sDef?.Type} " +
            //        $"cost:{sDef?.Description?.Cost} salvageId:{sDef.Description.Id}");
            //    if (sDef.Type == SalvageDef.SalvageType.COMPONENT) {
            //        allComponents.Add(sDef);                    
            //        if (salvagedComponents.ContainsKey(sDef.Description.Id)) {
            //            SalvageDef comp = salvagedComponents[sDef.Description.Id];
            //            comp.Count++;
            //            //comp.Description.UIName = $"{comp.Description.UIName}+";
            //        } else {
            //            salvagedComponents[sDef.Description.Id] = sDef;
            //        }
            //    } else if (sDef.Type == SalvageDef.SalvageType.MECH_PART) {
            //        allMechParts.Add(sDef);
            //    } else {
            //        allChassis.Add(sDef);
            //    }
            //}

            //List<SalvageDef> newSalvage = salvagedComponents.Values.ToList();
            //___finalPotentialSalvage.Clear();
            //___finalPotentialSalvage.AddRange(newSalvage);
        }
    }

    public static class SalvageHelper {

        public static Dictionary<string, SalvageHolder> SalvageState = new Dictionary<string, SalvageHolder>();

        // This always returns a quantity of 1!
        public static SalvageDef CloneToXName(SalvageDef salvageDef) {
            
            string uiNameWithQuantity = $"{salvageDef.Description.UIName} - {salvageDef.Count}";
            DescriptionDef newDescDef = new DescriptionDef(
                salvageDef.Description.Id,
                salvageDef.Description.Name,
                salvageDef.Description.Details,
                salvageDef.Description.Icon,
                salvageDef.Description.Cost,
                salvageDef.Description.Rarity,
                salvageDef.Description.Purchasable,
                salvageDef.Description.Manufacturer,
                salvageDef.Description.Model,
                uiNameWithQuantity
            );

            SalvageDef newDef = new SalvageDef(salvageDef) {
                Description = newDescDef,
                Count = 1
            };

            return newDef;
        }
    }

    public class SalvageHolder {
        public SalvageDef salvageDef;
        public int available;
    }

    [HarmonyPatch(typeof(Contract), "GetPotentialSalvage")]
    public static class Contract_GetPotentialSalvage {

        // At this point, salvage has been collapsed and grouped. For each of those that have count > 1, change their name, add them to the Dict, and set count to 1.
        public static void Postfix(Contract __instance, List<SalvageDef> __result) {
            LootMagnet.Logger.Log("C:GPS entered.");
            if (__result != null) {

                List<SalvageDef> normdSalvage = new List<SalvageDef>();
                foreach (SalvageDef sDef in __result) {
                    if (sDef.Count > 1) {
                        LootMagnet.Logger.Log($"C:GPS - found {sDef.Count} of salvage:({sDef?.Description?.Name} / {sDef?.Description.Id} with rewardId:{sDef?.RewardID} / GUID:{sDef?.GUID}");
                        SalvageDef newSDef = SalvageHelper.CloneToXName(sDef);
                        SalvageHelper.SalvageState[sDef.RewardID] = new SalvageHolder {
                            salvageDef = newSDef,
                            available = sDef.Count
                        };
                        normdSalvage.Add(newSDef);
                    } else {
                        normdSalvage.Add(sDef);
                    }
                }

                __result.Clear();
                __result.AddRange(normdSalvage);
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
    public static class Contract_FinalizeSalvage {

        public static void Postfix(Contract __instance) {
            LootMagnet.Logger.Log("C:FS entered.");
        }
    }

}
