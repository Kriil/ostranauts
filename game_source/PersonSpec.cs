using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Condowner;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

public class PersonSpec
{
	public PersonSpec()
	{
		this.strGender = this.GetRandomGender();
		DataHandler.GetFullName(this.strGender, out this.strFirstName, out this.strLastName);
		CondOwner condOwner = null;
		for (int i = 0; i < 5; i++)
		{
			if (!DataHandler.mapCOs.TryGetValue(this.FullName, out condOwner))
			{
				break;
			}
			float num = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
			if (num <= 0.5f)
			{
				this.strLastName = DataHandler.EmbellishName(this.strLastName, false, this.strGender);
			}
			else
			{
				this.strFirstName = DataHandler.EmbellishName(this.strFirstName, true, this.strGender);
			}
		}
	}

	public PersonSpec(JsonPersonSpec jps, bool bNew)
	{
		bool flag = CrewSim.coPlayer != null;
		if (jps.strGender == "player")
		{
			if (!flag)
			{
				this.strGender = this.GetRandomGender();
			}
			else if (CrewSim.coPlayer.HasCond("IsMale"))
			{
				this.strGender = "IsMale";
			}
			else if (CrewSim.coPlayer.HasCond("IsFemale"))
			{
				this.strGender = "IsFemale";
			}
			else
			{
				this.strGender = "IsNB";
			}
		}
		else if (jps.strGender == "notplayer")
		{
			if (!flag)
			{
				this.strGender = this.GetRandomGender();
			}
			else if (CrewSim.coPlayer.HasCond("IsMale"))
			{
				this.strGender = "IsFemale";
			}
			else if (CrewSim.coPlayer.HasCond("IsFemale"))
			{
				this.strGender = "IsMale";
			}
			else
			{
				this.strGender = "IsNB";
			}
		}
		else if (jps.strGender != string.Empty)
		{
			this.strGender = jps.strGender;
		}
		else
		{
			this.strGender = this.GetRandomGender();
		}
		if (jps.strSkin == "player")
		{
			if (!flag || CrewSim.coPlayer.pspec == null)
			{
				this.strSkin = string.Empty;
			}
			else
			{
				this.strSkin = CrewSim.coPlayer.pspec.strSkin;
			}
		}
		else if (jps.strSkin == "notplayer")
		{
			this.strSkin = string.Empty;
		}
		else if (jps.strSkin != string.Empty)
		{
			this.strSkin = jps.strSkin;
		}
		if (jps.strBodyType == "player")
		{
			if (!flag || CrewSim.coPlayer.pspec == null || string.IsNullOrEmpty(CrewSim.coPlayer.pspec.strBodyType))
			{
				this.strBodyType = string.Empty;
			}
			else
			{
				this.strBodyType = CrewSim.coPlayer.pspec.strBodyType;
			}
		}
		else if (jps.strBodyType == "notplayer")
		{
			this.strBodyType = string.Empty;
		}
		else if (jps.strBodyType != string.Empty)
		{
			this.strBodyType = jps.strBodyType;
		}
		if (jps.strFirstName == "player")
		{
			if (!flag)
			{
				this.strFirstName = null;
			}
			else
			{
				this.strFirstName = CrewSim.coPlayer.strName.Remove(CrewSim.coPlayer.strName.LastIndexOf(" "));
			}
		}
		else if (jps.strFirstName != string.Empty && jps.strFirstName != "notplayer")
		{
			this.strFirstName = jps.strFirstName;
		}
		if (jps.strLastName == "player")
		{
			if (!flag)
			{
				this.strLastName = null;
			}
			else
			{
				this.strLastName = CrewSim.coPlayer.strName.Substring(CrewSim.coPlayer.strName.LastIndexOf(" ") + 1);
			}
		}
		else if (jps.strLastName == "playerRand")
		{
			if (!flag || UnityEngine.Random.Range(0f, 1f) > 0.71f)
			{
				this.strLastName = null;
			}
			else
			{
				this.strLastName = CrewSim.coPlayer.strName.Substring(CrewSim.coPlayer.strName.LastIndexOf(" ") + 1);
			}
		}
		else if (jps.strLastName != string.Empty && jps.strLastName != "notplayer")
		{
			this.strLastName = jps.strLastName;
		}
		if (this.strFirstName == null && this.strLastName == null)
		{
			DataHandler.GetFullName(this.strGender, out this.strFirstName, out this.strLastName);
		}
		if (this.strFirstName == null)
		{
			this.strFirstName = DataHandler.GetName(true, this.strGender);
		}
		if (this.strLastName == null)
		{
			this.strLastName = DataHandler.GetName(false, this.strGender);
		}
		if (bNew)
		{
			for (int i = 0; i < 5; i++)
			{
				if (!DataHandler.mapCOs.TryGetValue(this.FullName, out this.co))
				{
					break;
				}
				float num = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
				if (num <= 0.5f)
				{
					this.strLastName = DataHandler.EmbellishName(this.strLastName, false, this.strGender);
				}
				else
				{
					this.strFirstName = DataHandler.EmbellishName(this.strFirstName, true, this.strGender);
				}
			}
		}
		if (flag)
		{
			if (jps.nAgeRangeBelow >= 0)
			{
				this.nAgeMin = Convert.ToInt32(CrewSim.coPlayer.GetCondAmount("StatAge")) - jps.nAgeRangeBelow;
			}
			if (jps.nAgeRangeAbove >= 0)
			{
				this.nAgeMax = Convert.ToInt32(CrewSim.coPlayer.GetCondAmount("StatAge")) + jps.nAgeRangeAbove;
			}
		}
		if (jps.nAgeMin >= 0 && (this.nAgeMin < 0 || jps.nAgeMin > this.nAgeMin))
		{
			this.nAgeMin = jps.nAgeMin;
		}
		if (jps.nAgeMax >= 0 && (this.nAgeMax < 0 || jps.nAgeMax < this.nAgeMax))
		{
			this.nAgeMax = jps.nAgeMax;
		}
		if (jps.strCareerNow != string.Empty)
		{
			this.nAgeMin = 18 + GUIChargenCareer.nTermYears;
		}
		this.strHomeworldNow = this.GetHomeWorld(jps.strHomeworldSet, flag);
		if (jps.strStrata == "player")
		{
			if (!flag)
			{
				this.nStrata = this.GetRandomStrata(DataHandler.GetHomeworld(this.strHomeworldNow));
			}
			else
			{
				this.nStrata = CrewSim.coPlayer.GetComponent<GUIChargenStack>().Strata;
			}
		}
		else if (jps.strStrata == "illegal")
		{
			this.nStrata = 0;
		}
		else if (jps.strStrata == "resident")
		{
			this.nStrata = 1;
		}
		else if (jps.strStrata == "citizen")
		{
			this.nStrata = 2;
		}
		this.strCareerNow = this.GetCareer(jps.strCareerNow, flag);
		this.strLootConds = jps.strLootConds;
		this.strLootCondsPreCareer = jps.strLootCondsPreCareer;
		this.strLoot = jps.strLoot;
		this.aGPMSets = jps.aGPMSets;
		this.strLootIAAdds = jps.strLootIAAdds;
		this.aFactionAdds = jps.aFactionAdds;
		this.strType = jps.strType;
		this.bAlive = jps.bAlive;
	}

	private string GetCareer(string careerNow, bool bPlayerExists)
	{
		string result = string.Empty;
		if (careerNow == "player")
		{
			if (bPlayerExists)
			{
				result = CrewSim.coPlayer.GetComponent<GUIChargenStack>().GetLatestCareer().GetJC().strName;
			}
			else
			{
				result = string.Empty;
			}
		}
		else if (!string.IsNullOrEmpty(careerNow))
		{
			result = careerNow;
		}
		return result;
	}

	private string GetHomeWorld(string jHomeWorld, bool bPlayerExists)
	{
		if (string.IsNullOrEmpty(jHomeWorld))
		{
			return this.GetRandomHomeworld().strATCCode;
		}
		string text = string.Empty;
		if (jHomeWorld == "player")
		{
			if (bPlayerExists)
			{
				text = CrewSim.coPlayer.GetComponent<GUIChargenStack>().GetHomeworld().strATCCode;
			}
			else
			{
				text = this.GetRandomHomeworld().strATCCode;
			}
		}
		else if (jHomeWorld == "notplayer")
		{
			if (!bPlayerExists)
			{
				text = this.GetRandomHomeworld().strATCCode;
			}
			else
			{
				string strATCCode = CrewSim.coPlayer.GetComponent<GUIChargenStack>().GetHomeworld().strATCCode;
				text = CrewSim.coPlayer.GetComponent<GUIChargenStack>().GetHomeworld().strATCCode;
				int num = 20;
				while (text == strATCCode && num > 0)
				{
					text = this.GetRandomHomeworld().strATCCode;
					num--;
				}
			}
		}
		else if (jHomeWorld != string.Empty)
		{
			Loot loot = DataHandler.GetLoot(jHomeWorld);
			if (loot.strName != "Blank")
			{
				List<string> lootNames = loot.GetLootNames(null, false, null);
				if (lootNames.Count > 0)
				{
					text = lootNames.First<string>();
				}
			}
			else
			{
				text = jHomeWorld;
			}
		}
		JsonHomeworld homeworld = DataHandler.GetHomeworld(text);
		if (homeworld != null)
		{
			return text;
		}
		return this.GetRandomHomeworld().strATCCode;
	}

	public bool IsCOMyMother(JsonPersonSpec jpsMother, CondOwner coMother)
	{
		if (coMother == null)
		{
			Debug.Log("Tried to match null condowner!");
			return false;
		}
		if (coMother.pspec == null)
		{
			Debug.Log("Tried to match condowner w/null pspec! CoMother: " + coMother.strName);
			return false;
		}
		if (jpsMother == null)
		{
			Debug.Log("Tried to match null personspec!");
			return false;
		}
		if (!jpsMother.bAliveIgnore && coMother.bAlive != jpsMother.bAlive)
		{
			return false;
		}
		if (jpsMother.strGender == "player")
		{
			if (!coMother.HasCond(this.strGender))
			{
				return false;
			}
		}
		else if (jpsMother.strGender != string.Empty && coMother.pspec.strGender != jpsMother.strGender)
		{
			return false;
		}
		if (jpsMother.strSkin == "player")
		{
			if (coMother.pspec.strSkin != this.strSkin)
			{
				return false;
			}
		}
		else if (jpsMother.strSkin != string.Empty && coMother.pspec.strSkin != jpsMother.strSkin)
		{
			return false;
		}
		if (jpsMother.strBodyType == "player")
		{
			if (coMother.pspec.strBodyType != this.strBodyType)
			{
				return false;
			}
		}
		else if (jpsMother.strBodyType != string.Empty && coMother.pspec.strBodyType != jpsMother.strBodyType)
		{
			return false;
		}
		if (jpsMother.strFirstName == "player")
		{
			if (coMother.strName.IndexOf(this.strFirstName) != 0)
			{
				return false;
			}
		}
		else if (jpsMother.strFirstName != string.Empty && coMother.pspec.strFirstName != jpsMother.strFirstName)
		{
			return false;
		}
		if (jpsMother.strLastName == "player")
		{
			if (coMother.strName.IndexOf(this.strLastName) != coMother.strName.Length - this.strLastName.Length)
			{
				return false;
			}
		}
		else if (jpsMother.strLastName != string.Empty && jpsMother.strLastName != coMother.pspec.strLastName)
		{
			return false;
		}
		if (jpsMother.nAgeRangeBelow >= 0 && Convert.ToInt32(coMother.GetCondAmount("StatAge")) - this.nAgeMin > jpsMother.nAgeRangeBelow)
		{
			return false;
		}
		if (jpsMother.nAgeRangeAbove >= 0 && this.nAgeMax - Convert.ToInt32(coMother.GetCondAmount("StatAge")) > jpsMother.nAgeRangeAbove)
		{
			return false;
		}
		if (jpsMother.strHomeworldFind == "player")
		{
			if (this.strHomeworldNow != coMother.GetComponent<GUIChargenStack>().GetHomeworld().strATCCode)
			{
				return false;
			}
		}
		else if (jpsMother.strHomeworldFind == "notplayer")
		{
			if (this.strHomeworldNow == coMother.GetComponent<GUIChargenStack>().GetHomeworld().strATCCode)
			{
				return false;
			}
		}
		else if (jpsMother.strHomeworldFind != string.Empty && coMother.pspec.strHomeworldNow != jpsMother.strHomeworldFind)
		{
			return false;
		}
		if (jpsMother.strRegID != null && jpsMother.strRegID != string.Empty)
		{
			if (this.co == null)
			{
				return false;
			}
			if (jpsMother.strRegID == "player")
			{
				if (this.co.ship == null || coMother.ship == null || this.co.ship.strRegID != coMother.ship.strRegID)
				{
					return false;
				}
			}
			else if (jpsMother.strRegID == "notplayer")
			{
				if (this.co.ship != null && coMother.ship != null && this.co.ship.strRegID == coMother.ship.strRegID)
				{
					return false;
				}
			}
			else if (this.co.ship.strRegID != jpsMother.strRegID)
			{
				return false;
			}
		}
		if (jpsMother.strStrata == "player")
		{
			if (this.nStrata != coMother.GetComponent<GUIChargenStack>().Strata)
			{
				return false;
			}
		}
		else if (jpsMother.strStrata == "illegal")
		{
			if (coMother.pspec.nStrata != 0)
			{
				return false;
			}
		}
		else if (jpsMother.strStrata == "resident")
		{
			if (coMother.pspec.nStrata != 1)
			{
				return false;
			}
		}
		else if (jpsMother.strStrata == "citizen" && coMother.pspec.nStrata != 2)
		{
			return false;
		}
		if (jpsMother.strCareerNow == "player")
		{
			if (this.strCareerNow != coMother.GetComponent<GUIChargenStack>().GetLatestCareer().GetJC().strName)
			{
				return false;
			}
		}
		else if (jpsMother.strCareerNow != string.Empty && coMother.pspec.strCareerNow != jpsMother.strCareerNow)
		{
			return false;
		}
		if (jpsMother.strCTRelFind != null && jpsMother.strCTRelFind != string.Empty)
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger(jpsMother.strCTRelFind);
			if (!condTrigger.TriggeredREL(this.GetCO(), coMother))
			{
				return false;
			}
		}
		if (jpsMother.strCT != null)
		{
			CondTrigger condTrigger2 = DataHandler.GetCondTrigger(jpsMother.strCT);
			if (!condTrigger2.Triggered(coMother, null, true))
			{
				return false;
			}
		}
		return true;
	}

	private string GetRandomGender()
	{
		float num = UnityEngine.Random.Range(0f, 100f);
		if (num <= 10f)
		{
			return "IsNB";
		}
		if (num < 54f)
		{
			return "IsMale";
		}
		return "IsFemale";
	}

	private JsonHomeworld GetRandomHomeworld()
	{
		Loot loot = DataHandler.GetLoot("HW_ANY");
		string strName = string.Empty;
		if (loot != null)
		{
			List<string> lootNames = loot.GetLootNames(null, false, null);
			if (lootNames.Count > 0)
			{
				strName = lootNames.First<string>();
			}
		}
		JsonHomeworld homeworld = DataHandler.GetHomeworld(strName);
		if (homeworld != null)
		{
			return homeworld;
		}
		List<JsonHomeworld> list = new List<JsonHomeworld>(DataHandler.dictHomeworlds.Values);
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (list[i].bPCOnly)
			{
				list.RemoveAt(i);
			}
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private int GetRandomStrata(JsonHomeworld jhw)
	{
		List<int> list = new List<int>();
		if (jhw.aCondsCitizen != null && jhw.aCondsCitizen.Length > 0)
		{
			list.Add(2);
		}
		if (jhw.aCondsIllegal != null && jhw.aCondsIllegal.Length > 0)
		{
			list.Add(0);
		}
		if (jhw.aCondsResident != null && jhw.aCondsResident.Length > 0)
		{
			list.Add(1);
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	public CondOwner GetCO()
	{
		if (this.co == null && this.strCO != null)
		{
			DataHandler.mapCOs.TryGetValue(this.strCO, out this.co);
		}
		return this.co;
	}

	public string FullName
	{
		get
		{
			if (!string.IsNullOrEmpty(this.strCO))
			{
				return this.strCO;
			}
			if (string.IsNullOrEmpty(this.strFirstName))
			{
				return this.strLastName;
			}
			if (string.IsNullOrEmpty(this.strLastName))
			{
				return this.strFirstName;
			}
			return this.strFirstName + " " + this.strLastName;
		}
	}

	public CondOwner MakeCondOwner(PersonSpec.StartShip nStart, Ship targetShip = null)
	{
		if (this.GetCO() != null)
		{
			if (targetShip != null && this.co.ship != targetShip)
			{
				CrewSim.MoveCO(this.co, targetShip, false);
			}
			return this.co;
		}
		if (this.strCareerNow == "ORG")
		{
			this.co = DataHandler.GetCondOwner("SysORG", this.FullName, null, true, null, null, null, null);
			this.co.strID = (this.co.strName = this.FullName);
		}
		else if (!string.IsNullOrEmpty(this.strType))
		{
			this.strFirstName = DataHandler.GetName(false, "Robot");
			this.strLastName = UnityEngine.Random.Range(1, 10000).ToString();
			this.co = DataHandler.GetCondOwner(this.strType, this.FullName, null, true, null, null, null, null);
		}
		else
		{
			this.co = DataHandler.GetCondOwner("Crew01", this.FullName, null, true, null, null, null, null);
		}
		this.co.pspec = this;
		this.co.socUs = this.co.gameObject.AddComponent<global::Social>();
		JsonFaction jsonFaction = new JsonFaction();
		jsonFaction.Init();
		JsonFaction jsonFaction2 = jsonFaction;
		string strID = this.co.strID;
		jsonFaction.strNameFriendly = strID;
		jsonFaction2.strName = strID;
		CrewSim.system.AddFaction(jsonFaction);
		this.co.AddFaction(jsonFaction);
		this.co.bLogConds = false;
		this.co.AddCondAmount("IsMale", -1.0, 0.0, 0f);
		this.co.AddCondAmount("IsFemale", -1.0, 0.0, 0f);
		this.co.AddCondAmount("IsNB", -1.0, 0.0, 0f);
		this.co.AddCondAmount(this.strGender, 1.0, 0.0, 0f);
		if (this.nAgeMin < 0)
		{
			this.nAgeMin = 1;
		}
		if (this.nAgeMin < 18)
		{
			this.nAgeMin = 18;
		}
		JsonHomeworld homeworld = DataHandler.GetHomeworld(this.strHomeworldNow);
		if (this.nStrata >= 0)
		{
			while (this.nStrata >= 0 && !GUIChargenHomeworld.IsValidStrata(this.nStrata, homeworld))
			{
				this.nStrata--;
			}
		}
		if (this.nStrata < 0)
		{
			this.nStrata = this.GetRandomStrata(homeworld);
		}
		int num = this.nStrata;
		GUIChargenStack guichargenStack = this.co.gameObject.AddComponent<GUIChargenStack>();
		this.nStrata = num;
		guichargenStack.strFirstName = this.strFirstName;
		guichargenStack.strLastName = this.strLastName;
		guichargenStack.ChangeHomeworld(homeworld, this.nStrata);
		if (this.nAgeMax < 0 && this.nAgeMax < CrewSim.system.GetYear() - homeworld.nFoundingYear)
		{
			this.nAgeMax = CrewSim.system.GetYear() - homeworld.nFoundingYear;
		}
		if (this.nAgeMin > this.nAgeMax)
		{
			this.nAgeMin = this.nAgeMax;
		}
		int num2 = UnityEngine.Random.Range(this.nAgeMin, this.nAgeMax + 1);
		if (this.strLootCondsPreCareer != null)
		{
			Loot loot = DataHandler.GetLoot(this.strLootCondsPreCareer);
			loot.ApplyCondLoot(this.co, 1f, null, 0f);
		}
		int num3 = 200;
		Loot loot2 = DataHandler.GetLoot("TXTRandomCareer");
		List<string> list = new List<string>();
		double num4 = this.co.GetCondAmount("StatAge");
		string text = this.strCareerNow;
		for (int i = 0; i < num3; i++)
		{
			if (num4 >= (double)num2)
			{
				break;
			}
			JsonCareer career;
			if (num4 >= (double)(num2 - GUIChargenCareer.nTermYears) && text != string.Empty && text != null)
			{
				career = DataHandler.GetCareer(text);
			}
			else
			{
				career = DataHandler.GetCareer(loot2.GetLootNameSingle(null));
			}
			if (career != null)
			{
				CondTrigger condTrigger = DataHandler.GetCondTrigger(career.strCTPrereqs);
				if (condTrigger.Triggered(this.co, null, true))
				{
					guichargenStack.AddCareer(career);
					CareerChosen latestCareer = guichargenStack.GetLatestCareer();
					list.Clear();
					foreach (string text2 in latestCareer.GetJC().aSkillsFirst)
					{
						if (!this.co.HasCond(text2))
						{
							list.Add(text2);
						}
					}
					int num5 = Mathf.Min(latestCareer.nSkillsLeftFirst, list.Count);
					for (int k = 0; k < num5; k++)
					{
						string text3 = list[UnityEngine.Random.Range(0, list.Count)];
						latestCareer.Choose(text3, latestCareer.GetJC().aSkillsFirst, false);
						list.Remove(text3);
					}
					list.Clear();
					foreach (string text4 in latestCareer.GetJC().aSkillsNext)
					{
						if (!this.co.HasCond(text4))
						{
							list.Add(text4);
						}
					}
					num5 = Mathf.Min(latestCareer.nSkillsLeftNext, list.Count);
					for (int m = 0; m < num5; m++)
					{
						string text5 = list[UnityEngine.Random.Range(0, list.Count)];
						latestCareer.Choose(text5, latestCareer.GetJC().aSkillsNext, false);
						list.Remove(text5);
					}
					list.Clear();
					foreach (string text6 in latestCareer.GetJC().aSkillsHobby)
					{
						if (!this.co.HasCond(text6))
						{
							list.Add(text6);
						}
					}
					num5 = Mathf.Min(latestCareer.nSkillsLeftHobby, list.Count);
					for (int num6 = 0; num6 < num5; num6++)
					{
						string text7 = list[UnityEngine.Random.Range(0, list.Count)];
						latestCareer.Choose(text7, latestCareer.GetJC().aSkillsHobby, false);
						list.Remove(text7);
					}
					guichargenStack.ApplyCareer(this.co, latestCareer, false);
					num4 += (double)GUIChargenCareer.nTermYears;
				}
			}
		}
		this.co.SetCondAmount("StatAge", (double)num2, 0.0);
		this.co.bAlive = this.bAlive;
		if (this.strLootConds != null)
		{
			Loot loot3 = DataHandler.GetLoot(this.strLootConds);
			loot3.ApplyCondLoot(this.co, 1f, null, 0f);
		}
		Dictionary<string, string> dictionary = DataHandler.ConvertStringArrayToDict(this.aGPMSets, null);
		foreach (KeyValuePair<string, string> keyValuePair in dictionary)
		{
			Dictionary<string, string> guipropMap = DataHandler.GetGUIPropMap(keyValuePair.Value);
			if (guipropMap != null)
			{
				this.co.mapGUIPropMaps[keyValuePair.Key] = guipropMap;
			}
		}
		this.strCO = this.co.strName;
		Ship ship = null;
		if (nStart != PersonSpec.StartShip.HOMEWORLD)
		{
			if (nStart != PersonSpec.StartShip.OLD)
			{
				if (nStart != PersonSpec.StartShip.NEW)
				{
				}
			}
			else if (targetShip != null)
			{
				ship = targetShip;
			}
			else
			{
				CrewSim.system.dictShips.TryGetValue(homeworld.strATCCode, out ship);
				if (ship == null)
				{
					List<Ship> list2 = new List<Ship>();
					foreach (Ship ship2 in CrewSim.system.dictShips.Values)
					{
						if (ship2.json.strName != "StationChargen" && ship2.json.strName != "PAX2020Start" && ship2.DMGStatus != Ship.Damage.Derelict)
						{
							list2.Add(ship2);
						}
					}
					int num7 = MathUtils.Rand(0, list2.Count, MathUtils.RandType.Flat, null);
					if (num7 < list2.Count)
					{
						ship = list2[num7];
					}
					list2 = null;
				}
			}
		}
		else
		{
			CrewSim.system.dictShips.TryGetValue(homeworld.strATCCode, out ship);
		}
		if (ship == null)
		{
			List<Ship> stations = CrewSim.system.GetStations(false);
			if (stations.Count == 0)
			{
				Debug.LogWarning("No stations found for new CO");
			}
			else
			{
				ship = stations.Randomize<Ship>().FirstOrDefault<Ship>();
				string str = (nStart != PersonSpec.StartShip.HOMEWORLD) ? string.Empty : ("jhw: " + homeworld.strATCCode);
				if (targetShip != null)
				{
					str = targetShip.strRegID;
				}
				Debug.LogWarning("New MakeCondOwner requested ship " + str + " but was moved to " + ship.strRegID);
			}
		}
		if (ship != null)
		{
			CrewSim.MoveCO(this.co, ship, false);
			this.co.ClaimShip(ship.strRegID);
		}
		List<CondOwner> list3 = new List<CondOwner>();
		if (this.strLoot != null)
		{
			List<CondOwner> coloot = DataHandler.GetLoot(this.strLoot).GetCOLoot(this.co, false, null);
			CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsEquippable");
			foreach (CondOwner condOwner in coloot)
			{
				if (condOwner == null)
				{
					Debug.LogError("ERROR: Null CO found in loot " + this.strLoot + ". Skipping.");
				}
				else if (condTrigger2 != null && condTrigger2.Triggered(condOwner, null, true))
				{
					list3.Add(condOwner);
				}
				else
				{
					this.EquipLoot(condOwner);
				}
			}
		}
		if (this.co.Crew != null)
		{
			bool bMale = this.co.HasCond("IsMale") || this.co.HasCond("IsNB");
			bool bFemale = this.co.HasCond("IsFemale") || this.co.HasCond("IsNB");
			this.co.Crew.SetBodyFaceSkin(Crew.GetBodyType(this.co), FaceAnim2.GetRandomFace(bMale, bFemale, this.strSkin));
		}
		foreach (CondOwner coLoot in list3)
		{
			this.EquipLoot(coLoot);
		}
		if (this.strLootIAAdds != null)
		{
			foreach (string strName in DataHandler.GetLoot(this.strLootIAAdds).GetLootNames(null, false, null))
			{
				Interaction interaction = DataHandler.GetInteraction(strName, null, false);
				if (interaction != null)
				{
					interaction.objUs = this.co;
					interaction.objThem = this.co;
					if (interaction.Triggered(false, true, false))
					{
						interaction.ApplyChain(null);
					}
				}
			}
		}
		if (this.aFactionAdds != null)
		{
			foreach (string strName2 in this.aFactionAdds)
			{
				this.co.AddFaction(CrewSim.system.GetFaction(strName2));
			}
		}
		CrewSim.system.AddAutoFactions(this.co);
		if (!ship.gameObject.activeInHierarchy)
		{
			CrewSim.RemoveTicker(this.co);
		}
		this.co.bLogConds = true;
		JsonAIPersonality jsonAIPersonality = (!this.co.IsRobot) ? DataHandler.dictAIPersonalities["Darlene"] : DataHandler.dictAIPersonalities["Robot"];
		foreach (KeyValuePair<string, CondHistory> keyValuePair2 in jsonAIPersonality.mapIAHist2)
		{
			this.co.mapIAHist[keyValuePair2.Key] = keyValuePair2.Value.Clone();
		}
		return this.co;
	}

	private void ProceduralTraining()
	{
		List<string> list = new List<string>();
		list.Add("IsHuman");
		List<JsonInteraction> list2 = new List<JsonInteraction>();
		foreach (JsonInteraction jsonInteraction in DataHandler.dictInteractions.Values)
		{
			if (jsonInteraction.bSocial || (jsonInteraction.bOpener && !jsonInteraction.bHumanOnly))
			{
				list2.Add(jsonInteraction);
			}
		}
		foreach (JsonInteraction jsonInteraction2 in list2)
		{
			if (!this.co.RemembersInteract(jsonInteraction2.strName))
			{
				Loot loot = DataHandler.GetLoot(jsonInteraction2.LootCTsUs);
				List<string> lootNames = loot.GetLootNames(null, false, null);
				CondTrigger condTrigger = null;
				float num = 0f;
				foreach (string text in lootNames)
				{
					if (condTrigger == null || condTrigger.strName != text)
					{
						if (condTrigger != null)
						{
							this.BasicTraining(this.co, condTrigger.strCondName, jsonInteraction2.strName, num);
						}
						condTrigger = DataHandler.GetCondTrigger(text);
						num = condTrigger.fCount;
					}
					else
					{
						num += condTrigger.fCount;
					}
				}
				if (condTrigger != null)
				{
					this.BasicTraining(this.co, condTrigger.strCondName, jsonInteraction2.strName, num);
				}
				loot = DataHandler.GetLoot(jsonInteraction2.LootCondsUs);
				Dictionary<string, double> condLoot = loot.GetCondLoot(1f, null, null);
				foreach (KeyValuePair<string, double> keyValuePair in condLoot)
				{
					this.BasicTraining(this.co, keyValuePair.Key, jsonInteraction2.strName, (float)keyValuePair.Value);
				}
			}
		}
		this.BasicTraining(this.co, "StatSleep", "SeekSleepSimpleAllow", -4f);
	}

	private void EquipLoot(CondOwner coLoot)
	{
		bool flag = true;
		if (this.co.compSlots != null && coLoot.mapSlotEffects.Keys.Count > 0)
		{
			foreach (string strSlot in coLoot.mapSlotEffects.Keys)
			{
				if (this.co.compSlots.SlotItem(strSlot, coLoot, true))
				{
					flag = false;
					break;
				}
			}
		}
		if (flag)
		{
			CondOwner condOwner = this.co.AddCO(coLoot, true, true, true);
			if (condOwner != null)
			{
				condOwner.Destroy();
			}
		}
	}

	private void BasicTraining(CondOwner co, string strCond, string strInteraction, float fCount)
	{
		co.aRememberIAs.Add(strInteraction);
		co.dictRememberScores[strCond] = (double)fCount;
		co.RememberInteractionEffectTraining(strInteraction);
		co.dictRememberScores.Remove(strCond);
		co.aRememberIAs.Clear();
	}

	public override string ToString()
	{
		return this.FullName;
	}

	public string strFirstName;

	public string strLastName;

	public string strGender;

	public string strSkin;

	public string strBodyType;

	public string strCareerNow;

	public string strHomeworldNow;

	public int nStrata = -1;

	public int nAgeMin = -1;

	public int nAgeMax = -1;

	public bool bAlive = true;

	public string strType;

	public string strLootCondsPreCareer;

	public string strLootConds;

	public string strLoot;

	public string[] aGPMSets;

	public string strLootIAAdds;

	public string[] aFactionAdds;

	public string strCO;

	private CondOwner co;

	public enum StartShip
	{
		NEW,
		OLD,
		HOMEWORLD
	}
}
