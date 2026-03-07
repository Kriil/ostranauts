using System;
using System.Collections.Generic;

public class DestCheck
{
	public void Destroy()
	{
		this.strDamageCond = null;
		this.strDamageCondMax = null;
		this.strLootModeSwitch = null;
	}

	public bool SetData(string[] aStrings, CondOwner co)
	{
		bool flag = false;
		if (aStrings != null)
		{
			flag = true;
			this.strDamageCond = aStrings[1];
			this.strLootModeSwitch = aStrings[2];
			this.strDamageCondMax = aStrings[3];
			flag &= double.TryParse(aStrings[4], out this.fSignalCheckPeriod);
			if (flag)
			{
				if (this.strDamageCond == "StatDamage")
				{
					co.AddCondAmount("IsDestructable", 1.0, 0.0, 0f);
				}
				else if (this.strDamageCondMax == "StatDismantleProgressMax" && !co.HasCond(this.strDamageCondMax))
				{
					PatchHandler.FixMissingDismantleMax(co);
				}
			}
		}
		return flag;
	}

	public bool DamageCheck(CondOwner co)
	{
		double condAmount = co.GetCondAmount(this.strDamageCond);
		if (condAmount >= co.GetCondAmount(this.strDamageCondMax))
		{
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			List<string> lootNames = DataHandler.GetLoot(this.strLootModeSwitch).GetLootNames(null, false, null);
			bool flag = false;
			foreach (string strName in lootNames)
			{
				Interaction interaction = DataHandler.GetInteraction(strName, null, false);
				if (interaction != null && interaction.CTTestUs.Triggered(co, null, true) && interaction.CTTestThem.Triggered(co, null, true))
				{
					flag = co.QueueInteraction(co, interaction, true);
				}
				if (flag)
				{
					if (this.strDamageCond == "StatDamage")
					{
						if (selectedCrew != null && selectedCrew.GetCORef(co) != null)
						{
							string strMsg = GrammarUtils.GenerateDescription(interaction);
							selectedCrew.LogMessage(strMsg, "Bad", co.strID);
						}
					}
					else if (this.strDamageCond == "StatRepairProgress")
					{
						co.fMSRedamageAmount = 0.8999999761581421;
						co.ZeroCondAmount("StatDamage");
					}
				}
			}
			if (flag)
			{
				co.ZeroCondAmount(this.strDamageCond);
			}
		}
		if (Math.Abs(condAmount - this._damageCondAmount) > 0.01)
		{
			this._damageCondAmount = condAmount;
			return true;
		}
		return false;
	}

	public double fSignalCheckPeriod;

	public string strLootModeSwitch;

	public string strDamageCond;

	private double _damageCondAmount;

	public string strDamageCondMax;
}
