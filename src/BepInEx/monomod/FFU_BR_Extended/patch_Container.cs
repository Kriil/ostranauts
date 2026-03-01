using System;
using MonoMod;
using UnityEngine;
// Extends container/inventory rules for FFU_BR_Extended.
// This likely supports inventory-effect propagation and the extra transfer or
// slot semantics that other patches rely on.
public class patch_Container : Container
{
	[MonoModReplace]
	public bool AllowedCO(CondOwner coIn)
	{
		bool flag = coIn == null || coIn == this.CO;
		bool result;
		if (flag)
		{
			result = false;
		}
		else
		{
			CondOwner condOwner = this.CO;
			while (condOwner != null)
			{
				bool flag2 = coIn == condOwner;
				if (flag2)
				{
					return false;
				}
				condOwner = condOwner.objCOParent;
			}
			bool flag3 = this.ctAllowed != null;
			result = (!flag3 || this.ctAllowed.Triggered(coIn, null, true));
		}
		return result;
	}
	[MonoModIgnore]
	private patch_CondOwner CO
	{
		get
		{
			return (patch_CondOwner)base.CO;
		}
	}
	[MonoModReplace]
	public void SetIsInContainer(CondOwner co)
	{
		bool flag = this.CO == this;
		if (flag)
		{
			Debug.Log("ERROR: Assigning self as own parent.");
		}
		co.objCOParent = this.CO;
		bool flag2 = co.coStackHead == null;
		if (flag2)
		{
			co.tf.SetParent(this.CO.tf);
			co.tf.localPosition = new Vector3(0f, 0f, Container.fZSubOffset);
			co.Visible = false;
		}
		bool flag3 = !this.CO.HasCond("IsHuman");
		if (flag3)
		{
			bool flag4 = this.CO.jsInvSlotEffect != null;
			if (flag4)
			{
				this.ApplyContainerEffects(co, this.CO.jsInvSlotEffect, false);
			}
			co.AddCondAmount("IsInContainer", 1.0, 0.0, 0f);
		}
		CondOwner condOwner = this.CO;
		while (condOwner != null)
		{
			bool flag5 = condOwner.HasCond("IsHuman") || condOwner.HasCond("IsRobot");
			if (flag5)
			{
				co.AddCondAmount("IsCarried", 1.0, 0.0, 0f);
				co.VisitCOs(new CondOwnerVisitorAddCond
				{
					strCond = "IsCarried",
					fAmount = 1.0
				}, true);
				break;
			}
			condOwner = condOwner.objCOParent;
		}
	}
	[MonoModReplace]
	public void ClearIsInContainer(CondOwner co)
	{
		bool flag = this.CO.jsInvSlotEffect != null;
		if (flag)
		{
			this.ApplyContainerEffects(co, this.CO.jsInvSlotEffect, true);
		}
		co.ZeroCondAmount("IsInContainer");
		co.ZeroCondAmount("IsCarried");
		co.VisitCOs(new CondOwnerVisitorZeroCond
		{
			strCond = "IsCarried"
		}, true);
		co.objCOParent = null;
	}
	private void ApplyContainerEffects(CondOwner co, JsonSlotEffects jse, bool bRemove = false)
	{
		bool flag = this.CO == null || co == null || jse == null;
		if (!flag)
		{
			co.ValidateParent();
			Slots.ApplyIAEffects(this.CO, co, jse, bRemove, false);
		}
	}
}
