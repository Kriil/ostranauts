using System;
using UnityEngine;

namespace Ostranauts.Ships.Comms
{
	public class Clearance
	{
		public Clearance()
		{
		}

		public Clearance(bool docked)
		{
			this.DockID = Mathf.RoundToInt((float)UnityEngine.Random.Range(1, 13)).ToString("00");
			this.Squak = Mathf.RoundToInt((float)UnityEngine.Random.Range(0, 9999)).ToString("0000");
			this.ClearanceType = ((!docked) ? "DOCK" : "PUSHBACK & TAXI");
			this.SquawkID = (UnityEngine.Random.Range(1, 100) < 50);
		}

		public string TargetRegId;

		public double IssueTimestamp;

		public string DockID;

		public string Squak;

		public string ClearanceType;

		public bool SquawkID;
	}
}
