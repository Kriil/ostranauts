using System;

// Character/person generation filter and template payload.
// Likely used by chargen, NPC spawning, and social/job systems to describe
// who should be created or matched, plus what data gets assigned.
public class JsonPersonSpec
{
	// Defaults unset filters to empty strings and leaves age limits disabled at -1.
	public JsonPersonSpec()
	{
		this.bAlive = true;
		this.strFirstName = string.Empty;
		this.strLastName = string.Empty;
		this.strGender = string.Empty;
		this.strSkin = string.Empty;
		this.strBodyType = string.Empty;
		this.strHomeworldFind = string.Empty;
		this.strHomeworldSet = string.Empty;
		this.strRegID = string.Empty;
		this.strStrata = string.Empty;
		this.strCareerNow = string.Empty;
		this.nAgeMax = -1;
		this.nAgeMin = -1;
		this.nAgeRangeAbove = -1;
		this.nAgeRangeBelow = -1;
	}

	// `strName` is the internal spec id used by the data registry.
	public string strName { get; set; }

	public string strFirstName { get; set; }

	public string strLastName { get; set; }

	public string strGender { get; set; }

	public string strSkin { get; set; }

	public string strBodyType { get; set; }

	public string strCareerNow { get; set; }

	// Likely references ids from careers, loot, condtrigs, and GUI prop-map related data.
	public string strHomeworldFind { get; set; }

	public string strHomeworldSet { get; set; }

	public string strRegID { get; set; }

	public string strStrata { get; set; }

	public string strRelSet { get; set; }

	public string strCTRelFind { get; set; }

	public string strCT { get; set; }

	public string strLoot { get; set; }

	public string strLootConds { get; set; }

	public string strLootCondsPreCareer { get; set; }

	public string[] aGPMSets { get; set; }

	public string strLootIAAdds { get; set; }

	public string[] aFactionAdds { get; set; }

	public int nAgeRangeBelow { get; set; }

	public int nAgeRangeAbove { get; set; }

	public int nAgeMin { get; set; }

	public int nAgeMax { get; set; }

	public bool bAlive { get; set; }

	public bool bAliveIgnore { get; set; }

	public string strType { get; set; }

	// Tests whether a live CondOwner matches this person-spec filter.
	// Used to find or validate existing crew/NPCs against the template rules.
	public bool Matches(CondOwner coRef)
	{
		if (coRef == null)
		{
			return false;
		}
		GUIChargenStack component = coRef.GetComponent<GUIChargenStack>();
		if (!this.bAliveIgnore && this.bAlive != coRef.bAlive)
		{
			return false;
		}
		if (this.strGender != "player" && this.strGender != string.Empty && !coRef.HasCond(this.strGender))
		{
			return false;
		}
		if (this.strSkin != "player" && this.strSkin != string.Empty)
		{
			if (coRef.Crew == null || coRef.Crew.FaceParts == null || coRef.Crew.FaceParts.Length < 3 || coRef.Crew.FaceParts[2].Length < 10)
			{
				return false;
			}
			if (coRef.Crew.FaceParts[2][9].ToString() != this.strSkin)
			{
				return false;
			}
		}
		if (this.strBodyType != "player" && this.strBodyType != string.Empty)
		{
			if (coRef.pspec == null || string.IsNullOrEmpty(coRef.pspec.strBodyType))
			{
				return false;
			}
			if (coRef.pspec.strBodyType != this.strBodyType)
			{
				return false;
			}
		}
		if (this.strFirstName != "player" && !string.IsNullOrEmpty(this.strFirstName) && (coRef.strName == null || coRef.strName.IndexOf(this.strFirstName) != 0))
		{
			return false;
		}
		if (this.strLastName != "player" && !string.IsNullOrEmpty(this.strLastName) && (coRef.strName == null || coRef.strName.IndexOf(this.strLastName) != coRef.strName.Length - this.strLastName.Length))
		{
			return false;
		}
		if (component != null && this.strHomeworldFind != "player" && this.strHomeworldFind != "notplayer" && this.strHomeworldFind != string.Empty)
		{
			JsonHomeworld homeworld = component.GetHomeworld();
			if (homeworld == null || this.strHomeworldFind != homeworld.strATCCode)
			{
				return false;
			}
		}
		if (this.strRegID != "player" && this.strRegID != "notplayer" && this.strRegID != string.Empty && (coRef.ship == null || this.strRegID != coRef.ship.strRegID))
		{
			return false;
		}
		if (this.strStrata != "player" && this.strStrata != string.Empty)
		{
			GUIChargenStack component2 = coRef.GetComponent<GUIChargenStack>();
			if (component2 != null)
			{
				int strata = component2.Strata;
				if (this.strStrata == "illegal")
				{
					if (strata != 0)
					{
						return false;
					}
				}
				else if (this.strStrata == "resident")
				{
					if (strata != 1)
					{
						return false;
					}
				}
				else if (this.strStrata == "citizen" && strata != 2)
				{
					return false;
				}
			}
		}
		if (component != null && this.strCareerNow != "player" && this.strCareerNow != string.Empty)
		{
			CareerChosen latestCareer = component.GetLatestCareer();
			if (latestCareer == null)
			{
				return false;
			}
			JsonCareer jc = latestCareer.GetJC();
			if (jc != null && this.strCareerNow != jc.strName)
			{
				return false;
			}
		}
		if (this.strCT != null)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger(this.strCT);
			if (!condTrigger.Triggered(coRef, null, true))
			{
				return false;
			}
		}
		return true;
	}

	public JsonPersonSpec Clone()
	{
		JsonPersonSpec jsonPersonSpec = (JsonPersonSpec)base.MemberwiseClone();
		if (this.aFactionAdds != null)
		{
			jsonPersonSpec.aFactionAdds = (string[])this.aFactionAdds.Clone();
		}
		if (this.aGPMSets != null)
		{
			jsonPersonSpec.aGPMSets = (string[])this.aGPMSets.Clone();
		}
		jsonPersonSpec.strLootIAAdds = this.strLootIAAdds;
		return jsonPersonSpec;
	}

	public JsonPersonSpec CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind)
		{
			return this.Clone();
		}
		JsonPersonSpec jsonPersonSpec = this.Clone();
		jsonPersonSpec.strName = this.strName.Replace(strFind, strReplace);
		jsonPersonSpec.strCT = CondTrigger.CloneDeep(this.strCT, strReplace, strFind);
		jsonPersonSpec.strCTRelFind = CondTrigger.CloneDeep(this.strCTRelFind, strReplace, strFind);
		jsonPersonSpec.strLoot = Loot.CloneDeep(this.strLoot, strReplace, strFind);
		jsonPersonSpec.strLootConds = Loot.CloneDeep(this.strLootConds, strReplace, strFind);
		jsonPersonSpec.strLootCondsPreCareer = Loot.CloneDeep(this.strLootCondsPreCareer, strReplace, strFind);
		jsonPersonSpec.strLootIAAdds = this.strLootIAAdds.Replace(strFind, strReplace);
		DataHandler.dictPersonSpecs[jsonPersonSpec.strName] = jsonPersonSpec;
		return jsonPersonSpec;
	}

	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		JsonPersonSpec jsonPersonSpec = null;
		if (!DataHandler.dictPersonSpecs.TryGetValue(strOrigName, out jsonPersonSpec))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		JsonPersonSpec jsonPersonSpec2 = null;
		if (!DataHandler.dictPersonSpecs.TryGetValue(text, out jsonPersonSpec2))
		{
			jsonPersonSpec2 = jsonPersonSpec.CloneDeep(strFind, strReplace);
		}
		return text;
	}

	public override string ToString()
	{
		return this.strName;
	}
}
