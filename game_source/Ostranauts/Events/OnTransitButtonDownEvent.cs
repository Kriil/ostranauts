using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnTransitButtonDownEvent : UnityEvent<JsonTransitConnection>
	{
	}
}
