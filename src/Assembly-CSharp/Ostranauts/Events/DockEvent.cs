using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class DockEvent : UnityEvent<Ship, Ship>
	{
	}
}
