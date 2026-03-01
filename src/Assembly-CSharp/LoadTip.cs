using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadTip : MonoBehaviour
{
	public void AssignTip()
	{
		if (this.txtTip == null)
		{
			return;
		}
		if (this.strDefaultTips.Count == 0)
		{
			return;
		}
		int index = UnityEngine.Random.Range(0, this.strDefaultTips.Count);
		this.txtTip.text = this.strDefaultTips[index];
		base.StartCoroutine(this.FadeIn());
	}

	private IEnumerator FadeIn()
	{
		this.actualTime = 0f;
		yield return null;
		this.actualTime += Time.unscaledDeltaTime;
		JsonTip newTip = DataHandler.GetTip();
		for (int i = 0; i < this.newTipAttempts; i++)
		{
			if (newTip != null && !(newTip.strCategory == LoadTipTracker.strCategory) && !(newTip.strBody == string.Empty))
			{
				break;
			}
			newTip = DataHandler.GetTip();
		}
		if (newTip != null && newTip.strBody != string.Empty)
		{
			this.txtTip.text = newTip.strBody;
			LoadTipTracker.strCategory = newTip.strCategory;
		}
		yield return null;
		if (this.actualTime > this.waitTime)
		{
			this.actualTime = 0f;
			this.waitTime = 0f;
		}
		do
		{
			this.actualTime += Time.unscaledDeltaTime;
			this.cgSelf.alpha = Mathf.Clamp01((this.actualTime - this.waitTime) / this.fadeTime);
			yield return null;
		}
		while (this.actualTime < this.fadeTime + this.waitTime);
		this.cgSelf.alpha = 1f;
		yield return null;
		yield break;
	}

	private List<string> strDefaultTips = new List<string>
	{
		"Lore Snippet:\n\nSet in 2079!",
		"Gameplay Tip:\n\nGas values only show at the cursor when they are at dangerours levels.\n\nPressing 'G' toggles on gas values all the time.",
		"Gameplay Tip:\n\nWater pouches can be refilled at sinks.\n\nSelect the sink whilst holding an empty pouch to get an option to dispense water.",
		"Community Tip:\n\nEven the best players ask for help sometimes.\n\n Blue Bottle Games' social channels are open for players new and old alike!",
		"Gameplay Tip:\n\nMost tools brands have an associated charger.\n\nBatteries recharge over time when left in the inventory of the charger."
	};

	[SerializeField]
	private TextMeshProUGUI txtTip;

	[SerializeField]
	private CanvasGroup cgSelf;

	[SerializeField]
	private float fadeTime = 2f;

	[SerializeField]
	private float waitTime = 0.5f;

	[SerializeField]
	private int newTipAttempts = 10;

	[SerializeField]
	private float actualTime;
}
