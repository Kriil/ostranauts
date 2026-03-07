using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class ManeuverEvent : UnityEvent<string, bool>
	{
	}
}
