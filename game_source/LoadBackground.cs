using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadBackground : MonoBehaviour
{
	public void AssignBackground()
	{
		if (this.imgBackground == null)
		{
			return;
		}
		if (this.imgVariants.Count == 0)
		{
			return;
		}
		int index = UnityEngine.Random.Range(0, this.imgVariants.Count);
		this.imgBackground.sprite = this.imgVariants[index];
		if (this.txtAttribution != null)
		{
			this.txtAttribution.text = this.strAttribution[index];
		}
	}

	[SerializeField]
	private List<Sprite> imgVariants;

	[SerializeField]
	private List<string> strAttribution;

	[SerializeField]
	private Image imgBackground;

	[SerializeField]
	private TextMeshProUGUI txtAttribution;
}
