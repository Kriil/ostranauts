using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIMeter : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.tfPanelIn = base.transform.Find("pnlInside/pnlInsideScrews");
		GameObject original = Resources.Load<GameObject>("GUIShip/GUIInputScrew");
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfPanelIn);
		Text componentInChildren = gameObject.GetComponentInChildren<Text>();
		componentInChildren.text = "Input 1";
		this.tfPanelIn.parent.gameObject.SetActive(false);
		this.fMax = 1f;
		this.fMin = 0f;
		gameObject.GetComponentInChildren<Button>().onClick.AddListener(delegate()
		{
			this.ChooseInput("strInput01");
		});
		this.imgConduit01 = gameObject.transform.Find("bmp").GetComponent<Image>();
		Texture2D texture2D = DataHandler.LoadPNG("missing.png", false, false);
		this.imgConduit01.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
		this.imgConduit01.transform.Rotate(Vector3.forward, 90f);
		this.dictPropMap = new Dictionary<string, string>
		{
			{
				"strGUIPrefab",
				"GUIMeter"
			},
			{
				"strTitle",
				"Meter"
			},
			{
				"strCond",
				string.Empty
			},
			{
				"strInput01",
				string.Empty
			},
			{
				"fMax",
				"1.0"
			},
			{
				"fMin",
				"0.0"
			}
		};
		this.lblTitle = base.transform.Find("lblTitle").GetComponent<Text>();
		this.bmpMeter = base.transform.Find("bmpMeter").GetComponent<Image>();
		base.transform.Find("btnScrew01").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("btnScrew02").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("btnScrew03").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("btnScrew04").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.OpenPanel();
		});
		base.transform.Find("pnlInside/btnDone").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.ClosePanel();
		});
	}

	private void Update()
	{
		float num = 0f;
		float num2 = this.fMax - this.fMin;
		if (num2 <= 0f)
		{
			num2 = 1f;
		}
		if (this.coTarget01 != null)
		{
			num = Convert.ToSingle(this.coTarget01.GetCondAmount(this.strCond01) - (double)this.fMin) / num2;
		}
		if (num < 0f)
		{
			num = 0f;
		}
		else if (num > 1f)
		{
			num = 1f;
		}
		this.bmpMeter.transform.localScale = new Vector3(1f, num, 1f);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this.lblTitle.text = this.dictPropMap["strTitle"];
		this.strCond01 = this.dictPropMap["strCond"];
		Ship ship = coSelf.ship;
		Renderer component = coSelf.gameObject.GetComponent<Renderer>();
		Vector2 vPos = new Vector2(component.bounds.min.x + 0.5f, component.bounds.max.y - 0.5f);
		int tileIndexAtWorldCoords = ship.GetTileIndexAtWorldCoords1(vPos);
		int num = tileIndexAtWorldCoords / ship.nCols;
		int num2 = tileIndexAtWorldCoords % ship.nCols;
		Item component2 = coSelf.GetComponent<Item>();
		this.fMax = float.Parse(this.dictPropMap["fMax"]);
		this.fMin = float.Parse(this.dictPropMap["fMin"]);
		this.SetInput(coSelf);
	}

	public override void SetInput(CondOwner co)
	{
		base.SetInput(co);
		this.coTarget01 = co;
	}

	private void OpenPanel()
	{
		this.tfPanelIn.parent.gameObject.SetActive(true);
	}

	private void ClosePanel()
	{
		this.tfPanelIn.parent.gameObject.SetActive(false);
	}

	private void ChooseInput(string strInputName)
	{
	}

	public Text lblTitle;

	public Image bmpMeter;

	public Transform tfPanelIn;

	public string strCond01;

	private Image imgConduit01;

	private CondOwner coTarget01;

	public float fMax;

	public float fMin;
}
