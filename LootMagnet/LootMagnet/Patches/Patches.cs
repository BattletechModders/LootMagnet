using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using IRBTModUtils;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LootMagnet
{

    [HarmonyPatch(typeof(Contract), "GenerateSalvage")]
    public static class Contract_GenerateSalvage
    {
        public static void Prefix(ref bool __runOriginal, Contract __instance)
        {
            if (!__runOriginal) return;

            Mod.Log.Info?.Write($"== Resolving salvage for contract:'{__instance.Name}' / '{__instance.GUID}' with result:{__instance.TheMissionResult}");
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract
    {

        public static void Prefix(ref bool __runOriginal, Contract __instance, MissionResult result, bool isGoodFaithEffort)
        {
            if (!__runOriginal) return;

            if (__instance != null && !__instance.ContractTypeValue.IsSkirmish)
            {
                SimGameState simulation = HBS.LazySingletonBehavior<UnityGameInstance>.Instance.Game.Simulation;

                ModState.Employer = __instance.GetTeamFaction("ecc8d4f2-74b4-465d-adf6-84445e5dfc230");
                SimGameReputation employerRep = simulation.GetReputation(ModState.Employer);
                ModState.EmployerRep = employerRep;
                ModState.EmployerRepRaw = simulation.GetRawReputation(ModState.Employer);

                ModState.IsEmployerAlly = simulation.IsFactionAlly(ModState.Employer);
                ModState.MRBRating = simulation.GetCurrentMRBLevel(); // Normalize to 0 indexing
                Mod.Log.Info?.Write($"At contract start for employer: Employer:({ModState.Employer}):  " +
                    $"employerRep:{ModState.EmployerRep}  employerIsAllied:{ModState.IsEmployerAlly}  " +
                    $"MRBRating: {ModState.MRBRating}  MRBIndex: {Helper.MRBCfgIdx()}");

                // Calculate the rollup, reputation and etc:
                Mod.Log.Info?.Write($" -- Contract Rollup Idx: {Helper.MRBCfgIdx()} => " +
                    $"RawRollup: {Mod.Config.RollupMRBValue[Helper.MRBCfgIdx()]}");
                RepCfg repCfg = Mod.Config.Reputation[Helper.FactionCfgIdx()];
                Mod.Log.Info?.Write($" -- Faction Rep Idx: {Helper.FactionCfgIdx()} => " +
                    $"Reputation: {repCfg.Reputation}  " +
                    $"RollupMultiComponent: {repCfg.RollupMultiComponent}  RollupMultiMech: {repCfg.RollupMultiMech}  " +
                    $"HoldbackTrigger: {repCfg.HoldbackTrigger}  HoldbackValueCapMulti: {repCfg.HoldbackValueCapMulti}");
            }
        }
    }

    [HarmonyPatch(typeof(ListElementController_SalvageMechPart_NotListView), "RefreshInfoOnWidget")]
    [HarmonyPatch(new Type[] { typeof(InventoryItemElement_NotListView) })]
    public static class ListElementController_SalvageMechPart_RefreshInfoOnWidget
    {
        public static void Postfix(ListElementController_SalvageMechPart_NotListView __instance, InventoryItemElement_NotListView theWidget)
        {
            Mod.Log.Debug?.Write($"LEC_SMP_NLV:RIOW - entered");
            if (__instance.salvageDef != null && __instance.salvageDef.RewardID != null && __instance.salvageDef.RewardID.Contains("_qty"))
            {
                int qtyIdx = __instance.salvageDef.RewardID.IndexOf("_qty");
                string countS = __instance.salvageDef.RewardID.Substring(qtyIdx + 4);
                int count = int.Parse(countS);
                Mod.Log.Debug?.Write($"LEC_SMP_NLV:RIOW - found quantity {count}, changing mechdef");

                DescriptionDef currentDesc = __instance.mechDef.Chassis.Description;
                string displayName = !String.IsNullOrEmpty(currentDesc.UIName) ? currentDesc.UIName : currentDesc.Name;
                string newUIName = $"{displayName} <lowercase>[QTY:{count}]</lowercase>";

                Text newPartName = new Text(newUIName, new object[] { });
                theWidget.mechPartName.SetText(newPartName);
            }
        }
    }

    [HarmonyPatch(typeof(ListElementController_SalvageMechPart_NotListView), "IsDuplicateContent")]
    [HarmonyPatch(new Type[] { typeof(ListElementController_BASE_NotListView) })]
    public static class ListElementController_SalvageMechPart_IsDuplicateContent
    {
        public static void Prefix(ref bool __runOriginal, ref bool __result, ListElementController_SalvageMechPart_NotListView __instance, ListElementController_BASE_NotListView otherController)
        {
            try
            {
                if (otherController.ItemWidget == null) { __runOriginal = false; __result = false; return; }
                if (__instance.ItemWidget == null) { __runOriginal = false; __result = false; return; }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(ListElementController_SalvageGear_NotListView), "IsDuplicateContent")]
    [HarmonyPatch(new Type[] { typeof(ListElementController_BASE_NotListView) })]
    public static class ListElementController_SalvageGear_NotListView_IsDuplicateContent
    {
        public static void Prefix(ref bool __runOriginal, ref bool __result, ListElementController_SalvageGear_NotListView __instance, ListElementController_BASE_NotListView otherController)
        {
            try
            {
                if (otherController.ItemWidget == null) { __runOriginal = false; __result = false; return; }
                if (__instance.ItemWidget == null) { __runOriginal = false; __result = false; return; }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(ListElementController_SalvageWeapon_NotListView), "IsDuplicateContent")]
    [HarmonyPatch(new Type[] { typeof(ListElementController_BASE_NotListView) })]
    public static class ListElementController_SalvageWeapon_NotListView_IsDuplicateContent
    {
        public static void Prefix(ref bool __runOriginal, ref bool __result, ListElementController_SalvageWeapon_NotListView __instance, ListElementController_BASE_NotListView otherController)
        {
            try
            {
                if (otherController.ItemWidget == null) { __runOriginal = false; __result = false; return; }
                if (__instance.ItemWidget == null) { __runOriginal = false; __result = false; return; }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(ListElementController_SalvageFullMech_NotListView), "IsDuplicateContent")]
    [HarmonyPatch(new Type[] { typeof(ListElementController_BASE_NotListView) })]
    public static class ListElementController_SalvageFullMech_NotListView_IsDuplicateContent
    {
        public static void Prefix(ref bool __runOriginal, ref bool __result, ListElementController_SalvageFullMech_NotListView __instance, ListElementController_BASE_NotListView otherController)
        {
            try
            {
                if (otherController.ItemWidget == null) { __runOriginal = false; __result = false; return; }
                if (__instance.ItemWidget == null) { __runOriginal = false; __result = false; return; }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(Contract), "AddToFinalSalvage")]
    [HarmonyAfter("io.github.denadan.CustomComponents")]
    public static class Contract_AddToFinalSalvage
    {

        public static int GetRealSalvageCount(this SalvageDef def)
        {
            if (string.IsNullOrEmpty(def.RewardID)) { return def.Count <= 0 ? 1 : def.Count; }
            if (def.RewardID.Contains("_qty") == false) { return def.Count <= 0 ? 1 : def.Count; }
            int qtyIdx = def.RewardID.IndexOf("_qty");
            string countS = def.RewardID.Substring(qtyIdx + "_qty".Length);
            int count = int.Parse(countS);
            return count;
        }
        public static int GetPreSalvageCount(this SalvageDef def)
        {
            if (string.IsNullOrEmpty(def.RewardID)) { return 1; }
            if (def.RewardID.Contains("_qty") == false) { return 1; }
            int qtyIdx = def.RewardID.IndexOf("_qty");
            string countS = def.RewardID.Substring(qtyIdx + "_qty".Length);
            int count = int.Parse(countS);
            return count;
        }
        public static float GetDefSellCost(this SalvageDef def)
        {
            if (UnityGameInstance.BattleTechGame.Simulation == null) { return 0f; }
            switch (def.Type)
            {
                case SalvageDef.SalvageType.COMPONENT: return def.MechComponentDef.Description.Cost;
                case SalvageDef.SalvageType.MECH_PART:
                    {
                        if (UnityGameInstance.BattleTechGame.DataManager.MechDefs.TryGet(def.Description.Id, out var mechDef))
                        {
                            return mechDef.SimGameMechPartCost;
                        }
                        return 0f;
                    };
                case SalvageDef.SalvageType.CHASSIS:
                    {
                        if (UnityGameInstance.BattleTechGame.DataManager.ChassisDefs.TryGet(def.Description.Id, out var chassisDef))
                        {
                            return chassisDef.Description.Cost;
                        }
                        else if (UnityGameInstance.BattleTechGame.DataManager.MechDefs.TryGet(def.Description.Id, out var mechDef))
                        {
                            return mechDef.SimGameMechPartCost * UnityGameInstance.BattleTechGame.Simulation.Constants.Story.DefaultMechPartMax; ;
                        }
                        return 0f;
                    };
            }
            return 0f;
        }
        public static void Prefix(ref bool __runOriginal, Contract __instance, ref SalvageDef def)
        {
            if (!__runOriginal) return;

            Mod.Log.Debug?.Write($"C:ATFS - entered.");
            if (def?.RewardID != null)
            {
                if (def.RewardID.Contains("_qty"))
                {
                    Mod.Log.Debug?.Write($"  Salvage ({def.Description.Name}) has rewardID:({def.RewardID}) with multiple quantities");
                    int qtyIdx = def.RewardID.IndexOf("_qty");
                    string countS = def.RewardID.Substring(qtyIdx + "_qty".Length);
                    Mod.Log.Debug?.Write($"  Salvage ({def.Description.Name}) with rewardID:({def.RewardID}) will be given count: {countS}");
                    int count = int.Parse(countS);
                    def.Count = count;
                }
                else
                {
                    Mod.Log.Debug?.Write($"  Salvage ({def.Description.Name}) has rewardID:({def.RewardID})");
                    List<string> compPartIds = ModState.CompensationParts.Select(sd => sd.RewardID).ToList();
                    if (compPartIds.Contains(def.RewardID))
                    {
                        Mod.Log.Debug?.Write($" Found item in compensation that was randomly assigned.");
                    }
                }
            }
            else
            {
                Mod.Log.Debug?.Write($"  RewardId was null for def:({def?.Description?.Name})");
            }
        }
    }

    [HarmonyPatch(typeof(AAR_SalvageScreen), "CalculateAndAddAvailableSalvage")]
    public static class AAR_SalvageScreen_CalculateAndAddAvailableSalvage
    {

        public static void Prefix(ref bool __runOriginal, AAR_SalvageScreen __instance)
        {
            if (!__runOriginal) return;

            Mod.Log.Debug?.Write("AAR_SS:CAAAS entered.");

            // Calculate potential salvage, which will be rolled up at this point (including mechs!)
            ModState.PotentialSalvage = __instance.contract.GetPotentialSalvage();
            ModState.HeldbackParts.Clear();
            ModState.CompensationParts.Clear();

            // Sort by price, since other functions depend on it
            ModState.PotentialSalvage.Sort(new Helper.SalvageDefByCostDescendingComparer());

            // Check for holdback
            bool hasMechParts = ModState.PotentialSalvage.FirstOrDefault(sd => sd.Type == SalvageDef.SalvageType.MECH_PART) != null;
            bool canHoldback = Mod.Config.Holdback.Enabled && (Thread.CurrentThread.isFlagSet("LootMagnet_supress_dialog") == false) && (ModState.Employer != null) && ModState.Employer.DoesGainReputation;
            float triggerChance = Helper.GetHoldbackTriggerChance();
            float holdbackRoll = Mod.Random.Next(101);
            Mod.Log.Info?.Write($"Holdback roll:{holdbackRoll}% triggerChance:{triggerChance}% hasMechParts:{hasMechParts} canHoldback:{canHoldback}");

            if (canHoldback && hasMechParts && holdbackRoll <= triggerChance)
            {
                Mod.Log.Info?.Write($"Holdback triggered, determining disputed mech parts.");
                Helper.CalculateHoldback(ModState.PotentialSalvage);
                Helper.CalculateCompensation(ModState.PotentialSalvage);
            }

            __instance.totalSalvageMadeAvailable = ModState.PotentialSalvage.Count - ModState.HeldbackParts.Count;
            Mod.Log.Info?.Write($"Setting totalSalvageMadeAvailable = potentialSalvage: {ModState.PotentialSalvage.Count} - heldbackParts: {ModState.HeldbackParts.Count}");

            if (ModState.HeldbackParts.Count > 0)
            {
                UIHelper.ShowHoldbackDialog();
            }
            else
            {
                // Roll up any remaining salvage and widget-ize it
                List<SalvageDef> rolledUpSalvage = Helper.RollupSalvage(ModState.PotentialSalvage);
                Helper.CalculateAndAddAvailableSalvage(__instance, rolledUpSalvage);
            }

            __runOriginal = false;
        }
        public static void Postfix(AAR_SalvageScreen __instance)
        {

        }
    }

    // executes after accepting salvage, we unlock the received item widgets
    [HarmonyPatch(typeof(AAR_SalvageChosen), "ConvertToFinalState")]
    public class AAR_SalvageChosen_ConvertToFinalState
    {

        public static void Postfix(AAR_SalvageChosen __instance)
        {
            // Skip if the UI element isn't visible
            if (!__instance.Visible)
            {
                Mod.Log.Info?.Write("SalvageChosen not visible, but ConvertToFinalState called - this should not happen, skipping.");
                return;
            }

            // Set each of the items clickable
            foreach (InventoryItemElement_NotListView iie in __instance.LeftoverInventory)
            {
                if (iie.controller != null && iie.controller.salvageDef != null && iie.controller.salvageDef.Type != SalvageDef.SalvageType.MECH_PART)
                {
                    Mod.Log.Debug?.Write($"Enabling non-mechpart for clicking: {iie.controller.salvageDef.Description.Id}");
                    iie.SetClickable(true);
                }
            }
            if (Mod.Config.UseImprovedSellUI == false)
            {
                // Update text with Quick Sell instructions
                string localText = new Text(Mod.Config.LocalizedText[ModConfig.LT_QUICK_SELL], new object[] { }).ToString();
                __instance.howManyReceivedText.SetText(string.Concat(__instance.howManyReceivedText.text, localText));
            }
            // Painful, full-context searches here
            ModState.HBSPopupRoot = GameObject.Find(ModConsts.HBSPopupRootGOName);
            ModState.FloatieFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name == "UnitedSansReg-Black SDF");
            ModState.SGCurrencyDisplay = (SGCurrencyDisplay)Object.FindObjectOfType(typeof(SGCurrencyDisplay));
        }
    }

    // Reset state once we're leaving the salvage screen
    [HarmonyPatch(typeof(AAR_SalvageScreen), "OnCompleted")]
    public class AAR_SalvageScreen_OnCompleted
    {
        public static void Prefix(ref bool __runOriginal)
        {
            Mod.Log.Debug?.Write("Resetting QuickSell state.");
            ModState.Contract = null;
            ModState.SimGameState = null;
            ModState.AAR_SalvageScreen = null;
            ModState.SGCurrencyDisplay = null;
            ModState.HBSPopupRoot = null;
            ModState.FloatieFont = null;
            ModState.Reset();
        }
    }

    [HarmonyPatch(typeof(InventoryItemElement_NotListView), "OnButtonClicked")]
    public class InventoryItemElement_NotListView_OnButtonClicked
    {

        public static void Postfix(InventoryItemElement_NotListView __instance)
        {

            Mod.Log.Info?.Write($"InventoryItemElement_NotListView.OnButtonClicked UseImprovedSellUI:{Mod.Config.UseImprovedSellUI}");
            if (Mod.Config.UseImprovedSellUI) { return; }
            // have to be holding shift
            if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                return;

            // Nothing to do, return
            if (__instance.controller == null || __instance.controller.salvageDef == null)
                return;

            // skip processing any non-parts or when the UI isn't up
            if (__instance.controller.salvageDef.Type != SalvageDef.SalvageType.COMPONENT)
                return;

            if (ModState.SimGameState == null || ModState.AAR_SalvageScreen == null)
            {
                Mod.Log.Warn?.Write("Expected state variables were null when performing quick sell - skipping!");
                return;
            }

            // Ensure we can access the necessary UI elements before adding money
            if (__instance.DropParent is AAR_SalvageChosen salvageChosen)
            {
                Mod.Log.Info?.Write("Checking contract salvage against controller item");
                List<SalvageDef> salvageResults = ModState.Contract.SalvageResults;
                SalvageDef matchingItem = salvageResults.FirstOrDefault(x => x == __instance.controller.salvageDef);
                if (matchingItem != null)
                {
                    // We have a matching salvageDef. Calculate the cost, and remove it from the list.
                    var cost = __instance.controller.salvageDef.Description.Cost;
                    var sellCost = Mathf.FloorToInt(cost * ModState.SimGameState.Constants.Finances.ShopSellModifier);

                    Mod.Log.Info?.Write($"Selling {matchingItem?.Description.Name} worth {matchingItem?.Description.Cost}" +
                                  $" x {ModState.SimGameState.Constants.Finances.ShopSellModifier} shopSellModifier = {matchingItem?.Description.Cost * ModState.SimGameState.Constants.Finances.ShopSellModifier}");

                    ModState.SimGameState.AddFunds(sellCost, "LootMagnet", false, true);
                    ModState.SGCurrencyDisplay.UpdateMoney();

                    // Create the new floatie text for the sell amount
                    var floatie = new GameObject(ModConsts.LootMagnetFloatieGOName);
                    floatie.transform.SetParent(ModState.HBSPopupRoot.transform);
                    floatie.transform.position = __instance.gameObject.transform.position;

                    var text = floatie.AddComponent<TextMeshProUGUI>();
                    text.font = ModState.FloatieFont;
                    text.SetText($"¢{sellCost:N0}");

                    floatie.AddComponent<FloatieBehaviour>();
                    floatie.AddComponent<FadeText>();

                    // Finally, remove it from salvage - which has already been fixed
                    ModState.Contract.SalvageResults.Remove(__instance.controller.salvageDef);

                    // Remove it from the inventory widgets
                    salvageChosen.LeftoverInventory.Remove(__instance);
                    if (__instance.DropParent != null)
                    {
                        __instance.RemoveFromParent();
                    }
                    __instance.gameObject.SetActive(false);

                }
                Mod.Log.Info?.Write("All items sold");

            }
        }
    }
}
