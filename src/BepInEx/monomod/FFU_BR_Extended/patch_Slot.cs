using System;
using MonoMod;
// Extends slot runtime data for FFU_BR_Extended.
// This is the likely backing patch for `nSlotOrder`, which the mod uses to
// enforce deterministic inventory window ordering.
public class patch_Slot : Slot
{
	[MonoModIgnore]
	public extern patch_Slot(JsonSlot jslot);
	[MonoModOriginal]
	public extern void orig_Slot(JsonSlot jslot);
	[MonoModConstructor]
	public void Slot(patch_JsonSlot jslot)
	{
		this.orig_Slot(jslot);
		this.nSlotOrder = ((jslot.nSlotOrder != null) ? jslot.nSlotOrder.Value : jslot.nDepth);
	}
	[MonoModReplace]
	public bool CanFit(CondOwner coFit, bool bAuto = true, bool bSub = false)
	{
		bool flag = this.aCOs == null;
		bool result;
		if (flag)
		{
			result = false;
		}
		else
		{
			foreach (CondOwner condOwner in this.aCOs)
			{
				bool flag2 = this.bHoldSlot && condOwner == null && coFit.mapSlotEffects.ContainsKey(this.strName) && (!bAuto || base.CanAutoSlot(coFit));
				if (flag2)
				{
					return true;
				}
				CondOwner condOwner2 = condOwner;
				while (condOwner2 != null)
				{
					bool flag3 = coFit == condOwner2;
					if (flag3)
					{
						return false;
					}
					condOwner2 = condOwner2.objCOParent;
				}
				bool flag4 = condOwner != null && condOwner.objContainer != null && condOwner.objContainer.CanFit(coFit, bAuto, bSub);
				if (flag4)
				{
					return true;
				}
			}
			result = false;
		}
		return result;
	}
	public int nSlotOrder;
}
