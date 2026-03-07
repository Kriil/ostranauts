using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIToggle4xInput
{
	public GUIToggle4xInput(string strSuffix, Transform tfPanelIn, GUIToggle4x gui)
	{
		GUIToggle4xInput $this = this;
		this.strName = "strInput0" + strSuffix;
		this.dictCOsInTile = new Dictionary<int, List<PairStringCO>>();
		GameObject original = Resources.Load<GameObject>("GUIShip/GUIInputScrewV");
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original);
		gameObject.GetComponentInChildren<Button>().onClick.AddListener(delegate()
		{
			$this.ChooseInput($this.strName, gui);
		});
		this.imgConduit = gameObject.transform.Find("bmp").GetComponent<Image>();
		Texture2D texture2D = DataHandler.LoadPNG("missing.png", false, false);
		this.imgConduit.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
		this.imgConduit.transform.Rotate(Vector3.forward, 90f);
		gameObject.transform.SetParent(tfPanelIn);
	}

	public void Init(CondOwner coSelf, string strConduit, string strIntAction)
	{
		this.SetInput(coSelf);
	}

	private void ChooseInput(string strInputName, GUIToggle4x gui)
	{
	}

	public void SetInput(CondOwner co)
	{
	}

	private List<PairStringCO> GetCOsByConduitName(string strConduit)
	{
		return new List<PairStringCO>();
	}

	public void Activate(CondOwner coSelf)
	{
		if (coSelf == null || this.coTarget == null)
		{
			return;
		}
		coSelf.QueueInteraction(this.coTarget, DataHandler.GetInteraction(this.strInteraction, null, false), true);
	}

	public string strName;

	public CondOwner coTarget;

	public string strInteraction;

	private Image imgConduit;

	private Dictionary<int, List<PairStringCO>> dictCOsInTile;
}
