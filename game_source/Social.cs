using System;
using System.Collections.Generic;
using UnityEngine;

public class Social : MonoBehaviour
{
	private void Awake()
	{
		this.Init(null);
	}

	public void Init(JsonSocial jss)
	{
		if (this.dictPeople == null)
		{
			this.dictPeople = new Dictionary<string, Relationship>();
		}
		if (this.dictPSpecs == null)
		{
			this.dictPSpecs = new Dictionary<string, PersonSpec>();
		}
		if (jss == null || jss.aPSpecsValues == null)
		{
			return;
		}
		if (jss.aRelationships2 != null)
		{
			for (int i = 0; i < jss.aPSpecsValues.Length; i++)
			{
				PersonSpec personSpec = jss.aPSpecsValues[i];
				string fullName = personSpec.FullName;
				this.dictPSpecs[fullName] = personSpec;
			}
			foreach (JsonRelationship jsonRelationship in jss.aRelationships2)
			{
				PersonSpec pspec = this.GetPSpec(jsonRelationship.strPSpec);
				if (pspec != null)
				{
					Relationship rel = new Relationship(jsonRelationship, pspec);
					this.AddPerson(rel);
				}
			}
		}
		else
		{
			this.AddOldRelData(jss);
		}
	}

	private void AddOldRelData(JsonSocial jss)
	{
		if (jss == null)
		{
			return;
		}
		for (int i = 0; i < jss.aPSpecsValues.Length; i++)
		{
			PersonSpec personSpec = jss.aPSpecsValues[i];
			string fullName = personSpec.FullName;
			if (jss.aRelationships == null || jss.aRelationships.Length < i - 1)
			{
				Debug.Log("Warning: Invalid relationship data for " + fullName + ". Skipping.");
			}
			else
			{
				string text = jss.aRelationships[i];
				text = text.Replace("[", string.Empty);
				text = text.Replace("]", string.Empty);
				string[] collection = text.Split(new char[]
				{
					','
				});
				List<string> aRelationships = new List<string>(collection);
				if (jss.aEvents == null || jss.aEvents.Length < i - 1)
				{
					Debug.Log("Warning: Invalid relationship event data for " + fullName + ". Skipping.");
				}
				else
				{
					text = jss.aEvents[i];
					text = text.Replace("[", string.Empty);
					text = text.Replace("]", string.Empty);
					if (text != string.Empty)
					{
						collection = text.Split(new char[]
						{
							','
						});
					}
					else
					{
						collection = new string[0];
					}
					List<string> aEvents = new List<string>(collection);
					Relationship relationship = new Relationship();
					relationship.pspec = personSpec;
					relationship.aRelationships = aRelationships;
					relationship.aEvents = aEvents;
					if (jss.aConds == null || jss.aConds.Length < i - 1)
					{
						Debug.Log("Warning: Invalid relationship conds data for " + fullName + ". Skipping.");
					}
					else
					{
						relationship.Conds = DataHandler.ConvertStringArrayToDictDouble(jss.aConds[i], null);
						if (relationship.Conds.Count == 0)
						{
							Debug.Log("ERROR: dictConds has 0 entries. Should be non-zero.");
						}
						this.AddPerson(relationship);
					}
				}
			}
		}
	}

	public void AddPerson(Relationship rel)
	{
		if (rel.pspec == null || rel.aRelationships == null)
		{
			return;
		}
		string fullName = rel.pspec.FullName;
		if (fullName == this.CO.strName)
		{
			Debug.Log("ERROR: Adding relationship to self. Aborting.");
			return;
		}
		this.dictPSpecs[fullName] = rel.pspec;
		if (!this.dictPeople.ContainsKey(fullName))
		{
			this.dictPeople.Add(fullName, rel);
		}
		foreach (string strREL in rel.aRelationships)
		{
			this.dictPeople[fullName].AddRelationship(this.CO, strREL);
		}
		foreach (string item in rel.aEvents)
		{
			if (this.dictPeople[fullName].aEvents.IndexOf(item) < 0)
			{
				this.dictPeople[fullName].aEvents.Add(item);
			}
		}
	}

	public void RenamePerson(string strNameOld, string strNameNew)
	{
		if (strNameOld == null || strNameNew == null || strNameOld == strNameNew || !this.dictPeople.ContainsKey(strNameOld))
		{
			return;
		}
		this.dictPeople[strNameNew] = this.dictPeople[strNameOld];
		this.dictPeople.Remove(strNameOld);
		this.dictPSpecs[strNameNew] = this.dictPSpecs[strNameOld];
		this.dictPSpecs.Remove(strNameOld);
	}

	public Relationship AddStranger(PersonSpec pspec)
	{
		if (pspec == null)
		{
			return null;
		}
		Relationship relationship = new Relationship(pspec, new List<string>
		{
			"RELStranger"
		}, new List<string>());
		this.AddPerson(relationship);
		return relationship;
	}

	public void RemovePerson(PersonSpec pspec, List<string> aRelationships)
	{
		if (pspec == null || aRelationships == null || aRelationships.Count == 0)
		{
			return;
		}
		string fullName = pspec.FullName;
		bool flag = false;
		if (this.dictPeople.ContainsKey(fullName))
		{
			foreach (string strREL in aRelationships)
			{
				this.dictPeople[fullName].RemoveRelationship(this.CO, strREL, false);
			}
			if (this.dictPeople[fullName].aRelationships.Count == 0)
			{
				flag = true;
			}
		}
		else
		{
			flag = true;
		}
		if (flag)
		{
			this.dictPSpecs.Remove(fullName);
			this.dictPeople.Remove(fullName);
		}
	}

	public bool HasPerson(PersonSpec pspecIn)
	{
		return pspecIn != null && this.dictPSpecs.ContainsValue(pspecIn);
	}

	public Relationship GetRelationship(string strName)
	{
		if (this.dictPeople.ContainsKey(strName))
		{
			return this.dictPeople[strName];
		}
		return null;
	}

	public List<Relationship> GetAllPeople()
	{
		List<Relationship> list = new List<Relationship>();
		foreach (string key in this.dictPeople.Keys)
		{
			list.Add(this.dictPeople[key]);
		}
		return list;
	}

	public string GetMatchingRelation(JsonPersonSpec jpsFilter, List<string> aForbids = null, JsonShipSpec jss = null)
	{
		if (jpsFilter == null || jpsFilter.strCTRelFind == null || this.CO == null || this.CO.pspec == null)
		{
			return null;
		}
		List<string> list = new List<string>();
		foreach (string text in this.dictPSpecs.Keys)
		{
			PersonSpec personSpec = null;
			if (this.dictPSpecs.TryGetValue(text, out personSpec))
			{
				if (aForbids == null || !aForbids.Contains(personSpec.FullName))
				{
					CondOwner co = personSpec.GetCO();
					if (!(co == null) && this.CO.pspec.IsCOMyMother(jpsFilter, co))
					{
						if (jss == null || jss.Matches(personSpec.GetCO().ship, this.CO))
						{
							list.Add(text);
						}
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[MathUtils.Rand(0, list.Count - 1, MathUtils.RandType.Flat, null)];
	}

	public List<string> GetMatchingRelationsAll(JsonPersonSpec jpsFilter)
	{
		if (jpsFilter == null || jpsFilter.strCTRelFind == null)
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, PersonSpec> keyValuePair in this.dictPSpecs)
		{
			CondOwner co = keyValuePair.Value.GetCO();
			if (!(co == null))
			{
				if (this.CO.pspec.IsCOMyMother(jpsFilter, co))
				{
					list.Add(co.strName);
				}
			}
		}
		return list;
	}

	public PersonSpec GetPSpec(string strName)
	{
		PersonSpec result = null;
		if (strName != null)
		{
			this.dictPSpecs.TryGetValue(strName, out result);
		}
		return result;
	}

	public JsonSocial GetJSON()
	{
		List<JsonRelationship> list = new List<JsonRelationship>();
		List<PersonSpec> list2 = new List<PersonSpec>();
		foreach (KeyValuePair<string, PersonSpec> keyValuePair in this.dictPSpecs)
		{
			list2.Add(keyValuePair.Value);
		}
		foreach (KeyValuePair<string, Relationship> keyValuePair2 in this.dictPeople)
		{
			JsonRelationship json = keyValuePair2.Value.GetJson();
			list.Add(json);
		}
		return new JsonSocial
		{
			aPSpecsValues = list2.ToArray(),
			aRelationships2 = list.ToArray()
		};
	}

	public CondOwner CO
	{
		get
		{
			if (!this.bTriedGettingCO)
			{
				this._co = base.GetComponent<CondOwner>();
				this.bTriedGettingCO = true;
			}
			return this._co;
		}
	}

	private Dictionary<string, Relationship> dictPeople;

	private Dictionary<string, PersonSpec> dictPSpecs;

	private bool bTriedGettingCO;

	private CondOwner _co;
}
