using System;
using System.Collections.Generic;
using UnityEngine;

// Fusion reactor integrated controller.
// Likely attached to the reactor assembly on a ship and ticked in-scene to
// read module conditions, consume reactants, and update reactor-side alarms.
public class FusionIC : MonoBehaviour
{
	// Creates the local reactor radiation/alarm emitter and binds its audio preset.
	private void Awake()
	{
		this.goAlarmRad = new GameObject("Reactor Alarm Rad");
		this.goAlarmRad.transform.position = base.transform.position;
		this.goAlarmRad.transform.SetParent(base.transform);
		this.aeAlarm = this.goAlarmRad.AddComponent<AudioEmitter>();
		this.aeAlarm.SetData(DataHandler.GetAudioEmitter("ShipRadAlarm"));
	}

	// Main runtime tick for the fusion controller.
	// It waits for a valid ship, lazily initializes module references, then
	// runs the reactor simulation and no-work-zone checks on separate timers.
	private void Update()
	{
		if (this.COSelf.ship == null)
		{
			return;
		}
		if (this._ctFuse == null)
		{
			this.Init();
		}
		if (StarSystem.fEpoch >= this.fTimeNextRun)
		{
			this.Run(StarSystem.fEpoch - this.fTimeLastRun);
		}
		if (StarSystem.fEpoch >= this.fTimeNextNWZ)
		{
			this.CheckNWZ();
		}
	}

	// One-time setup for cached timers, ignition state, and the reactor module hardpoints.
	// `Module01`..`Module32` appear to be map points on the parent CondOwner.
	private void Init()
	{
		this.fTimeLastRun = StarSystem.fEpoch;
		this.fTimeNextRun = StarSystem.fEpoch + 0.27000001072883606;
		if (this._ctFuse == null)
		{
			this._ctFuse = this.CTFuse;
		}
		if (this.COSelf.HasCond("IsReadyFusion"))
		{
			this.bIgnition = true;
		}
		if (!this.COSelf.HasCond("StatICABLWall"))
		{
			this.COSelf.AddCondAmount("StatICABLWall", 1.0, 0.0, 0f);
		}
		string arg = "Module";
		this.aModulePoints = new List<string>();
		for (int i = 1; i < 33; i++)
		{
			string text = arg + ((i >= 10) ? string.Empty : "0") + i;
			if (!this.COSelf.mapPoints.ContainsKey(text))
			{
				break;
			}
			this.aModulePoints.Add(text);
		}
	}

	// Catch-up hook used when the controller needs to be forced through one immediate update.
	public void CatchUp()
	{
		this.Update();
	}

	// Adds a module to a typed working list if it matches the expected condition tag.
	// These tags (`IsFusionLaserArray`, `IsFusionCryoPump`, etc.) are likely
	// condition ids from the condition registry.
	private bool AddModule(CondOwner co, string strCond, List<CondOwner> aCOs)
	{
		if (!co.HasCond(strCond))
		{
			return false;
		}
		if (aCOs.IndexOf(co) >= 0)
		{
			return false;
		}
		aCOs.Add(co);
		return true;
	}

	// Core reactor simulation step.
	// Likely reads GUI knob state from the reactor panels, rebuilds the nearby
	// module list when needed, then computes capacitor charge, fuel flow, and wear.
	private void Run(double fDeltaTime)
	{
		this.nKnobStateBus = 0;
		this.bCryoState = false;
		this.bCoilFwdState = false;
		this.bCoilRearState = false;
		this.nCryos = 0;
		this.nCryoTanks = 0;
		this.nPellets = 0;
		this.nLasers = 0;
		this.nFuelRegs = 0;
		if (this.COSelf.ship != null && this.COSelf.Pwr != null && this.COSelf.Pwr.jsonPI != null)
		{
			this.nKnobStateBus = (int)Convert.ToInt16(this.COSelf.GetGPMInfo("Panel A", "knobBus"));
			float num = Convert.ToSingle(this.COSelf.GetGPMInfo("Panel A", "knobRatio"));
			if (num != 1f)
			{
				num = 0f;
				if (this.bIgnition)
				{
					this.COSelf.mapGUIPropMaps["Panel A"]["slidCycle"] = "0";
				}
			}
			if (this.fReactantD < 0.0 || this.fReactantHe3 < 0.0)
			{
				this.GetReactants();
			}
			if (this.aModules != null)
			{
				foreach (CondOwner condOwner in this.aModules)
				{
					if (condOwner == null || condOwner.bDestroyed || condOwner.ship == null)
					{
						this.aModules = null;
						break;
					}
				}
			}
			if (this.aModules == null || this.COSelf.ship.bCheckFusion)
			{
				this.aModules = new List<CondOwner>();
				foreach (string strPointName in this.aModulePoints)
				{
					this.COSelf.ship.GetCOsAtWorldCoords1(this.COSelf.GetPos(strPointName, false), this.CTModule, false, false, this.aModules);
				}
				this.aLasers = new List<CondOwner>();
				this.aMHDs = new List<CondOwner>();
				this.aCryos = new List<CondOwner>();
				this.aPellets = new List<CondOwner>();
				this.aCorePumps = new List<CondOwner>();
				this.aCoils = new List<CondOwner>();
				this.aCaps = new List<CondOwner>();
				this.aFuelRegs = new List<CondOwner>();
				foreach (CondOwner condOwner2 in this.aModules)
				{
					if (!this.AddModule(condOwner2, "IsFusionLaserArray", this.aLasers))
					{
						if (!this.AddModule(condOwner2, "IsFusionPelletFeeder", this.aPellets))
						{
							if (!this.AddModule(condOwner2, "IsFusionCryoPump", this.aCryos))
							{
								if (!this.AddModule(condOwner2, "IsFusionCapacitor", this.aCaps))
								{
									if (!this.AddModule(condOwner2, "IsFusionFuelRegulator", this.aFuelRegs))
									{
										if (!this.AddModule(condOwner2, "IsFusionCorePump", this.aCorePumps))
										{
											if (!this.AddModule(condOwner2, "IsFusionFieldCoils", this.aCoils))
											{
												if (this.AddModule(condOwner2, "IsFusionMHDGenerator", this.aMHDs))
												{
												}
											}
										}
									}
								}
							}
						}
					}
				}
				this.COSelf.ship.bCheckFusion = false;
			}
			double num2 = 0.0;
			int num3 = 0;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			bool flag;
			if (this.nKnobStateBus >= 1)
			{
				double num7 = 1.0 - this.COSelf.GetCondAmount("StatICCapA");
				if (this.aCaps.Count == 0)
				{
					this.COSelf.ZeroCondAmount("StatICCapA");
				}
				else if (!this.bIgnition && num7 > 0.004000000189989805)
				{
					num7 /= 10.0;
					num7 = MathUtils.Clamp(num7, 0.04, 0.1);
					num7 *= fDeltaTime;
					num7 *= (double)this.aCaps.Count;
					this.COSelf.AddCondAmount("StatICCapA", num7 / (double)this.aCaps.Count, 0.0, 0f);
					num2 += num7 * 0.33899998664855957 * (double)this.aCaps.Count;
				}
				else if (this.aLasers.Count != 0)
				{
					num6 = 0f;
					foreach (CondOwner condOwner3 in this.aLasers)
					{
						this.nLasers++;
						float num8 = 1f - (float)(condOwner3.GetCondAmount("StatDamage") / condOwner3.GetCondAmount("StatDamageMax"));
						if (double.IsNaN((double)num8))
						{
							num8 = 1f;
						}
						num6 += num8;
						this.WearModule(condOwner3, fDeltaTime);
					}
					if (num6 > (float)this.aCaps.Count * 2f)
					{
						num6 = (float)this.aCaps.Count * 2f;
					}
					if ((float)this.nLasers > (float)this.aCaps.Count * 2f)
					{
						this.nLasers = this.aCaps.Count * 2;
					}
				}
				num3 = (int)Convert.ToInt16(this.COSelf.GetGPMInfo("Panel A", "knobPump"));
				if (num3 != 0 && this.aCorePumps.Count > 0)
				{
					int num9 = 0;
					foreach (CondOwner condOwner4 in this.aCorePumps)
					{
						if (!condOwner4.HasCond("IsOff"))
						{
							num9++;
						}
						condOwner4.ZeroCondAmount("IsOverrideOff");
						condOwner4.SetCondAmount("StatPower", 0.00015, 0.0);
						this.WearModule(condOwner4, fDeltaTime);
					}
					num7 = this.COSelf.GetCondAmount("StatICPressureA");
					if ((num3 == 1 && num7 > 0.3499999940395355) || (num3 == 2 && num7 > 0.10000000149011612))
					{
						double dAmount = num7 - num7 * fDeltaTime / 12.0 * (double)num9;
						this.COSelf.SetCondAmount("StatICPressureA", dAmount, 0.0);
						this.COSelf.SetCondAmount("StatICCoreTemp", dAmount, 0.0);
					}
					num2 += 7.599999662488699E-05 * fDeltaTime * (double)num9;
				}
				else
				{
					foreach (CondOwner condOwner5 in this.aCorePumps)
					{
						condOwner5.SetCondAmount("IsOverrideOff", 1.0, 0.0);
						condOwner5.ZeroCondAmount("StatPower");
					}
				}
				this.bCryoState = Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkCryo"));
				if (this.bCryoState && this.aCryos.Count > 0)
				{
					num4 = 0f;
					foreach (CondOwner condOwner6 in this.aCryos)
					{
						if (!condOwner6.HasCond("IsOff"))
						{
							this.nCryos++;
							float num8 = 1f - (float)(condOwner6.GetCondAmount("StatDamage") / condOwner6.GetCondAmount("StatDamageMax"));
							if (double.IsNaN((double)num8))
							{
								num8 = 1f;
							}
							num4 += num8 * 0.6f;
						}
						condOwner6.ZeroCondAmount("IsOverrideOff");
						condOwner6.SetCondAmount("StatPower", 0.00015, 0.0);
						this.WearModule(condOwner6, fDeltaTime);
					}
					num2 += 7.599999662488699E-05 * fDeltaTime * (double)this.nCryos;
				}
				else
				{
					foreach (CondOwner condOwner7 in this.aCryos)
					{
						condOwner7.SetCondAmount("IsOverrideOff", 1.0, 0.0);
						condOwner7.ZeroCondAmount("StatPower");
					}
				}
				List<CondOwner> triggeredCOListByType = CrewSim.objInstance.coDicts.GetTriggeredCOListByType(this.CTCryo, "ItmCanisterLHe01");
				this.nCryoTanks = triggeredCOListByType.Count;
				if (this.nCryoTanks == 0)
				{
					num4 = 0f;
				}
				flag = Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkPellet"));
				if (flag && this.aPellets.Count > 0)
				{
					num5 = 0f;
					foreach (CondOwner condOwner8 in this.aPellets)
					{
						if (!condOwner8.HasCond("IsOff"))
						{
							this.nPellets++;
							float num8 = 1f - (float)(condOwner8.GetCondAmount("StatDamage") / condOwner8.GetCondAmount("StatDamageMax"));
							if (double.IsNaN((double)num8))
							{
								num8 = 1f;
							}
							num5 += num8;
						}
						condOwner8.ZeroCondAmount("IsOverrideOff");
						condOwner8.SetCondAmount("StatPower", 5.6E-05, 0.0);
						this.WearModule(condOwner8, fDeltaTime);
					}
					if (this.nPellets > 0)
					{
						num2 += 7.599999662488699E-05 * fDeltaTime * (double)this.nPellets;
						this.COSelf.SetCondAmount("StatICReadyPellFeed", (double)num5, 0.0);
					}
				}
				else
				{
					this.COSelf.ZeroCondAmount("StatICReadyPellFeed");
					foreach (CondOwner condOwner9 in this.aPellets)
					{
						condOwner9.SetCondAmount("IsOverrideOff", 1.0, 0.0);
						condOwner9.ZeroCondAmount("StatPower");
					}
				}
				this.bCoilFwdState = Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkCoilFwd"));
				this.bCoilRearState = Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkCoilRear"));
				if ((this.bCoilFwdState || this.bCoilRearState) && this.aCoils.Count > 0)
				{
					this.nCoils = 0;
					foreach (CondOwner condOwner10 in this.aCoils)
					{
						if (!condOwner10.HasCond("IsOff"))
						{
							this.nCoils++;
						}
						condOwner10.ZeroCondAmount("IsOverrideOff");
						condOwner10.SetCondAmount("StatPower", 0.022, 0.0);
						this.WearModule(condOwner10, fDeltaTime);
					}
					if (this.bCoilFwdState && this.bCoilRearState)
					{
						this.nCoils *= 2;
					}
					double num10 = 5700.0 * fDeltaTime / 3600.0 * (double)this.nCoils;
					num2 += num10;
				}
				else
				{
					foreach (CondOwner condOwner11 in this.aCoils)
					{
						condOwner11.SetCondAmount("IsOverrideOff", 1.0, 0.0);
						condOwner11.ZeroCondAmount("StatPower");
					}
				}
				flag = Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkAlign"));
				if (flag)
				{
					num2 += 7.599999662488699E-05 * fDeltaTime;
					this.COSelf.SetCondAmount("StatICReadyLasAlign", 1.0, 0.0);
				}
				else
				{
					this.COSelf.ZeroCondAmount("StatICReadyLasAlign");
				}
				flag = Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkFuelReg"));
				if (flag)
				{
					foreach (CondOwner condOwner12 in this.aFuelRegs)
					{
						if (!condOwner12.HasCond("IsOff"))
						{
							this.nFuelRegs++;
						}
						condOwner12.ZeroCondAmount("IsOverrideOff");
						condOwner12.SetCondAmount("StatPower", 0.00016, 0.0);
						this.WearModule(condOwner12, fDeltaTime);
					}
					if (this.nFuelRegs > 0)
					{
						num2 += 7.599999662488699E-05 * fDeltaTime * (double)this.nFuelRegs;
					}
				}
				if (num5 > (float)this.nFuelRegs * 2f)
				{
					num5 = (float)this.nFuelRegs * 2f;
				}
				if ((float)this.nPellets > (float)this.nFuelRegs * 2f)
				{
					this.nPellets = this.nFuelRegs * 2;
				}
				flag = Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkIgnition"));
				if (flag)
				{
					if (!this.bIgnition)
					{
						if (this.COSelf.HasCond("IsReadyFusion"))
						{
							this.bIgnition = true;
							this.COSelf.ApplyGPMChanges(new string[]
							{
								"Panel A,knobPump,0"
							});
						}
						else if (this.COSelf.GetCondAmount("StatICCapA") > 0.949999988079071 && this.COSelf.GetCondAmount("StatICPressureA") < 0.15000000596046448 && this.COSelf.HasCond("StatICReadyLasAlign") && this.COSelf.HasCond("StatICReadyPellFeed"))
						{
							this.COSelf.AddCondAmount("IsReadyFusionIgnition", 1.0, 0.0, 0f);
							this.COSelf.ZeroCondAmount("IsPowered");
						}
						else
						{
							this.ShutDown();
						}
					}
					this.COSelf.ZeroCondAmount("StatICCapA");
				}
			}
			else
			{
				this.COSelf.SetCondAmount("IsOverrideOff", 1.0, 0.0);
			}
			double num11 = fDeltaTime;
			if (fDeltaTime > 1.0)
			{
				num11 = 1.0;
			}
			double num12 = 0.0;
			double num13 = this.COSelf.GetCondAmount("StatICCoreTemp");
			double num14 = Convert.ToDouble(this.COSelf.GetGPMInfo("Panel A", "slidCycle"));
			this.COSelf.SetCondAmount("StatICThrustThrottle", num14 * (double)num, 0.0);
			num13 -= num14 * 0.2 * num11;
			this.fPelletMax = (double)(Mathf.Min(num5, num6) * 2f);
			this.COSelf.SetCondAmount("StatICPellMax", this.fPelletMax, 0.0);
			this.COSelf.SetCondAmount("StatICPellMaxTheory", (double)(Mathf.Min(this.nPellets, this.nLasers) * 2), 0.0);
			double num15 = Convert.ToDouble(this.COSelf.GetGPMInfo("Panel A", "slidFlow"));
			if (this.fPelletMax == 0.0)
			{
				num15 = 0.0;
			}
			else
			{
				float a = 1f;
				if (this.nKnobStateBus > 0 && this.bIgnition)
				{
					double num16 = MathUtils.Min(num13 / 0.7250000238418579, 1.0);
					a = Mathf.Lerp((float)this.fPelletMax, 0.001f, (float)num16);
				}
				num15 = (double)Mathf.Lerp(a, (float)this.fPelletMax, (float)num15);
			}
			this.COSelf.SetCondAmount("StatICPellRate", num15, 0.0);
			if (this.nKnobStateBus > 0 && this.bIgnition)
			{
				num13 += num15 * 0.04 * num11;
				if (this.nCryos > 0 && num13 > 0.7250000238418579)
				{
					double num17 = 0.08 * num11 * (double)num4;
					if (num13 - 0.7250000238418579 > num17)
					{
						num13 -= num17;
					}
					else
					{
						num13 = 0.7250000238418579;
					}
				}
				double num18 = num13 / 0.7250000238418579;
				num12 = 0.3499999940395355 * num18;
			}
			else if (this.bCryoState)
			{
				num13 -= 0.08 * num11 * (double)num4;
			}
			if (this.bCryoState)
			{
				this.COSelf.SetCondAmount("StatICCryoMult", (double)num4, 0.0);
			}
			else
			{
				this.COSelf.ZeroCondAmount("StatICCryoMult");
			}
			this.COSelf.SetCondAmount("StatICCoreTemp", num13, 0.0);
			this.COSelf.SetCondAmount("StatICPressureA", num13, 0.0);
			this.COSelf.SetCondAmount("StatICPwrTotal", num12, 0.0);
			num12 /= 2.0;
			this.COSelf.SetCondAmount("StatICPwrFus", num12, 0.0);
			flag = (this.nKnobStateBus > 0 && Convert.ToBoolean(this.COSelf.GetGPMInfo("Panel A", "chkMHDOn")));
			if (flag && this.aMHDs.Count > 0)
			{
				foreach (CondOwner condOwner13 in this.aMHDs)
				{
					if (!condOwner13.HasCond("IsOff"))
					{
						this.nCoils++;
					}
					condOwner13.ZeroCondAmount("IsOverrideOff");
					condOwner13.SetCondAmount("StatPower", 2.0, 0.0);
					this.WearModule(condOwner13, fDeltaTime);
				}
				num3 = (int)Convert.ToInt16(this.COSelf.GetGPMInfo("Panel A", "knobRatio"));
				if (num3 == 0)
				{
					this.COSelf.SetCondAmount("StatICPwrMHD", num12 * 1.0, 0.0);
					this.COSelf.SetCondAmount("StatICPwrThrust", num12 * 0.0, 0.0);
					this.bMHDOn = true;
				}
				else if (num3 == 1)
				{
					this.COSelf.SetCondAmount("StatICPwrMHD", num12 * 0.050000011920928955, 0.0);
					this.COSelf.SetCondAmount("StatICPwrThrust", num12 * 0.949999988079071, 0.0);
					this.bMHDOn = false;
				}
				this.COSelf.SetCondAmount("IsReadyRecharge", 1.0, 0.0);
			}
			else
			{
				this.COSelf.SetCondAmount("StatICPwrMHD", 0.0, 0.0);
				this.COSelf.SetCondAmount("StatICPwrThrust", num12, 0.0);
				this.COSelf.ZeroCondAmount("IsReadyRecharge");
				this.bMHDOn = false;
				foreach (CondOwner condOwner14 in this.aMHDs)
				{
					condOwner14.SetCondAmount("IsOverrideOff", 1.0, 0.0);
					condOwner14.ZeroCondAmount("StatPower");
				}
			}
			this.COSelf.AddCondAmount("StatICPwrLoad", this.COSelf.GetCondAmount("StatICPwrFus") + num2 / 1000000.0 - this.COSelf.GetCondAmount("StatICPwrLoad"), 0.0, 0f);
			double num19 = (num13 - 0.75) * fDeltaTime;
			if (this.bIgnition && (!this.bCoilFwdState || this.aCoils.Count == 0) && !this.COSelf.HasCond("IsOverrideOff"))
			{
				num19 = num13 * fDeltaTime * 10.0;
			}
			if (num19 <= 0.0)
			{
				num19 = 0.0;
			}
			else
			{
				Debug.LogWarning("WARNING: Fusion reactor damage! " + num19);
			}
			this.COSelf.AddCondAmount("StatICABLWall", num19, 0.0, 0f);
			num2 -= this.COSelf.GetCondAmount("StatICPwrTotal") - this.COSelf.GetCondAmount("StatICPwrLoad");
			if (this.nKnobStateBus != 2 && num2 < 0.0)
			{
				num2 = 0.0;
			}
			if (num2 > 0.0)
			{
				this.COSelf.Pwr.UserPowerExt(num2);
			}
			if (this.CTFuse != null && this.CTFuse.Triggered(this.COSelf, null, true))
			{
				this.Fusion(fDeltaTime, this.COSelf.Pwr.jsonPI, num19);
			}
			else if (this.CTShutDown != null && this.CTShutDown.Triggered(this.COSelf, null, true))
			{
				this.ShutDown();
			}
			else if (this.COSelf.HasCond("IsOff"))
			{
				double num20 = this.COSelf.GetCondAmount("StatICCoreTemp");
				num20 = 0.25 * num20 * (double)CrewSim.TimeElapsedScaled();
				this.COSelf.AddCondAmount("StatICCoreTemp", -num20, 0.0, 0f);
			}
		}
		this.fTimeLastRun = StarSystem.fEpoch;
		this.fTimeNextRun = StarSystem.fEpoch + 0.27000001072883606;
	}

	private void WearModule(CondOwner co, double fDeltaTime)
	{
		double num = 1.5844382307706396E-09 * fDeltaTime;
		num *= co.GetCondAmount("StatDamageMax");
		num = MathUtils.Rand(0.0, num, MathUtils.RandType.Flat, null);
		this.DamageModule(co, num);
	}

	private void DamageModule(CondOwner co, double fDamage)
	{
		co.AddCondAmount("StatDamage", fDamage, 0.0, 0f);
		if (co.Item != null)
		{
			co.Item.VisualizeOverlays(false);
		}
	}

	public void GetReactants()
	{
		List<CondOwner> triggeredCOListByType = CrewSim.objInstance.coDicts.GetTriggeredCOListByType(this.CTReactantA, "ItmCanisterLH02");
		List<CondOwner> triggeredCOListByType2 = CrewSim.objInstance.coDicts.GetTriggeredCOListByType(this.CTReactantB, "ItmCanisterLHe02");
		this.fReactantD = 0.0;
		this.fReactantHe3 = 0.0;
		foreach (CondOwner condOwner in triggeredCOListByType)
		{
			if (this.COSelf.ship == condOwner.ship)
			{
				double condAmount = condOwner.GetCondAmount(FusionIC.aReactantNames[0]);
				this.fReactantD += condAmount;
			}
		}
		foreach (CondOwner condOwner2 in triggeredCOListByType2)
		{
			if (this.COSelf.ship == condOwner2.ship)
			{
				double condAmount2 = condOwner2.GetCondAmount(FusionIC.aReactantNames[1]);
				this.fReactantHe3 += condAmount2;
			}
		}
	}

	private void Fusion(double fDeltaTime, JsonPowerInfo jpi, double fWallDMG)
	{
		fDeltaTime = MathUtils.Max(fDeltaTime, 1E-05);
		List<CondOwner> triggeredCOListByType = CrewSim.objInstance.coDicts.GetTriggeredCOListByType(this.CTReactantA, "ItmCanisterLH02");
		List<CondOwner> triggeredCOListByType2 = CrewSim.objInstance.coDicts.GetTriggeredCOListByType(this.CTReactantB, "ItmCanisterLHe02");
		double num = this.COSelf.GetCondAmount("StatICPwrTotal");
		double condAmount = this.COSelf.GetCondAmount("StatICPwrThrust");
		double num2 = this.COSelf.GetCondAmount("StatICVe") / 70500000.0;
		double condAmount2 = this.co.GetCondAmount("StatICThrustThrottle");
		double condAmount3 = this.co.GetCondAmount("StatICPellRate");
		double condAmount4 = this.COSelf.GetCondAmount("StatICCoreTemp");
		if (condAmount4 > 0.7250000238418579)
		{
			num = 0.3499999940395355;
		}
		double num3 = 2.0 * (num * 1000000000000.0) * num2 / 70500000.0 / 70500000.0 * fDeltaTime * 393.06358381502895;
		double num4 = num3 * condAmount3 * (double)FusionIC.aReactantAmounts[0];
		double num5 = num3 * condAmount3 * (double)FusionIC.aReactantAmounts[1];
		double num6 = 0.0;
		double num7 = 0.0;
		double num8 = 699999988079.071 * num2 / 70500000.0 / 70500000.0 * 393.06358381502895 * this.fPelletMax;
		this.COSelf.ship.fFusionThrustMax = 332499990165.2337 * num2 / 70500000.0 * this.fPelletMax * 393.06358381502895;
		double num9 = (double)this.COSelf.ship.GetMaxTorchThrust((float)condAmount2) * this.COSelf.ship.Mass / 6.6845869117759804E-12;
		num9 *= condAmount4 / 0.7250000238418579;
		this.COSelf.ship.SetThrust(num9);
		if (this.COSelf.ship.objSS == null || !this.COSelf.ship.objSS.HasNavData())
		{
			this.COSelf.SetCondAmount("StatICDRate", num4 / fDeltaTime, 0.0);
			this.COSelf.SetCondAmount("StatICHe3Rate", num5 / fDeltaTime, 0.0);
		}
		this.fReactantD = 0.0;
		this.fReactantHe3 = 0.0;
		foreach (CondOwner condOwner in triggeredCOListByType)
		{
			if (this.COSelf.ship == condOwner.ship)
			{
				double condAmount5 = condOwner.GetCondAmount(FusionIC.aReactantNames[0]);
				this.fReactantD += condAmount5;
				if (num4 > 0.0)
				{
					if (condAmount5 >= num4)
					{
						condOwner.AddCondAmount(FusionIC.aReactantNames[0], -num4, 0.0, 0f);
						num6 += num4;
						num4 = 0.0;
					}
					else
					{
						num4 -= condAmount5;
						num6 += condAmount5;
						condOwner.AddCondAmount(FusionIC.aReactantNames[0], -condAmount5, 0.0, 0f);
					}
				}
			}
		}
		this.fReactantD -= num6;
		this.COSelf.ship.fShallowFusionRemain = this.fReactantD / (num8 * (double)FusionIC.aReactantAmounts[0]);
		if (double.IsNaN(this.COSelf.ship.fShallowFusionRemain))
		{
			this.COSelf.ship.fShallowFusionRemain = 0.0;
		}
		foreach (CondOwner condOwner2 in triggeredCOListByType2)
		{
			if (this.COSelf.ship == condOwner2.ship)
			{
				double condAmount6 = condOwner2.GetCondAmount(FusionIC.aReactantNames[1]);
				this.fReactantHe3 += condAmount6;
				if (num5 > 0.0)
				{
					if (condAmount6 >= num5)
					{
						condOwner2.AddCondAmount(FusionIC.aReactantNames[1], -num5, 0.0, 0f);
						num7 += num5;
						num5 = 0.0;
					}
					else
					{
						num5 -= condAmount6;
						num7 += condAmount6;
						condOwner2.AddCondAmount(FusionIC.aReactantNames[1], -condAmount6, 0.0, 0f);
					}
				}
			}
		}
		this.fReactantHe3 -= num7;
		double num10 = this.fReactantHe3 / (num8 * (double)FusionIC.aReactantAmounts[1]);
		if (num10 < this.COSelf.ship.fShallowFusionRemain)
		{
			this.COSelf.ship.fShallowFusionRemain = num10;
		}
		if (double.IsNaN(this.COSelf.ship.fShallowFusionRemain))
		{
			this.COSelf.ship.fShallowFusionRemain = 0.0;
		}
		if (condAmount3 == 0.0 || num4 != 0.0 || num5 != 0.0 || condAmount4 <= 0.0)
		{
			this.ShutDown();
		}
		else
		{
			this.COSelf.SetCondAmount("StatPower", jpi.fAmount, 0.0);
			if (this.COSelf.HasCond("IsTorchDrive"))
			{
				if (!this.COSelf.ship.bFusionReactorRunning)
				{
					this.COSelf.ship.bChangedStatus = true;
				}
				this.COSelf.ship.bFusionReactorRunning = true;
				if (this.COSelf.ship.objSS.vAccIn.x != 0f || this.COSelf.ship.objSS.vAccIn.y != 0f)
				{
					this.COSelf.ship.UnlockFromOrbit(true);
				}
			}
		}
		if (this.COSelf.HasCond("DcABLWall04") && this.bIgnition)
		{
			this.DamageModule(this.COSelf, fWallDMG);
			if (!this.aeAlarm.PlayingSteady)
			{
				this.aeAlarm.FadeInSteady(-1f, -1f);
			}
			foreach (CondOwner condOwner3 in this.COSelf.ship.GetPeople(true))
			{
				condOwner3.AddCondAmount("StatRad", fWallDMG, 0.0, 0f);
			}
		}
		else if (this.aeAlarm.PlayingSteady)
		{
			this.aeAlarm.StopSteady();
		}
		this.WearModule(this.COSelf, fDeltaTime);
	}

	public void SetReactants(double fShallowFusionRemain)
	{
		if (double.IsNaN(fShallowFusionRemain))
		{
			return;
		}
		List<CondOwner> triggeredCOListByType = CrewSim.objInstance.coDicts.GetTriggeredCOListByType(this.CTReactantA, "ItmCanisterLH02");
		List<CondOwner> triggeredCOListByType2 = CrewSim.objInstance.coDicts.GetTriggeredCOListByType(this.CTReactantB, "ItmCanisterLHe02");
		this.fReactantD = 0.0;
		this.fReactantHe3 = 0.0;
		double num = 2.0 * this.COSelf.ship.fFusionThrustMax * 70500000.0 / 0.949999988079071 / 70500000.0 / 70500000.0;
		if (double.IsNaN(num))
		{
			return;
		}
		foreach (CondOwner condOwner in triggeredCOListByType)
		{
			if (this.COSelf.ship == condOwner.ship)
			{
				double condAmount = condOwner.GetCondAmount(FusionIC.aReactantNames[0]);
				this.fReactantD += condAmount;
			}
		}
		double num2 = this.fReactantD / (num * (double)FusionIC.aReactantAmounts[0]);
		foreach (CondOwner condOwner2 in triggeredCOListByType2)
		{
			if (this.COSelf.ship == condOwner2.ship)
			{
				double condAmount2 = condOwner2.GetCondAmount(FusionIC.aReactantNames[1]);
				this.fReactantHe3 += condAmount2;
			}
		}
		double num3 = this.fReactantHe3 / (num * (double)FusionIC.aReactantAmounts[1]);
		double num4;
		if (num2 < num3)
		{
			num4 = num2 - fShallowFusionRemain;
		}
		else
		{
			num4 = num3 - fShallowFusionRemain;
		}
		double fFusionRemove = num4 * num * (double)FusionIC.aReactantAmounts[0];
		this.RemoveFromTanks(fFusionRemove, triggeredCOListByType, FusionIC.aReactantNames[0], 1107f);
		double fFusionRemove2 = num4 * num * (double)FusionIC.aReactantAmounts[1];
		this.RemoveFromTanks(fFusionRemove2, triggeredCOListByType2, FusionIC.aReactantNames[1], 129.11f);
	}

	private void RemoveFromTanks(double fFusionRemove, List<CondOwner> aCOs, string strReactant, float fDensity)
	{
		if (fFusionRemove == 0.0 || double.IsNaN(fFusionRemove) || string.IsNullOrEmpty(strReactant) || aCOs == null)
		{
			return;
		}
		double num = 0.0;
		double num2 = 0.0;
		foreach (CondOwner condOwner in aCOs)
		{
			if (this.COSelf.ship == condOwner.ship)
			{
				double condAmount = condOwner.GetCondAmount(strReactant);
				num += condAmount;
				if (fFusionRemove > 0.0)
				{
					if (fFusionRemove > condAmount)
					{
						condOwner.ZeroCondAmount(strReactant);
						fFusionRemove -= condAmount;
					}
					else
					{
						condOwner.AddCondAmount(strReactant, -fFusionRemove, 0.0, 0f);
						fFusionRemove = 0.0;
					}
				}
				else
				{
					double num3 = condOwner.GetCondAmount("StatVolume") * (double)fDensity;
					if (num3 >= condAmount + -fFusionRemove)
					{
						condOwner.AddCondAmount(strReactant, -fFusionRemove, 0.0, 0f);
						fFusionRemove = 0.0;
					}
					else
					{
						fFusionRemove = -(num3 - condAmount);
						condOwner.SetCondAmount(strReactant, num3, 0.0);
					}
				}
				num2 += condOwner.GetCondAmount(strReactant);
			}
		}
		double num4 = 2.0 * this.COSelf.ship.fFusionThrustMax * 70500000.0 / 0.949999988079071 / 70500000.0 / 70500000.0;
		double num5 = (double)FusionIC.aReactantAmounts[1];
		if (strReactant == "StatLiqD2O")
		{
			num5 = (double)FusionIC.aReactantAmounts[0];
		}
	}

	private void CheckNWZ()
	{
		bool flag = false;
		if (CrewSim.system != null && this.COSelf.ship != null && this.COSelf.ship.objSS != null)
		{
			Ship nearestStation = CrewSim.system.GetNearestStation(this.COSelf.ship.objSS.vPosx, this.COSelf.ship.objSS.vPosy, true);
			if (nearestStation != null)
			{
				double distance = nearestStation.objSS.GetDistance(this.COSelf.ship.objSS);
				if (distance <= 2.005376018132665E-06)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.COSelf.mapGUIPropMaps["Panel A"]["bNWZ"] = "true";
			this.COSelf.mapGUIPropMaps["Panel A"]["knobRatio"] = "0";
			if (this.bIgnition)
			{
				this.COSelf.mapGUIPropMaps["Panel A"]["slidCycle"] = "0";
			}
		}
		else
		{
			this.COSelf.mapGUIPropMaps["Panel A"]["bNWZ"] = "false";
		}
		this.fTimeNextNWZ = StarSystem.fEpoch + 1.0;
	}

	private void ShutDown()
	{
		Interaction interaction = DataHandler.GetInteraction(this.COSelf.GetComponent<Powered>().jsonPI.strIntPowerOff, null, false);
		if (interaction != null && interaction.CTTestUs.Triggered(this.COSelf, null, true) && interaction.CTTestThem.Triggered(this.COSelf, null, true))
		{
			this.COSelf.QueueInteraction(this.co, interaction, true);
		}
		this.COSelf.ZeroCondAmount("StatPower");
		foreach (CondOwner condOwner in this.aModules)
		{
			condOwner.SetCondAmount("IsOverrideOff", 1.0, 0.0);
		}
		this.COSelf.AddCondAmount("IsShuttingDown", 1.0, 0.0, 0f);
		this.COSelf.mapInfo["Charge"] = this.COSelf.GetCondAmount("StatPower").ToString() + "kWh";
		if (this.COSelf.ship.bFusionReactorRunning)
		{
			this.COSelf.ship.bChangedStatus = true;
		}
		this.COSelf.ship.bFusionReactorRunning = false;
		this.COSelf.ZeroCondAmount("StatICDRate");
		this.COSelf.ZeroCondAmount("StatICHe3Rate");
		this.bIgnition = false;
		this.COSelf.ship.SetThrust(0.0);
		this.SetControlsOff();
	}

	private void SetControlsOff()
	{
		this.COSelf.mapGUIPropMaps["Panel A"]["knobBus"] = "0";
		this.COSelf.mapGUIPropMaps["Panel A"]["knobPump"] = "0";
		this.COSelf.mapGUIPropMaps["Panel A"]["knobRatio"] = "0";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkCryo"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkMHDOn"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkIgnition"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkPellet"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkAlign"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkCoilFwd"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkCoilRear"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["chkFuelReg"] = "false";
		this.COSelf.mapGUIPropMaps["Panel A"]["slidFlow"] = "0";
		this.COSelf.mapGUIPropMaps["Panel A"]["slidCycle"] = "0";
	}

	public void SetDerelict()
	{
		this.COSelf.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
		this.SetControlsOff();
		this.bIgnition = false;
	}

	public override string ToString()
	{
		return this.COSelf.ToString();
	}

	public FusionIC.ModuleStatus GetModuleStatus(FusionIC.Module mdl)
	{
		FusionIC.ModuleStatus result = FusionIC.ModuleStatus.Loading;
		if (this.aCaps == null)
		{
			return result;
		}
		result = FusionIC.ModuleStatus.Missing;
		switch (mdl)
		{
		case FusionIC.Module.Laser:
			if (this.aLasers.Count > 0)
			{
				result = FusionIC.ModuleStatus.On;
			}
			break;
		case FusionIC.Module.MHD:
			if (this.aMHDs.Count > 0)
			{
				result = FusionIC.ModuleStatus.On;
			}
			break;
		case FusionIC.Module.CryoPump:
			if (this.aCryos.Count != 0 && this.nCryoTanks != 0)
			{
				if (this.nCryos > 0)
				{
					result = FusionIC.ModuleStatus.On;
				}
				else
				{
					result = FusionIC.ModuleStatus.Off;
				}
			}
			break;
		case FusionIC.Module.PelletFeed:
			if (this.nPellets > 0)
			{
				result = FusionIC.ModuleStatus.On;
			}
			else if (this.aPellets.Count > 0)
			{
				result = FusionIC.ModuleStatus.Off;
			}
			break;
		case FusionIC.Module.CorePump:
			if (this.aCorePumps.Count > 0)
			{
				result = FusionIC.ModuleStatus.On;
			}
			break;
		case FusionIC.Module.Coil:
			if (this.aCoils.Count > 0)
			{
				if (this.bCoilFwdState && this.nCoils > 0)
				{
					result = FusionIC.ModuleStatus.On;
				}
				else
				{
					result = FusionIC.ModuleStatus.Off;
				}
			}
			break;
		case FusionIC.Module.Capacitor:
			if (this.aCaps.Count > 0)
			{
				result = FusionIC.ModuleStatus.On;
			}
			break;
		case FusionIC.Module.FuelReg:
			if (this.nFuelRegs > 0)
			{
				result = FusionIC.ModuleStatus.On;
			}
			else if (this.aFuelRegs.Count > 0)
			{
				result = FusionIC.ModuleStatus.Off;
			}
			break;
		}
		return result;
	}

	public double ReactantD
	{
		get
		{
			return this.fReactantD;
		}
	}

	public double ReactantHe3
	{
		get
		{
			return this.fReactantHe3;
		}
	}

	public double MassUsageMax
	{
		get
		{
			if (this.COSelf == null || this.COSelf.ship == null)
			{
				return 0.0;
			}
			return 2.0 * this.COSelf.ship.fFusionThrustMax * 70500000.0 / 0.949999988079071 / 70500000.0 / 70500000.0;
		}
	}

	public bool MHDOn
	{
		get
		{
			return this.bMHDOn;
		}
	}

	public double TimeLastRun
	{
		get
		{
			return this.fTimeLastRun;
		}
	}

	public double TimeNextRun
	{
		get
		{
			return this.fTimeNextRun;
		}
		set
		{
			this.fTimeNextRun = value;
		}
	}

	public CondTrigger CTModule
	{
		get
		{
			if (FusionIC._ctModule == null)
			{
				FusionIC._ctModule = DataHandler.GetCondTrigger("TIsFusionModule");
			}
			return FusionIC._ctModule;
		}
	}

	public CondTrigger CTFuse
	{
		get
		{
			if (this._ctFuse == null)
			{
				this._ctFuse = new CondTrigger();
				this._ctFuse.aReqs = new string[]
				{
					"IsReadyFusion"
				};
				this._ctFuse.aForbids = new string[]
				{
					"IsOverrideOff"
				};
			}
			return this._ctFuse;
		}
	}

	public CondTrigger CTShutDown
	{
		get
		{
			if (this._ctShutDown == null)
			{
				this._ctShutDown = new CondTrigger();
				this._ctShutDown.aReqs = new string[]
				{
					"IsOverrideOff"
				};
				this._ctShutDown.aForbids = new string[]
				{
					"IsOff",
					"IsShuttingDown"
				};
			}
			return this._ctShutDown;
		}
	}

	public CondTrigger CTReactantA
	{
		get
		{
			if (this._ctReactantA == null)
			{
				this._ctReactantA = new CondTrigger();
				this._ctReactantA.aReqs = new string[]
				{
					FusionIC.aReactantNames[0]
				};
			}
			return this._ctReactantA;
		}
	}

	public CondTrigger CTReactantB
	{
		get
		{
			if (this._ctReactantB == null)
			{
				this._ctReactantB = new CondTrigger();
				this._ctReactantB.aReqs = new string[]
				{
					FusionIC.aReactantNames[1]
				};
			}
			return this._ctReactantB;
		}
	}

	public CondTrigger CTCryo
	{
		get
		{
			if (this._ctCryo == null)
			{
				this._ctCryo = new CondTrigger();
				this._ctCryo.aReqs = new string[]
				{
					"StatLiqHe"
				};
			}
			return this._ctCryo;
		}
	}

	public CondOwner COSelf
	{
		get
		{
			if (this.co == null)
			{
				this.co = base.GetComponent<CondOwner>();
			}
			return this.co;
		}
	}

	private CondOwner co;

	private CondTrigger _ctFuse;

	private CondTrigger _ctShutDown;

	private CondTrigger _ctReactantA;

	private CondTrigger _ctReactantB;

	private CondTrigger _ctCryo;

	private double fTimeLastRun;

	private double fTimeNextRun;

	private double fTimeNextNWZ;

	private double fReactantD = -1.0;

	private double fReactantHe3 = -1.0;

	private double fPelletMax;

	private bool bIgnition;

	private bool bMHDOn;

	private int nKnobStateBus;

	private bool bCryoState;

	private bool bCoilFwdState;

	private bool bCoilRearState;

	private int nCoils;

	private int nCryos;

	private int nCryoTanks;

	private int nPellets;

	private int nLasers;

	private int nFuelRegs;

	private List<string> aModulePoints;

	private static CondTrigger _ctModule;

	private GameObject goAlarmRad;

	private AudioEmitter aeAlarm;

	private List<CondOwner> aModules;

	private List<CondOwner> aLasers;

	private List<CondOwner> aMHDs;

	private List<CondOwner> aCryos;

	private List<CondOwner> aPellets;

	private List<CondOwner> aCorePumps;

	private List<CondOwner> aCoils;

	private List<CondOwner> aCaps;

	private List<CondOwner> aFuelRegs;

	public static string[] aReactantNames = new string[]
	{
		"StatLiqD2O",
		"StatSolidHe3"
	};

	public static float[] aReactantAmounts = new float[]
	{
		0.667f,
		1f
	};

	public const float FUSION_PERIOD = 0.27f;

	public const float FUSION_CAP_CHARGED = 0.004f;

	public const float FUSION_CORE_IDEAL_MEV = 0.725f;

	public const float FUSION_POWER_FRACTION = 0.35f;

	public const float FUSION_VE = 70500000f;

	public const float FUSION_PRESSURE_VACUUM = 0.15f;

	private const double FUSION_FUDGE = 393.06358381502895;

	private const float FUSION_POWER_THRUST_MODE = 0.95f;

	private const float FUSION_POWER_MHD_MODE = 0f;

	public enum ModuleStatus
	{
		Loading,
		Missing,
		Off,
		On
	}

	public enum Module
	{
		Laser,
		MHD,
		CryoPump,
		PelletFeed,
		CorePump,
		Coil,
		Capacitor,
		FuelReg
	}
}
