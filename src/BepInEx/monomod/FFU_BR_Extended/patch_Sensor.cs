using System;
using System.Collections.Generic;
using MonoMod;
// Sensor command patch for FFU_BR_Extended.
// The README notes sensor updates can self-test when `strPoint` is null and can
// use a custom `dfUpdateInterval`; this patch likely implements that behavior.
public class patch_Sensor : Sensor
{
	[MonoModReplace]
	public void Run()
	{
		bool flag = this.coUs == null || this.coUs.ship == null;
		if (!flag)
		{
			bool flag2 = string.IsNullOrEmpty(this.strPoint);
			List<CondOwner> list = new List<CondOwner>();
			bool flag3 = !flag2;
			if (flag3)
			{
				this.coUs.ship.GetCOsAtWorldCoords1(this.coUs.GetPos(this.strPoint, false), null, false, false, list);
			}
			foreach (KeyValuePair<string, string> keyValuePair in this.mapTests)
			{
				CondTrigger condTrigger = DataHandler.GetCondTrigger(keyValuePair.Key);
				bool flag4 = !flag2;
				if (flag4)
				{
					foreach (CondOwner condOwner in list)
					{
						bool flag5 = condTrigger.Triggered(condOwner, null, true);
						if (flag5)
						{
							Interaction interaction = DataHandler.GetInteraction(keyValuePair.Value, null, false);
							bool flag6 = interaction != null && interaction.CTTestUs.Triggered(this.coUs, null, true);
							if (flag6)
							{
								this.coUs.QueueInteraction(condOwner, interaction, false);
							}
						}
					}
				}
				else
				{
					bool flag7 = condTrigger.Triggered(this.coUs, null, true);
					if (flag7)
					{
						Interaction interaction2 = DataHandler.GetInteraction(keyValuePair.Value, null, false);
						bool flag8 = interaction2 != null && interaction2.CTTestUs.Triggered(this.coUs, null, true);
						if (flag8)
						{
							this.coUs.QueueInteraction(this.coUs, interaction2, false);
						}
					}
				}
			}
			this.dfEpochLast = StarSystem.fEpoch;
		}
	}
	[MonoModReplace]
	public void SetData(Dictionary<string, string> gpm)
	{
		bool flag = gpm != null;
		if (flag)
		{
			this.strPoint = gpm["strPoint"];
			string[] array = gpm["mapTests"].Split(new char[]
			{
				','
			});
			string s;
			bool flag2 = gpm.TryGetValue("dfUpdateInterval", out s);
			if (flag2)
			{
				double num;
				double.TryParse(s, out num);
				bool flag3 = num > 0.0;
				if (flag3)
				{
					this.dfUpdateInterval = num;
				}
			}
			this.mapTests = DataHandler.ConvertStringArrayToDict(array, null);
		}
	}
}
