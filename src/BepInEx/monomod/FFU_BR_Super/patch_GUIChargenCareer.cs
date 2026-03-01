using System;
using System.Collections.Generic;
using System.Text;
using FFU_Beyond_Reach;
using Ostranauts.ShipGUIs.Chargen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// Super-settings patch for character generation costs and limits.
// This likely implements free skill/trait changes and the broader chargen range
// options exposed by the FFU_BR super config.
public class patch_GUIChargenCareer : GUIChargenCareer
{
	private void AddSkillTrait(JsonCareer jc, string strChosen)
	{
		this.bmpDot2.color = Color.white;
		List<string> list = new List<string>(this.coUser.mapConds.Keys);
		this.cgs.AddCareer(jc);
		CareerChosen latestCareer = this.cgs.GetLatestCareer();
		this.cgs.ApplyCareer(this.coUser, latestCareer, true);
		latestCareer.bTermEnded = true;
		latestCareer.aEvents.Add(strChosen);
		double num = 1.0;
		bool flag = strChosen != null && strChosen.IndexOf("-") == 0;
		if (flag)
		{
			strChosen = strChosen.Substring(1);
			num = -1.0;
		}
		int num2 = FFU_BR_Defs.NoSkillTraitCost ? 0 : (base.GetTraitYears(strChosen) * (int)num);
		CondRuleThresh changedCRThresh = base.GetChangedCRThresh(0, num2);
		Dictionary<string, double> dictionary;
		bool flag2 = changedCRThresh != null && this._promisedAgeLoot.TryGetValue(changedCRThresh.strLootNew, out dictionary) && dictionary != null;
		if (flag2)
		{
			foreach (KeyValuePair<string, double> keyValuePair in dictionary)
			{
				this.coUser.AddCondAmount(keyValuePair.Key, keyValuePair.Value, 0.0, 0f);
			}
		}
		this.coUser.bFreezeCondRules = true;
		this.coUser.AddCondAmount("StatAge", (double)num2, 0.0, 0f);
		this.coUser.bFreezeCondRules = false;
		this.coUser.AddCondAmount(strChosen, num, 0.0, 0f);
		foreach (Condition condition in this.coUser.mapConds.Values)
		{
			bool flag3 = list.IndexOf(condition.strName) < 0 && latestCareer.aSkillsChosen.IndexOf(condition.strName) < 0 && condition.nDisplaySelf == 2 && condition.strName.IndexOf("Dc") != 0;
			if (flag3)
			{
				latestCareer.aSkillsChosen.Add(condition.strName);
			}
		}
	}
	private void RebuildMultiSelectSidebar()
	{
		bool flag = this._selectedSkills.Count == 0;
		if (flag)
		{
			base.UpdateSidebar();
		}
		else
		{
			foreach (object obj in this.tfSidebar)
			{
				Transform transform = (Transform)obj;
				Object.Destroy(transform.gameObject);
			}
			VerticalLayoutGroup component = this.tfSidebar.GetComponent<VerticalLayoutGroup>();
			component.spacing = 2f;
			LayoutRebuilder.ForceRebuildLayoutImmediate(this.tfSidebar.GetComponent<RectTransform>());
			string @string = DataHandler.GetString("GUI_CAREER_SIDEBAR_COST_2", false);
			int num = 0;
			base.SpawnSideBarHeader();
			foreach (SkillSelectionDTO skillSelectionDTO in this._selectedSkills)
			{
				int num2 = FFU_BR_Defs.NoSkillTraitCost ? 0 : (base.GetTraitYears(skillSelectionDTO.CondName) * skillSelectionDTO.Change);
				skillSelectionDTO.AgeConds = base.GetAgeRelatedConds(num, num2);
				base.SpawnAgeRelatedConds(skillSelectionDTO.AgeConds);
				GUISkillToggle guiskillToggle;
				bool flag2 = this._dictCheckmarks.TryGetValue(skillSelectionDTO.CondName, out guiskillToggle);
				if (flag2)
				{
					base.UpdateToggle(skillSelectionDTO.Condition, (double)skillSelectionDTO.Change);
				}
				num += num2;
				GameObject gameObject = Object.Instantiate<GameObject>(this.pnlColumnLists, this.tfSidebar);
				GameObject gameObject2 = Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject.transform);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("<b>");
				bool flag3 = skillSelectionDTO.Change < 0;
				if (flag3)
				{
					stringBuilder.Append("- ");
				}
				else
				{
					stringBuilder.Append("+ ");
				}
				stringBuilder.Append(skillSelectionDTO.Condition.strNameFriendly);
				stringBuilder.AppendLine("</b> ");
				gameObject2.GetComponent<TMP_Text>().text = stringBuilder.ToString();
				GameObject gameObject3 = Object.Instantiate<GameObject>(this.lblLeftTMP, gameObject.transform);
				stringBuilder = new StringBuilder();
				stringBuilder.Append(num2);
				stringBuilder.AppendLine(@string);
				TMP_Text component2 = gameObject3.GetComponent<TMP_Text>();
				component2.alignment = 516;
				component2.text = stringBuilder.ToString();
			}
			Object.Instantiate<GameObject>(this.bmpLine01, this.tfSidebar);
			GameObject gameObject4 = Object.Instantiate<GameObject>(this.lblLeftTMP, this.tfSidebar);
			TMP_Text component3 = gameObject4.GetComponent<TMP_Text>();
			component3.alignment = 516;
			component3.text = "<b>Total Cost: </b>" + num.ToString() + @string;
			base.AddApplyClearButtonSection(num);
			base.StartCoroutine(CrewSim.objInstance.ScrollBottom(this.srSide));
		}
	}
}
