using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Atmosphere and gas-storage simulator for rooms, tanks, and similar CondOwners.
// Tracks gas mole deltas, syncs with JsonAtmosphere data, and exposes gas prices.
public class GasContainer : MonoBehaviour, IManUpdater
{
	// Looks up one gas price from the `GasPrices` loot definition.
	public static float GetGasPrice(string gas)
	{
		if (string.IsNullOrEmpty(gas))
		{
			return 0f;
		}
		if (GasContainer._gasPrices == null || GasContainer._gasPrices.Count == 0)
		{
			GasContainer.InitGasPrices();
		}
		float result;
		GasContainer._gasPrices.TryGetValue(gas, out result);
		return result;
	}

	// Lazily loads the shared gas price table from `GasPrices` loot data.
	private static void InitGasPrices()
	{
		if (GasContainer._gasPrices != null && GasContainer._gasPrices.Count != 0)
		{
			return;
		}
		GasContainer._gasPrices = new Dictionary<string, float>();
		Dictionary<string, double> condLoot = DataHandler.GetLoot("GasPrices").GetCondLoot(1f, null, null);
		foreach (KeyValuePair<string, double> keyValuePair in condLoot)
		{
			GasContainer._gasPrices[keyValuePair.Key] = (float)keyValuePair.Value;
		}
	}

	// True for room-like containers whose volume is effectively "open to space".
	public bool IsVoid
	{
		get
		{
			return this.co != null && this.co.HasCond("IsRoom") && this.co.GetCondAmount("StatVolume") > 1E+50;
		}
	}

	// Caches the owning CondOwner, initializes gas maps, and loads helper triggers.
	private void Awake()
	{
		this.co = base.GetComponent<CondOwner>();
		this.mapGasMols1 = new Dictionary<string, double>();
		this.mapDGasMols = new Dictionary<string, double>();
		this.mapGasMols1.Add("StatGasMolTotal", 0.0);
		if (GasContainer._ctWallOrPortal == null)
		{
			GasContainer._ctWallOrPortal = DataHandler.GetCondTrigger("TIsWallOrPortalInstalled");
		}
		GasContainer.InitGasPrices();
	}

	// Main simulation tick routed through Run().
	private void Update()
	{
		this.Run(false);
	}

	// Forces this container toward a target JsonAtmosphere by computing the gas and
	// temperature deltas needed to match the requested partial pressures.
	public void SyncAtmo(JsonAtmosphere desiredValue)
	{
		double num = (double)desiredValue.fTemp;
		if (num == 0.0)
		{
			num = 0.01;
		}
		double condAmount = this.co.GetCondAmount("StatVolume");
		this.mapDGasMols["StatGasMolN2"] = (double)desiredValue.fN2 * condAmount / 0.008314000442624092 / num;
		double num2;
		if (this.mapGasMols1.TryGetValue("StatGasMolN2", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolN2"] = dictionary["StatGasMolN2"] - num2;
		}
		this.mapDGasMols["StatGasMolNH3"] = (double)desiredValue.fNH3 * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolNH3", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolNH3"] = dictionary["StatGasMolNH3"] - num2;
		}
		this.mapDGasMols["StatGasMolCH4"] = (double)desiredValue.fCH4 * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolCH4", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolCH4"] = dictionary["StatGasMolCH4"] - num2;
		}
		this.mapDGasMols["StatGasMolCO2"] = (double)desiredValue.fCO2 * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolCO2", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolCO2"] = dictionary["StatGasMolCO2"] - num2;
		}
		this.mapDGasMols["StatGasMolH2SO4"] = (double)desiredValue.fH2SO4 * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolH2SO4", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolH2SO4"] = dictionary["StatGasMolH2SO4"] - num2;
		}
		this.mapDGasMols["StatGasMolO2"] = (double)desiredValue.fO2 * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolO2", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolO2"] = dictionary["StatGasMolO2"] - num2;
		}
		this.mapDGasMols["StatGasMolHe2"] = (double)desiredValue.fHe2 * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolHe2", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolHe2"] = dictionary["StatGasMolHe2"] - num2;
		}
		this.mapDGasMols["StatGasMolH2"] = (double)desiredValue.fH2 * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolH2", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolH2"] = dictionary["StatGasMolH2"] - num2;
		}
		this.mapDGasMols["StatGasMolH2O"] = (double)desiredValue.fH2O * condAmount / 0.008314000442624092 / num;
		if (this.mapGasMols1.TryGetValue("StatGasMolH2O", out num2))
		{
			Dictionary<string, double> dictionary;
			(dictionary = this.mapDGasMols)["StatGasMolH2O"] = dictionary["StatGasMolH2O"] - num2;
		}
		double condAmount2 = this.co.GetCondAmount("StatGasTemp");
		this.fDGasTemp = num - condAmount2;
		if (double.IsNaN(this.fDGasTemp))
		{
			Debug.Log("fDGasTemp NaN");
		}
		this.co.GasChanged = true;
		this.Run(true);
	}

	// Manual driver for manager-controlled updates.
	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
	}

	private void Init()
	{
		if (this.co == null || this.co.mapConds == null)
		{
			string str = (!(this.co != null)) ? base.gameObject.name : this.co.strName;
			Debug.LogWarning("GasContainer null during Init on " + str);
			return;
		}
		if (this.co.HasCond("IsGasMolChanged"))
		{
			this.co.GasChanged = true;
		}
		this.mapGasMols1["StatGasMolTotal"] = 0.0;
		foreach (Condition condition in this.co.mapConds.Values)
		{
			if (condition.strName.IndexOf("StatGasMol") >= 0 && condition.strName.IndexOf("Total") < 0)
			{
				this.mapGasMols1[condition.strName] = condition.fCount;
				Dictionary<string, double> dictionary;
				(dictionary = this.mapGasMols1)["StatGasMolTotal"] = dictionary["StatGasMolTotal"] + condition.fCount;
				if (double.IsNaN(condition.fCount))
				{
					Debug.Log("NaN Found: mapDGasMols[strGas])");
				}
				else if (condition.fCount < 0.0)
				{
					Debug.Log("-Gas Found: Init");
				}
				if (!(condition.strName == "StatGasMolTotal"))
				{
					string strGas = condition.strName.Substring("StatGasMol".Length);
					this.fGasMass += GasContainer.GetGasMass(strGas, this.mapGasMols1[condition.strName]);
				}
			}
		}
		this.UpdateStats();
		this.bRequiresInit = false;
		this.co.ZeroCondAmount("IsGasRequiresInit");
	}

	public void BreakIn()
	{
		List<Condition> list = new List<Condition>();
		foreach (Condition condition in this.co.mapConds.Values)
		{
			if (condition.strName.IndexOf("StatGasMol") >= 0 && condition.strName.IndexOf("Total") < 0)
			{
				list.Add(condition);
			}
		}
		bool flag = false;
		Destructable component = this.co.GetComponent<Destructable>();
		if (this.co.GetCondAmount("StatDamageMax") > 0.0 && component.DmgLeft("StatDamage") <= 0.0)
		{
			flag = true;
		}
		foreach (Condition condition2 in list)
		{
			double num = -condition2.fCount;
			if (!flag)
			{
				num *= (double)MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
			}
			if (!this.mapDGasMols.ContainsKey(condition2.strName))
			{
				this.mapDGasMols[condition2.strName] = 0.0;
			}
			Dictionary<string, double> dictionary;
			string strName;
			(dictionary = this.mapDGasMols)[strName = condition2.strName] = dictionary[strName] + num;
			this.co.GasChanged = true;
		}
		this.Run(false);
	}

	public void AddGasMols(string strGas, double fAmount, bool run = true)
	{
		if (strGas == null)
		{
			return;
		}
		strGas = "StatGasMol" + strGas;
		if (DataHandler.GetCond(strGas) == null)
		{
			return;
		}
		if (!this.mapDGasMols.ContainsKey(strGas))
		{
			this.mapDGasMols[strGas] = 0.0;
		}
		Dictionary<string, double> dictionary;
		string key;
		(dictionary = this.mapDGasMols)[key = strGas] = dictionary[key] + fAmount;
		this.co.GasChanged = true;
		if (run)
		{
			this.Run(false);
		}
	}

	public void Run(bool rebuildMass = false)
	{
		if (this.bRequiresInit || this.co.HasCond("IsGasRequiresInit"))
		{
			if (this.co.Crew != null && this.co.HasCond("IsAirtight") && !this._carved && this.co.GetCondAmount("StatGasMolO2") == 0.0)
			{
				if (GasContainer.CTEVA.Triggered(this.co, null, true))
				{
					this.Init();
					this.PrefillSuits();
					return;
				}
				Room room = null;
				if (this.co.ship != null)
				{
					room = this.co.ship.GetRoomAtWorldCoords1(this.co.tf.position, false);
				}
				if (room != null)
				{
					this.CarveNewGasContainerFromRoom(room.CO, this.co);
				}
				return;
			}
			else
			{
				this.Init();
				if (this.co.HasCond("IsInstalled"))
				{
					if (GasContainer.LateInitGasContainers == null)
					{
						GasContainer.LateInitGasContainers = new List<CondOwner>();
						base.StartCoroutine(this.LateInit());
					}
					GasContainer.LateInitGasContainers.Add(this.co);
				}
			}
		}
		if (this.co.HasCond("IsGasContCarving"))
		{
			return;
		}
		if ((this.co.ship != null && this.co.GasChanged) || this.fDGasTemp != 0.0)
		{
			if (rebuildMass)
			{
				this.mapGasMols1["StatGasMolTotal"] = 0.0;
			}
			foreach (KeyValuePair<string, double> keyValuePair in this.mapDGasMols)
			{
				string key = keyValuePair.Key;
				if (!(key == "StatGasMolTotal"))
				{
					if (this.mapDGasMols.ContainsKey(key))
					{
						if (double.IsNaN(this.mapDGasMols[key]))
						{
							Debug.Log("NaN Found: mapDGasMols[strGas])");
							this.mapDGasMols[key] = 0.0;
						}
						this.co.AddCondAmount(key, this.mapDGasMols[key], 0.0, 0f);
						if (this.mapGasMols1.ContainsKey(key))
						{
							Dictionary<string, double> dictionary;
							string key2;
							(dictionary = this.mapGasMols1)[key2 = key] = dictionary[key2] + this.mapDGasMols[key];
						}
						else
						{
							this.mapGasMols1[key] = this.mapDGasMols[key];
						}
						if (rebuildMass)
						{
							Dictionary<string, double> dictionary;
							(dictionary = this.mapGasMols1)["StatGasMolTotal"] = dictionary["StatGasMolTotal"] + this.mapGasMols1[key];
						}
						else
						{
							Dictionary<string, double> dictionary;
							(dictionary = this.mapGasMols1)["StatGasMolTotal"] = dictionary["StatGasMolTotal"] + this.mapDGasMols[key];
						}
					}
				}
			}
			if (double.IsNaN(this.fDGasTemp))
			{
				Debug.Log("NaN Found: " + this.fDGasTemp);
				this.fDGasTemp = 0.0;
			}
			this.co.AddCondAmount("StatGasTemp", this.fDGasTemp, 0.0, 0f);
			double condAmount = this.co.GetCondAmount("StatGasPressure");
			double num = this.co.GetCondAmount("StatVolume");
			if (num == 0.0)
			{
				num = double.PositiveInfinity;
			}
			double num2 = this.mapGasMols1["StatGasMolTotal"] * 0.008314000442624092 * this.co.GetCondAmount("StatGasTemp") / num;
			this.co.AddCondAmount("StatGasPressure", num2 - condAmount, 0.0, 0f);
			if (double.IsNaN(condAmount) || double.IsNaN(num2) || double.IsNaN(this.fDGasTemp))
			{
				Debug.Log(string.Concat(new object[]
				{
					"NaN Found: ",
					condAmount,
					" or ",
					num2,
					" or ",
					this.fDGasTemp
				}));
			}
			double num3 = this.mapGasMols1["StatGasMolTotal"];
			if (num3 == 0.0)
			{
				num3 = 1E-06;
			}
			this.fGasMass = 0.0;
			foreach (KeyValuePair<string, double> keyValuePair2 in this.mapGasMols1)
			{
				string key3 = keyValuePair2.Key;
				if (!(key3 == "StatGasMolTotal"))
				{
					int index = FluidStrings.mol.IndexOf(key3);
					double num4 = this.mapGasMols1[key3] / num3 * this.co.GetCondAmount("StatGasPressure");
					double condAmount2 = this.co.GetCondAmount(FluidStrings.pps[index]);
					this.co.AddCondAmount(FluidStrings.pps[index], num4 - condAmount2, 0.0, 0f);
					this.fGasMass += GasContainer.GetGasMass(FluidStrings.moleculeNames[index], this.mapGasMols1[key3]);
					if (double.IsNaN(num4 - condAmount2))
					{
						Debug.Log(string.Concat(new object[]
						{
							"NaN Found: ",
							condAmount2,
							" or ",
							num4
						}));
					}
				}
			}
			if (double.IsNaN(num3))
			{
				Debug.Log("NaN Found: " + num3);
			}
			this.UpdateStats();
			this.mapDGasMols.Clear();
			this.fDGasTemp = 0.0;
			this.co.GasChanged = false;
		}
		if (this._timeNextPressureDifCheck < StarSystem.fEpoch)
		{
			this._timeNextPressureDifCheck = StarSystem.fEpoch + 1.0;
			this.CheckPressureDifference();
		}
	}

	private IEnumerator LateInit()
	{
		yield return null;
		if (GasContainer.LateInitGasContainers != null)
		{
			foreach (CondOwner condOwner in GasContainer.LateInitGasContainers)
			{
				if (!(condOwner == null) && condOwner.ship != null)
				{
					Room roomAtWorldCoords = condOwner.ship.GetRoomAtWorldCoords1(condOwner.tf.position, false);
					if (roomAtWorldCoords != null)
					{
						roomAtWorldCoords.CalculateRoomValue();
					}
				}
			}
			GasContainer.LateInitGasContainers = null;
		}
		yield break;
	}

	public float GetTotalGasValue()
	{
		if (this.mapGasMols1 == null || this.bRequiresInit)
		{
			return 0f;
		}
		float num = 0f;
		foreach (string text in this.mapGasMols1.Keys)
		{
			if (!(text == "StatGasMolTotal"))
			{
				string text2 = text.Substring("StatGasMol".Length);
				double gasMass = GasContainer.GetGasMass(text2, this.mapGasMols1[text]);
				float num2;
				if (GasContainer._gasPrices.TryGetValue(text2, out num2))
				{
					num += (float)((double)num2 * gasMass);
				}
			}
		}
		return num;
	}

	private void CheckPressureDifference()
	{
		if (this.co == null || this.co.ship == null)
		{
			return;
		}
		Room roomAtWorldCoords = this.co.ship.GetRoomAtWorldCoords1(base.transform.position, false);
		if (roomAtWorldCoords != null && roomAtWorldCoords.CO != null)
		{
			GasContainer.CheckPressureDifference(this, roomAtWorldCoords.CO.GasContainer, Vector2.zero, this.co);
		}
	}

	public static void CheckPressureDifference(GasContainer a, GasContainer b, Vector2 coPos, CondOwner coContainer = null)
	{
		double condAmount = a.co.GetCondAmount("StatGasPressure", false);
		double condAmount2 = b.co.GetCondAmount("StatGasPressure", false);
		double num = Math.Abs(condAmount - condAmount2);
		if (num < 150.0)
		{
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		if (coContainer == null)
		{
			if (!a.CachedBorderCOs.TryGetValue(coPos, out list))
			{
				list = new List<CondOwner>();
				a.co.ship.GetCOsAtWorldCoords1(coPos, GasContainer._ctWallOrPortal, false, false, list);
				a.CachedBorderCOs[coPos] = list;
				b.CachedBorderCOs[coPos] = list;
			}
		}
		else
		{
			list.Add(coContainer);
		}
		bool flag = false;
		foreach (CondOwner condOwner in list)
		{
			if (condOwner == null || condOwner.bDestroyed || condOwner.ship == null)
			{
				flag = true;
			}
			else
			{
				double num2 = condOwner.GetCondAmount("StatGasPressureMax", false);
				if (num2 != 0.0)
				{
					num2 += 150.0;
					if (num > num2)
					{
						float max = (float)(num / num2);
						float num3 = UnityEngine.Random.Range(0f, max);
						condOwner.AddCondAmount("StatDamage", (double)num3, 0.0, 0f);
						if (condOwner == coContainer)
						{
							AudioManager.am.PlayCreakAudio("TXTRandomCreakCanAudio");
						}
						else
						{
							AudioManager.am.PlayCreakAudio("TXTRandomCreakMedAudio");
						}
					}
				}
			}
		}
		if (flag)
		{
			a.CachedBorderCOs.Clear();
			b.CachedBorderCOs.Clear();
		}
	}

	private void PrefillSuits()
	{
		if (this.co.Crew == null || !this.co.HasCond("IsAirtight") || this.co.GetCondAmount("StatGasMolO2") != 0.0)
		{
			return;
		}
		List<CondOwner> cos = this.co.GetCOs(true, DataHandler.GetCondTrigger("TIsFitContainerEVABottle"));
		if (cos == null || cos.Count == 0)
		{
			return;
		}
		List<CondOwner> cos2 = this.co.GetCOs(true, DataHandler.GetCondTrigger("TIsEVAOn"));
		if (cos2 == null || cos2.Count == 0)
		{
			return;
		}
		this.AddGasMols("O2", 1.0, false);
	}

	public void UpdateStats()
	{
		this.co.mapInfo["Pressure"] = this.co.GetCondAmount("StatGasPressure").ToString("n2") + "kPa";
	}

	public double RemoveGasMass(double fMass)
	{
		if (fMass <= 0.0 || this.fGasMass <= 0.0)
		{
			return 0.0;
		}
		double num = Math.Min(1.0, fMass / this.fGasMass);
		double num2 = this.co.GetCondAmount("StatVolume");
		if (num2 == 0.0)
		{
			num2 = double.PositiveInfinity;
		}
		foreach (string text in this.mapGasMols1.Keys)
		{
			if (!(text == "StatGasMolTotal"))
			{
				string str = text.Substring("StatGasMol".Length);
				string strName = "StatGasPp" + str;
				double condAmount = this.co.GetCondAmount(strName);
				double num3 = condAmount - num * condAmount;
				double num4 = this.co.GetCondAmount("StatGasTemp");
				if (num4 == 0.0)
				{
					num4 = 0.01;
				}
				else if (double.IsNaN(num4))
				{
					Debug.Log("Found NaN temp.");
					num4 = 0.01;
				}
				double num5 = num2 * (condAmount - num3) / (0.008314000442624092 * num4);
				if (this.mapDGasMols.ContainsKey(text))
				{
					Dictionary<string, double> dictionary;
					string key;
					(dictionary = this.mapDGasMols)[key = text] = dictionary[key] + -num5;
				}
				else
				{
					this.mapDGasMols[text] = -num5;
				}
				if (this.mapDGasMols[text] != 0.0)
				{
					this.co.GasChanged = true;
					this.co.AddCondAmount(strName, -this.co.GetCondAmount(strName), 0.0, 0f);
				}
			}
		}
		return num * this.fGasMass;
	}

	public static void MergeGasContainersAndDestroy(CondOwner coDestroy, CondOwner coAbsorb)
	{
		if (coDestroy == null)
		{
			return;
		}
		double num = double.PositiveInfinity;
		double num2 = double.PositiveInfinity;
		double num3 = 0.009999999776482582;
		double num4 = 0.009999999776482582;
		if (coDestroy != null)
		{
			num = coDestroy.GetCondAmount("StatVolume");
			if (num == 0.0)
			{
				num = double.PositiveInfinity;
			}
			num3 = coDestroy.GetCondAmount("StatGasTemp");
		}
		if (coAbsorb != null)
		{
			num2 = coAbsorb.GetCondAmount("StatVolume");
			if (num2 == 0.0)
			{
				num2 = double.PositiveInfinity;
			}
			num4 = coAbsorb.GetCondAmount("StatGasTemp");
		}
		if ((coDestroy == null && coAbsorb == null) || coDestroy == coAbsorb || (num == double.PositiveInfinity && num2 == double.PositiveInfinity))
		{
			return;
		}
		GasContainer gasContainer = null;
		GasContainer gasContainer2 = null;
		if (coDestroy != null)
		{
			gasContainer = coDestroy.GasContainer;
		}
		if (coAbsorb != null)
		{
			gasContainer2 = coAbsorb.GasContainer;
		}
		Dictionary<string, double> gasMolsVoid;
		if (gasContainer != null)
		{
			gasMolsVoid = gasContainer.mapGasMols1;
		}
		else
		{
			gasMolsVoid = GasExchange.GasMolsVoid;
		}
		Dictionary<string, double> gasMolsVoid2;
		if (gasContainer2 != null)
		{
			gasMolsVoid2 = gasContainer2.mapGasMols1;
		}
		else
		{
			gasMolsVoid2 = GasExchange.GasMolsVoid;
		}
		foreach (KeyValuePair<string, double> keyValuePair in gasMolsVoid)
		{
			string key = keyValuePair.Key;
			if (!(key == "StatGasMolTotal"))
			{
				if (!gasMolsVoid2.ContainsKey(key))
				{
					gasMolsVoid2.Add(key, 0.0);
				}
				double num5 = gasMolsVoid[key];
				double num6 = gasMolsVoid2[key];
				if (coAbsorb != null && coAbsorb.HasCond("StatVolume"))
				{
					if (gasContainer2.mapDGasMols.ContainsKey(key))
					{
						Dictionary<string, double> dictionary;
						string key2;
						(dictionary = gasContainer2.mapDGasMols)[key2 = key] = dictionary[key2] + num5;
					}
					else
					{
						gasContainer2.mapDGasMols[key] = num5;
					}
					if (gasContainer2.mapDGasMols[key] != 0.0)
					{
						coAbsorb.GasChanged = true;
					}
				}
			}
		}
		double num7 = num3;
		if (gasMolsVoid2["StatGasMolTotal"] + gasMolsVoid["StatGasMolTotal"] > 0.0)
		{
			num7 = (num3 * gasMolsVoid["StatGasMolTotal"] + num4 * gasMolsVoid2["StatGasMolTotal"]) / (gasMolsVoid2["StatGasMolTotal"] + gasMolsVoid["StatGasMolTotal"]);
		}
		if (gasContainer2 != null && coAbsorb.HasCond("StatVolume"))
		{
			gasContainer2.fDGasTemp += num7 - num4;
			if (double.IsNaN(gasContainer2.fDGasTemp))
			{
				Debug.Log("fDGasTemp NaN");
				gasContainer2.fDGasTemp = 0.0;
			}
		}
		List<Condition> list = new List<Condition>();
		foreach (Condition condition in coDestroy.mapConds.Values)
		{
			if (condition.strName.IndexOf("StatGas") >= 0)
			{
				list.Add(condition);
			}
		}
		bool bLogConds = coDestroy.bLogConds;
		coDestroy.bLogConds = false;
		foreach (Condition condition2 in list)
		{
			coDestroy.AddCondAmount(condition2.strName, -condition2.fCount, 0.0, 0f);
		}
		coDestroy.bLogConds = bLogConds;
		foreach (string text in gasMolsVoid.Keys)
		{
			if (!(text == "StatGasMolTotal"))
			{
				string key3 = text.Substring("StatGasMol".Length);
				coDestroy.mapInfo.Remove(key3);
			}
		}
		coDestroy.mapInfo.Remove("Temp");
		coDestroy.mapInfo.Remove("Pressure");
		coDestroy.mapInfo.Remove("Mass");
		BodyTemp component = coDestroy.GetComponent<BodyTemp>();
		if (component != null)
		{
			component.ForceUpdate();
		}
		UnityEngine.Object.Destroy(gasContainer);
	}

	public void CarveNewGasContainerFromRoom(CondOwner coRoom, CondOwner coNew)
	{
		this._carved = true;
		coNew.AddCondAmount("IsGasContCarving", 1.0, 0.0, 0f);
		base.StartCoroutine(this._CarveNewGasContainerFromRoom(coRoom, coNew));
	}

	private IEnumerator _CarveNewGasContainerFromRoom(CondOwner coRoom, CondOwner coUs)
	{
		yield return null;
		if (coRoom == null && coUs != null && coUs.ship != null)
		{
			Room roomAtWorldCoords = coUs.ship.GetRoomAtWorldCoords1(coUs.tf.position, false);
			if (roomAtWorldCoords != null)
			{
				coRoom = roomAtWorldCoords.CO;
			}
		}
		if (coUs == null || coRoom == null)
		{
			if (coUs != null)
			{
				coUs.AddCondAmount("IsGasContCarving", -1.0, 0.0, 0f);
			}
			yield break;
		}
		GasContainer gcRoom = coRoom.GasContainer;
		Dictionary<string, double> mapGasMolsUs = this.mapGasMols1;
		Dictionary<string, double> mapGasMolsRoom = (!(gcRoom != null)) ? GasExchange.GasMolsVoid : gcRoom.mapGasMols1;
		double fVolUs = coUs.GetCondAmount("StatVolume");
		double fTempUs = coUs.GetCondAmount("StatGasTemp");
		if (fVolUs == 0.0)
		{
			fVolUs = double.PositiveInfinity;
		}
		double fVolRoom = coRoom.GetCondAmount("StatVolume");
		if (fVolRoom == 0.0)
		{
			fVolRoom = double.PositiveInfinity;
		}
		double fTempRoom = coRoom.GetCondAmount("StatGasTemp");
		if (fTempRoom == 0.0)
		{
			fTempRoom = 0.01;
		}
		foreach (string text in mapGasMolsRoom.Keys)
		{
			if (!(text == "StatGasMolTotal"))
			{
				if (!mapGasMolsUs.ContainsKey(text))
				{
					mapGasMolsUs.Add(text, 0.0);
				}
				double num = mapGasMolsUs[text];
				double num2 = mapGasMolsRoom[text];
				double num3 = num2 * 0.008314000442624092 * fTempRoom / fVolRoom;
				double num4 = (num + num2) * 0.008314000442624092 * fTempRoom / (fVolUs + fVolRoom);
				double num5 = num4 - num3;
				double num6 = (num3 + num5) * fVolRoom / (0.008314000442624092 * fTempRoom);
				if (gcRoom.IsVoid)
				{
					num2 = num3 * fVolUs / (0.008314000442624092 * fTempRoom);
					num6 = 0.0;
				}
				if (double.IsNaN(num6))
				{
					num6 = 0.0;
				}
				if (double.IsNaN(num2))
				{
					Debug.Log("NaN Found: " + num2);
					num2 = 0.0;
				}
				if (!gcRoom.IsVoid)
				{
					if (gcRoom.mapDGasMols.ContainsKey(text))
					{
						Dictionary<string, double> dictionary;
						string key;
						(dictionary = gcRoom.mapDGasMols)[key = text] = dictionary[key] + (num6 - num2);
					}
					else
					{
						gcRoom.mapDGasMols[text] = num6 - num2;
					}
					if (gcRoom.mapDGasMols[text] != 0.0)
					{
						coRoom.GasChanged = true;
					}
				}
				if (coUs.HasCond("StatVolume"))
				{
					if (this.mapDGasMols.ContainsKey(text))
					{
						Dictionary<string, double> dictionary;
						string key2;
						(dictionary = this.mapDGasMols)[key2 = text] = dictionary[key2] + (num2 - num6);
					}
					else
					{
						this.mapDGasMols[text] = num2 - num6;
					}
					if (this.mapDGasMols[text] != 0.0)
					{
						coUs.GasChanged = true;
					}
				}
			}
		}
		if (coUs.HasCond("StatVolume"))
		{
			this.fDGasTemp += fTempRoom - fTempUs;
		}
		if (double.IsNaN(this.fDGasTemp))
		{
			Debug.Log("NaN DTemp Found");
			this.fDGasTemp = 0.0;
		}
		coUs.AddCondAmount("IsGasContCarving", -1.0, 0.0, 0f);
		yield break;
	}

	public void SetData()
	{
		this.co = base.GetComponent<CondOwner>();
	}

	public double Mass
	{
		get
		{
			return this.fGasMass;
		}
	}

	public static double GetGasMass(string strGas, double fMols)
	{
		double result = 0.0;
		switch (strGas)
		{
		case "H2":
			result = 0.0020158999999999997 * fMols;
			break;
		case "He2":
			result = 0.008005204 * fMols;
			break;
		case "CH4":
			result = 0.016042999999999998 * fMols;
			break;
		case "NH3":
			result = 0.017030999999999998 * fMols;
			break;
		case "H2O":
			result = 0.0180153 * fMols;
			break;
		case "N2":
			result = 0.0280134 * fMols;
			break;
		case "O2":
			result = 0.0319988 * fMols;
			break;
		case "CO2":
			result = 0.04401 * fMols;
			break;
		case "H2SO4":
			result = 0.0980785 * fMols;
			break;
		}
		return result;
	}

	public static double GetGasDensity(string strGas, double pressure, double temperature)
	{
		if (temperature == 0.0)
		{
			return 0.0;
		}
		return pressure * GasContainer.GetGasMass(strGas, 1.0) / 0.008314000442624092 / temperature;
	}

	public static double GetGasDensity(JsonAtmosphere jAtmo)
	{
		double num = 0.0;
		if (jAtmo == null || jAtmo.fTemp == 0f)
		{
			return 0.0;
		}
		if (jAtmo.fCO2 != 0f)
		{
			num += GasContainer.GetGasDensity("CO2", (double)jAtmo.fCO2, (double)jAtmo.fTemp);
		}
		if (jAtmo.fN2 != 0f)
		{
			num += GasContainer.GetGasDensity("N2", (double)jAtmo.fN2, (double)jAtmo.fTemp);
		}
		if (jAtmo.fH2SO4 != 0f)
		{
			num += GasContainer.GetGasDensity("H2SO4", (double)jAtmo.fH2SO4, (double)jAtmo.fTemp);
		}
		if (jAtmo.fO2 != 0f)
		{
			num += GasContainer.GetGasDensity("O2", (double)jAtmo.fO2, (double)jAtmo.fTemp);
		}
		if (jAtmo.fH2 != 0f)
		{
			num += GasContainer.GetGasDensity("H2", (double)jAtmo.fH2, (double)jAtmo.fTemp);
		}
		if (jAtmo.fHe2 != 0f)
		{
			num += GasContainer.GetGasDensity("He2", (double)jAtmo.fHe2, (double)jAtmo.fTemp);
		}
		if (jAtmo.fCH4 != 0f)
		{
			num += GasContainer.GetGasDensity("CH4", (double)jAtmo.fCH4, (double)jAtmo.fTemp);
		}
		if (jAtmo.fNH3 != 0f)
		{
			num += GasContainer.GetGasDensity("NH3", (double)jAtmo.fNH3, (double)jAtmo.fTemp);
		}
		if (jAtmo.fH2O != 0f)
		{
			num += GasContainer.GetGasDensity("H2O", (double)jAtmo.fH2O, (double)jAtmo.fTemp);
		}
		return num;
	}

	public static CondTrigger CTEVA
	{
		get
		{
			if (GasContainer._ctEVA == null)
			{
				GasContainer._ctEVA = DataHandler.GetCondTrigger("TIsWearingEVAOn");
			}
			return GasContainer._ctEVA;
		}
	}

	public const double VOID_THRESHOLD = 1E+50;

	private const double MIN_PRESSURE_DIFF = 150.0;

	private CondOwner co;

	public double fDGasTemp;

	public Dictionary<string, double> mapDGasMols;

	public Dictionary<string, double> mapGasMols1;

	private bool bRequiresInit = true;

	private double fGasMass;

	private bool _carved;

	private static CondTrigger _ctEVA;

	private static CondTrigger _ctWallOrPortal;

	public Dictionary<Vector2, List<CondOwner>> CachedBorderCOs = new Dictionary<Vector2, List<CondOwner>>();

	private double _timeNextPressureDifCheck;

	private static Dictionary<string, float> _gasPrices = new Dictionary<string, float>();

	private static List<CondOwner> LateInitGasContainers;

	public bool hasRun;
}
