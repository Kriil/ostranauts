using System;
using System.Linq;
using FFU_Beyond_Reach;
using UnityEngine;
// Super-settings patch for interaction speed/cost calculations.
// This module applies the configurable super-character multiplier and relaxed
// upper-limit settings during work-rate calculations.
public class patch_Interaction : Interaction
{
	private void CalcRate()
	{
		bool flag = this.strActionGroup != "Work" || this.bCTThemModifierCalculated;
		if (!flag)
		{
			this.fCTThemModifierUs = 1f;
			bool flag2 = this.strCTThemMultCondUs != null;
			if (flag2)
			{
				this.fCTThemModifierUs = (float)base.objUs.GetCondAmount(this.strCTThemMultCondUs);
				bool flag3 = FFU_BR_Defs.AllowSuperChars && FFU_BR_Defs.SuperCharacters.Length != 0 && FFU_BR_Defs.SuperCharacters.Contains(base.objUs.strName);
				if (flag3)
				{
					this.fCTThemModifierUs *= FFU_BR_Defs.SuperCharMultiplier;
				}
			}
			this.fCTThemModifierUs = Mathf.Clamp(this.fCTThemModifierUs, 1f, FFU_BR_Defs.ModifyUpperLimit ? FFU_BR_Defs.BonusUpperLimit : 10f);
			this.fCTThemModifierTools = 1f;
			bool flag4 = this.strCTThemMultCondTools != null;
			if (flag4)
			{
				this.fCTThemModifierTools = 0f;
				bool flag5 = this.aLootItemUseContract != null;
				if (flag5)
				{
					foreach (CondOwner condOwner in this.aLootItemUseContract)
					{
						bool flag6 = condOwner != null && this.strCTThemMultCondTools != null;
						if (flag6)
						{
							this.fCTThemModifierTools += (float)condOwner.GetCondAmount(this.strCTThemMultCondTools);
						}
					}
				}
			}
			this.fCTThemModifierPenalty = (float)base.objUs.GetCondAmount("StatWorkSpeedPenalty");
			bool flag7 = (double)this.fCTThemModifierPenalty > 0.99;
			if (flag7)
			{
				this.fCTThemModifierPenalty = 0.99f;
			}
			this.bCTThemModifierCalculated = true;
		}
	}
}
