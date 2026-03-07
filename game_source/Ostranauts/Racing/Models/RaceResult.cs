using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core.Models;

namespace Ostranauts.Racing.Models
{
	public class RaceResult
	{
		public RaceResult()
		{
		}

		public RaceResult(JsonRaceResultSave jResult)
		{
			this.TrackName = jResult.strTrackName;
			this.PilotName = jResult.strPilotName;
			this.PointsEarned = jResult.nPointsEarned;
			this.FinishingPosition = jResult.nFinishingPosition;
			if (jResult.aLapTimes != null)
			{
				List<LapTime> list = new List<LapTime>();
				foreach (JsonLapTime jLap in jResult.aLapTimes)
				{
					list.Add(new LapTime(jLap));
				}
				this.LapTimes = list.ToArray();
			}
		}

		public double[] GetLapTimes()
		{
			if (this.LapTimes == null)
			{
				return new double[0];
			}
			List<double> list = new List<double>();
			foreach (LapTime lapTime in this.LapTimes)
			{
				list.Add(lapTime.TotalTime);
			}
			return list.ToArray();
		}

		public double TotalTime
		{
			get
			{
				double result;
				if (this.LapTimes != null)
				{
					result = this.LapTimes.Sum((LapTime x) => x.TotalTime);
				}
				else
				{
					result = 0.0;
				}
				return result;
			}
		}

		public double GetWayPointTimeByID(string wpID, int lapIndex)
		{
			LapTime lapTime = this.LapTimes[lapIndex];
			if (lapTime.WayPointTimes != null)
			{
				foreach (Tuple<string, double> tuple in lapTime.WayPointTimes)
				{
					if (tuple.Item1 == wpID)
					{
						return tuple.Item2;
					}
				}
			}
			return double.PositiveInfinity;
		}

		public JsonRaceResultSave GetJson()
		{
			JsonRaceResultSave jsonRaceResultSave = new JsonRaceResultSave
			{
				strTrackName = this.TrackName,
				strPilotName = this.PilotName,
				nPointsEarned = this.PointsEarned,
				nFinishingPosition = this.FinishingPosition
			};
			if (this.LapTimes != null)
			{
				List<JsonLapTime> list = new List<JsonLapTime>();
				foreach (LapTime lapTime in this.LapTimes)
				{
					if (lapTime != null)
					{
						list.Add(lapTime.GetJson());
					}
				}
				jsonRaceResultSave.aLapTimes = list.ToArray();
			}
			return jsonRaceResultSave;
		}

		public string TrackName;

		public string PilotName;

		public int PointsEarned;

		public int FinishingPosition;

		public LapTime[] LapTimes;
	}
}
