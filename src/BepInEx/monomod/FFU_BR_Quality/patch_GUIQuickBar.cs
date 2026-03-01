using System;
using FFU_Beyond_Reach;
using UnityEngine;
// QoL patch for interaction quick-bar placement.
// This is the likely runtime hook for quick-bar pinning and the horizontal/
// vertical tweak values exposed in the FFU_BR config.
public class patch_GUIQuickBar : GUIQuickBar
{
	private extern void orig_Start();
	private void Start()
	{
		this.QuickBarOverride();
		this.orig_Start();
	}
	private extern void orig_ExpandCollapse(bool refreshSizeOnly = false);
	private void ExpandCollapse(bool refreshSizeOnly = false)
	{
		this.QuickBarOverride();
		this.orig_ExpandCollapse(refreshSizeOnly);
	}
	private void QuickBarOverride()
	{
		bool flag = !FFU_BR_Defs.QuickBarPinning;
		if (!flag)
		{
			bool flag2 = FFU_BR_Defs.QuickBarTweaks.Length != 3;
			if (!flag2)
			{
				this._pinPosition = new Vector3(FFU_BR_Defs.QuickBarTweaks[0], FFU_BR_Defs.QuickBarTweaks[1], 0f);
				this.bExpanded = (FFU_BR_Defs.QuickBarTweaks[2] == 1f);
			}
		}
	}
}
