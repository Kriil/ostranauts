using System;
using Ostranauts.Objectives;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnAlarmObjectiveEvent : UnityEvent<AlarmObjective>
	{
	}
}
