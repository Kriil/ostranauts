using System;
using System.Collections.Generic;

public class PatchHandler
{
	public static void FixMissingDismantleMax(CondOwner co)
	{
		if (co == null || PatchHandler.DictMissingDismantleLookUp == null)
		{
			return;
		}
		if (PatchHandler.DictMissingDismantleLookUp.ContainsKey(co.strCODef))
		{
			co.AddCondAmount("StatDismantleProgressMax", PatchHandler.DictMissingDismantleLookUp[co.strCODef], 0.0, 0f);
		}
		else
		{
			co.AddCondAmount("StatDismantleProgressMax", 100.0, 0.0, 0f);
		}
	}

	private static Dictionary<string, double> DictMissingDismantleLookUp
	{
		get
		{
			if (PatchHandler._dictLookUp != null)
			{
				return PatchHandler._dictLookUp;
			}
			PatchHandler._dictLookUp = new Dictionary<string, double>();
			PatchHandler._dictLookUp["ItmAICargo01Off"] = 370.0;
			PatchHandler._dictLookUp["ItmAICargo01OffLoose"] = 370.0;
			PatchHandler._dictLookUp["ItmAirPump02Off"] = 140.0;
			PatchHandler._dictLookUp["ItmAirPump02OffLoose"] = 140.0;
			PatchHandler._dictLookUp["ItmAlarmN2Off"] = 110.0;
			PatchHandler._dictLookUp["ItmAlarmO2Off"] = 110.0;
			PatchHandler._dictLookUp["ItmAlarmN2OffLoose"] = 110.0;
			PatchHandler._dictLookUp["ItmAlarmO2OffLoose"] = 110.0;
			PatchHandler._dictLookUp["ItmAlarmTempOff"] = 110.0;
			PatchHandler._dictLookUp["ItmAlarmTempOffLoose"] = 110.0;
			PatchHandler._dictLookUp["ItmBattery02"] = 140.0;
			PatchHandler._dictLookUp["ItmBattery02Loose"] = 140.0;
			PatchHandler._dictLookUp["ItmBattery03"] = 110.0;
			PatchHandler._dictLookUp["ItmBattery04"] = 110.0;
			PatchHandler._dictLookUp["ItmBatteryDrill01"] = 110.0;
			PatchHandler._dictLookUp["ItmBatteryWelder01"] = 110.0;
			PatchHandler._dictLookUp["ItmChargerBattDrill01Off"] = 220.0;
			PatchHandler._dictLookUp["ItmChargerBattDrill01OffLoose"] = 220.0;
			PatchHandler._dictLookUp["ItmChargerBattEVAOff"] = 220.0;
			PatchHandler._dictLookUp["ItmChargerBattEVAOffLoose"] = 220.0;
			PatchHandler._dictLookUp["ItmChargerBattWelder01Off"] = 220.0;
			PatchHandler._dictLookUp["ItmChargerBattWelder01OffLoose"] = 220.0;
			PatchHandler._dictLookUp["ItmChargerBattery04Off"] = 220.0;
			PatchHandler._dictLookUp["ItmChargerBattery04OffLoose"] = 220.0;
			PatchHandler._dictLookUp["ItmConduit00"] = 30.0;
			PatchHandler._dictLookUp["ItmConduit00Loose"] = 30.0;
			PatchHandler._dictLookUp["ItmConduit04"] = 30.0;
			PatchHandler._dictLookUp["ItmConduit04Loose"] = 30.0;
			PatchHandler._dictLookUp["ItmCooler01Off"] = 250.0;
			PatchHandler._dictLookUp["ItmCooler01OffLoose"] = 250.0;
			PatchHandler._dictLookUp["ItmCrate01"] = 20.0;
			PatchHandler._dictLookUp["ItmCrate01Lock"] = 20.0;
			PatchHandler._dictLookUp["ItmDoor01ClosedLocked"] = 300.0;
			PatchHandler._dictLookUp["ItmDoor01Closed"] = 300.0;
			PatchHandler._dictLookUp["ItmDoor01Open"] = 300.0;
			PatchHandler._dictLookUp["ItmDoor01OpenLocked"] = 300.0;
			PatchHandler._dictLookUp["ItmDoor01ClosedLoose"] = 300.0;
			PatchHandler._dictLookUp["ItmFloorGrate01"] = 110.0;
			PatchHandler._dictLookUp["ItmFloorGrate01Loose"] = 110.0;
			PatchHandler._dictLookUp["ItmFridge01"] = 240.0;
			PatchHandler._dictLookUp["ItmFridge01Loose"] = 240.0;
			PatchHandler._dictLookUp["ItmHeater01Off"] = 240.0;
			PatchHandler._dictLookUp["ItmHeater01OffLoose"] = 240.0;
			PatchHandler._dictLookUp["ItmLitWall1x0Off"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0OrangeOff"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0VibrantRedOff"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0VibrantPurpleOff"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0VibrantGreenOff"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0OffLoose"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0OrangeOffLoose"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0VibrantRedOffLoose"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0VibrantPurpleOffLoose"] = 10.0;
			PatchHandler._dictLookUp["ItmLitWall1x0VibrantGreenOffLoose"] = 10.0;
			PatchHandler._dictLookUp["OutfitEVA01Off"] = 130.0;
			PatchHandler._dictLookUp["OutfitHelmet02"] = 10.0;
			PatchHandler._dictLookUp["OutfitHelmet03"] = 120.0;
			PatchHandler._dictLookUp["OutfitPS01"] = 10.0;
			PatchHandler._dictLookUp["ItmRCSCluster01Off"] = 150.0;
			PatchHandler._dictLookUp["ItmRCSCluster01Loose"] = 150.0;
			PatchHandler._dictLookUp["ItmRCSDistro01Off"] = 250.0;
			PatchHandler._dictLookUp["ItmRCSDistro01Loose"] = 250.0;
			PatchHandler._dictLookUp["ItmRCSDistro02Off"] = 130.0;
			PatchHandler._dictLookUp["ItmRCSDistro02Loose"] = 130.0;
			PatchHandler._dictLookUp["ItmReactorIC02Off"] = 750.0;
			PatchHandler._dictLookUp["ItmReactorIC03Off"] = 750.0;
			PatchHandler._dictLookUp["ItmReactorIC02OffLoose"] = 750.0;
			PatchHandler._dictLookUp["ItmReactorIC03OffLoose"] = 750.0;
			PatchHandler._dictLookUp["ItmSink01"] = 40.0;
			PatchHandler._dictLookUp["ItmSink01Loose"] = 40.0;
			PatchHandler._dictLookUp["ItmStationNavOff"] = 360.0;
			PatchHandler._dictLookUp["ItmStationNavLoose"] = 360.0;
			PatchHandler._dictLookUp["ItmSwitch01Off"] = 30.0;
			PatchHandler._dictLookUp["ItmSwitch01On"] = 30.0;
			PatchHandler._dictLookUp["ItmSwitch01Loose"] = 30.0;
			PatchHandler._dictLookUp["ItmToolBox01"] = 30.0;
			PatchHandler._dictLookUp["ItmToolBox02"] = 10.0;
			PatchHandler._dictLookUp["ItmToolDrill01"] = 130.0;
			PatchHandler._dictLookUp["ItmToolGrinder02"] = 130.0;
			PatchHandler._dictLookUp["ItmToolLaserTorch01"] = 230.0;
			PatchHandler._dictLookUp["ItmToolSolderingIron01"] = 120.0;
			PatchHandler._dictLookUp["ItmToolWelder01"] = 130.0;
			PatchHandler._dictLookUp["ItmToolWorkLamp01"] = 10.0;
			PatchHandler._dictLookUp["ItmVent01Closed"] = 20.0;
			PatchHandler._dictLookUp["ItmVent01Open"] = 20.0;
			PatchHandler._dictLookUp["ItmVent01Loose"] = 20.0;
			PatchHandler._dictLookUp["ItmWall1x1"] = 110.0;
			PatchHandler._dictLookUp["ItmWall1x1Loose"] = 110.0;
			PatchHandler._dictLookUp["ItmWristPDA01"] = 230.0;
			return PatchHandler._dictLookUp;
		}
	}

	private static Dictionary<string, double> _dictLookUp;
}
