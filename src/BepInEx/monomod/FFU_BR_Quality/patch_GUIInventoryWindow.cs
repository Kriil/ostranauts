using System;
using FFU_Beyond_Reach;
using UnityEngine.UI;
// QoL patch for per-window inventory placement and ordering.
// Likely: this works with the organized inventory settings and last-target
// transfer behavior so item windows stack predictably.
public class patch_GUIInventoryWindow : GUIInventoryWindow
{
	public extern void orig_Pin(bool bPin, bool bRespawn);
	public void Pin(bool bPin, bool bRespawn = false)
	{
		bool flag = !FFU_BR_Defs.OrgInventoryMode || FFU_BR_Defs.OrgInventoryTweaks.Length != 4;
		if (flag)
		{
			this.orig_Pin(bPin, bRespawn);
		}
	}
	public extern void orig_SetData(CondOwner co, InventoryWindowType ttype);
	public void SetData(CondOwner co, InventoryWindowType ttype)
	{
		this.orig_SetData(co, ttype);
		bool flag = FFU_BR_Defs.OrgInventoryMode && FFU_BR_Defs.OrgInventoryTweaks.Length == 4;
		if (flag)
		{
			Button component = base.transform.Find("Tab Background/PinButton").GetComponent<Button>();
			bool flag2 = component != null;
			if (flag2)
			{
				component.gameObject.SetActive(false);
			}
		}
	}
}
