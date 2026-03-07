using System;

namespace Ostranauts.Objectives
{
	public class AlarmObjective : Objective
	{
		public AlarmObjective(AlarmType alarmType, CondOwner coTarget, string strDisplayName) : base(coTarget, strDisplayName, null, coTarget.ship.strRegID)
		{
			this.AlarmType = alarmType;
		}

		public AlarmObjective(AlarmType alarmType, CondOwner coTarget, string strDisplayName, string description) : base(coTarget, strDisplayName, null, coTarget.ship.strRegID)
		{
			this.AlarmType = alarmType;
			this.strDisplayDesc = description;
			this.ShowAlways = true;
		}

		public AlarmObjective(AlarmType alarmType, CondOwner coTarget, string strDisplayName, string strCT, string shipCOID, string description) : base(coTarget, strDisplayName, strCT, shipCOID)
		{
			this.strDisplayDesc = description;
			this.AlarmType = alarmType;
		}

		public AlarmObjective(AlarmType alarmType, CondOwner coTarget, string strDisplayName, string strCT, bool stackableObjective = false, string shipCOID = "") : base(coTarget, strDisplayName, strCT, shipCOID)
		{
			this.AlarmType = alarmType;
		}

		public AlarmType AlarmType { get; private set; }

		public readonly bool ShowAlways;
	}
}
