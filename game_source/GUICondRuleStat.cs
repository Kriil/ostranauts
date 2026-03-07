using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GUICondRuleStat : MonoBehaviour
{
	private void Awake()
	{
		this.txt = base.transform.Find("txt").GetComponent<TMP_Text>();
		this.txtArrow = base.transform.Find("txtArrow").GetComponent<TMP_Text>();
		this.glm = base.transform.Find("pnlLeds").GetComponent<GUILedMeter>();
		this.glm.SetState(1);
	}

	public void Draw(CondRule cr, CondOwner co, List<string> aReveals)
	{
		if (cr == null || co == null)
		{
			return;
		}
		CondRuleThresh currentThresh = cr.GetCurrentThresh(co);
		string discomfortForCond = co.GetDiscomfortForCond(cr.strCond);
		Condition condition = null;
		if (aReveals != null && aReveals.Contains(discomfortForCond) && co.mapConds.TryGetValue(discomfortForCond, out condition))
		{
			if (this.glm.State == 0)
			{
				this.glm.SetState(2);
			}
			int num = Array.IndexOf<CondRuleThresh>(cr.aThresholds, currentThresh);
			this.glm.SetValue(cr.aThresholds.Length - num);
			this.txt.text = condition.strNameFriendly;
			this.txtArrow.text = GUIStatus.GetStatusText(condition, Color.white);
		}
		else
		{
			this.glm.SetState(0);
			this.glm.SetValue(0);
			this.txt.text = DataHandler.GetCond(cr.strCond).strNameFriendly + ": ?";
			this.txtArrow.text = string.Empty;
		}
	}

	private TMP_Text txt;

	private TMP_Text txtArrow;

	private GUILedMeter glm;
}
