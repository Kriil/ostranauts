using System;
using Ostranauts.Social.Models;
using UnityEngine.Events;

namespace Ostranauts.Events
{
	[Serializable]
	public class ContextUpdatedEvent : UnityEvent<Interaction, SocialStakes>
	{
	}
}
