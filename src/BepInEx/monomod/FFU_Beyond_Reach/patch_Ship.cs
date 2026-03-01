using System;
using FFU_Beyond_Reach;
using MonoMod;
using UnityEngine;
// Extends ship runtime/save synchronization for FFU_BR.
// Likely: this works with changesMap and smart loading so modified ship-bound
// CondOwners can be repaired or remapped without hand-editing saves.
public class patch_Ship : Ship
{
	[MonoModIgnore]
	public extern patch_Ship(GameObject go);
	public extern void orig_InitShip(bool bTemplateOnly, Ship.Loaded nLoad, string strRegIDNew = null);
	public void InitShip(bool bTemplateOnly, Ship.Loaded nLoad, string strRegIDNew = null)
	{
		bool flag = FFU_BR_Defs.ModSyncLoading && this.json != null;
		if (flag)
		{
			patch_DataHandler.SwitchSlottedItems(this.json, bTemplateOnly);
			patch_DataHandler.RecoverMissingItems(this.json);
			bool flag2 = !bTemplateOnly;
			if (flag2)
			{
				patch_DataHandler.RecoverMissingCOs(this.json);
				patch_DataHandler.SyncConditions(this.json);
				patch_DataHandler.UpdateConditions(this.json);
				patch_DataHandler.SyncSlotEffects(this.json);
				patch_DataHandler.SyncInvEffects(this.json);
			}
		}
		this.orig_InitShip(bTemplateOnly, nLoad, strRegIDNew);
	}
}
