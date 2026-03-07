using System;
using System.Collections.Generic;
using Ostranauts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Character creation appearance/body editor.
// This screen lets the player randomize or edit face parts, pronouns, name,
// and other presentation details before confirming the crew member.
public class GUIChargenBody : GUIData
{
	// Wires up the chargen controls for appearance, pronouns, and text input.
	protected override void Awake()
	{
		base.Awake();
		this.btnRandFace = base.transform.Find("bmpPDABG/btnAppearance").GetComponent<Button>();
		this.btnRandFace.onClick.AddListener(delegate()
		{
			this.ChangeAppearance();
		});
		AudioManager.AddBtnAudio(this.btnRandFace.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnRandName = base.transform.Find("bmpPDABG/btnRandName").GetComponent<Button>();
		this.btnRandName.onClick.AddListener(delegate()
		{
			this.RandName();
		});
		AudioManager.AddBtnAudio(this.btnRandName.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.chkHe = base.transform.Find("bmpPDABG/chkHe").GetComponent<Toggle>();
		this.chkHe.onValueChanged.AddListener(delegate(bool value)
		{
			this.ChangePronoun("IsMale", value);
		});
		AudioManager.AddBtnAudio(this.chkHe.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppDelete");
		this.chkShe = base.transform.Find("bmpPDABG/chkShe").GetComponent<Toggle>();
		this.chkShe.onValueChanged.AddListener(delegate(bool value)
		{
			this.ChangePronoun("IsFemale", value);
		});
		AudioManager.AddBtnAudio(this.chkShe.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppDelete");
		this.chkThey = base.transform.Find("bmpPDABG/chkThey").GetComponent<Toggle>();
		this.chkThey.onValueChanged.AddListener(delegate(bool value)
		{
			this.ChangePronoun("IsNB", value);
		});
		AudioManager.AddBtnAudio(this.chkThey.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppDelete");
		this.chkSeekingHe = base.transform.Find("bmpPDABG/chkSeekingHe/chkSeeking").GetComponent<Toggle>();
		this.chkSeekingHe.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnSeekingChanged();
		});
		AudioManager.AddBtnAudio(this.chkHe.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppDelete");
		this.chkSeekingShe = base.transform.Find("bmpPDABG/chkSeekingShe/chkSeeking").GetComponent<Toggle>();
		this.chkSeekingShe.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnSeekingChanged();
		});
		AudioManager.AddBtnAudio(this.chkHe.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppDelete");
		this.chkSeekingThey = base.transform.Find("bmpPDABG/chkSeekingThey/chkSeeking").GetComponent<Toggle>();
		this.chkSeekingThey.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.OnSeekingChanged();
		});
		AudioManager.AddBtnAudio(this.chkHe.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppDelete");
		this.tboxName = base.transform.Find("bmpPDABG/tboxName").GetComponent<TMP_InputField>();
		this.tboxName.onValueChanged.AddListener(delegate(string value)
		{
			this.ChangeName(value);
		});
		this.tboxName.onSelect.AddListener(delegate(string A_0)
		{
			CrewSim.Typing = true;
		});
		this.tboxName.onDeselect.AddListener(delegate(string A_0)
		{
			CrewSim.Typing = false;
		});
		this.btnDone = base.transform.Find("bmpPDABG/btnDone").GetComponent<Button>();
		this.btnDone.onClick.AddListener(delegate()
		{
			this.Done();
		});
		AudioManager.AddBtnAudio(this.btnDone.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppDone");
		this.bmpHand = base.transform.Find("pnlHandMask/bmpHand").GetComponent<RawImage>();
		this.txtEyesNum = base.transform.Find("pnlFaceBtns/pnlEyesNum/txt").GetComponent<TMP_Text>();
		this.txtGlassesNum = base.transform.Find("pnlFaceBtns/pnlGlassesNum/txt").GetComponent<TMP_Text>();
		this.txtSkinNum = base.transform.Find("pnlFaceBtns/pnlSkinNum/txt").GetComponent<TMP_Text>();
		this.txtHeadNum = base.transform.Find("pnlFaceBtns/pnlHeadNum/txt").GetComponent<TMP_Text>();
		this.txtLipsNum = base.transform.Find("pnlFaceBtns/pnlLipsNum/txt").GetComponent<TMP_Text>();
		this.txtNeckNum = base.transform.Find("pnlFaceBtns/pnlNeckNum/txt").GetComponent<TMP_Text>();
		this.txtNoseNum = base.transform.Find("pnlFaceBtns/pnlNoseNum/txt").GetComponent<TMP_Text>();
		this.txtPupilsNum = base.transform.Find("pnlFaceBtns/pnlPupilsNum/txt").GetComponent<TMP_Text>();
		this.txtScarNum = base.transform.Find("pnlFaceBtns/pnlScarNum/txt").GetComponent<TMP_Text>();
		this.txtTeethNum = base.transform.Find("pnlFaceBtns/pnlTeethNum/txt").GetComponent<TMP_Text>();
		this.txtHairNum = base.transform.Find("pnlFaceBtns/pnlHairNum/txt").GetComponent<TMP_Text>();
		this.txtBeardNum = base.transform.Find("pnlFaceBtns/pnlBeardNum/txt").GetComponent<TMP_Text>();
		this.txtBodyNum = base.transform.Find("pnlFaceBtns/pnlBodyNum/txt").GetComponent<TMP_Text>();
		this.aTxtNums = new TMP_Text[]
		{
			this.txtEyesNum,
			this.txtGlassesNum,
			this.txtHeadNum,
			this.txtLipsNum,
			this.txtNeckNum,
			this.txtNoseNum,
			this.txtPupilsNum,
			this.txtScarNum,
			this.txtTeethNum,
			this.txtHairNum,
			this.txtBeardNum
		};
		this.btnEyes = base.transform.Find("pnlFaceBtns/btnEyesNext").GetComponent<Button>();
		this.btnEyes.onClick.AddListener(delegate()
		{
			this.FacePartNext(0, 1);
		});
		AudioManager.AddBtnAudio(this.btnEyes.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnGlasses = base.transform.Find("pnlFaceBtns/btnGlassesNext").GetComponent<Button>();
		this.btnGlasses.onClick.AddListener(delegate()
		{
			this.FacePartNext(1, 1);
		});
		AudioManager.AddBtnAudio(this.btnGlasses.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnSkin = base.transform.Find("pnlFaceBtns/btnSkinNext").GetComponent<Button>();
		this.btnSkin.onClick.AddListener(delegate()
		{
			this.ChangeSkin(1);
		});
		AudioManager.AddBtnAudio(this.btnSkin.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnHead = base.transform.Find("pnlFaceBtns/btnHeadNext").GetComponent<Button>();
		this.btnHead.onClick.AddListener(delegate()
		{
			this.FacePartNext(2, 1);
		});
		AudioManager.AddBtnAudio(this.btnHead.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnLips = base.transform.Find("pnlFaceBtns/btnLipsNext").GetComponent<Button>();
		this.btnLips.onClick.AddListener(delegate()
		{
			this.FacePartNext(3, 1);
		});
		AudioManager.AddBtnAudio(this.btnLips.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnNeck = base.transform.Find("pnlFaceBtns/btnNeckNext").GetComponent<Button>();
		this.btnNeck.onClick.AddListener(delegate()
		{
			this.FacePartNext(4, 1);
		});
		AudioManager.AddBtnAudio(this.btnNeck.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnNose = base.transform.Find("pnlFaceBtns/btnNoseNext").GetComponent<Button>();
		this.btnNose.onClick.AddListener(delegate()
		{
			this.FacePartNext(5, 1);
		});
		AudioManager.AddBtnAudio(this.btnNose.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnPupils = base.transform.Find("pnlFaceBtns/btnPupilsNext").GetComponent<Button>();
		this.btnPupils.onClick.AddListener(delegate()
		{
			this.FacePartNext(6, 1);
		});
		AudioManager.AddBtnAudio(this.btnPupils.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnScar = base.transform.Find("pnlFaceBtns/btnScarNext").GetComponent<Button>();
		this.btnScar.onClick.AddListener(delegate()
		{
			this.FacePartNext(7, 1);
		});
		AudioManager.AddBtnAudio(this.btnScar.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnTeeth = base.transform.Find("pnlFaceBtns/btnTeethNext").GetComponent<Button>();
		this.btnTeeth.onClick.AddListener(delegate()
		{
			this.FacePartNext(8, 1);
		});
		AudioManager.AddBtnAudio(this.btnTeeth.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnHair = base.transform.Find("pnlFaceBtns/btnHairNext").GetComponent<Button>();
		this.btnHair.onClick.AddListener(delegate()
		{
			this.FacePartNext(9, 1);
		});
		AudioManager.AddBtnAudio(this.btnHair.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnBeard = base.transform.Find("pnlFaceBtns/btnBeardNext").GetComponent<Button>();
		this.btnBeard.onClick.AddListener(delegate()
		{
			this.FacePartNext(10, 1);
		});
		AudioManager.AddBtnAudio(this.btnBeard.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnBody = base.transform.Find("pnlFaceBtns/btnBodyNext").GetComponent<Button>();
		this.btnBody.onClick.AddListener(delegate()
		{
			this.BodyNext(1);
		});
		AudioManager.AddBtnAudio(this.btnBody.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnEyesBack = base.transform.Find("pnlFaceBtns/btnEyesBack").GetComponent<Button>();
		this.btnEyesBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(0, -1);
		});
		AudioManager.AddBtnAudio(this.btnEyesBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnGlassesBack = base.transform.Find("pnlFaceBtns/btnGlassesBack").GetComponent<Button>();
		this.btnGlassesBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(1, -1);
		});
		AudioManager.AddBtnAudio(this.btnGlassesBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnSkinBack = base.transform.Find("pnlFaceBtns/btnSkinBack").GetComponent<Button>();
		this.btnSkinBack.onClick.AddListener(delegate()
		{
			this.ChangeSkin(-1);
		});
		AudioManager.AddBtnAudio(this.btnSkinBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnHeadBack = base.transform.Find("pnlFaceBtns/btnHeadBack").GetComponent<Button>();
		this.btnHeadBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(2, -1);
		});
		AudioManager.AddBtnAudio(this.btnHeadBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnLipsBack = base.transform.Find("pnlFaceBtns/btnLipsBack").GetComponent<Button>();
		this.btnLipsBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(3, -1);
		});
		AudioManager.AddBtnAudio(this.btnLipsBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnNeckBack = base.transform.Find("pnlFaceBtns/btnNeckBack").GetComponent<Button>();
		this.btnNeckBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(4, -1);
		});
		AudioManager.AddBtnAudio(this.btnNeckBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnNoseBack = base.transform.Find("pnlFaceBtns/btnNoseBack").GetComponent<Button>();
		this.btnNoseBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(5, -1);
		});
		AudioManager.AddBtnAudio(this.btnNoseBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnPupilsBack = base.transform.Find("pnlFaceBtns/btnPupilsBack").GetComponent<Button>();
		this.btnPupilsBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(6, -1);
		});
		AudioManager.AddBtnAudio(this.btnPupilsBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnScarBack = base.transform.Find("pnlFaceBtns/btnScarBack").GetComponent<Button>();
		this.btnScarBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(7, -1);
		});
		AudioManager.AddBtnAudio(this.btnScarBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnTeethBack = base.transform.Find("pnlFaceBtns/btnTeethBack").GetComponent<Button>();
		this.btnTeethBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(8, -1);
		});
		AudioManager.AddBtnAudio(this.btnTeethBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnHairBack = base.transform.Find("pnlFaceBtns/btnHairBack").GetComponent<Button>();
		this.btnHairBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(9, -1);
		});
		AudioManager.AddBtnAudio(this.btnHairBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnBeardBack = base.transform.Find("pnlFaceBtns/btnBeardBack").GetComponent<Button>();
		this.btnBeardBack.onClick.AddListener(delegate()
		{
			this.FacePartNext(10, -1);
		});
		AudioManager.AddBtnAudio(this.btnBeardBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this.btnBodyBack = base.transform.Find("pnlFaceBtns/btnBodyBack").GetComponent<Button>();
		this.btnBodyBack.onClick.AddListener(delegate()
		{
			this.BodyNext(-1);
		});
		AudioManager.AddBtnAudio(this.btnBodyBack.gameObject, "ShipUIBtnDatingAppButton", "ShipUIBtnDatingAppRandomize");
		this._paperDollManager.ctFilter = DataHandler.GetCondTrigger("TIsBodyPart");
		this.ToggleBodyView(false);
		this.chkBodyHead.onValueChanged.AddListener(delegate(bool value)
		{
			this.ToggleBodyView(value);
		});
		Loot loot = DataHandler.GetLoot("TXTBodyTypesAll");
		this.aBodyTypes = loot.GetLootNames(null, false, null);
		this.dictSeekings = new Dictionary<Toggle, string>();
		this.dictSeekings[this.chkSeekingHe] = "IsAttractedMen";
		this.dictSeekings[this.chkSeekingShe] = "IsAttractedWomen";
		this.dictSeekings[this.chkSeekingThey] = "IsAttractedNB";
	}

	private void Update()
	{
		if (this.bRestoreFaceX)
		{
			this.MoveFaceWithMouse();
		}
		this.KeyHandler();
	}

	private void MoveFaceWithMouse()
	{
		Vector3 vector = new Vector3(Input.mousePosition.x / (float)Screen.width, Input.mousePosition.y / (float)Screen.height, 0f);
		vector.x -= 0.5f;
		vector.y -= 0.5f;
		float num = 0.1f;
		vector *= num;
		vector.x = Mathf.Clamp(vector.x, -num / 2f, num / 2f);
		vector.y = Mathf.Clamp(vector.y, -num / 2f, num / 2f);
		if (this.cgPortrait.alpha > 0f)
		{
			MonoSingleton<GUIRenderTargets>.Instance.SetTransform(this.coUser, new Vector3?(vector));
		}
		Vector3 position = this._paperDollManager.transform.position;
		Transform transform = this._paperDollManager.transform;
		float num2 = this.fOffsetX;
		Vector3 vector2 = vector;
		transform.position = new Vector3(num2 + vector2.x * this.fOffsetCoeff, position.y, position.z);
	}

	private void Done()
	{
		if (this.coUser.HasCond("IsInChargen"))
		{
			CrewSim.SwitchUI("strGUIPrefabRight");
		}
		else
		{
			CrewSim.LowerUI(false);
		}
	}

	private void GetCOInfo()
	{
		this.bIgnoreEvents = true;
		this.coUser = this.COSelf.GetInteractionCurrent().objThem;
		this.crew = this.coUser.GetComponent<Crew>();
		this.UpdatePartList();
		Texture texture = MonoSingleton<GUIRenderTargets>.Instance.CreatePortrait(this.coUser);
		if (this._bmpPortrait != null)
		{
			this._bmpPortrait.texture = texture;
		}
		if (this._bmpPdaPortrait != null)
		{
			this._bmpPdaPortrait.texture = texture;
		}
		CrewSim.SetToggleWithoutNotify(this.chkHe, this.coUser.HasCond("IsMale"));
		CrewSim.SetToggleWithoutNotify(this.chkShe, this.coUser.HasCond("IsFemale"));
		CrewSim.SetToggleWithoutNotify(this.chkThey, this.coUser.HasCond("IsNB"));
		if (this.coUser.HasCond("IsInChargen"))
		{
			CrewSim.Paused = true;
			CrewSim.bPauseLock = true;
			MonoSingleton<GUIRenderTargets>.Instance.SetFace(this.coUser, false);
			this.tboxName.text = this.coUser.strName;
			this.GetHand();
		}
		else
		{
			MonoSingleton<GUIRenderTargets>.Instance.SetFace(this.coUser, false);
			this.tboxName.text = this.coUser.strName;
			this.GetHand();
			this.btnRandFace.gameObject.SetActive(false);
			this.btnRandName.gameObject.SetActive(false);
			this.tboxName.interactable = false;
			this.btnEyes.gameObject.SetActive(false);
			this.btnSkin.gameObject.SetActive(false);
			this.btnLips.gameObject.SetActive(false);
			this.btnNeck.gameObject.SetActive(false);
			this.btnNose.gameObject.SetActive(false);
			this.btnPupils.gameObject.SetActive(false);
			this.btnTeeth.gameObject.SetActive(false);
			this.btnHead.gameObject.SetActive(false);
			this.btnBody.gameObject.SetActive(false);
			this.btnEyesBack.gameObject.SetActive(false);
			this.btnSkinBack.gameObject.SetActive(false);
			this.btnLipsBack.gameObject.SetActive(false);
			this.btnNeckBack.gameObject.SetActive(false);
			this.btnNoseBack.gameObject.SetActive(false);
			this.btnPupilsBack.gameObject.SetActive(false);
			this.btnTeethBack.gameObject.SetActive(false);
			this.btnHeadBack.gameObject.SetActive(false);
			this.btnBodyBack.gameObject.SetActive(false);
		}
		foreach (KeyValuePair<Toggle, string> keyValuePair in this.dictSeekings)
		{
			CrewSim.SetToggleWithoutNotify(keyValuePair.Key, this.coUser.HasCond(keyValuePair.Value));
		}
		this.bIgnoreEvents = false;
		this.UpdatePartNums();
		this._paperDollManager.SetPaperDoll(this.coUser);
		this._paperDollManager.HideBackDrag();
		this._paperDollManager.HideTrash();
	}

	private void RandFacePart(int nPart)
	{
		string[] faceParts = this.crew.FaceParts;
		if (nPart < 0 || nPart >= faceParts.Length)
		{
			return;
		}
		string[] faceGroups = FaceAnim2.GetFaceGroups(this.crew.FaceParts);
		string str = "Nonbinary";
		if (this.coUser.HasCond("IsMale"))
		{
			str = "Male";
		}
		if (this.coUser.HasCond("IsFemale"))
		{
			str = "Female";
		}
		string str2 = faceGroups[0];
		List<string> lootNames = DataHandler.GetLoot("TXTPortrait" + str2 + str).GetLootNames("TXTPortrait", false, null);
		faceParts[nPart] = lootNames[nPart];
		this.crew.FaceParts = faceParts;
		MonoSingleton<GUIRenderTargets>.Instance.SetFace(this.coUser, true);
		this.UpdatePartNums();
	}

	private void FacePartNext(int nPart, int nDir)
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		Crew component = objThem.GetComponent<Crew>();
		string[] faceParts = component.FaceParts;
		if (nPart < 0 || nPart >= faceParts.Length)
		{
			return;
		}
		string text = faceParts[nPart];
		int num = this.mapParts[nPart].IndexOf(text);
		int num2 = num + nDir;
		if (this.mapParts[nPart].Count > 0)
		{
			if (num2 >= this.mapParts[nPart].Count)
			{
				text = this.mapParts[nPart][0];
			}
			else if (num2 < 0)
			{
				text = this.mapParts[nPart][this.mapParts[nPart].Count - 1];
			}
			else
			{
				text = this.mapParts[nPart][num2];
			}
		}
		faceParts[nPart] = text;
		component.FaceParts = faceParts;
		MonoSingleton<GUIRenderTargets>.Instance.SetFace(objThem, true);
		if (nPart == 8)
		{
			this.coUser.faceRef.SetEmoteStateOverride(1);
		}
		this.UpdatePartNums();
		this._paperDollManager.SetMttPaperDoll(objThem);
		this._paperDollManager.HideBackDrag();
		this._paperDollManager.HideTrash();
	}

	private void BodyNext(int nDir)
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		int num = this.aBodyTypes.IndexOf(objThem.Crew.BodyType);
		num += nDir;
		if (num >= this.aBodyTypes.Count)
		{
			num = 0;
		}
		else if (num < 0)
		{
			num = this.aBodyTypes.Count - 1;
		}
		objThem.Crew.BodyType = this.aBodyTypes[num];
		this.UpdatePartNums();
		this._paperDollManager.SetPaperDoll(this.coUser);
		this._paperDollManager.HideBackDrag();
		this._paperDollManager.HideTrash();
		this.ToggleBodyView(true);
	}

	private void OnSeekingChanged()
	{
		bool flag = true;
		foreach (KeyValuePair<Toggle, string> keyValuePair in this.dictSeekings)
		{
			if (keyValuePair.Key.isOn)
			{
				this.coUser.SetCondAmount(keyValuePair.Value, 1.0, 0.0);
				flag = false;
			}
			else
			{
				this.coUser.ZeroCondAmount(keyValuePair.Value);
			}
		}
		if (flag)
		{
			this.coUser.SetCondAmount("IsAttractedNone", 1.0, 0.0);
		}
		else
		{
			this.coUser.ZeroCondAmount("IsAttractedNone");
		}
	}

	private void RandName()
	{
		string strGender = "IsNB";
		if (this.coUser.HasCond("IsMale"))
		{
			strGender = "IsMale";
		}
		if (this.coUser.HasCond("IsFemale"))
		{
			strGender = "IsFemale";
		}
		string text = null;
		string text2 = null;
		DataHandler.GetFullName(strGender, out text, out text2);
		for (int i = 0; i < 5; i++)
		{
			CondOwner condOwner;
			if (!DataHandler.mapCOs.TryGetValue(text + " " + text2, out condOwner))
			{
				break;
			}
			float num = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
			if (num <= 0.5f)
			{
				text2 = DataHandler.EmbellishName(text2, false, strGender);
			}
			else
			{
				text = DataHandler.EmbellishName(text, true, strGender);
			}
		}
		this.tboxName.text = text + " " + text2;
	}

	private void ChangeName(string value)
	{
		if (this.bIgnoreEvents)
		{
			return;
		}
		string strName = this.coUser.strName;
		if (string.IsNullOrEmpty(value))
		{
			if (!CrewSim.Typing && !string.IsNullOrEmpty(strName))
			{
				this.tboxName.text = strName;
			}
			return;
		}
		if (value.IndexOfAny(GUIChargenBody.FORBIDDEN_WINDOWS_CHARACTERS) != -1)
		{
			string text = this.tboxName.text.Substring(0, this.tboxName.text.Length - 1);
			this.bIgnoreEvents = true;
			this.tboxName.text = text;
			this.bIgnoreEvents = false;
			AudioManager.am.PlayAudioEmitter("ShipUIBtnDatingAppDelete", false, false);
			return;
		}
		if (value.Length > 50)
		{
			string text2 = value.Substring(0, 50);
			AudioManager.am.PlayAudioEmitter("ShipUIBtnDatingAppDelete", false, false);
			this.tboxName.text = text2;
		}
		else
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnDatingAppType", false, false);
		}
		CondOwner x = null;
		if (DataHandler.mapCOs.TryGetValue(value, out x) && x != this.coUser)
		{
			string text3 = this.tboxName.text.Substring(0, this.tboxName.text.Length - 1);
			this.bIgnoreEvents = true;
			this.tboxName.text = text3;
			this.bIgnoreEvents = false;
			AudioManager.am.PlayAudioEmitter("ShipUIBtnDatingAppDelete", false, false);
			this.coUser.LogMessage(DataHandler.GetString("GUI_BODY_ERROR_SAME_NAME1", false) + value + DataHandler.GetString("GUI_BODY_ERROR_SAME_NAME2", false), "Bad", this.coUser.strID);
			return;
		}
		if (strName == value)
		{
			return;
		}
		this.coUser.strID = value;
		this.coUser.strName = value;
		this.coUser.strNameFriendly = value;
		if (this.coUser.objCondID != null)
		{
			this.coUser.mapConds.Remove(this.coUser.objCondID.strName);
			this.coUser.objCondID.strName = this.coUser.strName;
			this.coUser.mapConds[this.coUser.objCondID.strName] = this.coUser.objCondID;
		}
		int num = value.LastIndexOf(' ');
		string strFirstName = string.Empty;
		string strLastName = string.Empty;
		if (num >= 0)
		{
			strFirstName = value.Substring(0, num);
			strLastName = value.Substring(num + 1);
		}
		else
		{
			strLastName = value;
		}
		this.coUser.pspec.strFirstName = strFirstName;
		this.coUser.pspec.strLastName = strLastName;
		this.coUser.pspec.strCO = this.coUser.strName;
		GUIChargenStack component = this.coUser.GetComponent<GUIChargenStack>();
		component.strFirstName = strFirstName;
		component.strLastName = strLastName;
		foreach (List<Pledge2> list in this.coUser.dictPledges.Values)
		{
			foreach (Pledge2 pledge in list)
			{
				if (pledge.Us == this.coUser)
				{
					pledge.Us = this.coUser;
				}
				if (pledge.Them == this.coUser)
				{
					pledge.Them = this.coUser;
				}
			}
		}
		if (this.coUser.socUs != null)
		{
			List<Relationship> allPeople = this.coUser.socUs.GetAllPeople();
			foreach (Relationship relationship in allPeople)
			{
				if (relationship.pspec.GetCO() != null)
				{
					global::Social socUs = relationship.pspec.GetCO().socUs;
					if (socUs != null)
					{
						socUs.RenamePerson(strName, this.coUser.strName);
					}
				}
			}
		}
		List<string> list2 = new List<string>();
		list2.AddRange(this.coUser.GetShipsOwned());
		foreach (string strRegID in list2)
		{
			CrewSim.system.RegisterShipOwner(strRegID, this.coUser.strName);
		}
	}

	private void ChangePronoun(string strPronoun, bool value)
	{
		if (this.bIgnoreEvents)
		{
			return;
		}
		if (!value)
		{
			return;
		}
		if (strPronoun != null)
		{
			if (!(strPronoun == "IsMale"))
			{
				if (!(strPronoun == "IsFemale"))
				{
					if (strPronoun == "IsNB")
					{
						if (!this.coUser.HasCond("IsNB"))
						{
							this.coUser.AddCondAmount("IsNB", 1.0, 0.0, 0f);
						}
						this.coUser.AddCondAmount("IsMale", -1.0, 0.0, 0f);
						this.coUser.AddCondAmount("IsFemale", -1.0, 0.0, 0f);
						this.coUser.pspec.strGender = "IsNB";
					}
				}
				else
				{
					if (!this.coUser.HasCond("IsFemale"))
					{
						this.coUser.AddCondAmount("IsFemale", 1.0, 0.0, 0f);
					}
					this.coUser.AddCondAmount("IsMale", -1.0, 0.0, 0f);
					this.coUser.AddCondAmount("IsNB", -1.0, 0.0, 0f);
					this.coUser.pspec.strGender = "IsFemale";
				}
			}
			else
			{
				if (!this.coUser.HasCond("IsMale"))
				{
					this.coUser.AddCondAmount("IsMale", 1.0, 0.0, 0f);
				}
				this.coUser.AddCondAmount("IsFemale", -1.0, 0.0, 0f);
				this.coUser.AddCondAmount("IsNB", -1.0, 0.0, 0f);
				this.coUser.pspec.strGender = "IsMale";
			}
		}
		this.tboxName.text = this.coUser.strName;
	}

	private void UpdatePartList()
	{
		string[] faceParts = this.crew.FaceParts;
		string[] faceGroups = FaceAnim2.GetFaceGroups(this.crew.FaceParts);
		string str = "Nonbinary";
		this.coUser.pspec.strSkin = faceGroups[0];
		this.bRestoreFaceX = true;
		this.mapParts = new Dictionary<int, List<string>>();
		List<string> allLootNames = DataHandler.GetLoot("TXTPortrait" + this.coUser.pspec.strSkin + str).GetAllLootNames();
		int num = 0;
		foreach (string text in allLootNames)
		{
			if (!this.mapParts.ContainsKey(num))
			{
				this.mapParts[num] = new List<string>();
			}
			if (text.IndexOf("Missing") >= 0)
			{
				num++;
			}
			else
			{
				this.mapParts[num].Add(text);
			}
		}
	}

	private void ChangeAppearance()
	{
		this.crew.SetBodyFaceSkin(Crew.GetBodyType(this.coUser), FaceAnim2.GetRandomFace(this.chkHe.isOn || this.chkThey.isOn, this.chkShe.isOn || this.chkThey.isOn, null));
		MonoSingleton<GUIRenderTargets>.Instance.SetFace(this.coUser, true);
		this.UpdatePartList();
		this.GetHand();
		this.ChangeSkinRelatives();
		this.UpdatePartNums();
		this._paperDollManager.SetPaperDoll(this.coUser);
		this._paperDollManager.HideBackDrag();
		this._paperDollManager.HideTrash();
	}

	private void UpdatePartNums()
	{
		foreach (KeyValuePair<int, List<string>> keyValuePair in this.mapParts)
		{
			int num = keyValuePair.Value.IndexOf(this.crew.FaceParts[keyValuePair.Key]) + 1;
			int count = keyValuePair.Value.Count;
			this.aTxtNums[keyValuePair.Key].text = num + "/" + count;
		}
		int num2 = "ABC".IndexOf(this.coUser.pspec.strSkin) + 1;
		this.txtSkinNum.text = num2 + "/" + "ABC".Length;
		num2 = this.aBodyTypes.IndexOf(this.coUser.Crew.BodyType) + 1;
		this.txtBodyNum.text = num2 + "/" + this.aBodyTypes.Count;
	}

	private void ChangeSkin(int nDir)
	{
		string[] faceParts = this.crew.FaceParts;
		string[] faceGroups = FaceAnim2.GetFaceGroups(this.crew.FaceParts);
		this.coUser.pspec.strSkin = faceGroups[0];
		int num = "ABC".IndexOf(this.coUser.pspec.strSkin);
		num += nDir;
		if (num >= "ABC".Length)
		{
			num = 0;
		}
		else if (num < 0)
		{
			num = "ABC".Length - 1;
		}
		this.coUser.pspec.strSkin = "ABC"[num].ToString();
		List<string> lootNames = DataHandler.GetLoot("TXTPortrait" + this.coUser.pspec.strSkin + "Nonbinary").GetLootNames("TXTPortrait", false, null);
		faceParts[2] = lootNames[2];
		this.crew.FaceParts = faceParts;
		this.UpdatePartList();
		foreach (KeyValuePair<int, List<string>> keyValuePair in this.mapParts)
		{
			if (keyValuePair.Value.IndexOf(faceParts[keyValuePair.Key]) < 0)
			{
				faceParts[keyValuePair.Key] = keyValuePair.Value[UnityEngine.Random.Range(0, keyValuePair.Value.Count - 1)];
			}
		}
		this.crew.FaceParts = faceParts;
		MonoSingleton<GUIRenderTargets>.Instance.SetFace(this.coUser, true);
		this.GetHand();
		this.ChangeSkinRelatives();
		this.UpdatePartNums();
		this._paperDollManager.SetPaperDoll(this.coUser);
		this._paperDollManager.HideBackDrag();
		this._paperDollManager.HideTrash();
	}

	private void ChangeSkinRelatives()
	{
		JsonPersonSpec personSpec = DataHandler.GetPersonSpec("RELFamilyFind");
		CondOwner condOwner = null;
		foreach (string key in this.coUser.socUs.GetMatchingRelationsAll(personSpec))
		{
			if (DataHandler.mapCOs.TryGetValue(key, out condOwner))
			{
				condOwner.pspec.strSkin = this.coUser.pspec.strSkin;
				Crew component = condOwner.GetComponent<Crew>();
				component.SetBodyFaceSkin(Crew.GetBodyType(condOwner), FaceAnim2.GetRandomFace(condOwner.HasCond("IsMale"), condOwner.HasCond("IsFemale"), condOwner.pspec.strSkin));
			}
		}
		GUIChargenCareer.bRedrawSidebar = true;
	}

	private void GetHand()
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		string str = "GUIPDAHand" + objThem.pspec.strSkin;
		this.bmpHand.texture = DataHandler.LoadPNG(str + ".png", false, false);
	}

	private void ToggleBodyView(bool bBody)
	{
		this.cgPaperdoll.alpha = (float)((!bBody) ? 0 : 1);
		this.cgPortrait.alpha = (float)((!bBody) ? 1 : 0);
		if (this.chkBodyHead.isOn != bBody)
		{
			CrewSim.SetToggleWithoutNotify(this.chkBodyHead, bBody);
		}
	}

	private void KeyHandler()
	{
		if (Input.GetKey(KeyCode.W))
		{
			this.coUser.faceRef.SetEmoteStateOverride(4);
		}
		else if (Input.GetKey(KeyCode.A))
		{
			this.coUser.faceRef.SetEmoteStateOverride(2);
		}
		else if (Input.GetKey(KeyCode.S))
		{
			this.coUser.faceRef.SetEmoteStateOverride(1);
		}
		else if (Input.GetKey(KeyCode.D))
		{
			this.coUser.faceRef.SetEmoteStateOverride(3);
		}
		else if (Input.GetKey(KeyCode.X))
		{
			this.coUser.faceRef.SetEmoteStateOverride(5);
		}
	}

	public override void SaveAndClose()
	{
		if (this.bRestoreFaceX)
		{
			MonoSingleton<GUIRenderTargets>.Instance.SetTransform(this.coUser, null);
			this.bRestoreFaceX = false;
		}
		base.SaveAndClose();
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.fOffsetX = this._paperDollManager.transform.position.x;
		this.GetCOInfo();
	}

	public const string PRONOUN_HE = "IsMale";

	public const string PRONOUN_SHE = "IsFemale";

	public const string PRONOUN_THEY = "IsNB";

	public const string strSkinChoices = "ABC";

	public const string LOOT_MALE = "TXTBodyTypesMale";

	public const string LOOT_FEMALE = "TXTBodyTypesFemale";

	public const string LOOT_NB = "TXTBodyTypesNB";

	private static readonly char[] FORBIDDEN_WINDOWS_CHARACTERS = new char[]
	{
		'<',
		'>',
		':',
		'"',
		'/',
		'\\',
		'|',
		'?',
		'*'
	};

	private Toggle chkHe;

	private Toggle chkShe;

	private Toggle chkThey;

	private Toggle chkSeekingHe;

	private Toggle chkSeekingShe;

	private Toggle chkSeekingThey;

	private TMP_InputField tboxName;

	private Button btnDone;

	private Button btnHair;

	private Button btnHead;

	private Button btnScar;

	private Button btnGlasses;

	private Button btnSkin;

	private Button btnPupils;

	private Button btnEyes;

	private Button btnNose;

	private Button btnTeeth;

	private Button btnLips;

	private Button btnNeck;

	private Button btnBeard;

	private Button btnBody;

	private Button btnHairBack;

	private Button btnHeadBack;

	private Button btnScarBack;

	private Button btnGlassesBack;

	private Button btnSkinBack;

	private Button btnPupilsBack;

	private Button btnEyesBack;

	private Button btnNoseBack;

	private Button btnTeethBack;

	private Button btnLipsBack;

	private Button btnNeckBack;

	private Button btnBeardBack;

	private Button btnBodyBack;

	private Button btnRandName;

	private Button btnRandFace;

	private TMP_Text txtHairNum;

	private TMP_Text txtHeadNum;

	private TMP_Text txtScarNum;

	private TMP_Text txtGlassesNum;

	private TMP_Text txtSkinNum;

	private TMP_Text txtPupilsNum;

	private TMP_Text txtEyesNum;

	private TMP_Text txtNoseNum;

	private TMP_Text txtTeethNum;

	private TMP_Text txtLipsNum;

	private TMP_Text txtNeckNum;

	private TMP_Text txtBeardNum;

	private TMP_Text txtBodyNum;

	private TMP_Text[] aTxtNums;

	private RawImage bmpHand;

	private CondOwner coUser;

	private Crew crew;

	private bool bIgnoreEvents;

	private Dictionary<int, List<string>> mapParts;

	private bool bRestoreFaceX;

	private float fOffsetX;

	private List<string> aBodyTypes;

	private Dictionary<Toggle, string> dictSeekings;

	public float fOffsetCoeff = 16f;

	[SerializeField]
	private RawImage _bmpPortrait;

	[SerializeField]
	private RawImage _bmpPdaPortrait;

	[SerializeField]
	private GUIPaperDollManager _paperDollManager;

	[SerializeField]
	private CanvasGroup cgPaperdoll;

	[SerializeField]
	private CanvasGroup cgPortrait;

	[SerializeField]
	private Toggle chkBodyHead;
}
