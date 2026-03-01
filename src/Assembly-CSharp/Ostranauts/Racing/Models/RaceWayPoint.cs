using System;

namespace Ostranauts.Racing.Models
{
	public class RaceWayPoint
	{
		public double? GetTimeSplit()
		{
			if (this.JsonTrackWaypoint.fWayPointTime <= 0.0)
			{
				return null;
			}
			return new double?(this.TimeStamp - this.JsonTrackWaypoint.fWayPointTime);
		}

		public void Destroy()
		{
			this.GoalPostA.Destroy(false);
			this.GoalPostB.Destroy(false);
		}

		public string Name;

		public Ship GoalPostA;

		public Ship GoalPostB;

		public double TimeStamp;

		public double DistanceBetweenWaypoints;

		public bool IsStartFinish;

		public JsonTrackWaypoint JsonTrackWaypoint;
	}
}
