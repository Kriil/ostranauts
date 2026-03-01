using System;
using System.Collections.Generic;
using System.Linq;
using FFU_Beyond_Reach;
using MonoMod;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;
public class patch_GUIInventory : GUIInventory
{
	[MonoModReplace]
	public GUIInventoryWindow SpawnInventoryWindow(patch_CondOwner CO, InventoryWindowType type, GUIInventoryWindow winParent, Vector3? vPos = null)
	{
		bool flag = CO == null;
		GUIInventoryWindow result;
		if (flag)
		{
			result = null;
		}
		else
		{
			CanvasManager.ShowCanvasGroup(this.canvasGroup);
			bool flag2 = type == null || CO.objContainer != null || CO.dictSlotsLayout.Count > 0;
			GUIInventoryWindow guiinventoryWindow = null;
			for (int i = 0; i < this.activeWindows.Count; i++)
			{
				bool flag3 = !flag2;
				if (flag3)
				{
					break;
				}
				bool flag4 = this.activeWindows[i].CO == CO && type == this.activeWindows[i].type;
				if (flag4)
				{
					flag2 = false;
					guiinventoryWindow = this.activeWindows[i];
				}
			}
			bool flag5 = flag2;
			if (flag5)
			{
				GameObject gameObject = Object.Instantiate<GameObject>(this.inventoryGridPrefab, base.transform);
				GUIInventoryWindow component = gameObject.GetComponent<GUIInventoryWindow>();
				this.activeWindows.Add(component);
				component.SetData(CO, type);
				guiinventoryWindow = component;
			}
			List<Slot> list = FFU_BR_Defs.StrictInvSorting ? CO.GetSortedSlots() : CO.GetSlots(true, 1);
			List<CondOwner> list2 = new List<CondOwner>();
			foreach (Slot slot in list)
			{
				bool flag6 = slot.strName == "social";
				if (flag6)
				{
					CondOwner condOwner = slot.aCOs.FirstOrDefault<CondOwner>();
					base.SpawnSocialMovesWindow(condOwner);
				}
				else
				{
					bool bHide = slot.bHide;
					if (!bHide)
					{
						CondOwner[] aCOs = slot.aCOs;
						foreach (CondOwner condOwner2 in aCOs)
						{
							bool flag7 = condOwner2 != null && GUIInventory.CTOpenInv.Triggered(condOwner2, null, true) && (condOwner2.objContainer != null || (condOwner2.dictSlotsLayout != null && condOwner2.dictSlotsLayout.Count != 0));
							if (flag7)
							{
								bool flag8 = CO.dictSlotsLayout != null && CO.dictSlotsLayout.ContainsKey(slot.strName);
								if (flag8)
								{
									GUIInventoryWindow guiinventoryWindow2 = base.SpawnInventoryWindow(condOwner2, 1, guiinventoryWindow, new Vector3?(CO.dictSlotsLayout[slot.strName]));
									guiinventoryWindow2.ToggleTab(false);
								}
								else
								{
									list2.Add(condOwner2);
								}
							}
						}
					}
				}
			}
			bool flag9 = flag2;
			if (flag9)
			{
				bool flag10 = vPos != null && winParent != null;
				if (flag10)
				{
					guiinventoryWindow.transform.localPosition = winParent.transform.localPosition + vPos.GetValueOrDefault() * 1.5f * CanvasManager.CanvasRatio;
					guiinventoryWindow.transform.SetParent(winParent.transform, true);
					bool flag11 = FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime;
					if (flag11)
					{
						Debug.Log(string.Format("#DEBUG# Windows/Parent Index: {0}", this.activeWindows.IndexOf(guiinventoryWindow)) + string.Format("/{0}, X: {1}, Y: {2}, Height: ", this.activeWindows.IndexOf(winParent), guiinventoryWindow.gridBorderRect.rect.x, guiinventoryWindow.gridBorderRect.rect.y) + string.Format("{0}, Width: {1}, CO: {2}", guiinventoryWindow.gridBorderRect.rect.height, guiinventoryWindow.gridBorderRect.rect.width, guiinventoryWindow.CO.strName));
					}
				}
				else
				{
					guiinventoryWindow.ResetBorder();
					int num = this.activeWindows.IndexOf(guiinventoryWindow);
					GUIInventoryWindow guiinventoryWindow3 = winParent;
					bool flag12 = guiinventoryWindow3 == null && num > 0;
					if (flag12)
					{
						for (int k = num - 1; k >= 0; k--)
						{
							bool flag13 = !this.activeWindows[k].IsChild && !this.activeWindows[k].bCustomPos;
							if (flag13)
							{
								guiinventoryWindow3 = this.activeWindows[k];
								break;
							}
						}
					}
					guiinventoryWindow.transform.localPosition = base.GetWindowPosition(guiinventoryWindow, guiinventoryWindow3) * 1.5f * CanvasManager.CanvasRatio;
					bool flag14 = FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime;
					if (flag14)
					{
						Debug.Log(string.Format("#DEBUG# Windows/Parent Index: {0}", this.activeWindows.IndexOf(guiinventoryWindow)) + string.Format("/{0}, X: {1}, Y: {2}, Height: ", this.activeWindows.IndexOf(guiinventoryWindow3), guiinventoryWindow.gridBorderRect.rect.x, guiinventoryWindow.gridBorderRect.rect.y) + string.Format("{0}, Width: {1}, CO: {2}", guiinventoryWindow.gridBorderRect.rect.height, guiinventoryWindow.gridBorderRect.rect.width, guiinventoryWindow.CO.strName));
					}
				}
				CanvasManager.SetAnchorsToCorners(guiinventoryWindow.transform);
				bool flag15 = winParent == null;
				if (flag15)
				{
					base.StartCoroutine(base.FlyIn(guiinventoryWindow));
				}
			}
			foreach (CondOwner condOwner3 in list2)
			{
				base.SpawnInventoryWindow(condOwner3, 1, winParent, null);
			}
			bool flag16 = CrewSim.coPlayer.HasCond("TutorialLockerWaiting") && GUIInventory.instance.IsCOShown(CrewSim.coPlayer) && (CO.HasCond("TutorialLockerTarget") || (CO.HasCond("IsStorageFurniture") && CrewSim.coPlayer.HasCond("IsENCFirstDockOKLG")));
			if (flag16)
			{
				CrewSim.coPlayer.ZeroCondAmount("TutorialLockerWaiting");
				MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			}
			bool flag17 = GUIInventory.OnOpenInventory != null && guiinventoryWindow != null;
			if (flag17)
			{
				GUIInventory.OnOpenInventory.Invoke(guiinventoryWindow);
			}
			result = guiinventoryWindow;
		}
		return result;
	}
}
