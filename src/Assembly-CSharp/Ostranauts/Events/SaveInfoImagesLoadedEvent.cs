using System;
using Ostranauts.Core.Models;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class SaveInfoImagesLoadedEvent : UnityEvent<SaveInfo>
	{
	}
}
