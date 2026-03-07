using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostranauts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUISocialStatus : MonoBehaviour
{
	private void Awake()
	{
		this.txtThemAbout = base.transform.Find("txtAbout").GetComponent<TMP_Text>();
		this.txtThemStatus = base.transform.Find("txtStatus").GetComponent<TMP_Text>();
		this.txtThemTraits01 = base.transform.Find("txtTraits01").GetComponent<TMP_Text>();
		this.txtThemTraits02 = base.transform.Find("txtTraits02").GetComponent<TMP_Text>();
		this.txtThemSkills01 = base.transform.Find("txtSkills01").GetComponent<TMP_Text>();
		this.txtThemSkills02 = base.transform.Find("txtSkills02").GetComponent<TMP_Text>();
		this.txtThemName = base.transform.Find("txtName").GetComponent<TMP_Text>();
		this.chkThemStatus = base.transform.Find("chkStatus").GetComponent<Toggle>();
		this.chkThemTraits = base.transform.Find("chkTraits").GetComponent<Toggle>();
		this.chkThemSkills = base.transform.Find("chkSkills").GetComponent<Toggle>();
		this.chkThemStatus.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnClickToggles();
		});
		this.chkThemTraits.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnClickToggles();
		});
		this.chkThemSkills.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnClickToggles();
		});
		AudioManager.AddBtnAudio(this.chkThemStatus.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		AudioManager.AddBtnAudio(this.chkThemTraits.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		AudioManager.AddBtnAudio(this.chkThemSkills.gameObject, "ShipUIBtnJobsKioskClickIn", "ShipUIBtnJobsKioskClickOut");
		this.tfThemStatBars = base.transform.Find("pnlStats");
		GameObject original = Resources.Load("prefabCondRuleStatBar(Masked)") as GameObject;
		this.dictStatBarsThem = new Dictionary<string, GUICondRuleStat2>();
		this.dictStatBarsThem["StatAchievement"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatAltruism"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatAutonomy"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatContact"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatEsteem"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatFamily"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatIntimacy"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatMeaning"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatPrivacy"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatSecurity"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.dictStatBarsThem["StatSelfRespect"] = UnityEngine.Object.Instantiate<GameObject>(original, this.tfThemStatBars).GetComponent<GUICondRuleStat2>();
		this.OnClickToggles();
	}

	private void OnClickToggles()
	{
		CanvasManager.HideCanvasGroup(this.txtThemStatus.GetComponent<CanvasGroup>());
		CanvasManager.HideCanvasGroup(this.txtThemSkills01.GetComponent<CanvasGroup>());
		CanvasManager.HideCanvasGroup(this.txtThemSkills02.GetComponent<CanvasGroup>());
		CanvasManager.HideCanvasGroup(this.txtThemTraits01.GetComponent<CanvasGroup>());
		CanvasManager.HideCanvasGroup(this.txtThemTraits02.GetComponent<CanvasGroup>());
		CanvasManager.HideCanvasGroup(this.tfThemStatBars.GetComponent<CanvasGroup>());
		if (this.chkThemStatus.isOn)
		{
			CanvasManager.ShowCanvasGroup(this.txtThemStatus.GetComponent<CanvasGroup>());
			CanvasManager.ShowCanvasGroup(this.tfThemStatBars.GetComponent<CanvasGroup>());
		}
		else if (this.chkThemTraits.isOn)
		{
			CanvasManager.ShowCanvasGroup(this.txtThemTraits01.GetComponent<CanvasGroup>());
			CanvasManager.ShowCanvasGroup(this.txtThemTraits02.GetComponent<CanvasGroup>());
		}
		else if (this.chkThemSkills.isOn)
		{
			CanvasManager.ShowCanvasGroup(this.txtThemSkills01.GetComponent<CanvasGroup>());
			CanvasManager.ShowCanvasGroup(this.txtThemSkills02.GetComponent<CanvasGroup>());
		}
	}

	public void SetData(CondOwner coUs, CondOwner coThem, bool bFirstPerson, Interaction iaSelGambit = null)
	{
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = new StringBuilder();
		StringBuilder stringBuilder3 = new StringBuilder();
		StringBuilder stringBuilder4 = new StringBuilder();
		if (coUs == null || coUs.socUs == null)
		{
			this.txtThemName.text = "N/A";
			this.txtThemAbout.text = string.Empty;
			this.txtThemStatus.text = string.Empty;
			this.txtThemTraits01.text = string.Empty;
			this.txtThemTraits02.text = string.Empty;
			return;
		}
		Relationship relationship = null;
		if (coThem == null && coUs != CrewSim.coPlayer)
		{
			coThem = CrewSim.coPlayer;
		}
		if (coThem != coUs && coThem != null && coThem.socUs != null)
		{
			if (coUs.socUs == null)
			{
				coUs.socUs = coUs.gameObject.AddComponent<global::Social>();
			}
			if (coUs.socUs.GetRelationship(coThem.strName) == null)
			{
				Relationship relationship2 = coUs.socUs.AddStranger(coThem.pspec);
			}
			if (coThem.socUs == null)
			{
				coThem.socUs = coThem.gameObject.AddComponent<global::Social>();
			}
			Relationship relationship3 = coThem.socUs.GetRelationship(coUs.strName);
			if (relationship3 == null)
			{
				relationship3 = coThem.socUs.AddStranger(coUs.pspec);
			}
			relationship = relationship3;
			if (bFirstPerson)
			{
				stringBuilder.AppendLine("They See Us As:");
			}
			else
			{
				stringBuilder.AppendLine("We See Them As:");
			}
			if (relationship.aRelationships.Count == 0)
			{
				stringBuilder.AppendLine("None");
			}
			else
			{
				foreach (string strName in relationship.aRelationships)
				{
					stringBuilder.AppendLine(DataHandler.GetCond(strName).strNameFriendly);
				}
			}
			stringBuilder.AppendLine();
		}
		else if (coUs == CrewSim.coPlayer)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine();
		}
		GUIChargenStack component = coUs.GetComponent<GUIChargenStack>();
		if (component != null)
		{
			stringBuilder.Append("Age: ");
			stringBuilder.AppendLine(coUs.GetCondAmount("StatAge").ToString("0"));
			stringBuilder.Append("Career: ");
			if (component.GetLatestCareer() != null && component.GetLatestCareer().GetJC() != null)
			{
				stringBuilder.AppendLine(component.GetLatestCareer().GetJC().strNameFriendly);
			}
			else
			{
				stringBuilder.AppendLine("?");
			}
			stringBuilder.Append("Homeworld: ");
			if (component.GetHomeworld() != null)
			{
				stringBuilder.AppendLine(component.GetHomeworld().strColonyName);
				stringBuilder.Append("Strata: ");
				string text = string.Empty;
				foreach (Condition condition in coUs.mapConds.Values)
				{
					if (condition.strName.IndexOf("IsStrata") == 0)
					{
						text = condition.strNameFriendly;
					}
				}
				if (text != string.Empty)
				{
					stringBuilder.AppendLine(text);
				}
				else
				{
					stringBuilder.AppendLine("?");
				}
			}
			else
			{
				stringBuilder.Append("?");
			}
		}
		this.txtThemName.text = coUs.strName;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		List<string> list = new List<string>();
		bool flag = false;
		if (flag)
		{
			foreach (Condition condition2 in coUs.mapConds.Values)
			{
				if (condition2.nDisplayOther != 0 && condition2.nDisplayOther != 3)
				{
					if (list.IndexOf(condition2.strName) < 0 && GUISocialCombat2.CountsAsSocialReveal(condition2.strName, true, true, true, true))
					{
						list.Add(condition2.strName);
					}
				}
			}
		}
		else if (coUs == CrewSim.coPlayer)
		{
			foreach (Condition condition3 in coUs.mapConds.Values)
			{
				if (condition3.nDisplaySelf == 2)
				{
					if (list.IndexOf(condition3.strName) < 0 && GUISocialCombat2.CountsAsSocialReveal(condition3.strName, true, true, true, true))
					{
						list.Add(condition3.strName);
					}
				}
			}
		}
		else
		{
			if (relationship != null)
			{
				list = relationship.aReveals;
			}
			foreach (Condition condition4 in coUs.mapConds.Values)
			{
				if (condition4.nDisplayOther == 2)
				{
					if (list.IndexOf(condition4.strName) < 0 && GUISocialCombat2.CountsAsSocialReveal(condition4.strName, true, true, true, true))
					{
						list.Add(condition4.strName);
					}
				}
			}
		}
		List<string> list2 = new List<string>();
		foreach (string text2 in list)
		{
			if (!coUs.mapConds.ContainsKey(text2))
			{
				list2.Add(text2);
			}
			else
			{
				Condition condition5 = null;
				if (coUs.mapConds.TryGetValue(text2, out condition5))
				{
					if (GUISocialCombat2.CountsAsSocialReveal(text2, false, true, false, false))
					{
						stringBuilder2.Append("<color=#" + ColorUtility.ToHtmlStringRGB(DataHandler.GetColor(condition5.strColor)) + ">");
						stringBuilder2.Append(condition5.strNameFriendly);
						stringBuilder2.AppendLine("</color>");
						num++;
					}
					else if (GUISocialCombat2.CountsAsSocialReveal(text2, false, false, true, false))
					{
						stringBuilder3.Append("<color=#" + ColorUtility.ToHtmlStringRGB(DataHandler.GetColor(condition5.strColor)) + ">");
						stringBuilder3.Append(condition5.strNameFriendly);
						stringBuilder3.AppendLine("</color>");
						num2++;
					}
					else if (GUISocialCombat2.CountsAsSocialReveal(text2, false, false, false, true))
					{
						stringBuilder4.Append("<color=#" + ColorUtility.ToHtmlStringRGB(DataHandler.GetColor(condition5.strColor)) + ">");
						stringBuilder4.Append(condition5.strNameFriendly);
						stringBuilder4.AppendLine("</color>");
						num3++;
					}
				}
			}
		}
		foreach (KeyValuePair<string, GUICondRuleStat2> keyValuePair in this.dictStatBarsThem)
		{
			Condition cond = null;
			CondRule condRule = coUs.GetCondRule(keyValuePair.Key);
			string discomfortForCond = coUs.GetDiscomfortForCond(condRule.strCond);
			if (!string.IsNullOrEmpty(discomfortForCond))
			{
				if (coUs.mapConds.TryGetValue(discomfortForCond, out cond) && !GUIStatus.StatusIsOld(cond) && !list.Contains(discomfortForCond))
				{
					list.Add(discomfortForCond);
				}
				keyValuePair.Value.Draw(condRule, coUs, list);
				keyValuePair.Value.ClearClosers();
				if (!bFirstPerson)
				{
					List<Interaction> list3 = new List<Interaction>();
					List<Interaction> list4 = new List<Interaction>();
					foreach (string strName2 in coUs.aInteractions)
					{
						Interaction interaction = DataHandler.GetInteraction(strName2, null, false);
						if (interaction != null && interaction.nMoveType == Interaction.MoveType.GAMBIT_FAIL)
						{
							list4.Add(interaction);
						}
					}
					if (iaSelGambit != null && iaSelGambit.nMoveType == Interaction.MoveType.GAMBIT)
					{
						foreach (string strName3 in iaSelGambit.aInverse)
						{
							Interaction interaction2 = DataHandler.GetInteraction(strName3, null, false);
							if (interaction2 != null)
							{
								if (interaction2.nMoveType == Interaction.MoveType.GAMBIT_FAIL)
								{
									list4.Add(interaction2);
								}
								else if (interaction2.nMoveType == Interaction.MoveType.GAMBIT_PASS)
								{
									list3.Add(interaction2);
								}
							}
						}
					}
					keyValuePair.Value.DrawClosersUs(condRule, list3);
					keyValuePair.Value.DrawClosersThem(condRule, list4);
				}
			}
		}
		if (coUs != CrewSim.coPlayer)
		{
			if (relationship != null)
			{
				foreach (string item in list2)
				{
					relationship.aReveals.Remove(item);
				}
			}
			if (num == 0)
			{
				stringBuilder.AppendLine(DataHandler.GetString("GUI_SOCIAL_NONE_REVEALED", false));
			}
			if (num2 == 0)
			{
				stringBuilder3.AppendLine(DataHandler.GetString("GUI_SOCIAL_NONE_REVEALED", false));
			}
			if (num3 == 0)
			{
				stringBuilder4.AppendLine(DataHandler.GetString("GUI_SOCIAL_NONE_REVEALED", false));
			}
		}
		this.txtThemAbout.text = stringBuilder.ToString();
		this.txtThemStatus.text = stringBuilder2.ToString();
		this.txtThemTraits01.text = stringBuilder3.ToString();
		this.txtThemSkills01.text = stringBuilder4.ToString();
		MonoSingleton<GUIRenderTargets>.Instance.SetFace(coUs, false);
	}

	public static void GetPassFailConds(List<string> aCondsPass, List<string> aCondsFail, CondOwner coUs, CondOwner coTarget, Interaction iaSelGambit = null)
	{
		if (coUs == null || coTarget == null || coTarget.socUs == null || aCondsPass == null || aCondsFail == null)
		{
			return;
		}
		if (GUISocialStatus.aStats == null)
		{
			GUISocialStatus.aStats = new List<string>();
			GUISocialStatus.aStats.Add("StatAchievement");
			GUISocialStatus.aStats.Add("StatAltruism");
			GUISocialStatus.aStats.Add("StatAutonomy");
			GUISocialStatus.aStats.Add("StatContact");
			GUISocialStatus.aStats.Add("StatEsteem");
			GUISocialStatus.aStats.Add("StatFamily");
			GUISocialStatus.aStats.Add("StatIntimacy");
			GUISocialStatus.aStats.Add("StatMeaning");
			GUISocialStatus.aStats.Add("StatPrivacy");
			GUISocialStatus.aStats.Add("StatSecurity");
			GUISocialStatus.aStats.Add("StatSelfRespect");
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (string strCond in GUISocialStatus.aStats)
		{
			CondRule condRule = coTarget.GetCondRule(strCond);
			List<Interaction> list3 = new List<Interaction>();
			List<Interaction> list4 = new List<Interaction>();
			Relationship relationship = coTarget.socUs.GetRelationship(coUs.strName);
			if (relationship != null && relationship.strContext != null && relationship.strContext != "Default")
			{
				foreach (string strName in DataHandler.GetLoot(relationship.strContext).GetLootNames(null, false, null))
				{
					Interaction interaction = DataHandler.GetInteraction(strName, null, false);
					if (interaction != null && interaction.nMoveType == Interaction.MoveType.GAMBIT_FAIL)
					{
						list4.Add(interaction);
					}
				}
			}
			if (iaSelGambit != null && iaSelGambit.nMoveType == Interaction.MoveType.GAMBIT)
			{
				foreach (string strName2 in iaSelGambit.aInverse)
				{
					Interaction interaction2 = DataHandler.GetInteraction(strName2, null, false);
					if (interaction2 != null)
					{
						if (interaction2.nMoveType == Interaction.MoveType.GAMBIT_FAIL)
						{
							list4.Add(interaction2);
						}
						else if (interaction2.nMoveType == Interaction.MoveType.GAMBIT_PASS)
						{
							list3.Add(interaction2);
						}
					}
				}
			}
			if (list4.Count != 0 || list3.Count != 0)
			{
				string value = "Dc" + condRule.strCond.Substring(4);
				List<string> list5 = new List<string>();
				foreach (CondRuleThresh condRuleThresh in condRule.aThresholds)
				{
					Loot loot = DataHandler.GetLoot(condRuleThresh.strLootNew);
					List<string> lootNames = loot.GetLootNames(null, false, null);
					foreach (string text in lootNames)
					{
						if (text.IndexOf(value) == 0)
						{
							list5.Add(text);
						}
					}
				}
				list.AddRange(GUISocialStatus.GetHighlights(list3, list5));
				list2.AddRange(GUISocialStatus.GetHighlights(list4, list5));
			}
		}
		aCondsPass.AddRange(list.Distinct<string>());
		aCondsFail.AddRange(list2.Distinct<string>());
	}

	private static List<string> GetHighlights(List<Interaction> aIAs, List<string> aDCs)
	{
		List<string> list = new List<string>();
		foreach (Interaction interaction in aIAs)
		{
			if (interaction != null)
			{
				CondTrigger cttestUs = interaction.CTTestUs;
				List<string> closerHighlights = cttestUs.GetCloserHighlights(aDCs);
				for (int i = 0; i < aDCs.Count; i++)
				{
					if (closerHighlights.IndexOf(aDCs[i]) >= 0)
					{
						list.Add(aDCs[i]);
					}
				}
			}
		}
		return list;
	}

	private Dictionary<string, GUICondRuleStat2> dictStatBarsThem;

	private static List<string> aStats;

	private TMP_Text txtThemName;

	private TMP_Text txtThemAbout;

	private TMP_Text txtThemStatus;

	private TMP_Text txtThemTraits01;

	private TMP_Text txtThemTraits02;

	private TMP_Text txtThemSkills01;

	private TMP_Text txtThemSkills02;

	private Transform tfThemStatBars;

	private Toggle chkThemStatus;

	private Toggle chkThemTraits;

	private Toggle chkThemSkills;
}
