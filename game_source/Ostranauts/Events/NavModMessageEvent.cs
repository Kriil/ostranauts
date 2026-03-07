using System;
using Ostranauts.ShipGUIs.NavStation;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class NavModMessageEvent : UnityEvent<NavModMessageType, object>
	{
	}
}
