using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.Events.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Paper-doll equipment and wound UI. Likely mirrors the selected crew member's
// body slots, slotted items, drag/drop state, and visible injury overlays.
public class GUIPaperDollManager : MonoBehaviour
{
	// Unity setup: caches key widget references, creates the slotted-item root,
	// and ensures the shared wound/slot events exist before listeners attach.
	private void Awake()
	{
		this.tfPaperDoll = (base.transform as RectTransform);
		this.aSlots = new List<GameObject>();
		if (!this.cgPaperDoll)
		{
			this.cgPaperDoll = base.GetComponent<CanvasGroup>();
		}
		if (!this.bmpBack)
		{
			this.bmpBack = base.transform.Find("bmpBack").GetComponent<RawImage>();
		}
		if (!this.bmpBody)
		{
			this.bmpBody = base.transform.Find("bmpBody").GetComponent<RawImage>();
		}
		if (!this.bmpDrag)
		{
			this.bmpDrag = base.transform.Find("bmpDrag").GetComponent<RawImage>();
		}
		if (!this.rectTrash)
		{
			this.rectTrash = base.transform.Find("bmpTrashHB").GetComponent<RectTransform>();
		}
		if (!this.cgTrashIco)
		{
			this.cgTrashIco = base.transform.Find("bmpTrashIco").GetComponent<CanvasGroup>();
		}
		if (!this.helmetMask)
		{
			this.helmetMask = base.transform.Find("helmetMask").GetComponent<Image>();
		}
		if (!this.bmpPortrait && this.helmetMask != null)
		{
			this.helmetMask.transform.Find("bmpPortrait").GetComponent<RawImage>();
		}
		this.cgBtnTrash = this.btnTrashUndo.GetComponent<CanvasGroup>();
		this.btnTrashUndo.onClick.AddListener(delegate()
		{
			this.UndoTrash();
		});
		CanvasManager.HideCanvasGroup(this.cgBtnTrash);
		this.tfSlottedItemRoot = new GameObject("SlottedItems").transform;
		this.tfSlottedItemRoot.SetParent(this.tfPaperDoll);
		if (Wound.OnWoundUpdated == null)
		{
			Wound.OnWoundUpdated = new WoundUpdatedEvent();
		}
		if (Slots.OnSlotContentUpdated == null)
		{
			Slots.OnSlotContentUpdated = new SlotUpdatedEvent();
		}
	}

	// Delayed startup so CrewSim can finish selecting a crew member first, then
	// binds the first paper-doll to that active character.
	private IEnumerator Start()
	{
		if (GUIPaperDollManager.InitFinished)
		{
			yield break;
		}
		GUIPaperDollManager.InitFinished = true;
		Wound.OnWoundUpdated.AddListener(new UnityAction<PaperdollWoundDTO>(this.OnWoundUpdated));
		yield return new WaitUntil(() => CrewSim.GetSelectedCrew() != null);
		this.SetPaperDoll(CrewSim.GetSelectedCrew());
		yield break;
	}

	// Removes event listeners and destroys spawned slot/wound visuals on teardown.
	private void OnDestroy()
	{
		Slots.OnSlotContentUpdated.RemoveListener(new UnityAction<CondOwner, CondOwner>(this.OnSlotUpdated));
		Wound.OnWoundUpdated.RemoveListener(new UnityAction<PaperdollWoundDTO>(this.OnWoundUpdated));
		if (this.mapCOIDsToGO != null)
		{
			foreach (KeyValuePair<string, GameObject> keyValuePair in this.mapCOIDsToGO)
			{
				UnityEngine.Object.Destroy(keyValuePair.Value);
			}
			this.mapCOIDsToGO.Clear();
			this.mapCOIDsToGO = null;
		}
		if (this.mapCOIDsToGOPain != null)
		{
			foreach (KeyValuePair<string, RawImage> keyValuePair2 in this.mapCOIDsToGOPain)
			{
				UnityEngine.Object.Destroy(keyValuePair2.Value);
			}
			this.mapCOIDsToGOPain.Clear();
			this.mapCOIDsToGOPain = null;
		}
		this.ctFilter = null;
		if (this.aSlots != null)
		{
			foreach (GameObject obj in this.aSlots)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.aSlots.Clear();
			this.aSlots = null;
		}
		UnityEngine.Object.Destroy(this.bmpBody);
		UnityEngine.Object.Destroy(this.bmpBack);
		UnityEngine.Object.Destroy(this.bmpDrag);
		UnityEngine.Object.Destroy(this.bmpPortrait);
		UnityEngine.Object.Destroy(this.helmetMask);
		this.tfPaperDoll = null;
		this.tfSlottedItemRoot = null;
		this.cgPaperDoll = null;
		this._coUsRef = null;
		this._rmbHitCO = null;
	}

	// Per-frame drag/hover handling while the inventory and paper-doll are open.
	private void Update()
	{
		if (this.cgPaperDoll == null || this.cgPaperDoll.alpha == 0f || !GUIInventory.instance.IsInventoryVisible)
		{
			return;
		}
		if (this.coUs != null)
		{
			this.coTtipSlotted = null;
			this.goTtipPaperDollCopy = null;
			this.CheckHit(ref this.coTtipSlotted, ref this.goTtipPaperDollCopy);
			if (this.coTtipSlotted != null)
			{
				GUIInventory.instance.coTooltip = this.coTtipSlotted;
			}
			else if (GUIInventory.instance.coTooltip != null && !GUIInventory.instance.coTooltip.HasCond("IsInContainer"))
			{
				GUIInventory.instance.coTooltip = null;
			}
		}
		if (this.coTrash != null)
		{
			this.CheckTrash();
		}
		if (Input.GetMouseButtonDown(0))
		{
			this.OnPointerDown0();
		}
		if (Input.GetMouseButtonDown(1))
		{
			this.OnPointerDown1();
		}
	}

	private void CheckTrash()
	{
		if (!this.txtCount)
		{
			this.txtCount = this.cgBtnTrash.transform.Find("txtCount").GetComponent<TextMeshProUGUI>();
		}
		if (this.cgBtnTrash && this.cgBtnTrash.alpha == 0f)
		{
			CanvasManager.ShowCanvasGroup(this.cgBtnTrash);
		}
		if (this.fTrashEpoch > 0.0 && this.coTrash != null)
		{
			if (!CrewSim.Paused)
			{
				this.fTrashEpoch -= (double)Time.unscaledDeltaTime;
			}
			this.txtCount.text = MathUtils.RoundToInt(this.fTrashEpoch).ToString();
			return;
		}
		CanvasManager.HideCanvasGroup(this.cgBtnTrash);
		if (this.coTrash != null)
		{
			this.coTrash.Destroy();
		}
	}

	public void DelayTrash(GUIInventoryItem gi)
	{
		if (gi == null)
		{
			return;
		}
		if (this.coTrash != null)
		{
			this.coTrash.RemoveFromCurrentHome(false);
			this.coTrash.Destroy();
		}
		this.coTrash = gi.CO;
		this.fTrashEpoch = 5.0;
		CanvasManager.ShowCanvasGroup(this.cgBtnTrash);
	}

	private void UndoTrash()
	{
		if (GUIInventory.instance.Selected != null)
		{
			if (this.coUs != null)
			{
				this.coUs.LogMessage(DataHandler.GetString("GUI_INV_NO_UNDO", false), "Bad", this.coUs.strID);
			}
			return;
		}
		GUIInventoryItem guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(this.coTrash.strID, null);
		if (guiinventoryItem != null)
		{
			guiinventoryItem.AttachToCursor(null);
			this.coTrash = null;
		}
		CanvasManager.HideCanvasGroup(this.cgBtnTrash);
	}

	private void OnSlotUpdated(CondOwner coSlot, CondOwner coItem)
	{
		if (coSlot == null || !GUIInventory.instance.IsInventoryVisible)
		{
			return;
		}
		CondOwner condOwner = coSlot.RootParent(null);
		if (this.strCOIDLast == coSlot.strID || (condOwner != null && condOwner.strID == this.strCOIDLast))
		{
			this.SetPaperDoll(this.coUs);
		}
		this.SetHelmetMask();
	}

	private void OnWoundUpdated(PaperdollWoundDTO woundDTO)
	{
		if (woundDTO == null || woundDTO.CoDoll.strID != this.strCOIDLast)
		{
			return;
		}
		this.SwapPaperDollImage(woundDTO.CoSlot, woundDTO.WoundTex);
		this.SetPainAlpha(woundDTO.CoSlot);
	}

	public void SetMttPaperDoll(CondOwner co)
	{
		if (co == null || co.strID == this.strCOIDLast)
		{
			return;
		}
		Slots.OnSlotContentUpdated.AddListener(new UnityAction<CondOwner, CondOwner>(this.OnSlotUpdated));
		Wound.OnWoundUpdated.AddListener(new UnityAction<PaperdollWoundDTO>(this.OnWoundUpdated));
		this.SetPaperDoll(co);
		this.HideTrash();
	}

	public void SetPaperDoll(CondOwner coUs)
	{
		this.strCOIDLast = null;
		this.mapCOIDsToGO.Clear();
		this.mapCOIDsToGOPain.Clear();
		foreach (GameObject gameObject in this.aSlots)
		{
			gameObject.transform.SetParent(null);
			UnityEngine.Object.Destroy(gameObject);
		}
		this.aSlots.Clear();
		this.SetFace(coUs);
		if (coUs == null)
		{
			return;
		}
		if (this.bmpBody == null)
		{
			this.bmpBody = base.transform.Find("bmpBody").GetComponent<RawImage>();
		}
		this.HideBackDrag();
		string str = "body" + FaceAnim2.GetFaceGroups(coUs.Crew.FaceParts).First<string>() + coUs.Crew.BodyType;
		if (coUs.IsRobot)
		{
			str = coUs.strType;
		}
		string str2 = "paperdoll/" + str;
		this.bmpBody.texture = DataHandler.LoadPNG(str2 + ".png", false, false);
		this.strCOIDLast = coUs.strID;
		this.CreateSlots(coUs);
	}

	public void SetHelmetMask()
	{
		bool enabled = this.helmetMask.enabled;
		bool flag = false;
		if (!this.coUs.compSlots)
		{
			if (enabled)
			{
				this.helmetMask.enabled = false;
			}
			return;
		}
		Slot slot = this.coUs.compSlots.GetSlot("head_out");
		if (slot == null)
		{
			if (enabled)
			{
				this.helmetMask.enabled = false;
			}
			return;
		}
		if (!slot.GetOutermostCO())
		{
			if (enabled)
			{
				this.helmetMask.enabled = false;
			}
			return;
		}
		if (slot.GetOutermostCO().HasCond("IsPSHelmet") || slot.GetOutermostCO().HasCond("IsEVAHelmet"))
		{
			flag = true;
		}
		if (flag && !enabled)
		{
			this.helmetMask.enabled = true;
		}
	}

	public void HideTrash()
	{
		RawImage component = base.transform.Find("bmpTrash").GetComponent<RawImage>();
		if (component.gameObject.activeSelf)
		{
			component.gameObject.SetActive(false);
		}
		this.btnTrashUndo.gameObject.SetActive(false);
	}

	public void HideBackDrag()
	{
		if (this.bmpBack == null)
		{
			this.bmpBack = base.transform.Find("bmpBack").GetComponent<RawImage>();
		}
		if (this.bmpBack.gameObject.activeSelf)
		{
			this.bmpBack.gameObject.SetActive(false);
		}
		if (this.bmpDrag == null)
		{
			this.bmpDrag = base.transform.Find("bmpDrag").GetComponent<RawImage>();
		}
		if (this.bmpDrag.gameObject.activeSelf)
		{
			this.bmpDrag.gameObject.SetActive(false);
		}
	}

	private void SetFace(CondOwner coUs)
	{
		if (!this.bmpPortrait)
		{
			return;
		}
		if (coUs == null || coUs.IsRobot)
		{
			if (this.bmpPortrait.gameObject.activeSelf)
			{
				this.bmpPortrait.gameObject.SetActive(false);
			}
			this.bmpPortrait.texture = null;
		}
		else
		{
			this.bmpPortrait.texture = MonoSingleton<GUIRenderTargets>.Instance.CreatePortrait(coUs);
			if (!this.bmpPortrait.gameObject.activeSelf)
			{
				this.bmpPortrait.gameObject.SetActive(true);
			}
		}
	}

	private void CreateSlots(CondOwner coSlot)
	{
		if (coSlot == null || coSlot.compSlots == null)
		{
			return;
		}
		CondOwner condOwner = coSlot.RootParent(null);
		if ((condOwner == null && coSlot.strID != this.strCOIDLast) || (condOwner != null && condOwner.strID != this.strCOIDLast))
		{
			return;
		}
		List<Slot> slotsDepthFirst = coSlot.compSlots.GetSlotsDepthFirst(false);
		foreach (Slot slot in slotsDepthFirst)
		{
			this.CreateEmptyHitboxImage(slot, coSlot);
			if (this.bmpDrag != null && slot.strName.Contains("drag"))
			{
				this.bmpDrag.gameObject.SetActive(true);
			}
			if (this.bmpBack != null && slot.strName.Contains("back"))
			{
				this.bmpBack.gameObject.SetActive(true);
			}
			if (!this.coUs.IsRobot || (!(slot.strName == "heldL") && !(slot.strName == "heldR")))
			{
				for (int i = 0; i < slot.aCOs.Length; i++)
				{
					CondOwner condOwner2 = slot.aCOs[i];
					if (this.ctFilter == null || this.ctFilter.Triggered(condOwner2, null, true))
					{
						this.CreateNewPaperDollImage(condOwner2, slot.strName);
					}
				}
			}
		}
	}

	public void DestroySlots(CondOwner coSlot)
	{
		if (coSlot == null)
		{
			return;
		}
		List<string> list = new List<string>
		{
			coSlot.strID
		};
		List<GameObject> list2 = new List<GameObject>();
		this.GatherSlots(coSlot, list, list2);
		foreach (string strCOID in list)
		{
			this.DestroyPaperDollImage(strCOID);
		}
		foreach (GameObject gameObject in list2)
		{
			gameObject.transform.SetParent(null);
			UnityEngine.Object.Destroy(gameObject);
			this.aSlots.Remove(gameObject);
		}
	}

	public void GatherSlots(CondOwner coSlot, List<string> aSlotImages, List<GameObject> aSlotsEmpty)
	{
		if (coSlot == null || aSlotImages == null || aSlotsEmpty == null)
		{
			return;
		}
		if (!aSlotImages.Contains(coSlot.strID))
		{
			aSlotImages.Add(coSlot.strID);
		}
		Slots component = coSlot.GetComponent<Slots>();
		if (component == null)
		{
			return;
		}
		foreach (GameObject gameObject in this.aSlots)
		{
			GUIPaperDollHitbox component2 = gameObject.GetComponent<GUIPaperDollHitbox>();
			if (component2 != null && component2.coUs != null && component2.coUs.strID == coSlot.strID && !aSlotsEmpty.Contains(gameObject))
			{
				aSlotsEmpty.Add(gameObject);
			}
		}
		foreach (Slot slot in component.GetSlotsDepthFirst(false))
		{
			for (int i = 0; i < slot.aCOs.Length; i++)
			{
				CondOwner condOwner = slot.aCOs[i];
				if (!(condOwner == null))
				{
					if (!aSlotImages.Contains(condOwner.strID))
					{
						aSlotImages.Add(condOwner.strID);
					}
					this.GatherSlots(condOwner, aSlotImages, aSlotsEmpty);
				}
			}
		}
	}

	private void CreateEmptyHitboxImage(Slot slot, CondOwner coSlot)
	{
		if (slot == null || coSlot == null)
		{
			return;
		}
		GameObject original = Resources.Load("prefabSlotMask") as GameObject;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfPaperDoll.parent);
		gameObject.name = slot.strName;
		this.aSlots.Add(gameObject);
		RawImage component = gameObject.GetComponent<RawImage>();
		component.texture = DataHandler.LoadPNG(slot.strHitboxImage + ".png", false, false);
		component.color = new Color(1f, 1f, 1f, 0f);
		string text = slot.strIconImage;
		if (string.IsNullOrEmpty(text))
		{
			text = "blank";
		}
		RawImage component2 = gameObject.transform.Find("bmpIcon").GetComponent<RawImage>();
		component2.texture = DataHandler.LoadPNG(text + ".png", false, false);
		component2.color = new Color(1f, 1f, 0f, 1f);
		RectTransform component3 = gameObject.GetComponent<RectTransform>();
		component3.anchorMin = this.tfPaperDoll.anchorMin;
		component3.anchorMax = this.tfPaperDoll.anchorMax;
		component3.anchoredPosition = this.tfPaperDoll.anchoredPosition;
		component3.sizeDelta = this.tfPaperDoll.sizeDelta;
		GUIPaperDollHitbox component4 = gameObject.GetComponent<GUIPaperDollHitbox>();
		component4.strSlotName = slot.strName;
		component4.coUs = coSlot;
		component4.cgIcon = component2.GetComponent<CanvasGroup>();
		component4.cgIcon.alpha = 0f;
		if (slot.bHide)
		{
			CanvasManager.HideCanvasGroup(component3);
		}
		else
		{
			CanvasManager.ShowCanvasGroup(component3);
		}
		gameObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
		Transform slotDepth = this.GetSlotDepth(slot.nDepth);
		gameObject.transform.SetParent(slotDepth, true);
		CanvasManager.SetAnchorsToCorners(component3);
	}

	public void CreateNewPaperDollImage(CondOwner coSlotted, string strSlot)
	{
		if (coSlotted == null)
		{
			return;
		}
		if (coSlotted.objCOParent == null || coSlotted.objCOParent.compSlots == null)
		{
			Debug.LogError("Error: Paper doll item " + coSlotted.strName + " has null parent/slots. Skipping...");
			return;
		}
		Slot slot = coSlotted.objCOParent.compSlots.GetSlot(strSlot);
		if (coSlotted.slotNow != slot)
		{
			return;
		}
		Transform slotDepth = this.GetSlotDepth(slot.nDepth);
		Transform transform = null;
		IEnumerator enumerator = slotDepth.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform2 = (Transform)obj;
				if (!(transform2.name != strSlot))
				{
					GUIPaperDollHitbox component = transform2.GetComponent<GUIPaperDollHitbox>();
					if (component != null && component.coUs != null && component.coUs.strID == coSlotted.objCOParent.strID)
					{
						transform = transform2;
						break;
					}
				}
			}
		}
		finally
		{
			IDisposable disposable;
			if ((disposable = (enumerator as IDisposable)) != null)
			{
				disposable.Dispose();
			}
		}
		if (transform == null)
		{
			transform = slotDepth.Find(strSlot);
		}
		if (transform != null)
		{
			Item item = coSlotted.Item;
			string text = null;
			string text2 = null;
			bool flag = false;
			bool flag2 = !slot.bAlignSlot;
			JsonSlotEffects jsonSlotEffects = null;
			coSlotted.mapSlotEffects.TryGetValue(strSlot, out jsonSlotEffects);
			float[] array = new float[]
			{
				0.2859f,
				0.0014f,
				0.9675f,
				0.5577f
			};
			if (jsonSlotEffects != null)
			{
				text = jsonSlotEffects.strSlotImage;
				text2 = jsonSlotEffects.strSlotImageUnder;
				if (item != null && coSlotted.mapAltSlotImgs != null && !string.IsNullOrEmpty(item.ImgOverride) && coSlotted.mapAltSlotImgs.ContainsKey(item.ImgOverride))
				{
					text = coSlotted.mapAltSlotImgs[item.ImgOverride];
				}
				flag = !string.IsNullOrEmpty(text);
				flag2 = (jsonSlotEffects.bWholeBody || flag2);
				if (jsonSlotEffects.aTextAnchors != null)
				{
					if (jsonSlotEffects.aTextAnchors.Length != 4)
					{
						Debug.LogWarning(string.Concat(new object[]
						{
							"Incorrect length of text anchors in JsonSlotEffects: ",
							jsonSlotEffects.ToString(),
							" Must be 4, is: ",
							jsonSlotEffects.aTextAnchors.Length
						}));
					}
					else
					{
						array = jsonSlotEffects.aTextAnchors;
					}
				}
			}
			Texture2D texture2D = null;
			if (text == null)
			{
				if (item != null)
				{
					text = item.ImgOverride;
				}
				else if (coSlotted.Crew != null && !coSlotted.IsRobot)
				{
					texture2D = FaceAnim2.GetPNG(coSlotted);
					flag = true;
					text2 = coSlotted.strPortraitImg;
				}
				else
				{
					text = coSlotted.strPortraitImg;
				}
			}
			if (text == null)
			{
				text = "missing";
			}
			Wound wound = null;
			if (coSlotted.HasCond("IsWound"))
			{
				wound = coSlotted.GetComponent<Wound>();
				if (wound != null)
				{
					texture2D = wound.GetSlotImage();
				}
			}
			if (texture2D == null)
			{
				texture2D = DataHandler.LoadPNG(text + ".png", false, false);
			}
			GameObject gameObject = null;
			if (!this.mapCOIDsToGO.TryGetValue(coSlotted.strID, out gameObject))
			{
				GameObject original = Resources.Load("prefabGUIInvSlottedItem") as GameObject;
				gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this.tfPaperDoll.parent);
				gameObject.name = coSlotted.strName;
				GUIPaperDollHitbox component2 = gameObject.GetComponent<GUIPaperDollHitbox>();
				component2.coUs = coSlotted;
				component2.UpdateStackText(gameObject);
			}
			gameObject.GetComponent<GUIPaperDollHitbox>().strSlotName = strSlot;
			RawImage component3 = gameObject.GetComponent<RawImage>();
			component3.texture = texture2D;
			if (item != null)
			{
				component3.material = item.SetUpInventoryMaterial(component3.texture);
			}
			if (text2 != null)
			{
				component3.material.SetTexture("_DmgTex", DataHandler.LoadPNG(text2 + ".png", false, false));
				component3.material.SetFloat("_DmgPresent", 1f);
				component3.material.SetFloat("_Lerp", 0f);
			}
			RectTransform component4 = gameObject.GetComponent<RectTransform>();
			RectTransform component5 = transform.GetComponent<RectTransform>();
			if (flag2)
			{
				component4.anchorMin = this.tfPaperDoll.anchorMin;
				component4.anchorMax = this.tfPaperDoll.anchorMax;
				component4.anchoredPosition = this.tfPaperDoll.anchoredPosition;
				component4.sizeDelta = this.tfPaperDoll.sizeDelta;
				gameObject.transform.SetParent(transform, true);
				RectTransform component6 = component4.GetChild(0).GetComponent<RectTransform>();
				RectTransform component7 = component4.GetChild(1).GetComponent<RectTransform>();
				component6.anchorMin = new Vector2(array[0], array[1]);
				component7.anchorMin = new Vector2(array[0], array[1]);
				component6.anchorMax = new Vector2(array[2], array[3]);
				component7.anchorMax = new Vector2(array[2], array[3]);
				component6.offsetMin = new Vector2(0f, 0f);
				component6.offsetMax = new Vector2(0f, 0f);
				component7.offsetMin = new Vector2(0f, 0f);
				component7.offsetMax = new Vector2(0f, 0f);
				component6.sizeDelta = new Vector2(1f, 1f);
				component7.sizeDelta = new Vector2(1f, 1f);
				if (jsonSlotEffects != null && jsonSlotEffects.bMirror)
				{
					component6.localScale = new Vector3(-1f, 1f, 1f);
					component7.localScale = new Vector3(-1f, 1f, 1f);
				}
				else
				{
					component6.localScale = new Vector3(1f, 1f, 1f);
					component7.localScale = new Vector3(1f, 1f, 1f);
				}
			}
			else
			{
				gameObject.transform.SetParent(transform, true);
				component4.anchoredPosition = new Vector2(slot.ptAlign.x * component5.rect.width / 2f, slot.ptAlign.y * component5.rect.height / 2f);
				float num = component5.rect.width / 310f;
				float num2 = component5.rect.height / 384f;
				if (flag)
				{
					num /= 3f;
					num2 /= 3f;
				}
				else
				{
					num *= 3f;
					num2 *= 3f;
				}
				component4.sizeDelta = new Vector2((float)component3.texture.width * num, (float)component3.texture.height * num2);
			}
			if (jsonSlotEffects != null && jsonSlotEffects.bMirror)
			{
				component4.localScale = new Vector3(-1f, 1f, 1f);
			}
			else
			{
				component4.localScale = new Vector3(1f, 1f, 1f);
			}
			CanvasManager.SetAnchorsToCorners(component4);
			this.mapCOIDsToGO[coSlotted.strID] = gameObject;
			if (wound != null)
			{
				Texture2D painPNG = wound.GetPainPNG();
				RawImage rawImage = UnityEngine.Object.Instantiate<RawImage>(this.bmpBody, this.tfPaperDoll.parent);
				rawImage.gameObject.name = wound.strName + coSlotted.strID;
				rawImage.texture = painPNG;
				double condAmount = coSlotted.GetCondAmount("StatPain");
				rawImage.color = new Color(1f, 1f, 1f, (float)condAmount);
				RectTransform component8 = rawImage.GetComponent<RectTransform>();
				component8.anchorMin = this.tfPaperDoll.anchorMin;
				component8.anchorMax = this.tfPaperDoll.anchorMax;
				component8.anchoredPosition = this.tfPaperDoll.anchoredPosition;
				component8.sizeDelta = this.tfPaperDoll.sizeDelta;
				rawImage.transform.SetParent(transform, true);
				if (jsonSlotEffects != null && jsonSlotEffects.bMirror)
				{
					component8.localScale = new Vector3(-1f, 1f, 1f);
				}
				else
				{
					component8.localScale = new Vector3(1f, 1f, 1f);
				}
				CanvasManager.SetAnchorsToCorners(component8);
				this.mapCOIDsToGOPain[coSlotted.strID] = rawImage;
			}
			this.CreateSlots(coSlotted);
			this.SetHelmetMask();
		}
		else
		{
			Debug.LogWarning("slot null " + coSlotted.name);
		}
	}

	private Transform GetSlotDepth(int nDepth)
	{
		Transform transform = this.tfSlottedItemRoot.Find("SlotDepth" + nDepth);
		if (transform == null)
		{
			for (int i = this.tfSlottedItemRoot.childCount; i < nDepth + 1; i++)
			{
				transform = new GameObject("SlotDepth" + i).transform;
				transform.SetParent(this.tfSlottedItemRoot);
			}
		}
		return transform;
	}

	public void DestroyPaperDollImage(string strCOID)
	{
		if (strCOID == null)
		{
			return;
		}
		GameObject obj = null;
		if (this.mapCOIDsToGO.TryGetValue(strCOID, out obj))
		{
			UnityEngine.Object.Destroy(obj);
			this.mapCOIDsToGO.Remove(strCOID);
		}
		RawImage obj2 = null;
		if (this.mapCOIDsToGOPain.TryGetValue(strCOID, out obj2))
		{
			UnityEngine.Object.Destroy(obj2);
			this.mapCOIDsToGOPain.Remove(strCOID);
		}
	}

	public void UpdatePaperDollImage(CondOwner co)
	{
		if (co == null || co.strID == null)
		{
			return;
		}
		GameObject gameObject = null;
		if (!this.mapCOIDsToGO.TryGetValue(co.strID, out gameObject))
		{
			return;
		}
		GUIPaperDollHitbox component = gameObject.transform.parent.GetComponent<GUIPaperDollHitbox>();
		if (component == null)
		{
			return;
		}
		if (component.strSlotName != null)
		{
			this.DestroyPaperDollImage(co.strID);
			this.CreateNewPaperDollImage(co, component.strSlotName);
		}
	}

	public void SwapPaperDollImage(CondOwner co, Texture2D texture)
	{
		if (co == null || co.strID == null || texture == null)
		{
			return;
		}
		GameObject gameObject = null;
		if (!this.mapCOIDsToGO.TryGetValue(co.strID, out gameObject))
		{
			return;
		}
		RawImage component = gameObject.GetComponent<RawImage>();
		if (component == null)
		{
			return;
		}
		component.texture = texture;
	}

	public void SetPainAlpha(CondOwner coWound)
	{
		if (coWound == null)
		{
			return;
		}
		RawImage rawImage = null;
		if (this.mapCOIDsToGOPain.TryGetValue(coWound.strID, out rawImage))
		{
			double condAmount = coWound.GetCondAmount("StatPain");
			double condAmount2 = coWound.RootParent(null).GetCondAmount("ThreshStatPain");
			rawImage.color = new Color(1f, 1f, 1f, (float)(condAmount / condAmount2));
		}
	}

	private bool IsObstructedByUI()
	{
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = Input.mousePosition;
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		if (list.Count == 0)
		{
			return false;
		}
		RaycastResult raycastResult = list.FirstOrDefault<RaycastResult>();
		return raycastResult.gameObject != null && raycastResult.gameObject != base.gameObject && raycastResult.gameObject.name != "bmpPortrait";
	}

	private void OnPointerDown0()
	{
		if (CrewSim.objInstance.contextMenuPool.IsRaised)
		{
			return;
		}
		if (this.cgPaperDoll == null || this.cgPaperDoll.alpha == 0f || !GUIInventory.instance.IsInventoryVisible)
		{
			return;
		}
		if (this.coUs == null || this.coUs.compSlots == null)
		{
			return;
		}
		if (GUIInventory.instance.LastSelected == null && GUIInventory.instance.Selected == null)
		{
			CondOwner condOwner = null;
			GameObject gameObject = null;
			this.CheckHit(ref condOwner, ref gameObject);
			if (condOwner == null || gameObject == null || this.IsObstructedByUI())
			{
				return;
			}
			if (Input.GetMouseButtonDown(0) && condOwner.Item != null)
			{
				GUIInventoryItem guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(condOwner.strID, null);
				if (guiinventoryItem != null)
				{
					if (GUIActionKeySelector.commandQuickMove.Held)
					{
						guiinventoryItem.OnShiftPointerDown();
						return;
					}
					guiinventoryItem.AttachToCursor(null);
					if (gameObject)
					{
						guiinventoryItem.cgPaperDollCopy = gameObject.GetComponent<CanvasGroup>();
						guiinventoryItem.cgPaperDollCopy.alpha = 0.5f;
					}
				}
			}
		}
	}

	private void OnPointerDown1()
	{
		if (CrewSim.objInstance.contextMenuPool.IsRaised)
		{
			return;
		}
		if (this.cgPaperDoll == null || this.cgPaperDoll.alpha == 0f || !GUIInventory.instance.IsInventoryVisible)
		{
			return;
		}
		if (CrewSim.inventoryGUI.Selected != null)
		{
			return;
		}
		if (this.coUs == null)
		{
			return;
		}
		this._rmbHitCO = null;
		CondOwner rmbHitCO = null;
		GameObject gameObject = null;
		this.CheckHit(ref rmbHitCO, ref gameObject);
		this._rmbHitCO = rmbHitCO;
		if (this._rmbHitCO != null)
		{
			CrewSim.OnRightClick.Invoke(new List<CondOwner>
			{
				this._rmbHitCO
			});
		}
	}

	private void CheckHit(ref CondOwner coSlotted, ref GameObject goPaperDollCopy)
	{
		List<Slot> slotsDepthFirst = this.coUs.compSlots.GetSlotsDepthFirst(true);
		for (int i = slotsDepthFirst.Count - 1; i >= 0; i--)
		{
			Slot slot = slotsDepthFirst[i];
			if (slot != null && !slot.bHide)
			{
				coSlotted = slot.GetOutermostCO();
				if (!(coSlotted == null) && !coSlotted.bSlotLocked)
				{
					foreach (KeyValuePair<string, GameObject> keyValuePair in this.mapCOIDsToGO)
					{
						if (keyValuePair.Key == coSlotted.strID)
						{
							Texture2D img = keyValuePair.Value.GetComponent<RawImage>().texture as Texture2D;
							if (this.AlphaHit(keyValuePair.Value.GetComponent<RectTransform>(), img, Input.mousePosition))
							{
								goPaperDollCopy = keyValuePair.Value;
								break;
							}
						}
					}
					if (goPaperDollCopy != null)
					{
						break;
					}
				}
			}
		}
		if (goPaperDollCopy == null)
		{
			coSlotted = null;
		}
	}

	public bool AlphaHit(RectTransform tf, Texture2D img, Vector3 ptMouse)
	{
		if (img == null)
		{
			return false;
		}
		Vector2 vector;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(tf, ptMouse, CrewSim.objInstance.UICamera, out vector);
		vector.x += tf.rect.width / 2f;
		vector.y += tf.rect.height / 2f;
		if (vector.x < 0f)
		{
			return false;
		}
		if (vector.x > tf.rect.width)
		{
			return false;
		}
		if (vector.y < 0f)
		{
			return false;
		}
		if (vector.y > tf.rect.height)
		{
			return false;
		}
		vector.x *= (float)img.width / tf.rect.width;
		vector.y *= (float)img.height / tf.rect.height;
		int xCenter = MathUtils.RoundToInt(vector.x);
		int yCenter = MathUtils.RoundToInt(vector.y);
		foreach (GUIPaperDollManager.PixelPos pixelPos in this.GetPixelCluster(xCenter, yCenter))
		{
			if (img.GetPixel(pixelPos.x, pixelPos.y).a >= 0.2f)
			{
				return true;
			}
		}
		return false;
	}

	private IEnumerable<GUIPaperDollManager.PixelPos> GetPixelCluster(int xCenter, int yCenter)
	{
		List<GUIPaperDollManager.PixelPos> list = new List<GUIPaperDollManager.PixelPos>();
		for (int i = 2; i >= -2; i--)
		{
			for (int j = -2; j <= 2; j++)
			{
				list.Add(new GUIPaperDollManager.PixelPos(j + xCenter, i + yCenter));
			}
		}
		return list;
	}

	public List<Slot> GetSlotsForScreenPosition(Vector3 screenPosition)
	{
		List<Slot> list = new List<Slot>();
		foreach (GameObject gameObject in this.aSlots)
		{
			if (!(gameObject == null))
			{
				GUIPaperDollHitbox component = gameObject.GetComponent<GUIPaperDollHitbox>();
				if (!(component == null))
				{
					CondOwner coUs = component.coUs;
					if (!(coUs == null))
					{
						Slots component2 = coUs.GetComponent<Slots>();
						if (!(component2 == null))
						{
							Slot slot = component2.GetSlot(component.strSlotName);
							if (slot != null)
							{
								if (!slot.bHide)
								{
									Texture2D img = component.GetComponent<RawImage>().texture as Texture2D;
									if (this.AlphaHit(component.GetComponent<RectTransform>(), img, Input.mousePosition))
									{
										list.Add(slot);
									}
									CondOwner outermostCO = slot.GetOutermostCO();
									if (!(outermostCO == null) && !outermostCO.bSlotLocked && !string.IsNullOrEmpty(outermostCO.strID) && this.mapCOIDsToGO.ContainsKey(outermostCO.strID))
									{
										GameObject gameObject2 = this.mapCOIDsToGO[outermostCO.strID];
										img = (gameObject2.GetComponent<RawImage>().texture as Texture2D);
										if (this.AlphaHit(gameObject2.GetComponent<RectTransform>(), img, Input.mousePosition) && !list.Contains(slot))
										{
											list.Add(slot);
										}
									}
								}
							}
						}
					}
				}
			}
		}
		return list;
	}

	public void ToggleSlotIconsForCO(CondOwner co)
	{
		this.cgTrashIco.alpha = (float)((!(co == null)) ? 1 : 0);
		foreach (GameObject gameObject in this.aSlots)
		{
			bool flag = false;
			if (!(gameObject == null))
			{
				GUIPaperDollHitbox component = gameObject.GetComponent<GUIPaperDollHitbox>();
				if (!(component == null) && !(component.cgIcon == null))
				{
					CondOwner coUs = component.coUs;
					if (coUs != null)
					{
						Slots component2 = coUs.GetComponent<Slots>();
						if (component2 != null)
						{
							Slot slot = component2.GetSlot(component.strSlotName);
							if (slot != null && !slot.bHide && co != null && co.mapSlotEffects.ContainsKey(slot.strName))
							{
								flag = true;
							}
						}
					}
					component.cgIcon.alpha = (float)((!flag) ? 0 : 1);
				}
			}
		}
	}

	public Slot GetSlot(string strSlotName)
	{
		if (this.coUs != null && this.coUs.compSlots != null)
		{
			return this.coUs.compSlots.GetSlot(strSlotName);
		}
		return null;
	}

	public void LogSlotError(string strError)
	{
		this.coUs.LogMessage(strError, "Bad", this.coUs.strID);
	}

	private CondOwner coUs
	{
		get
		{
			if (this._coUsRef != null && this._coUsRef.strID == this.strCOIDLast)
			{
				return this._coUsRef;
			}
			if (!string.IsNullOrEmpty(this.strCOIDLast))
			{
				DataHandler.mapCOs.TryGetValue(this.strCOIDLast, out this._coUsRef);
			}
			return this._coUsRef;
		}
	}

	public string strCOIDLast;

	public Dictionary<string, GameObject> mapCOIDsToGO = new Dictionary<string, GameObject>();

	public CondTrigger ctFilter;

	private Dictionary<string, RawImage> mapCOIDsToGOPain = new Dictionary<string, RawImage>();

	private List<GameObject> aSlots;

	[SerializeField]
	private RawImage bmpBody;

	[SerializeField]
	private RawImage bmpBack;

	[SerializeField]
	private RawImage bmpDrag;

	[SerializeField]
	private RawImage bmpPortrait;

	[SerializeField]
	private Image helmetMask;

	[SerializeField]
	public RectTransform rectTrash;

	[SerializeField]
	public CanvasGroup cgTrashIco;

	[SerializeField]
	public Button btnTrashUndo;

	public CanvasGroup cgBtnTrash;

	private CondOwner coTrash;

	private TextMeshProUGUI txtCount;

	private double fTrashEpoch = 5.0;

	private RectTransform tfPaperDoll;

	private Transform tfSlottedItemRoot;

	[SerializeField]
	private CanvasGroup cgPaperDoll;

	private CondOwner _coUsRef;

	private const float fMinAlpha = 0.2f;

	private CondOwner coTtipSlotted;

	private GameObject goTtipPaperDollCopy;

	private const int _slotReferenceWidth = 310;

	private const int _slotReferenceHeight = 384;

	private CondOwner _rmbHitCO;

	private static bool InitFinished;

	private struct PixelPos
	{
		public PixelPos(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public int x;

		public int y;
	}
}
