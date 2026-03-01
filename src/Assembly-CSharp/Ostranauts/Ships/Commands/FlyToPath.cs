using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.AIPilots.Interfaces;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.Ships.Commands
{
	// AI/nav command that computes and hands off a NavData course toward a target
	// ShipSitu, then finishes once the ship is close enough for the next command.
	public class FlyToPath : BaseCommand
	{
		// AI-driven constructor used by ship pilots.
		public FlyToPath(IAICharacter pilot)
		{
			this._ai = pilot;
			base.ShipUs = pilot.ShipUs;
		}

		// Direct constructor for manually created ship commands.
		public FlyToPath(Ship shipUs, bool enableDebugLogs)
		{
			base.ShipUs = shipUs;
			this._debugMode = enableDebugLogs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return (base.ShipUs.shipScanTarget == null || base.ShipUs.fAIPauseTimer > StarSystem.fEpoch) ? "Calculating target coordinates" : ("Flying to " + base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override string[] SaveData
		{
			get
			{
				return new string[]
				{
					this.fTorchSpeedLimit.ToString()
				};
			}
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				string text = value.FirstOrDefault<string>();
				if (string.IsNullOrEmpty(text))
				{
					return;
				}
				double.TryParse(text, out this.fTorchSpeedLimit);
			}
		}

		// Main command tick: updates/validates the target, plans a new course when
		// needed, and installs the resulting NavData on the ship.
		public override CommandCode RunCommand()
		{
			if (base.ShipUs.shipSituTarget != null)
			{
				base.ShipUs.shipSituTarget.UpdateTime(StarSystem.fEpoch, false);
				double rangeTo = base.ShipUs.objSS.GetRangeTo(base.ShipUs.shipSituTarget);
				float handOffDistance = base.GetHandOffDistance(CollisionManager.GetCollisionDistanceAU(base.ShipUs.objSS, base.ShipUs.shipSituTarget));
				if (base.ShipUs.objSS.NavData != null && base.ShipUs.objSS.NavData.GetArrivalEpoch() - StarSystem.fEpoch < 20.0)
				{
					AIShipManager.PrioritizeShip(base.ShipUs);
				}
				if (rangeTo <= (double)handOffDistance)
				{
					this._rejectedAIPathCounter = 0;
					base.ShipUs.objSS.ResetNavData();
					base.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
					this.TorchOff();
					return CommandCode.Finished;
				}
			}
			if (base.ShipUs.objSS.NavData != null)
			{
				return CommandCode.Ongoing;
			}
			if (base.ShipUs.NavPlayerManned || !base.ShipUs.NavAIManned)
			{
				this.TorchOff();
				return CommandCode.Finished;
			}
			if (base.ShipUs.shipSituTarget == null)
			{
				TargetData targetData = (this._ai == null) ? null : this._ai.GetTarget();
				if (targetData != null)
				{
					base.ShipUs.shipSituTarget = targetData.Situ;
					base.ShipUs.shipScanTarget = targetData.Ship;
				}
				if (targetData == null || targetData.Situ == null)
				{
					this.TorchOff();
					return CommandCode.Cancelled;
				}
			}
			NavDataPoint navOrigin = this.CreateNavPointStatic(base.ShipUs.objSS, StarSystem.fEpoch, false, false);
			NavDataPoint navDestination = this.CreateNavPointStatic(base.ShipUs.shipSituTarget, StarSystem.fEpoch, false, true);
			this.nRecursion = 0;
			FlyToPath.SliderData navSliderValues = this.GetNavSliderValues();
			NavData navData = this.PlanTrip4(navOrigin, navDestination, navSliderValues.Limit, navSliderValues.Coast);
			bool flag = !base.ShipUs.NavPlayerManned;
			if (flag && (navData == null || (!navData.InAtmo && !navData.IsUsingTorchDrive() && navData.GetArrivalRCSFuel() <= 0.0) || (navData.InAtmo && base.ShipUs.LiftRotorsThrustStrength <= 0f && navData.GetArrivalRCSFuel() <= 0.0 && !navData.IsUsingTorchDrive())))
			{
				base.ShipUs.shipScanTarget = null;
				base.ShipUs.shipSituTarget = null;
				base.ShipUs.objSS.ResetNavData();
				this._rejectedAIPathCounter++;
				this.TorchOff();
				if (this._rejectedAIPathCounter >= 10)
				{
					this._rejectedAIPathCounter = 0;
					return CommandCode.Cancelled;
				}
				return CommandCode.Ongoing;
			}
			else
			{
				if (navData == null)
				{
					this.TorchOff();
					return CommandCode.Cancelled;
				}
				this._rejectedAIPathCounter = 0;
				base.ShipUs.objSS.NavData = navData;
				return CommandCode.Ongoing;
			}
		}

		// Reads the nav-station course limit/coast sliders when a full player nav
		// panel is available; AI ships use defaults.
		private FlyToPath.SliderData GetNavSliderValues()
		{
			FlyToPath.SliderData sliderData = new FlyToPath.SliderData();
			if (base.ShipUs.LoadState != Ship.Loaded.Full || base.ShipUs.IsAIShip)
			{
				return sliderData;
			}
			CondOwner condOwner = base.ShipUs.aNavs.FirstOrDefault<CondOwner>();
			if (condOwner != null)
			{
				Dictionary<string, string> dictionary = condOwner.mapGUIPropMaps["Panel A"];
				if (condOwner.mapGUIPropMaps.TryGetValue("Panel A", out dictionary))
				{
					string empty = string.Empty;
					float num = 1f;
					if (dictionary.TryGetValue("fCrsLim", out empty) && float.TryParse(empty, out num))
					{
						sliderData.Limit = num;
					}
					if (dictionary.TryGetValue("fCrsCoast", out empty) && float.TryParse(empty, out num))
					{
						sliderData.Coast = num;
					}
				}
			}
			return sliderData;
		}

		private void TorchOff()
		{
			base.ShipUs.SetReactorGPMValue("slidCycle", "0");
			base.ShipUs.SetReactorGPMValue("slidFlow", "0");
			if (base.ShipUs.LoadState >= Ship.Loaded.Edit)
			{
				base.ShipUs.SetThrust(0.0);
			}
			base.ShipUs.bChangedStatus = true;
		}

		public NavData PlanTrip4(NavDataPoint navOrigin, NavDataPoint navDestination, float fLimiter, float fCoast)
		{
			this.nRecursion++;
			if (this.nRecursion > 4)
			{
				return null;
			}
			Point point = navDestination.ObjSS.vVel - navOrigin.ObjSS.vVel;
			Point point2 = navDestination.ObjSS.vPos - navOrigin.ObjSS.vPos;
			Vector2 vector = default(Vector2);
			BodyOrbit bodyOrbit = null;
			CrewSim.system.GetGreatestGravBO(navOrigin.ObjSS, StarSystem.fEpoch, ref vector, ref bodyOrbit);
			double distance = (double)navOrigin.ObjSS.GetRadiusAU() + navOrigin.ObjSS.GetDistance(bodyOrbit.dXReal, bodyOrbit.dYReal);
			JsonAtmosphere atmosphereAtDistance = bodyOrbit.GetAtmosphereAtDistance(distance);
			bool flag = atmosphereAtDistance.GetTotalKPA() > BodyOrbit.AtmoKPaThreshold;
			bool flag2 = !base.ShipUs.NavPlayerManned;
			bool flag3 = base.ShipUs.bFusionReactorRunning;
			bool flag4 = point2.magnitude > 3.342293712194078E-05;
			flag3 = (flag3 && flag4);
			double num;
			if (flag3)
			{
				num = (double)base.ShipUs.GetMaxTorchThrust(fLimiter);
			}
			else
			{
				double x = (base.ShipUs.LiftRotorsThrustStrength <= 0f) ? base.ShipUs.RCSAccelMax : ((double)(base.ShipUs.LiftRotorsThrustStrength / 149597870f));
				if (flag)
				{
					num = MathUtils.Min(x, 1.9016982118751132E-10);
				}
				else
				{
					num = MathUtils.Min(base.ShipUs.RCSAccelMax, 1.9016982118751132E-10);
				}
			}
			if (num == 0.0)
			{
				return null;
			}
			NavDataPoint navDataPoint = navOrigin.Clone();
			bool flag5 = false;
			if (flag3)
			{
				ShipSitu shipSitu = this.CreateBorderSituBetweenPoints(navOrigin.ObjSS, navDestination.ObjSS, navOrigin.ArrivalTime, false);
				if (shipSitu != null && shipSitu != navOrigin.ObjSS)
				{
					Point v = shipSitu.vPos - navOrigin.ObjSS.vPos;
					v *= 1.1;
					if (navDataPoint.ObjSS.bBOLocked || navDataPoint.ObjSS.bIsBO)
					{
						navDataPoint.ObjSS.vBOOffsetx += v.X;
						navDataPoint.ObjSS.vBOOffsety += v.Y;
					}
					else
					{
						navDataPoint.ObjSS.vPosx += v.X;
						navDataPoint.ObjSS.vPosy += v.Y;
					}
					if (flag2)
					{
						double distance2 = MathUtils.GetDistance(navOrigin.ObjSS, shipSitu);
						flag5 = (distance2 != 0.0 && distance2 < 4.01075203626533E-06);
					}
				}
			}
			navDataPoint.ObjSS.UpdateTime(navDataPoint.ArrivalTime, false);
			if (flag5)
			{
				navDataPoint.ObjSS.LockToBO(navDataPoint.ArrivalTime, false);
				return this.PlanTrip4(navOrigin, navDataPoint, fLimiter, fCoast);
			}
			if (flag2)
			{
				ShipSitu dockingNavPoint = FlightCPU.GetDockingNavPoint(navDataPoint.ObjSS, navDestination.ObjSS, navDataPoint.ArrivalTime, false);
				if (dockingNavPoint != null)
				{
					NavDataPoint navDestination2 = new NavDataPoint(navDataPoint.ArrivalTime, dockingNavPoint);
					return this.PlanTrip4(navDataPoint, navDestination2, fLimiter, fCoast);
				}
			}
			this.fTorchSpeedLimit = 0.00020039887409959505;
			if (!flag4)
			{
				if (this._ai == null)
				{
					this.fTorchSpeedLimit = ((!flag) ? 5.013440183831985E-09 : 2.5067200919159927E-09);
				}
				else
				{
					this.fTorchSpeedLimit = this._ai.MaxSpeed(new bool?(flag));
				}
			}
			this.LastSpeedMax = 0.0;
			double num2 = 2.0 * Math.Sqrt(point2.magnitude / num);
			num2 += point.magnitude / num;
			if (flag2)
			{
				ShipSitu dockingNavPoint2 = FlightCPU.GetDockingNavPoint(navDataPoint.ObjSS, navDestination.ObjSS, navDataPoint.ArrivalTime + num2, true);
				if (dockingNavPoint2 != null)
				{
					NavDataPoint navDestination3 = new NavDataPoint(navDataPoint.ArrivalTime + num2, dockingNavPoint2);
					return this.PlanTrip4(navDataPoint, navDestination3, fLimiter, fCoast);
				}
			}
			NavData navData = this.PlanTrip4Sub(navDataPoint, navDestination, fLimiter, num2, flag3, flag);
			if (navData == null)
			{
				return null;
			}
			float num3 = 10f;
			double num4 = num2;
			int num5 = 0;
			Point point3;
			while ((float)num5 < num3)
			{
				navDestination.ObjSS.UpdateTime(navData.Destination.ArrivalTime, false);
				point3 = navData.Destination.ObjSS.vPos - navDestination.ObjSS.vPos;
				double num6 = navData.Destination.ArrivalTime - navData.Origin.ArrivalTime;
				double num7 = num6 - num4;
				if (this._debugMode)
				{
					Debug.LogWarning(string.Concat(new object[]
					{
						base.ShipUs.strRegID,
						" plot ",
						num5,
						" offset: ",
						MathUtils.GetDistUnits(point3.X),
						", ",
						MathUtils.GetDistUnits(point3.Y),
						"; dArrivalTime: ",
						num7
					}));
				}
				num4 = num6;
				Point point4 = point2;
				double num8 = (!flag3) ? ((double)CollisionManager.GetCollisionDistanceAU(navDestination.ObjSS, navDataPoint.ObjSS) * 2.0) : 2.005376018132665E-06;
				point4 = point4.normalized * -num8;
				ShipSitu shipSitu2 = new ShipSitu(navDestination.ObjSS);
				shipSitu2.vBOOffsetx += point4.X;
				shipSitu2.vBOOffsety += point4.Y;
				NavDataPoint navDestination4 = new NavDataPoint(navData.Destination.ArrivalTime, shipSitu2);
				if (num5 == 1)
				{
					double val = (this._ai == null) ? 5.013440183831985E-09 : this._ai.MaxSpeed(new bool?(flag));
					this.fTorchSpeedLimit = Math.Max(val, (double)fCoast * this.LastSpeedMax);
				}
				navData = this.PlanTrip4Sub(navData.Origin, navDestination4, fLimiter, num6, flag3, flag);
				num5++;
			}
			navDestination.ObjSS.UpdateTime(navData.Destination.ArrivalTime, false);
			point3 = navData.Destination.ObjSS.vPos - navDestination.ObjSS.vPos;
			if (this._debugMode)
			{
				Debug.LogWarning(string.Concat(new string[]
				{
					base.ShipUs.strRegID,
					" final offset: ",
					MathUtils.GetDistUnits(point3.X),
					", ",
					MathUtils.GetDistUnits(point3.Y)
				}));
			}
			navDestination.ObjSS.UpdateTime(StarSystem.fEpoch, false);
			return navData;
		}

		public NavData PlanTrip4Sub(NavDataPoint navOrigin, NavDataPoint navDestination, float fLimiter, double fDurationEst, bool bUsesTorch, bool bInAtmo)
		{
			bool flag = false;
			navDestination.ObjSS.UpdateTime(StarSystem.fEpoch + fDurationEst, false);
			double num;
			if (bUsesTorch)
			{
				num = (double)base.ShipUs.GetMaxTorchThrust(fLimiter);
			}
			else if (bInAtmo)
			{
				num = MathUtils.Min((double)(base.ShipUs.LiftRotorsThrustStrength / 149597870f), 1.9016982118751132E-10);
			}
			else
			{
				num = MathUtils.Min(base.ShipUs.RCSAccelMax, 1.9016982118751132E-10);
			}
			if (num == 0.0)
			{
				return null;
			}
			double num2 = num / 6.6845869117759804E-12 / 9.8100004196167;
			NavData navData = new NavData(base.ShipUs);
			NavDataPoint navDataPoint = navOrigin.Clone();
			navDataPoint.ObjSS.bIsBO = (navDataPoint.ObjSS.bBOLocked = false);
			navDataPoint.ObjSS.vBOOffsetx = (navDataPoint.ObjSS.vBOOffsety = 0.0);
			navData.InAtmo = bInAtmo;
			navData.AddNavDataPoint(navDataPoint);
			if (this._debugMode && flag)
			{
				GUIOrbitDraw.AddDebugDraw(base.ShipUs.strRegID + ":Start", navDataPoint.ObjSS, Color.green, false);
			}
			Point point = navDestination.ObjSS.vVel - navOrigin.ObjSS.vVel;
			double num3 = point.magnitude / num;
			Point point2 = point.normalized * num;
			Point point3 = 0.5 * point2 * num3 * num3 + navDataPoint.ObjSS.vVel * num3 + navDataPoint.ObjSS.vPos;
			point3 += navDestination.ObjSS.vVel * (fDurationEst - num3);
			Point point4 = navDestination.ObjSS.vPos - point3;
			Point point5 = point4.normalized * num;
			double magnitude = point4.magnitude;
			double num4 = Math.Sqrt(magnitude / num);
			double num5 = this.fTorchSpeedLimit / num;
			double num6 = num4 - num5;
			if (num5 > num4)
			{
				num6 = 0.0;
			}
			else
			{
				num4 -= num6;
				double num7 = 0.5 * num * num4 * num4;
				double num8 = magnitude - num7 * 2.0;
				num6 = num8 / this.fTorchSpeedLimit;
			}
			int num9 = 10;
			double fTimeSegment = num3 / (double)num9;
			double lastSpeedMax = this.LastSpeedMax;
			if (this._debugMode && flag)
			{
				Debug.LogWarning(string.Concat(new string[]
				{
					base.ShipUs.strRegID,
					"-WPStart Dist to Dest: ",
					MathUtils.GetDistUnits((navDataPoint.ObjSS.vPos - navDestination.ObjSS.vPos).magnitude),
					"; VREL to Dest: ",
					MathUtils.GetDistUnits((navDataPoint.ObjSS.vVel - navDestination.ObjSS.vVel).magnitude)
				}));
			}
			this.PlotBurnLoop(navData, num9, fTimeSegment, point2, bUsesTorch, bInAtmo, fLimiter, navDestination, ":WPV", 1, 0);
			this.LastSpeedMax = lastSpeedMax;
			fTimeSegment = num4 / (double)num9;
			if (this._debugMode && flag)
			{
				Debug.LogWarning(base.ShipUs.strRegID + "-dXY+Start");
			}
			this.PlotBurnLoop(navData, num9, fTimeSegment, point5, bUsesTorch, bInAtmo, fLimiter, navDestination, ":WPXY+", 2, 0);
			fTimeSegment = num6 / (double)num9;
			if (this._debugMode && flag)
			{
				Debug.LogWarning(base.ShipUs.strRegID + "-dXYCStart");
			}
			if (num6 > 0.0)
			{
				this.PlotBurnLoop(navData, num9, fTimeSegment, default(Point), bUsesTorch, bInAtmo, fLimiter, navDestination, ":WPXYC", 0, 0);
			}
			fTimeSegment = num4 / (double)num9;
			if (this._debugMode && flag)
			{
				Debug.LogWarning(base.ShipUs.strRegID + "-dXY-Start");
			}
			this.PlotBurnLoop(navData, num9, fTimeSegment, -1.0 * point5, bUsesTorch, bInAtmo, fLimiter, navDestination, ":WPXY-", 0, 2);
			navData.Destination.TorchCycle = 0.0;
			return navData;
		}

		private void PlotBurnLoop(NavData navData, int nWPs, double fTimeSegment, Point vAcc, bool bUsesTorch, bool bInAtmo, float fLimiter, NavDataPoint navDestination, string strWPLabel = ":WP", int nEaseOrg = 0, int nEaseDest = 0)
		{
			if (nEaseOrg > 0)
			{
				this.PlotBurnLoop(navData, nWPs, fTimeSegment / (double)nWPs, vAcc, bUsesTorch, bInAtmo, fLimiter, navDestination, strWPLabel, nEaseOrg - 1, 0);
			}
			bool flag = false;
			NavDataPoint navDataPoint = navData.Destination;
			int num = 0;
			if (nEaseOrg > 0)
			{
				num = 1;
			}
			int num2 = 0;
			if (nEaseDest > 0)
			{
				num2 = 1;
			}
			for (int i = num; i < nWPs - num2; i++)
			{
				Point point = navDataPoint.ObjSS.vPos + fTimeSegment * navDataPoint.ObjSS.vVel + 0.5 * fTimeSegment * fTimeSegment * vAcc;
				Point v = navDataPoint.ObjSS.vVel + fTimeSegment * vAcc;
				ShipSitu shipSitu = new ShipSitu();
				shipSitu.vPosx = point.X;
				shipSitu.vPosy = point.Y;
				shipSitu.vVelX = v.X;
				shipSitu.vVelY = v.Y;
				if (bUsesTorch)
				{
					shipSitu.vAccIn = new Vector2((float)vAcc.X, (float)vAcc.Y);
				}
				else
				{
					shipSitu.vAccRCS = new Vector2((float)vAcc.X, (float)vAcc.Y);
				}
				shipSitu.bIsBO = (shipSitu.bBOLocked = false);
				shipSitu.vBOOffsetx = (shipSitu.vBOOffsety = 0.0);
				shipSitu.fRot = this.GetRotation(vAcc);
				if (vAcc.X == 0.0 && vAcc.Y == 0.0)
				{
					shipSitu.fRot = navDataPoint.ObjSS.fRot;
				}
				double dVdiff = vAcc.magnitude * fTimeSegment;
				double num3 = base.ShipUs.CalculateRCSFuelConsumption(dVdiff);
				if (bInAtmo)
				{
					num3 = navDataPoint.FuelLevel;
				}
				else
				{
					num3 = navDataPoint.FuelLevel - num3;
				}
				double num4 = navDataPoint.TorchFuelLevel;
				double torchCycle = 0.0;
				if (bUsesTorch)
				{
					num3 = navDataPoint.FuelLevel;
					num4 = base.ShipUs.CalculateTorchFuelConsumption(dVdiff, fLimiter);
					if (num4 > 0.0)
					{
						torchCycle = (double)fLimiter * 1.0;
					}
					else
					{
						torchCycle = 0.0;
					}
					num4 = navDataPoint.TorchFuelLevel - num4;
				}
				double magnitude = (v - navDestination.ObjSS.vVel).magnitude;
				if (magnitude > this.LastSpeedMax)
				{
					this.LastSpeedMax = magnitude;
				}
				NavDataPoint navDataPoint2 = new NavDataPoint(navDataPoint.ArrivalTime + fTimeSegment, shipSitu, num3, torchCycle, num4);
				navData.AddNavDataPoint(navDataPoint2);
				if (this._debugMode && flag)
				{
					GUIOrbitDraw.AddDebugDraw(base.ShipUs.strRegID + strWPLabel + i, navDataPoint2.ObjSS, Color.red, false);
				}
				navDataPoint = navDataPoint2;
				if (navDestination != null)
				{
					navDestination.ObjSS.UpdateTime(navDataPoint2.ArrivalTime, false);
					if (this._debugMode && flag)
					{
						Debug.LogWarning(string.Concat(new object[]
						{
							base.ShipUs.strRegID,
							strWPLabel,
							i,
							" Dist to Dest: ",
							MathUtils.GetDistUnits((navDataPoint2.ObjSS.vPos - navDestination.ObjSS.vPos).magnitude),
							"; VREL to Dest: ",
							MathUtils.GetDistUnits((navDataPoint2.ObjSS.vVel - navDestination.ObjSS.vVel).magnitude)
						}));
					}
				}
			}
			if (nEaseDest > 0)
			{
				this.PlotBurnLoop(navData, nWPs, fTimeSegment / (double)nWPs, vAcc, bUsesTorch, bInAtmo, fLimiter, navDestination, strWPLabel, 0, nEaseDest - 1);
			}
		}

		public static bool IsInterregionalPath(ShipSitu origin, ShipSitu destination)
		{
			return origin != null && destination != null && origin.GetDistance(destination) > 3.008064027198998E-06;
		}

		private ShipSitu CreateBorderSituBetweenPoints(ShipSitu a, ShipSitu b, double fEpoch, bool createSituNearDestination = false)
		{
			if (a == null || b == null)
			{
				return null;
			}
			ShipSitu shipSitu = new ShipSitu();
			if (createSituNearDestination)
			{
				shipSitu.CopyFrom(b, true);
			}
			else
			{
				shipSitu.CopyFrom(a, true);
			}
			shipSitu.IsRegionBorderPoint = true;
			Ship ship = (!createSituNearDestination) ? CrewSim.system.GetNearestStation(a.vPosx, a.vPosy, true) : CrewSim.system.GetNearestStation(b.vPosx, b.vPosy, true);
			if (ship == null)
			{
				if (this._debugMode)
				{
					Debug.LogWarning("No closest station found returning " + ((!createSituNearDestination) ? " origin-situ" : " destination-situ"));
				}
				return (!createSituNearDestination) ? a : b;
			}
			float num = 6.684587E-09f;
			Point[] array = (!createSituNearDestination) ? MathUtils.FindCircleLineIntersections(ship.objSS.vPosx, ship.objSS.vPosy, (double)(2.005376E-06f + num), b.vPos, a.vPos) : MathUtils.FindCircleLineIntersections(ship.objSS.vPosx, ship.objSS.vPosy, (double)(2.005376E-06f + num), a.vPos, b.vPos);
			if (array.Length == 1)
			{
				shipSitu.vPosx = array[0].X;
				shipSitu.vPosy = array[0].Y;
				if (this._debugMode)
				{
					Debug.LogWarning("CreateBorderSitu: Only 1 intersection! closest station: " + ship.strRegID);
				}
				return shipSitu;
			}
			if (array.Length != 2)
			{
				return null;
			}
			if (!createSituNearDestination)
			{
				double distance = MathUtils.GetDistance(array[0].X, array[0].Y, b.vPosx, b.vPosy);
				double distance2 = MathUtils.GetDistance(array[1].X, array[1].Y, b.vPosx, b.vPosy);
				if (distance < distance2)
				{
					shipSitu.vPosx = array[0].X;
					shipSitu.vPosy = array[0].Y;
					if (this._debugMode)
					{
						Debug.LogWarning("CreateBorderSitu: Closest station: " + ship.strRegID + " invert:false- CB<DB");
					}
					return shipSitu;
				}
				shipSitu.vPosx = array[1].X;
				shipSitu.vPosy = array[1].Y;
				if (this._debugMode)
				{
					Debug.LogWarning("CreateBorderSitu: Closest station: " + ship.strRegID + " invert:false- CB>DB");
				}
				return shipSitu;
			}
			else
			{
				double distance3 = MathUtils.GetDistance(array[0].X, array[0].Y, a.vPosx, a.vPosy);
				double distance4 = MathUtils.GetDistance(array[1].X, array[1].Y, a.vPosx, a.vPosy);
				if (distance3 < distance4)
				{
					shipSitu.vPosx = array[0].X;
					shipSitu.vPosy = array[0].Y;
					if (this._debugMode)
					{
						Debug.LogWarning("CreateBorderSitu: Closest station: " + ship.strRegID + " invert:true- CA<DA");
					}
					return shipSitu;
				}
				shipSitu.vPosx = array[1].X;
				shipSitu.vPosy = array[1].Y;
				if (this._debugMode)
				{
					Debug.LogWarning("CreateBorderSitu: Closest station: " + ship.strRegID + " invert:true- CA>DA");
				}
				return shipSitu;
			}
		}

		private double GetATCSpeedLimit(Ship atc, Point vVel)
		{
			double dX = vVel.X - atc.objSS.vVelX;
			double dY = vVel.Y - atc.objSS.vVelY;
			double magnitude = MathUtils.GetMagnitude(dX, dY);
			double num = (this._ai == null) ? 5.013440183831985E-09 : this._ai.MaxSpeed(new bool?(false));
			if (magnitude > num * 0.800000011920929)
			{
				return magnitude + num;
			}
			return num;
		}

		public NavDataPoint CreateNavPointStatic(ShipSitu target, double fEpoch, bool isBorder = false, bool bLockToBO = true)
		{
			ShipSitu shipSitu = new ShipSitu(target);
			if (!shipSitu.bBOLocked && !shipSitu.bIsBO && bLockToBO)
			{
				BodyOrbit nearestBO = CrewSim.system.GetNearestBO(shipSitu, fEpoch, false);
				shipSitu.LockToBO(nearestBO, -1.0);
			}
			Point vVel = new Point(target.vVel.X, target.vVel.Y);
			if (isBorder || target.IsRegionBorderPoint)
			{
				Ship nearestStationRegional = CrewSim.system.GetNearestStationRegional(target.vPosx, target.vPosy);
				double atcspeedLimit = this.GetATCSpeedLimit(nearestStationRegional, vVel);
				vVel = target.vVel.normalized * atcspeedLimit + nearestStationRegional.objSS.vVel;
				shipSitu.vVelX = vVel.X;
				shipSitu.vVelY = vVel.Y;
				shipSitu.IsRegionBorderPoint = true;
			}
			return new NavDataPoint(fEpoch, shipSitu, base.ShipUs.GetRCSRemain(), 0.0, base.ShipUs.fShallowFusionRemain);
		}

		private float GetRotation(Point vDir)
		{
			return -Mathf.Atan2((float)vDir.X, (float)vDir.Y);
		}

		private readonly IAICharacter _ai;

		public double fTorchSpeedLimit = 0.00020039887409959505;

		public double LastSpeedMax;

		public bool _debugMode;

		public int nRecursion;

		private int _rejectedAIPathCounter;

		private class SliderData
		{
			public float Limit = 1f;

			public float Coast = 1f;
		}
	}
}
