using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;
using UnityEngine;

public class GasPump : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.bUpdateRemote = true;
		this.co = base.GetComponent<CondOwner>();
	}

	private void Update()
	{
		if (this.bUpdateRemote)
		{
			this.UpdateRemote();
		}
		double num = StarSystem.fEpoch - this.fTimeOfLastSignalCheck;
		if (num < (double)this.gr.fSignalCheckRate)
		{
			return;
		}
		if (this.co.ship == null || CrewSim.system == null)
		{
			return;
		}
		float fCoeff = Convert.ToSingle(num / (double)this.gr.fSignalCheckRate);
		if (this.bRespire)
		{
			this.Respire2(fCoeff);
		}
		if (this.bPump)
		{
			this.Pump(fCoeff);
		}
		if (this.bRespire || this.bPump)
		{
			this.HandleActivePumpAudio();
		}
		this.CatchUp();
	}

	private void HandleActivePumpAudio()
	{
		bool flag = StarSystem.fEpoch - this._lastActivePumpAir > 2.0;
		bool flag2 = StarSystem.fEpoch - this._lastActivePump > 2.0;
		if (this._audioEmitterAir != null)
		{
			if (flag)
			{
				this._audioEmitterAir.StopSteady();
			}
			else
			{
				if (!this._audioEmitterAir.PlayingSteady)
				{
					this._audioEmitterAir.FadeInSteady(-1f, -1f);
				}
				this._audioEmitterAir.PlaySteady();
			}
		}
		if (this._audioEmitter != null)
		{
			if (flag2)
			{
				this._audioEmitter.StopSteady();
			}
			else
			{
				if (!this._audioEmitter.PlayingSteady)
				{
					this._audioEmitter.FadeInSteady(-1f, -1f);
				}
				this._audioEmitter.PlaySteady();
			}
		}
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
		this.fTimeOfLastSignalCheck = StarSystem.fEpoch;
	}

	private void UpdateRemote()
	{
		Dictionary<string, string> dictionary = null;
		if (this.co.ship != null && this.co.mapGUIPropMaps.TryGetValue(this.strCOKey, out dictionary))
		{
			string empty = string.Empty;
			if (dictionary.TryGetValue("strInput01", out empty))
			{
				CondOwner cobyID = this.co.ship.GetCOByID(empty);
				if (cobyID != null)
				{
					this.strRemoteID = cobyID.strID;
				}
			}
			if (dictionary.TryGetValue("bTurbo", out empty) && bool.Parse(empty))
			{
				this.co.SetCondAmount("IsTurboOn", 1.0, 0.0);
			}
			if (dictionary.TryGetValue("bReverse", out empty) && bool.Parse(empty))
			{
				this.co.SetCondAmount("IsReverseOn", 1.0, 0.0);
			}
			if (dictionary.TryGetValue("bSlowMode", out empty) && bool.Parse(empty))
			{
				this.co.SetCondAmount("IsSlowModeOn", 1.0, 0.0);
			}
			if (dictionary.TryGetValue("nKnobBus", out empty))
			{
				this.co.AddCondAmount("IsOverrideOff", -this.co.GetCondAmount("IsOverrideOff"), 0.0, 0f);
				this.co.AddCondAmount("IsOverrideOn", -this.co.GetCondAmount("IsOverrideOn"), 0.0, 0f);
				int num = int.Parse(empty);
				if (num != 1)
				{
					if (num != 2)
					{
						this.co.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
					}
					else
					{
						this.co.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
					}
				}
			}
		}
		this.bUpdateRemote = false;
	}

	public Tuple<CondOwner, CondOwner> GetActors()
	{
		return new Tuple<CondOwner, CondOwner>(this.GetA(), this.GetB());
	}

	private CondOwner GetA()
	{
		List<CondOwner> list = new List<CondOwner>();
		if (this.gr.bAllowExternA)
		{
			this.co.ship.GetCOsAtWorldCoords1(this.co.GetPos(this.gr.strPtA, false), this.ctA, true, false, list);
		}
		CondOwner.NullSafeAddRange(ref list, this.co.GetCOs(false, this.ctA));
		return (list.Count <= 0) ? null : list[0];
	}

	private CondOwner GetB()
	{
		List<CondOwner> list = new List<CondOwner>();
		if (this.gr.bAllowExternB)
		{
			this.co.ship.GetCOsAtWorldCoords1(this.co.GetPos(this.gr.strPtB, false), this.ctB, true, false, list);
		}
		CondOwner.NullSafeAddRange(ref list, this.co.GetCOs(false, this.ctB));
		return (list.Count <= 0) ? null : list[0];
	}

	private void Pump(float fCoeff)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = this.co.HasCond("IsOverrideOn");
		if (flag3)
		{
			flag = true;
		}
		else if (this.co.HasCond("IsOverrideOff"))
		{
			flag = false;
		}
		else if (this.strRemoteID == null)
		{
			flag = this.ctSignalMain.Triggered(this.co, null, true);
			CondOwner condOwner = this.co;
		}
		else
		{
			CondOwner cobyID = this.co.ship.GetCOByID(this.strRemoteID);
			if (cobyID != null && this.ctSignalMain.Triggered(cobyID, null, true))
			{
				flag = true;
			}
		}
		if (flag)
		{
			double num = 0.009999999776482582;
			CondOwner condOwner2 = this.GetA();
			CondOwner condOwner3 = null;
			float num2 = 1f;
			if (this.co.HasCond("IsTurboOn"))
			{
				num2 = (float)this.co.GetCondAmount("IsTurbo");
			}
			if (this.co.HasCond("IsSlowModeOn"))
			{
				num2 = 0.1f;
			}
			bool flag4 = this.ctSignalA.Triggered(condOwner2, null, true) || flag3;
			if (flag4)
			{
				condOwner3 = condOwner2;
				if (this.gr.strPtA != this.gr.strPtB || this.gr.strCTA != this.gr.strCTB)
				{
					condOwner3 = this.GetB();
				}
				flag2 = (this.ctSignalB.Triggered(condOwner3, null, true) || flag3);
			}
			if (flag4 && flag2)
			{
				if (this.co.HasCond("IsReverseOn"))
				{
					CondOwner condOwner4 = condOwner2;
					condOwner2 = condOwner3;
					condOwner3 = condOwner4;
				}
				GasContainer gasContainer = null;
				GasContainer gasContainer2 = null;
				if (condOwner2 != null)
				{
					gasContainer = condOwner2.GasContainer;
				}
				if (condOwner3 != null)
				{
					gasContainer2 = condOwner3.GasContainer;
				}
				this._lastActivePump = StarSystem.fEpoch;
				if (gasContainer != null)
				{
					num = condOwner2.GetCondAmount("StatGasTemp");
					if (num == 0.0)
					{
						num = 0.01;
					}
					Dictionary<string, double> mapGasMols = gasContainer.mapGasMols1;
					foreach (KeyValuePair<string, double> keyValuePair in mapGasMols)
					{
						string key = keyValuePair.Key;
						if (!(key == "StatGasMolTotal"))
						{
							int index = FluidStrings.mol.IndexOf(key);
							string text = FluidStrings.moleculeNames[index];
							string strName = FluidStrings.pps[index];
							double condAmount = condOwner2.GetCondAmount(strName);
							double num3 = condAmount * (double)(this.gr.fVol * fCoeff * num2) / (0.008314000442624092 * num);
							if (num3 > condOwner2.GetCondAmount(key) || num3 < 1E-05)
							{
								num3 = condOwner2.GetCondAmount(key);
							}
							if (!gasContainer.IsVoid)
							{
								if (gasContainer.mapDGasMols.ContainsKey(key))
								{
									Dictionary<string, double> mapDGasMols;
									string key2;
									(mapDGasMols = gasContainer.mapDGasMols)[key2 = key] = mapDGasMols[key2] + -num3;
								}
								else
								{
									gasContainer.mapDGasMols[key] = -num3;
								}
								if (gasContainer.mapDGasMols[key] != 0.0)
								{
									this._lastActivePumpAir = StarSystem.fEpoch;
									condOwner2.GasChanged = true;
									condOwner2.AddCondAmount(strName, -condOwner2.GetCondAmount(strName), 0.0, 0f);
								}
							}
							if (double.IsNaN(num3))
							{
								Debug.Log("NaN Found: " + num3);
							}
							if (gasContainer2 != null && !gasContainer2.IsVoid)
							{
								double num4 = condOwner3.GetCondAmount("StatGasTemp");
								if (double.IsNaN(num4))
								{
									num4 = 0.01;
								}
								Dictionary<string, double> mapGasMols2 = gasContainer2.mapGasMols1;
								double num5 = num4;
								if (condOwner2.GasChanged)
								{
									num5 = (num * num3 + num4 * mapGasMols2["StatGasMolTotal"]) / (num3 + mapGasMols2["StatGasMolTotal"]);
								}
								if (double.IsNaN(num5))
								{
									num5 = num4;
								}
								if (gasContainer2.mapDGasMols.ContainsKey(key))
								{
									Dictionary<string, double> mapDGasMols;
									string key3;
									(mapDGasMols = gasContainer2.mapDGasMols)[key3 = key] = mapDGasMols[key3] + num3;
								}
								else
								{
									gasContainer2.mapDGasMols[key] = num3;
								}
								if (gasContainer2.mapDGasMols[key] != 0.0)
								{
									this._lastActivePumpAir = StarSystem.fEpoch;
									condOwner3.GasChanged = true;
									condOwner3.AddCondAmount(strName, -condOwner3.GetCondAmount(strName), 0.0, 0f);
								}
								gasContainer2.fDGasTemp = num5 - num4;
								if (double.IsNaN(num4) || double.IsNaN(num5) || double.IsNaN(num3))
								{
									Debug.Log(string.Concat(new object[]
									{
										"NaN Found: ",
										num4,
										" or ",
										num5,
										" or ",
										num3
									}));
								}
							}
						}
					}
					if (gasContainer != null)
					{
						gasContainer.Run(false);
					}
					if (gasContainer2 != null)
					{
						gasContainer2.Run(false);
					}
				}
			}
			this.co.mapInfo["Status"] = "Pumping";
		}
		else
		{
			this.co.mapInfo["Status"] = "Idle";
		}
	}

	private void Start()
	{
		if (this.co == null || this.co.ship == null)
		{
			return;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsAirPumpAudioProducer");
		if (!condTrigger.Triggered(this.co, null, true))
		{
			return;
		}
		this._audioEmitterAir = null;
		if (this.gr != null && this.gr.strAudioEmitterAir != null)
		{
			JsonAudioEmitter audioEmitter = DataHandler.GetAudioEmitter(this.gr.strAudioEmitterAir);
			if (audioEmitter != null)
			{
				this._audioEmitterAir = this.co.gameObject.AddComponent<AudioEmitter>();
				this._audioEmitterAir.SetData(audioEmitter);
			}
		}
		this._audioEmitter = null;
		if (this.gr != null && this.gr.strAudioEmitterPump != null)
		{
			JsonAudioEmitter audioEmitter2 = DataHandler.GetAudioEmitter(this.gr.strAudioEmitterPump);
			if (audioEmitter2 != null)
			{
				this._audioEmitter = this.co.gameObject.AddComponent<AudioEmitter>();
				this._audioEmitter.SetData(audioEmitter2);
			}
		}
	}

	private void Respire2(float fCoeff)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = this.co.HasCond("IsOverrideOn");
		if (flag3)
		{
			flag = true;
		}
		else if (this.co.HasCond("IsOverrideOff"))
		{
			flag = false;
		}
		else if (this.strRemoteID == null)
		{
			flag = this.ctSignalMain.Triggered(this.co, null, true);
		}
		else
		{
			CondOwner cobyID = this.co.ship.GetCOByID(this.strRemoteID);
			if (this.ctSignalMain.Triggered(cobyID, null, true))
			{
				flag = true;
			}
		}
		if (flag)
		{
			List<CondOwner> list = new List<CondOwner>();
			if (this.gr.bAllowExternA)
			{
				this.co.ship.GetCOsAtWorldCoords1(this.co.GetPos(this.gr.strPtA, false), this.ctA, true, false, list);
			}
			if (this.ctA != null && this.ctA.Triggered(this.co, null, true) && list.IndexOf(this.co) < 0)
			{
				list.Add(this.co);
			}
			if (this.ctA == null || this.ctA.aReqs == null || !this.ctA.aReqs.Contains("IsHuman"))
			{
				CondOwner.NullSafeAddRange(ref list, this.co.GetCOsEarlyOut(false, this.ctA));
			}
			CondOwner condOwner = null;
			CondOwner condOwner2 = null;
			if (list.Count > 0)
			{
				condOwner = list[0];
			}
			bool flag4 = this.ctSignalA.Triggered(condOwner, null, true) || flag3;
			if (flag4)
			{
				condOwner2 = condOwner;
				if (this.gr.strPtA != this.gr.strPtB || this.gr.strCTA != this.gr.strCTB)
				{
					list.Clear();
					if (this.gr.bAllowExternB)
					{
						this.co.ship.GetCOsAtWorldCoords1(this.co.GetPos(this.gr.strPtB, false), this.ctB, true, false, list);
					}
					if (this.ctB != null && this.ctB.Triggered(this.co, null, true) && list.IndexOf(this.co) < 0)
					{
						list.Add(this.co);
					}
					if (this.ctB == null || this.ctB.aReqs == null || !this.ctB.aReqs.Contains("IsHuman"))
					{
						CondOwner.NullSafeAddRange(ref list, this.co.GetCOsEarlyOut(false, this.ctB));
					}
					if (list.Count > 0)
					{
						condOwner2 = list[0];
					}
				}
				flag2 = (this.ctSignalB.Triggered(condOwner2, null, true) || flag3);
			}
			if (flag4 && flag2)
			{
				if (this.co.HasCond("IsReverseOn"))
				{
					CondOwner condOwner3 = condOwner;
					condOwner = condOwner2;
					condOwner2 = condOwner3;
				}
				float num = 1f;
				if (this.co.HasCond("IsTurboOn"))
				{
					num = (float)this.co.GetCondAmount("IsTurbo");
				}
				this._lastActivePump = StarSystem.fEpoch;
				if (condOwner != null)
				{
					GasContainer gasContainer = condOwner.GasContainer;
					if (gasContainer != null)
					{
						double num2 = Math.Max(0.01, condOwner.GetCondAmount("StatGasPressure"));
						double num3 = Math.Max(0.01, condOwner.GetCondAmount("StatGasTemp"));
						if (this.co.HasCond("IsHuman"))
						{
							if (condOwner.HasCond("DcGasPressure01"))
							{
								Interaction interaction = DataHandler.GetInteraction("EVTExposeVoid", null, false);
								if (interaction != null && interaction.Triggered(this.co, this.co, false, false, false, true, null))
								{
									interaction.objUs = this.co;
									interaction.objThem = this.co;
									interaction.ApplyChain(null);
								}
							}
							else
							{
								this.co.ZeroCondAmount("IsVoidCooldown");
							}
						}
						foreach (JsonGasRespireData jsonGasRespireData in this.gr.aGases)
						{
							int num4 = FluidStrings.moleculeNames.IndexOf(jsonGasRespireData.strGasIn);
							double condAmount = condOwner.GetCondAmount(FluidStrings.pps[num4]);
							double num5 = Math.Max(0.01, (double)jsonGasRespireData.fGasPressTotalRef);
							double num6 = (double)jsonGasRespireData.fConvRate * condAmount * (double)(this.gr.fVol * fCoeff * num) / (0.008314000442624092 * num3);
							if (num6 > condOwner.GetCondAmount(FluidStrings.mol[num4]))
							{
								num6 = condOwner.GetCondAmount(FluidStrings.mol[num4]);
							}
							if (double.IsNaN(num6))
							{
								Debug.Log("NaN Found: " + num6);
								num6 = 0.0;
							}
							if (!gasContainer.IsVoid && num4 >= 0 && num4 < FluidStrings.mol.Count)
							{
								if (gasContainer.mapDGasMols.ContainsKey(FluidStrings.mol[num4]))
								{
									Dictionary<string, double> mapDGasMols;
									string key;
									(mapDGasMols = gasContainer.mapDGasMols)[key = FluidStrings.mol[num4]] = mapDGasMols[key] + -num6;
								}
								else
								{
									gasContainer.mapDGasMols[FluidStrings.mol[num4]] = -num6;
								}
								if (gasContainer.mapDGasMols[FluidStrings.mol[num4]] != 0.0)
								{
									condOwner.GasChanged = true;
									this._lastActivePumpAir = StarSystem.fEpoch;
								}
								gasContainer.Run(false);
							}
							double num7 = condAmount / num2 * (num2 / num5);
							jsonGasRespireData.GetLoot().ApplyCondLoot(this.co, (float)((double)jsonGasRespireData.fStatRate * num6 * num7), null, 0f);
							if (condOwner2 != null)
							{
								GasContainer gasContainer2 = condOwner2.GasContainer;
								if (gasContainer2 != null && !gasContainer2.IsVoid)
								{
									int num8 = FluidStrings.moleculeNames.IndexOf(jsonGasRespireData.strGasOut);
									if (num8 >= 0 && num8 < FluidStrings.mol.Count)
									{
										if (gasContainer2.mapDGasMols.ContainsKey(FluidStrings.mol[num8]))
										{
											Dictionary<string, double> mapDGasMols;
											string key2;
											(mapDGasMols = gasContainer2.mapDGasMols)[key2 = FluidStrings.mol[num8]] = mapDGasMols[key2] + num6;
										}
										else
										{
											gasContainer2.mapDGasMols[FluidStrings.mol[num8]] = num6;
										}
										if (gasContainer2.mapDGasMols[FluidStrings.mol[num8]] != 0.0)
										{
											condOwner2.GasChanged = true;
											this._lastActivePumpAir = StarSystem.fEpoch;
										}
										gasContainer2.Run(false);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	public void SetData(JsonGasRespire jgr, bool bRespire, bool bPump, string strCOKey)
	{
		if (jgr == null)
		{
			return;
		}
		this.gr = jgr;
		if (this.gr.aGases == null)
		{
			this.gr.aGases = new JsonGasRespireData[0];
		}
		this.bRespire = bRespire;
		this.bPump = bPump;
		this.strCOKey = strCOKey;
		this.CatchUp();
		this.ctA = DataHandler.GetCondTrigger(this.gr.strCTA);
		this.ctB = DataHandler.GetCondTrigger(this.gr.strCTB);
		this.ctSignalMain = DataHandler.GetCondTrigger(this.gr.strSignalCTMain);
		this.ctSignalA = DataHandler.GetCondTrigger(this.gr.strSignalCTA);
		this.ctSignalB = DataHandler.GetCondTrigger(this.gr.strSignalCTB);
	}

	public bool bUpdateRemote;

	private CondOwner co;

	private JsonGasRespire gr;

	private CondTrigger ctA;

	private CondTrigger ctB;

	private CondTrigger ctSignalMain;

	private CondTrigger ctSignalA;

	private CondTrigger ctSignalB;

	private string strRemoteID;

	private bool bRespire;

	private bool bPump;

	private string strCOKey;

	private double fTimeOfLastSignalCheck;

	private double _lastActivePump;

	private double _lastActivePumpAir;

	private AudioEmitter _audioEmitterAir;

	private AudioEmitter _audioEmitter;
}
