using System;
using System.Collections.Generic;
using FFU_Beyond_Reach;
using MonoMod;
using UnityEngine;
public class patch_GUIHelmet : GUIHelmet
{
	private extern void orig_Init();
	private void Init()
	{
		this.orig_Init();
		this.ghTempInt.DangerLow = 289.15;
		this.ghTempInt.DangerHigh = 315.15;
	}
	[MonoModReplace]
	public void UpdateUI(CondOwner coRoomIn, CondOwner coRoomOut)
	{
		bool flag = coRoomIn == null || !coRoomIn.HasCond("IsHuman");
		if (flag)
		{
			base.Style = 0;
			base.Visible = false;
		}
		else
		{
			bool flag2 = coRoomOut == null;
			if (!flag2)
			{
				bool flag3 = !this.bInit;
				if (flag3)
				{
					this.Init();
				}
				base.Visible = true;
				List<CondOwner> list = new List<CondOwner>();
				bool flag4 = true;
				bool flag5 = true;
				bool flag6 = false;
				bool flag7 = false;
				bool flag8 = coRoomIn.HasCond("IsEVAHUD");
				if (flag8)
				{
					list = coRoomIn.compSlots.GetCOs("shirt_out", false, this.ctEVA);
					bool flag9 = list.Count > 0;
					if (flag9)
					{
						CondOwner condOwner = list[0];
						flag6 = true;
						bool flag10 = flag6;
						if (flag10)
						{
							list = condOwner.GetCOs(false, null);
							bool flag11 = false;
							bool flag12 = false;
							bool flag13 = list != null;
							if (flag13)
							{
								double num = 0.0;
								double num2 = 0.0;
								double num3 = 0.0;
								double num4 = 0.0;
								double num5 = 0.0;
								double num6 = 0.0;
								foreach (CondOwner condOwner2 in list)
								{
									bool flag14 = this.ctEVABottle.Triggered(condOwner2, null, false);
									if (flag14)
									{
										bool flag15 = !flag11;
										if (flag15)
										{
											flag11 = true;
										}
										double condAmount = condOwner2.GetCondAmount("StatGasMolO2");
										double condAmount2 = condOwner2.GetCondAmount("StatRef");
										bool showEachO2Battery = FFU_BR_Defs.ShowEachO2Battery;
										if (showEachO2Battery)
										{
											num5 += condAmount / condAmount2 * 100.0;
										}
										else
										{
											num += condAmount;
											num3 += condAmount2;
										}
									}
									else
									{
										bool flag16 = this.ctEVABatt.Triggered(condOwner2, null, false);
										if (flag16)
										{
											bool flag17 = !flag12;
											if (flag17)
											{
												flag12 = true;
											}
											Powered component = condOwner2.GetComponent<Powered>();
											double condAmount3 = condOwner2.GetCondAmount("StatPower");
											double num7 = condOwner2.GetCondAmount("StatPowerMax") * condOwner2.GetDamageState();
											bool flag18 = component != null;
											if (flag18)
											{
												num7 = component.PowerStoredMax;
											}
											bool flag19 = num7 == 0.0;
											if (flag19)
											{
												num7 = 1.0;
											}
											bool showEachO2Battery2 = FFU_BR_Defs.ShowEachO2Battery;
											if (showEachO2Battery2)
											{
												num6 += condAmount3 / num7 * 100.0;
											}
											else
											{
												num2 += condAmount3;
												num4 += num7;
											}
										}
									}
								}
								bool flag20 = flag11;
								if (flag20)
								{
									flag5 = false;
									bool flag21 = !FFU_BR_Defs.ShowEachO2Battery;
									if (flag21)
									{
										num5 = num / num3 * 100.0;
									}
									this.txtO2.text = num5.ToString("n2") + "%";
									bool flag22 = num5 != this.fO2Last;
									if (flag22)
									{
										bool flag23 = num5 < (double)FFU_BR_Defs.SuitOxygenNotify;
										if (flag23)
										{
											this.asO2Beep.Play();
										}
										this.fO2Last = num5;
									}
								}
								bool flag24 = flag12;
								if (flag24)
								{
									flag4 = false;
									bool flag25 = !FFU_BR_Defs.ShowEachO2Battery;
									if (flag25)
									{
										num6 = num2 / num4 * 100.0;
									}
									this.txtBatt.text = num6.ToString("n2") + "%";
									bool flag26 = num6 != this.fPwrLast;
									if (flag26)
									{
										bool flag27 = num6 < (double)FFU_BR_Defs.SuitPowerNotify;
										if (flag27)
										{
											this.asO2Beep.Play();
										}
										this.fPwrLast = num6;
									}
								}
							}
						}
					}
					bool flag28 = flag4;
					if (flag28)
					{
						base.Style = 1;
					}
					else
					{
						base.Style = 2;
					}
				}
				else
				{
					bool flag29 = coRoomIn.HasCond("IsPSHUD");
					if (flag29)
					{
						flag7 = true;
						base.Style = 2;
					}
				}
				base.HUDOn = flag6;
				base.GaugeOn = flag7;
				bool flag30 = (int)Time.realtimeSinceStartup % 2 == 0;
				double condAmount4 = coRoomIn.GetCondAmount("StatGasPpO2");
				double condAmount5 = coRoomIn.GetCondAmount("StatGasPpCO2");
				bool flag31 = condAmount4 <= this.fO2PPMin || condAmount5 >= this.fCO2Max;
				if (flag31)
				{
					base.TriggerTutorial();
				}
				bool flag32 = flag7;
				if (flag32)
				{
					base.UpdatePSGauge(condAmount4, condAmount5);
				}
				else
				{
					bool flag33 = flag6;
					if (flag33)
					{
						bool flag34 = flag5;
						if (flag34)
						{
							this.txtO2.text = "ERROR";
						}
						this.ghO2Int.Value = coRoomIn.GetCondAmount("StatGasPpO2");
						this.ghO2Ext.Value = coRoomOut.GetCondAmount("StatGasPpO2");
						this.ghPressInt.Value = coRoomIn.GetCondAmount("StatGasPressure");
						this.ghPressExt.Value = coRoomOut.GetCondAmount("StatGasPressure");
						this.ghTempInt.Value = coRoomIn.GetCondAmount("StatGasTemp");
						this.ghTempExt.Value = coRoomOut.GetCondAmount("StatGasTemp");
						this.ghPressExt.DangerLow = this.ghPressInt.Value - this.fPressureDiffMax;
						this.ghPressExt.DangerHigh = this.ghPressInt.Value + this.fPressureDiffMax;
					}
				}
			}
		}
	}
	private double fPwrLast;
}
