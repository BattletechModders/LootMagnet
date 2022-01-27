using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using BattleTech.UI.Tooltips;
using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LootMagnet {
  //uixPrfPanl_AA_nextButtonPanel
  //AAR_SalvageScreen
  //uixPrfPanl_SIM_mechStorage-Widget
  public static class TransformHelper {
    public static T FindComponent<T>(this GameObject go, string checkName) where T : Component {
      T[] components = go.GetComponentsInChildren<T>(true);
      foreach (T t in components) {
        if (t.transform.name == checkName) return t;
      }
      return null;
    }
  }

  //public static class CustomSalvageHelper {
  //  private static MethodInfo GetPrefabId_mi = null;
  //  private static MethodInfo GetMDefFromCDef_mi = null;
  //  public static HashSet<string> GetCompatibleChassis(string chassisId, SimGameState sim) {
  //    HashSet<string> result = new HashSet<string>();
  //    result.Add(chassisId);
  //    Mod.Log.Debug?.Write("GetCompatibleChassis '" + chassisId + "'");
  //    if (GetPrefabId_mi != null && GetMDefFromCDef_mi != null) {
  //      try {
  //        string mechDefId = (string)GetMDefFromCDef_mi.Invoke(null, new object[] { chassisId });
  //        Mod.Log.Debug?.Write("mechDefId '" + mechDefId + "'");
  //        if (sim.DataManager.MechDefs.TryGet(mechDefId, out var mechDef)) {
  //          string prefabId = (string)GetPrefabId_mi.Invoke(null, new object[] { mechDef });
  //          Mod.Log.Debug?.Write("prefabId '" + prefabId + "'");
  //          List<ChassisDef> chassisDefs = sim.GetAllInventoryMechDefs(true);
  //          foreach (ChassisDef chassis in chassisDefs) {
  //            mechDefId = (string)GetMDefFromCDef_mi.Invoke(null, new object[] { chassis.Description.Id });
  //            Mod.Log.Debug?.Write(" chassis '" + chassis.Description.Id + "' mechDef " + mechDefId);
  //            if (sim.DataManager.MechDefs.TryGet(mechDefId, out var compatibleMechDef)) {
  //              string compatiblePrefabId = (string)GetPrefabId_mi.Invoke(null, new object[] { compatibleMechDef });
  //              Mod.Log.Debug?.Write(" prefabId " + compatiblePrefabId);
  //              if (compatiblePrefabId == prefabId) { result.Add(chassis.Description.Id); }
  //            }
  //          }
  //        }
  //      } catch (Exception e) {
  //        Mod.Log.Error?.Write(e);
  //      }
  //    }
  //    foreach (string id in result) {
  //      Mod.Log.Debug?.Write(id);
  //    }
  //    return result;
  //  }
  //  public static void DetectAPI() {
  //    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
  //      if (assembly.FullName.StartsWith("CustomSalvage")) {
  //        Mod.Log.Debug?.Write("CustomSalvage assembly found " + assembly.FullName);
  //        Type ChassisHandler = assembly.GetType("CustomSalvage.ChassisHandler");
  //        if (ChassisHandler != null) {
  //          Mod.Log.Debug?.Write("ChassisHandler type found " + ChassisHandler.FullName);
  //          GetPrefabId_mi = ChassisHandler.GetMethod("GetPrefabId", BindingFlags.Static | BindingFlags.Public);
  //          if (GetPrefabId_mi != null) {
  //            Mod.Log.Debug?.Write("ChassisHandler.GetCompatible method found " + GetPrefabId_mi.Name);
  //          }
  //          GetMDefFromCDef_mi = ChassisHandler.GetMethod("GetMDefFromCDef", BindingFlags.Static | BindingFlags.Public);
  //          if (GetMDefFromCDef_mi != null) {
  //            Mod.Log.Debug?.Write("ChassisHandler.GetMDefFromCDef method found " + GetMDefFromCDef_mi.Name);
  //          }
  //        }
  //        break;
  //      }
  //    }
  //  }
  //}

  public static class CustomSalvageHelper {
    private static MethodInfo GetCompatible_mi = null;
    private static MethodInfo RegisterMechDef_mi = null;
    public static void RegisterMechDef(MechDef mechDef) {
      RegisterMechDef_mi?.Invoke(null, new object[] { mechDef, 0 });
    }
    public static HashSet<string> GetCompatibleChassis(string chassisId) {
      HashSet<string> result = new HashSet<string>();
      result.Add(chassisId);
      if(GetCompatible_mi != null) {
        try {
          List<MechDef> mechDefs = (List<MechDef>)GetCompatible_mi.Invoke(null, new object[] { chassisId });
          foreach(MechDef mechDef in mechDefs) {
            result.Add(mechDef.ChassisID);
          }
        } catch(Exception e) {
          Mod.Log.Error?.Write(e);
        }
      }
      Mod.Log.Debug?.Write("GetCompatibleChassis "+chassisId);
      foreach(string id in result) {
        Mod.Log.Debug?.Write(" '" + id+"'");
      }
      return result;
    }
    public static void DetectAPI() {
      foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
        if (assembly.FullName.StartsWith("CustomSalvage")) {
          Mod.Log.Debug?.Write("CustomSalvage assembly found "+assembly.FullName);
          Type ChassisHandler = assembly.GetType("CustomSalvage.ChassisHandler");
          if(ChassisHandler != null) {
            Mod.Log.Debug?.Write("ChassisHandler type found " + ChassisHandler.FullName);
            GetCompatible_mi = ChassisHandler.GetMethod("GetCompatible", BindingFlags.Static | BindingFlags.Public);
            if(GetCompatible_mi != null) {
              Mod.Log.Debug?.Write("ChassisHandler.GetCompatible method found " + GetCompatible_mi.Name);
            }
            RegisterMechDef_mi = ChassisHandler.GetMethod("RegisterMechDef", BindingFlags.Static | BindingFlags.Public);
            if (RegisterMechDef_mi != null) {
              Mod.Log.Debug?.Write("ChassisHandler.RegisterMechDef method found " + RegisterMechDef_mi.Name);
            }
          }
          break;
        }
      }
    }
  }
  public class SalvageStorageChassisWidget : MonoBehaviour {
    public SalavageStorageWidget parent { get; set; } = null;
    public string chassisId { get; set; } = string.Empty;
    public string mechId { get; set; } = string.Empty;
    public HashSet<string> compatibleChassis { get; set; } = null;
    public void OnPointerClick() {
      Mod.Log.Info?.Write("SalvageStorageChassisWidget.OnPointerClick "+chassisId);
      if (compatibleChassis == null) {
        compatibleChassis = CustomSalvageHelper.GetCompatibleChassis(chassisId);
      }
      foreach(string chID in compatibleChassis) {
        Mod.Log.Info?.Write(" " + chID);
      }
      if(parent != null) {
        if(parent.storageWidget != null) {
          for (int index = 0; index < parent.storageWidget.inventory.Count; ++index) {
            IMechLabDraggableItem labDraggableItem = parent.storageWidget.inventory[index];
            labDraggableItem.GameObject.SetActive(compatibleChassis.Contains(labDraggableItem.ChassisDef.Description.Id));
          }
        }
      }
    }
    public void OnPointerEnter() {
      Mod.Log.Info?.Write("SalvageStorageChassisWidget.OnPointerEnter " + chassisId);
    }
    public void OnPointerExit() {
      Mod.Log.Info?.Write("SalvageStorageChassisWidget.OnPointerExit " + chassisId);
    }
  }
  public class SalavageStorageWidget : MonoBehaviour, IMechLabDropTarget {
    public HashSet<SalvageStorageChassisWidget> childWidgets { get; set; } = new HashSet<SalvageStorageChassisWidget>();
    public MechBayMechStorageWidget storageWidget { get; set; } = null;
    public GameObject storageWidgetGO { get; set; } = null;
    public GameObject storageButtonGO { get; set; } = null;
    public AAR_SalvageScreen salvageScreen { get; set; } = null;
    public MechLabDropTargetType dropTargetType { get { return MechLabDropTargetType.MechList; } }
    private MechBayChassisUnitElement dragItem { get; set; } = null;
    public IMechLabDraggableItem DragItem => (IMechLabDraggableItem)this.dragItem;
    public IMechLabDropTarget ParentDropTarget => (IMechLabDropTarget)this;
    public bool Initialized => true;
    public bool StackQuantities => false;
    public bool IsSimGame => true;
    public SimGameState Sim { get { return salvageScreen ? salvageScreen.Sim : null; } }
    public void ApplicationFocusChange(bool hasFocus) {
    }
    public bool OnAddItem(IMechLabDraggableItem item, bool validate) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnAddItem " + item.GetType().Name);
      return true;
    }
    public void OnButtonClicked(IMechLabDraggableItem item) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnButtonClicked " + item.GetType().Name);
    }
    public void OnButtonDoubleClicked(IMechLabDraggableItem item) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnButtonDoubleClicked " + item.GetType().Name);
    }
    public void OnDrag(PointerEventData eventData) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnDrag");
    }
    public bool OnItemGrab(IMechLabDraggableItem item, PointerEventData eventData) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnItemGrab " + item.GetType().Name);
      return false;
    }
    public void OnItemHoverEnter(IMechLabDraggableItem item, PointerEventData eventData) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnItemHoverEnter " + item.GetType().Name);
    }
    public void OnItemHoverExit(IMechLabDraggableItem item, PointerEventData eventData) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnItemHoverExit " + item.GetType().Name);
    }
    public void OnMechLabDrop(PointerEventData eventData, MechLabDropTargetType addToType) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnMechLabDrop " + addToType);
    }
    public bool OnRemoveItem(IMechLabDraggableItem item, bool validate) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnRemoveItem " + item.GetType().Name);
      return false;
    }
    public void OrderItemRepair(IMechLabDraggableItem item) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OrderItemRepair " + item.GetType().Name);
    }
    public void SetRaycastBlockerActive(bool isActive) {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.SetRaycastBlockerActive " + isActive);
    }
    public HBSDOTweenButton showHideButton { get; set; } = null;
    public void OnHideShowButton() {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnHideShowButton");
      try {
        if (this.storageWidget != null) {
          this.storageWidget.gameObject.SetActive(!this.storageWidget.gameObject.activeSelf);
          return;
        }
        showHideButton?.SetState(ButtonState.Disabled, true);
        storageWidget = this.gameObject.GetComponentInChildren<MechBayMechStorageWidget>();
        if (storageWidget == null) {
          storageWidgetGO = this.Sim.DataManager.PooledInstantiate("uixPrfPanl_SIM_mechStorage-Widget", BattleTechResourceType.UIModulePrefabs);
          if (storageWidgetGO != null) {
            storageWidgetGO.name = "uixPrfPanl_SIM_mechStorage-Widget-MANAGED";
            Transform Overall_layout = this.gameObject.FindComponent<Transform>("Overall-layout");
            if (Overall_layout == null) {
              Overall_layout = this.gameObject.transform;
            }
            storageWidgetGO.transform.SetParent(Overall_layout);
            storageWidgetGO.transform.localScale = Vector3.one;
            storageWidgetGO.transform.localPosition = Vector3.zero;
            storageWidget = storageWidgetGO.GetComponentInChildren<MechBayMechStorageWidget>(true);
            if (storageWidget != null) {
              GameObject uixPrfPanl_AA_SalvageLeftPanel = this.gameObject.FindComponent<Transform>("uixPrfPanl_AA_SalvageLeftPanel").gameObject;
              RectTransform deco = uixPrfPanl_AA_SalvageLeftPanel.FindComponent<RectTransform>("deco");
              RectTransform storageWidgetRT = storageWidget.gameObject.GetComponent<RectTransform>();
              storageWidgetRT.pivot = new Vector2(0f, 1f);
              storageWidgetRT.position = deco.position;
              storageWidgetRT.localScale = new Vector3(0.9f, 0.9f, 0.9f);
              Transform toDisable = storageWidget.gameObject.FindComponent<Transform>("T_brackets_cap (3)");
              toDisable?.gameObject.SetActive(false);
              toDisable = storageWidget.gameObject.FindComponent<Transform>("uixPrfDeco_brackets_cap-Bttm (1)");
              toDisable?.gameObject.SetActive(false);
              toDisable = storageWidget.gameObject.FindComponent<Transform>("Deco (1)");
              toDisable?.gameObject.SetActive(false);
              Vector3 pos = storageWidgetRT.position;
              pos.x = -100f;
              storageWidgetRT.position = pos;
            }
          }
        } else {
          storageWidgetGO = storageWidget.gameObject;
        }
        if (storageWidget != null) {
          storageWidget.SetData(this, this.Sim.DataManager, "uixPrfPanl_storageMechUnit-Element", false, true, MechLabDraggableItemType.Chassis);
          storageWidget.InitInventory(this.Sim.GetAllInventoryMechDefs(), true);
        }
        showHideButton?.SetState(ButtonState.Enabled, true);
      } catch (Exception e) {
        Mod.Log.Error?.Write(e);
      }
    }
    public void Init(AAR_SalvageScreen salvageScreen) {
      this.salvageScreen = salvageScreen;
      MechBayMechStorageWidget existingStorageWidget = this.gameObject.GetComponentInChildren<MechBayMechStorageWidget>(true);
      if (existingStorageWidget != null) { existingStorageWidget.gameObject.SetActive(false); }
      RectTransform hideShowButton = this.gameObject.FindComponent<RectTransform>("uixPrfPanl_AA_storageButtonPanel");
      if (hideShowButton == null) {
        GameObject buttonSource = this.gameObject.FindComponent<Transform>("uixPrfPanl_AA_nextButtonPanel").gameObject;
        this.storageButtonGO = GameObject.Instantiate(buttonSource);
        hideShowButton = storageButtonGO.transform as RectTransform;
        storageButtonGO.name = "uixPrfPanl_AA_storageButtonPanel";
        hideShowButton.SetParent(buttonSource.transform.parent);
        GameObject uixPrfPanl_AA_SalvageLeftPanel = this.gameObject.FindComponent<Transform>("uixPrfPanl_AA_SalvageLeftPanel").gameObject;
        RectTransform deco = uixPrfPanl_AA_SalvageLeftPanel.FindComponent<RectTransform>("deco");
        deco.gameObject.SetActive(false);
        hideShowButton.position = deco.position;
        hideShowButton.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        hideShowButton.pivot = new Vector2(0f, 0f);
        showHideButton = hideShowButton.gameObject.GetComponentInChildren<HBSDOTweenButton>();
        showHideButton.gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        showHideButton.OnClicked = new UnityEngine.Events.UnityEvent();
        showHideButton.OnClicked.AddListener(new UnityEngine.Events.UnityAction(this.OnHideShowButton));
        showHideButton.SetState(ButtonState.Enabled, true);
        LocalizableText text = hideShowButton.GetComponentInChildren<LocalizableText>();
        text?.SetText("STORAGE");
      }
    }
    public void OnDestroy() {
      Mod.Log.Debug?.Write( "SalavageStorageWidget.OnDestroy");
      if (this.storageWidget != null) { storageWidget = null; }
      if (storageWidgetGO != null) { GameObject.Destroy(storageWidgetGO); storageWidgetGO = null; }
      if (storageButtonGO != null) { GameObject.Destroy(storageButtonGO); storageButtonGO = null; }
      foreach(var childWidget in this.childWidgets) {
        GameObject.Destroy(childWidget);
      }
      childWidgets.Clear();
    }
  }
  [HarmonyPatch(typeof(AAR_SalvageScreen))]
  [HarmonyPatch("InitializeData")]
  [HarmonyPatch(MethodType.Normal)]
  public static class AAR_SalvageScreen_InitializeData {
    public static void Postfix(AAR_SalvageScreen __instance) {
      try {
        SalavageStorageWidget storageWidget = __instance.GetComponent<SalavageStorageWidget>();
        if (storageWidget == null) {
          storageWidget = __instance.gameObject.AddComponent<SalavageStorageWidget>();
        }
        if (storageWidget != null) {
          storageWidget.Init(__instance);
        }
      } catch (Exception e) {
        Mod.Log.Error?.Write(e);
      }
    }
  }
  [HarmonyPatch(typeof(AAR_SalvageScreen))]
  [HarmonyPatch("CalculateAndAddAvailableSalvage")]
  [HarmonyPatch(MethodType.Normal)]
  public static class AAR_CalculateAndAddAvailableSalvage {
    public static void Postfix(AAR_SalvageScreen __instance, List<ListElementController_BASE_NotListView> ___AllSalvageControllers) {
      try {
        SalavageStorageWidget storageWidget = __instance.GetComponent<SalavageStorageWidget>();
        if (storageWidget == null) {
          storageWidget = __instance.gameObject.AddComponent<SalavageStorageWidget>();
          storageWidget.Init(__instance);
        }
        if (storageWidget != null) {
          __instance.Sim.GetAllInventoryMechDefs(); //to make CustomSalvageRecalculate things
          foreach(var salvageElement in ___AllSalvageControllers) {
            var chassisElement = salvageElement as ListElementController_SalvageMechPart_NotListView;
            if (chassisElement == null) { continue; }
            if (chassisElement.ItemWidget == null) { continue; }
            SalvageStorageChassisWidget storageChassisWidget = chassisElement.ItemWidget.gameObject.GetComponent<SalvageStorageChassisWidget>();
            if(storageChassisWidget == null) {
              storageChassisWidget = chassisElement.ItemWidget.gameObject.AddComponent<SalvageStorageChassisWidget>();
            }
            if (storageChassisWidget != null) {
              storageChassisWidget.parent = storageWidget;
              storageWidget.childWidgets.Add(storageChassisWidget);
              storageChassisWidget.chassisId = chassisElement.chassisDef.Description.Id;
              storageChassisWidget.mechId = chassisElement.mechDef.Description.Id;
              CustomSalvageHelper.RegisterMechDef(chassisElement.mechDef);
            }
          }
        }
      } catch (Exception e) {
        Mod.Log.Error?.Write(e);
      }
    }
  }
  [HarmonyPatch(typeof(AAR_SalvageScreen))]
  [HarmonyPatch("OnCompleted")]
  [HarmonyPatch(MethodType.Normal)]
  public static class AAR_SalvageScreen_OnCompleted_storage {
    public static void Prefix(AAR_SalvageScreen __instance) {
      try {
        SalavageStorageWidget storageWidget = __instance.GetComponent<SalavageStorageWidget>();
        if (storageWidget == null) {
          GameObject.Destroy(storageWidget);
        }
      } catch (Exception e) {
        Mod.Log.Error?.Write(e);
      }
    }
  }
  [HarmonyPatch(typeof(InventoryItemElement_NotListView))]
  [HarmonyPatch("OnButtonClicked")]
  [HarmonyPatch(MethodType.Normal)]
  public static class InventoryItemElement_NotListView_OnButtonClicked_Storage {
    public static void Postfix(InventoryItemElement_NotListView __instance) {
      try {
        SalvageStorageChassisWidget storageChassisWidget = __instance.gameObject.GetComponent<SalvageStorageChassisWidget>();
        if(storageChassisWidget != null) {
          storageChassisWidget.OnPointerClick();
        }
      } catch (Exception e) {
        Mod.Log.Error?.Write(e);
      }
    }
  }
  [HarmonyPatch(typeof(InventoryItemElement_NotListView))]
  [HarmonyPatch("OnPointerEnter")]
  [HarmonyPatch(MethodType.Normal)]
  public static class InventoryItemElement_NotListView_OnPointerEnter {
    public static void Postfix(InventoryItemElement_NotListView __instance) {
      try {
        SalvageStorageChassisWidget storageChassisWidget = __instance.gameObject.GetComponent<SalvageStorageChassisWidget>();
        if (storageChassisWidget != null) {
          storageChassisWidget.OnPointerEnter();
        }
      } catch (Exception e) {
        Mod.Log.Error?.Write(e);
      }
    }
  }
  [HarmonyPatch(typeof(InventoryItemElement_NotListView))]
  [HarmonyPatch("OnPointerExit")]
  [HarmonyPatch(MethodType.Normal)]
  public static class InventoryItemElement_NotListView_OnPointerExit {
    public static void Postfix(InventoryItemElement_NotListView __instance) {
      try {
        SalvageStorageChassisWidget storageChassisWidget = __instance.gameObject.GetComponent<SalvageStorageChassisWidget>();
        if (storageChassisWidget != null) {
          storageChassisWidget.OnPointerExit();
        }
      } catch (Exception e) {
        Mod.Log.Error?.Write(e);
      }
    }
  }

}