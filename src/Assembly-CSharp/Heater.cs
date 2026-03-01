using System;
using System.Collections.Generic;
using UnityEngine;

public class Heater : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.bUpdateRemote = true;
		this.coUs = base.GetComponent<CondOwner>();
	}

	private void Update()
	{
		if (this.bUpdateRemote)
		{
			this.UpdateRemote();
		}
		if (this.coUs.ship == null || CrewSim.system == null || this.strSignalCond == null)
		{
			return;
		}
		if (this.dfEpochLastCheck <= 0.0)
		{
			this.dfEpochLastCheck = StarSystem.fEpoch;
		}
		double num = StarSystem.fEpoch - this.dfEpochLastCheck;
		if (num >= this.dfUpdateInterval)
		{
			this.Heat(num);
			this.CatchUp();
		}
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
		this.dfEpochLastCheck = StarSystem.fEpoch;
	}

	private void UpdateRemote()
	{
		Dictionary<string, string> dictionary = null;
		if (this.coUs.mapGUIPropMaps.TryGetValue(this.strCOKey, out dictionary))
		{
			dictionary.TryGetValue("strInput01", out this.strRemoteID);
			dictionary.TryGetValue("strCondMonitor01", out this.strSignalCond);
			dictionary.TryGetValue("strSubPoint", out this.strSubPoint);
			dictionary.TryGetValue("strAddPoint", out this.strAddPoint);
		}
		this.bUpdateRemote = false;
	}

	private void Heat(double fTimePassed)
	{
		bool flag = false;
		if (this.coUs.HasCond("IsOverrideOn"))
		{
			flag = true;
		}
		else if (this.coUs.HasCond("IsOverrideOff"))
		{
			flag = false;
		}
		else if (this.strRemoteID == null || this.strRemoteID == this.coUs.strID)
		{
			flag = this.coUs.HasCond(this.strSignalCond);
			CondOwner condOwner = this.coUs;
		}
		else
		{
			CondOwner cobyID = this.coUs.ship.GetCOByID(this.strRemoteID);
			if (cobyID != null && cobyID.HasCond(this.strSignalCond))
			{
				flag = true;
			}
		}
		this.coUs.mapInfo.Remove("Status");
		if (flag)
		{
			CondOwner condOwner2 = null;
			CondOwner condOwner3 = null;
			double num = 1.0;
			if (this.strAddPoint != "ignore")
			{
				List<CondOwner> list = new List<CondOwner>();
				this.coUs.ship.GetCOsAtWorldCoords1(this.coUs.GetPos(this.strAddPoint, false), this.ct, false, false, list);
				if (list.Count != 0)
				{
					if (list.Contains(this.coUs))
					{
						condOwner2 = this.coUs;
					}
					else
					{
						condOwner2 = list[0];
					}
				}
			}
			if (this.strSubPoint != "ignore")
			{
				List<CondOwner> list2 = new List<CondOwner>();
				this.coUs.ship.GetCOsAtWorldCoords1(this.coUs.GetPos(this.strSubPoint, false), this.ct, false, false, list2);
				if (list2.Count != 0)
				{
					if (list2.Contains(this.coUs))
					{
						condOwner3 = this.coUs;
					}
					else
					{
						condOwner3 = list2[0];
					}
				}
			}
			if (condOwner2 == null)
			{
				condOwner2 = condOwner3;
				num = -num;
				condOwner3 = null;
			}
			if (condOwner2 == null)
			{
				return;
			}
			string strCODef = condOwner2.strCODef;
			if (condOwner3 != null)
			{
				strCODef = condOwner3.strCODef;
			}
			double num2 = 20.7;
			double num3 = 0.9;
			GasContainer gasContainer = condOwner2.GasContainer;
			double condAmount = condOwner2.GetCondAmount("StatGasTemp");
			double condAmount2 = this.coUs.GetCondAmount("StatSolidTemp");
			double condAmount3 = this.coUs.GetCondAmount("StatHeatArea");
			double num4 = gasContainer.mapGasMols1["StatGasMolTotal"];
			if (num4 == 0.0)
			{
				num4 = double.PositiveInfinity;
			}
			double num5 = condAmount2 * condAmount2 * condAmount2 * condAmount2 - condAmount * condAmount * condAmount * condAmount;
			double num6 = num3 * 5.67E-08 * condAmount3 * num5;
			double condAmount4 = this.coUs.GetCondAmount("StatHeatVol");
			double num7 = condOwner2.GetCondAmount("StatVolume");
			if (num7 == 0.0)
			{
				num7 = double.PositiveInfinity;
			}
			double num8 = condAmount4 / num7;
			double num9 = num6 / num2 / num4 * num8 * fTimePassed * num;
			double condAmount5 = condOwner2.GetCondAmount("StatGasPressure");
			if (condAmount5 < 101.30000305175781)
			{
				num9 *= condAmount5 / 101.30000305175781;
			}
			gasContainer.fDGasTemp += num9;
			if (condOwner3 == null)
			{
				this.coUs.mapInfo["Status"] = "Heating";
				return;
			}
			num = -num;
			gasContainer = condOwner3.GasContainer;
			condAmount = condOwner3.GetCondAmount("StatGasTemp");
			num4 = gasContainer.mapGasMols1["StatGasMolTotal"];
			if (num4 == 0.0)
			{
				num4 = double.PositiveInfinity;
			}
			num7 = condOwner3.GetCondAmount("StatVolume");
			if (num7 == 0.0)
			{
				num7 = double.PositiveInfinity;
			}
			num8 = condAmount4 / num7;
			num9 = num6 / num2 / num4 * num8 * fTimePassed * num;
			condAmount5 = condOwner3.GetCondAmount("StatGasPressure");
			if (condAmount5 < 101.30000305175781)
			{
				num9 *= condAmount5 / 101.30000305175781;
			}
			gasContainer.fDGasTemp += num9;
			this.coUs.mapInfo["Status"] = "Cooling";
		}
		else
		{
			this.coUs.mapInfo["Status"] = "Idle";
		}
	}

	public void SetData(string strCT)
	{
		this.ct = DataHandler.GetCondTrigger(strCT);
	}

	private const double STEFAN_BOLTZMAN_CONSTANT = 5.67E-08;

	public bool bUpdateRemote;

	private string strRemoteID;

	private string strCOKey = "Panel A";

	private string strSignalCond;

	private string strSubPoint;

	private string strAddPoint;

	private CondOwner coUs;

	private CondTrigger ct;

	private double dfEpochLastCheck = -1.0;

	private double dfUpdateInterval = 1.0;
}
