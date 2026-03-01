using System;
using System.Collections.Generic;

public class CareerChosen
{
	public CareerChosen(string strJC, bool bFirst)
	{
		this.bFirst = bFirst;
		this.strJC = strJC;
		this.bConfirmed = false;
		this.bTermEnded = false;
		this.strStartATC = null;
		this.aSkillsChosen = new List<string>();
		this.aHobbiesChosen = new List<string>();
		this.aEvents = new List<string>();
		this.aSocials = new List<string>();
		this.aShips = new List<string>();
		if (bFirst)
		{
			this.nSkillsLeftFirst = this.GetJC().nFirst;
		}
		else
		{
			this.nSkillsLeftNext = this.GetJC().nNext;
		}
		this.nSkillsLeftHobby = this.GetJC().nHobby;
	}

	public void Init(JsonCareerChosen jcc)
	{
		this.strJC = jcc.strJC;
		this.bFirst = jcc.bFirst;
		if (jcc.aSkillsChosen != null)
		{
			this.aSkillsChosen = new List<string>(jcc.aSkillsChosen);
		}
		if (jcc.aHobbiesChosen != null)
		{
			this.aHobbiesChosen = new List<string>(jcc.aHobbiesChosen);
		}
		if (jcc.aEvents != null)
		{
			this.aEvents = new List<string>(jcc.aEvents);
		}
		if (jcc.aSocials != null)
		{
			this.aSocials = new List<string>(jcc.aSocials);
		}
		if (jcc.aShips != null)
		{
			this.aShips = new List<string>(jcc.aShips);
		}
		this.strStartATC = jcc.strStartATC;
		this.fStartATCRange = jcc.fStartATCRange;
		this.fCashReward = jcc.fShipCashReward;
		this.fShipMortgage = jcc.fShipMortgage;
		this.fShipDmgMax = jcc.fShipDmgMax;
		this.nSkillsLeftFirst = 0;
		this.nSkillsLeftNext = 0;
		this.nSkillsLeftHobby = 0;
		this.nAge = jcc.nAge;
		this.bShipOwned = jcc.bShipOwned;
		this.bConfirmed = true;
		this.bTermEnded = false;
	}

	public void Choose(string strName, string[] aChooseFrom, bool bRemove)
	{
		if (aChooseFrom == this.GetJC().aSkillsFirst)
		{
			this.ChooseSkill(strName, true, bRemove);
		}
		else if (aChooseFrom == this.GetJC().aSkillsNext)
		{
			this.ChooseSkill(strName, false, bRemove);
		}
		else if (aChooseFrom == this.GetJC().aSkillsHobby)
		{
			this.ChooseHobby(strName, bRemove);
		}
	}

	private void ChooseSkill(string strName, bool bFirst, bool bRemove)
	{
		if (bRemove)
		{
			if (this.aSkillsChosen.Remove(strName))
			{
				if (bFirst)
				{
					this.nSkillsLeftFirst++;
				}
				else
				{
					this.nSkillsLeftNext++;
				}
			}
		}
		else if (this.aSkillsChosen.IndexOf(strName) < 0)
		{
			this.aSkillsChosen.Add(strName);
			if (bFirst)
			{
				this.nSkillsLeftFirst--;
			}
			else
			{
				this.nSkillsLeftNext--;
			}
		}
	}

	private void ChooseHobby(string strName, bool bRemove)
	{
		if (bRemove)
		{
			if (this.aHobbiesChosen.Remove(strName))
			{
				this.nSkillsLeftHobby++;
			}
		}
		else if (this.aHobbiesChosen.IndexOf(strName) < 0)
		{
			this.aHobbiesChosen.Add(strName);
			this.nSkillsLeftHobby--;
		}
	}

	public JsonCareer GetJC()
	{
		if (this.objJC != null)
		{
			return this.objJC;
		}
		this.objJC = DataHandler.GetCareer(this.strJC);
		return this.objJC;
	}

	public JsonCareerChosen GetJSON()
	{
		return new JsonCareerChosen
		{
			strJC = this.strJC,
			aSkillsChosen = this.aSkillsChosen.ToArray(),
			aHobbiesChosen = this.aHobbiesChosen.ToArray(),
			aEvents = this.aEvents.ToArray(),
			aSocials = this.aSocials.ToArray(),
			strStartATC = this.strStartATC,
			fStartATCRange = this.fStartATCRange,
			aShips = this.aShips.ToArray(),
			fShipCashReward = this.fCashReward,
			fShipDmgMax = this.fShipDmgMax,
			fShipMortgage = this.fShipMortgage,
			bShipOwned = this.bShipOwned,
			bFirst = this.bFirst,
			nAge = this.nAge
		};
	}

	public string strJC;

	public List<string> aSkillsChosen;

	public List<string> aHobbiesChosen;

	public List<string> aEvents;

	public List<string> aSocials;

	public List<string> aShips;

	public string strStartATC;

	public float fStartATCRange;

	public float fCashReward;

	public float fShipMortgage;

	public float fShipDmgMax;

	public int nSkillsLeftFirst;

	public int nSkillsLeftNext;

	public int nSkillsLeftHobby;

	public bool bFirst;

	public bool bConfirmed;

	public bool bTermEnded;

	public bool bShipOwned;

	public int nAge;

	private JsonCareer objJC;
}
