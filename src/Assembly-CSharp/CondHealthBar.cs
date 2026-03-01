using System;
using UnityEngine;
using UnityEngine.UI;

public class CondHealthBar : MonoBehaviour
{
	private void Start()
	{
	}

	public void RefreshValues()
	{
		if (this.m_condOwner == null)
		{
			return;
		}
		if (this.m_numerator != null && this.m_numerator != string.Empty)
		{
			this.m_numAmount = (float)this.m_condOwner.GetCondAmount(this.m_numerator);
		}
		if (this.m_denominator != null && this.m_denominator != string.Empty)
		{
			this.m_denAmount = (float)this.m_condOwner.GetCondAmount(this.m_denominator);
		}
		this.CalcFill();
	}

	private void CalcFill()
	{
		if (this.m_numAmount < 0f)
		{
			this.m_numAmount = 0f;
		}
		if (this.m_denAmount <= 0f)
		{
			this.m_bar.fillAmount = 1f;
			this.m_bar.enabled = false;
			this.m_bg.enabled = false;
		}
		else
		{
			float num = this.m_numAmount / this.m_denAmount;
			this.m_bar.enabled = true;
			this.m_bg.enabled = true;
			if (this.m_flip)
			{
				this.m_bar.fillAmount = 1f - num;
			}
			else
			{
				this.m_bar.fillAmount = num;
			}
		}
	}

	public CondOwner condOwner
	{
		set
		{
			this.m_condOwner = value;
			if (this.m_condOwner == null)
			{
				return;
			}
			if (this.m_numerator != null && this.m_numerator != string.Empty)
			{
				this.m_numAmount = (float)this.m_condOwner.GetCondAmount(this.m_numerator);
			}
			if (this.m_denominator != null && this.m_denominator != string.Empty)
			{
				this.m_denAmount = (float)this.m_condOwner.GetCondAmount(this.m_denominator);
			}
			this.CalcFill();
		}
	}

	public float numAmount
	{
		get
		{
			return this.m_numAmount;
		}
		set
		{
			this.m_numAmount = value;
			this.CalcFill();
		}
	}

	public float denAmount
	{
		get
		{
			return this.m_denAmount;
		}
		set
		{
			this.m_denAmount = value;
			this.CalcFill();
		}
	}

	public string numerator
	{
		get
		{
			return this.m_numerator;
		}
		set
		{
			this.m_numerator = value;
			if (this.m_numerator != null && this.m_numerator != string.Empty)
			{
				this.m_numAmount = (float)this.m_condOwner.GetCondAmount(this.m_numerator);
			}
			this.CalcFill();
		}
	}

	public string denominator
	{
		get
		{
			return this.m_denominator;
		}
		set
		{
			this.m_denominator = value;
			if (this.m_denominator != null && this.m_denominator != string.Empty)
			{
				this.m_denAmount = (float)this.m_condOwner.GetCondAmount(this.m_denominator);
			}
			this.CalcFill();
		}
	}

	public string fillColor
	{
		set
		{
			this.m_bar.color = DataHandler.GetColor(value);
		}
	}

	public string bgColor
	{
		set
		{
			this.m_bg.color = DataHandler.GetColor(value);
		}
	}

	private float m_numAmount;

	private float m_denAmount;

	private string m_numerator = "StatDamage";

	private string m_denominator = "StatDamageMax";

	private CondOwner m_condOwner;

	public Image m_bar;

	public Image m_bg;

	public bool m_flip = true;
}
