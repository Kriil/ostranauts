using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;

namespace Ostranauts.Racing.Models
{
	public class LapTime
	{
		public LapTime()
		{
		}

		public LapTime(JsonRaceTrack jTrack)
		{
			this.TotalTime = jTrack.fAvgLapTime;
			this.WayPointTimes = new List<Tuple<string, double>>();
			foreach (JsonTrackWaypoint jsonTrackWaypoint in jTrack.aWaypoints)
			{
				this.WayPointTimes.Add(new Tuple<string, double>(jsonTrackWaypoint.ID, jsonTrackWaypoint.fWayPointTime));
			}
		}

		public LapTime(JsonLapTime jLap)
		{
			this.TotalTime = jLap.fTotalTime;
			this.WayPointTimes = new List<Tuple<string, double>>();
			if (jLap.aWayPointTimes == null || jLap.aWayPointIDs == null || jLap.aWayPointTimes.Length != jLap.aWayPointIDs.Length)
			{
				return;
			}
			for (int i = 0; i < jLap.aWayPointTimes.Length; i++)
			{
				this.WayPointTimes.Add(new Tuple<string, double>(jLap.aWayPointIDs[i], jLap.aWayPointTimes[i]));
			}
		}

		public void AddWayPointTime(RaceWayPoint wayPoint)
		{
			if (wayPoint == null || wayPoint.JsonTrackWaypoint == null || string.IsNullOrEmpty(wayPoint.JsonTrackWaypoint.ID))
			{
				return;
			}
			if (this.WayPointTimes == null)
			{
				this.WayPointTimes = new List<Tuple<string, double>>();
			}
			foreach (Tuple<string, double> tuple in this.WayPointTimes)
			{
				if (!(tuple.Item1 != wayPoint.JsonTrackWaypoint.ID))
				{
					tuple.Item2 = wayPoint.TimeStamp;
					return;
				}
			}
			this.WayPointTimes.Add(new Tuple<string, double>(wayPoint.JsonTrackWaypoint.ID, wayPoint.TimeStamp));
		}

		public JsonLapTime GetJson()
		{
			JsonLapTime jsonLapTime = new JsonLapTime
			{
				fTotalTime = this.TotalTime
			};
			if (this.WayPointTimes != null && this.WayPointTimes.Count > 0)
			{
				jsonLapTime.aWayPointIDs = (from x in this.WayPointTimes
				select x.Item1).ToArray<string>();
				jsonLapTime.aWayPointTimes = (from x in this.WayPointTimes
				select x.Item2).ToArray<double>();
			}
			return jsonLapTime;
		}

		public double TotalTime;

		public List<Tuple<string, double>> WayPointTimes;
	}
}
