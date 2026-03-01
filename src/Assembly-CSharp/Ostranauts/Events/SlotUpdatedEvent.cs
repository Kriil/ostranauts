using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class SlotUpdatedEvent : UnityEvent<CondOwner, CondOwner>
	{
	}
}
