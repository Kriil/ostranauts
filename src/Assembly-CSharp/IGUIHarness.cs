using System;
using System.Collections.Generic;
using UnityEngine;

public class IGUIHarness : MonoBehaviour
{
	public void SaveAndClose()
	{
		if (this.bCleanedUp)
		{
			return;
		}
		GUIData component = base.GetComponent<GUIData>();
		if (component != null)
		{
			component.SaveAndClose();
			if (CrewSim.tplCurrentUI == null || CrewSim.tplCurrentUI.Item1 != "FFWD")
			{
				CondOwner coself = component.COSelf;
				if (coself != null)
				{
					Interaction interactionCurrent = coself.GetInteractionCurrent();
					if (interactionCurrent != null)
					{
						interactionCurrent.fDuration = 0.0;
						coself.SetTicker(interactionCurrent.strName, 0f);
					}
				}
			}
		}
		UnityEngine.Object.Destroy(base.gameObject);
		this.bCleanedUp = true;
		if (this.goUILeft != null)
		{
			this.goUILeft.GetComponent<IGUIHarness>().SaveAndClose();
		}
		if (this.goUIRight != null)
		{
			this.goUIRight.GetComponent<IGUIHarness>().SaveAndClose();
		}
		if (this.goUITop != null)
		{
			this.goUITop.GetComponent<IGUIHarness>().SaveAndClose();
		}
		if (this.goUIBottom != null)
		{
			this.goUIBottom.GetComponent<IGUIHarness>().SaveAndClose();
		}
	}

	public void SetGUIDir(CondOwner coSelf, string strGPMKey, string strDir)
	{
		if (coSelf == null)
		{
			Debug.Log("Cannot SetGUI on null CO.");
			return;
		}
		if (strGPMKey == null || strGPMKey == string.Empty)
		{
			Debug.Log(string.Concat(new object[]
			{
				"Cannot SetGUI on ",
				coSelf,
				"'s GUI named ",
				strGPMKey
			}));
			return;
		}
		if (!coSelf.mapGUIPropMaps.ContainsKey(strGPMKey))
		{
			Debug.Log(string.Concat(new object[]
			{
				"No such GUI Key found on ",
				coSelf,
				": ",
				strGPMKey
			}));
			return;
		}
		string @string = DataHandler.GetString("GUI_NAV_SWITCH", false);
		string text = string.Empty;
		GameObject gameObject;
		if (coSelf.mapGUIRefs.ContainsKey(strGPMKey) && coSelf.mapGUIRefs[strGPMKey] != null)
		{
			gameObject = coSelf.mapGUIRefs[strGPMKey].gameObject;
			text = gameObject.GetComponent<GUIData>().strFriendlyName;
		}
		else
		{
			Dictionary<string, string> dictionary = coSelf.mapGUIPropMaps[strGPMKey];
			GameObject gameObject2 = Resources.Load<GameObject>("GUIShip/" + dictionary["strGUIPrefab"]);
			if (gameObject2 == null)
			{
				Debug.Log("No such GUI prefab found: " + dictionary["strGUIPrefab"]);
				return;
			}
			gameObject = UnityEngine.Object.Instantiate<GameObject>(gameObject2);
			gameObject.transform.SetParent(base.transform.parent, false);
			GUIData component = gameObject.GetComponent<GUIData>();
			coSelf.mapGUIRefs[strGPMKey] = gameObject.GetComponent<IGUIHarness>();
			int num = dictionary["strGUIPrefab"].IndexOf('/');
			if (component == null && num >= 0)
			{
				string text2 = dictionary["strGUIPrefab"].Substring(num + 1);
			}
			component.Init(coSelf, dictionary, strGPMKey);
			text = component.strFriendlyName;
		}
		if (gameObject == null)
		{
			Debug.Log(string.Concat(new object[]
			{
				"Cannot SetGUI on ",
				coSelf,
				"'s GUI named ",
				strGPMKey
			}));
			return;
		}
		if (strDir != null)
		{
			if (!(strDir == "strGUIPrefabLeft"))
			{
				if (!(strDir == "strGUIPrefabRight"))
				{
					if (!(strDir == "strGUIPrefabTop"))
					{
						if (strDir == "strGUIPrefabBottom")
						{
							gameObject.GetComponent<Animator>().SetInteger("AnimState", 8);
							this.goUIBottom = gameObject;
						}
					}
					else
					{
						gameObject.GetComponent<Animator>().SetInteger("AnimState", 7);
						this.goUITop = gameObject;
					}
				}
				else
				{
					gameObject.GetComponent<Animator>().SetInteger("AnimState", 4);
					this.goUIRight = gameObject;
				}
			}
			else
			{
				gameObject.GetComponent<Animator>().SetInteger("AnimState", 3);
				this.goUILeft = gameObject;
			}
		}
	}

	public GameObject GoDir(string strDirection)
	{
		GameObject gameObject = base.gameObject;
		int integer = base.gameObject.GetComponent<Animator>().GetInteger("AnimState");
		if (strDirection != null)
		{
			if (!(strDirection == "strGUIPrefabLeft"))
			{
				if (!(strDirection == "strGUIPrefabRight"))
				{
					if (!(strDirection == "strGUIPrefabTop"))
					{
						if (strDirection == "strGUIPrefabBottom")
						{
							if ((integer == 5 || integer == 0) && this.goUIBottom != null)
							{
								gameObject.GetComponent<GUIData>().bActive = false;
								this.goUIBottom.GetComponent<Animator>().SetInteger("AnimState", 0);
								base.GetComponent<Animator>().SetInteger("AnimState", 11);
								gameObject = this.goUIBottom;
							}
						}
					}
					else if ((integer == 5 || integer == 0) && this.goUITop != null)
					{
						gameObject.GetComponent<GUIData>().bActive = false;
						this.goUITop.GetComponent<Animator>().SetInteger("AnimState", 0);
						base.GetComponent<Animator>().SetInteger("AnimState", 12);
						gameObject = this.goUITop;
					}
				}
				else if ((integer == 5 || integer == 0) && this.goUIRight != null)
				{
					gameObject.GetComponent<GUIData>().bActive = false;
					this.goUIRight.GetComponent<Animator>().SetInteger("AnimState", 0);
					base.GetComponent<Animator>().SetInteger("AnimState", 1);
					gameObject = this.goUIRight;
				}
			}
			else if ((integer == 5 || integer == 0) && this.goUILeft != null)
			{
				gameObject.GetComponent<GUIData>().bActive = false;
				this.goUILeft.GetComponent<Animator>().SetInteger("AnimState", 0);
				base.GetComponent<Animator>().SetInteger("AnimState", 2);
				gameObject = this.goUILeft;
			}
		}
		gameObject.GetComponent<GUIData>().bActive = true;
		return gameObject;
	}

	public const int ANIM_CENTER = 5;

	public const int ANIM_LEFT = 3;

	public const int ANIM_RIGHT = 4;

	public const int ANIM_TOP = 7;

	public const int ANIM_BOTTOM = 8;

	public const int ANIM_LEFTOUT = 1;

	public const int ANIM_RIGHTOUT = 2;

	public const int ANIM_TOPOUT = 11;

	public const int ANIM_BOTTOMOUT = 12;

	public const int ANIM_LEFTIN = 0;

	public const int ANIM_RIGHTIN = 0;

	public const int ANIM_TOPIN = 0;

	public const int ANIM_BOTTOMIN = 0;

	public const string DIR_LEFT = "strGUIPrefabLeft";

	public const string DIR_RIGHT = "strGUIPrefabRight";

	public const string DIR_TOP = "strGUIPrefabTop";

	public const string DIR_BOTTOM = "strGUIPrefabBottom";

	public GameObject goUILeft;

	public GameObject goUIRight;

	public GameObject goUITop;

	public GameObject goUIBottom;

	private bool bCleanedUp;
}
