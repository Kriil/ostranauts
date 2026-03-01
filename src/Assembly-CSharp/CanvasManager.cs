using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.UI.CrewBar;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Central UI canvas orchestrator for the CrewSim scene.
// This caches the named canvases, controls visibility/state transitions, and
// keeps layered HUD panels in a consistent stack order.
public class CanvasManager : MonoBehaviour
{
	// Exposes the current high-level GUI state machine.
	public CanvasManager.GUIState NState
	{
		get
		{
			return this.nState;
		}
	}

	// True while the canvas stack is fading between major states.
	public bool IsTransitioning
	{
		get
		{
			return this.bFadingIn || this.bFadingOut;
		}
	}

	// Resolves and caches all scene canvases, then hides them until a mode is activated.
	public void Init()
	{
		this.goCanvasGUI = GameObject.Find("Canvas GUI");
		this.goCanvasWorld = GameObject.Find("Canvas World");
		this.goCanvasWorldGUI = GameObject.Find("Canvas World GUI");
		this.canvasStackHolder = GameObject.Find("Canvas Stack");
		this.prefabCanvas = (Resources.Load("prefabCanvas") as GameObject);
		this.goCanvasContextMenu = UnityEngine.Object.Instantiate<GameObject>(this.prefabCanvas, this.canvasStackHolder.transform);
		this.goCanvasContextMenu.GetComponent<Canvas>().worldCamera = CrewSim.objInstance.UICamera;
		this.goCanvasContextMenu.GetComponent<Canvas>().planeDistance = 20f;
		this.goCanvasContextMenu.name = "Canvas Context Menu";
		this.goCanvasJobMenu = this.canvasStackHolder.transform.Find("Canvas Jobs").gameObject;
		this.goCanvasCrewBar = this.canvasStackHolder.transform.Find("Canvas Crew Bar").gameObject;
		this.goCanvasShipEdit = this.canvasStackHolder.transform.Find("Canvas ShipEdit").gameObject;
		this.goCanvasObjectiveMenu = this.canvasStackHolder.transform.Find("Canvas Objectives").gameObject;
		this.goCanvasSocialCombat = this.canvasStackHolder.transform.Find("Canvas Social Combat 2").gameObject;
		this.goCanvasPDA = this.canvasStackHolder.transform.Find("Canvas PDA").gameObject;
		this.goCanvasInventory = this.canvasStackHolder.transform.Find("Canvas Inventory").gameObject;
		this.goCanvasDebug = this.canvasStackHolder.transform.Find("Canvas Debug").gameObject;
		this.goCanvasQuit = this.canvasStackHolder.transform.Find("Canvas Quit").gameObject;
		this.goCanvasBlack = this.canvasStackHolder.transform.Find("Canvas Black").gameObject;
		this.goCanvasGameOver = this.canvasStackHolder.transform.Find("Canvas Game Over").gameObject;
		this.goCanvasFloaties = this.canvasStackHolder.transform.Find("Canvas Floaties").gameObject;
		this.goCanvasHelmet = this.canvasStackHolder.transform.Find("Canvas Helmet").gameObject;
		this.goCanvasControlPanels = this.canvasStackHolder.transform.Find("Canvas Control Panels").gameObject;
		this.goCanvasParallax = this.canvasStackHolder.transform.Find("Canvas Parallax").gameObject;
		this.helmet = this.goCanvasHelmet.GetComponent<GUIHelmet>();
		this.goCanvasContextMenu.GetComponent<Canvas>().sortingOrder = this.goCanvasInventory.GetComponent<Canvas>().sortingOrder + 1;
		this.rectControlPanels = this.goCanvasControlPanels.transform.Find("pnlInteractionNav").GetComponent<RectTransform>();
		this.vControlPanelRestPos = this.rectControlPanels.localPosition;
		this.canvasStack.Add(this.goCanvasObjectiveMenu);
		this.canvasStack.Add(this.goCanvasContextMenu);
		this.canvasStack.Add(this.goCanvasJobMenu);
		this.canvasStack.Add(this.goCanvasCrewBar);
		this.canvasStack.Add(this.goCanvasShipEdit);
		this.canvasStack.Add(this.goCanvasGUI);
		this.canvasStack.Add(this.goCanvasControlPanels);
		this.canvasStack.Add(this.goCanvasWorld);
		this.canvasStack.Add(this.goCanvasWorldGUI);
		this.canvasStack.Add(this.goCanvasSocialCombat);
		this.canvasStack.Add(this.goCanvasPDA);
		this.canvasStack.Add(this.goCanvasInventory);
		this.canvasStack.Add(this.goCanvasQuit);
		this.canvasStack.Add(this.goCanvasGameOver);
		this.canvasStack.Add(this.goCanvasBlack);
		this.canvasStack.Add(this.goCanvasFloaties);
		this.canvasStack.Add(this.goCanvasHelmet);
		this.canvasStack.Add(this.goCanvasDebug);
		this.canvasStack.Add(this.goCanvasParallax);
		CanvasManager.canvasScaler = this.goCanvasGUI.GetComponent<CanvasScaler>();
		for (int i = 0; i < this.canvasStack.Count; i++)
		{
			this.canvasStack[i].transform.SetParent(this.canvasStackHolder.transform);
			this.canvasStack[i].GetComponent<CanvasGroup>().alpha = 0f;
			this.canvasStack[i].GetComponent<CanvasGroup>().interactable = false;
			this.canvasStack[i].GetComponent<CanvasGroup>().blocksRaycasts = false;
		}
		this.CrewSimNormal();
	}

	// Helper for pointer-over-UI checks using the CrewSim UI camera.
	public static bool IsOverUIElement(GameObject go)
	{
		RectTransform component = go.GetComponent<RectTransform>();
		return component != null && RectTransformUtility.RectangleContainsScreenPoint(component, Input.mousePosition, CrewSim.objInstance.UICamera);
	}

	// Unclear: this currently always returns 1, so any real scaling logic may have been optimized out or lost in decompilation.
	public static float CanvasRatio
	{
		get
		{
			if (CanvasManager.canvasScaler == null)
			{
				CanvasManager.canvasScaler = UnityEngine.Object.FindObjectOfType<CanvasScaler>();
			}
			if (CanvasManager.canvasScaler)
			{
				return 1f;
			}
			return 1f;
		}
	}

	// Hide helpers used by most UI systems to disable alpha, input, and raycasts.
	public static void HideCanvasGroup(GameObject gameObject)
	{
		if (gameObject == null)
		{
			return;
		}
		CanvasGroup component = gameObject.GetComponent<CanvasGroup>();
		CanvasManager.CleanupDropDownBlockers(gameObject);
		CanvasManager.HideCanvasGroup(component);
	}

	public static void HideCanvasGroup(RectTransform rect)
	{
		if (rect == null)
		{
			return;
		}
		CanvasGroup component = rect.GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(component);
	}

	public static void HideCanvasGroup(CanvasGroup cg)
	{
		if (cg == null)
		{
			return;
		}
		cg.alpha = 0f;
		cg.interactable = false;
		cg.blocksRaycasts = false;
		GraphicRaycaster component = cg.GetComponent<GraphicRaycaster>();
		if (component != null)
		{
			component.enabled = false;
		}
	}

	// Show helpers paired with the hide overloads above.
	public static void ShowCanvasGroup(GameObject gameObject)
	{
		if (gameObject == null)
		{
			return;
		}
		CanvasGroup component = gameObject.GetComponent<CanvasGroup>();
		CanvasManager.ShowCanvasGroup(component);
	}

	public static void ShowCanvasGroup(RectTransform rect)
	{
		if (rect == null)
		{
			return;
		}
		CanvasGroup component = rect.GetComponent<CanvasGroup>();
		CanvasManager.ShowCanvasGroup(component);
	}

	// Tracks whether the inventory canvas should be included in the visible set.
	public void ToggleInventoryVisibility(bool show)
	{
		if (show && !this.CanvasVisible.Contains(this.goCanvasInventory))
		{
			this.CanvasVisible.Add(this.goCanvasInventory);
		}
		else
		{
			this.CanvasVisible.Remove(this.goCanvasInventory);
		}
	}

	public static void ShowCanvasGroup(CanvasGroup cg)
	{
		if (cg == null)
		{
			return;
		}
		cg.alpha = 1f;
		cg.interactable = true;
		cg.blocksRaycasts = true;
		GraphicRaycaster component = cg.GetComponent<GraphicRaycaster>();
		if (component != null)
		{
			component.enabled = true;
		}
	}

	// Global quit-dialog visibility check.
	public static bool IsCanvasQuitShowing()
	{
		return !(CanvasManager.instance == null) && !(CanvasManager.instance.goCanvasQuit == null) && CanvasManager.instance.goCanvasQuit.GetComponent<CanvasGroup>().alpha > 0f;
	}

	public static void ShowCanvasQuit()
	{
		if (CanvasManager.IsCanvasQuitShowing())
		{
			return;
		}
		string[] array = new string[]
		{
			"GUIQuit/pnlBG/btnCancel",
			"GUIQuit/pnlBG/pnlMain/btnOptions",
			"GUIQuit/pnlBG/pnlMain/btnSave",
			"GUIQuit/pnlBG/pnlMain/btnQuit"
		};
		foreach (string name in array)
		{
			Button component = CanvasManager.instance.goCanvasQuit.transform.Find(name).GetComponent<Button>();
			if (component == null)
			{
				Debug.Log("Button not found:" + component.name);
			}
			else
			{
				component.interactable = false;
				component.interactable = true;
			}
		}
		CanvasGroup component2 = CanvasManager.instance.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlMain/btnSave").GetComponent<CanvasGroup>();
		if (CrewSim.bShipEdit)
		{
			CanvasManager.HideCanvasGroup(component2);
		}
		else
		{
			CanvasManager.ShowCanvasGroup(component2);
		}
		CrewSim.objGUISaveIndicator.EstablishSave(false);
		CanvasManager.instance.bPreviousPaused = CrewSim.Paused;
		CrewSim.Paused = true;
		CrewSim.bPauseLock = true;
		CanvasManager.ShowCanvasGroup(CanvasManager.instance.goCanvasQuit);
		AudioManager.am.PlayAudioEmitter("UIEscOpen", false, false);
	}

	public void HideCanvasQuit()
	{
		if (!CanvasManager.IsCanvasQuitShowing())
		{
			return;
		}
		CanvasManager.HideCanvasGroup(CanvasManager.instance.goCanvasQuit);
		CrewSim.Paused = CanvasManager.instance.bPreviousPaused;
		CrewSim.bPauseLock = false;
		AudioManager.am.PlayAudioEmitter("UIEscClose", false, false);
	}

	public static void ToggleCanvasQuit()
	{
		if (CanvasManager.instance.bFadingIn || CanvasManager.instance.bFadingOut)
		{
			return;
		}
		if (CanvasManager.IsCanvasQuitShowing())
		{
			CanvasManager.instance.HideCanvasQuit();
		}
		else
		{
			CanvasManager.ShowCanvasQuit();
		}
	}

	public static void SetAnchorsToCorners(Transform tf)
	{
		CanvasManager.SetAnchorsToCorners(tf.GetComponent<RectTransform>());
	}

	public static void SetAnchorsToCorners(RectTransform rect)
	{
		RectTransform component = rect.parent.GetComponent<RectTransform>();
		if (rect == null || component == null)
		{
			return;
		}
		Vector2 anchorMin = new Vector2(rect.anchorMin.x + rect.offsetMin.x / component.rect.width, rect.anchorMin.y + rect.offsetMin.y / component.rect.height);
		Vector2 anchorMax = new Vector2(rect.anchorMax.x + rect.offsetMax.x / component.rect.width, rect.anchorMax.y + rect.offsetMax.y / component.rect.height);
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		Vector2 vector = new Vector2(0f, 0f);
		rect.offsetMax = vector;
		rect.offsetMin = vector;
	}

	public static void SetCornersToAnchors(RectTransform rect)
	{
		Vector2 vector = new Vector2(0f, 0f);
		rect.offsetMax = vector;
		rect.offsetMin = vector;
	}

	public void CanvasShake(Vector3 vShake, float fShakeAmp)
	{
		this.rectControlPanels.localPosition = new Vector3(this.vControlPanelRestPos.x + vShake.x * fShakeAmp, this.vControlPanelRestPos.y + vShake.y * fShakeAmp, this.vControlPanelRestPos.z);
	}

	private void FadeUpCanvases(IEnumerator routine)
	{
		if (this._canvasCoroutine != null)
		{
			base.StopCoroutine(this._canvasCoroutine);
			this._canvasCoroutine = null;
		}
		this._canvasCoroutine = base.StartCoroutine(routine);
	}

	private IEnumerator FadeUpSocialCanvasesSpecial(float duration)
	{
		this.bFadingIn = true;
		float timePassed = 0f;
		List<CanvasGroup> CGsIn = new List<CanvasGroup>();
		List<CanvasGroup> CGsOut = new List<CanvasGroup>();
		List<float> aCGAlphasIn = new List<float>();
		List<float> aCGAlphasOut = new List<float>();
		AudioManager.am.PlayAudioEmitter("UISocializeEnter", false, false);
		for (int i = 0; i < this.canvasStack.Count; i++)
		{
			CanvasGroup component = this.canvasStack[i].GetComponent<CanvasGroup>();
			if (component != null)
			{
				if (this.CanvasVisible.Contains(this.canvasStack[i]))
				{
					component.interactable = true;
					component.blocksRaycasts = true;
					if (!(this.canvasStack[i] == this.goCanvasSocialCombat))
					{
						CGsIn.Add(component);
						aCGAlphasIn.Add(component.alpha);
					}
				}
				else
				{
					CGsOut.Add(component);
					aCGAlphasOut.Add(component.alpha);
					component.interactable = false;
					component.blocksRaycasts = false;
				}
			}
		}
		while (timePassed < duration)
		{
			timePassed += Time.unscaledDeltaTime;
			float blend = Mathf.Clamp01(timePassed / duration);
			for (int j = 0; j < CGsIn.Count; j++)
			{
				CGsIn[j].alpha = Mathf.Lerp(aCGAlphasIn[j], 1f, blend);
			}
			for (int k = 0; k < CGsOut.Count; k++)
			{
				CGsOut[k].alpha = Mathf.Lerp(aCGAlphasOut[k], 0f, blend);
			}
			yield return null;
		}
		timePassed = 0f;
		while (timePassed < duration)
		{
			timePassed += Time.unscaledDeltaTime * 0.65f;
			yield return null;
		}
		timePassed = 0f;
		CanvasGroup canvasGroup = this.goCanvasSocialCombat.GetComponent<CanvasGroup>();
		while (timePassed < duration)
		{
			timePassed += Time.unscaledDeltaTime;
			float blend2 = Mathf.Clamp01(timePassed / duration);
			canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, blend2);
			yield return null;
		}
		this.UpdateRaycasters();
		this.bFadingIn = false;
		this._canvasCoroutine = null;
		yield break;
	}

	private IEnumerator FadeUpAllCanvases(float duration)
	{
		this.bFadingIn = true;
		float timePassed = 0f;
		List<CanvasGroup> CGsIn = new List<CanvasGroup>();
		List<CanvasGroup> CGsOut = new List<CanvasGroup>();
		List<float> aCGAlphasIn = new List<float>();
		List<float> aCGAlphasOut = new List<float>();
		for (int i = 0; i < this.canvasStack.Count; i++)
		{
			CanvasGroup component = this.canvasStack[i].GetComponent<CanvasGroup>();
			if (!(component == null))
			{
				if (this.CanvasVisible.Contains(this.canvasStack[i]))
				{
					CGsIn.Add(component);
					aCGAlphasIn.Add(component.alpha);
					component.interactable = true;
					component.blocksRaycasts = true;
				}
				else
				{
					CGsOut.Add(component);
					aCGAlphasOut.Add(component.alpha);
					component.interactable = false;
					component.blocksRaycasts = false;
				}
			}
		}
		while (timePassed < duration)
		{
			timePassed += Time.unscaledDeltaTime;
			float blend = Mathf.Clamp01(timePassed / duration);
			for (int j = 0; j < CGsIn.Count; j++)
			{
				CGsIn[j].alpha = Mathf.Lerp(aCGAlphasIn[j], 1f, blend);
			}
			for (int k = 0; k < CGsOut.Count; k++)
			{
				CGsOut[k].alpha = Mathf.Lerp(aCGAlphasOut[k], 0f, blend);
			}
			yield return null;
		}
		this.UpdateRaycasters();
		this.bFadingIn = false;
		this._canvasCoroutine = null;
		yield break;
	}

	private IEnumerator FadeDownAllCanvases(float duration)
	{
		this.bFadingOut = true;
		float timePassed = 0f;
		List<CanvasGroup> CGs = new List<CanvasGroup>();
		List<float> aCGAlphas = new List<float>();
		for (int i = 0; i < this.canvasStack.Count; i++)
		{
			CanvasGroup component = this.canvasStack[i].GetComponent<CanvasGroup>();
			if (component != null)
			{
				if (this.CanvasVisible.Contains(this.canvasStack[i]))
				{
					component.interactable = true;
					component.blocksRaycasts = true;
				}
				else
				{
					CGs.Add(component);
					aCGAlphas.Add(component.alpha);
					component.interactable = false;
					component.blocksRaycasts = false;
				}
			}
		}
		while (timePassed < duration)
		{
			timePassed += Time.unscaledDeltaTime;
			float blend = Mathf.Clamp01(timePassed / duration);
			for (int j = 0; j < CGs.Count; j++)
			{
				CGs[j].alpha = Mathf.Lerp(aCGAlphas[j], 0f, blend);
			}
			yield return null;
		}
		if (GUIInventory.instance.activeWindows.Count != 0)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
		this.UpdateRaycasters();
		this.bFadingOut = false;
		this._canvasCoroutine = null;
		yield break;
	}

	public void SocialCombat(CondOwner coUs, CondOwner coThem, bool fDelayedPause)
	{
		if (this.nState == CanvasManager.GUIState.SOCIAL)
		{
			return;
		}
		this.nState = CanvasManager.GUIState.SOCIAL;
		Debug.Log("Setting Canvases to " + this.nState.ToString());
		if (CrewSim.inventoryGUI.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), true);
		}
		this.CanvasVisible.Clear();
		this.CanvasVisible.Add(this.goCanvasLetterboxes);
		this.CanvasVisible.Add(this.goCanvasSocialCombat);
		this.CanvasVisible.Add(this.goCanvasHelmet);
		this.CanvasVisible.Add(this.goCanvasPDA);
		this.CanvasVisible.Add(this.goCanvasFloaties);
		this.CanvasVisible.Add(this.goCanvasCrewBar);
		this.CanvasVisible.Add(this.goCanvasParallax);
		this.goCanvasSocialCombat.GetComponent<GUISocialCombat2>().SetData(coUs, coThem, fDelayedPause, null);
		this.FadeUpCanvases(this.FadeUpSocialCanvasesSpecial(0.5f));
		CrewSim.objInstance.LetterboxBottom.AnimateIn();
		CrewSim.objInstance.LetterboxTop.AnimateIn();
		CrewSim.objInstance.camMain.GetComponent<CameraFocusZoom>().FocusOnPlayer();
	}

	public void CrewSimNormal()
	{
		if (this.nState == CanvasManager.GUIState.GAMEOVER)
		{
			return;
		}
		this.nState = CanvasManager.GUIState.NORMAL;
		Debug.Log("#Info# Setting Canvases to " + this.nState.ToString());
		this.CanvasVisible.RemoveAll((GameObject x) => x != this.goCanvasInventory);
		this.CanvasVisible.Add(this.goCanvasWorld);
		this.CanvasVisible.Add(this.goCanvasWorldGUI);
		this.CanvasVisible.Add(this.goCanvasContextMenu);
		this.CanvasVisible.Add(this.goCanvasGUI);
		this.CanvasVisible.Add(this.goCanvasHelmet);
		this.CanvasVisible.Add(this.goCanvasObjectiveMenu);
		this.CanvasVisible.Add(this.goCanvasJobMenu);
		this.CanvasVisible.Add(this.goCanvasPDA);
		this.CanvasVisible.Add(this.goCanvasFloaties);
		this.CanvasVisible.Add(this.goCanvasCrewBar);
		this.CanvasVisible.Add(this.goCanvasParallax);
		if (GUIInventory.instance.NeedsRestore())
		{
			this.CanvasVisible.Add(this.goCanvasInventory);
		}
		MonoSingleton<GUICrewStatus>.Instance.Show();
		this.FadeUpCanvases(this.FadeUpAllCanvases(0.5f));
		this.ShowOrbits(false);
		if (CrewSim.objInstance.camMain != null)
		{
			CrewSim.objInstance.camMain.GetComponent<CameraFocusZoom>().Unfocus();
		}
		if (GUIInventory.instance.NeedsRestore() && !GUIInventory.instance.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
	}

	public void HideUI()
	{
		this.nState = CanvasManager.GUIState.HIDE_UI;
		Debug.Log("Setting Canvases to " + this.nState.ToString());
		if (CrewSim.inventoryGUI.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
		this.CanvasVisible.Clear();
		this.CanvasVisible.Add(this.goCanvasWorld);
		this.CanvasVisible.Add(this.goCanvasHelmet);
		this.CanvasVisible.Add(this.goCanvasParallax);
		this.FadeUpCanvases(this.FadeUpAllCanvases(0.5f));
		this.ShowOrbits(false);
		if (CrewSim.objInstance.camMain != null)
		{
			CrewSim.objInstance.camMain.GetComponent<CameraFocusZoom>().Unfocus();
		}
	}

	public void ShipGUI(List<GameObject> aSkip = null)
	{
		if (this.nState == CanvasManager.GUIState.SHIPGUI)
		{
			return;
		}
		this.nState = CanvasManager.GUIState.SHIPGUI;
		Debug.Log("#Info# Setting Canvases to " + this.nState.ToString());
		if (CrewSim.inventoryGUI.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), true);
		}
		this.CanvasVisible.Clear();
		this.CanvasVisible.Add(this.goCanvasWorld);
		this.CanvasVisible.Add(this.goCanvasWorldGUI);
		this.CanvasVisible.Add(this.goCanvasGUI);
		this.CanvasVisible.Add(this.goCanvasControlPanels);
		this.CanvasVisible.Add(this.goCanvasParallax);
		if (!CrewSim.bShipEdit)
		{
			this.CanvasVisible.Add(this.goCanvasPDA);
			this.CanvasVisible.Add(this.goCanvasObjectiveMenu);
			this.CanvasVisible.Add(this.goCanvasJobMenu);
			this.CanvasVisible.Add(this.goCanvasContextMenu);
			this.CanvasVisible.Add(this.goCanvasHelmet);
			this.CanvasVisible.Add(this.goCanvasFloaties);
			this.CanvasVisible.Add(this.goCanvasCrewBar);
		}
		if (aSkip != null)
		{
			foreach (GameObject item in aSkip)
			{
				this.CanvasVisible.Remove(item);
			}
		}
		this.FadeUpCanvases(this.FadeUpAllCanvases(0.5f));
	}

	public void GameOver(CondOwner coDead)
	{
		if (this.nState == CanvasManager.GUIState.GAMEOVER)
		{
			return;
		}
		this.nState = CanvasManager.GUIState.GAMEOVER;
		Debug.Log("Setting Canvases to " + this.nState.ToString());
		if (CrewSim.inventoryGUI.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
		this.CanvasVisible.Clear();
		this.CanvasVisible.Add(this.goCanvasWorld);
		this.CanvasVisible.Add(this.goCanvasWorldGUI);
		this.CanvasVisible.Add(this.goCanvasGUI);
		this.CanvasVisible.Add(this.goCanvasGameOver);
		this.CanvasVisible.Add(this.goCanvasParallax);
		if (!CrewSim.bShipEdit)
		{
			this.CanvasVisible.Add(this.goCanvasHelmet);
			this.CanvasVisible.Add(this.goCanvasFloaties);
			this.CanvasVisible.Add(this.goCanvasCrewBar);
		}
		TMP_Text component = this.goCanvasGameOver.transform.Find("GUIGameOver/prefabTextScroll/txt").GetComponent<TMP_Text>();
		List<string> list = new List<string>();
		if (coDead != null)
		{
			component.text = coDead.GetMessageLog(20);
			foreach (Condition condition in coDead.mapConds.Values)
			{
				if (condition.bFatal)
				{
					list.Add(condition.strName);
				}
			}
		}
		CrewSim.objInstance.camMain.GetComponent<CameraFocusZoom>().FocusOnPlayer();
		AudioManager.am.SuggestMusic("GameOver", true);
		CrewSim.QueuePause(true, 2f);
		this.FadeUpCanvases(this.FadeUpAllCanvases(0.5f));
	}

	public void Black()
	{
		if (this.nState == CanvasManager.GUIState.BLACK)
		{
			return;
		}
		this.nState = CanvasManager.GUIState.BLACK;
		Debug.Log("#Info# Setting Canvases to " + this.nState.ToString());
		if (CrewSim.inventoryGUI.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
		this.CanvasVisible.Clear();
		this.CanvasVisible.Add(this.goCanvasBlack);
		this.FadeUpCanvases(this.FadeUpAllCanvases(0.5f));
	}

	public void ShipEdit()
	{
		if (this.nState == CanvasManager.GUIState.SHIPEDIT)
		{
			return;
		}
		this.nState = CanvasManager.GUIState.SHIPEDIT;
		if (CrewSim.inventoryGUI.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
		this.CanvasVisible.Clear();
		this.CanvasVisible.Add(this.goCanvasWorld);
		this.CanvasVisible.Add(this.goCanvasWorldGUI);
		this.CanvasVisible.Add(this.goCanvasGUI);
		this.CanvasVisible.Add(this.goCanvasShipEdit);
		this.CanvasVisible.Add(this.goCanvasParallax);
		MonoSingleton<GUICrewStatus>.Instance.Hide();
		this.FadeUpCanvases(this.FadeUpAllCanvases(0.5f));
	}

	private void UpdateRaycasters()
	{
		for (int i = 0; i < this.canvasStack.Count; i++)
		{
			GraphicRaycaster component = this.canvasStack[i].GetComponent<GraphicRaycaster>();
			if (!(component == null))
			{
				CanvasGroup component2 = this.canvasStack[i].GetComponent<CanvasGroup>();
				if (!(component2 == null))
				{
					component.enabled = (component2.alpha > 0f);
				}
			}
		}
	}

	public static void CleanupDropDownBlockers(GameObject goCaller)
	{
		if (goCaller == null)
		{
			return;
		}
		Canvas componentInParent = goCaller.transform.GetComponentInParent<Canvas>();
		if (componentInParent == null)
		{
			return;
		}
		IEnumerator enumerator = componentInParent.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				if (!(transform.name != "Blocker"))
				{
					UnityEngine.Object.Destroy(transform.gameObject);
					break;
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
	}

	public void ShowOrbits(bool bShow)
	{
		MonoSingleton<GUIRenderTargets>.Instance.ShowOrbits(bShow);
	}

	public CanvasManager.GUIState State
	{
		get
		{
			return this.nState;
		}
	}

	public static CanvasManager instance;

	public List<GameObject> canvasStack = new List<GameObject>();

	public GameObject goCanvasWorld;

	public GameObject goCanvasWorldGUI;

	public GameObject goCanvasGUI;

	public GameObject prefabCanvas;

	public GameObject goCanvasContextMenu;

	public GameObject goCanvasJobMenu;

	public GameObject goCanvasShipEdit;

	public GameObject goCanvasCrewBar;

	public GameObject goCanvasControlPanels;

	public GameObject goCanvasObjectiveMenu;

	public GameObject goCanvasSocialCombat;

	public GameObject goCanvasLetterboxes;

	public GameObject goCanvasPDA;

	public GameObject goCanvasInventory;

	public GameObject goCanvasDebug;

	public GameObject goCanvasQuit;

	public GameObject goCanvasGameOver;

	public GameObject goCanvasBlack;

	public GameObject goCanvasFloaties;

	public GameObject goCanvasHelmet;

	public GameObject goCanvasParallax;

	private RectTransform rectControlPanels;

	private Vector3 vControlPanelRestPos;

	public GameObject guiLetterboxTop;

	public GameObject guiLetterboxBottom;

	public GameObject canvasStackHolder;

	public List<GameObject> CanvasVisible = new List<GameObject>();

	public List<GameObject> CanvasNotVisible = new List<GameObject>();

	public static CanvasScaler canvasScaler;

	private CanvasManager.GUIState nState = CanvasManager.GUIState.LOADING;

	private bool bPreviousPaused;

	private bool bFadingIn;

	private bool bFadingOut;

	private Coroutine _canvasCoroutine;

	public GUIHelmet helmet;

	public enum GUIState
	{
		SOCIAL,
		NORMAL,
		LOADING,
		SHIPGUI,
		SHIPEDIT,
		GAMEOVER,
		BLACK,
		HIDE_UI
	}
}
