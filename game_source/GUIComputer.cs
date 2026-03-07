using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;

// Legacy ship computer terminal UI. Likely handles login, simple text-driven
// home screens, and animated icon widgets for installed ship computers.
public class GUIComputer : GUIData
{
	// Unity setup: binds the login/home widgets, wires button handlers, and
	// initializes the animation bookkeeping tables.
	protected override void Awake()
	{
		base.Awake();
		this.Login = base.transform.Find("MiddleGround/Login");
		this.TimeDisplay = base.transform.Find("MiddleGround/Login/Time/TimeDisplay");
		this.LoginBar = base.transform.Find("MiddleGround/Login/LoginBar");
		this.WelcomeMessage = base.transform.Find("MiddleGround/Login/LoginBar/WelcomeMessage");
		this.LoginBarButton = base.transform.Find("MiddleGround/Login/LoginBar/ButtonOverlay");
		this.PasswordText = base.transform.Find("MiddleGround/Login/LoginBar/PasswordText");
		this.Tick = base.transform.Find("MiddleGround/Login/Tick");
		this.TickShadow = base.transform.Find("MiddleGround/Login/TickShadow");
		this.PowerIcon = base.transform.Find("MiddleGround/Home/PowerIcon");
		this.SleepIcon = base.transform.Find("MiddleGround/Home/SleepIcon");
		this.SearchIcon = base.transform.Find("MiddleGround/Home/SearchIcon");
		this.Home = base.transform.Find("MiddleGround/Home");
		this.TextPanel = base.transform.Find("MiddleGround/Home/TextPanel");
		this.EntryText = base.transform.Find("MiddleGround/Home/TextPanel/GUITextScrollHomebrew/Text");
		this.ContinueButton = base.transform.Find("MiddleGround/Home/TextPanel/ContinueObject/ContinueButton");
		this.ContinueButton.GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.Continue();
		});
		this.Login.gameObject.SetActive(false);
		this.Home.gameObject.SetActive(false);
		this.rectControllers = new Dictionary<RectTransform, GUIComputerAnimController>();
		this.animatingRectControllers = new Dictionary<RectTransform, GUIComputerAnimController>();
		this.animations = new Dictionary<RectTransform, Queue<GUIComputer.TestAnimInfo>>();
		this.LoginBarButton.GetComponent<Button>().onClick.AddListener(new UnityAction(this.LoginToDevice));
		this.PowerIcon.GetComponent<Button>().onClick.AddListener(new UnityAction(this.SaveAndClose));
		this.SleepIcon.GetComponent<Button>().onClick.AddListener(new UnityAction(this.SaveAndClose));
		this.SearchIcon.gameObject.AddComponent<CanvasGroup>();
		this.SearchIcon.GetComponent<Button>().onClick.AddListener(new UnityAction(this.AttachSearch));
		this.searchIconStart = this.SearchIcon.transform.localPosition;
	}

	// Unclear: reserved escape hook for the terminal UI; currently does nothing.
	public void Escape()
	{
	}

	// Starts dragging the search icon so the player can drop it onto a target.
	public void AttachSearch()
	{
		this.heldRect = this.SearchIcon.GetComponent<RectTransform>();
		this.heldRect.GetComponent<CanvasGroup>().blocksRaycasts = false;
		this.heldRect.GetComponent<CanvasGroup>().interactable = false;
		this.heldRect.SetSiblingIndex(base.transform.parent.childCount - 1);
	}

	// Opens the terminal, reloads context, enables the screen post effects, and
	// subscribes the objective tracker to this ship.
	public void Activate()
	{
		Transform transform = base.transform.Find("MiddleGround/Login");
		this.Login.gameObject.SetActive(true);
		transform.gameObject.SetActive(true);
		this.Home.gameObject.SetActive(false);
		this.LoadPlayerSettings();
		this.LoadContext();
		CanvasManager.ShowCanvasGroup(base.transform.gameObject);
		this.bloom = CrewSim.objInstance.UICamera.GetComponent<BloomOptimized>();
		this.noise = CrewSim.objInstance.UICamera.GetComponent<NoiseAndGrain>();
		this.chromatic = CrewSim.objInstance.UICamera.GetComponent<VignetteAndChromaticAberration>();
		this.fisheye = CrewSim.objInstance.UICamera.GetComponent<Fisheye>();
		this.PasswordText.GetComponent<TextMeshProUGUI>().text = string.Empty;
		this.TextPanel.gameObject.SetActive(false);
		this.Login.gameObject.SetActive(true);
		MonoSingleton<ObjectiveTracker>.Instance.AddShipSubscription(this.COSelf.ship.strRegID);
	}

	// Temporarily blanks the UI and disables the screen effects during sleep.
	public void Sleep()
	{
		this.bloom.enabled = false;
		this.noise.enabled = false;
		this.chromatic.enabled = false;
		this.fisheye.enabled = false;
		base.GetComponent<CanvasGroup>().alpha = 0f;
		base.GetComponent<CanvasGroup>().interactable = false;
		base.GetComponent<CanvasGroup>().blocksRaycasts = false;
	}

	// Continues queued icon animations for one rect after the previous anim ends.
	public void NotifyAnimationComplete(RectTransform rect)
	{
		GUIComputerAnimController guicomputerAnimController = this.animatingRectControllers[rect];
		Queue<GUIComputer.TestAnimInfo> queue = null;
		this.animations.TryGetValue(rect, out queue);
		GUIComputer.TestAnimInfo testAnimInfo = null;
		if (queue == null)
		{
			guicomputerAnimController.animState = ComputerAnimState.NONE;
			this.animations.Remove(rect);
			this.animatingRectControllers.Remove(rect);
			return;
		}
		if (queue.Count > 0)
		{
			testAnimInfo = queue.Dequeue();
		}
		if (testAnimInfo == null)
		{
			guicomputerAnimController.animState = ComputerAnimState.NONE;
			queue = null;
			this.animations.Remove(rect);
			this.animatingRectControllers.Remove(rect);
		}
		else
		{
			this.UpdateGraphicAnim(rect, testAnimInfo);
		}
	}

	// Spawns a new animated computer graphic and starts it in the requested state.
	public void CreateNewGraphic(string pngName, ComputerAnimState animState, Vector3 origin, Vector3 destination)
	{
		GameObject gameObject = Resources.Load("prefabGUIComputerElement") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.Home);
		GUIComputerAnimController component = gameObject.GetComponent<GUIComputerAnimController>();
		component.Init(this);
		gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG(pngName + ".png", false, false);
		gameObject.transform.SetParent(this.Home);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (float)gameObject.GetComponent<RawImage>().texture.width);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)gameObject.GetComponent<RawImage>().texture.height);
		gameObject.transform.localPosition = origin;
		component.animState = animState;
		component.destination = destination;
	}

	// Variant that creates a graphic and returns the RectTransform for callers
	// that need to queue additional animations manually.
	public RectTransform CreateNewGraphicReturn(string pngName, Vector3 origin, GUIComputer.TestAnimInfo info)
	{
		GameObject gameObject = Resources.Load("prefabGUIComputerElement") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.Home);
		GUIComputerAnimController component = gameObject.GetComponent<GUIComputerAnimController>();
		component.Init(this);
		gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG(pngName + ".png", false, false);
		gameObject.transform.SetParent(this.Home);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (float)gameObject.GetComponent<RawImage>().texture.width);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)gameObject.GetComponent<RawImage>().texture.height);
		gameObject.transform.localPosition = origin;
		this.ApplyAnimState(component.rect, info);
		return component.rect;
	}

	// Variant that spawns a graphic with an immediate animation state only.
	public RectTransform CreateNewGraphicReturn(string pngName, ComputerAnimState animState, Vector3 origin)
	{
		GameObject gameObject = Resources.Load("prefabGUIComputerElement") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.Home);
		GUIComputerAnimController component = gameObject.GetComponent<GUIComputerAnimController>();
		component.Init(this);
		gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG(pngName + ".png", false, false);
		gameObject.transform.SetParent(this.Home);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (float)gameObject.GetComponent<RawImage>().texture.width);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)gameObject.GetComponent<RawImage>().texture.height);
		gameObject.transform.localPosition = origin;
		component.animState = animState;
		return component.rect;
	}

	// Convenience overload for spawning a one-state animated graphic.
	public void CreateNewGraphic(string pngName, ComputerAnimState animState, Vector3 origin)
	{
		GameObject gameObject = Resources.Load("prefabGUIComputerElement") as GameObject;
		gameObject = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.Home);
		GUIComputerAnimController component = gameObject.GetComponent<GUIComputerAnimController>();
		component.Init(this);
		gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG(pngName + ".png", false, false);
		gameObject.transform.SetParent(this.Home);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (float)gameObject.GetComponent<RawImage>().texture.width);
		component.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)gameObject.GetComponent<RawImage>().texture.height);
		gameObject.transform.localPosition = origin;
		component.animState = animState;
	}

	public void UpdateGraphicAnim(RectTransform rect, GUIComputer.TestAnimInfo info)
	{
		GUIComputerAnimController guicomputerAnimController = this.rectControllers[rect];
		guicomputerAnimController.animState = info.animState;
		guicomputerAnimController.destination = info.destination;
		guicomputerAnimController.moveSpeed = info.moveSpeed;
		guicomputerAnimController.lerpRatio = info.lerpRatio;
	}

	public void PrepareText()
	{
		this.entry = DataHandler.GetEntry("TestLore");
		this.EntryText.gameObject.SetActive(true);
		this.EntryText.GetComponent<TextMeshProUGUI>().text = this.JSONToRichText(this.entry, 0);
		this.entryIndex = 1;
	}

	public string JSONToRichText(JsonComputerEntry entry, int index)
	{
		string text = string.Empty;
		string text2 = entry.strSubEntries[index];
		string[] array = entry.strSubEntries[index].Split(new char[]
		{
			'¬'
		});
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == "italic")
			{
				text += "<i>";
			}
			else if (array[i] == "unitalic")
			{
				text += "</i>";
			}
			else if (array[i] == "bold")
			{
				text += "<b>";
			}
			else if (array[i] == "unbold")
			{
				text += "</b>";
			}
			else if (array[i] == "left")
			{
				text += "<align=\"left\">";
			}
			else if (array[i] == "right")
			{
				text += "<align=\"right\">";
			}
			else if (array[i] == "mleft")
			{
				text += "<margin-left=15em>";
			}
			else if (array[i] == "mright")
			{
				text += "<margin-right=15em>";
			}
			else if (array[i] == "mr")
			{
				text += "<margin-right=0em>";
				text += "<margin-left=0em>";
			}
			else
			{
				text += array[i];
			}
		}
		return text;
	}

	public IEnumerator LoginAnimation()
	{
		float duration = 0f;
		float threshold = UnityEngine.Random.Range(0.02f, 0.04f);
		for (;;)
		{
			if (duration > threshold)
			{
				this.PasswordText.GetComponent<TextMeshProUGUI>().text = this.PasswordText.GetComponent<TextMeshProUGUI>().text + "*";
				duration = 0f;
				threshold = UnityEngine.Random.Range(0.02f, 0.04f);
			}
			duration += Time.deltaTime;
			if (this.PasswordText.GetComponent<TextMeshProUGUI>().text.Length > 35)
			{
				break;
			}
			yield return null;
		}
		CanvasManager.ShowCanvasGroup(this.Tick.gameObject);
		CanvasManager.ShowCanvasGroup(this.TickShadow.gameObject);
		duration = 0.4f;
		while (duration > 0f)
		{
			duration -= Time.deltaTime;
			yield return null;
		}
		this.Login.gameObject.SetActive(false);
		this.Home.gameObject.SetActive(true);
		this.LoadHome();
		yield return null;
		yield break;
	}

	public void QueueAnimation(RectTransform rect, GUIComputer.TestAnimInfo info)
	{
		Queue<GUIComputer.TestAnimInfo> queue = null;
		this.animations.TryGetValue(rect, out queue);
		if (queue == null)
		{
			queue = new Queue<GUIComputer.TestAnimInfo>();
			queue.Enqueue(info);
			this.animations[rect] = queue;
		}
		else
		{
			queue.Enqueue(info);
		}
	}

	public void ApplyAnimState(RectTransform rect, GUIComputer.TestAnimInfo state)
	{
		GUIComputerAnimController guicomputerAnimController = this.rectControllers[rect];
		guicomputerAnimController.animState = state.animState;
		guicomputerAnimController.destination = state.destination;
		guicomputerAnimController.countdown = state.countdown;
		guicomputerAnimController.countdownEnd = state.countdownEnd;
		guicomputerAnimController.moveSpeed = state.moveSpeed;
		guicomputerAnimController.lerpRatio = state.lerpRatio;
	}

	public void LoadHome()
	{
		this.PrepareText();
		this.PasswordText.GetComponent<TextMeshProUGUI>().text = string.Empty;
		GUIComputer.TestAnimInfo testAnimInfo = new GUIComputer.TestAnimInfo();
		testAnimInfo.destination = new Vector3(-300f, 0f);
		testAnimInfo.moveSpeed = 500f;
		testAnimInfo.lerpRatio = 0.12f;
		testAnimInfo.animState = ComputerAnimState.LERPTO;
		RectTransform parent = this.CreateNewGraphicReturn("ComputerIconPDA", Vector3.zero, testAnimInfo);
		GameObject original = Resources.Load("prefabGUIComputerElementText") as GameObject;
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original);
		GUITextDropShadow component = gameObject.GetComponent<GUITextDropShadow>();
		component.textBase.text = this.COOwner.pspec.strFirstName + "'s Tablet";
		component.textShadow.text = this.COOwner.pspec.strFirstName + "'s Tablet";
		gameObject.GetComponent<RectTransform>().transform.SetParent(parent);
		gameObject.GetComponent<RectTransform>().transform.localPosition = new Vector3(0f, -64f);
		gameObject.GetComponent<RectTransform>().transform.localScale = Vector3.one;
		component.textBase.color = new Color32(74, 82, 55, byte.MaxValue);
		GUIComputer.TestAnimInfo testAnimInfo2 = new GUIComputer.TestAnimInfo();
		testAnimInfo2.animState = ComputerAnimState.BLINK;
		RectTransform rectTransform = this.CreateNewGraphicReturn("ComputerIconCable2", Vector3.zero, testAnimInfo2);
		GUIComputer.TestAnimInfo testAnimInfo3 = new GUIComputer.TestAnimInfo();
		GUIComputer.TestAnimInfo testAnimInfo4 = new GUIComputer.TestAnimInfo();
		testAnimInfo3.countdown = 1f;
		testAnimInfo3.animState = ComputerAnimState.COUNTDOWN;
		testAnimInfo3.countdownEnd = CountdownEnd.SHOW;
		testAnimInfo4.animState = ComputerAnimState.LERPTO;
		testAnimInfo4.lerpRatio = 0.12f;
		testAnimInfo4.destination = new Vector3(300f, 0f);
		testAnimInfo4.moveSpeed = 500f;
		RectTransform rectTransform2 = this.CreateNewGraphicReturn("ComputerIconPDA", Vector3.zero, testAnimInfo3);
		rectTransform2.gameObject.AddComponent<Button>();
		rectTransform2.gameObject.GetComponent<Button>().onClick.AddListener(new UnityAction(this.SearchDevice));
		rectTransform2.GetComponent<CanvasGroup>().alpha = 0f;
		this.QueueAnimation(rectTransform2, testAnimInfo4);
		PersonSpec personSpec = new PersonSpec();
		gameObject = UnityEngine.Object.Instantiate<GameObject>(original);
		component = gameObject.GetComponent<GUITextDropShadow>();
		component.textBase.text = personSpec.strFirstName + "'s Tablet";
		component.textShadow.text = personSpec.strFirstName + "'s Tablet";
		gameObject.GetComponent<RectTransform>().transform.SetParent(rectTransform2);
		gameObject.GetComponent<RectTransform>().transform.localPosition = new Vector3(0f, -64f);
		gameObject.GetComponent<RectTransform>().transform.localScale = Vector3.one;
		component.textBase.color = new Color32(74, 82, 55, byte.MaxValue);
		rectTransform.GetComponent<RawImage>().color = new Color32(74, 82, 55, byte.MaxValue);
	}

	public void ClearScreen()
	{
	}

	public void SearchDevice()
	{
		if (this.heldRect != null && this.heldRect.transform == this.SearchIcon)
		{
			this.SearchIcon.GetComponent<CanvasGroup>().interactable = true;
			this.SearchIcon.GetComponent<CanvasGroup>().blocksRaycasts = true;
			this.SearchIcon.transform.localPosition = this.searchIconStart;
			this.heldRect = null;
		}
		foreach (RectTransform rectTransform in this.rectControllers.Keys)
		{
			UnityEngine.Object.Destroy(rectTransform.gameObject);
		}
		this.rectControllers.Clear();
		this.animatingRectControllers.Clear();
		this.animations.Clear();
		this.TextPanel.gameObject.SetActive(true);
		this.EntryText.gameObject.SetActive(true);
	}

	public void Continue()
	{
		if (this.entryIndex > this.entry.strSubEntries.Length - 1)
		{
			return;
		}
		TextMeshProUGUI component = this.EntryText.GetComponent<TextMeshProUGUI>();
		component.text += this.JSONToRichText(this.entry, this.entryIndex);
		this.entryIndex++;
	}

	public void LoginToDevice()
	{
		if (!this.LoggedIn)
		{
			base.StartCoroutine("LoginAnimation");
			this.LoggedIn = true;
		}
		else
		{
			base.StopCoroutine("LoginAnimation");
			this.Login.gameObject.SetActive(false);
			this.Home.gameObject.SetActive(true);
			this.LoadHome();
		}
	}

	public void UpdateTextWithDropShadow(string text, Transform t)
	{
		TextMeshProUGUI component = t.GetComponent<TextMeshProUGUI>();
		TextMeshProUGUI component2 = t.parent.GetChild(t.GetSiblingIndex() - 1).GetComponent<TextMeshProUGUI>();
		component.text = text;
		component2.text = text;
	}

	public void LoadPlayerSettings()
	{
		this.UpdateTextWithDropShadow("Welcome, " + this.COOwner.pspec.strFirstName, this.WelcomeMessage);
	}

	public void LoadContext()
	{
	}

	private void Update()
	{
		this.UpdateTextWithDropShadow(StarSystem.sUTCEpoch, this.TimeDisplay);
		if (this.heldRect != null)
		{
			this.heldRect.transform.localPosition = Input.mousePosition - new Vector3((float)(CrewSim.resolutionX / 2), (float)(CrewSim.resolutionY / 2));
		}
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.Activate();
	}

	public override void SaveAndClose()
	{
		if (this.dictPropMap == null)
		{
			return;
		}
		this.bloom.enabled = false;
		this.noise.enabled = false;
		this.chromatic.enabled = false;
		this.fisheye.enabled = false;
		foreach (RectTransform rectTransform in this.rectControllers.Keys)
		{
			UnityEngine.Object.Destroy(rectTransform.gameObject);
		}
		this.rectControllers.Clear();
		this.animatingRectControllers.Clear();
		this.animations.Clear();
		this.LoggedIn = false;
		base.SaveAndClose();
	}

	private CondOwner COOwner
	{
		get
		{
			if (this._coOwner == null)
			{
				Interaction interactionCurrent = this.COSelf.GetInteractionCurrent();
				if (interactionCurrent != null)
				{
					this._coOwner = interactionCurrent.objThem;
				}
			}
			return this._coOwner;
		}
	}

	private CondOwner _coOwner;

	public Dictionary<RectTransform, GUIComputerAnimController> rectControllers;

	public Dictionary<RectTransform, GUIComputerAnimController> animatingRectControllers;

	public Dictionary<RectTransform, Queue<GUIComputer.TestAnimInfo>> animations;

	public Transform Login;

	public Transform TimeDisplay;

	public Transform LoginBar;

	public Transform WelcomeMessage;

	public Transform LoginBarButton;

	public Transform PasswordText;

	public Transform Tick;

	public Transform TickShadow;

	public Transform PowerIcon;

	public Transform SleepIcon;

	public Transform SearchIcon;

	public Transform Home;

	public Transform TextPanel;

	public Transform EntryText;

	public Transform ContinueButton;

	public BloomOptimized bloom;

	public NoiseAndGrain noise;

	public VignetteAndChromaticAberration chromatic;

	public Fisheye fisheye;

	public bool LoggedIn;

	public RectTransform heldRect;

	public Vector3 searchIconStart;

	private JsonComputerEntry entry;

	private int entryIndex;

	public class TestAnimInfo
	{
		public TestAnimInfo()
		{
			this.destination = Vector3.zero;
			this.animState = ComputerAnimState.NONE;
			this.moveSpeed = 0f;
			this.lerpRatio = 0f;
			this.countdownEnd = CountdownEnd.NONE;
		}

		public Vector3 destination;

		public ComputerAnimState animState;

		public float moveSpeed;

		public float lerpRatio;

		public float countdown;

		public CountdownEnd countdownEnd;
	}
}
