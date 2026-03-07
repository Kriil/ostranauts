using FFU_Beyond_Reach;
using MonoMod;
using UnityEngine;
using UnityEngine.UI;

public partial class patch_GUIInventory : GUIInventory {
    private float colMaxWidth = 0;
    private extern Vector2 orig_GetWindowPosition(GUIInventoryWindow winCurrent, GUIInventoryWindow winPrev);
    private Vector2 GetWindowPosition(GUIInventoryWindow winCurrent, GUIInventoryWindow winPrev) {
        if (!FFU_BR_Defs.OrgInventoryMode || FFU_BR_Defs.OrgInventoryTweaks.Length != 4) return orig_GetWindowPosition(winCurrent, winPrev);
        float wScale = 1f / (1.5f * CanvasManager.CanvasRatio);
        float xOffset = 0f;
        float yOffset = 0f;
        float yAnchor = 0f;
        if (rectItmAnchor != null) {
            xOffset = wScale * rectItmAnchor.localPosition.x;
            yAnchor = wScale * (rectItmAnchor.localPosition.y + FFU_BR_Defs.OrgInventoryTweaks[0]);
        } else {
            xOffset = wScale * (rectPaperDoll.localPosition.x + rectPaperDoll.rect.width / 2f + 5f);
            yAnchor = wScale * (rectPaperDoll.localPosition.y + rectPaperDoll.rect.height / 2f + FFU_BR_Defs.OrgInventoryTweaks[0]);
        }
        yOffset = yAnchor;
        if (winPrev != null) {
            xOffset = winPrev.transform.localPosition.x * wScale;
            yOffset = (winPrev.transform.localPosition.y - winPrev.gridBorderRect.rect.height - winPrev.tabImage.rectTransform.rect.height - 5f - FFU_BR_Defs.OrgInventoryTweaks[3]) * wScale;
            float winHeight = (winCurrent.gridBorderRect.rect.height + winCurrent.tabImage.rectTransform.rect.height) * wScale;
            float prevMaxWidth = (Mathf.Max(winPrev.tabImage.rectTransform.rect.width, winPrev.gridBorderRect.rect.width) + 5.5f + FFU_BR_Defs.OrgInventoryTweaks[2]) * wScale;
            if (prevMaxWidth > colMaxWidth) colMaxWidth = prevMaxWidth;
            float yOffsetExpected = yOffset - winHeight;
            float yOffsetLimit = -yAnchor - FFU_BR_Defs.OrgInventoryTweaks[1];
            if (FFU_BR_Defs.ActLogging >= FFU_BR_Defs.ActLogs.Runtime) Debug.Log($"#Info# Current Window: {winCurrent.CO.strCODef}, " +
                $"Previous Window: {winPrev.CO.strCODef}, yOffset: {yOffset}, winHeight: {winHeight}, yAnchor: {yAnchor}, " +
                $"prevMaxWidth: {prevMaxWidth}, yOffsetExpected: {yOffsetExpected}, yOffsetLimit: {yOffsetLimit}");
            if (yOffsetExpected < yOffsetLimit) {
                yOffset = yAnchor;
                xOffset += colMaxWidth;
                colMaxWidth = 0f;
            }
        } else colMaxWidth = 0;
        return new Vector2(xOffset, yOffset);
    }

    [MonoModReplace] private void Update() {
        bLastMouseInInv = false;
        if (canvasGroup.alpha != 0f) {
            bLastMouseInInv = MouseOverInventory(Input.mousePosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectInv, Input.mousePosition, CrewSim.objInstance.UICamera, out var localPoint);
            localPoint.x = Mathf.Clamp(localPoint.x, rectDragBounds.rect.xMin + rectDragBounds.localPosition.x, rectDragBounds.rect.xMax + rectDragBounds.localPosition.x);
            localPoint.y = Mathf.Clamp(localPoint.y, rectDragBounds.rect.yMin + rectDragBounds.localPosition.y, rectDragBounds.rect.yMax + rectDragBounds.localPosition.y);
            if (Selected != null) {
                Selected.transform.localPosition =
                    new Vector3((Input.mousePosition.x - (Screen.width / 2f)) *
                    (1280f / Screen.width * CrewSim.objInstance.AspectRatioMod()),
                    (Input.mousePosition.y - (Screen.height / 2f)) * 720f / Screen.height, 0f);
                CanvasManager.SetAnchorsToCorners(Selected.transform);
                if (bLastMouseInInv) Selected.cgSelf.alpha = 1f;
                else Selected.cgSelf.alpha = 0f;
            }
            if (selectedTab != null && (!FFU_BR_Defs.OrgInventoryMode || FFU_BR_Defs.OrgInventoryTweaks.Length != 4)) {
                selectedTab.transform.parent.localPosition = new Vector3(localPoint.x - mouseOffset.x, localPoint.y - mouseOffset.y, 0f);
                CanvasManager.SetAnchorsToCorners(selectedTab.transform.parent.GetComponent<RectTransform>());
            }
            for (int refIdx = activeWindows.Count - 1; refIdx >= 0; refIdx--) activeWindows[refIdx].UpdateWindow(CODoll);
            if (coTooltip != null && Selected == null) {
                tooltip.SetTooltip(coTooltip, GUITooltip.TooltipWindow.Inventory);
                CrewSim.objInstance.tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
                string strName = coTooltip.FriendlyName;
                if (coTooltip.StackCount > 1) strName += "(x" + coTooltip.StackCount + ")";
                strName += "\n";
                bool isNotFirst = false;
                foreach (Condition refCond in coTooltip.mapConds.Values) {
                    if (refCond.nDisplayOther == 2) {
                        if (isNotFirst) strName += ",";
                        strName += " " + refCond.strNameFriendly;
                        isNotFirst = true;
                    }
                }
                tooltip.transform.SetSiblingIndex(base.transform.childCount - 1);
            } else tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
        }
        LastSelected = null;
        if (nJustClickedItem < 3) nJustClickedItem++;
        if (Input.GetMouseButtonUp(0) && bJustClickedItem) {
            bJustClickedItem = false;
            nJustClickedItem = 0;
        }
    }
}

public partial class patch_GUIInventoryWindow : GUIInventoryWindow {
    public extern void orig_Pin(bool bPin, bool bRespawn);
    public void Pin(bool bPin, bool bRespawn = false) {
        if (!FFU_BR_Defs.OrgInventoryMode || FFU_BR_Defs.OrgInventoryTweaks.Length != 4) orig_Pin(bPin, bRespawn);
    }
    public extern void orig_SetData(CondOwner co, InventoryWindowType ttype);
    public void SetData(CondOwner co, InventoryWindowType ttype) {
        orig_SetData(co, ttype);
        if (FFU_BR_Defs.OrgInventoryMode && FFU_BR_Defs.OrgInventoryTweaks.Length == 4) {
            Button btnPin = base.transform.Find("Tab Background/PinButton").GetComponent<Button>();
            if (btnPin != null) btnPin.gameObject.SetActive(false);
        }
    }
}

// Reference Output: ILSpy v9.1.0.7988 / C# 12.0 / 2022.8

/* GUIInventory.GetWindowPosition
private Vector2 GetWindowPosition(GUIInventoryWindow winCurrent, GUIInventoryWindow winPrev)
{
	string gPMInfo = winCurrent.CO.GetGPMInfo("GUIInv", "TabPos");
	if (!string.IsNullOrEmpty(gPMInfo))
	{
		Vector3 vector = default(Vector3);
		string[] array = gPMInfo.Split('|');
		if (array.Length == 2)
		{
			float result = 0f;
			if (float.TryParse(array[0], out result))
			{
				vector.x = result;
			}
			if (float.TryParse(array[1], out result))
			{
				vector.y = result;
			}
			return vector;
		}
	}
	float num = 1f / (1.5f * CanvasManager.CanvasRatio);
	float num2 = 0f;
	float num3 = 0f;
	float num4 = 0f;
	float num5 = 0f;
	if (rectItmAnchor != null)
	{
		num2 = num * rectItmAnchor.localPosition.x;
		num5 = num * rectItmAnchor.localPosition.y;
	}
	else
	{
		num2 = num * (rectPaperDoll.localPosition.x + rectPaperDoll.rect.width / 2f + 5f);
		num5 = num * (rectPaperDoll.localPosition.y + rectPaperDoll.rect.height / 2f);
	}
	num4 = num5;
	num3 = num4;
	if (winPrev != null)
	{
		num2 = winPrev.transform.localPosition.x * num;
		num3 = winPrev.transform.localPosition.y - winPrev.gridBorderRect.rect.height - winPrev.tabImage.rectTransform.rect.height * 1.7f;
		num3 *= num;
		float num6 = winCurrent.gridBorderRect.rect.height + winCurrent.tabImage.rectTransform.rect.height * 1.7f;
		if (num3 - num6 < num4 - num5 * 2f)
		{
			num3 = num4;
			num2 += 100f;
		}
	}
	return new Vector2(num2, num3);
}
*/

/* GUIInventory.Update
private void Update()
{
	bLastMouseInInv = false;
	if (canvasGroup.alpha != 0f)
	{
		bLastMouseInInv = MouseOverInventory(Input.mousePosition);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(rectInv, Input.mousePosition, CrewSim.objInstance.UICamera, out var localPoint);
		localPoint.x = Mathf.Clamp(localPoint.x, rectDragBounds.rect.xMin + rectDragBounds.localPosition.x, rectDragBounds.rect.xMax + rectDragBounds.localPosition.x);
		localPoint.y = Mathf.Clamp(localPoint.y, rectDragBounds.rect.yMin + rectDragBounds.localPosition.y, rectDragBounds.rect.yMax + rectDragBounds.localPosition.y);
		if (Selected != null)
		{
			Selected.transform.localPosition = new Vector3((Input.mousePosition.x - (float)(Screen.width / 2)) * (1280f / (float)Screen.width * CrewSim.objInstance.AspectRatioMod()), (Input.mousePosition.y - (float)(Screen.height / 2)) * 720f / (float)Screen.height, 0f);
			CanvasManager.SetAnchorsToCorners(Selected.transform);
			if (bLastMouseInInv)
			{
				Selected.cgSelf.alpha = 1f;
			}
			else
			{
				Selected.cgSelf.alpha = 0f;
			}
		}
		if (selectedTab != null)
		{
			selectedTab.transform.parent.localPosition = new Vector3(localPoint.x - mouseOffset.x, localPoint.y - mouseOffset.y, 0f);
			CanvasManager.SetAnchorsToCorners(selectedTab.transform.parent.GetComponent<RectTransform>());
		}
		for (int refIdx = activeWindows.Count - 1; refIdx >= 0; refIdx--)
		{
			activeWindows[refIdx].UpdateWindow(CODoll);
		}
		if (coTooltip != null && Selected == null)
		{
			tooltip.SetTooltip(coTooltip, GUITooltip.TooltipWindow.Inventory);
			CrewSim.objInstance.tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
			string strName = coTooltip.FriendlyName;
			if (coTooltip.StackCount > 1)
			{
				string text2 = strName;
				strName = text2 + "(x" + coTooltip.StackCount + ")";
			}
			strName += "\n";
			bool isNotFirst = false;
			foreach (Condition refCond in coTooltip.mapConds.Values)
			{
				if (refCond.nDisplayOther == 2)
				{
					if (isNotFirst)
					{
						strName += ",";
					}
					strName = strName + " " + refCond.strNameFriendly;
					isNotFirst = true;
				}
			}
			tooltip.transform.SetSiblingIndex(base.transform.childCount - 1);
		}
		else
		{
			tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
		}
	}
	LastSelected = null;
	if (nJustClickedItem < 3)
	{
		nJustClickedItem++;
	}
	if (Input.GetMouseButtonUp(0) && bJustClickedItem)
	{
		bJustClickedItem = false;
		nJustClickedItem = 0;
	}
}
*/