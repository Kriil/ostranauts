using System;
using System.Collections.Generic;

namespace Ostranauts.Condowner
{
	public class CondHistory
	{
		public CondHistory()
		{
			this.mapInteractions = new Dictionary<string, InteractionHistory>();
		}

		public CondHistory(string strCondName)
		{
			this.strCondName = strCondName;
			this.mapInteractions = new Dictionary<string, InteractionHistory>();
		}

		public string strCondName { get; set; }

		public Dictionary<string, InteractionHistory> mapInteractions { get; set; }

		public CondHistory Clone()
		{
			CondHistory condHistory = new CondHistory(this.strCondName);
			foreach (KeyValuePair<string, InteractionHistory> keyValuePair in this.mapInteractions)
			{
				condHistory.mapInteractions[keyValuePair.Key] = keyValuePair.Value.Clone();
			}
			return condHistory;
		}

		public JsonCondHistory GetJson()
		{
			JsonCondHistory jsonCondHistory = new JsonCondHistory();
			jsonCondHistory.strCondName = this.strCondName;
			List<JsonInteractionHistory> list = new List<JsonInteractionHistory>();
			foreach (InteractionHistory interactionHistory in this.mapInteractions.Values)
			{
				if (interactionHistory != null && !string.IsNullOrEmpty(interactionHistory.strName))
				{
					list.Add(interactionHistory.GetJson());
				}
			}
			jsonCondHistory.mapInteractions = list.ToArray();
			return jsonCondHistory;
		}

		public void Destroy()
		{
			foreach (InteractionHistory interactionHistory in this.mapInteractions.Values)
			{
				interactionHistory.Destroy();
			}
			this.mapInteractions.Clear();
			this.mapInteractions = null;
		}

		public void AddInteractionScore(string strInteraction, float fScore, bool bNew)
		{
			if (strInteraction == null)
			{
				return;
			}
			if (!this.mapInteractions.ContainsKey(strInteraction))
			{
				this.mapInteractions[strInteraction] = new InteractionHistory(strInteraction);
			}
			this.mapInteractions[strInteraction].AddInteractionScore(fScore, bNew);
		}

		public void AddCondScore(string strInteraction, string strCond, float fScore, bool bNew)
		{
			if (strInteraction == null)
			{
				return;
			}
			InteractionHistory interactionHistory;
			if (this.mapInteractions.TryGetValue(strInteraction, out interactionHistory))
			{
				interactionHistory.AddCondScore(strCond, fScore, bNew);
			}
			else
			{
				this.mapInteractions[strInteraction] = new InteractionHistory(strInteraction);
			}
		}

		public override string ToString()
		{
			return this.strCondName;
		}

		public string Print(string strPrefix)
		{
			string text = string.Empty;
			foreach (string key in this.mapInteractions.Keys)
			{
				text += this.mapInteractions[key].Print(strPrefix + "," + this.strCondName);
			}
			return text;
		}
	}
}
