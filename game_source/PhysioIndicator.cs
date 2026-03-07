using System;
using Ostranauts.Core;
using Ostranauts.UI.CrewBar;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhysioIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	private float CalculateFillAmount(CondOwner co, bool instant)
	{
		if (this._physio.NeedsRoom && !co.HasCond("IsAirtight") && co.currentRoom != null && co.currentRoom.CO != null)
		{
			co = co.currentRoom.CO;
		}
		double num = co.GetCondAmount(this.strStatTracked);
		this.SetImgDirection(num);
		this.fStatAmount = num;
		num -= this.fMinimum;
		float num2;
		if (num < 0.0)
		{
			num2 = (float)(num / this.fInverseMaximum);
			this._imgIcon.texture = this._physio.iconNeg;
		}
		else
		{
			num2 = (float)(num / this.fMaximum);
			this._imgIcon.texture = this._physio.iconPos;
		}
		float num3 = this._imgFill.fillAmount;
		if ((double)Mathf.Abs(num2 - num3) < 0.01)
		{
			num3 = num2;
		}
		return (!instant) ? (num3 + (num2 - num3) * CrewSim.TimeElapsedUnscaled() * 2f) : num2;
	}

	private void SetImgDirection(double stat)
	{
		double num = stat - this.fStatAmount;
		if (num > 0.001)
		{
			this._imgDirection.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
		}
		else if (num < -0.001)
		{
			this._imgDirection.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
		}
	}

	public void UpdateCO(CondOwner co, bool instant = false)
	{
		this._imgFill.fillAmount = this.CalculateFillAmount(co, instant);
		if (this._physio == null || this._physio.aConds == null)
		{
			return;
		}
		foreach (Condition condition in this._physio.aConds)
		{
			if (co.HasCond(condition.strName))
			{
				this._imgFill.color = DataHandler.GetColor(condition.strColor);
				this.strMain = condition.strNameFriendly;
				this.strBody = GrammarUtils.GetInflectedString(condition.strDesc, condition, co);
				this.strCoFriendlyName = co.FriendlyName;
				return;
			}
		}
		this._imgFill.color = DataHandler.GetColor("Good");
		this.strMain = "Unknown";
		this.strBody = "Status is not available";
	}

	public void Init(PhysioDef physio = null)
	{
		this._physio = (physio ?? MonoSingleton<GUICrewStatus>.Instance.GetPhysioDef(this.strStatTracked));
		if (this._physio == null)
		{
			return;
		}
		this.strStatTracked = this._physio.StatTracked;
		this._physio.Init();
		this.strTitle = this._physio.Title;
		this.strIcon = this._physio.Icon;
		this.strIconNeg = this._physio.IconNeg;
		this.fMaximum = this._physio.Maximum;
		this.fMinimum = this._physio.Minimum;
		this.fInverseMaximum = this._physio.InverseMaximum;
		if (this._bmpPulse != null)
		{
			this._imgPulse = this._bmpPulse.gameObject.GetComponent<Image>();
			this._bmpPulse.alpha = 0f;
		}
		this._imgIcon.texture = DataHandler.LoadPNG(this.strIcon + ".png", false, false);
	}

	private void Update()
	{
		if (this._bmpPulse == null)
		{
			return;
		}
		if (this._imgFill.fillAmount < 0.8f)
		{
			this._bmpPulse.alpha = 0f;
		}
		else
		{
			this._bmpPulse.alpha = Mathf.Lerp(0f, 1f, (Mathf.Sin(Time.unscaledTime * 2f) + 1f) / 2f);
			if (this._imgPulse != null)
			{
				this._imgPulse.color = this._imgFill.color;
			}
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		GUITooltip2.SetToolTip_1(DataHandler.GetString(this.strTitle, false), this.strMain, this.strBody.Replace("[us]", this.strCoFriendlyName), true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		GUITooltip2.SetToolTip_1(string.Empty, string.Empty, string.Empty, false);
	}

	private string strTitle = "GUI_STAT_NONE";

	private string strBody = "Normal: This status is normal";

	private string strMain = "Normal";

	private string strIcon = "ComputerIconX";

	private string strIconNeg = "ComputerIconX";

	private string strCoFriendlyName = "[us]";

	[SerializeField]
	private string strStatTracked = string.Empty;

	private double fStatAmount;

	private PhysioDef _physio;

	private double fMinimum;

	private double fMaximum = 100.0;

	private double fInverseMaximum = -100.0;

	[SerializeField]
	private Image _imgFill;

	[SerializeField]
	private Image _imgDirection;

	[SerializeField]
	private RawImage _imgIcon;

	[SerializeField]
	private CanvasGroup _bmpPulse;

	private Image _imgPulse;
}
