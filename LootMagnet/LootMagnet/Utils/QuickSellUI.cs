using BattleTech.UI;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BattleTech.Save.Core.ThreadedSaveManagerRequest;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using BattleTech.Data;
using BattleTech.UI.Tooltips;
using CustomComponents;
using System.Threading;
using static BattleTech.SimGameBattleSimulator;
using BattleTech.Save.SaveGameStructure;
using TMPro;
using HBS;
using BattleTech.UI.TMProWrapper;
using UIWidgetsSamples.Shops;
using IRBTModUtils;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Events;
using BattleTech.Network.Services.Types;
using static RootMotion.FinalIK.Grounding;
using System.Collections;

namespace LootMagnet.Utils
{
    public static class QuickSellHelper
    {
        private static MethodInfo get_RawInventoryPerfFix;
        private static MethodInfo get_GetRawInventoryCustomFilter;
        private static MethodInfo CustomShops_SellItem = null;
        private static FieldInfo get_SaleShops = null;
        public static void OnSellItems(string id, ComponentType componentType, int count, int cost)
        {
            try
            {
                if (get_SaleShops == null) { return; }
                Mod.Log.Info?.Write($"OnSellItems {id}:{componentType} count:{count} cost:{cost}");
                ShopDefItem shopDefItem = new ShopDefItem(
                    id, Shop.ComponentTypeToStopItemType(componentType), 1f, count, false, false, cost
                );
                UnityGameInstance.BattleTechGame.Simulation.MessageCenter.PublishMessage(new SimGamePurchaseMessage(shopDefItem, shopDefItem.SellCost, SimGamePurchaseMessage.TransactionType.Sell));
                IEnumerable saleShops = get_SaleShops.GetValue(null) as IEnumerable;
                foreach (var saleShop in saleShops)
                {
                    Mod.Log.Info?.Write($" {saleShop.GetType().FullName}");
                    if (saleShop.GetType().GetInterface("IShopDescriptor") == null) { continue; }
                    Mod.Log.Info?.Write($"  has IShopDescriptor");
                    if (Traverse.Create(saleShop).Property<bool>("CanUse").Value == false) { continue; }
                    Mod.Log.Info?.Write($"  CanUse");
                    if (Traverse.Create(saleShop).Method("OnSellItem", shopDefItem, count).GetValue<bool>() == false) { continue; }
                    Mod.Log.Info?.Write($"  OnSellItem");
                    break;
                    //if (saleShop is IShopDescriptor shopDescriptor && shopDescriptor.CanUse && saleShop.OnSellItem(shopDefItem, num1))
                    //break;
                }
            }catch(Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
        public static void InitCustomShopInfrustructure()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("CustomShops, Version="))
                {
                    Type CustomShops_Control = assembly.GetType("CustomShops.Control");
                    if(CustomShops_Control != null)
                    {
                        Mod.Log.Info?.Write($"{CustomShops_Control.FullName} found");
                        QuickSellHelper.get_SaleShops = AccessTools.Field(CustomShops_Control, "SaleShops");
                        if (get_SaleShops != null)
                        {
                            Mod.Log.Info?.Write($"  {get_SaleShops.Name} found");
                        }
                    }
                    else
                    {

                    }
                    Type CustomShops_UIControler = assembly.GetType("CustomShops.UIControler");
                    if(CustomShops_UIControler != null)
                    {
                        Mod.Log.Info?.Write($"{CustomShops_UIControler.FullName} found");
                        QuickSellHelper.CustomShops_SellItem = AccessTools.Method(CustomShops_UIControler, "SellItem");
                        if (CustomShops_SellItem != null)
                        {
                            Mod.Log.Info?.Write($"  {CustomShops_SellItem.Name} found");
                        }
                    }
                }
            }
        }
        public static void InitMechLabInventoryAccess()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith("BattletechPerformanceFix, Version="))
                {
                    Type MechLabFixPublic = assembly.GetType("BattletechPerformanceFix.MechlabFix.MechLabFixPublic");
                    if (MechLabFixPublic != null)
                    {
                        Mod.Log.Info?.Write($"{MechLabFixPublic.FullName} found");
                        PropertyInfo prop = AccessTools.Property(MechLabFixPublic, "RawInventory");
                        if (prop != null)
                        {
                            Mod.Log.Info?.Write($" {prop.Name} found");
                            get_RawInventoryPerfFix = prop.GetMethod;
                        }
                        else
                        {
                            Mod.Log.Info?.Write($" BattletechPerformanceFix.MechlabFix.MechLabFixPublic.RawInventory not found");
                        }
                    }
                    else
                    {
                        Mod.Log.Info?.Write($"BattletechPerformanceFix.MechlabFix.MechLabFixPublic not found");
                    }
                }
                if (assembly.FullName.StartsWith("CustomFilters, Version="))
                {
                    Type MechLabFixPublic = assembly.GetType("CustomFilters.MechLabScrolling.MechLabFixPublic");
                    if (MechLabFixPublic != null)
                    {
                        Mod.Log.Info?.Write($"{MechLabFixPublic.FullName} found");
                        get_GetRawInventoryCustomFilter = AccessTools.Method(MechLabFixPublic, "GetRawInventory");
                        if (get_GetRawInventoryCustomFilter != null)
                        {
                            Mod.Log.Info?.Write($" {get_GetRawInventoryCustomFilter.Name} found");
                        }
                        else
                        {
                            Mod.Log.Info?.Write($" CustomFilters.MechLabScrolling.MechLabFixPublic.GetRawInventory not found");
                        }
                    }
                    else
                    {
                        Mod.Log.Info?.Write($"CustomFilters.MechLabScrolling.MechLabFixPublic not found");
                    }
                }
            }
        }
        public static List<ListElementController_BASE_NotListView> BTPerfFixRawInventory(MechLabPanel panel)
        {
            List<ListElementController_BASE_NotListView> result = null;
            if (get_GetRawInventoryCustomFilter != null) { result = (List<ListElementController_BASE_NotListView>)get_GetRawInventoryCustomFilter.Invoke(null, new object[] { panel.inventoryWidget }); };
            if (result != null) { return result; }
            if (get_RawInventoryPerfFix != null) { result = (List<ListElementController_BASE_NotListView>)get_RawInventoryPerfFix.Invoke(null, new object[] { }); }
            if (result != null) { return result; }
            return null;
        }

        public static void SellItem(this ListElementController_BASE_NotListView item, int clickCount)
        {
            if (Mod.Config.UseImprovedSellUI == false) { return; }
            if (item == null) { return; }
            if (item.ItemWidget == null) { return; }
            if (UnityGameInstance.BattleTechGame.Simulation == null) { return; }
            RewardsPopup[] rewards = item.ItemWidget.GetComponentsInParent<RewardsPopup>(true);
            foreach(var reward in rewards)
            {
                if (reward.rarestSalvageDef == null) { continue; }
                reward.SellItem(item);
                return;
            }
            AAR_SalvageScreen[] salvageScreens = item.ItemWidget.GetComponentsInParent<AAR_SalvageScreen>(true);
            foreach (var salvageScreen in salvageScreens)
            {
                if (salvageScreen.AllSalvageControllers.Contains(item))
                {
                    salvageScreen.SellItem(item);
                    return;
                }
            }
            if (clickCount >= 2)
            {
                UnityGameInstance.BattleTechGame.Simulation?.RoomManager?.MechBayRoom?.mechBay?.mechLab?.SellItem(item, (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));
            }
        }
        public static void SellItem(this RewardsPopup reward, ListElementController_BASE_NotListView item)
        {
            Mod.Log.Info?.Write($"SellItem reward rare:{(reward.rarestSalvageDef == null?"null":reward.rarestSalvageDef.Description.Id)} cur:{(item.salvageDef == null?"null":item.salvageDef.Description.Id)}");
            if (reward.rarestSalvageDef == item.salvageDef)
            {
                GenericPopupBuilder.Create(GenericPopupType.Warning, "CAN'T SELL MAIN REWARD ITEM").AddFader().Render();
                return;
            }
            ShopDefItem matchingItem = null;
            foreach (var shopItem in reward.allShopDefItems)
            {
                Mod.Log.Info?.Write($" {shopItem.GUID}:{shopItem.Count}");
                if (shopItem.Count <= 0) { continue; }
                if (shopItem.GUID == item.salvageDef.Description.Id) {
                    matchingItem = shopItem;
                    break;
                }
            }
            if(matchingItem == null)
            {
                Mod.Log.Warn?.Write($"Can't find matching item for {item.salvageDef.Description.Id}");
                return;
            }
            var cost = item.salvageDef.Description.Cost;
            var sellCost = Mathf.FloorToInt(cost * UnityGameInstance.BattleTechGame.Simulation.Constants.Finances.ShopSellModifier);

            Mod.Log.Info?.Write($"Selling {matchingItem?.GUID} worth {item.salvageDef.Description.Cost}" +
                          $" x {UnityGameInstance.BattleTechGame.Simulation.Constants.Finances.ShopSellModifier} shopSellModifier = {item.salvageDef.Description.Cost * UnityGameInstance.BattleTechGame.Simulation.Constants.Finances.ShopSellModifier}");

            UnityGameInstance.BattleTechGame.Simulation.AddFunds(sellCost, "LootMagnet", false, true);
            UnityGameInstance.BattleTechGame.Simulation.RoomManager.CurrencyWidget.UpdateMoney();
            //UIManager.Instance.UIRoot.gameObject.GetComponentInChildren<>

            //ModState.SGCurrencyDisplay.UpdateMoney();

            // Create the new floatie text for the sell amount
            var floatie = new GameObject(ModConsts.LootMagnetFloatieGOName);
            floatie.transform.SetParent(SceneSingletonBehavior<UIManager>.Instance.popupNode.gameObject.transform);
            floatie.transform.position = item.ItemWidget.gameObject.transform.position;

            var text = floatie.AddComponent<TextMeshProUGUI>();
            if(ModState.FloatieFont == null)
            {
                ModState.FloatieFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name == "UnitedSansReg-Black SDF");
            }
            text.font = ModState.FloatieFont;
            text.SetText($"¢{sellCost:N0}");

            floatie.AddComponent<FloatieBehaviour>();
            floatie.AddComponent<FadeText>();
            reward.AllSalvageControllers.Remove(item);
            item.Pool();
            matchingItem.Count -= 1;
            OnSellItems(item.salvageDef.Description.Id, item.salvageDef.ComponentType, 1, sellCost);
        }
        public static void SellItem(this AAR_SalvageScreen salvageScreen, ListElementController_BASE_NotListView item)
        {
            Mod.Log.Info?.Write($"SellItem salvage:{item.salvageDef.Description.Id} cost:{item.salvageDef.GetDefSellCost()} count:{item.salvageDef.GetRealSalvageCount()}");
            List<SalvageDef> salvageResults = salvageScreen.contract.SalvageResults;
            SalvageDef matchingItem = salvageResults.FirstOrDefault(x => x == item.salvageDef);
            if (matchingItem == null)
            {
                Mod.Log.Warn?.Write($"Can't find matching item for {item.salvageDef.Description.Id}");
                return;
            }
            var count = item.salvageDef.GetRealSalvageCount();
            var cost = item.salvageDef.GetDefSellCost();
            var itemCost = Mathf.FloorToInt(cost * salvageScreen.Sim.Constants.Finances.ShopSellModifier);
            var sellCost = itemCost * count;
            Mod.Log.Info?.Write($"Selling {matchingItem?.GUID} worth {cost}" +
                          $" x {salvageScreen.Sim.Constants.Finances.ShopSellModifier} shopSellModifier x {count} = {sellCost}");

            salvageScreen.Sim.AddFunds(sellCost, "LootMagnet", false, true);
            salvageScreen.missionResultParent.currencyDisplay.UpdateMoney();
            //UIManager.Instance.UIRoot.gameObject.GetComponentInChildren<>

            //ModState.SGCurrencyDisplay.UpdateMoney();

            // Create the new floatie text for the sell amount
            var floatie = new GameObject(ModConsts.LootMagnetFloatieGOName);
            floatie.transform.SetParent(SceneSingletonBehavior<UIManager>.Instance.popupNode.gameObject.transform);
            floatie.transform.position = item.ItemWidget.gameObject.transform.position;

            var text = floatie.AddComponent<TextMeshProUGUI>();
            if (ModState.FloatieFont == null)
            {
                ModState.FloatieFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name == "UnitedSansReg-Black SDF");
            }
            text.font = ModState.FloatieFont;
            text.SetText($"¢{sellCost:N0}");

            floatie.AddComponent<FloatieBehaviour>();
            floatie.AddComponent<FadeText>();
            salvageScreen.contract.SalvageResults.Remove(item.salvageDef);

            // Remove it from the inventory widgets
            salvageScreen.salvageChosen.LeftoverInventory.Remove(item.ItemWidget);
            if (item.ItemWidget.DropParent != null)
            {
                item.ItemWidget.RemoveFromParent();
            }
            item.ItemWidget.gameObject.SetActive(false);
            OnSellItems(matchingItem.Description.Id, matchingItem.ComponentType, count, itemCost);
        }
        public static void SellItem(this MechLabPanel mechLabPanel, ListElementController_BASE_NotListView item, bool all)
        {
            if (mechLabPanel.Sim == null) { return; }
            Mod.Log.Info?.Write($"SellItem mechlab:{item.componentDef.Description.Id} cost:{item.componentDef.Description.Cost} count:{item.quantity}");
            ListElementController_BASE_NotListView matchingItem = null;
            var ownerListCustom = BTPerfFixRawInventory(mechLabPanel);
            if(ownerListCustom != null)
            {
                matchingItem = ownerListCustom.FirstOrDefault(x => x.componentDef.Description.Id == item.componentDef.Description.Id);
            }
            else
            {
                var matchingWidget = mechLabPanel.inventoryWidget.localInventory.FirstOrDefault(x=>x.controller.componentDef.Description.Id == item.componentDef.Description.Id);
                if (matchingWidget != null) { matchingItem = matchingWidget.controller; }
            }
            if (matchingItem == null)
            {
                Mod.Log.Warn?.Write($"Can't find matching item for {item.componentDef.Description.Id}");
                return;
            }
            int count = (all ? matchingItem.quantity : 1);
            var itemCost = Mathf.FloorToInt(item.componentDef.Description.Cost * mechLabPanel.Sim.Constants.Finances.ShopSellModifier);
            var sellCost = itemCost * count;

            Mod.Log.Info?.Write($"Selling {item.componentDef.Description.Id} worth {item.componentDef.Description.Cost}" +
                          $" x {mechLabPanel.Sim.Constants.Finances.ShopSellModifier} shopSellModifier x {count} = {sellCost}");

            mechLabPanel.Sim.AddFunds(sellCost, "LootMagnet", false, true);
            mechLabPanel.Sim.RoomManager.CurrencyWidget.UpdateMoney();
            //UIManager.Instance.UIRoot.gameObject.GetComponentInChildren<>

            //ModState.SGCurrencyDisplay.UpdateMoney();

            // Create the new floatie text for the sell amount
            var floatie = new GameObject(ModConsts.LootMagnetFloatieGOName);
            floatie.transform.SetParent(SceneSingletonBehavior<UIManager>.Instance.popupNode.gameObject.transform);
            floatie.transform.position = item.ItemWidget.gameObject.transform.position;

            var text = floatie.AddComponent<TextMeshProUGUI>();
            if (ModState.FloatieFont == null)
            {
                ModState.FloatieFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name == "UnitedSansReg-Black SDF");
            }
            text.font = ModState.FloatieFont;
            text.SetText($"¢{sellCost:N0}");

            floatie.AddComponent<FloatieBehaviour>();
            floatie.AddComponent<FadeText>();
            for (int t = 0; t < count; ++t)
            {
                matchingItem.quantity -= 1;
                mechLabPanel.Sim.RemoveItemStat(matchingItem.componentDef.Description.Id, SimGameState.GetTypeFromComponent(matchingItem.componentDef.ComponentType), false);
            }
            Mod.Log.Warn?.Write($" quantity:{matchingItem.quantity}");
            if (item.quantity <= 0)
            {
                if (matchingItem.ItemWidget.DropParent != null)
                {
                    matchingItem.ItemWidget.RemoveFromParent();
                }
                mechLabPanel.inventoryWidget.localInventory.Remove(matchingItem.ItemWidget);
                if (ownerListCustom != null)
                {
                    ownerListCustom.Remove(matchingItem);
                    matchingItem.Pool();
                }
            }
            else
            {
                matchingItem.RefreshInfo();
            }
            OnSellItems(matchingItem.componentDef.Description.Id, matchingItem.componentDef.ComponentType, count, itemCost);
        }
    }
    public class QuickSellUIItem : EventTrigger
    {
        public ListElementController_BASE_NotListView owner = null;
        public SVGAsset originalIcon = null;
        public SVGAsset hoverIcon = null;
        public GameObject priceElement = null;
        public SVGImage priceIcon = null;
        public LocalizableText priceText = null;
        public HBSTooltip priceTooltip = null;
        public RectTransform TYPE_ICON_TR = null;
        public RectTransform TOOLTIP_TR = null;
        public RectTransform TOOLTIP2_TR = null;
        public RectTransform main_tr = null;
        public void Init(ListElementController_BASE_NotListView owner)
        {
            this.owner = owner;
            //this.originalIcon = owner.ItemWidget.icon.vectorGraphics;
        }
        public void UpdateIcon()
        {
            if(owner != null)
            {
                if(owner.ItemWidget != null)
                {
                    this.originalIcon = owner.ItemWidget.icon.vectorGraphics;
                    TOOLTIP2_TR.anchoredPosition = new Vector2(0f, 0f);
                }
            }
        }
        public void Pool()
        {
            Mod.Log.Info?.Write($"QuickSellUIItem.Pool");
            this.owner = null;
            this.enabled = false;
            if (priceElement != null) { priceElement.SetActive(false); }
        }
        public void IconLoaded(string id, SVGAsset icon)
        {
           this.hoverIcon = icon;
            if (this.priceIcon != null) this.priceIcon.vectorGraphics = this.hoverIcon;
        }
        public void UpdatePrice(bool usePreCount)
        {
            try
            {
                if (this.priceElement == null) { return; }
                if (this.owner == null) { return; }
                if (UnityGameInstance.BattleTechGame.Simulation == null) { return; }
                if (this.priceText == null) { return; }
                int count = usePreCount ? owner.salvageDef.GetPreSalvageCount() : owner.salvageDef.GetRealSalvageCount();
                Mod.Log.Info?.Write($"UpdatePrice: {owner.salvageDef.Description.Id}:{owner.salvageDef.Type} id:{owner.salvageDef.RewardID} cost:{owner.salvageDef.GetDefSellCost()} count:{owner.salvageDef.Count} count:{count} sellMod:{UnityGameInstance.BattleTechGame.Simulation.Constants.Finances.ShopSellModifier}");
                var cost = owner.salvageDef.GetDefSellCost() * count;
                var sellCost = Mathf.FloorToInt(cost * UnityGameInstance.BattleTechGame.Simulation.Constants.Finances.ShopSellModifier);
                this.priceText.SetText("{0}", sellCost);
                this.priceElement.SetActive(sellCost > 0f);
            }catch(Exception e)
            {
                UIManager.logger.LogException(e);
                Mod.Log.Error?.Write(e.ToString());
            }
        }
        public void UpdateMechLabPrice()
        {
            try
            {
                if (this.priceElement == null) { return; }
                if (this.owner == null) { return; }
                if (UnityGameInstance.BattleTechGame.Simulation == null) { return; }
                if (this.priceText == null) { return; }
                int count = 1;
                Mod.Log.Info?.Write($"UpdatePrice: {owner.componentDef.Description.Id}:{owner.componentDef.ComponentType} cost:{owner.componentDef.Description.Cost} count:{owner.quantity} sellMod:{UnityGameInstance.BattleTechGame.Simulation.Constants.Finances.ShopSellModifier}");
                var cost = owner.componentDef.Description.Cost * count;
                var sellCost = Mathf.FloorToInt(cost * UnityGameInstance.BattleTechGame.Simulation.Constants.Finances.ShopSellModifier);
                this.priceText.SetText("{0}", sellCost);
                this.priceElement.SetActive(true);
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
                Mod.Log.Error?.Write(e.ToString());
            }
        }
        public static QuickSellUIItem Instantine(InventoryItemElement_NotListView parent)
        {
            var TOOLTIP = parent.SalvageTooltip.gameObject;
            var TYPE_ICON_TR = parent.icon.transform.parent.parent.gameObject.GetComponent<RectTransform>();
            var TOOLTIP2 = GameObject.Instantiate(TOOLTIP);
            TOOLTIP2.name = "TOOLTIP2";
            var TOOLTIP_TR = TOOLTIP.GetComponent<RectTransform>();
            var TOOLTIP2_TR = TOOLTIP2.GetComponent<RectTransform>();
            TOOLTIP2.transform.SetParent(TOOLTIP.transform.parent);
            var main_tr = parent.gameObject.GetComponent<RectTransform>();
            TOOLTIP2_TR.pivot = new Vector2(0f, 0.5f);
            Mod.Log.Info?.Write("QuickSellUIItem. InitAndCreate");
            TOOLTIP2_TR.sizeDelta = new Vector2(TYPE_ICON_TR.sizeDelta.x - main_tr.sizeDelta.x, 0f);
            TOOLTIP_TR.sizeDelta = new Vector2(-TYPE_ICON_TR.sizeDelta.x, 0f);
            TOOLTIP_TR.anchoredPosition = new Vector2(TYPE_ICON_TR.sizeDelta.x / 2f, 1);
            TOOLTIP2_TR.anchoredPosition = new Vector2(0f,1f);
            Mod.Log.Info?.Write($" TYPE_ICON_TR:{TYPE_ICON_TR.sizeDelta} main_tr:{main_tr.sizeDelta} TOOLTIP_TR:{TOOLTIP_TR.sizeDelta},{TOOLTIP_TR.anchoredPosition}");
            var tooltip2 = TOOLTIP2.GetComponent<HBSTooltip>();
            if (tooltip2 != null) { GameObject.Destroy(tooltip2); }
            //parent.gameObject.name = ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView + FullMechSalvageInfo.FULL_MECH_SUFFIX + "(Clone)";
            var result = TOOLTIP2.AddComponent<QuickSellUIItem>();
            result.TYPE_ICON_TR = TYPE_ICON_TR;
            result.TOOLTIP2_TR = TOOLTIP2_TR;
            result.TOOLTIP_TR = TOOLTIP_TR;
            result.main_tr = main_tr;
            GameObject priceElement = GameObject.Instantiate(parent.qtyElement);
            priceElement.transform.SetParent(parent.qtyElement.transform.parent);
            priceElement.transform.localScale = Vector3.one;
            priceElement.transform.localPosition = parent.qtyElement.transform.localPosition;
            result.priceElement = priceElement;
            if (result.priceElement != null)
            {
                result.priceElement.name = "price";
                result.priceIcon = result.priceElement.GetComponentInChildren<SVGImage>(true);
                result.priceText = result.priceElement.GetComponentInChildren<LocalizableText>(true);
                result.priceTooltip = result.priceElement.GetComponentInChildren<HBSTooltip>(true);
                result.priceElement.SetActive(false);
                if(result.priceTooltip != null)
                {
                    result.priceTooltip.SetDefaultStateData(TooltipUtilities.GetStateDataFromObject("SELLING PRICE"));
                }
            }
            if (UIManager.Instance.dataManager.Exists(BattleTechResourceType.SVGAsset, Mod.Config.SellIcon))
            {
                result.hoverIcon = UIManager.Instance.dataManager.GetObjectOfType<SVGAsset>(Mod.Config.SellIcon, BattleTechResourceType.SVGAsset);
                if(result.priceIcon != null) result.priceIcon.vectorGraphics = result.hoverIcon;
            }
            else
            {
                if (UIManager.Instance.dataManager.ResourceLocator.EntryByID(Mod.Config.SellIcon, BattleTechResourceType.SVGAsset) != null)
                {
                    LoadRequest loadRequest = UIManager.Instance.dataManager.CreateLoadRequest();
                    loadRequest.AddLoadRequest<SVGAsset>(BattleTechResourceType.SVGAsset, Mod.Config.SellIcon, result.IconLoaded);
                    loadRequest.ProcessRequests();
                }
            }
            result.enabled = false;
            return result;
        }
        public override void OnPointerClick(PointerEventData eventData)
        {
            
            Mod.Log.Info?.Write($"QuickSellUIItem.OnPointerClick {eventData.clickCount}");
            try
            {
                this.owner?.SellItem(eventData.clickCount);
                //FullMechSalvageInfoPopup.Instance?.SetData(this)?.Show();
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
        public override void OnPointerEnter(PointerEventData eventData)
        {
            Mod.Log.Info?.Write($"QuickSellUIItem.OnPointerEnter");
            this.owner?.ItemWidget?.tooltip?.OnPointerEnter(eventData);
            if (this.owner != null)
            {
                this.owner.ItemWidget.icon.vectorGraphics = this.hoverIcon;
            }
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            Mod.Log.Info?.Write($"QuickSellUIItem.OnPointerExit");
            this.owner?.ItemWidget?.tooltip?.OnPointerExit(eventData);
            if (this.owner != null)
            {
                this.owner.ItemWidget.icon.vectorGraphics = this.originalIcon;
            }
        }
    }
    //[HarmonyPatch(typeof(ListElementController_SalvageWeapon_NotListView), "InitAndCreate")]
    //internal static class ListElementController_SalvageWeapon_NotListView_InitAndCreate
    //{

    //    public static void Postfix(ListElementController_SalvageWeapon_NotListView __instance, SalvageDef theSalvageDef, SimGameState theSim, DataManager dm, IMechLabDropTarget dropParent, int theQuantity, bool isStoreItem)
    //    {
    //        try
    //        {
    //            QuickSellUIItem qsitem = __instance.ItemWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
    //            if (qsitem == null)
    //            {
    //                qsitem = QuickSellUIItem.Instantine(__instance.ItemWidget);
    //            }
    //            qsitem.Init(__instance);
    //            qsitem.enabled = false;
    //        }catch(Exception e)
    //        {
    //            UIManager.logger.LogException(e);
    //        }
    //    }
    //}
    //[HarmonyPatch(typeof(ListElementController_SalvageGear_NotListView), "InitAndCreate")]
    //internal static class ListElementController_SalvageGear_NotListView_InitAndCreate
    //{

    //    public static void Postfix(ListElementController_SalvageWeapon_NotListView __instance, SalvageDef theSalvageDef, SimGameState theSim, DataManager dm, IMechLabDropTarget dropParent, int theQuantity, bool isStoreItem)
    //    {
    //        try { 
    //            QuickSellUIItem qsitem = __instance.ItemWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
    //            if (qsitem == null)
    //            {
    //                qsitem = QuickSellUIItem.Instantine(__instance.ItemWidget);
    //            }
    //            qsitem.Init(__instance);
    //            qsitem.enabled = false;
    //        }catch(Exception e)
    //        {
    //            UIManager.logger.LogException(e);
    //        }
    //    }
    //}
    //[HarmonyPatch(typeof(ListElementController_SalvageMechPart_NotListView), "InitAndCreate")]
    //internal static class ListElementController_SalvageMechPart_NotListView_InitAndCreate
    //{

    //    public static void Postfix(ListElementController_SalvageMechPart_NotListView __instance, SalvageDef theSalvageDef, SimGameState theSim, DataManager dm, IMechLabDropTarget dropParent, int theQuantity, bool isStoreItem)
    //    {
    //        try
    //        {
    //            QuickSellUIItem qsitem = __instance.ItemWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
    //            if (qsitem == null)
    //            {
    //                qsitem = QuickSellUIItem.Instantine(__instance.ItemWidget);
    //            }
    //            qsitem.Init(__instance);
    //            qsitem.enabled = false;
    //        }
    //        catch (Exception e)
    //        {
    //            UIManager.logger.LogException(e);
    //        }
    //    }
    //}
    [HarmonyPatch(typeof(InventoryItemElement_NotListView), "SetData")]
    [HarmonyPatch(new Type[] { typeof(ListElementController_BASE_NotListView), typeof(IMechLabDropTarget), typeof(int), typeof(bool), typeof(UnityAction<InventoryItemElement_NotListView>) })]
    internal static class InventoryItemElement_NotListView_SetData
    {

        public static void Postfix(InventoryItemElement_NotListView __instance, ListElementController_BASE_NotListView controller, IMechLabDropTarget dropParent, int quantity, bool isStoreItem, UnityAction<InventoryItemElement_NotListView> callback)
        {
            try
            {
                QuickSellUIItem qsitem = __instance.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                if (qsitem == null)
                {
                    qsitem = QuickSellUIItem.Instantine(__instance);
                }
                qsitem.Init(controller);
                qsitem.enabled = false;
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(ListElementController_BASE_NotListView), "Pool")]
    internal static class ListElementController_BASE_NotListView_Pool
    {

        public static void Prefix(ListElementController_BASE_NotListView __instance)
        {
            try { 
                if (__instance.ItemWidget == null) { return; }
                QuickSellUIItem qsitem = __instance.ItemWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                if (qsitem != null)
                {
                    qsitem.Pool();
                }
            }catch(Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(DataManager), "PoolGameObject")]
    internal static class DataManager_PoolGameObject
    {

        public static void Postfix(DataManager __instance, string id, GameObject gameObj)
        {
            try
            {
                if (gameObj == null) { return; }
                if (id != ListElementController_BASE_NotListView.INVENTORY_ELEMENT_PREFAB_NotListView) { return; } 
                QuickSellUIItem qsitem = gameObj.GetComponentInChildren<QuickSellUIItem>(true);
                if (qsitem != null)
                {
                    qsitem.Pool();
                }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(RewardsPopup), "ClearItems")]
    internal static class RewardsPopup_ClearItems
    {

        public static void Prefix(RewardsPopup __instance)
        {
            try
            {
                foreach(var item in __instance.AllSalvageControllers)
                {
                    if (item == null) { continue; }
                    if (item.ItemWidget == null) { continue; }
                    QuickSellUIItem qsitem = item.ItemWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                    if (qsitem != null)
                    {
                        qsitem.Pool();
                    }
                }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(RewardsPopup), "AllItemsReady")]
    internal static class RewardsPopup_AllItemsReady
    {

        public static void Prefix(RewardsPopup __instance)
        {
            try
            {
                foreach (var item in __instance.AllSalvageControllers)
                {
                    if (item == null) { continue; }
                    if (item.salvageDef == __instance.rarestSalvageDef) { continue; }
                    if (item.ItemWidget == null) { continue; }
                    QuickSellUIItem qsitem = item.ItemWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                    if (qsitem == null) { continue; }
                    if (Mod.Config.UseImprovedSellUI == false) { continue; }
                    qsitem.enabled = true;
                    qsitem.UpdateIcon();
                    qsitem.UpdatePrice(false);
                }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(AAR_SalvageChosen), "AddLeftovers")]
    internal static class RewardsPopup_AddLeftovers
    {

        public static void Prefix(AAR_SalvageChosen __instance, InventoryItemElement_NotListView item)
        {
            try
            {
                if (item == null) { return; }
                QuickSellUIItem qsitem = item.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                if (qsitem == null) { return; }
                if (item.controller == null) { return; }
                if (item.controller.salvageDef == null) { return; }
                if (item.controller.salvageDef.GetDefSellCost() == 0f) { return; }
                if (Mod.Config.UseImprovedSellUI == false) { return; }
                qsitem.enabled = true;
                qsitem.UpdateIcon();
                qsitem.UpdatePrice(false);
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(ListElementController_InventoryGear_NotListView), "SetupLook")]
    internal static class ListElementController_InventoryGear_NotListView_SetupLook
    {

        public static void Postfix(ListElementController_InventoryGear_NotListView __instance, InventoryItemElement_NotListView theWidget)
        {
            try
            {
                if (theWidget == null) { return; }
                QuickSellUIItem qsitem = theWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                if (qsitem == null) { return; }
                if (theWidget.controller == null) { return; }
                if (theWidget.controller.componentDef == null) { return; }
                if (theWidget.controller.quantity == 0) { return; }
                if (Mod.Config.UseImprovedSellUI == false) { return; }
                if (UnityGameInstance.BattleTechGame.Simulation == null) { return; }
                qsitem.enabled = true;
                qsitem.UpdateIcon();
                qsitem.UpdateMechLabPrice();
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(ListElementController_InventoryWeapon_NotListView), "SetupLook")]
    internal static class ListElementController_InventoryWeapon_NotListView_SetupLook
    {

        public static void Postfix(ListElementController_InventoryGear_NotListView __instance, InventoryItemElement_NotListView theWidget)
        {
            try
            {
                if (theWidget == null) { return; }
                QuickSellUIItem qsitem = theWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                if (qsitem == null) { return; }
                if (theWidget.controller == null) { return; }
                if (theWidget.controller.componentDef == null) { return; }
                if (theWidget.controller.quantity == 0) { return; }
                if (Mod.Config.UseImprovedSellUI == false) { return; }
                if (UnityGameInstance.BattleTechGame.Simulation == null) { return; }
                qsitem.enabled = true;
                qsitem.UpdateIcon();
                qsitem.UpdateMechLabPrice();
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }
        }
    }
    [HarmonyPatch(typeof(AAR_SalvageScreen), "CalculateAndAddAvailableSalvage")]
    public static class AAR_SalvageScreen_CalculateAndAddAvailableSalvage
    {

        public static void Postfix(AAR_SalvageScreen __instance)
        {
            try
            {
                foreach (var item in __instance.AllSalvageControllers)
                {
                    if (item == null) { continue; }
                    if (item.ItemWidget == null) { continue; }
                    QuickSellUIItem qsitem = item.ItemWidget.gameObject.GetComponentInChildren<QuickSellUIItem>(true);
                    if (qsitem == null) { continue; }
                    if (Mod.Config.UseImprovedSellUI == false) { continue; }
                    qsitem.UpdateIcon();
                    qsitem.UpdatePrice(true);
                }
            }
            catch (Exception e)
            {
                UIManager.logger.LogException(e);
            }

        }
    }
}
