using System;
using System.Collections.Generic;
using Ostranauts.Objectives;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnShipSubscriptionUpdatedEvent : UnityEvent<List<Objective>>
	{
	}
}
