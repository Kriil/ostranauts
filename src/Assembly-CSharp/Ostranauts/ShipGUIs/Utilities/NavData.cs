using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Objectives;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.ShipGUIs.Utilities
{
	// Runtime plotted navigation/autopilot path.
	// This is the live counterpart to JsonNavData and interpolates between
	// NavDataPoints to drive ship motion, fuel use, and torching state.
	public class NavData
	{
		// Creates an empty runtime path owned by a specific ship.
		public NavData(Ship ship)
		{
			this._shipUs = ship;
		}

		// Rehydrates a plotted path from JsonNavData after loading.
		public NavData(JsonNavData jnd)
		{
			if (jnd == null || CrewSim.system == null)
			{
				return;
			}
			this._shipUs = CrewSim.system.GetShipByRegID(jnd.strRegID);
			this.IsTorching = jnd.bIsTorching;
			this.fFlowMultPlot = jnd.fFlowMultPlot;
			this.InAtmo = jnd.bInAtmo;
			if (jnd.aPoints != null)
			{
				this._navPoints = new List<NavDataPoint>();
				foreach (JsonNavDataPoint jsonNavDataPoint in jnd.aPoints)
				{
					NavDataPoint navDataPoint = new NavDataPoint(jsonNavDataPoint.ArrivalTime, new ShipSitu(jsonNavDataPoint.ObjSS), jsonNavDataPoint.FuelLevel, jsonNavDataPoint.TorchCycle, jsonNavDataPoint.TorchFuelLevel);
					navDataPoint.TorchDecelReq = jsonNavDataPoint.TorchDecelReq;
					this._navPoints.Add(navDataPoint);
				}
			}
		}

		// Path endpoints used by nav UI and travel logic.
		public NavDataPoint Destination
		{
			get
			{
				return this._navPoints.LastOrDefault<NavDataPoint>();
			}
		}

		public NavDataPoint Origin
		{
			get
			{
				return this._navPoints.FirstOrDefault<NavDataPoint>();
			}
		}

		// Appends one waypoint/state sample to the plotted path.
		public void AddNavDataPoint(NavDataPoint navDat)
		{
			this._navPoints.Add(navDat);
		}

		// Direct index access for UI/tools that inspect the plotted path.
		public NavDataPoint GetNavDataPointAtIndex(int index)
		{
			return this._navPoints[index];
		}

		// Applies the current path segment to a ShipSitu at the current epoch.
		// This interpolates kinematics between NavDataPoints, updates torch/fuel
		// state, and can abort autopilot if reactor conditions drift too far.
		public bool TimeAdvance(ShipSitu situUs)
		{
			double fEpoch = StarSystem.fEpoch;
			NavDataPoint navDataPoint = this._navPoints.First<NavDataPoint>();
			NavDataPoint navDataPoint2 = this._navPoints.Last<NavDataPoint>();
			for (int i = 1; i < this._navPoints.Count; i++)
			{
				if (fEpoch <= this._navPoints[i].ArrivalTime)
				{
					navDataPoint = this._navPoints[i - 1];
					navDataPoint2 = this._navPoints[i];
					int num = i - 1;
					break;
				}
			}
			if (navDataPoint == null || navDataPoint2 == null || navDataPoint == navDataPoint2)
			{
				return false;
			}
			navDataPoint.ObjSS.UpdateTime(fEpoch, true);
			navDataPoint2.ObjSS.UpdateTime(fEpoch, true);
			float b = (float)(navDataPoint2.ArrivalTime - navDataPoint.ArrivalTime);
			float value = (float)(fEpoch - navDataPoint.ArrivalTime);
			float num2 = Mathf.InverseLerp(0f, b, value);
			if (this._shipUs == null || this._shipUs.bDestroyed)
			{
				situUs.ResetNavData();
				return false;
			}
			ShipSitu shipSitu = new ShipSitu(situUs);
			situUs.fRot = this.LerpAngle(navDataPoint.ObjSS.fRot, navDataPoint2.ObjSS.fRot, num2);
			situUs.fA = 0f;
			situUs.fW = 0f;
			Tuple<double, double> tuple = NavData.LerpBetween2Positions(navDataPoint.VelX, navDataPoint.VelY, navDataPoint2.VelX, navDataPoint2.VelY, num2);
			situUs.vVelX = tuple.Item1;
			situUs.vVelY = tuple.Item2;
			float magnitude = this._shipUs.objSS.vAccIn.magnitude;
			situUs.ResetAccelerations();
			Tuple<double, double> tuple2 = NavData.LerpBetween2Positions((double)navDataPoint.ObjSS.vAccIn.x, (double)navDataPoint.ObjSS.vAccIn.y, (double)navDataPoint2.ObjSS.vAccIn.x, (double)navDataPoint2.ObjSS.vAccIn.y, num2);
			situUs.vAccIn.x = (float)tuple2.Item1;
			situUs.vAccIn.y = (float)tuple2.Item2;
			tuple2 = NavData.LerpBetween2Positions((double)navDataPoint.ObjSS.vAccRCS.x, (double)navDataPoint.ObjSS.vAccRCS.y, (double)navDataPoint2.ObjSS.vAccRCS.x, (double)navDataPoint2.ObjSS.vAccRCS.y, num2);
			situUs.vAccRCS.x = (float)tuple2.Item1;
			situUs.vAccRCS.y = (float)tuple2.Item2;
			double num3 = (double)num2;
			Tuple<double, double> tuple3 = NavData.LerpBetween2Positions(navDataPoint.ObjSS.vPosx, navDataPoint.ObjSS.vPosy, navDataPoint2.ObjSS.vPosx, navDataPoint2.ObjSS.vPosy, (float)num3);
			situUs.vPosx = tuple3.Item1;
			situUs.vPosy = tuple3.Item2;
			if (this._shipUs != null && !this._shipUs.bDestroyed)
			{
				if (navDataPoint.FuelLevel - navDataPoint2.FuelLevel > 0.001)
				{
					double rcsremain = this._shipUs.GetRCSRemain();
					float num4 = Mathf.Lerp((float)navDataPoint.FuelLevel, (float)navDataPoint2.FuelLevel, num2);
					double num5 = rcsremain - (double)num4;
					if (num5 > 1E-08)
					{
						this._shipUs.RemoveGasMass((float)num5);
					}
				}
				if (navDataPoint.TorchFuelLevel - navDataPoint2.TorchFuelLevel > 0.001 || this.IsTorching)
				{
					float num6 = 0f;
					float.TryParse(this._shipUs.GetReactorGPMValue("slidCycle"), out num6);
					float num7 = 0f;
					float.TryParse(this._shipUs.GetReactorGPMValue("slidFlow"), out num7);
					double num8 = 0.0;
					double.TryParse(this._shipUs.GetReactorGPMValue("fFlowEpochResume"), out num8);
					double num9 = (double)Mathf.Lerp((float)navDataPoint.TorchFuelLevel, (float)navDataPoint2.TorchFuelLevel, num2);
					if (this._shipUs.Reactor != null)
					{
						FusionIC component = this._shipUs.Reactor.GetComponent<FusionIC>();
						if (component != null)
						{
							component.CatchUp();
							component.SetReactants(num9);
							double num10 = (navDataPoint.TorchFuelLevel - navDataPoint2.TorchFuelLevel) * component.MassUsageMax * (double)FusionIC.aReactantAmounts[0];
							double num11 = (navDataPoint.TorchFuelLevel - navDataPoint2.TorchFuelLevel) * component.MassUsageMax * (double)FusionIC.aReactantAmounts[1];
							double num12 = navDataPoint2.ArrivalTime - navDataPoint.ArrivalTime;
							this._shipUs.Reactor.SetCondAmount("StatICDRate", num10 / num12, 0.0);
							this._shipUs.Reactor.SetCondAmount("StatICHe3Rate", num11 / num12, 0.0);
						}
					}
					else
					{
						this._shipUs.fShallowFusionRemain = num9;
					}
					if (!this.IsTorching)
					{
						this._shipUs.bChangedStatus = true;
					}
					this.IsTorching = true;
					float num4 = Mathf.Lerp((float)navDataPoint.TorchCycle, (float)navDataPoint2.TorchCycle, num2);
					double num5 = (double)(num6 - num4);
					this._shipUs.SetReactorGPMValue("slidCycle", num4.ToString());
					double num13 = (double)num7;
					if (this._shipUs.Reactor != null && this._shipUs.Reactor.HasCond("StatICPellMaxTheory"))
					{
						float maxTorchThrust = this._shipUs.GetMaxTorchThrust(num4);
						if (num8 < StarSystem.fEpoch)
						{
							num13 = NavData.GetAdjustedFLOW(this._shipUs.Reactor, num13, (double)magnitude, (double)maxTorchThrust);
						}
						if (double.IsNaN(num13))
						{
							num13 = (double)num4;
						}
						bool flag = false;
						if (num13 != 0.0 && this.fFlowMultPlot != 0.0 && Math.Abs(navDataPoint.TorchCycle - navDataPoint2.TorchCycle) < 0.001)
						{
							double num14 = this._shipUs.Reactor.GetCondAmount("StatICCoreTemp") / 0.7250000238418579;
							flag = (flag || num14 > 1.2 || num14 < 0.8);
						}
						if (flag)
						{
							this._shipUs.Reactor.AddCondAmount("IsAutopilotAborted", 1.0, 0.0, 0f);
							this._shipUs.shipSituTarget = null;
							this._shipUs.shipScanTarget = null;
							situUs.ResetNavData();
							this._shipUs.SetReactorGPMValue("slidFlow", "0.0");
							this._shipUs.SetReactorGPMValue("slidCycle", "0.0");
							AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this._shipUs.strRegID);
							if (aishipByRegID != null && aishipByRegID.ActiveCommandName == "FlyToAutoPilot")
							{
								AIShipManager.UnregisterShip(this._shipUs);
							}
							if (this._shipUs.LoadState >= Ship.Loaded.Edit)
							{
								this._shipUs.SetThrust(0.0);
							}
							this._shipUs.bChangedStatus = true;
							CondOwner condOwner = this._shipUs.aNavs.FirstOrDefault<CondOwner>();
							if (condOwner != null)
							{
								AlarmObjective objective = new AlarmObjective(AlarmType.nav_autopilot, condOwner, DataHandler.GetString("OBJV_NAV_AUTOPILOT_DESC", false));
								MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
							}
							this._shipUs.LogAdd(DataHandler.GetString("NAV_LOG_AP_DISABLED", false), StarSystem.fEpoch, true);
							return false;
						}
					}
					this._shipUs.SetReactorGPMValue("slidFlow", num13.ToString());
				}
				else
				{
					if (this.IsTorching)
					{
						this._shipUs.bChangedStatus = true;
					}
					this.IsTorching = false;
				}
				if (fEpoch > navDataPoint2.ArrivalTime)
				{
					bool result = false;
					if (GUIFFWD.Active)
					{
						if (this.IsTorching || !navDataPoint2.ObjSS.bIsBO)
						{
							this._shipUs.shipSituTarget = new ShipSitu();
							this._shipUs.shipSituTarget.CopyFrom(navDataPoint2.ObjSS, true);
							this._shipUs.shipSituTarget.ResetAccelerations();
							this._shipUs.shipSituTarget.TimeAdvance(fEpoch - navDataPoint2.ArrivalTime, false);
							this._shipUs.objSS.CopyFrom(this._shipUs.shipSituTarget, true);
						}
						if (this.IsTorching)
						{
							this._shipUs.SetReactorGPMValue("slidFlow", "0.0");
							this._shipUs.SetReactorGPMValue("slidCycle", "0.0");
						}
						result = true;
					}
					situUs.ResetNavData();
					return result;
				}
			}
			this.fEpochLast = fEpoch;
			return true;
		}

		// Converts a desired reactor cycle into a nominal flow setting.
		public static double GetFLOWforCYCLE(CondOwner coReactor, double fCycle)
		{
			if (coReactor == null || fCycle <= 0.0)
			{
				return 0.0;
			}
			return fCycle * coReactor.GetCondAmount("StatICPellMaxTheory") / coReactor.GetCondAmount("StatICPellMax") * 0.9;
		}

		// Nudges flow toward target thrust while compensating for reactor core temperature.
		public static double GetAdjustedFLOW(CondOwner coReactor, double fFlowNow, double fThrustActual, double fThrustTarget)
		{
			if (coReactor == null)
			{
				return 0.0;
			}
			double condAmount = coReactor.GetCondAmount("StatICCoreTemp");
			double num = 0.7250000238418579 / condAmount;
			if (Math.Abs(num - 1.0) > 0.05 && fFlowNow != 0.0)
			{
				fFlowNow *= num;
			}
			else
			{
				double value = fThrustTarget - fThrustActual;
				if (Math.Abs(value) >= fThrustTarget * 0.001)
				{
					if (fThrustTarget > fThrustActual)
					{
						fFlowNow += 0.001;
					}
					else
					{
						fFlowNow -= 0.001;
					}
				}
			}
			return MathUtils.Clamp(fFlowNow, 0.0, 1.0);
		}

		// Stores the plotted flow multiplier used during torch interpolation.
		public void SetFlowMultPlot(double fValue)
		{
			this.fFlowMultPlot = fValue;
		}

		// Samples the plotted path into a temporary ShipSitu at an arbitrary epoch.
		public ShipSitu GetShipSituAtTime(double fEpoch, bool bOverflow)
		{
			NavDataPoint navDataPoint = this._navPoints.First<NavDataPoint>();
			NavDataPoint navDataPoint2 = this._navPoints.Last<NavDataPoint>();
			for (int i = 1; i < this._navPoints.Count; i++)
			{
				if (fEpoch <= this._navPoints[i].ArrivalTime)
				{
					navDataPoint = this._navPoints[i - 1];
					navDataPoint2 = this._navPoints[i];
					break;
				}
			}
			if (navDataPoint == null || navDataPoint2 == null || navDataPoint == navDataPoint2)
			{
				return null;
			}
			navDataPoint.ObjSS.UpdateTime(fEpoch, true);
			navDataPoint2.ObjSS.UpdateTime(fEpoch, true);
			float b = (float)(navDataPoint2.ArrivalTime - navDataPoint.ArrivalTime);
			float value = (float)(fEpoch - navDataPoint.ArrivalTime);
			float t = Mathf.InverseLerp(0f, b, value);
			if (fEpoch > navDataPoint2.ArrivalTime && !bOverflow)
			{
				return null;
			}
			ShipSitu shipSitu = new ShipSitu();
			shipSitu.fRot = this.LerpAngle(navDataPoint.ObjSS.fRot, navDataPoint2.ObjSS.fRot, t);
			shipSitu.fA = 0f;
			shipSitu.fW = 0f;
			Tuple<double, double> tuple = NavData.LerpBetween2Positions(navDataPoint.VelX, navDataPoint.VelY, navDataPoint2.VelX, navDataPoint2.VelY, t);
			shipSitu.vVelX = tuple.Item1;
			shipSitu.vVelY = tuple.Item2;
			Tuple<double, double> tuple2 = NavData.LerpBetween2Positions(navDataPoint.ObjSS.vPosx, navDataPoint.ObjSS.vPosy, navDataPoint2.ObjSS.vPosx, navDataPoint2.ObjSS.vPosy, t);
			shipSitu.vPosx = tuple2.Item1;
			shipSitu.vPosy = tuple2.Item2;
			return shipSitu;
		}

		private NavDataPoint GetNavDataPointClosestToTimeStamp(double timestamp)
		{
			for (int i = 1; i < this._navPoints.Count; i++)
			{
				if (timestamp <= this._navPoints[i].ArrivalTime)
				{
					NavDataPoint navDataPoint = (Math.Abs(timestamp - this._navPoints[i - 1].ArrivalTime) >= Math.Abs(timestamp - this._navPoints[i].ArrivalTime)) ? this._navPoints[i] : this._navPoints[i - 1];
					return navDataPoint.Clone();
				}
			}
			return null;
		}

		public double GetDistanceToTarget(double timestamp)
		{
			NavDataPoint navDataPoint = this._navPoints.Last<NavDataPoint>();
			if (navDataPoint == null)
			{
				return 0.0;
			}
			NavDataPoint navDataPointClosestToTimeStamp = this.GetNavDataPointClosestToTimeStamp(timestamp);
			if (navDataPointClosestToTimeStamp == null || navDataPoint == navDataPointClosestToTimeStamp)
			{
				return -1.0;
			}
			navDataPointClosestToTimeStamp.ObjSS.UpdateTime(timestamp, false);
			navDataPoint.ObjSS.UpdateTime(timestamp, false);
			double rangeTo = navDataPointClosestToTimeStamp.ObjSS.GetRangeTo(navDataPoint.ObjSS);
			navDataPointClosestToTimeStamp.ObjSS.UpdateTime(StarSystem.fEpoch, false);
			navDataPoint.ObjSS.UpdateTime(StarSystem.fEpoch, false);
			return rangeTo;
		}

		private float CalculateShipRotation(NavDataPoint origin, NavDataPoint target, float posInTime)
		{
			return this.LerpAngle(origin.ObjSS.fRot, target.ObjSS.fRot, posInTime);
		}

		private float LerpAngle(float a, float b, float t)
		{
			t = Mathf.Max(0f, Mathf.Min(1f, t));
			float num = b - a;
			if (num < -3.1415927f)
			{
				b += 6.2831855f;
			}
			else if (num > 3.1415927f)
			{
				b -= 6.2831855f;
			}
			return Mathf.Lerp(a, b, t);
		}

		public static Tuple<double, double> LerpBetween2Positions(double aX, double aY, double bX, double bY, float t)
		{
			t = Mathf.Max(0f, Mathf.Min(1f, t));
			double item = aX + (bX - aX) * (double)t;
			double item2 = aY + (bY - aY) * (double)t;
			return new Tuple<double, double>(item, item2);
		}

		public double GetArrivalRCSFuel()
		{
			if (this._navPoints == null || this._navPoints.Count == 0)
			{
				return 0.0;
			}
			return this._navPoints[this._navPoints.Count - 1].FuelLevel;
		}

		public double GetArrivalTorchFuel()
		{
			if (this._navPoints == null || this._navPoints.Count == 0)
			{
				return 0.0;
			}
			return this._navPoints[this._navPoints.Count - 1].TorchFuelLevel;
		}

		public bool IsUsingTorchDrive()
		{
			return this._navPoints != null && this._navPoints.FirstOrDefault<NavDataPoint>() != null && this.GetArrivalTorchFuel() < this._navPoints.FirstOrDefault<NavDataPoint>().TorchFuelLevel;
		}

		public double GetArrivalEpoch()
		{
			if (this._navPoints == null || this._navPoints.Count == 0)
			{
				return StarSystem.fEpoch;
			}
			return this._navPoints[this._navPoints.Count - 1].ArrivalTime;
		}

		public List<Vector2> GetPoints(GUIOrbitDraw gorb)
		{
			List<Vector2> list = new List<Vector2>();
			if (this._navPoints == null)
			{
				return list;
			}
			foreach (NavDataPoint navDataPoint in this._navPoints)
			{
				double num = 0.0;
				navDataPoint.ObjSS.UpdateTime(StarSystem.fEpoch, false);
				double num2;
				gorb.SolarToCanvas(navDataPoint.ObjSS.vPosx, navDataPoint.ObjSS.vPosy, out num2, out num);
				list.Add(new Vector2((float)num2, (float)num));
			}
			return list;
		}

		public List<NavDataPoint> GetNavDataPoints()
		{
			return this._navPoints;
		}

		public Tuple<NavDataPoint, NavDataPoint> GetCurrentNavPoints(double time)
		{
			if (this._navPoints == null)
			{
				return null;
			}
			for (int i = 1; i < this._navPoints.Count; i++)
			{
				if (time <= this._navPoints[i].ArrivalTime)
				{
					return new Tuple<NavDataPoint, NavDataPoint>(this._navPoints[i - 1], this._navPoints[i]);
				}
			}
			return null;
		}

		public JsonNavData GetJSON()
		{
			JsonNavData jsonNavData = new JsonNavData();
			if (this._shipUs == null)
			{
				return null;
			}
			jsonNavData.strRegID = this._shipUs.strRegID;
			jsonNavData.bIsTorching = this.IsTorching;
			jsonNavData.fFlowMultPlot = this.fFlowMultPlot;
			jsonNavData.bInAtmo = this.InAtmo;
			if (this._navPoints != null)
			{
				List<JsonNavDataPoint> list = new List<JsonNavDataPoint>();
				foreach (NavDataPoint navDataPoint in this._navPoints)
				{
					list.Add(navDataPoint.GetJSON());
				}
				jsonNavData.aPoints = list.ToArray();
			}
			return jsonNavData;
		}

		public void SetShip(Ship ship)
		{
			this._shipUs = ship;
		}

		private List<NavDataPoint> _navPoints = new List<NavDataPoint>();

		private Ship _shipUs;

		private double fEpochLast;

		public double fFlowMultPlot;

		private Point vVelLast;

		public bool IsTorching;

		public bool InAtmo;
	}
}
