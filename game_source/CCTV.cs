using System;
using TMPro;
using UnityEngine;

public class CCTV : MonoBehaviour
{
	private void Awake()
	{
		this.txt = base.GetComponent<TMP_Text>();
		this.rt = base.GetComponent<RectTransform>();
		this.rtParent = this.rt.parent;
		this.txt.text = string.Empty;
		Vector3 localScale = this.rtParent.localScale;
		this.rt.localScale = new Vector3(1f / localScale.x, 1f / localScale.y, 1f / localScale.z);
	}

	private void Update()
	{
		this.fTimeLast += CrewSim.TimeElapsedScaled();
		this.fTimeLastPause += CrewSim.TimeElapsedScaled();
		if (this.fTimeLast >= this.fDelay)
		{
			if (this.nIndex >= this.strText.Length)
			{
				string @string = DataHandler.GetString("NEWS_SUFFIX", false);
				string a = string.Empty;
				this.strText = string.Empty;
				int num = MathUtils.Rand(0, 4, MathUtils.RandType.Flat, null);
				for (int i = 0; i < num; i++)
				{
					JsonHeadline headline = DataHandler.GetHeadline();
					if (a != headline.strRegion)
					{
						this.strText = this.strText + headline.strRegion + @string;
						a = headline.strRegion;
					}
					this.strText = this.strText + headline.strDesc + "\n";
				}
				this.strText += DataHandler.GetString("ADS_PREFIX", false);
				num = MathUtils.Rand(1, 3, MathUtils.RandType.Flat, null);
				for (int j = 0; j < num; j++)
				{
					JsonAd ad = DataHandler.GetAd();
					this.strText = this.strText + ad.strDesc + "\n";
				}
				a = string.Empty;
				num = MathUtils.Rand(0, 4, MathUtils.RandType.Flat, null);
				for (int k = 0; k < num; k++)
				{
					JsonHeadline headline2 = DataHandler.GetHeadline();
					if (a != headline2.strRegion)
					{
						this.strText = this.strText + headline2.strRegion + @string;
						a = headline2.strRegion;
					}
					this.strText = this.strText + headline2.strDesc + "\n";
				}
				this.nIndex = 0;
				this.strMiddle = string.Empty;
			}
			if (this.txt.isTextTruncated)
			{
				int characterCount = this.txt.textInfo.lineInfo[0].characterCount;
				this.strMiddle = this.strMiddle.Substring(characterCount);
			}
			char c = this.strText[this.nIndex];
			this.strMiddle += c;
			this.txt.text = this.strBegin + this.strMiddle + this.strEnd;
			float num2 = this.fTimeLast * MathUtils.Rand(0.3f, 3f, MathUtils.RandType.Flat, null);
			if (c == '.' || c == '\n')
			{
				num2 += 20f * this.fDelay;
			}
			else if (c == ',')
			{
				num2 += 10f * this.fDelay;
			}
			if (this.fTimeLastPause > this.fDelayPause)
			{
				num2 *= 15f;
				this.fTimeLastPause = 0f;
			}
			this.fTimeLast -= num2;
			this.nIndex++;
		}
		this.rt.eulerAngles = new Vector3(0f, 0f, 0f);
		Vector3 localScale = this.rtParent.localScale;
		float z = this.rtParent.rotation.eulerAngles.z;
		bool flag = Mathf.Abs(z) >= 5f && Mathf.Abs(180f - z) >= 5f;
		if (flag)
		{
			this.rt.localScale = new Vector3(1f / localScale.y, 1f / localScale.x, 1f / localScale.z);
		}
		else
		{
			this.rt.localScale = new Vector3(1f / localScale.x, 1f / localScale.y, 1f / localScale.z);
		}
		Vector3 position = this.rtParent.GetComponent<CondOwner>().GetPos("cctv", false);
		this.rt.position = position;
	}

	private string strText = string.Empty;

	private string strBegin = string.Empty;

	private string strMiddle = string.Empty;

	private string strEnd = string.Empty;

	private float fDelay = 0.005f;

	private float fDelayPause = 0.5f;

	private float fTimeLast;

	private float fTimeLastPause;

	private TMP_Text txt;

	private RectTransform rt;

	private Transform rtParent;

	private int nIndex;
}
