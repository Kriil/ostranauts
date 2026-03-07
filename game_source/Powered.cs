using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.Utils;
using UnityEngine;

// Power simulation component for devices, batteries, lights, and tools. This
// appears to consume/recharge `StatPower`, manage low-power behavior, and drive
// flicker/UI state for powered items.
public class Powered : MonoBehaviour
{
	// Tracks recent power usage so UI or diagnostics can summarize drain/charge.
	public PowerUsageRecorder PowerUsageRecorder { get; set; }

	// Staggers the first update and refreshes the global user-settings-based
	// flicker chance used by damaged powered lights.
	private void Start()
	{
		this.fUpdateLast = StarSystem.fEpoch + (double)((float)UnityEngine.Random.Range(0, 100) * 0.01f);
		Powered.UpdateBaseFlickerAmount();
	}

	// Main runtime loop: runs the coarse power sim once per second, applies light
	// flicker, and warns the owning human when stored power drops below 5%.
	private void Update()
	{
		if (StarSystem.fEpoch - this.fUpdateLast >= 1.0)
		{
			this.fUpdateLast = StarSystem.fEpoch;
			this.Run();
		}
		if (!CrewSim.Paused && MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < (double)Powered._flickerChance && this.itm != null && this.itm.aLights.Count > 0 && (this.fDamageAmount >= Powered.fMinDamage || (double)this.itm.fFlickerAmount < 1.0))
		{
			float fFlickerAmount = 1f;
			float num = (float)((1.0 - this.fDamageAmount * this.fDamageAmount) / (1.0 - Powered.fMinDamage * Powered.fMinDamage));
			if (num > 1f)
			{
				num = 1f;
			}
			if (this.itm.fFlickerAmount == 0f)
			{
				if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < (double)num)
				{
					fFlickerAmount = num;
				}
			}
			else if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) >= (double)num)
			{
				fFlickerAmount = 0f;
			}
			this.itm.fFlickerAmount = fFlickerAmount;
			foreach (Visibility visibility in this.itm.aLights)
			{
				visibility.fFlickerAmount = fFlickerAmount;
			}
		}
		if (StarSystem.fEpoch - this.fMinorUpdate < 0.30000001192092896)
		{
			return;
		}
		this.fMinorUpdate = StarSystem.fEpoch;
		double condAmount = this.CO.GetCondAmount("StatPower");
		double powerStoredMax = this.PowerStoredMax;
		if (powerStoredMax == 0.0 || condAmount / powerStoredMax < 0.05)
		{
			if (!this.isOver5Percent)
			{
				return;
			}
			CondOwner condOwner = this.CO.RootParent(null);
			if (condOwner != null)
			{
				int num2 = 10;
				bool flag = true;
				CondOwner condOwner2 = condOwner;
				while (flag && num2 > 0)
				{
					if (condOwner2.HasCond("IsHuman"))
					{
						condOwner2.LogMessage(this.CO.strNameFriendly + " is low on power.", "Bad", this.CO.strID);
						flag = false;
					}
					else if (condOwner2.objCOParent == null)
					{
						flag = false;
					}
					else
					{
						condOwner2 = condOwner2.objCOParent;
						num2--;
					}
				}
			}
			this.isOver5Percent = false;
		}
		else
		{
			this.isOver5Percent = true;
			if (condAmount > powerStoredMax)
			{
				this.ResetCurrentToMaxPower();
			}
		}
	}

	// Reads the user settings flicker level and converts it into the shared
	// random flicker chance for damaged powered items.
	public static void UpdateBaseFlickerAmount()
	{
		int nFlickerAmount = DataHandler.GetUserSettings().nFlickerAmount;
		if (nFlickerAmount < 0)
		{
			Powered._flickerChance = 0f;
		}
		else if (nFlickerAmount == 1)
		{
			Powered._flickerChance = 0.025f;
		}
		else
		{
			Powered._flickerChance = 0.2f;
		}
	}

	// Core power tick: recharges if allowed, shuts down on override/signal loss,
	// spends power when active, and refreshes any bound power UI.
	private void Run()
	{
		CondOwner condOwner = this.CO;
		if (condOwner == null)
		{
			Debug.LogError("ERROR: Running code on null CO.");
			return;
		}
		if (condOwner.ship != null)
		{
			if (this.ctRecharge != null && this.ctRecharge.Triggered(condOwner, null, true))
			{
				this.Recharge();
			}
			if (condOwner.HasCond("IsOverrideOff") || condOwner.HasCond("IsSignalOff"))
			{
				this.ShutDown(condOwner);
			}
			else if (this.bUsesPower && this.ctUsePower.Triggered(condOwner, null, true))
			{
				this.UsePower(condOwner, this.jsonPI.fAmount);
			}
			this.UpdatePowerUI();
		}
		if (this.bDebug)
		{
			Powered.debugTimesRunThisFrame++;
			Powered.bOutput = true;
		}
	}

	// Debug-only output summarizing how many Powered ticks ran this frame.
	private void LateUpdate()
	{
		if (Powered.bOutput)
		{
			Debug.Log("Run Powered.Run() " + Powered.debugTimesRunThisFrame + " times this frame");
			Powered.bOutput = false;
			Powered.debugTimesRunThisFrame = 0;
		}
	}

	// External helper for systems that want to request a one-off power draw.
	public void UserPowerExt(double fAmount)
	{
		this.UsePower(this.CO, fAmount);
	}

	// Drains power from connected inputs, slotted/internal power sources, and
	// finally local stored power on the owning CondOwner.
	private void UsePower(CondOwner coUs, double fAmount)
	{
		this.fPowerConnected = 0.0;
		double num = fAmount;
		double condAmount = coUs.GetCondAmount("StatPower");
		List<CondOwner> list = new List<CondOwner>();
		if (this.jsonPI.bAllowExtPower && num > 0.0)
		{
			for (int i = 0; i < this.jsonPI.aInputPts.Length; i++)
			{
				Vector2 pos = coUs.GetPos(this.jsonPI.aInputPts[i], false);
				Tile tileAtWorldCoords = coUs.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
				if (!(tileAtWorldCoords == null))
				{
					foreach (Powered powered in tileAtWorldCoords.aConnectedPowerCOs)
					{
						if (!(powered == null) && !(powered.CO == null))
						{
							list.Add(powered.CO);
						}
					}
				}
			}
			num = this.GatherPower(num, list);
			this.fPowerConnected += this.QueryPower(list);
		}
		if (num > 0.0)
		{
			List<CondOwner> cos = coUs.GetCOs(false, this.ctPowerSource);
			if (cos != null)
			{
				num = this.GatherPower(num, cos);
			}
		}
		if (num > 0.0)
		{
			if (condAmount >= fAmount)
			{
				num = 0.0;
				coUs.AddCondAmount("StatPower", -fAmount, 0.0, 0f);
				this.fPowerConnected += coUs.GetCondAmount("StatPower");
				if (this.PowerUsageRecorder == null)
				{
					this.PowerUsageRecorder = new PowerUsageRecorder();
				}
				this.PowerUsageRecorder.RecordChange(-fAmount);
			}
			else
			{
				num -= condAmount;
				coUs.AddCondAmount("StatPower", -condAmount, 0.0, 0f);
				this.fPowerConnected += coUs.GetCondAmount("StatPower");
				if (this.PowerUsageRecorder == null)
				{
					this.PowerUsageRecorder = new PowerUsageRecorder();
				}
				this.PowerUsageRecorder.RecordChange(-condAmount);
			}
		}
		if (num <= 0.0 && this.jsonPI.strIntPowerOn != null)
		{
			if (!coUs.HasCond("IsPowered"))
			{
				coUs.SetCondAmount("IsPowered", 1.0, 0.0);
				Interaction interaction = DataHandler.GetInteraction(this.jsonPI.strIntPowerOn, null, true);
				if (interaction != null && interaction.CTTestUs.Triggered(coUs, null, true) && interaction.CTTestThem.Triggered(coUs, null, true))
				{
					coUs.QueueInteraction(coUs, interaction, true);
				}
				else
				{
					DataHandler.ReleaseTrackedInteraction(interaction);
				}
			}
			this.fDamageAmount = (double)coUs.GetDamage();
		}
		else if (num > 0.0 && this.jsonPI.strIntPowerOff != null)
		{
			this.ShutDown(coUs);
		}
		if (num <= 0.0 && list.Count > 0 && coUs.HasCond("IsRechargingContainer"))
		{
			double num2 = this.fPowerConnected;
			List<CondOwner> cos2 = coUs.GetCOs(true, this.ctPowerSource);
			if (cos2 != null)
			{
				foreach (CondOwner condOwner in cos2)
				{
					if (!(condOwner == null))
					{
						Powered pwr = condOwner.Pwr;
						if (!(pwr == null))
						{
							double powerRechargeAmount = pwr.PowerRechargeAmount;
							if (powerRechargeAmount > 0.0)
							{
								if (num2 >= powerRechargeAmount)
								{
									pwr.CO.AddCondAmount("StatPower", powerRechargeAmount, 0.0, 0f);
									if (pwr.PowerUsageRecorder == null)
									{
										pwr.PowerUsageRecorder = new PowerUsageRecorder();
									}
									pwr.PowerUsageRecorder.RecordChange(powerRechargeAmount);
									num2 -= powerRechargeAmount;
								}
								else
								{
									pwr.CO.AddCondAmount("StatPower", num2, 0.0, 0f);
									if (pwr.PowerUsageRecorder == null)
									{
										pwr.PowerUsageRecorder = new PowerUsageRecorder();
									}
									pwr.PowerUsageRecorder.RecordChange(num2);
									num2 = 0.0;
								}
								if (num2 <= 0.0)
								{
									break;
								}
							}
						}
					}
				}
			}
			if (num2 < this.fPowerConnected)
			{
				this.GatherPower(this.fPowerConnected - num2, list);
				this.fPowerConnected = num2;
			}
		}
	}

	private void ShutDown(CondOwner coUs)
	{
		if (!coUs.HasCond("IsPowered"))
		{
			return;
		}
		coUs.ZeroCondAmount("IsPowered");
		CondOwner condOwner = coUs.RootParent(null);
		if (condOwner != null)
		{
			int num = 10;
			bool flag = true;
			CondOwner condOwner2 = condOwner;
			while (flag && num > 0)
			{
				if (condOwner2.objCOParent == null)
				{
					flag = false;
				}
				else
				{
					condOwner2 = condOwner2.objCOParent;
					if (condOwner2.HasCond("IsHuman"))
					{
						condOwner2.LogMessage(coUs.strNameFriendly + " powers off.", "Neutral", coUs.strID);
						flag = false;
					}
					else
					{
						num--;
					}
				}
			}
		}
		Interaction interaction = DataHandler.GetInteraction(this.jsonPI.strIntPowerOff, null, false);
		if (interaction != null && interaction.CTTestUs.Triggered(coUs, null, true) && interaction.CTTestThem.Triggered(coUs, null, true))
		{
			coUs.QueueInteraction(coUs, interaction, true);
		}
		if (coUs.HasCond("IsPowerObjective"))
		{
			MonoSingleton<ObjectiveTracker>.Instance.AddObjective(new AlarmObjective(AlarmType.nav_power, coUs, "Unpowered devices", "TIsPowered", true, coUs.ship.strRegID));
		}
	}

	private void Recharge()
	{
		if (this.CO == null)
		{
			Debug.LogError("ERROR: Running code on null CO.");
			return;
		}
		Dictionary<Powered, double> dictionary = new Dictionary<Powered, double>();
		double num = -1.0;
		double num2 = 0.0;
		for (int i = 0; i < this.jsonPI.aInputPts.Length + 1; i++)
		{
			Vector2 vector = Vector2.zero;
			if (i < this.jsonPI.aInputPts.Length)
			{
				vector = this.CO.GetPos(this.jsonPI.aInputPts[i], false);
			}
			else
			{
				if (this.CO.mapPoints == null || !this.CO.mapPoints.ContainsKey("PowerOutput"))
				{
					break;
				}
				vector = this.CO.GetPos("PowerOutput", false);
			}
			Tile tileAtWorldCoords = this.CO.ship.GetTileAtWorldCoords1(vector.x, vector.y, true, true);
			if (!(tileAtWorldCoords == null))
			{
				foreach (Powered powered in tileAtWorldCoords.aConnectedPowerCOs)
				{
					if (!(powered == null) && !(powered.CO == null))
					{
						if (powered.CO.HasCond("IsPowerStorage"))
						{
							double powerRechargeAmount = powered.PowerRechargeAmount;
							num2 += powerRechargeAmount;
							if (powerRechargeAmount > 0.0)
							{
								if (!dictionary.ContainsKey(powered))
								{
									dictionary.Add(powered, powerRechargeAmount);
								}
								if (num < 0.0 || powerRechargeAmount < num)
								{
									num = powerRechargeAmount;
								}
							}
						}
					}
				}
			}
		}
		double num3 = this.CO.GetCondAmount("StatPower");
		if (num2 > 0.0)
		{
			if (num3 >= num2)
			{
				foreach (Powered powered2 in dictionary.Keys)
				{
					powered2.CO.AddCondAmount("StatPower", dictionary[powered2], 0.0, 0f);
					if (powered2.PowerUsageRecorder == null)
					{
						powered2.PowerUsageRecorder = new PowerUsageRecorder();
					}
					powered2.PowerUsageRecorder.RecordChange(dictionary[powered2]);
					num3 -= dictionary[powered2];
				}
			}
			else
			{
				double num4 = num;
				num = -1.0;
				List<Powered> list = new List<Powered>();
				while (num3 > 0.0)
				{
					List<Powered> list2 = new List<Powered>(dictionary.Keys);
					foreach (Powered powered3 in list2)
					{
						powered3.CO.AddCondAmount("StatPower", num4, 0.0, 0f);
						if (powered3.PowerUsageRecorder == null)
						{
							powered3.PowerUsageRecorder = new PowerUsageRecorder();
						}
						powered3.PowerUsageRecorder.RecordChange(num4);
						Dictionary<Powered, double> dictionary2;
						Powered key;
						(dictionary2 = dictionary)[key = powered3] = dictionary2[key] - num4;
						if (dictionary[powered3] <= 0.0)
						{
							list.Add(powered3);
						}
						else if (num < 0.0 || dictionary[powered3] < num)
						{
							num = dictionary[powered3];
						}
						num3 -= num4;
					}
					list2.Clear();
					while (list.Count > 0)
					{
						dictionary.Remove(list[0]);
						list.Remove(list[0]);
					}
					if (num < 0.0)
					{
						list2 = null;
						break;
					}
					if (num * (double)dictionary.Keys.Count <= num3)
					{
						num4 = num;
						num = -1.0;
					}
					else
					{
						num = num3 / (double)dictionary.Keys.Count;
					}
					list2 = null;
					if (num3 == 0.0)
					{
						break;
					}
				}
				list.Clear();
				list = null;
			}
			dictionary.Clear();
			dictionary = null;
		}
		this.fPowerLast = num3;
		this.CO.SetCondAmount("StatPower", this.fPowerLast, 0.0);
	}

	private double GatherPower(double fPowerNeeded, List<CondOwner> aCOs)
	{
		if (aCOs == null)
		{
			Debug.LogError("ERROR: Gathering power from null list.");
			return 0.0;
		}
		Powered.mapPwrs.Clear();
		foreach (CondOwner condOwner in aCOs.Distinct<CondOwner>())
		{
			Powered pwr = condOwner.Pwr;
			if (!(pwr == null) && !(pwr.CO == null))
			{
				CondOwner condOwner2 = pwr.co;
				double condAmount = condOwner2.GetCondAmount("StatPower");
				if (condAmount > 0.0)
				{
					if (condOwner2.HasCond("IsPowerGen"))
					{
						double num = fPowerNeeded;
						if (fPowerNeeded > condAmount)
						{
							num = condAmount;
						}
						pwr.TransmitPower(num, 0);
						fPowerNeeded -= num;
						if (fPowerNeeded <= 0.0)
						{
							break;
						}
					}
					else
					{
						Powered.mapPwrs.Add(pwr, -condAmount);
					}
				}
			}
		}
		if (fPowerNeeded <= 0.0)
		{
			return fPowerNeeded;
		}
		foreach (KeyValuePair<Powered, double> keyValuePair in from key in Powered.mapPwrs
		orderby key.Value
		select key)
		{
			double num2 = -keyValuePair.Value;
			Powered key2 = keyValuePair.Key;
			double num3 = key2.TransmitPower(fPowerNeeded, 0);
			if (num2 - num3 <= 0.0)
			{
				CondOwner condOwner3 = this.co.RootParent(null);
				if (condOwner3 != null)
				{
					condOwner3.LogMessage(key2.co.strNameFriendly + " no longer has power!", "Bad", key2.co.strID);
				}
			}
			fPowerNeeded -= num3;
			if (fPowerNeeded <= 0.0)
			{
				return fPowerNeeded;
			}
		}
		return fPowerNeeded;
	}

	private double TransmitPower(double fAmount, int nDepth)
	{
		if (this.CO == null)
		{
			Debug.LogError("ERROR: Running code on null CO.");
			return 0.0;
		}
		double num = 0.0;
		double num2 = 0.0;
		if (this.CO.GetCondAmount("StatPower") >= fAmount)
		{
			num = fAmount;
			this.CO.AddCondAmount("StatPower", -fAmount, 0.0, 0f);
		}
		else
		{
			num += this.CO.GetCondAmount("StatPower");
			this.CO.AddCondAmount("StatPower", -num, 0.0, 0f);
			this.bNoPowerThisLoop = true;
			nDepth++;
			for (int i = 0; i < this.jsonPI.aInputPts.Length; i++)
			{
				Vector2 pos = this.CO.GetPos(this.jsonPI.aInputPts[i], false);
				Tile tileAtWorldCoords = this.CO.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
				if (!(tileAtWorldCoords == null))
				{
					foreach (Powered powered in tileAtWorldCoords.aConnectedPowerCOs)
					{
						if (!(powered == null) && !(powered.CO == null))
						{
							if (!powered.bNoPowerThisLoop)
							{
								num2 += powered.TransmitPower(this.jsonPI.fAmount - num - num2, nDepth);
								if (num + num2 == this.jsonPI.fAmount)
								{
									break;
								}
							}
						}
					}
				}
			}
			nDepth--;
			if (nDepth <= 0)
			{
				this.UnmarkTransmits();
			}
		}
		if (this.PowerUsageRecorder == null)
		{
			this.PowerUsageRecorder = new PowerUsageRecorder();
		}
		this.PowerUsageRecorder.RecordChange(-num);
		return num + num2;
	}

	private void UnmarkTransmits()
	{
		this.bNoPowerThisLoop = false;
		if (this.CO == null)
		{
			Debug.LogError("ERROR: Running code on null CO.");
			return;
		}
		for (int i = 0; i < this.jsonPI.aInputPts.Length; i++)
		{
			Vector2 pos = this.CO.GetPos(this.jsonPI.aInputPts[i], false);
			Tile tileAtWorldCoords = this.CO.ship.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
			if (!(tileAtWorldCoords == null))
			{
				foreach (Powered powered in tileAtWorldCoords.aConnectedPowerCOs)
				{
					if (!(powered == null) && !(powered.CO == null))
					{
						if (powered.bNoPowerThisLoop)
						{
							powered.UnmarkTransmits();
						}
					}
				}
			}
		}
	}

	private double QueryPower(List<CondOwner> aCOs)
	{
		if (aCOs == null)
		{
			Debug.LogError("ERROR: Querying power on null list.");
			return 0.0;
		}
		double num = 0.0;
		foreach (CondOwner condOwner in aCOs)
		{
			Powered pwr = condOwner.Pwr;
			if (pwr != null && pwr.CO != null)
			{
				double condAmount = pwr.CO.GetCondAmount("StatPower");
				if (condAmount > 0.0)
				{
					num += condAmount;
				}
			}
		}
		return num;
	}

	private void UpdatePowerUI()
	{
		if (this.PowerUsageRecorder != null)
		{
			PowerUsageDTO statsString = this.PowerUsageRecorder.GetStatsString(this.CO.GetCondAmount("StatPower"), this.PowerStoredMax);
			this.CO.mapInfo["PowerCurrentLoad"] = statsString.PowerCurrentLoad;
			this.CO.mapInfo["PowerRemainingTime"] = statsString.PowerRemainingTime;
		}
		if (!CrewSim.PowerVizVisible || this.guiPower == null)
		{
			return;
		}
		float num = 0f;
		double powerStoredMax = this.PowerStoredMax;
		double condAmount = this.CO.GetCondAmount("StatPower");
		if (powerStoredMax > 0.0)
		{
			num = Mathf.Min(Convert.ToSingle(condAmount) / (float)powerStoredMax, 1f);
		}
		else if (this.fPowerLast > 0.0)
		{
			num = Convert.ToSingle(this.fPowerLast / this.jsonPI.fAmount);
		}
		this.guiPower.Set(num, this.jsonPI);
		if (this.CO.HasCond("StatPower"))
		{
			double num2 = (double)(num * 100f);
			this.CO.mapInfo["Charge"] = num2.ToString("n2") + "%";
		}
	}

	public void SetData(string strJsonPI)
	{
		this.jsonPI = DataHandler.GetPowerInfo(strJsonPI);
		if (this.jsonPI == null)
		{
			Debug.Log("Unable to load PowerInfo: " + strJsonPI);
			return;
		}
		if (this.jsonPI.Overlay())
		{
			this.guiPower = base.gameObject.AddComponent<GUIPowerOverlay>();
			Transform transform = this.guiPower.transform;
			Transform transform2 = this.CO.transform;
			transform.SetParent(base.transform, false);
			transform.localScale = new Vector3(transform2.localScale.x, transform2.localScale.y, 1f / transform2.localScale.z);
			Vector2 v = default(Vector2);
			if (this.jsonPI.aInputPts != null && this.jsonPI.aInputPts.Length > 0)
			{
				v = this.CO.GetPos(this.jsonPI.aInputPts[0], false);
			}
			this.guiPower.AlignInput(v, new Vector3(1f / transform2.localScale.x, 1f / transform2.localScale.y, 1f / transform2.localScale.z));
		}
		if (this.jsonPI.fAmount > 0.0)
		{
			if (this.jsonPI.strUsePowerCT != null)
			{
				this.bUsesPower = true;
				this.ctUsePower = DataHandler.GetCondTrigger(this.jsonPI.strUsePowerCT);
			}
			if (this.jsonPI.strRechargeCT != null)
			{
				this.ctRecharge = DataHandler.GetCondTrigger(this.jsonPI.strRechargeCT);
			}
		}
		this.ctPowerSource = ((!string.IsNullOrEmpty(this.jsonPI.strPowerSourceCT)) ? DataHandler.GetCondTrigger(this.jsonPI.strPowerSourceCT) : (this.ctPowerSource = DataHandler.GetCondTrigger("TIsPowerStorage")));
	}

	public bool Hide
	{
		get
		{
			return this.guiPower != null && this.guiPower.Hide;
		}
		set
		{
			if (this.guiPower != null)
			{
				this.guiPower.Hide = value;
			}
		}
	}

	public double PowerConnected
	{
		get
		{
			return this.fPowerConnected;
		}
	}

	public double PowerStoredMax
	{
		get
		{
			if (this.fMaxStored < 0.0)
			{
				this.fMaxStored = this.CO.GetCondAmount("StatPowerMax");
			}
			if (this.CO != null)
			{
				return this.fMaxStored * this.CO.GetDamageState();
			}
			return this.fMaxStored;
		}
	}

	public void ResetMaxPower()
	{
		this.fMaxStored = -1.0;
	}

	public void ResetCurrentToMaxPower()
	{
		if (this.CO == null || !this.CO.HasCond("StatPowerMax"))
		{
			return;
		}
		double powerStoredMax = this.PowerStoredMax;
		double condAmount = this.CO.GetCondAmount("StatPower");
		if (powerStoredMax < condAmount)
		{
			this.CO.SetCondAmount("StatPower", powerStoredMax, 0.0);
		}
	}

	public double PowerStoredPercent
	{
		get
		{
			double condAmount = this.CO.GetCondAmount("StatPower");
			double num = this.CO.GetCondAmount("StatPowerMax");
			if (this.CO != null)
			{
				num *= this.CO.GetDamageState();
			}
			if (num <= 0.0)
			{
				return 0.0;
			}
			if (condAmount <= 0.0)
			{
				return 0.0;
			}
			if (condAmount > num)
			{
				return 1.0;
			}
			return condAmount / num;
		}
	}

	public double PowerRechargeAmount
	{
		get
		{
			double num = this.PowerStoredMax;
			if (this.CO != null)
			{
				num = this.PowerStoredMax - this.CO.GetCondAmount("StatPower");
				num *= 0.001;
			}
			return num;
		}
	}

	public override string ToString()
	{
		if (this.CO == null)
		{
			return "Powered: null";
		}
		return this.CO.ToString();
	}

	public CondOwner CO
	{
		get
		{
			if (this.co == null && this.guiPower != null)
			{
				this.co = base.GetComponent<CondOwner>();
				this.itm = base.GetComponent<Item>();
			}
			return this.co;
		}
	}

	private static readonly double fMinDamage = 0.5;

	public CondOwner co;

	public JsonPowerInfo jsonPI;

	private GUIPowerOverlay guiPower;

	private bool bUsesPower;

	private CondTrigger ctUsePower;

	private CondTrigger ctRecharge;

	private double fPowerLast;

	private double fUpdateLast;

	private double fMinorUpdate;

	private double fPowerConnected;

	private double fDamageAmount;

	private Item itm;

	private CondTrigger ctPowerSource;

	private static Dictionary<Powered, double> mapPwrs = new Dictionary<Powered, double>();

	private static int debugTimesRunThisFrame;

	private static bool bOutput;

	private bool bDebug;

	private bool isOver5Percent;

	private double fMaxStored = -1.0;

	public bool bNoPowerThisLoop;

	private static float _flickerChance = 0.2f;
}
