using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LootMagnet {

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

                Faction employerFaction = __instance.GetTeamFaction("ecc8d4f2-74b4-465d-adf6-84445e5dfc230");
                SimGameReputation employerRep = simulation.GetReputation(employerFaction);
                State.EmployerReputation = employerRep;
                State.IsEmployerAlly = simulation.IsCareerFactionAlly(employerFaction);
                State.MRBRating = simulation.GetCurrentMRBLevel();
            }            
        }
    }

    [HarmonyPatch(typeof(Contract), "GetPotentialSalvage")]
    public static class Contract_GetPotentialSalvage {

        // At this point, salvage has been collapsed and grouped. For each of those that have count > 1, change their name, add them to the Dict, and set count to 1.
        public static void Postfix(Contract __instance, List<SalvageDef> __result) {
            if (__result != null) {

                // Roll up the salvage
                float salvageThreshold = Helper.GetSalvageThreshold();
                List<SalvageDef> rolledUpSalvage = Helper.RollupSalvage(__result, salvageThreshold);

                // Check for holdback
                float holdbackChance = Helper.GetHoldbackChance();
                int holdbackPicks = Helper.GetHoldbackPicks();
                List<SalvageDef> postHoldbackSalvage = Helper.HoldbackSalvage(rolledUpSalvage, holdbackChance, holdbackPicks);

                __result.Clear();
                __result.AddRange(postHoldbackSalvage);
            }
        }
    }

    [HarmonyPatch(typeof(AAR_SalvageScreen), "AddNewSalvageEntryToWidget")]
    public static class AAR_SalvageScreen_AddNewSalvageEntryToWidget {

        // Handle any remainders here
        public static void Postfix(AAR_SalvageScreen __instance, SalvageDef salvageDef, IMechLabDropTarget targetWidget) {

        }        
    }    

    [HarmonyPatch(typeof(Contract), "AddToFinalSalvage")]
    public static class Contract_AddToFinalSalvage {
        
        public static void Prefix(Contract __instance, ref SalvageDef def) {
            if (def != null) {
                if (def.RewardID != null && def.RewardID.Contains("_qty")) {
                    int qtyIdx = def.RewardID.IndexOf("_qty");
                    string countS = def.RewardID.Substring(qtyIdx + 4);
                    LootMagnet.Logger.Log($"C:ATFS - def:{def.RewardID} will be given count: {countS}");
                    int count = int.Parse(countS);
                    def.Count = count;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
    public static class Contract_FinalizeSalvage {

        public static void Postfix(Contract __instance) {
            LootMagnet.Logger.Log("C:FS entered.");
            State.SalvageState.Clear();
            State.EmployerReputation = SimGameReputation.INDIFFERENT;
            State.IsEmployerAlly = false;
            State.MRBRating = 0;
        }
    }

}
