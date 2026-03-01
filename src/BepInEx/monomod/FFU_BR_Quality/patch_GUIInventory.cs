using System;
using FFU_Beyond_Reach;
using MonoMod;
using UnityEngine;

// QoL patch for the inventory UI layout and drag/update flow.
// This implements the configurable organized-inventory behavior documented in
// `OrgInventoryMode` and the related spacing offsets.
public class patch_GUIInventory : GUIInventory
{
	private extern Vector2 orig_GetWindowPosition(GUIInventoryWindow winCurrent, GUIInventoryWindow winPrev);
	// Overrides vanilla inventory-window stacking when organized inventory mode is on.
	// The offsets come from `OrgInventoryTweaks`, letting the mod spread windows
	// into cleaner columns instead of the stock cascade.
	private Vector2 GetWindowPosition(GUIInventoryWindow winCurrent, GUIInventoryWindow winPrev)
	{
		bool flag = !FFU_BR_Defs.OrgInventoryMode || FFU_BR_Defs.OrgInventoryTweaks.Length != 4;
		Vector2 result;
		if (flag)
		{
			result = this.orig_GetWindowPosition(winCurrent, winPrev);
		}
		else
		{
			float num = 1f / (1.5f * CanvasManager.CanvasRatio);
			bool flag2 = this.rectItmAnchor != null;
			float num2;
			float num3;
			if (flag2)
			{
				num2 = num * this.rectItmAnchor.localPosition.x;
				num3 = num * (this.rectItmAnchor.localPosition.y + FFU_BR_Defs.OrgInventoryTweaks[0]);
			}
			else
			{
				num2 = num * (this.rectPaperDoll.localPosition.x + this.rectPaperDoll.rect.width / 2f + 5f);
				num3 = num * (this.rectPaperDoll.localPosition.y + this.rectPaperDoll.rect.height / 2f + FFU_BR_Defs.OrgInventoryTweaks[0]);
			}
			float num4 = num3;
			bool flag3 = winPrev != null;
			if (flag3)
			{
				num2 = winPrev.transform.localPosition.x * num;
				num4 = (winPrev.transform.localPosition.y - winPrev.gridBorderRect.rect.height - winPrev.tabImage.rectTransform.rect.height - 5f - FFU_BR_Defs.OrgInventoryTweaks[3]) * num;
				float num5 = (winCurrent.gridBorderRect.rect.height + winCurrent.tabImage.rectTransform.rect.height) * num;
				float num6 = (Mathf.Max(winPrev.tabImage.rectTransform.rect.width, winPrev.gridBorderRect.rect.width) + 5.5f + FFU_BR_Defs.OrgInventoryTweaks[2]) * num;
				bool flag4 = num6 > this.colMaxWidth;
				if (flag4)
				{
					this.colMaxWidth = num6;
				}
				float num7 = num4 - num5;
				float num8 = -num3 - FFU_BR_Defs.OrgInventoryTweaks[1];
				bool flag5 = FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime;
				if (flag5)
				{
					Debug.Log(string.Concat(new string[]
					{
						"#Info# Current Window: ",
						winCurrent.CO.strCODef,
						", ",
						string.Format("Previous Window: {0}, yOffset: {1}, winHeight: {2}, yAnchor: {3}, ", new object[]
						{
							winPrev.CO.strCODef,
							num4,
							num5,
							num3
						}),
						string.Format("prevMaxWidth: {0}, yOffsetExpected: {1}, yOffsetLimit: {2}", num6, num7, num8)
					}));
				}
				bool flag6 = num7 < num8;
				if (flag6)
				{
					num4 = num3;
					num2 += this.colMaxWidth;
					this.colMaxWidth = 0f;
				}
			}
			else
			{
				this.colMaxWidth = 0f;
			}
			result = new Vector2(num2, num4);
		}
		return result;
	}
	// Replaces the inventory update loop so FFU_BR's layout mode can coexist with
	// the normal drag/tooltip behavior without fighting the vanilla tab mover.
	[MonoModReplace]
	private void Update()
	{
		this.bLastMouseInInv = false;
		bool flag = this.canvasGroup.alpha != 0f;
		if (flag)
		{
			this.bLastMouseInInv = base.MouseOverInventory(Input.mousePosition);
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectInv, Input.mousePosition, CrewSim.objInstance.UICamera, ref vector);
			vector.x = Mathf.Clamp(vector.x, this.rectDragBounds.rect.xMin + this.rectDragBounds.localPosition.x, this.rectDragBounds.rect.xMax + this.rectDragBounds.localPosition.x);
			vector.y = Mathf.Clamp(vector.y, this.rectDragBounds.rect.yMin + this.rectDragBounds.localPosition.y, this.rectDragBounds.rect.yMax + this.rectDragBounds.localPosition.y);
			bool flag2 = base.Selected != null;
			if (flag2)
			{
				base.Selected.transform.localPosition = new Vector3((Input.mousePosition.x - (float)Screen.width / 2f) * (1280f / (float)Screen.width * CrewSim.objInstance.AspectRatioMod()), (Input.mousePosition.y - (float)Screen.height / 2f) * 720f / (float)Screen.height, 0f);
				CanvasManager.SetAnchorsToCorners(base.Selected.transform);
				bool bLastMouseInInv = this.bLastMouseInInv;
				if (bLastMouseInInv)
				{
					base.Selected.cgSelf.alpha = 1f;
				}
				else
				{
					base.Selected.cgSelf.alpha = 0f;
				}
			}
			bool flag3 = this.selectedTab != null && (!FFU_BR_Defs.OrgInventoryMode || FFU_BR_Defs.OrgInventoryTweaks.Length != 4);
			if (flag3)
			{
				this.selectedTab.transform.parent.localPosition = new Vector3(vector.x - this.mouseOffset.x, vector.y - this.mouseOffset.y, 0f);
				CanvasManager.SetAnchorsToCorners(this.selectedTab.transform.parent.GetComponent<RectTransform>());
			}
			for (int i = this.activeWindows.Count - 1; i >= 0; i--)
			{
				this.activeWindows[i].UpdateWindow(base.CODoll);
			}
			bool flag4 = this.coTooltip != null && base.Selected == null;
			if (flag4)
			{
				this.tooltip.SetTooltip(this.coTooltip, 0);
				CrewSim.objInstance.tooltip.SetTooltip(null, 8);
				string str = this.coTooltip.FriendlyName;
				bool flag5 = this.coTooltip.StackCount > 1;
				if (flag5)
				{
					str = str + "(x" + this.coTooltip.StackCount.ToString() + ")";
				}
				str += "\n";
				bool flag6 = false;
				foreach (Condition condition in this.coTooltip.mapConds.Values)
				{
					bool flag7 = condition.nDisplayOther == 2;
					if (flag7)
					{
						bool flag8 = flag6;
						if (flag8)
						{
							str += ",";
						}
						str = str + " " + condition.strNameFriendly;
						flag6 = true;
					}
				}
				this.tooltip.transform.SetSiblingIndex(base.transform.childCount - 1);
			}
			else
			{
				this.tooltip.SetTooltip(null, 8);
			}
		}
		this.LastSelected = null;
		bool flag9 = this.nJustClickedItem < 3;
		if (flag9)
		{
			this.nJustClickedItem++;
		}
		bool flag10 = Input.GetMouseButtonUp(0) && this.bJustClickedItem;
		if (flag10)
		{
			this.bJustClickedItem = false;
			this.nJustClickedItem = 0;
		}
	}
	private float colMaxWidth = 0f;
	[MonoModIgnore]
	public static patch_GUIInventory instance;
	public GUIInventoryWindow targetWindow = null;
}
