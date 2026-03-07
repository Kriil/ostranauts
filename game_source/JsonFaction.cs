using System;
using System.Collections.Generic;

// Runtime/save-friendly faction record.
// This tracks faction reputation scores, current members, and an optional
// interaction gate that determines whether a CondOwner counts as "triggered" for the faction.
public class JsonFaction
{
	// Internal id, friendly name, and auto-membership flag.
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public bool bAutoAdd { get; set; }

	public string strInteractionTriggered { get; set; }

	public Dictionary<string, float> dictFactionRels { get; set; }

	public List<string> aMembers { get; set; }

	public string strCompany { get; set; }

	// Ensures the relationship and membership collections exist.
	public void Init()
	{
		this.dictFactionRels = new Dictionary<string, float>();
		this.aMembers = new List<string>();
	}

	// Clears references and destroys any cached interaction object.
	public void Destroy()
	{
		this.strName = null;
		this.strNameFriendly = null;
		this.dictFactionRels.Clear();
		this.dictFactionRels = null;
		this.aMembers.Clear();
		this.aMembers = null;
		if (this._iaTriggered != null)
		{
			this._iaTriggered.Destroy();
			this._iaTriggered = null;
		}
	}

	// Deep-ish copy used for star-system saves or temporary mutation.
	public JsonFaction Clone()
	{
		JsonFaction jsonFaction = new JsonFaction();
		jsonFaction.strName = this.strName;
		jsonFaction.strNameFriendly = this.strNameFriendly;
		jsonFaction.dictFactionRels = new Dictionary<string, float>();
		if (this.dictFactionRels != null)
		{
			foreach (KeyValuePair<string, float> keyValuePair in this.dictFactionRels)
			{
				jsonFaction.dictFactionRels[keyValuePair.Key] = keyValuePair.Value;
			}
		}
		if (this.aMembers != null)
		{
			jsonFaction.aMembers = new List<string>(this.aMembers);
		}
		else
		{
			jsonFaction.aMembers = new List<string>();
		}
		jsonFaction.bAutoAdd = this.bAutoAdd;
		jsonFaction.strInteractionTriggered = this.strInteractionTriggered;
		jsonFaction.strCompany = this.strCompany;
		return jsonFaction;
	}

	// Applies a reputation delta from another faction.
	public void ApplyFactionRep(string strFactionDoing, float fChange)
	{
		if (string.IsNullOrEmpty(strFactionDoing) || strFactionDoing == this.strName || fChange == 0f)
		{
			return;
		}
		if (!this.dictFactionRels.ContainsKey(strFactionDoing))
		{
			this.dictFactionRels[strFactionDoing] = fChange;
		}
		else
		{
			Dictionary<string, float> dictFactionRels;
			(dictFactionRels = this.dictFactionRels)[strFactionDoing] = dictFactionRels[strFactionDoing] + fChange;
		}
	}

	// Returns the current reputation score toward another faction.
	public float GetFactionScore(string strFaction)
	{
		if (string.IsNullOrEmpty(strFaction))
		{
			return 0f;
		}
		if (strFaction == this.strName)
		{
			return 50f;
		}
		float result = 0f;
		if (!this.dictFactionRels.TryGetValue(strFaction, out result))
		{
			return 0f;
		}
		return result;
	}

	// Resolves and caches the trigger interaction, then tests it against the actor.
	// `strInteractionTriggered` likely points at a `data/interactions` id.
	public bool Triggered(CondOwner co)
	{
		if (co == null)
		{
			return false;
		}
		if (this._iaTriggered == null && !string.IsNullOrEmpty(this.strInteractionTriggered))
		{
			this._iaTriggered = DataHandler.GetInteraction(this.strInteractionTriggered, null, false);
		}
		return this._iaTriggered == null || this._iaTriggered.Triggered(co, co, false, false, false, true, null);
	}

	// Collapses the numeric score into a simple reputation bucket.
	public static JsonFaction.Reputation GetReputation(float fScore)
	{
		if (fScore >= 50f)
		{
			return JsonFaction.Reputation.Likes;
		}
		if (fScore > -50f)
		{
			return JsonFaction.Reputation.Neutral;
		}
		return JsonFaction.Reputation.Dislikes;
	}

	private Interaction _iaTriggered;

	private const float LIKES_THRESHOLD = 50f;

	public enum Reputation
	{
		Neutral,
		Likes,
		Dislikes
	}
}
