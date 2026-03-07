using System;
using System.Collections.Generic;
using UnityEngine;

public class GUIChargenStack : MonoBehaviour
{
	private void Awake()
	{
		if (this.aCareers == null)
		{
			this.Init(null);
		}
	}

	public void Init(JsonChargenStack jcgs)
	{
		this.aCareers = new List<CareerChosen>();
		this.aHomeworldTraits = new List<string>();
		if (jcgs == null)
		{
			this.ChangeHomeworld(DataHandler.GetHomeworld("OKLG"), 0);
			return;
		}
		this.strFirstName = jcgs.strFirstName;
		this.strLastName = jcgs.strLastName;
		this.ChangeHomeworld(DataHandler.GetHomeworld(jcgs.strJSH), jcgs.nStrata);
		if (jcgs.aCareers != null)
		{
			foreach (JsonCareerChosen jsonCareerChosen in jcgs.aCareers)
			{
				CareerChosen careerChosen = new CareerChosen(jsonCareerChosen.strJC, jsonCareerChosen.bFirst);
				careerChosen.Init(jsonCareerChosen);
				this.aCareers.Add(careerChosen);
			}
		}
		this.bCareerEnded = jcgs.bCareerEnded;
		this.strRegIDChosen = jcgs.strRegIDChosen;
	}

	public void ChangeHomeworld(JsonHomeworld jshNew, int nStrataNew)
	{
		if (this.co == null)
		{
			this.co = base.GetComponent<CondOwner>();
		}
		int count = this.co.mapConds.Count;
		string[] array = null;
		if (this.jsh != null)
		{
			int num = this.nStrata;
			if (num != 2)
			{
				if (num != 1)
				{
					if (num == 0)
					{
						array = this.jsh.aCondsIllegal;
					}
				}
				else
				{
					array = this.jsh.aCondsResident;
				}
			}
			else
			{
				array = this.jsh.aCondsCitizen;
			}
			if (array == null)
			{
				array = new string[0];
			}
			foreach (string strName in array)
			{
				this.co.AddCondAmount(strName, -1.0, 0.0, 0f);
			}
			this.jsh = null;
		}
		count = this.co.mapConds.Count;
		if (jshNew != null)
		{
			if (nStrataNew != 2)
			{
				if (nStrataNew != 1)
				{
					if (nStrataNew == 0)
					{
						array = jshNew.aCondsIllegal;
					}
				}
				else
				{
					array = jshNew.aCondsResident;
				}
			}
			else
			{
				array = jshNew.aCondsCitizen;
			}
			if (array == null)
			{
				array = new string[0];
			}
			foreach (string strName2 in array)
			{
				this.co.AddCondAmount(strName2, 1.0, 0.0, 0f);
			}
			this.jsh = jshNew;
			if (this.co.pspec != null)
			{
				this.co.pspec.nStrata = nStrataNew;
				this.co.pspec.strHomeworldNow = jshNew.strATCCode;
			}
			count = this.co.mapConds.Count;
		}
	}

	public void AddCareer(JsonCareer jc)
	{
		if (this.aCareers == null)
		{
			this.Init(null);
		}
		bool bFirst = true;
		foreach (CareerChosen careerChosen in this.aCareers)
		{
			if (careerChosen.GetJC() == jc)
			{
				bFirst = false;
				break;
			}
		}
		CareerChosen item = new CareerChosen(jc.strName, bFirst);
		this.aCareers.Add(item);
	}

	public void RemoveCareer()
	{
		if (this.aCareers.Count > 0 && !this.aCareers[this.aCareers.Count - 1].bConfirmed)
		{
			this.aCareers.RemoveAt(this.aCareers.Count - 1);
		}
	}

	public int Strata
	{
		get
		{
			return this.nStrata;
		}
	}

	public JsonHomeworld GetHomeworld()
	{
		return this.jsh;
	}

	public CareerChosen GetLatestCareer()
	{
		if (this.aCareers.Count > 0)
		{
			return this.aCareers[this.aCareers.Count - 1];
		}
		return null;
	}

	public string GetLatestCareerName()
	{
		CareerChosen latestCareer = this.GetLatestCareer();
		if (latestCareer == null)
		{
			return DataHandler.GetString("NOT_APPLICABLE", false);
		}
		JsonCareer jc = latestCareer.GetJC();
		if (jc == null)
		{
			return DataHandler.GetString("NOT_APPLICABLE", false);
		}
		return jc.strNameFriendly;
	}

	public void ApplyCareer(CondOwner coUser, CareerChosen jcc, bool bEvents)
	{
		jcc.bConfirmed = true;
		foreach (string strName in jcc.aSkillsChosen)
		{
			coUser.AddCondAmount(strName, 1.0, 0.0, 0f);
		}
		foreach (string strName2 in jcc.aHobbiesChosen)
		{
			coUser.AddCondAmount(strName2, 1.0, 0.0, 0f);
		}
		string text = null;
		bool bLogConds = coUser.bLogConds;
		coUser.bLogConds = false;
		Loot loot = DataHandler.GetLoot("CGCareerFullList");
		foreach (string text2 in loot.GetAllLootNames())
		{
			if (coUser.HasCond(text2))
			{
				coUser.ZeroCondAmount(text2);
				coUser.AddCondAmount(text2 + "Past", 1.0, 0.0, 0f);
				text = text2;
			}
		}
		coUser.bLogConds = bLogConds;
		List<string> lootNames;
		if (jcc.bFirst)
		{
			lootNames = DataHandler.GetLoot(jcc.GetJC().strLootConds).GetLootNames(null, false, null);
		}
		else
		{
			lootNames = DataHandler.GetLoot(jcc.GetJC().strLootCondsNext).GetLootNames(null, false, null);
		}
		foreach (string text3 in lootNames)
		{
			if (text3 == text || text3 == "-" + text)
			{
				coUser.bLogConds = false;
			}
			if (text3[0] == '-')
			{
				coUser.AddCondAmount(text3.Substring(1), -1.0, 0.0, 0f);
			}
			else
			{
				coUser.AddCondAmount(text3, 1.0, 0.0, 0f);
			}
			coUser.bLogConds = bLogConds;
		}
		coUser.pspec.strCareerNow = jcc.GetJC().strName;
		jcc.nAge = (coUser.pspec.nAgeMax = (coUser.pspec.nAgeMin = Convert.ToInt32(coUser.GetCondAmount("StatAge"))));
	}

	public JsonChargenStack GetJSON()
	{
		JsonChargenStack jsonChargenStack = new JsonChargenStack();
		List<JsonCareerChosen> list = new List<JsonCareerChosen>();
		foreach (CareerChosen careerChosen in this.aCareers)
		{
			list.Add(careerChosen.GetJSON());
		}
		jsonChargenStack.aCareers = list.ToArray();
		jsonChargenStack.aHomeworldTraits = this.aHomeworldTraits.ToArray();
		jsonChargenStack.bCareerEnded = this.bCareerEnded;
		jsonChargenStack.strRegIDChosen = this.strRegIDChosen;
		jsonChargenStack.strJSH = this.jsh.strName;
		jsonChargenStack.nStrata = this.nStrata;
		jsonChargenStack.strFirstName = this.strFirstName;
		jsonChargenStack.strLastName = this.strLastName;
		return jsonChargenStack;
	}

	private CondOwner co;

	public string strFirstName;

	public string strLastName;

	private JsonHomeworld jsh;

	public List<string> aHomeworldTraits;

	private int nStrata;

	public List<CareerChosen> aCareers;

	public bool bCareerEnded;

	public float fShipChance;

	public string strRegIDChosen;
}
