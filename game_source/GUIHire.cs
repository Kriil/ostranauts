using System;
using System.Collections.Generic;
using Ostranauts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIHire : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.tfMain = base.transform.Find("pnlList/scrollMask/pnlContent");
		this.bmpPortrait = base.transform.Find("bmpPortrait");
		this.btnHire = base.transform.Find("btnHire").GetComponent<Button>();
		this.btnHire.onClick.AddListener(delegate()
		{
			this.HireButton();
		});
		base.transform.Find("btnCancel").GetComponent<Button>().onClick.AddListener(delegate()
		{
			CrewSim.LowerUI(false);
		});
	}

	private void PageResume()
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		GUIChargenStack component = objThem.GetComponent<GUIChargenStack>();
		MonoSingleton<GUIRenderTargets>.Instance.SetFace(objThem, false);
		GameObject original = Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		string text = "Name: " + objThem.strName;
		text = text + "\nAge: " + Convert.ToInt32(objThem.GetCondAmount("StatAge"));
		text = text + "\nHomeworld: " + component.GetHomeworld().strColonyName;
		text += "\nLegal Status: ";
		foreach (Condition condition in objThem.mapConds.Values)
		{
			if (condition.nDisplayOther == 2 && condition.strName.IndexOf("IsStrata") >= 0)
			{
				text = text + "\n  " + GrammarUtils.GetInflectedString(condition.strDesc, condition, objThem);
			}
		}
		text += "\nCurrent Employer: ";
		if (objThem.Company == null)
		{
			text += "None";
		}
		else
		{
			text += objThem.Company.strName;
		}
		gameObject.GetComponent<Text>().text = text;
		original = (Resources.Load("GUIShip/GUIChargenCareer/bmpLine01") as GameObject);
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject);
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		text = "Career History";
		int year = CrewSim.system.GetYear();
		int num = Mathf.FloorToInt(Convert.ToSingle(objThem.GetCondAmount("StatAge")));
		int num2 = year - num;
		foreach (CareerChosen careerChosen in component.aCareers)
		{
			num2 += GUIChargenCareer.nTermYears;
			string text2 = text;
			text = string.Concat(new object[]
			{
				text2,
				"\n",
				num2,
				" - ",
				num2 + GUIChargenCareer.nTermYears,
				": ",
				careerChosen.GetJC().strNameFriendly
			});
			foreach (string str in careerChosen.aEvents)
			{
				text = text + "\n" + str;
			}
		}
		if (component.bCareerEnded)
		{
			gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
			text += "\nCareer ended.";
		}
		gameObject.GetComponent<Text>().text = text;
		original = (Resources.Load("GUIShip/GUIChargenCareer/bmpLine01") as GameObject);
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		original = (Resources.Load("GUIShip/GUIChargenCareer/lblLeft") as GameObject);
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfMain);
		text = "Skills: ";
		foreach (Condition condition2 in objThem.mapConds.Values)
		{
			if (condition2.nDisplayOther != 0 && condition2.nDisplayOther != 3 && condition2.strName.IndexOf("Skill") == 0)
			{
				text = text + "\n  " + condition2.strNameFriendly;
			}
		}
		gameObject.GetComponent<Text>().text = text;
		if (objThem.Company == this.COSelf.Company)
		{
			this.btnHire.GetComponentInChildren<TMP_Text>().text = "Fire";
		}
		else
		{
			this.btnHire.GetComponentInChildren<TMP_Text>().text = "Hire";
		}
	}

	private void HireButton()
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		if (objThem.Company == this.COSelf.Company)
		{
			this.Fire(objThem);
		}
		else
		{
			this.Hire(objThem);
		}
		CrewSim.LowerUI(false);
	}

	private void Hire(CondOwner coTarget)
	{
		this.COSelf.Company.mapRoster[coTarget.strID] = new JsonCompanyRules();
		coTarget.Company = this.COSelf.Company;
		string strMsg = coTarget.strName + " is now a member of " + coTarget.Company.strName + ".";
		this.COSelf.LogMessage(strMsg, "Neutral", this.COSelf.strName);
		coTarget.LogMessage(strMsg, "Neutral", coTarget.strName);
		coTarget.AddCondAmount("IsPlayerCrew", 1.0, 0.0, 0f);
	}

	private void Fire(CondOwner coTarget)
	{
		this.COSelf.Company.mapRoster.Remove(coTarget.strID);
		coTarget.Company = null;
		string strMsg = coTarget.strName + " no longer a member of " + this.COSelf.Company.strName + ".";
		this.COSelf.LogMessage(strMsg, "Neutral", this.COSelf.strName);
		coTarget.LogMessage(strMsg, "Neutral", coTarget.strName);
		coTarget.AddCondAmount("IsDrafted", -coTarget.GetCondAmount("IsDrafted"), 0.0, 0f);
		coTarget.AddCondAmount("IsPlayerCrew", -coTarget.GetCondAmount("IsPlayerCrew"), 0.0, 0f);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.PageResume();
	}

	private Transform bmpPortrait;

	private Transform tfMain;

	private Button btnHire;
}
