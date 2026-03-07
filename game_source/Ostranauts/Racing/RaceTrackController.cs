using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Objectives;
using Ostranauts.Racing.Models;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.Commands;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace Ostranauts.Racing
{
	public class RaceTrackController
	{
		public RaceTrackController(Ship station, bool isPractice, string raceTrackName, List<RaceResult> aiTimes = null)
		{
			CrewSim.coPlayer.AddCondAmount("IsInRaceSession", 1.0, 0.0, 0f);
			this.StationHost = station;
			this._isPractice = isPractice;
			this._aiTimes = aiTimes;
			this._jsonTrack = DataHandler.GetRaceTrack(raceTrackName);
			this._remainingLaps = this._jsonTrack.nLaps;
			this._raceBuoys = this.BuildRaceTrack(station, this._jsonTrack);
		}

		public CommandCode RunCommand()
		{
			if (this._raceBuoys == null || this._raceBuoys.Count == 0)
			{
				return CommandCode.Cancelled;
			}
			Ship ship = CrewSim.coPlayer.ship;
			if (ship == null || ship.objSS.bBOLocked)
			{
				return CommandCode.Ongoing;
			}
			int num = 0;
			for (int i = 0; i < this._raceBuoys.Count; i++)
			{
				RaceWayPoint raceWayPoint = this._raceBuoys[i];
				if (raceWayPoint.GoalPostA.bDestroyed || raceWayPoint.GoalPostB.bDestroyed)
				{
					this.RemoveRaceTrack();
					return CommandCode.Cancelled;
				}
				if (raceWayPoint.TimeStamp != 0.0)
				{
					num++;
				}
				else if (this.IsShipBetweenGoalPosts(ship, raceWayPoint))
				{
					if (this._startTimeStamp == 0.0 && i > 0)
					{
						break;
					}
					if (this._startTimeStamp == 0.0)
					{
						this._startTimeStamp = StarSystem.fEpoch;
						this.ShowSessionStartObjective();
					}
					if (this._jsonTrack.RaceTrackType != JsonRaceTrack.TrackType.Orientation && i > 0 && this._raceBuoys[i - 1].TimeStamp == 0.0)
					{
						break;
					}
					if (i == this._raceBuoys.Count - 1 && num < i)
					{
						break;
					}
					this.SaveWaypointTime(raceWayPoint);
					break;
				}
			}
			if (!this.CheckLapFinished())
			{
				return CommandCode.Ongoing;
			}
			if (!this._isPractice)
			{
				this._remainingLaps--;
			}
			this.SaveLapTime(this._raceBuoys.Last<RaceWayPoint>().TimeStamp);
			if (this._remainingLaps > 0)
			{
				foreach (RaceWayPoint raceWayPoint2 in this._raceBuoys)
				{
					raceWayPoint2.TimeStamp = 0.0;
					raceWayPoint2.GoalPostA.publicName = raceWayPoint2.Name + " A";
					raceWayPoint2.GoalPostB.publicName = raceWayPoint2.Name + " B";
				}
				this._startTimeStamp = 0.0;
				return CommandCode.Ongoing;
			}
			this.EndSession();
			return CommandCode.Finished;
		}

		private void SaveWaypointTime(RaceWayPoint wayPoint)
		{
			wayPoint.TimeStamp = StarSystem.fEpoch - this._startTimeStamp;
			if (this._currentLap == null)
			{
				this._currentLap = new LapTime();
			}
			this._currentLap.AddWayPointTime(wayPoint);
			this.ShowWaypointTimeObjective(wayPoint);
		}

		private void SaveLapTime(double lapTime)
		{
			if (this._currentLap == null)
			{
				this._currentLap = new LapTime();
			}
			this._currentLap.TotalTime = lapTime;
			if (this._currentLap.WayPointTimes != null && this._currentLap.WayPointTimes.FirstOrDefault<Tuple<string, double>>() != null && Math.Abs(this._currentLap.WayPointTimes.FirstOrDefault<Tuple<string, double>>().Item2 - lapTime) < 0.01)
			{
				this._currentLap.WayPointTimes.FirstOrDefault<Tuple<string, double>>().Item2 = 0.0;
			}
			double oldpB = 0.0;
			if (this._isPractice)
			{
				oldpB = MonoSingleton<RacingLeagueManager>.Instance.UpdatePersonalBest(this._jsonTrack.strName, this._currentLap);
			}
			this._lapTimes.Add(this._currentLap);
			this._currentLap = null;
			this.ShowLapTimeObjective(lapTime, oldpB);
		}

		private void ShowSessionStartObjective()
		{
			string strDisplayName = (!this._isPractice) ? "Race Started!" : "Practice lap started!";
			string description = (!this._isPractice) ? ("Laps remaining: " + this._remainingLaps) : string.Empty;
			MonoSingleton<ObjectiveTracker>.Instance.AddObjective(new AlarmObjective(AlarmType.race_checkPoint, CrewSim.coPlayer.ship.ShipCO, strDisplayName, description));
		}

		private void ShowWaypointTimeObjective(RaceWayPoint wayPoint)
		{
			if (wayPoint.IsStartFinish)
			{
				return;
			}
			string text = "Time: " + RacingLeagueManager.FormatTime(wayPoint.TimeStamp) + "\n";
			text += this.GetPositionString(wayPoint);
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.ship.ShipCO.strID);
			MonoSingleton<ObjectiveTracker>.Instance.AddObjective(new AlarmObjective(AlarmType.race_checkPoint, CrewSim.coPlayer.ship.ShipCO, "New Sector Time: ", text));
		}

		private void ShowLapTimeObjective(double lapTime, double oldpB)
		{
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.ship.ShipCO.strID);
			List<double> ailapTimeForCurrentLap = this.GetAILapTimeForCurrentLap(this._jsonTrack.nLaps - (this._remainingLaps + 1));
			string text = "Pos: ";
			LapTime personalBestForTrack = MonoSingleton<RacingLeagueManager>.Instance.GetPersonalBestForTrack(this._jsonTrack.strName);
			double num = (!(this._isPractice & personalBestForTrack != null)) ? this._jsonTrack.fAvgLapTime : personalBestForTrack.TotalTime;
			if (ailapTimeForCurrentLap != null && ailapTimeForCurrentLap.Count > 0)
			{
				for (int i = 0; i < ailapTimeForCurrentLap.Count; i++)
				{
					double num2 = ailapTimeForCurrentLap[i];
					if (lapTime < num2)
					{
						text = text + (i + 1) + ", ";
						break;
					}
					if (i == ailapTimeForCurrentLap.Count - 1)
					{
						text = text + (i + 1) + ", ";
					}
				}
				num = ailapTimeForCurrentLap.First<double>();
			}
			else
			{
				text = string.Empty;
				if (oldpB > lapTime)
				{
					num = oldpB;
					text = "New Personal Best: ";
				}
			}
			double num3 = lapTime - num;
			string text2 = (num3 <= 0.0) ? "<color=green> " : "<color=red> +";
			string str = string.Concat(new string[]
			{
				text,
				RacingLeagueManager.FormatTime(lapTime),
				" (",
				text2,
				num3.ToString("F2"),
				" </color>)"
			});
			string str2 = (!this._isPractice && this._remainingLaps != 0) ? ("\nLaps remaining: " + this._remainingLaps) : string.Empty;
			MonoSingleton<ObjectiveTracker>.Instance.AddObjective(new AlarmObjective(AlarmType.race_checkPoint, CrewSim.coPlayer.ship.ShipCO, "New Lap Time: ", str + str2));
		}

		private string GetPositionString(RaceWayPoint wayPoint)
		{
			if (this._aiTimes != null)
			{
				List<double> aitimesForWP = this.GetAITimesForWP(wayPoint.JsonTrackWaypoint.ID, this._jsonTrack.nLaps - this._remainingLaps);
				for (int i = 0; i < aitimesForWP.Count; i++)
				{
					double num = aitimesForWP[i];
					if (wayPoint.TimeStamp <= num)
					{
						int num2 = i + 1;
						double num3 = (i == 0) ? 0.0 : (wayPoint.TimeStamp - aitimesForWP[i - 1]);
						double num4 = (i == aitimesForWP.Count - 1) ? 0.0 : (aitimesForWP[i + 1] - wayPoint.TimeStamp);
						string text = (num2 != 1) ? ("<color=red>" + num3.ToString("F2") + "s</color>") : string.Empty;
						string text2 = (num2 != aitimesForWP.Count) ? ("<color=green>" + num4.ToString("F2") + "s</color>") : string.Empty;
						return string.Concat(new object[]
						{
							"Pos: ",
							num2,
							", Splits: ",
							text,
							" <|>",
							text2
						});
					}
				}
				return string.Concat(new object[]
				{
					"Pos: ",
					aitimesForWP.Count,
					", Splits: <color=red>",
					(wayPoint.TimeStamp - aitimesForWP[aitimesForWP.Count - 1]).ToString("F2"),
					"s</color>"
				});
			}
			if (this._isPractice)
			{
				LapTime personalBestForTrack = MonoSingleton<RacingLeagueManager>.Instance.GetPersonalBestForTrack(this._jsonTrack.strName);
				if (personalBestForTrack == null)
				{
					return string.Empty;
				}
				foreach (Tuple<string, double> tuple in personalBestForTrack.WayPointTimes)
				{
					if (tuple != null)
					{
						if (tuple.Item1 == wayPoint.JsonTrackWaypoint.ID)
						{
							double num5 = wayPoint.TimeStamp - tuple.Item2;
							string str = (num5 >= 0.0) ? "<color=red>" : "<color=green>";
							return "Split to PB: " + str + num5.ToString("F2") + "</color>";
						}
					}
				}
			}
			return string.Empty;
		}

		private List<double> GetAITimesForWP(string waypointID, int currentLapIndex)
		{
			List<double> list = new List<double>();
			foreach (RaceResult raceResult in this._aiTimes)
			{
				list.Add(raceResult.GetWayPointTimeByID(waypointID, currentLapIndex));
			}
			list.Sort();
			return list;
		}

		private List<double> GetAILapTimeForCurrentLap(int currentLapIndex)
		{
			if (this._aiTimes == null)
			{
				return null;
			}
			List<double> list = new List<double>();
			foreach (RaceResult raceResult in this._aiTimes)
			{
				double[] lapTimes = raceResult.GetLapTimes();
				if (currentLapIndex < lapTimes.Length)
				{
					list.Add(lapTimes[currentLapIndex]);
				}
			}
			list.Sort();
			return list;
		}

		private bool IsShipBetweenGoalPosts(Ship playerShip, RaceWayPoint bu)
		{
			double rangeTo = playerShip.GetRangeTo(bu.GoalPostA);
			if (rangeTo > bu.DistanceBetweenWaypoints)
			{
				return false;
			}
			double rangeTo2 = playerShip.GetRangeTo(bu.GoalPostB);
			return rangeTo + rangeTo2 <= bu.DistanceBetweenWaypoints * 1.100000023841858;
		}

		private bool CheckLapFinished()
		{
			foreach (RaceWayPoint raceWayPoint in this._raceBuoys)
			{
				if (raceWayPoint.TimeStamp == 0.0)
				{
					return false;
				}
			}
			return true;
		}

		private void RemoveRaceTrack()
		{
			foreach (RaceWayPoint raceWayPoint in this._raceBuoys)
			{
				raceWayPoint.Destroy();
			}
		}

		public void EndSession()
		{
			this.RemoveRaceTrack();
			CrewSim.coPlayer.ZeroCondAmount("IsInRaceSession");
			MonoSingleton<RacingLeagueManager>.Instance.ReportResults(this._jsonTrack, this._lapTimes, this._aiTimes, this._isPractice);
		}

		private List<RaceWayPoint> BuildRaceTrack(Ship shipStation, JsonRaceTrack jTrack)
		{
			List<RaceWayPoint> list = new List<RaceWayPoint>();
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(shipStation.objSS, StarSystem.fEpoch, false);
			Point v = (shipStation.objSS.vPos - nearestBO.vPos).normalized * ((nearestBO.fRadiusKM + (double)jTrack.fOrbitHeightKM) / 149597872.0);
			Point referencePos = v + nearestBO.vPos;
			for (int i = 0; i < jTrack.aWaypoints.Length; i++)
			{
				string name = (i != 0) ? i.ToString() : "Start";
				RaceWayPoint raceWayPoint = this.SpawnCheckPoint(referencePos, jTrack.aWaypoints[i], name);
				if (raceWayPoint == null)
				{
					Debug.LogWarning("Could not spawn Buoy nr" + i);
				}
				else
				{
					raceWayPoint.Name = name;
					raceWayPoint.IsStartFinish = (i == 0);
					list.Add(raceWayPoint);
				}
			}
			if (jTrack.RaceTrackType != JsonRaceTrack.TrackType.PointToPoint)
			{
				list.Add(new RaceWayPoint
				{
					Name = "End",
					IsStartFinish = true,
					GoalPostA = list.First<RaceWayPoint>().GoalPostA,
					GoalPostB = list.First<RaceWayPoint>().GoalPostB,
					DistanceBetweenWaypoints = list.First<RaceWayPoint>().DistanceBetweenWaypoints,
					JsonTrackWaypoint = list.First<RaceWayPoint>().JsonTrackWaypoint
				});
			}
			else
			{
				RaceWayPoint raceWayPoint2 = list.Last<RaceWayPoint>();
				raceWayPoint2.Name = "End";
				raceWayPoint2.IsStartFinish = true;
			}
			return list;
		}

		private RaceWayPoint SpawnCheckPoint(Point referencePos, JsonTrackWaypoint jWP, string name)
		{
			Ship ship = this.SpawnBuoy(referencePos, jWP.fAx, jWP.fAy, name + " A");
			if (ship == null)
			{
				return null;
			}
			CrewSim.coPlayer.ship.aProxIgnores.Add(ship.strRegID);
			Ship ship2 = this.SpawnBuoy(referencePos, jWP.fBx, jWP.fBy, name + " B");
			if (ship2 == null)
			{
				return null;
			}
			CrewSim.coPlayer.ship.aProxIgnores.Add(ship2.strRegID);
			return new RaceWayPoint
			{
				GoalPostA = ship,
				GoalPostB = ship2,
				DistanceBetweenWaypoints = ship.GetRangeTo(ship2),
				JsonTrackWaypoint = jWP
			};
		}

		private Ship SpawnBuoy(Point referencePos, float x, float y, string name)
		{
			Ship ship = AIShip.SpawnNewShip(this.StationHost.strRegID + this._buoyFactionPostFix, "RaceBuoy", Ship.Damage.New, name, null);
			if (ship == null)
			{
				return null;
			}
			ship.objSS.vPosx = referencePos.X + (double)(x / 149597870f);
			ship.objSS.vPosy = referencePos.Y + (double)(y / 149597870f);
			ship.objSS.LockToBO(-1.0, false);
			ship.objSS.bIsBO = true;
			ship.strXPDR = ship.strRegID;
			ship.bXPDRAntenna = true;
			ship.publicName = name;
			ship.Classification = Ship.TypeClassification.Waypoint;
			ship.ToggleVis(false, true);
			return ship;
		}

		private Ship StationHost;

		private List<RaceWayPoint> _raceBuoys;

		private string _buoyFactionPostFix = "RaceDirector";

		private double _startTimeStamp;

		private bool _trackLoops;

		private int _remainingLaps;

		private List<LapTime> _lapTimes = new List<LapTime>();

		private LapTime _currentLap;

		private bool _isPractice;

		private JsonRaceTrack _jsonTrack;

		private List<RaceResult> _aiTimes;
	}
}
