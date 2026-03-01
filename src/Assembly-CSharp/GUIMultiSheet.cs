using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GUIMultiSheet : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.cg = base.GetComponent<CanvasGroup>();
		Button component = base.transform.Find("btnPageL").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.PrevPage();
		});
		this.bmpPageL = component.transform.Find("bmp").GetComponent<RawImage>();
		this.cgL = component.GetComponent<CanvasGroup>();
		component = base.transform.Find("btnPageR").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.NextPage();
		});
		this.bmpPageR = component.transform.Find("bmp").GetComponent<RawImage>();
		this.cgR = component.GetComponent<CanvasGroup>();
	}

	private void SetPage(int nPage)
	{
		if (nPage % 2 == 0)
		{
			nPage--;
		}
		string text = null;
		if (nPage - this.nIndex > 1)
		{
			text = "ShipUIPaperRustle01";
		}
		else if (nPage - this.nIndex < -1)
		{
			text = "ShipUIPaperRustle02";
		}
		this.nIndex = -1;
		if (nPage >= 0 && this.aPages.Count > nPage)
		{
			CanvasManager.ShowCanvasGroup(this.cgL);
			this.nIndex = nPage;
			this.bmpPageL.texture = DataHandler.LoadPNG(this.aPages[nPage], false, false);
			this.bmpPageL.texture.filterMode = FilterMode.Bilinear;
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgL);
		}
		nPage++;
		if (nPage >= 0 && this.aPages.Count > nPage)
		{
			CanvasManager.ShowCanvasGroup(this.cgR);
			this.nIndex = nPage - 1;
			this.bmpPageR.texture = DataHandler.LoadPNG(this.aPages[nPage], false, false);
			this.bmpPageR.texture.filterMode = FilterMode.Bilinear;
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgR);
		}
		if (text != null)
		{
			AudioManager.am.PlayAudioEmitter(text, false, false);
			AudioManager.am.TweakAudioEmitter(text, 0.85f - UnityEngine.Random.Range(0f, 0.15f), 1f);
		}
	}

	private void NextPage()
	{
		if (this.nIndex >= this.aPages.Count)
		{
			return;
		}
		this.SetPage(this.nIndex + 2);
	}

	private void PrevPage()
	{
		if (this.nIndex < -1)
		{
			return;
		}
		this.SetPage(this.nIndex - 2);
	}

	private void SetUI()
	{
		string b = null;
		if (!this.GetPropMap().TryGetValue("strManual", out b))
		{
			return;
		}
		this.aPages = new List<string>();
		for (int i = 0; i < DataHandler.dictManPages["Manual Pages"].Length - 1; i += 2)
		{
			string a = DataHandler.dictManPages["Manual Pages"][i];
			if (!(a != b))
			{
				string str = DataHandler.dictManPages["Manual Pages"][i + 1];
				string[] files = Directory.GetFiles(DataHandler.strAssetPath + "images/manuals/" + str, "*.png");
				foreach (string path in files)
				{
					DirectoryInfo directoryInfo = new DirectoryInfo(path);
					this.aPages.Add("manuals/" + directoryInfo.Parent.Name + "/" + directoryInfo.Name);
				}
			}
		}
		this.SetPage(0);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.SetUI();
	}

	private CanvasGroup cg;

	private CanvasGroup cgL;

	private CanvasGroup cgR;

	private RawImage bmpPageL;

	private RawImage bmpPageR;

	private List<string> aPages;

	private int nIndex = -1;
}
