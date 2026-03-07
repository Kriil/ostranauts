using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIOneSheet : GUIData
{
	protected override void Awake()
	{
		base.Awake();
	}

	private void SetUI()
	{
		string strFileName = null;
		if (this.GetPropMap().TryGetValue("strPNG", out strFileName))
		{
			Transform transform = base.transform.Find("Viewport/RawImage");
			RawImage component = transform.GetComponent<RawImage>();
			component.texture = DataHandler.LoadPNG(strFileName, false, false);
			component.texture.filterMode = FilterMode.Bilinear;
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.SetUI();
	}
}
