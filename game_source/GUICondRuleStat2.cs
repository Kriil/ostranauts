using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUICondRuleStat2 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	private void Awake()
	{
		this.txtBack = base.transform.Find("txtBack").GetComponent<TMP_Text>();
		this.txtFore = base.transform.Find("bmpMask").Find("txtFore").GetComponent<TMP_Text>();
		this.txtArrow = base.transform.Find("txtArrow").GetComponent<TMP_Text>();
		this.barMask = base.transform.Find("bmpMask").GetComponent<Image>();
		this.barCore = base.transform.Find("bmpMask").Find("bmpBarFore").GetComponent<Image>();
		this.aBarBad = new List<Image>();
		this.aBarGood = new List<Image>();
		this.GetBars("bmpBarBad", this.aBarBad);
		this.GetBars("bmpBarGood", this.aBarGood);
		this.clrGreen = DataHandler.GetColor("SocialStatusGreen");
		this.clrRed = DataHandler.GetColor("SocialStatusRed");
		this.clrUnknown = DataHandler.GetColor("SocialStatusUnknown");
		this.clrGood = DataHandler.GetColor("SocialStatusPositive");
		this.clrBad = DataHandler.GetColor("SocialStatusNegative");
		this.clrBarDC5 = DataHandler.GetColor("SocialStatusDC5");
		this.clrBarDC4 = DataHandler.GetColor("SocialStatusDC4");
		this.clrBarDC3 = DataHandler.GetColor("SocialStatusDC3");
		this.clrBarDC2 = DataHandler.GetColor("SocialStatusDC2");
		this.clrBarDC1 = DataHandler.GetColor("SocialStatusDC1");
		this.clrBarDC0 = DataHandler.GetColor("SocialStatusDC0");
		this.SetUpColors();
	}

	private void Update()
	{
		float num = this.fLerpAmt * Time.deltaTime;
		if (this.barMask.fillAmount - num > this.fIntent)
		{
			this.barMask.fillAmount -= num;
		}
		else if (this.barMask.fillAmount + num < this.fIntent)
		{
			this.barMask.fillAmount += num;
		}
		else
		{
			this.barMask.fillAmount = this.fIntent;
		}
		Color color = this.clrGradient.Evaluate(this.barMask.fillAmount);
		if (this.fFlashTime > 0f)
		{
			color = Color.Lerp(color, this.clrFlash, this.fFlashTime / this.fFlashMax);
			this.fFlashTime -= num;
		}
		this.barCore.color = color;
		if (this.barMask.fillAmount == 0f)
		{
			this.txtBack.color = this.clrUnknown;
		}
		else
		{
			color.a = 1f;
			this.txtBack.color = color;
		}
		if (this.bFlash)
		{
			this.bFlash = false;
			this.Flash();
		}
	}

	private void GetBars(string strBar, List<Image> aBars)
	{
		Transform transform = base.transform.Find(strBar);
		IEnumerator enumerator = transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform2 = (Transform)obj;
				aBars.Add(transform2.GetComponent<Image>());
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
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
			this.barMask.gameObject.SetActive(true);
			int num = Array.IndexOf<CondRuleThresh>(cr.aThresholds, currentThresh);
			this.fIntent = (float)(cr.aThresholds.Length - num) / (float)cr.aThresholds.Length;
			this.txtBack.text = condition.strNameFriendly;
			this.txtFore.text = condition.strNameFriendly;
			this.txtArrow.text = GUIStatus.GetStatusText(condition, Color.white);
			this.strTtTitle = condition.strNameFriendly;
			this.strTtBody = GrammarUtils.GetInflectedString(condition.strDesc, condition, co);
			double condAmount = co.GetCondAmount(cr.strCond);
			if (this.fCondAmt != condAmount)
			{
				if (this.fCondAmt > condAmount)
				{
					this.clrFlash = this.clrGood;
				}
				else
				{
					this.clrFlash = this.clrBad;
				}
				this.Flash();
				this.fCondAmt = condAmount;
			}
		}
		else
		{
			this.barMask.fillAmount = 0f;
			this.fIntent = 0f;
			this.barMask.gameObject.SetActive(false);
			this.txtBack.text = DataHandler.GetCond(cr.strCond).strNameFriendly + ": ?";
			this.txtFore.text = DataHandler.GetCond(cr.strCond).strNameFriendly + ": ?";
			this.txtArrow.text = string.Empty;
		}
	}

	public void ClearClosers()
	{
		foreach (Image image in this.aBarBad)
		{
			image.color = Color.clear;
		}
		foreach (Image image2 in this.aBarGood)
		{
			image2.color = Color.clear;
		}
	}

	public void DrawClosersUs(CondRule cr, List<Interaction> aIAs)
	{
		this.DrawClosers(cr, aIAs, this.aBarGood, this.clrGreen, false);
	}

	public void DrawClosersThem(CondRule cr, List<Interaction> aIAs)
	{
		this.DrawClosers(cr, aIAs, this.aBarBad, this.clrRed, false);
	}

	public void DrawClosers(CondRule cr, List<Interaction> aIAs, List<Image> aBar, Color clr, bool bCTTestThem)
	{
		if (cr == null || aIAs == null)
		{
			return;
		}
		string value = "Dc" + cr.strCond.Substring(4);
		List<string> list = new List<string>();
		foreach (CondRuleThresh condRuleThresh in cr.aThresholds)
		{
			Loot loot = DataHandler.GetLoot(condRuleThresh.strLootNew);
			List<string> lootNames = loot.GetLootNames(null, false, null);
			foreach (string text in lootNames)
			{
				if (text.IndexOf(value) == 0)
				{
					list.Add(text);
				}
			}
		}
		uint num = 0U;
		foreach (Interaction interaction in aIAs)
		{
			if (interaction != null)
			{
				CondTrigger condTrigger = interaction.CTTestUs;
				if (bCTTestThem)
				{
					condTrigger = interaction.CTTestThem;
				}
				List<string> closerHighlights = condTrigger.GetCloserHighlights(list);
				for (int j = 0; j < list.Count; j++)
				{
					if (closerHighlights.IndexOf(list[j]) >= 0)
					{
						uint num2 = 1U << j;
						num |= num2;
					}
				}
			}
		}
		RectTransform rectTransform = null;
		switch (num)
		{
		case 15U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.2f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		case 16U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.2f, 1f);
			break;
		case 17U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.2f, 1f);
			aBar[1].color = clr;
			rectTransform = aBar[1].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.8f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		default:
			switch (num)
			{
			case 1U:
				aBar[0].color = clr;
				rectTransform = aBar[0].GetComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(0.8f, 0f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				break;
			case 3U:
				aBar[0].color = clr;
				rectTransform = aBar[0].GetComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(0.6f, 0f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				break;
			case 4U:
				aBar[0].color = clr;
				rectTransform = aBar[0].GetComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(0.6f, 0f);
				rectTransform.anchorMax = new Vector2(0.8f, 1f);
				break;
			case 6U:
				aBar[0].color = clr;
				rectTransform = aBar[0].GetComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(0.4f, 0f);
				rectTransform.anchorMax = new Vector2(0.6f, 1f);
				break;
			case 7U:
				aBar[0].color = clr;
				rectTransform = aBar[0].GetComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(0.4f, 0f);
				rectTransform.anchorMax = new Vector2(1f, 1f);
				break;
			case 8U:
				aBar[0].color = clr;
				rectTransform = aBar[0].GetComponent<RectTransform>();
				rectTransform.anchorMin = new Vector2(0.2f, 0f);
				rectTransform.anchorMax = new Vector2(0.4f, 1f);
				break;
			}
			break;
		case 19U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.2f, 1f);
			aBar[1].color = clr;
			rectTransform = aBar[1].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.6f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		case 21U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.2f, 1f);
			aBar[1].color = clr;
			rectTransform = aBar[1].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.4f, 0f);
			rectTransform.anchorMax = new Vector2(0.6f, 1f);
			aBar[2].color = clr;
			rectTransform = aBar[2].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.8f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		case 23U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.2f, 1f);
			aBar[1].color = clr;
			rectTransform = aBar[1].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.4f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		case 24U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.4f, 1f);
			break;
		case 25U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.4f, 1f);
			aBar[1].color = clr;
			rectTransform = aBar[1].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.8f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		case 27U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.4f, 1f);
			aBar[1].color = clr;
			rectTransform = aBar[1].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.6f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		case 28U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.6f, 1f);
			break;
		case 29U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.6f, 1f);
			aBar[1].color = clr;
			rectTransform = aBar[1].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0.8f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		case 30U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(0.8f, 1f);
			break;
		case 31U:
			aBar[0].color = clr;
			rectTransform = aBar[0].GetComponent<RectTransform>();
			rectTransform.anchorMin = new Vector2(0f, 0f);
			rectTransform.anchorMax = new Vector2(1f, 1f);
			break;
		}
		if (rectTransform != null)
		{
			rectTransform.offsetMin = new Vector2(-3f, -3f);
			rectTransform.offsetMax = new Vector2(3f, 3f);
		}
	}

	public void SetUpColors()
	{
		this.clrGradient = new Gradient();
		GradientColorKey[] array = new GradientColorKey[6];
		array[0].color = this.clrBarDC0;
		array[0].time = 0f;
		array[1].color = this.clrBarDC1;
		array[1].time = 0.2f;
		array[2].color = this.clrBarDC2;
		array[2].time = 0.4f;
		array[3].color = this.clrBarDC3;
		array[3].time = 0.6f;
		array[4].color = this.clrBarDC4;
		array[4].time = 0.8f;
		array[5].color = this.clrBarDC5;
		array[5].time = 1f;
		GradientAlphaKey[] array2 = new GradientAlphaKey[2];
		array2[0].alpha = 1f;
		array2[0].time = 0f;
		array2[1].alpha = 1f;
		array2[1].time = 1f;
		this.clrGradient.SetKeys(array, array2);
		this.clrGradient.mode = GradientMode.Blend;
	}

	public void Flash()
	{
		this.fFlashTime = this.fFlashMax;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
	}

	private TMP_Text txtBack;

	private TMP_Text txtFore;

	private TMP_Text txtArrow;

	private List<Image> aBarGood;

	private List<Image> aBarBad;

	private Image barMask;

	private Image barCore;

	private Color clrGreen;

	private Color clrRed;

	public Color clrUnknown;

	public Color clrFlash;

	public Color clrGood;

	public Color clrBad;

	public Color clrBarDC5;

	public Color clrBarDC4;

	public Color clrBarDC3;

	public Color clrBarDC2;

	public Color clrBarDC1;

	public Color clrBarDC0;

	public Gradient clrGradient;

	public bool bFlash;

	private float fLerpAmt = 0.3f;

	private float fFlashMax = 0.75f;

	private float fFlashTime;

	private float fIntent;

	private double fCondAmt;

	private string strTtTitle = string.Empty;

	private string strTtBody = string.Empty;
}
