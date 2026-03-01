using System;
using System.Collections.Generic;
using Ostranauts.Core.Models;
using UnityEngine;

public class ShipStatus
{
	public static void PrintStatus(CondOwner COSelf, ref string[] aValues)
	{
		for (int i = 0; i < aValues.Length; i++)
		{
			aValues[i] = ShipStatus.strBad + "ERROR" + ShipStatus.strClose;
		}
		if (COSelf.ship != null)
		{
			double num = 0.0;
			double num2 = 0.0;
			double num3 = 0.0;
			CondTrigger condTrigger = null;
			aValues[0] = COSelf.ship.GetRatingString();
			aValues[1] = COSelf.ship.Mass.ToString("N0") + " kg";
			List<CondOwner> icos;
			if (string.IsNullOrEmpty(COSelf.ship.strXPDR))
			{
				icos = COSelf.ship.GetICOs1(Ship.ctXPDR, false, false, false);
				if (icos.Count > 0)
				{
					aValues[2] = ShipStatus.strBad + "OFFLINE" + ShipStatus.strClose;
				}
				else
				{
					aValues[2] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
				}
			}
			else
			{
				aValues[2] = ShipStatus.strGood + COSelf.ship.strXPDR + ShipStatus.strClose;
			}
			aValues[3] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsXPDRAnt");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			num = 0.0;
			num2 = 0.0;
			foreach (CondOwner condOwner in icos)
			{
				num += 1.0;
				if (!condOwner.HasCond("IsOff"))
				{
					num2 += 1.0;
				}
			}
			if (num2 > 0.0)
			{
				aValues[3] = string.Concat(new object[]
				{
					ShipStatus.strGood,
					(int)num2,
					"/",
					(int)num,
					ShipStatus.strClose
				});
			}
			else
			{
				aValues[3] = string.Concat(new object[]
				{
					ShipStatus.strBad,
					(int)num2,
					"/",
					(int)num,
					ShipStatus.strClose
				});
			}
			aValues[4] = ShipStatus.strGood + "ONLINE" + ShipStatus.strClose;
			aValues[5] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsReactorIC");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			foreach (CondOwner condOwner2 in icos)
			{
				if (condOwner2.HasCond("IsInstalled"))
				{
					num = condOwner2.GetCondAmount("StatPower");
					if (num != 0.0)
					{
						aValues[5] = ShipStatus.strGood + "ONLINE" + ShipStatus.strClose;
						break;
					}
					aValues[5] = ShipStatus.strBad + "OFFLINE" + ShipStatus.strClose;
				}
			}
			aValues[6] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsCanisterLHe02Installed");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			num = 0.0;
			foreach (CondOwner condOwner3 in icos)
			{
				num += condOwner3.GetCondAmount("StatSolidHe3");
			}
			if (num > 100.0)
			{
				aValues[6] = ShipStatus.strGood + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			else
			{
				aValues[6] = ShipStatus.strBad + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			aValues[7] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsCanisterLH02Installed");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			num = 0.0;
			foreach (CondOwner condOwner4 in icos)
			{
				num += condOwner4.GetCondAmount("StatLiqD2O");
			}
			if (num > 1000.0)
			{
				aValues[7] = ShipStatus.strGood + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			else
			{
				aValues[7] = ShipStatus.strBad + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			aValues[8] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsRCSClusterInstalled");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			num = 0.0;
			num2 = 0.0;
			foreach (CondOwner condOwner5 in icos)
			{
				num += 1.0;
				if (!condOwner5.HasCond("IsOff"))
				{
					num2 += 1.0;
				}
			}
			if (num2 > 1.0)
			{
				aValues[8] = string.Concat(new object[]
				{
					ShipStatus.strGood,
					(int)num2,
					"/",
					(int)num,
					ShipStatus.strClose
				});
			}
			else
			{
				aValues[8] = string.Concat(new object[]
				{
					ShipStatus.strBad,
					(int)num2,
					"/",
					(int)num,
					ShipStatus.strClose
				});
			}
			aValues[9] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsRCSDistroInstalled");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			foreach (CondOwner condOwner6 in icos)
			{
				if (!condOwner6.HasCond("IsOff"))
				{
					aValues[9] = ShipStatus.strGood + "ONLINE" + ShipStatus.strClose;
					break;
				}
				aValues[9] = ShipStatus.strBad + "OFFLINE" + ShipStatus.strClose;
			}
			num = COSelf.ship.GetRCSRemain();
			if (num >= 200.0)
			{
				aValues[10] = ShipStatus.strGood + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			else
			{
				aValues[10] = ShipStatus.strBad + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			Powered component = COSelf.GetComponent<Powered>();
			num = component.PowerConnected;
			if (num >= 20.0)
			{
				if (num >= 1000000.0)
				{
					aValues[11] = ShipStatus.strGood + (num / 1000000.0).ToString("N0") + " GWh" + ShipStatus.strClose;
				}
				else
				{
					aValues[11] = ShipStatus.strGood + num.ToString("N0") + " kWh" + ShipStatus.strClose;
				}
			}
			else
			{
				aValues[11] = ShipStatus.strBad + num.ToString("#.00 kWh") + ShipStatus.strClose;
			}
			aValues[12] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsAirPump02Installed");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			condTrigger = DataHandler.GetCondTrigger("TIsRTAO2Installed");
			num = 0.0;
			num2 = 0.0;
			num3 = 0.0;
			foreach (CondOwner condOwner7 in icos)
			{
				num3 += 1.0;
				if (!condOwner7.HasCond("IsOff"))
				{
					Tuple<double, double> o2UnderPump = ShipStatus.GetO2UnderPump(condOwner7, condTrigger);
					num += o2UnderPump.Item1;
					num2 += o2UnderPump.Item2;
				}
			}
			if (num2 > 0.0)
			{
				aValues[12] = string.Concat(new object[]
				{
					ShipStatus.strGood,
					num2,
					"/",
					num3,
					ShipStatus.strClose
				});
			}
			else
			{
				aValues[12] = string.Concat(new object[]
				{
					ShipStatus.strBad,
					num2,
					"/",
					num3,
					ShipStatus.strClose
				});
			}
			if (num > 35.0)
			{
				aValues[13] = ShipStatus.strGood + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			else
			{
				aValues[13] = ShipStatus.strBad + num.ToString("#.00 kg") + ShipStatus.strClose;
			}
			aValues[14] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsHeater01Installed");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			foreach (CondOwner condOwner8 in icos)
			{
				if (!condOwner8.HasCond("IsOff"))
				{
					aValues[14] = ShipStatus.strGood + "ONLINE" + ShipStatus.strClose;
					break;
				}
				aValues[14] = ShipStatus.strBad + "OFFLINE" + ShipStatus.strClose;
			}
			aValues[15] = ShipStatus.strBad + "NOT FOUND" + ShipStatus.strClose;
			condTrigger = DataHandler.GetCondTrigger("TIsCooler01Installed");
			icos = COSelf.ship.GetICOs1(condTrigger, false, false, false);
			foreach (CondOwner condOwner9 in icos)
			{
				if (!condOwner9.HasCond("IsOff"))
				{
					aValues[15] = ShipStatus.strGood + "ONLINE" + ShipStatus.strClose;
					break;
				}
				aValues[15] = ShipStatus.strBad + "OFFLINE" + ShipStatus.strClose;
			}
		}
	}

	public static Tuple<double, double> GetO2UnderPump(CondOwner coPump, CondTrigger ctGasCan)
	{
		Tuple<double, double> tuple = new Tuple<double, double>();
		if (coPump == null || coPump.mapPoints == null)
		{
			return tuple;
		}
		foreach (KeyValuePair<string, Vector2> keyValuePair in coPump.mapPoints)
		{
			if (keyValuePair.Key.IndexOf("GasInput") >= 0)
			{
				List<CondOwner> list = new List<CondOwner>();
				coPump.ship.GetCOsAtWorldCoords1(coPump.GetPos(keyValuePair.Key, false), ctGasCan, true, false, list);
				foreach (CondOwner condOwner in list)
				{
					GasContainer gasContainer = condOwner.GasContainer;
					if (gasContainer != null)
					{
						double gasMass = GasContainer.GetGasMass("O2", condOwner.GetCondAmount("StatGasMolO2"));
						tuple.Item1 = gasMass;
						if (gasMass > 0.0)
						{
							tuple.Item2 += 1.0;
						}
						break;
					}
				}
			}
		}
		return tuple;
	}

	public static string[] aNames = new string[]
	{
		"VESSEL RATING CODE:",
		"VESSEL MASS:",
		"TRANSPONDER:",
		"TRANSPONDER ANTENNA:",
		"NAV STATION:",
		"REACTOR:",
		"REACTOR HE3:",
		"REACTOR D2O:",
		"RCS THRUSTERS:",
		"RCS DISTRIBUTOR:",
		"RCS REMASS:",
		"BACKUP POWER:",
		"LIFE SUPPORT WORKING O2 PUMPS:",
		"LIFE SUPPORT O2 STORES:",
		"LIFE SUPPORT HEAT:",
		"LIFE SUPPORT COOL:"
	};

	private static string strGood = "<color=#009900>";

	private static string strBad = "<color=#990000>";

	private static string strClose = "</color>";
}
