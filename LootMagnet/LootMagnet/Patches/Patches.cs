using BattleTech;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LootMagnet.Patches {


    public static class SalvageHelper {

        public static Dictionary<string, SalvageHolder> SalvageState = new Dictionary<string, SalvageHolder>();

        public static SimGameReputation EmployerReputation = SimGameReputation.INDIFFERENT;
        public static int MSRBLevel = 0;

        // This always returns a quantity of 1!
        public static SalvageDef CloneToXName(SalvageDef salvageDef) {
            
            string uiNameWithQuantity = $"{salvageDef.Description.UIName} ({salvageDef.Count}ct.)";
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

    [HarmonyPatch]
    public static class Contract_GenerateSalvage {

        // Private method can't be patched by annotations, so use MethodInfo
        public static MethodInfo TargetMethod() {
            return AccessTools.Method(typeof(Contract), "GenerateSalvage");
        }

        public static void Postfix(Contract __instance, List<UnitResult> enemyMechs, List<VehicleDef> enemyVehicles, List<UnitResult> lostUnits, bool logResults,
            List<SalvageDef> ___finalPotentialSalvage) {

            LootMagnet.Logger.Log("Checking salvage results for contract.");

        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract {
        
        public static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort) {
            if (__instance != null) {
                SimGameState simulation = HBS.LazySingletonBehavior<UnityGameInstance>.Instance.Game.Simulation;

                SimGameReputation employerRep = simulation.GetReputation(__instance.GetTeamFaction("ecc8d4f2-74b4-465d-adf6-84445e5dfc230"));
                SalvageHelper.EmployerReputation = employerRep;
                SalvageHelper.MSRBLevel = simulation.GetCurrentMRBLevel();
            }            
        }
    }

    [HarmonyPatch(typeof(Contract), "GetPotentialSalvage")]
    public static class Contract_GetPotentialSalvage {

        // At this point, salvage has been collapsed and grouped. For each of those that have count > 1, change their name, add them to the Dict, and set count to 1.
        public static void Postfix(Contract __instance, List<SalvageDef> __result) {
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

    [HarmonyPatch(typeof(Contract), "AddToFinalSalvage")]
    public static class Contract_AddToFinalSalvage {
        
        public static void Prefix(Contract __instance, ref SalvageDef def) {
            if (def != null) {
                if (SalvageHelper.SalvageState.ContainsKey(def.RewardID)) {
                    SalvageHolder sHolder = SalvageHelper.SalvageState[def.RewardID];
                    LootMagnet.Logger.Log($"C:ATFS - updating {def.RewardID} / {def?.Description?.Name} to count {sHolder.available}");
                    def.Count = sHolder.available;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
    public static class Contract_FinalizeSalvage {

        public static void Postfix(Contract __instance) {
            LootMagnet.Logger.Log("C:FS entered.");
            SalvageHelper.SalvageState.Clear();
            SalvageHelper.EmployerReputation = SimGameReputation.INDIFFERENT;
            SalvageHelper.MSRBLevel = 0;
        }
    }

}
