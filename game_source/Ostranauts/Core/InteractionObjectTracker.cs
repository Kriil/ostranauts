using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Tools.ExtensionMethods;

namespace Ostranauts.Core
{
	// Simple interaction object pool/tracker.
	// This reuses Interaction instances by `strName` to cut allocation churn
	// while still keeping a tracked set of live interaction objects by GUID.
	public class InteractionObjectTracker
	{
		// Returns a recycled Interaction when available, otherwise creates a new one.
		// The reset path preserves object pooling while still applying fresh save/json data.
		public Interaction GetObject(JsonInteraction jsonIn, JsonInteractionSave jis)
		{
			Stack<Interaction> stack;
			if (this._dictAvailableInteractions.TryGetValue(jsonIn.strName, out stack) && stack.Count > 0)
			{
				Interaction interaction = stack.Pop();
				if (interaction != null)
				{
					interaction.ResetObject(jsonIn, jis);
					this._dictTrackedInteractions.TryAdd(interaction.id, interaction);
					return interaction;
				}
			}
			Interaction interaction2 = new Interaction(jsonIn, jis);
			this._dictTrackedInteractions.TryAdd(interaction2.id, interaction2);
			this.CheckTrackingSize();
			return interaction2;
		}

		// Removes a live Interaction from tracking and returns it to the name-based pool.
		public void ReleaseObject(Interaction releasedInteraction)
		{
			if (releasedInteraction == null || false || string.IsNullOrEmpty(releasedInteraction.strName))
			{
				if (releasedInteraction == null)
				{
					this._dictTrackedInteractions = this.RemoveNullsFromDictionary();
				}
				return;
			}
			this._dictTrackedInteractions.Remove(releasedInteraction.id);
			Stack<Interaction> stack;
			if (this._dictAvailableInteractions.TryGetValue(releasedInteraction.strName, out stack))
			{
				stack.Push(releasedInteraction);
			}
			else
			{
				Stack<Interaction> stack2 = new Stack<Interaction>();
				stack2.Push(releasedInteraction);
				this._dictAvailableInteractions.Add(releasedInteraction.strName, stack2);
			}
		}

		// Removes an Interaction from tracking without pooling it.
		public void UntrackObject(Interaction releasedInteraction)
		{
			if (releasedInteraction == null)
			{
				return;
			}
			this._dictTrackedInteractions.Remove(releasedInteraction.id);
		}

		// Rebuilds the tracking dictionary without null entries.
		private Dictionary<Guid, Interaction> RemoveNullsFromDictionary()
		{
			return (from x in this._dictTrackedInteractions
			where x.Value != null
			select x).ToDictionary((KeyValuePair<Guid, Interaction> u) => u.Key, (KeyValuePair<Guid, Interaction> u) => u.Value);
		}

		// Hard cap on tracked interactions to avoid unbounded growth.
		// Unclear: this trims the first key snapshot rather than a true LRU list.
		private void CheckTrackingSize()
		{
			if (this._dictTrackedInteractions.Count < 500)
			{
				return;
			}
			Guid[] array = this._dictTrackedInteractions.Keys.ToArray<Guid>();
			int num = 0;
			while (num < 250 && num < array.Length)
			{
				this._dictTrackedInteractions.Remove(array[num]);
				num++;
			}
		}

		private const int _maxTrackedCount = 500;

		private readonly Dictionary<string, Stack<Interaction>> _dictAvailableInteractions = new Dictionary<string, Stack<Interaction>>();

		private Dictionary<Guid, Interaction> _dictTrackedInteractions = new Dictionary<Guid, Interaction>();
	}
}
