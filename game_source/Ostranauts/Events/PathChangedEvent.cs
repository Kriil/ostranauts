using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class PathChangedEvent : UnityEvent<string>
	{
	}
}
