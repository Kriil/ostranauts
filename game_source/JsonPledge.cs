using System;

[Serializable]
// Relationship/social pledge definition.
// Likely used by social AI or promises/agreements between CondOwners, with
// interactions that start, end, or emergency-cancel the pledge state.
public class JsonPledge
{
	// `strName` is the internal id; `strNameFriendly` is the UI-facing label.
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strType { get; set; }

	public string strIATrigger { get; set; }

	// Likely references `data/interactions` ids that conclude or interrupt the pledge.
	public string[] aIAEnd { get; set; }

	public string strIAEmergency { get; set; }

	public string strThemID { get; set; }

	public bool bThemAllowDocked { get; set; }

	public bool bThemForgetOnDo { get; set; }

	public int nPriority { get; set; }

	// Minimal validity check used before registration or runtime use.
	public bool Valid()
	{
		return this.strType != null && this.strIATrigger != null;
	}

	// Shallow equality helper for comparing pledge templates.
	public static bool Same(JsonPledge pl1, JsonPledge pl2)
	{
		return pl1 == pl2 || (pl1 != null && pl2 != null && !(pl1.strType != pl2.strType) && !(pl1.strIATrigger != pl2.strIATrigger) && pl1.aIAEnd == pl2.aIAEnd && !(pl1.strIAEmergency != pl2.strIAEmergency) && pl1.bThemAllowDocked == pl2.bThemAllowDocked && pl1.bThemForgetOnDo == pl2.bThemForgetOnDo && pl1.nPriority == pl2.nPriority);
	}

	// Shallow copy for quick duplication.
	public JsonPledge Clone()
	{
		return (JsonPledge)base.MemberwiseClone();
	}

	// Deep-clones pledge-linked interaction ids by replacing a token inside their names.
	// Likely used when generating personalized variants per character/relationship.
	public JsonPledge CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		JsonPledge jsonPledge = this.Clone();
		jsonPledge.strName = this.strName.Replace(strFind, strReplace);
		jsonPledge.strIAEmergency = JsonInteraction.CloneDeep(this.strIAEmergency, strReplace, strFind);
		if (this.aIAEnd != null)
		{
			jsonPledge.aIAEnd = (string[])this.aIAEnd.Clone();
			for (int i = 0; i < this.aIAEnd.Length; i++)
			{
				jsonPledge.aIAEnd[i] = JsonInteraction.CloneDeep(this.aIAEnd[i], strReplace, strFind);
			}
		}
		jsonPledge.strIATrigger = JsonInteraction.CloneDeep(this.strIATrigger, strReplace, strFind);
		DataHandler.dictPledges[jsonPledge.strName] = jsonPledge;
		return jsonPledge;
	}

	// String-id helper that clones on demand and returns the new pledge id.
	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		JsonPledge jsonPledge = null;
		if (!DataHandler.dictPledges.TryGetValue(strOrigName, out jsonPledge))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		JsonPledge jsonPledge2 = null;
		if (!DataHandler.dictPledges.TryGetValue(text, out jsonPledge2))
		{
			jsonPledge2 = jsonPledge.CloneDeep(strFind, strReplace);
		}
		return text;
	}
}
