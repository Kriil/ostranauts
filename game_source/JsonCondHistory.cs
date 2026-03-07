using System;
using System.Collections.Generic;
using Ostranauts.Condowner;

[Serializable]
public class JsonCondHistory
{
	public string strCondName { get; set; }

	public JsonInteractionHistory[] mapInteractions { get; set; }

	public CondHistory GetData()
	{
		CondHistory condHistory = new CondHistory();
		condHistory.strCondName = this.strCondName;
		condHistory.mapInteractions = new Dictionary<string, InteractionHistory>();
		if (this.mapInteractions != null)
		{
			foreach (JsonInteractionHistory jsonInteractionHistory in this.mapInteractions)
			{
				InteractionHistory data = jsonInteractionHistory.GetData();
				if (data != null)
				{
					condHistory.mapInteractions.Add(data.strName, data);
				}
			}
		}
		return condHistory;
	}
}
