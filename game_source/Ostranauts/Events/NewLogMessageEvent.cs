using System;
using Ostranauts.Ships.Comms;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class NewLogMessageEvent : UnityEvent<ShipMessage>
	{
	}
}
