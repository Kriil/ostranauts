using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIManual : MonoBehaviour
{
	private void Start()
	{
	}

	private void Init()
	{
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
		component = base.transform.Find("btnExit").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.Close();
		});
		this.aColors = new List<Color>();
		for (int i = 1; i < 13; i++)
		{
			this.aColors.Add(DataHandler.GetColor("BinderTab" + i.ToString("00")));
		}
		this.aPages = new List<string>();
		this.dictManuals = new Dictionary<string, int>();
		List<string> list = new List<string>();
		for (int j = 0; j < DataHandler.dictManPages["Manual Pages"].Length - 1; j += 2)
		{
			string text = DataHandler.dictManPages["Manual Pages"][j];
			string str = DataHandler.dictManPages["Manual Pages"][j + 1];
			this.dictManuals[text] = this.aPages.Count;
			list.Insert(0, text);
			string[] files = Directory.GetFiles(DataHandler.strAssetPath + "images/manuals/" + str, "*.png");
			foreach (string path in files)
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				this.aPages.Add("manuals/" + directoryInfo.Parent.Name + "/" + directoryInfo.Name);
			}
		}
		this.dictTabsRight = new Dictionary<string, GameObject>();
		this.dictTabsLeft = new Dictionary<string, GameObject>();
		int l;
		for (l = list.Count; l > 6; l -= 6)
		{
		}
		for (int m = 0; m < list.Count; m++)
		{
			string strName = list[m];
			GameObject gameObject = base.transform.Find("prefabBinderTabR0" + l).gameObject;
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject);
			gameObject2.transform.SetParent(base.transform);
			gameObject2.transform.Find("txt").GetComponent<TMP_Text>().text = strName;
			gameObject2.transform.Find("pnlName").GetComponent<Button>().onClick.AddListener(delegate()
			{
				this.SetPage(strName);
			});
			gameObject2.transform.Find("pnlName").GetComponent<Image>().color = this.aColors[this.nColorIndex];
			this.CopyRectTransform(gameObject, gameObject2);
			this.dictTabsRight[strName] = gameObject2;
			gameObject = base.transform.Find("prefabBinderTabL0" + l).gameObject;
			gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject);
			gameObject2.transform.SetParent(base.transform);
			gameObject2.transform.SetSiblingIndex(gameObject2.transform.GetSiblingIndex() - 2);
			gameObject2.transform.Find("txt").GetComponent<TMP_Text>().text = strName;
			gameObject2.transform.Find("pnlName").GetComponent<Button>().onClick.AddListener(delegate()
			{
				this.SetPage(strName);
			});
			gameObject2.transform.Find("pnlName").GetComponent<Image>().color = this.aColors[this.nColorIndex];
			this.CopyRectTransform(gameObject, gameObject2);
			this.dictTabsLeft[strName] = gameObject2;
			this.nColorIndex++;
			if (this.nColorIndex >= this.aColors.Count)
			{
				this.nColorIndex = 0;
			}
			l--;
			if (l < 1)
			{
				l = 6;
			}
		}
		for (int n = 1; n < 7; n++)
		{
			GameObject gameObject3 = base.transform.Find("prefabBinderTabR0" + n).gameObject;
			gameObject3.SetActive(false);
			gameObject3 = base.transform.Find("prefabBinderTabL0" + n).gameObject;
			gameObject3.SetActive(false);
		}
		this.SetPage(0);
		this.bInit = true;
	}

	private void CopyRectTransform(GameObject objFrom, GameObject objTo)
	{
		RectTransform component = objFrom.GetComponent<RectTransform>();
		RectTransform component2 = objTo.GetComponent<RectTransform>();
		component2.anchorMin = component.anchorMin;
		component2.anchorMax = component.anchorMax;
		component2.anchoredPosition = component.anchoredPosition;
		component2.sizeDelta = component.sizeDelta;
		Vector3 one = Vector3.one;
		if (component2.localScale.x < 0f)
		{
			one.x = -1f;
		}
		component2.localScale = one;
		CanvasManager.SetAnchorsToCorners(component2);
	}

	private void Update()
	{
		if (!DataHandler.bLoaded)
		{
			return;
		}
		if (!this.bInit)
		{
			this.Init();
		}
		this.KeyHandler();
	}

	private void SetPage(string strName)
	{
		int num = this.dictManuals[strName];
		if (num == this.nIndex || num == this.nIndex + 1)
		{
			if (this.dictTabsRight[strName].activeInHierarchy)
			{
				this.NextPage();
			}
			else if (this.dictTabsLeft[strName].activeInHierarchy)
			{
				this.PrevPage();
			}
		}
		else
		{
			this.SetPage(num);
		}
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
		foreach (string key in this.dictManuals.Keys)
		{
			this.dictTabsRight[key].SetActive(this.dictManuals[key] >= this.nIndex + 1);
			this.dictTabsLeft[key].SetActive(this.dictManuals[key] < this.nIndex + 1);
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

	public void Close()
	{
		this.cg.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, false, true);
		this.cg.interactable = false;
		this.cg.blocksRaycasts = false;
	}

	private void KeyHandler()
	{
		if (GUIActionKeySelector.commandEscape.Down && this.cg.alpha == 1f)
		{
			this.Close();
		}
	}

	private CanvasGroup cg;

	private CanvasGroup cgL;

	private CanvasGroup cgR;

	private RawImage bmpPageL;

	private RawImage bmpPageR;

	private Dictionary<string, int> dictManuals;

	private List<Color> aColors;

	private Dictionary<string, GameObject> dictTabsRight;

	private Dictionary<string, GameObject> dictTabsLeft;

	private List<string> aPages;

	private int nIndex = -1;

	private int nColorIndex;

	private const int NUMTABS = 6;

	private bool bInit;
}
