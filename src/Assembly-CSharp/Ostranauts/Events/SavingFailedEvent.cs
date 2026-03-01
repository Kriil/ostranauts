using System;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class SavingFailedEvent : UnityEvent<Exception>
	{
	}
}
