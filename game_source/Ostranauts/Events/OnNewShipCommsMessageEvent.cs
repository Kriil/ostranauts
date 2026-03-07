using System;
using Ostranauts.Ships.Comms;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnNewShipCommsMessageEvent : UnityEvent<ShipMessage>
	{
	}
}
