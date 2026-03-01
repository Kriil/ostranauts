using System;
using System.Collections.Generic;
using MonoMod;
public class patch_Heater : Heater
{
	[MonoModReplace]
	private void Heat(double fTimePassed)
	{
		bool flag = false;
		bool flag2 = this.coUs.HasCond("IsOverrideOn");
		if (flag2)
		{
			flag = true;
		}
		else
		{
			bool flag3 = this.coUs.HasCond("IsOverrideOff");
			if (flag3)
			{
				flag = false;
			}
			else
			{
				bool flag4 = this.strRemoteID == null || this.strRemoteID == this.coUs.strID;
				if (flag4)
				{
					flag = this.coUs.HasCond(this.strSignalCond);
					CondOwner coUs = this.coUs;
				}
				else
				{
					CondOwner cobyID = this.coUs.ship.GetCOByID(this.strRemoteID);
					bool flag5 = cobyID != null && cobyID.HasCond(this.strSignalCond);
					if (flag5)
					{
						flag = true;
					}
				}
			}
		}
		this.coUs.mapInfo.Remove("Status");
		bool flag6 = flag;
		if (flag6)
		{
			CondOwner condOwner = null;
			CondOwner condOwner2 = null;
			double num = 1.0;
			bool flag7 = this.strAddPoint != "ignore";
			if (flag7)
			{
				List<CondOwner> list = new List<CondOwner>();
				this.coUs.ship.GetCOsAtWorldCoords1(this.coUs.GetPos(this.strAddPoint, false), this.ct, false, false, list);
				bool flag8 = list.Count != 0;
				if (flag8)
				{
					condOwner = ((!list.Contains(this.coUs)) ? list[0] : this.coUs);
				}
			}
			bool flag9 = this.strSubPoint != "ignore";
			if (flag9)
			{
				List<CondOwner> list2 = new List<CondOwner>();
				this.coUs.ship.GetCOsAtWorldCoords1(this.coUs.GetPos(this.strSubPoint, false), this.ct, false, false, list2);
				bool flag10 = list2.Count != 0;
				if (flag10)
				{
					condOwner2 = ((!list2.Contains(this.coUs)) ? list2[0] : this.coUs);
				}
			}
			bool flag11 = condOwner == null;
			if (flag11)
			{
				condOwner = condOwner2;
				num = 0.0 - num;
				condOwner2 = null;
			}
			bool flag12 = condOwner == null;
			if (!flag12)
			{
				string strCODef = condOwner.strCODef;
				bool flag13 = condOwner2 != null;
				if (flag13)
				{
					strCODef = condOwner2.strCODef;
				}
				double num2 = 20.7;
				double num3 = 0.9;
				double num4 = 5.67E-08;
				double num5 = 101.30000305175781;
				GasContainer gasContainer = condOwner.GasContainer;
				double condAmount = condOwner.GetCondAmount("StatGasTemp");
				double condAmount2 = this.coUs.GetCondAmount("StatEmittedTemp");
				bool flag14 = condAmount2 == 0.0;
				if (flag14)
				{
					condAmount2 = this.coUs.GetCondAmount("StatSolidTemp");
				}
				double condAmount3 = this.coUs.GetCondAmount("StatHeatArea");
				double num6 = gasContainer.mapGasMols1["StatGasMolTotal"];
				bool flag15 = num6 == 0.0;
				if (flag15)
				{
					num6 = double.PositiveInfinity;
				}
				double num7 = condAmount2 * condAmount2 * condAmount2 * condAmount2 - condAmount * condAmount * condAmount * condAmount;
				double num8 = num3 * num4 * condAmount3 * num7;
				double condAmount4 = this.coUs.GetCondAmount("StatHeatVol");
				double num9 = condOwner.GetCondAmount("StatVolume");
				bool flag16 = num9 == 0.0;
				if (flag16)
				{
					num9 = double.PositiveInfinity;
				}
				double num10 = condAmount4 / num9;
				double num11 = num8 / num2 / num6 * num10 * fTimePassed * num;
				double condAmount5 = condOwner.GetCondAmount("StatGasPressure");
				bool flag17 = condAmount5 < num5;
				if (flag17)
				{
					num11 *= condAmount5 / num5;
				}
				gasContainer.fDGasTemp += num11;
				bool flag18 = condOwner2 == null;
				if (flag18)
				{
					this.coUs.mapInfo["Status"] = "Heating";
				}
				else
				{
					num = 0.0 - num;
					gasContainer = condOwner2.GasContainer;
					condAmount = condOwner2.GetCondAmount("StatGasTemp");
					num6 = gasContainer.mapGasMols1["StatGasMolTotal"];
					bool flag19 = num6 == 0.0;
					if (flag19)
					{
						num6 = double.PositiveInfinity;
					}
					num9 = condOwner2.GetCondAmount("StatVolume");
					bool flag20 = num9 == 0.0;
					if (flag20)
					{
						num9 = double.PositiveInfinity;
					}
					num10 = condAmount4 / num9;
					num11 = num8 / num2 / num6 * num10 * fTimePassed * num;
					condAmount5 = condOwner2.GetCondAmount("StatGasPressure");
					bool flag21 = condAmount5 < num5;
					if (flag21)
					{
						num11 *= condAmount5 / num5;
					}
					gasContainer.fDGasTemp += num11;
					this.coUs.mapInfo["Status"] = "Cooling";
				}
			}
		}
		else
		{
			this.coUs.mapInfo["Status"] = "Idle";
		}
	}
}
