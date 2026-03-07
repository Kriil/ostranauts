using System;
using TMPro;
using UnityEngine;

public class GUIStatus : MonoBehaviour
{
	private void Init()
	{
		this.txtCondsUs = base.transform.Find("txt01").GetComponent<TMP_Text>();
		this.txtNameUs = base.transform.Find("txtName").GetComponent<TMP_Text>();
	}

	public static bool StatusIsOld(Condition cond)
	{
		if (cond == null || cond.fCondRuleTrack == 0.0)
		{
			return true;
		}
		int num = Mathf.RoundToInt((float)(StarSystem.fEpoch - cond.fCondRuleTrackTime));
		return num >= 3;
	}

	public static string GetStatusText(Condition cond, Color clrArrow)
	{
		if (cond == null || cond.fCondRuleTrack == 0.0)
		{
			return string.Empty;
		}
		string text = string.Empty;
		float num = (float)(StarSystem.fEpoch - cond.fCondRuleTrackTime);
		int num2 = Mathf.RoundToInt(num);
		float a = 1f;
		if (GUIStatus.StatusIsOld(cond))
		{
			cond.fCondRuleTrack = 0.0;
		}
		else if (num2 >= 1)
		{
			a = (0.75f + 0.25f * Mathf.Sin(5f * num)) * Mathf.Pow(0.85f, (float)num2);
		}
		if ((!cond.bCondRuleTrackInvert && cond.fCondRuleTrack > 0.0) || (cond.bCondRuleTrackInvert && cond.fCondRuleTrack < 0.0))
		{
			text = text + "<sprite=\"FontSprites\" index=3 color=\"#" + ColorUtility.ToHtmlStringRGBA(new Color(clrArrow.r, clrArrow.g, clrArrow.b, a)) + "\">";
		}
		else if ((!cond.bCondRuleTrackInvert && cond.fCondRuleTrack < 0.0) || (cond.bCondRuleTrackInvert && cond.fCondRuleTrack > 0.0))
		{
			text = text + "<sprite=\"FontSprites\" index=2 color=\"#" + ColorUtility.ToHtmlStringRGBA(new Color(clrArrow.r, clrArrow.g, clrArrow.b, a)) + "\">";
		}
		return text;
	}

	private TMP_Text txtCondsUs;

	private TMP_Text txtNameUs;
}
