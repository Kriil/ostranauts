using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class OnKeyDownEvent : UnityEvent<KeyCode>
	{
	}
}
