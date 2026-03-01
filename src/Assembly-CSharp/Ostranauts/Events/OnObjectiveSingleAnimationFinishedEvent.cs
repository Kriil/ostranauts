using System;
using Ostranauts.Objectives;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnObjectiveSingleAnimationFinishedEvent : UnityEvent<ObjectivePanel>
	{
	}
}
