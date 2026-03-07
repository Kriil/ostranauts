using System;
using UnityEngine;
using UnityEngine.UI;

public class GUI7Seg : MonoBehaviour
{
	private void Awake()
	{
		Transform transform = base.transform;
		this.aDigits = new Image[transform.childCount / 3];
		this.aDots = new GameObject[transform.childCount / 3];
		for (int i = 0; i < this.aDots.Length; i++)
		{
			this.aDigits[i] = transform.Find("bmpDigit" + i).GetComponent<Image>();
			this.aDots[i] = transform.Find("bmpDot" + i).gameObject;
		}
		this.fValue = 0f;
		this.State = 0;
	}

	private void Update()
	{
		if (this.nState == 1)
		{
			float num = this.fInitTime;
			this.fInitTime += Time.deltaTime * this.fInitSpeed;
			if (this.fInitTime > 1f && num <= 1f)
			{
				for (int i = this.aDigits.Length - 1; i >= 0; i--)
				{
					this.aDots[i].SetActive(true);
					this.aDigits[i].sprite = this.aSpriteSheet[8];
				}
			}
			else if (this.fInitTime > 2f && num <= 2f)
			{
				this.SetEmpty();
			}
			else if (this.fInitTime > 3f)
			{
				this.State = 2;
			}
		}
		if (this.nState == 2)
		{
			if (this.bTextMode)
			{
				if (this.strValueLast != this.strValue)
				{
					this.SetDisplay(this.strValue);
				}
			}
			else if (this.fValueLast != this.fValue)
			{
				this.SetDisplay(this.fValue);
			}
		}
	}

	public void SetValue(float fVal)
	{
		this.bTextMode = false;
		this.fValue = fVal;
	}

	public void SetValue(string strVal)
	{
		this.bTextMode = true;
		this.strValue = strVal;
	}

	private void SetDisplay(float fVal)
	{
		if (fVal < 0f)
		{
			fVal = 0f;
		}
		string text = fVal.ToString("0.00");
		while (text.Length - 1 > this.aDigits.Length)
		{
			if (text[text.Length - 1] != '.')
			{
				text = text.Remove(text.Length - 1);
			}
			else
			{
				text = text.Remove(1);
			}
		}
		int num = this.aDigits.Length - (text.Length - 1);
		int num2 = text.Length;
		int num3 = text.IndexOf('.');
		for (int i = this.aDigits.Length - 1; i >= 0; i--)
		{
			num2--;
			this.aDots[i].SetActive(num2 == num3);
			if (num2 == num3)
			{
				num2--;
			}
			if (i >= num)
			{
				char c = text[num2];
				int num4 = (int)(c - '0');
				this.aDigits[i].sprite = this.aSpriteSheet[num4];
			}
			else
			{
				this.aDigits[i].sprite = this.aSpriteSheet[10];
			}
		}
		this.fValueLast = fVal;
	}

	private void SetDisplay(string strVal)
	{
		for (int i = 0; i < this.aDigits.Length; i++)
		{
			if (i >= strVal.Length)
			{
				this.aDigits[i].sprite = this.aSpriteSheet[10];
				this.aDots[i].SetActive(false);
			}
			else if (strVal[i] == '.')
			{
				this.aDots[i].SetActive(true);
			}
			else
			{
				this.aDots[i].SetActive(false);
				char c = strVal[i];
				int num = (int)(c - '0');
				if (num >= 0 && num < this.aSpriteSheet.Length)
				{
					this.aDigits[i].sprite = this.aSpriteSheet[num];
				}
				else
				{
					this.aDigits[i].sprite = this.aSpriteSheet[10];
				}
			}
		}
		this.strValueLast = strVal;
	}

	private void SetEmpty()
	{
		this.fValueLast = -1f;
		for (int i = this.aDigits.Length - 1; i >= 0; i--)
		{
			this.aDots[i].SetActive(false);
			this.aDigits[i].sprite = this.aSpriteSheet[10];
		}
	}

	public int State
	{
		get
		{
			return this.nState;
		}
		set
		{
			this.nState = value;
			int num = this.nState;
			if (num != 1)
			{
				if (num != 2)
				{
					this.SetEmpty();
				}
				else if (this.bTextMode)
				{
					this.SetDisplay(this.strValue);
				}
				else
				{
					this.SetDisplay(this.fValue);
				}
			}
			else
			{
				this.fInitTime = 0f;
			}
		}
	}

	private Image[] aDigits;

	private GameObject[] aDots;

	public Sprite[] aSpriteSheet;

	public float fInitSpeed = 2f;

	public bool bTextMode;

	private float fValue;

	private float fValueLast = -1f;

	private string strValue = string.Empty;

	private string strValueLast;

	private int nState;

	private float fInitTime;

	public const int STATE_OFF = 0;

	public const int STATE_INIT = 1;

	public const int STATE_ON = 2;
}
