using BattleTech;
using BattleTech.UI;
using Harmony;
using Localize;
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

        public static void Prefix(Contract __instance) {
            LootMagnet.Logger.Log($"== Resolving salvage for contract:'{__instance.Name}' / '{__instance.GUID}' with result:{__instance.TheMissionResult}");
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract {
        
        public static void Prefix(Contract __instance, MissionResult result, bool isGoodFaithEffort) {
            if (__instance != null && !__instance.IsArenaSkirmish) {
                SimGameState simulation = HBS.LazySingletonBehavior<UnityGameInstance>.Instance.Game.Simulation;

                State.Employer = __instance.GetTeamFaction("ecc8d4f2-74b4-465d-adf6-84445e5dfc230");
                SimGameReputation employerRep = simulation.GetReputation(State.Employer);
                State.EmployerRep = employerRep;
                State.EmployerRepRaw = simulation.GetRawReputation(State.Employer);
                State.IsEmployerAlly = simulation.IsCareerFactionAlly(State.Employer);
                State.MRBRating = simulation.GetCurrentMRBLevel() - 1; // Normalize to 0 indexing
                LootMagnet.Logger.Log($"At contract start, Player has MRB:{State.MRBRating} / EmployerRep:{State.EmployerRep} / EmployerAllied:{State.IsEmployerAlly}");
            }            
        }
    }

    [HarmonyPatch(typeof(Contract), "GetPotentialSalvage")]
    public static class Contract_GetPotentialSalvage {

        // At this point, salvage has been collapsed and grouped. For each of those that have count > 1, change their name, add them to the Dict, and set count to 1.
        public static void Postfix(Contract __instance, List<SalvageDef> __result, List<SalvageDef> ___finalPotentialSalvage) {
            if (__result != null) {

                // Roll up the salvage
                float salvageThreshold = Helper.GetSalvageThreshold(false);
                List<SalvageDef> rolledUpSalvage = Helper.RollupSalvage(__result);

                __result.Clear();
                __result.AddRange(rolledUpSalvage);

                ___finalPotentialSalvage.Clear();
                ___finalPotentialSalvage.AddRange(rolledUpSalvage);
            }
        }
    } 

    [HarmonyPatch(typeof(ListElementController_SalvageMechPart_NotListView), "RefreshInfoOnWidget")]
    [HarmonyPatch(new Type[] { typeof(InventoryItemElement_NotListView) })]
    public static class ListElementController_SalvageMechPart_RefreshInfoOnWidget {
        public static void Postfix(ListElementController_SalvageMechPart_NotListView __instance, InventoryItemElement_NotListView theWidget, MechDef ___mechDef, SalvageDef ___salvageDef) {
            LootMagnet.Logger.LogIfDebug($"LEC_SMP_NLV:RIOW - entered");
            if (___salvageDef.RewardID != null && ___salvageDef.RewardID.Contains("_qty")) {
                int qtyIdx = ___salvageDef.RewardID.IndexOf("_qty");
                string countS = ___salvageDef.RewardID.Substring(qtyIdx + 4);
                int count = int.Parse(countS);
                LootMagnet.Logger.LogIfDebug($"LEC_SMP_NLV:RIOW - found quantity {count}, changing mechdef");

                DescriptionDef currentDesc = ___mechDef.Chassis.Description;
                string newUIName = $"{currentDesc.UIName} <lowercase>[QTY:{count}]</lowercase>";

                Text newPartName = new Text(newUIName, new object[] { });
                theWidget.mechPartName.SetText(newPartName);
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "AddToFinalSalvage")]
    [HarmonyAfter("io.github.denadan.CustomComponents")]
    public static class Contract_AddToFinalSalvage {
        
        public static void Prefix(Contract __instance, ref SalvageDef def) {
            if (def != null) {
                if (def.RewardID != null && def.RewardID.Contains("_qty")) {
                    int qtyIdx = def.RewardID.IndexOf("_qty");
                    string countS = def.RewardID.Substring(qtyIdx + 4);
                    LootMagnet.Logger.LogIfDebug($"Salvage {def.Description.Name} with rewardID:{def.RewardID} will be given count: {countS}");
                    int count = int.Parse(countS);
                    def.Count = count;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "FinalizeSalvage")]
    public static class Contract_FinalizeSalvage {

        public static void Postfix(Contract __instance) {
            LootMagnet.Logger.LogIfDebug("C:FS entered.");
        }
    }


    [HarmonyPatch(typeof(AAR_SalvageScreen), "SalvageConfirmed")]
    public static class AAR_SalvageScreen_SalvageConfirmed {

        public static void Postfix(AAR_SalvageScreen __instance, Contract ___contract) {
            LootMagnet.Logger.LogIfDebug("AAR_SS:SC entered.");

            // Check for holdback
            float triggerChance = Helper.GetHoldbackTriggerChance();
            float holdbackRoll = LootMagnet.Random.Next(101);
            if (holdbackRoll <= triggerChance) {
                LootMagnet.Logger.Log($"Holdback triggered from roll:{holdbackRoll} <= triggerChance:{triggerChance}. Removing salvage.");
                Helper.HoldbackSalvage(___contract, __instance);
            }
        }
    }

}
