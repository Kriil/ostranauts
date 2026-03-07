using System;
using Ostranauts.Core;
using Ostranauts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUISaveIndicator : MonoBehaviour
{
	public void Awake()
	{
	}

	public void Reset()
	{
		this._SaveIcon.sprite = this._NoSaveSprite;
	}

	public void EstablishSave(bool saved)
	{
		this._SaveTime.text = "Last Save:\n" + TimeUtils.FromUnixTimeSeconds(MonoSingleton<LoadManager>.Instance.LastSaveTimestamp).ToString();
		if (saved)
		{
			this._SaveIcon.sprite = this._SaveSprite;
		}
		else
		{
			double lastSaveTimestamp = MonoSingleton<LoadManager>.Instance.LastSaveTimestamp;
			if (lastSaveTimestamp <= 0.0)
			{
				this._SaveIcon.sprite = this._NoSaveSprite;
				this._SaveTime.text = "No Save";
			}
			else
			{
				double num = TimeUtils.GetCurrentEpochTimeSeconds() - lastSaveTimestamp;
				if (num < 30.0)
				{
					this._SaveIcon.sprite = this._SaveSprite;
				}
				else
				{
					this._SaveIcon.sprite = this._OldSaveSprite;
				}
			}
		}
	}

	public Sprite _OldSaveSprite;

	public Sprite _NoSaveSprite;

	public Sprite _SaveSprite;

	public TextMeshProUGUI _SaveTime;

	public Image _SaveIcon;
}
