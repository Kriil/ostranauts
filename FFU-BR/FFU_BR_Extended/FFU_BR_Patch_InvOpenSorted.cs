using FFU_Beyond_Reach;
using MonoMod;
using Ostranauts.Core;
using Ostranauts.Objectives;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class patch_JsonSlot : JsonSlot {
    public int? nSlotOrder { get; set; }
}

public partial class patch_Slot : Slot {
    public int nSlotOrder;
    [MonoModIgnore] public extern patch_Slot(JsonSlot jslot);
    [MonoModOriginal] public extern void orig_Slot(JsonSlot jslot);
    [MonoModConstructor] public void Slot(patch_JsonSlot jslot) {
        orig_Slot(jslot);
        nSlotOrder = jslot.nSlotOrder != null ? jslot.nSlotOrder.Value : jslot.nDepth;
    }
}

public partial class patch_GUIInventory : GUIInventory {
    [MonoModReplace] public GUIInventoryWindow SpawnInventoryWindow(patch_CondOwner CO, InventoryWindowType type, GUIInventoryWindow winParent, Vector3? vPos = null) {
        if (CO == null) return null;
        CanvasManager.ShowCanvasGroup(canvasGroup);
        bool isValid = type == InventoryWindowType.Ground || CO.objContainer != null || CO.dictSlotsLayout.Count > 0;
        GUIInventoryWindow invWindowMain = null;
        for (int i = 0; i < activeWindows.Count; i++) {
            if (!isValid) break;
            if (activeWindows[i].CO == CO && type == activeWindows[i].type) {
                isValid = false;
                invWindowMain = activeWindows[i];
            }
        }
        if (isValid) {
            GameObject invGO = Object.Instantiate(inventoryGridPrefab, base.transform);
            GUIInventoryWindow winCurr = invGO.GetComponent<GUIInventoryWindow>();
            activeWindows.Add(winCurr);
            winCurr.SetData(CO, type);
            invWindowMain = winCurr;
        }
        List<Slot> sortedSlots = FFU_BR_Defs.StrictInvSorting ? CO.GetSortedSlots() : CO.GetSlots(true);
        List<CondOwner> nonDictCOs = [];
        foreach (Slot slot in sortedSlots) {
            if (slot.strName == "social") {
                CondOwner coSlotted = slot.aCOs.FirstOrDefault();
                SpawnSocialMovesWindow(coSlotted);
            } else {
                if (slot.bHide) continue;
                CondOwner[] aCOs = slot.aCOs;
                foreach (CondOwner refCO in aCOs) {
                    if (refCO != null && CTOpenInv.Triggered(refCO) && (refCO.objContainer != null || (refCO.dictSlotsLayout != null && refCO.dictSlotsLayout.Count != 0))) {
                        GUIInventoryWindow invWindowSub = null;
                        if (CO.dictSlotsLayout != null && CO.dictSlotsLayout.ContainsKey(slot.strName)) {
                            invWindowSub = SpawnInventoryWindow(refCO, InventoryWindowType.Container, invWindowMain, CO.dictSlotsLayout[slot.strName]);
                            invWindowSub.ToggleTab(false);
                        } else nonDictCOs.Add(refCO);
                    }
                }
            }
        }
        if (isValid) {
            if (vPos.HasValue && winParent != null) {
                invWindowMain.transform.localPosition = winParent.transform.localPosition + vPos.GetValueOrDefault() * 1.5f * CanvasManager.CanvasRatio;
                invWindowMain.transform.SetParent(winParent.transform, true);
                if (FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime) Debug.Log($"#DEBUG# Windows/Parent Index: {activeWindows.IndexOf(invWindowMain)}" +
                $"/{activeWindows.IndexOf(winParent)}, X: {invWindowMain.gridBorderRect.rect.x}, Y: {invWindowMain.gridBorderRect.rect.y}, Height: " +
                $"{invWindowMain.gridBorderRect.rect.height}, Width: {invWindowMain.gridBorderRect.rect.width}, CO: {invWindowMain.CO.strName}");
            } else {
                invWindowMain.ResetBorder();
                int invIdx = activeWindows.IndexOf(invWindowMain);
                GUIInventoryWindow winParentRef = winParent;
                if (winParentRef == null && invIdx > 0) {
                    for (int refIdx = invIdx - 1; refIdx >= 0; refIdx--) {
                        if (!activeWindows[refIdx].IsChild && !activeWindows[refIdx].bCustomPos) {
                            winParentRef = activeWindows[refIdx];
                            break;
                        }
                    }
                }
                invWindowMain.transform.localPosition = GetWindowPosition(invWindowMain, winParentRef) * 1.5f * CanvasManager.CanvasRatio;
                if (FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime) Debug.Log($"#DEBUG# Windows/Parent Index: {activeWindows.IndexOf(invWindowMain)}" +
                $"/{activeWindows.IndexOf(winParentRef)}, X: {invWindowMain.gridBorderRect.rect.x}, Y: {invWindowMain.gridBorderRect.rect.y}, Height: " +
                $"{invWindowMain.gridBorderRect.rect.height}, Width: {invWindowMain.gridBorderRect.rect.width}, CO: {invWindowMain.CO.strName}");
            }
            CanvasManager.SetAnchorsToCorners(invWindowMain.transform);
            if (winParent == null) StartCoroutine(FlyIn(invWindowMain));
        }
        foreach (CondOwner refCO in nonDictCOs) SpawnInventoryWindow(refCO, InventoryWindowType.Container, winParent);
        if (CrewSim.coPlayer.HasCond("TutorialLockerWaiting") &&
            instance.IsCOShown(CrewSim.coPlayer) && (CO.HasCond("TutorialLockerTarget") ||
            (CO.HasCond("IsStorageFurniture") && CrewSim.coPlayer.HasCond("IsENCFirstDockOKLG")))) {
            CrewSim.coPlayer.ZeroCondAmount("TutorialLockerWaiting");
            MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
        }
        if (OnOpenInventory != null && invWindowMain != null) OnOpenInventory.Invoke(invWindowMain);
        return invWindowMain;
    }
}

public partial class patch_CondOwner : CondOwner {
    public List<Slot> GetSortedSlots() {
        if (compSlots == null) return _emptySlotsResult ??= [];
        List<Slot> sortedSlots = [];
        GetSlotsRecursive(ref sortedSlots, compSlots?.aSlots?.ToList(),
            FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime);
        return sortedSlots;

        // Depth Sorting Method
        int SortByDepth(Slot s1, Slot s2) {
            if ((s1 as patch_Slot) == null || (s2 as patch_Slot) == null) return 0;
            return (s1 as patch_Slot).nSlotOrder.CompareTo((s2 as patch_Slot).nSlotOrder);
        }

        // Recursive Slot Sorting
        void GetSlotsRecursive(ref List<Slot> srtSlots, List<Slot> refSlots,
            bool dLog, bool dSort = true, int sDepth = 0) {
            if (refSlots != null && refSlots.Count > 0) {
                refSlots.Sort(SortByDepth);
                foreach (Slot refSlot in refSlots) {
                    if (dLog) Debug.Log($"#Info# Sorted Slot " +
                        $"{string.Empty.PadLeft(sDepth, '=')}> {refSlot.strName}");
                    srtSlots.Add(refSlot);
                    foreach (CondOwner subCO in refSlot.aCOs) {
                        List<Slot> subSlots = subCO?.compSlots?.aSlots?.ToList();
                        GetSlotsRecursive(ref srtSlots, subSlots, dLog, dSort, sDepth + 1);
                    }
                }
            }
        }
    }
}

// Reference Output: ILSpy v9.1.0.7988 / C# 12.0 / 2022.8

/* GUIInventory.SpawnInventoryWindow
public GUIInventoryWindow SpawnInventoryWindow(CondOwner CO, InventoryWindowType type, GUIInventoryWindow winParent, Vector3? vPos = null)
{
	if (CO == null)
	{
		return null;
	}
	CanvasManager.ShowCanvasGroup(canvasGroup);
	bool flag = type == InventoryWindowType.Ground || CO.objContainer != null;
	if (CO.dictSlotsLayout.Count > 0)
	{
		flag = true;
	}
	GUIInventoryWindow gUIInventoryWindow = null;
	for (int i = 0; i < activeWindows.Count; i++)
	{
		if (!flag)
		{
			break;
		}
		if (activeWindows[i].CO == CO && type == activeWindows[i].type)
		{
			flag = false;
			gUIInventoryWindow = activeWindows[i];
		}
	}
	float num = 1.5f * CanvasManager.CanvasRatio;
	if (flag)
	{
		GameObject gameObject = Object.Instantiate(inventoryGridPrefab, base.transform);
		GUIInventoryWindow component = gameObject.GetComponent<GUIInventoryWindow>();
		activeWindows.Add(component);
		component.SetData(CO, type);
		gUIInventoryWindow = component;
	}
	foreach (Slot slot in CO.GetSlots(bDeep: true))
	{
		if (slot.strName == "social")
		{
			CondOwner coSlotted = slot.aCOs.FirstOrDefault();
			SpawnSocialMovesWindow(coSlotted);
		}
		else
		{
			if (slot.bHide)
			{
				continue;
			}
			CondOwner[] aCOs = slot.aCOs;
			foreach (CondOwner condOwner in aCOs)
			{
				if (!(condOwner == null) && CTOpenInv.Triggered(condOwner) && (!(condOwner.objContainer == null) || (condOwner.dictSlotsLayout != null && condOwner.dictSlotsLayout.Count != 0)))
				{
					GUIInventoryWindow gUIInventoryWindow2 = null;
					if (CO.dictSlotsLayout != null && CO.dictSlotsLayout.ContainsKey(slot.strName))
					{
						gUIInventoryWindow2 = SpawnInventoryWindow(condOwner, InventoryWindowType.Container, gUIInventoryWindow, CO.dictSlotsLayout[slot.strName]);
						gUIInventoryWindow2.ToggleTab(bShow: false);
					}
					else
					{
						gUIInventoryWindow2 = SpawnInventoryWindow(condOwner, InventoryWindowType.Container, gUIInventoryWindow);
					}
				}
			}
		}
	}
	if (flag)
	{
		if (vPos.HasValue && winParent != null)
		{
			gUIInventoryWindow.transform.localPosition = winParent.transform.localPosition + vPos.GetValueOrDefault() * 1.5f * CanvasManager.CanvasRatio;
			gUIInventoryWindow.transform.SetParent(winParent.transform, worldPositionStays: true);
		}
		else
		{
			gUIInventoryWindow.ResetBorder();
			int num2 = activeWindows.IndexOf(gUIInventoryWindow);
			GUIInventoryWindow gUIInventoryWindow3 = winParent;
			if (gUIInventoryWindow3 == null && num2 > 0)
			{
				for (int num3 = num2 - 1; num3 >= 0; num3--)
				{
					if (!activeWindows[num3].IsChild && !activeWindows[num3].bCustomPos)
					{
						gUIInventoryWindow3 = activeWindows[num3];
						break;
					}
				}
			}
			gUIInventoryWindow.transform.localPosition = GetWindowPosition(gUIInventoryWindow, gUIInventoryWindow3) * 1.5f * CanvasManager.CanvasRatio;
		}
		CanvasManager.SetAnchorsToCorners(gUIInventoryWindow.transform);
		if (winParent == null)
		{
			StartCoroutine(FlyIn(gUIInventoryWindow));
		}
	}
	if (CrewSim.coPlayer.HasCond("TutorialLockerWaiting") && instance.IsCOShown(CrewSim.coPlayer) && (CO.HasCond("TutorialLockerTarget") || (CO.HasCond("IsStorageFurniture") && CrewSim.coPlayer.HasCond("IsENCFirstDockOKLG"))))
	{
		CrewSim.coPlayer.ZeroCondAmount("TutorialLockerWaiting");
		MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
	}
	if (OnOpenInventory != null && gUIInventoryWindow != null)
	{
		OnOpenInventory.Invoke(gUIInventoryWindow);
	}
	return gUIInventoryWindow;
}
*/

/* Slots.GetSlotsDepthFirst
public List<Slot> GetSlotsDepthFirst(bool bDeep)
{
	List<Slot> list = new List<Slot>(aSlots);
	if (bDeep)
	{
		foreach (Slot aSlot in aSlots)
		{
			list.AddRange(aSlot.GetSlots(bDeep, bChildFirst: false));
		}
	}
	list.Sort(SortBySlotDepth);
	return list;
}
*/