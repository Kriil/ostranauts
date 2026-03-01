using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Ostranauts;
using Ostranauts.Condowner;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Objectives;
using Ostranauts.Racing;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships;
using Ostranauts.Social.Models;
using Ostranauts.TargetVisualization;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using Ostranauts.UI.CrewBar;
using Ostranauts.UI.Loading;
using Ostranauts.UI.MegaToolTip;
using Ostranauts.UI.Quickbar.Models;
using Ostranauts.UI.ShipEdit;
using Ostranauts.Utils;
using Ostranauts.Utils.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vectrosity;

// Main in-scene gameplay controller for the CrewSim scene.
// This MonoBehaviour appears to bootstrap cameras, UI, selection, visibility,
// ship editing, and the central runtime references used by most systems.
public class CrewSim : MonoBehaviour
{
	// Screen shake amplitude after applying the player's preference multiplier.
	private float fShakeAmp
	{
		get
		{
			return this.fShakeRaw * this.fShakeUserPref;
		}
		set
		{
			this.fShakeRaw = value;
		}
	}

	// Becomes true once the large scene/bootstrap load path has finished.
	public bool FinishedLoading
	{
		get
		{
			return this._finishedLoading;
		}
	}

	// Switches between the normal gameplay camera and the screenshot camera.
	public Camera SwitchActiveCamera(bool useMainCam)
	{
		if (useMainCam)
		{
			this.ActiveCam = this.camMain;
			this.ScreenShotCam.gameObject.SetActive(false);
		}
		else
		{
			this.ScreenShotCam.gameObject.SetActive(true);
			this.ActiveCam = this.ScreenShotCam;
		}
		return this.ActiveCam;
	}

	// Unity startup entrypoint for the main gameplay scene.
	// This wires up shared singletons, loads cursor/UI assets, creates core
	// helper components, and prepares the scene for the longer content load.
	private void Awake()
	{
		if (CrewSim.OnTileSelectionUpdated == null)
		{
			CrewSim.OnTileSelectionUpdated = new OnTileSelectionEvent();
		}
		GameObject gameObject = GameObject.Find("Canvas LoadFail (DeleteOnLoad)");
		if (gameObject != null)
		{
			this.LoadFailure = gameObject.GetComponent<CanvasGroup>();
		}
		if (this.LoadFailure != null)
		{
			CanvasManager.ShowCanvasGroup(this.LoadFailure);
		}
		if (Application.isEditor)
		{
			CrewSim.bEnableDebugCommands = true;
		}
		CrewSim.aCursors = new Texture2D[3];
		CrewSim.aCursors[0] = DataHandler.LoadPNG("GUICursor01.png", false, false);
		CrewSim.aCursors[1] = DataHandler.LoadPNG("GUICursor01Feet.png", false, false);
		CrewSim.aCursors[2] = DataHandler.LoadPNG("GUICursor01Hand.png", false, false);
		this.SetCursor(0);
		AudioManager.am.FadeOutMusic(2f);
		CrewSim.objInstance = this;
		CrewSim.bShipEditTest = false;
		CrewSim.aLoadedShips = new List<Ship>();
		CrewSim.strDebugOut = string.Empty;
		CrewSim.coPlayer = null;
		CrewSim.fTotalGameSec = 0f;
		CrewSim.fTotalGameSecUnscaled = 0f;
		CrewSim.fTotalGameSecSession = 0f;
		this.nLastClickIndex = 0;
		this.vLastClick = new Vector2(1000f, 1000f);
		CrewSim.ResetTimeScale();
		CrewSim.fTimeCoeffPause = 1f;
		this.aHidden = new List<string>();
		this.aFields = new List<GameObject>();
		CrewSim.aSelected = new List<CondOwner>();
		CrewSim.blocks = new HashSet<Block>();
		CrewSim.goSun = new GameObject("LightsSuns");
		CrewSim.goSun.transform.SetParent(GameObject.Find("PlayState").transform, false);
		CrewSim.aLights = new HashSet<global::Visibility>();
		CrewSim.aTickers = new UniqueList<CondOwner>();
		CrewSim.aTickersTemp = new List<CondOwner>();
		CrewSim.pathfinders = new List<Pathfinder>();
		CrewSim.tplAutoPause = new Tuple<double, string>();
		this.coDicts = base.gameObject.AddComponent<CODicts>();
		Debug.Log("Initializing Crewsim Canvasmanager");
		this.UICamera = GameObject.Find("UI Camera").GetComponent<Camera>();
		this.vhs = this.UICamera.GetComponent<VHSPostProcessEffect>();
		if (this.vhs != null)
		{
			this.vhs.enabled = false;
		}
		CrewSim.CanvasManager = base.gameObject.AddComponent<CanvasManager>();
		CanvasManager.instance = CrewSim.CanvasManager;
		CrewSim.CanvasManager.Init();
		this.fShakeUserPref = PlayerPrefs.GetFloat("ScreenShakeMod", 1f);
		Debug.Log("Initializing Crewsim inventory gui");
		CrewSim.inventoryGUI = CrewSim.CanvasManager.goCanvasInventory.GetComponent<GUIInventory>();
		Debug.Log("Initializing Crewsim visibility");
		global::Visibility.visTemplate = ((GameObject)Resources.Load("LoS")).GetComponent<global::Visibility>();
		this.visPlayer = UnityEngine.Object.Instantiate<global::Visibility>(global::Visibility.visTemplate, base.transform);
		this.visPlayer.GO.layer = LayerMask.NameToLayer("LoS");
		this.visPlayer.Radius = 20f;
		this.visPlayer.LightColor = Color.black;
		this.visPlayer.tfParent = base.transform;
		CrewSim.vfxPuffs = base.transform.Find("vfxGasPuffs").GetComponent<VFXGasPuffs>();
		CrewSim.vfxSparks = base.transform.Find("vfxSparksRoot").GetComponent<VFXSparks>();
		GameObject gameObject2 = Resources.Load("vfxBulletTrail") as GameObject;
		if (gameObject2 != null)
		{
			CrewSim.BulletTrail = (gameObject2.GetComponent("TrailRenderer") as TrailRenderer);
		}
		ObjReader.use.scaleFactor = new Vector3(0.0625f, 0.0625f, 0.0625f);
		ObjReader.use.objRotation = new Vector3(90f, 0f, 180f);
		Debug.Log("Initializing crewsim transforms");
		CrewSim.goCrewBar = CrewSim.CanvasManager.goCanvasCrewBar.transform.Find("GUICrewStatus").gameObject;
		CrewSim.goCrewBarPortraitButton = CrewSim.goCrewBar.transform.Find("btnInvBig").gameObject;
		CrewSim.goCrewBarPortraitButton.GetComponent<Button>().onClick.AddListener(delegate()
		{
			if (!CrewSim.bRaiseUI)
			{
				CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
			}
			this.CamCenter(CrewSim.GetSelectedCrew());
		});
		CrewSim.goIntUIPanel = CrewSim.CanvasManager.goCanvasControlPanels.transform.Find("pnlInteractionNav/pnlInteractionUI").gameObject;
		GameObject gameObject3 = GameObject.Find("Main Camera");
		this.camZoom = gameObject3.GetComponent<CameraFocusZoom>();
		CrewSim.btnCPExit = CrewSim.CanvasManager.goCanvasControlPanels.transform.Find("pnlInteractionNav/btnUIExit").GetComponent<Button>();
		CrewSim.btnCPExit.onClick.AddListener(delegate()
		{
			CrewSim.bJustClickedInput = true;
			CrewSim.LowerUI(CrewSim.tplCurrentUI != null && CrewSim.tplCurrentUI.Item1 == "FFWD" && CrewSim.tplLastUI != null && CrewSim.tplLastUI.Item1 != "FFWD");
		});
		AudioManager.AddBtnAudio(CrewSim.btnCPExit.gameObject, "ShipUIBtnPanelSwitchIn", "ShipUIBtnPanelSwitchOut");
		CrewSim.btnCPTop = CrewSim.CanvasManager.goCanvasControlPanels.transform.Find("pnlInteractionNav/btnUIUp").GetComponent<Button>();
		CrewSim.btnCPTop.onClick.AddListener(delegate()
		{
			CrewSim.SwitchUI("strGUIPrefabTop");
		});
		AudioManager.AddBtnAudio(CrewSim.btnCPTop.gameObject, "ShipUIBtnPanelSwitchIn", "ShipUIBtnPanelSwitchOut");
		CrewSim.btnCPBottom = CrewSim.CanvasManager.goCanvasControlPanels.transform.Find("pnlInteractionNav/btnUIDn").GetComponent<Button>();
		CrewSim.btnCPBottom.onClick.AddListener(delegate()
		{
			CrewSim.SwitchUI("strGUIPrefabBottom");
		});
		AudioManager.AddBtnAudio(CrewSim.btnCPBottom.gameObject, "ShipUIBtnPanelSwitchIn", "ShipUIBtnPanelSwitchOut");
		CrewSim.btnCPLeft = CrewSim.CanvasManager.goCanvasControlPanels.transform.Find("pnlInteractionNav/btnUILeft").GetComponent<Button>();
		CrewSim.btnCPLeft.onClick.AddListener(delegate()
		{
			CrewSim.SwitchUI("strGUIPrefabLeft");
		});
		AudioManager.AddBtnAudio(CrewSim.btnCPLeft.gameObject, "ShipUIBtnPanelSwitchIn", "ShipUIBtnPanelSwitchOut");
		CrewSim.btnCPRight = CrewSim.CanvasManager.goCanvasControlPanels.transform.Find("pnlInteractionNav/btnUIRight").GetComponent<Button>();
		CrewSim.btnCPRight.onClick.AddListener(delegate()
		{
			CrewSim.SwitchUI("strGUIPrefabRight");
		});
		AudioManager.AddBtnAudio(CrewSim.btnCPRight.gameObject, "ShipUIBtnPanelSwitchIn", "ShipUIBtnPanelSwitchOut");
		Debug.Log("Initializing Crewsim Shipedit canvas");
		CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/btnClearScene").GetComponent<Button>().onClick.AddListener(delegate()
		{
			CrewSim.jsonShip = null;
			string strRegID = CrewSim.shipCurrentLoaded.strRegID;
			if (strRegID != null)
			{
				CrewSim.system.dictShips.Remove(strRegID);
			}
			this.StartShipEdit();
		});
		CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/btnCrewSim").GetComponent<Button>().onClick.AddListener(delegate()
		{
			if (Input.GetKey(KeyCode.LeftShift))
			{
				this.TestShip("OKLG_AND_VENUS", "VNCA", 60f);
			}
			else
			{
				this.TestShip(null, null, 2f);
			}
		});
		CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/btnSelect").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.SetPartCursor(null);
		});
		CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/GUIWarning/btnQuit").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.QuitToMenu(false);
		});
		CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/GUIWarning/btnEdit").GetComponent<Button>().onClick.AddListener(delegate()
		{
			CanvasManager.HideCanvasGroup(CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/GUIWarning").gameObject);
		});
		UnityEvent onClick = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/btnExit").GetComponent<Button>().onClick;
		if (CrewSim.<>f__mg$cache0 == null)
		{
			CrewSim.<>f__mg$cache0 = new UnityAction(CanvasManager.ShowCanvasQuit);
		}
		onClick.AddListener(CrewSim.<>f__mg$cache0);
		CrewSim.chkFill = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/chkFill").GetComponent<Toggle>();
		CrewSim.chkDraw = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/chkDraw").GetComponent<Toggle>();
		AudioManager.AddBtnAudio(CrewSim.chkFill.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
		AudioManager.AddBtnAudio(CrewSim.chkDraw.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
		Debug.Log("Initializing Crewsim Auto Button");
		CrewSim.chkAIAuto = CrewSim.goCrewBar.transform.Find("pnlControlButtons/AutoTask/chkAutoTask").GetComponent<Toggle>();
		CrewSim.chkAIAuto.onValueChanged.AddListener(new UnityAction<bool>(this.ToggleAutotask));
		AudioManager.AddBtnAudio(CrewSim.chkAIAuto.gameObject, "ShipUIBtnReactorCoilFwdIn", "ShipUIBtnReactorCoilFwdOut");
		Debug.Log("Initializing Crewsim Wallet Button");
		CrewSim.chkWallet = CrewSim.goCrewBar.transform.Find("pnlControlButtons/Wallet/chkWallet").GetComponent<Toggle>();
		CrewSim.chkWallet.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleFinances();
		});
		AudioManager.AddBtnAudio(CrewSim.chkWallet.gameObject, "UIGameplayCash", "UIGameplayClick");
		CrewSim.txtCash = CrewSim.goCrewBar.transform.Find("pnlControlButtons/Wallet/chkWallet/Background/lbl").GetComponent<TMP_Text>();
		this.camMain = gameObject3.GetComponent<Camera>();
		this.CamZoom(1f);
		this.vShake = default(Vector3);
		this.camHighlight = gameObject3.transform.Find("HighlightCam").GetComponent<Camera>();
		this.ScreenShotCam = GameObject.Find("ScreenshotCam").GetComponent<Camera>();
		this.ScreenShotCam.gameObject.SetActive(false);
		TMP_Text component = CrewSim.CanvasManager.goCanvasGUI.transform.Find("txtVersion").GetComponent<TMP_Text>();
		if (IntPtr.Size == 8)
		{
			component.text = DataHandler.strBuild + " (64)";
		}
		else
		{
			component.text = DataHandler.strBuild + " (32)";
		}
		Debug.Log("Initializing Crewsim Debug Canvas");
		CrewSim.txtDialogue = (Resources.Load("txtDialogue") as GameObject).GetComponent<TMP_Text>();
		CrewSim.txtAnim = CrewSim.CanvasManager.goCanvasDebug.transform.Find("pnlAnim/txt").GetComponent<Text>();
		CrewSim.txtMessageLog = CrewSim.goCrewBar.transform.Find("pnlMessageScroll/Viewport/txt").GetComponent<TMP_Text>();
		CrewSim.txtQueue = CrewSim.CanvasManager.goCanvasDebug.transform.Find("pnlQueue/txt").GetComponent<Text>();
		CrewSim.txtPriorities = CrewSim.CanvasManager.goCanvasDebug.transform.Find("pnlPriorities/txt").GetComponent<Text>();
		CrewSim.txtDebug = CrewSim.CanvasManager.goCanvasDebug.transform.Find("txtDebug").GetComponent<Text>();
		CrewSim.txtDebug2 = CrewSim.CanvasManager.goCanvasDebug.transform.Find("txtDebug2").GetComponent<TMP_Text>();
		CrewSim.rectRotate = CrewSim.CanvasManager.goCanvasGUI.transform.Find("bmpRotate").GetComponent<RectTransform>();
		CrewSim.cgRotate = CrewSim.rectRotate.GetComponent<CanvasGroup>();
		this.srMessageLog = CrewSim.goCrewBar.transform.Find("pnlMessageScroll").GetComponent<ScrollRect>();
		this.btnPartTemplate = (Resources.Load("prefabBtnPart") as GameObject);
		this.goShipEdit = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit").gameObject;
		this.pnlPartsContent = this.goShipEdit.transform.Find("pnlParts/scrParts/pnlPartsContent");
		this.pnlPartsContent2 = this.goShipEdit.transform.Find("pnlParts2/scrParts/pnlPartsContent");
		this.pnlPartsSearch = this.goShipEdit.transform.Find("pnlPartsSearch");
		Button component2 = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/btnReplaceFloors").GetComponent<Button>();
		component2.onClick.AddListener(delegate()
		{
			this.ReplaceSelection(false);
		});
		CrewSim.txtBtnReplaceFloors = component2.transform.Find("txt").GetComponent<TMP_Text>();
		component2 = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/btnReplaceWalls").GetComponent<Button>();
		component2.onClick.AddListener(delegate()
		{
			this.ReplaceSelection(true);
		});
		CrewSim.txtBtnReplaceWalls = component2.transform.Find("txt").GetComponent<TMP_Text>();
		CrewSim.txtEyedropper = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/txtEyedropper").GetComponent<TMP_Text>();
		CrewSim.UpdateEyedropperKey(GUIActionKeySelector.commandEyedropper.KeyName);
		TMP_InputField inputField = this.pnlPartsSearch.GetChild(0).GetComponent<TMP_InputField>();
		inputField.onValueChanged.AddListener(delegate(string A_1)
		{
			this.SearchParts(inputField.text);
		});
		inputField.onSelect.AddListener(delegate(string A_0)
		{
			CrewSim.StartTyping();
		});
		inputField.onDeselect.AddListener(delegate(string A_0)
		{
			CrewSim.EndTyping();
		});
		this.pnlPartEdit = this.goShipEdit.transform.Find("scrPartEdit/Viewport/pnlPartEdit").gameObject;
		CrewSim.tgMenu = CrewSim.CanvasManager.goCanvasGUI.GetComponent<ToggleGroup>();
		CrewSim.guiPDA = CrewSim.CanvasManager.goCanvasPDA.transform.Find("GUIPDA2").GetComponent<GUIPDA>();
		CrewSim.guiPDA.Init();
		Toggle chkLight = this.goShipEdit.transform.Find("chkLight").GetComponent<Toggle>();
		chkLight.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleAmbientLight(chkLight);
		});
		Toggle chkBG = this.goShipEdit.transform.Find("chkBG").GetComponent<Toggle>();
		chkBG.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleBGMode(chkBG);
		});
		CrewSim.chkPwrSE = this.goShipEdit.transform.Find("chkPower").GetComponent<Toggle>();
		CrewSim.chkPwrSE.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.TogglePowerUI(CrewSim.shipCurrentLoaded, CrewSim.chkPwrSE);
		});
		CrewSim.chkPwrSE.isOn = CrewSim.PowerVizVisible;
		Debug.Log("Initializing Crewsim Data Canvas");
		CrewSim.chkAutoPause = CrewSim.goCrewBar.transform.Find("pnlControlButtons/AutoPause/chkAutoPause").GetComponent<Toggle>();
		CrewSim.chkAutoPause.onValueChanged.AddListener(delegate(bool isOn)
		{
			this.bcombatAutoPauseAllowed = isOn;
		});
		Debug.Log("Initializing Crewsim Quit Menu");
		Button component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/btnCancel").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			CrewSim.CanvasManager.Invoke("HideCanvasQuit", 0.01f);
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIPauseBtnIn", "UIPauseBtnOut");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlMain/btnQuit").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			this.PopupQuitToMenu();
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIPauseBtnIn", "UIPauseBtnOut");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlMain/btnClose").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			this.PopupQuitToDesktop();
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIPauseBtnIn", "UIPauseBtnOut");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlMain/btnSave").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			MonoSingleton<LoadManager>.Instance.ShowSaveMenu(CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit"));
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIPauseBtnIn", "UIPauseBtnOut");
		CrewSim.objGUISaveIndicator = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlInfo/pnlTop").GetComponent<GUISaveIndicator>();
		CrewSim.objGUISaveOnClose = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlInfo/pnlBottom/Toggle").GetComponent<Toggle>();
		if (CrewSim.objGUISaveOnClose != null)
		{
			if (DataHandler.GetUserSettings() != null)
			{
				CrewSim.objGUISaveOnClose.isOn = DataHandler.GetUserSettings().bSaveOnClose;
			}
			CrewSim.objGUISaveOnClose.onValueChanged.AddListener(delegate(bool isOn)
			{
				if (DataHandler.GetUserSettings() != null)
				{
					DataHandler.GetUserSettings().bSaveOnClose = isOn;
					DataHandler.SaveUserSettings();
				}
			});
		}
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlMain/btnLoad").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			UnityEngine.Object.Instantiate<GameObject>(this._loadingPrefab, CrewSim.CanvasManager.goCanvasQuit.transform);
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIPauseBtnIn", "UIPauseBtnOut");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlMain/btnOptions").GetComponent<Button>();
		component3.onClick.AddListener(new UnityAction(this.Options));
		AudioManager.AddBtnAudio(component3.gameObject, "UIPauseBtnIn", "UIPauseBtnOut");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlLinks/btnManual").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			this.Manual();
		});
		AudioManager.AddBtnAudio(component3.gameObject, "ShipUIPaperRustle01", null);
		CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlLinks/btnManual/txt").GetComponent<TMP_Text>().text = DataHandler.GetString("GUI_QUIT_MANUAL", false);
		component3 = CrewSim.CanvasManager.goCanvasGameOver.transform.Find("GUIGameOver/btnQuit").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			this.QuitToMenu(false);
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIPauseBtnIn", "UIPauseBtnOut");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlLinks/pnlSteam/btn").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			Application.OpenURL("https://steamcommunity.com/app/1022980/discussions/");
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIGameplayCash", "UIGameplayClick");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlLinks/pnlDiscord/btn").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			Application.OpenURL("https://discord.gg/UxZg8Ur");
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIGameplayCash", "UIGameplayClick");
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlLinks/btnGuide").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			Application.OpenURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + DataHandler.GetString("STEAM_GUIDE_ID", false));
		});
		component3 = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlBG/pnlLinks/pnlPDFs/btn").GetComponent<Button>();
		component3.onClick.AddListener(delegate()
		{
			try
			{
				Application.OpenURL(Application.streamingAssetsPath + "/images/manuals/");
			}
			catch (Exception ex)
			{
				Debug.Log(ex.Message + "\n" + ex.StackTrace.ToString());
			}
		});
		AudioManager.AddBtnAudio(component3.gameObject, "UIGameplayCash", "UIGameplayClick");
		TileUtils.goSelPartTiles = new GameObject("Selected Part Tiles");
		TileUtils.goPartTiles = new GameObject("Ship Part Tiles");
		TileUtils.aSelPartTiles = new List<Tile>();
		Debug.Log("Initializing Crewsim Ledger");
		Ledger.Init(null);
		GUIStationRefuel.SetPrices();
		Debug.Log("Initializing Crewsim Context Menu Pool");
		GameObject original = Resources.Load("prefabContextMenuPool") as GameObject;
		this.contextMenuPool = UnityEngine.Object.Instantiate<GameObject>(original, CrewSim.CanvasManager.goCanvasContextMenu.transform).GetComponent<ContextMenuPool>();
		this.contextMenuPool.name = "Context Menu Pool";
		this.cursorRoundel.fillSecondsMax = this.RightMouseButtonDownMax;
		this.cursorRoundel.ResetFill();
		Debug.Log("Initializing Crewsim GigManager");
		this.workManager = base.gameObject.AddComponent<WorkManager>();
		GigManager.Init(null);
		CrewSim.resolutionX = Screen.width;
		CrewSim.resolutionY = Screen.height;
		this.checkResolution = true;
		Debug.Log("Initializing Crewsim Tooltip");
		this.tooltipGO = (Resources.Load("prefabTooltip") as GameObject);
		this.tooltipGO = UnityEngine.Object.Instantiate<GameObject>(this.tooltipGO, CanvasManager.instance.goCanvasGUI.transform);
		this.tooltip = this.tooltipGO.GetComponent<GUITooltip>();
		this.tooltip.window = GUITooltip.TooltipWindow.Hide;
		this.tooltip.tooltipCG.alpha = 0f;
		this.cgPause = CrewSim.CanvasManager.canvasStackHolder.transform.Find("Canvas PopUp/bmpPause").GetComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(this.cgPause);
		this.ActiveCam = this.camMain;
		if (this.LoadFailure != null)
		{
			CanvasManager.HideCanvasGroup(this.LoadFailure);
		}
		Debug.Log("Finished Initializing Crewsim");
		Info.instance.canvas.worldCamera = this.UICamera;
	}

	private void Start()
	{
		CrewSim.OnFinishLoading.Invoke();
		CrewSim.OnFinishLoading.RemoveAllListeners();
	}

	private void AdvanceSim(float fDelta)
	{
		CrewSim.fTotalGameSec += fDelta * CrewSim.fTimeCoeffPause;
		this.UpdateICOs();
	}

	private void SetCursor(int nNew)
	{
		if (CrewSim.nCursor == nNew || nNew < 0 || nNew >= CrewSim.aCursors.Length)
		{
			return;
		}
		Cursor.SetCursor(CrewSim.aCursors[nNew], new Vector2(0f, 0f), CursorMode.Auto);
		CrewSim.nCursor = nNew;
	}

	private void Update()
	{
		if (!this._finishedLoading)
		{
			return;
		}
		bool flag;
		if (CrewSim.fPauseFlashExtra > (double)Time.realtimeSinceStartup)
		{
			flag = ((double)Time.realtimeSinceStartup % 0.4 > 0.2);
		}
		else
		{
			flag = ((int)Time.realtimeSinceStartup % 2 == 0);
		}
		if (CrewSim.tplAutoPause.Item1 > 0.0 && StarSystem.fEpoch <= CrewSim.tplAutoPause.Item1)
		{
			CrewSim.TriggerAutoPause(CrewSim.tplAutoPause.Item2);
			CrewSim.ResetAutoPause();
		}
		if (CrewSim.Paused)
		{
			if (flag && this.cgPause.alpha != 1f)
			{
				this.cgPause.alpha = 1f;
			}
			else if (!flag && this.cgPause.alpha != 0f)
			{
				this.cgPause.alpha = 0f;
			}
		}
		else if (this.cgPause.alpha != 0f)
		{
			this.cgPause.alpha = 0f;
		}
		if (GUIOptionSelect.bRaised || this.LoadFailure.interactable)
		{
			return;
		}
		CrewSim.fTotalGameSecUnscaled += Time.unscaledDeltaTime;
		CrewSim.fTotalGameSecSession += Time.unscaledDeltaTime;
		BeatManager.Update((double)Time.unscaledDeltaTime);
		this.KeyHandler();
		if (CrewSim.nRetogglePwr > 0)
		{
			CrewSim.nRetogglePwr--;
			if (CrewSim.nRetogglePwr == 0)
			{
				this.TogglePowerUI(CrewSim.shipCurrentLoaded, null);
			}
		}
		double fEpoch = StarSystem.fEpoch;
		double num = (double)(Time.deltaTime * CrewSim.fTimeCoeffPause);
		double num3;
		for (double num2 = num; num2 > 0.0; num2 -= num3)
		{
			num3 = num2;
			if (CrewSim.aTickers.Count > 0 && CrewSim.aTickers.FirstOrDefault().fNextTickerSecs < num3)
			{
				num3 = CrewSim.aTickers.FirstOrDefault().fNextTickerSecs;
			}
			if (num3 < 0.004)
			{
				num3 = 0.004;
			}
			StarSystem.fEpoch += num3;
			this.AdvanceSim((float)num3);
		}
		StarSystem.fEpoch = fEpoch;
		if (CrewSim.system != null)
		{
			CrewSim.system.Update(num);
		}
		else
		{
			Debug.LogWarning("Warning: No star system loaded!");
		}
		string text = string.Empty;
		if (CrewSim.aSelected.Count == 1 && CrewSim.bDebugShow)
		{
			CondOwner condOwner = CrewSim.aSelected[0];
			CrewSim.txtAnim.text = condOwner.GetAnimState().ToString();
			CrewSim.txtQueue.text = condOwner.GetDebugQueue();
			CrewSim.txtPriorities.text = condOwner.GetDebugPriorities();
			Text text2 = CrewSim.txtAnim;
			string text3 = text2.text;
			text2.text = string.Concat(new string[]
			{
				text3,
				"\nSlp: ",
				condOwner.GetCondAmount("StatSleep").ToString("N2"),
				"\nCmf: ",
				condOwner.GetCondAmount("StatSleepComfort").ToString("N2"),
				"\nWake: ",
				condOwner.GetCondAmount("DcSleepCycleAwake").ToString("N2"),
				"\nRest: ",
				condOwner.GetCondAmount("DcSleepCycleRest").ToString("N2")
			});
			text = CrewSim.aSelected[0].GetDebugConds("Stat");
			if (CrewSim.txtDebug2.text != text)
			{
				CrewSim.txtDebug2.text = text;
			}
		}
		else
		{
			CrewSim.txtDebug2.text = string.Empty;
		}
		if (this.lineSignal != null)
		{
			float scaleFactor = CrewSim.CanvasManager.goCanvasGUI.GetComponent<Canvas>().scaleFactor;
			Vector2 value = this.camMain.WorldToScreenPoint(this.coConnectMode.transform.position) / scaleFactor;
			Vector2 value2 = Input.mousePosition / scaleFactor;
			this.lineSignal.points2[0] = value;
			this.lineSignal.points2[1] = new Vector2(value2.x, value.y);
			this.lineSignal.points2[2] = value2;
			List<CondOwner> mouseOverCO = this.GetMouseOverCO(this._layerMaskTileHelpers, this.ctSelectFilter, null);
			if (mouseOverCO.Count > 0)
			{
				this.lineSignal.textureScale = 1f;
				this.lineSignal.textureOffset = Time.time * 12f % 1f;
			}
			else
			{
				this.lineSignal.textureScale = 128f;
				this.lineSignal.textureOffset = 0f;
			}
			this.lineSignal.Draw();
		}
		if (this.linePower != null && CrewSim.shipCurrentLoaded != null)
		{
			this.linePower.points2.Clear();
			this.DrawPower(CrewSim.shipCurrentLoaded);
			foreach (Ship ship in CrewSim.shipCurrentLoaded.GetAllDockedShipsFull())
			{
				this.DrawPower(ship);
			}
			this.linePower.textureScale = 1f;
			this.linePower.textureOffset = -Time.time * 3f % 1f;
			this.linePower.Draw();
		}
		if (this.lineSelectRect != null)
		{
			float scaleFactor2 = CrewSim.CanvasManager.goCanvasGUI.GetComponent<Canvas>().scaleFactor;
			Vector3 a = this.camMain.WorldToScreenPoint(this.vDragStart);
			this.lineSelectRect.points2[0] = a / scaleFactor2;
			this.lineSelectRect.points2[1] = new Vector2(Input.mousePosition.x / scaleFactor2, a.y / scaleFactor2);
			this.lineSelectRect.points2[2] = new Vector2(Input.mousePosition.x / scaleFactor2, Input.mousePosition.y / scaleFactor2);
			this.lineSelectRect.points2[3] = new Vector2(a.x / scaleFactor2, Input.mousePosition.y / scaleFactor2);
			this.lineSelectRect.points2[4] = a / scaleFactor2;
			this.lineSelectRect.Draw();
		}
		this.UpdateHighlight();
		this.MouseHandler();
		this.contextMenuPool.MoveToCondOwnerPosition();
		this.bRaisedMenuThisFrame = false;
		this.vMouse = this.ActiveCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, -this.camMain.transform.position.z));
		CrewSim.bRaiseUI = (CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPGUI || CrewSim.CanvasManager.State == CanvasManager.GUIState.SOCIAL);
		this.vShake = UnityEngine.Random.insideUnitSphere * this.fShakeAmp;
		float num4 = 1f;
		if (CrewSim.GetMouseButton(0))
		{
			num4 = 0.25f;
		}
		CrewSim.CanvasManager.CanvasShake(this.vShake, this.fShakeAmp * 30f * num4);
		this.fShakeAmp *= 0.95f;
		if (CrewSim.bDebugShow)
		{
			text = "Time: " + CrewSim.fTotalGameSec.ToString("N2");
			if (CrewSim.fTimeCoeffPause == 0f)
			{
				text += " x0";
			}
			else
			{
				text = text + " x" + Time.timeScale.ToString("N2");
			}
			text += "\n";
			if (CrewSim.txtDebug.text != text)
			{
				CrewSim.txtDebug.text = text;
			}
		}
		CondOwner condOwner2 = CrewSim.coPlayer;
		if (CrewSim.aSelected.Count > 0 && CrewSim.aSelected[0].HasCond("IsHuman"))
		{
			condOwner2 = CrewSim.aSelected[0];
		}
		if (condOwner2 != null)
		{
			MonoSingleton<GUICrewStatus>.Instance.UpdateCrewBar(condOwner2);
			Audio_VacuumController component = condOwner2.GetComponent<Audio_VacuumController>();
			if (component != null)
			{
				component.CheckCurrent();
			}
			CanvasManager.instance.helmet.TunnelOpacity(CanvasManager.instance.helmet.GetTunnelAmount(condOwner2), false);
		}
		if (CrewSim.shipCurrentLoaded != null)
		{
			float num5 = CrewSim.shipCurrentLoaded.objSS.vAccDrag.magnitude / 6.684587E-12f;
			if (num5 > 1f)
			{
				float num6 = CrewSim.shipCurrentLoaded.CurrentRotorEfficiency;
				num5 /= 200f;
				num5 = MathUtils.Clamp(num5, 0f, 1f);
				num6 = Mathf.Clamp(2f * num6 + num5, 0.1f, 1f);
				AudioManager.am.PlayWindAudio(num5, num6, (double)CrewSim.TimeElapsedScaled());
			}
			else
			{
				AudioManager.am.StopWindAudio();
			}
		}
		if (CrewSim.bPoolVisUpdates)
		{
			this.UpdateVisLights();
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew != null)
		{
			this.visPlayer.Position = selectedCrew.tf.position;
			if (selectedCrew.HasCond("IsVisualImpaired"))
			{
				this.visPlayer.Radius = 20f;
			}
			else
			{
				this.visPlayer.Radius = 25f;
			}
			if (CrewSim.coPlayer.ship != null && CrewSim.coPlayer.ship.fLastVisit <= 0.0 && !CrewSim.bRaiseUI)
			{
				if (CrewSim.coPlayer.ship.objSS.bIsBO)
				{
					string strIA = "ENCFirstDock" + CrewSim.coPlayer.ship.strRegID;
					if (!BeatManager.RunEncounter(strIA, true))
					{
						AudioManager.am.SuggestMusic(AudioManager.am.GetStationTag(CrewSim.coPlayer.ship.strRegID), true);
					}
				}
				if (selectedCrew.ship != null)
				{
					selectedCrew.ship.fLastVisit = StarSystem.fEpoch;
					if (selectedCrew.ship.fFirstVisit <= 0.0)
					{
						selectedCrew.ship.fFirstVisit = StarSystem.fEpoch;
					}
				}
			}
		}
		else if (CrewSim.bShipEdit)
		{
			this.visPlayer.Position = this.vMouse;
		}
		if (Screen.width != CrewSim.resolutionX || Screen.height != CrewSim.resolutionY || this.checkResolution)
		{
			this.SetResolution(Screen.width, Screen.height);
			CrewSim.resolutionX = Screen.width;
			CrewSim.resolutionY = Screen.height;
			this.checkResolution = false;
		}
		AudioManager.am.UpdateMusic();
		PlotManager.Update();
		if (!CrewSim.Paused)
		{
			Item.ItemAnimationUpdate.Invoke();
		}
	}

	private void LateUpdate()
	{
		if (!this._finishedLoading)
		{
			return;
		}
		if (this.camFollow)
		{
			this.CamCenterTravel();
		}
		this.MoveViewHandler();
		Destructable.LateUpdateDebug();
	}

	private void DrawPower(Ship ship)
	{
		if (ship == null || ship.aPwrTiles == null || ship.LoadState < Ship.Loaded.Edit)
		{
			return;
		}
		float scaleFactor = CrewSim.CanvasManager.goCanvasGUI.GetComponent<Canvas>().scaleFactor;
		int num = 7600;
		for (int i = 0; i <= ship.aPwrTiles.Count - 2; i += 2)
		{
			if (ship.aPwrTiles[i] == null || ship.aPwrTiles[i + 1] == null)
			{
				ship.aPwrTiles.RemoveRange(i, 2);
				i -= 2;
			}
			else
			{
				Vector2 item = this.camMain.WorldToScreenPoint(ship.aPwrTiles[i].tf.position) / scaleFactor;
				Vector2 item2 = this.camMain.WorldToScreenPoint(ship.aPwrTiles[i + 1].tf.position) / scaleFactor;
				this.linePower.points2.Add(item);
				this.linePower.points2.Add(item2);
				if (this.linePower.points2.Count > num)
				{
					break;
				}
			}
		}
	}

	public void EmptyScene()
	{
		this.camMain.GetComponent<GameRenderer>().StencilCam.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
		CrewSim.LowerUI(false);
		this.UpdateLog(CrewSim.coPlayer, null);
		this.goShipEdit.SetActive(CrewSim.bShipEdit);
		this.SetBracketTarget(null, false, false);
		this.SetPartCursor(null);
		if (CrewSim.system != null)
		{
			CrewSim.system.Destroy();
			CrewSim.system = null;
		}
		if (CrewSim.shipCurrentLoaded != null && CrewSim.shipCurrentLoaded.aTiles != null)
		{
			CrewSim.shipCurrentLoaded.Destroy(true);
		}
		CrewSim.coPlayer = null;
		this.nLastClickIndex = 0;
		this.vLastClick.Set(1000f, 1000f);
		CrewSim.ResetTimeScale();
		CrewSim.fTimeCoeffPause = 1f;
		this.CamZoom(1f);
	}

	public void TestShip(string starSystem, string spawnNearstation = null, float pushbackDistance = 2f)
	{
		if (CrewSim.shipCurrentLoaded != null)
		{
			SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
			CrewSim.jsonShip = CrewSim.shipCurrentLoaded.GetJSON(CrewSim.shipCurrentLoaded.strRegID, false, null);
			CrewSim.bShipEdit = false;
			CrewSim.bShipEditTest = true;
			this.NewGame(starSystem);
			if (spawnNearstation != null)
			{
				CrewSim.objInstance.DebugSetPlayerStartingPosition(spawnNearstation, pushbackDistance, null);
			}
			CrewSim.CanvasManager.CrewSimNormal();
		}
		else
		{
			Debug.Log("Error: No ship loaded. Aborting test.");
		}
	}

	public void NewGame(string solarsystem = null)
	{
		base.StartCoroutine(this.StartNewGame(solarsystem));
	}

	private IEnumerator StartNewGame(string starSystem)
	{
		this._finishedLoading = false;
		CrewSim.bShipEdit = false;
		LoadingScreen.SetProgressBar(0.1f, "Emptying Scene");
		yield return null;
		this.EmptyScene();
		LoadingScreen.SetProgressBar(0.2f, "Init Star System");
		yield return null;
		CrewSim.strSaveVersion = DataHandler.strBuild;
		CrewSim.bSaveUsesOldContainerGrids = false;
		CrewSim.aSaveVersion = new int[]
		{
			0,
			6,
			4,
			1
		};
		string[] aStrVersion = CrewSim.strSaveVersion.Split(new char[]
		{
			'.'
		});
		if (aStrVersion.Length > 0)
		{
			CrewSim.aSaveVersion = new int[aStrVersion.Length];
		}
		for (int i = 0; i < aStrVersion.Length; i++)
		{
			int.TryParse(aStrVersion[i], out CrewSim.aSaveVersion[i]);
		}
		bool loadSystemAndShips = !CrewSim.bShipEditTest || starSystem != null;
		CrewSim.system = new StarSystem();
		JsonStarSystemSave jStarSys = null;
		string systemToLoad = starSystem ?? "NewGame";
		DataHandler.dictStarSystems.TryGetValue(systemToLoad, out jStarSys);
		if (jStarSys != null && loadSystemAndShips)
		{
			Debug.Log("Loading 'NewGame' star system.");
			yield return CrewSim.system.Init(jStarSys, null);
		}
		else
		{
			if (loadSystemAndShips)
			{
				Debug.Log("No data found for 'NewGame' star system. Loading default star system.");
			}
			yield return CrewSim.system.Init(loadSystemAndShips, loadSystemAndShips);
		}
		MarketManager.Init(null);
		LoadingScreen.SetProgressBar(0.4f, "Load Starting Area");
		yield return null;
		Ship ship;
		if (CrewSim.bShipEditTest && CrewSim.jsonShip != null)
		{
			GameObject go = new GameObject("goShip");
			ship = new Ship(go);
			ship.json = CrewSim.jsonShip;
			ship.InitShip(true, Ship.Loaded.Full, null);
			ship.objSS.vPosx = 1.0;
			ship.objSS.vPosy = 1.0;
			ship.objSS.vVelX = 0.0;
			ship.objSS.vVelY = 0.0;
		}
		else if (CrewSim.jsonShip != null)
		{
			GameObject go2 = new GameObject("goShip");
			ship = new Ship(go2);
			ship.json = CrewSim.jsonShip;
			ship.InitShip(true, Ship.Loaded.Full, null);
		}
		else
		{
			string strRegID = "OKLG";
			ship = CrewSim.system.SpawnShip(strRegID, Ship.Loaded.Full);
			ship.fLastVisit = StarSystem.fEpoch;
		}
		ship.gameObject.transform.SetParent(base.transform, false);
		LoadingScreen.SetProgressBar(0.5f, "Spawn Player Character");
		yield return null;
		this.GetRandomCrew(null);
		if (CrewSim.jsonShip != null && !ship.IsStation(false))
		{
			CrewSim.system.RegisterShipOwner(ship.strRegID, CrewSim.coPlayer.strName);
			CrewSim.coPlayer.ClaimShip(ship.strRegID);
			CrewSim.coPlayer.ZeroCondAmount("IsInChargen");
		}
		LoadingScreen.SetProgressBar(0.55f, "Init Plot Manager");
		yield return null;
		BeatManager.Init();
		CrimeManager.Init();
		PlotManager.Init(null);
		LoadingScreen.SetProgressBar(0.6f, "Update Game Objects");
		yield return null;
		this.UpdateICOs();
		CrewSim.SetCashButton(CrewSim.coPlayer.GetCondAmount("StatUSD"));
		LoadingScreen.SetProgressBar(0.65f, "Toggle Power UI");
		yield return null;
		if (CrewSim.PowerVizVisible)
		{
			this.TogglePowerUI(ship, null);
		}
		TileUtils.goPartTiles.SetActive(!TileUtils.goPartTiles.activeInHierarchy);
		LoadingScreen.SetProgressBar(0.8f, "Init AI Ship Manager");
		yield return null;
		AIShipManager.Init(null);
		LoadingScreen.SetProgressBar(0.9f, "Set Camera");
		yield return null;
		this.CamCenter(CrewSim.coPlayer);
		CanvasManager.instance.Black();
		yield return new WaitForSeconds(0.6f);
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			4,
			5
		}))
		{
			List<string> strApps = DataHandler.dictSettings["UserSettings"].strApps;
			int num = strApps.IndexOf("power");
			if (num >= 0)
			{
				strApps[num] = "viz";
			}
			DataHandler.SaveUserSettings();
			CrewSim.guiPDA.RefreshHotbar();
		}
		bool isRegularStart = starSystem == null || CrewSim.jsonShip == null;
		this.CrewSimTut = base.gameObject.AddComponent<CrewSimTut>();
		this.SetChargen(isRegularStart);
		this.SetTutorial();
		this._finishedLoading = true;
		LoadingScreen.DestroyLoadingInstance();
		yield break;
	}

	public void SetTutorial()
	{
		if (CrewSim.bIsQuickstartSession)
		{
			bool flag = PlayerPrefs.GetFloat("QuickstartTutorial", 1f) == 0f;
			if (PlayerPrefs.GetFloat("QuickstartChargen", 1f) != 0f && flag)
			{
				CrewSimTut.forceTutorialNoChargen = true;
			}
		}
		this.CrewSimTut.SetNewGameObjectives();
	}

	public void SetChargen(bool isRegularStart = true)
	{
		bool flag = false;
		if (CrewSim.bIsQuickstartSession && PlayerPrefs.GetFloat("QuickstartChargen", 1f) == 0f)
		{
			flag = true;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsCareerKiosk");
		if (!CrewSim.bShipEditTest && !condTrigger.IsBlank() && CrewSim.shipCurrentLoaded != null && isRegularStart)
		{
			List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(condTrigger, false, false, false);
			if (cos.Count > 0)
			{
				CondOwner objTarget = cos[0];
				Interaction interaction = DataHandler.GetInteraction("GUIChargenBodyStarter", null, false);
				CrewSim.coPlayer.QueueInteraction(objTarget, interaction, true);
				CrewSim.bUILock = true;
				this.startedChargen = true;
			}
			else
			{
				CrewSim.CanvasManager.CrewSimNormal();
			}
		}
		else
		{
			CrewSim.CanvasManager.CrewSimNormal();
		}
		if (flag && !isRegularStart)
		{
			CondOwner condOwner = DataHandler.GetCondOwner("ItmSink01Starter");
			CrewSim.shipCurrentLoaded.AddCO(condOwner, false);
			condOwner.tf.position = CrewSim.coPlayer.tf.position;
			Interaction interaction2 = DataHandler.GetInteraction("GUIChargenBodyStarter", null, false);
			CrewSim.coPlayer.QueueInteraction(condOwner, interaction2, true);
			CrewSim.bUILock = true;
			this.startedChargen = true;
		}
	}

	public void LoadGame(string fileName, string strShipsFolder, Dictionary<string, byte[]> dictFiles = null)
	{
		base.StartCoroutine(this.DoLoadGame(fileName, strShipsFolder, dictFiles));
	}

	public void LoadGame(SaveInfo saveInfo)
	{
		base.StartCoroutine(this.DoLoadGame(saveInfo.PathPlayer, saveInfo.PathShipsFolder, null));
	}

	private IEnumerator DoLoadGame(string fileName, string strShipsFolder, Dictionary<string, byte[]> dictFiles = null)
	{
		Debug.Log("#Info# CrewSim.objInstance.LoadGame(\"" + fileName + "\");");
		this._finishedLoading = false;
		CrewSim.jsonShip = null;
		CrewSim.bShipEdit = false;
		this.EmptyScene();
		LoadingScreen.SetProgressBar(0.3f, "Load files");
		yield return null;
		JsonGameSave jGS = DataHandler.LoadSaveFile(fileName, dictFiles);
		if (jGS == null)
		{
			yield break;
		}
		CrewSim.fTotalGameSec = jGS.fTotalGameSec;
		CrewSim.fTotalGameSecUnscaled = jGS.fTotalGameSecUnscaled;
		CrewSim.strSaveVersion = jGS.strVersion;
		if (CrewSim.strSaveVersion == null)
		{
			CrewSim.strSaveVersion = "<0.6.4.1";
		}
		string strSaveVersionTemp = CrewSim.strSaveVersion.Replace("\n", string.Empty);
		strSaveVersionTemp = strSaveVersionTemp.Replace("\r", string.Empty);
		strSaveVersionTemp = strSaveVersionTemp.Replace("<", string.Empty);
		strSaveVersionTemp = strSaveVersionTemp.Replace("Early Access Build: ", string.Empty);
		CrewSim.aSaveVersion = new int[]
		{
			0,
			6,
			4,
			1
		};
		string[] aStrVersion = strSaveVersionTemp.Split(new char[]
		{
			'.'
		});
		if (aStrVersion.Length > 0)
		{
			CrewSim.aSaveVersion = new int[aStrVersion.Length];
		}
		for (int i = 0; i < aStrVersion.Length; i++)
		{
			int.TryParse(aStrVersion[i], out CrewSim.aSaveVersion[i]);
		}
		CrewSim.bSaveUsesOldDockCount = this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			6,
			7,
			3
		});
		CrewSim.bSaveUsesOldContainerGrids = this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			6,
			4,
			3
		});
		CrewSim.bSaveHasENCPoliceBoard = this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			12,
			1,
			4
		});
		CrewSim.bSaveHasCondRuleDupes = this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			4,
			1
		});
		CrewSim.bSaveHasMissingPledgeUs = this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			2,
			14
		});
		CrewSim.bSaveHasMissingPledgePayloads = this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			3,
			16
		});
		LoadingScreen.SetProgressBar(0.4f, "Load Ships");
		yield return null;
		JsonShip[] aShips = null;
		if (dictFiles != null)
		{
			List<JsonShip> list = new List<JsonShip>();
			foreach (string text in dictFiles.Keys)
			{
				if (text.IndexOf("ships/") >= 0 && !(Path.GetExtension(text) == ".png"))
				{
					Dictionary<string, JsonShip> dictionary = new Dictionary<string, JsonShip>();
					DataHandler.JsonToData<JsonShip>(text, dictionary, dictFiles);
					foreach (JsonShip item in dictionary.Values)
					{
						list.Add(item);
					}
				}
			}
			aShips = list.ToArray();
		}
		else if (Directory.Exists(strShipsFolder))
		{
			List<JsonShip> list2 = new List<JsonShip>();
			string[] files = Directory.GetFiles(strShipsFolder);
			foreach (string strFile in files)
			{
				Dictionary<string, JsonShip> dictionary2 = new Dictionary<string, JsonShip>();
				DataHandler.JsonToData<JsonShip>(strFile, dictionary2);
				foreach (JsonShip item2 in dictionary2.Values)
				{
					list2.Add(item2);
				}
			}
			aShips = list2.ToArray();
		}
		else
		{
			aShips = jGS.aShips;
		}
		LoadingScreen.SetProgressBar(0.5f, "Load star system");
		yield return null;
		DataHandler.dictCOSaves.Clear();
		int nSkippedCOs = 0;
		if (jGS.aCOs != null)
		{
			JsonCondOwnerSave[] aCOs = jGS.aCOs;
			for (int k = 0; k < aCOs.Length; k++)
			{
				JsonCondOwnerSave jcos = aCOs[k];
				if (string.IsNullOrEmpty(jcos.strRegIDLast) || !aShips.Any((JsonShip x) => x.strRegID == jcos.strRegIDLast))
				{
					nSkippedCOs++;
				}
				else
				{
					DataHandler.dictCOSaves[jcos.strID] = jcos;
				}
			}
		}
		Debug.Log("Skipped loading " + nSkippedCOs + " orphaned COs.");
		CrewSim.system = new StarSystem();
		CrewSim.system.bAllowTemplates = false;
		LoadingScreen.SetProgressBar(0.6f, "Init system");
		yield return null;
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			0,
			18
		}) && jGS.objSystem != null && jGS.objSystem.aBOs != null)
		{
			foreach (JsonBodyOrbitSave jsonBodyOrbitSave in jGS.objSystem.aBOs)
			{
				if (jsonBodyOrbitSave != null && jsonBodyOrbitSave.strName != null && jsonBodyOrbitSave.strName.IndexOf("VORB") == 0)
				{
					jsonBodyOrbitSave.fPeriod = CrewSim.system.CalculatePeriodFromPAET((double)((float)jsonBodyOrbitSave.fPerh), (double)((float)jsonBodyOrbitSave.fAph), 4.7000000939106485E+24) * 31556926.0;
				}
			}
		}
		yield return CrewSim.system.Init(jGS.objSystem, aShips);
		if (jGS.jComp != null)
		{
			CrewSim.system.AddCompany(jGS.jComp.Clone());
		}
		MarketManager.Init(jGS.objMarketSave);
		LoadingScreen.SetProgressBar(0.7f, "Load ship");
		yield return null;
		Ship ship = CrewSim.system.dictShips[jGS.strShip];
		ship.InitShip(false, Ship.Loaded.Full, null);
		CondOwnerVisitorCatchUp visAct = new CondOwnerVisitorCatchUp();
		ship.VisitCOs(visAct, true, true, true);
		CrewSim.SetCustomInfos(jGS.aCustomInfos);
		if (CrewSim.PowerVizVisible)
		{
			this.TogglePowerUI(ship, null);
			CrewSim.nRetogglePwr = 1;
		}
		LoadingScreen.SetProgressBar(0.75f, "Load roster");
		yield return null;
		CrewSim.system.bAllowTemplates = true;
		CrewSim.coPlayer = DataHandler.mapCOs[jGS.strPlayerCO];
		bool bCompRegen = true;
		bool bRosterRegen = true;
		if (CrewSim.coPlayer.Company != null)
		{
			bCompRegen = false;
			bRosterRegen = false;
		}
		else if (jGS.jComp != null && CrewSim.system.GetCompany(jGS.jComp.strName) != null)
		{
			JsonCompany company = CrewSim.system.GetCompany(jGS.jComp.strName);
			bCompRegen = false;
			foreach (string key in company.mapRoster.Keys)
			{
				CondOwner condOwner = null;
				if (DataHandler.mapCOs.TryGetValue(key, out condOwner))
				{
					condOwner.Company = company;
					if (condOwner == CrewSim.coPlayer)
					{
						bRosterRegen = false;
					}
				}
			}
		}
		LoadingScreen.SetProgressBar(0.8f, "Load plot");
		yield return null;
		if (bRosterRegen)
		{
			if (bCompRegen)
			{
				CrewSim.coPlayer.Company = new JsonCompany();
				CrewSim.coPlayer.Company.strName = CrewSim.coPlayer.strName + "'s Company";
				CrewSim.coPlayer.Company.strRegID = ship.strRegID;
			}
			else
			{
				CrewSim.coPlayer.Company = CrewSim.system.GetCompany(jGS.jComp.strName);
			}
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID] = new JsonCompanyRules();
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].bShoreLeave = true;
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].bAirlockPermission = false;
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].bRestorePermission = true;
			int nUTCHour = StarSystem.nUTCHour;
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].StartWorkdayAt(nUTCHour);
			CrewSim.coPlayer.ShiftChange(CrewSim.coPlayer.Company.GetShift(nUTCHour, CrewSim.coPlayer), true);
		}
		BeatManager.Init();
		CrimeManager.Init();
		PlotManager.Init(jGS);
		LoadingScreen.SetProgressBar(0.85f, "Load ledger");
		yield return null;
		if (jGS.aLIs != null)
		{
			Ledger.Init(jGS.aLIs);
		}
		LoadingScreen.SetProgressBar(0.9f, "Update COs");
		yield return null;
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			3,
			3
		}))
		{
			Ship shipByRegID = CrewSim.system.GetShipByRegID("OKLG");
			if (shipByRegID != null && shipByRegID.ShipCO != null)
			{
				shipByRegID.ShipCO.SetCondAmount("StationIsDerelictCollector", 40.0, 0.0);
				shipByRegID.ShipCO.SetCondAmount("StationIsDerelictSpawner", 60.0, 0.0);
			}
			shipByRegID = CrewSim.system.GetShipByRegID("VORB");
			if (shipByRegID != null && shipByRegID.ShipCO != null)
			{
				shipByRegID.ShipCO.SetCondAmount("StationIsDerelictCollector", 9.0, 0.0);
				shipByRegID.ShipCO.SetCondAmount("StationIsDerelictSpawner", 16.0, 0.0);
			}
			shipByRegID = CrewSim.system.GetShipByRegID("JFTS");
			if (shipByRegID != null)
			{
				shipByRegID.bNoCollisions = false;
			}
			shipByRegID = CrewSim.system.GetShipByRegID("JATL");
			if (shipByRegID != null)
			{
				shipByRegID.bNoCollisions = false;
			}
			shipByRegID = CrewSim.system.GetShipByRegID("SVIR");
			if (shipByRegID != null)
			{
				shipByRegID.bNoCollisions = false;
			}
			foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
			{
				if (keyValuePair.Value != null && keyValuePair.Value.json != null)
				{
					if (keyValuePair.Value.fLastVisit == 0.0 && !keyValuePair.Value.IsDerelict())
					{
						if (keyValuePair.Value.json.strName == "IbexCargo" || keyValuePair.Value.json.strName == "MesaCargo")
						{
							keyValuePair.Value.bFusionReactorRunning = true;
						}
					}
				}
			}
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			5,
			7
		}))
		{
			LoadingScreen.SetProgressBar(0.9f, "Retrofitting Station Save Data");
			yield return null;
			foreach (Ship ship2 in CrewSim.system.GetStations(true))
			{
				if (ship2 != null && !(ship2.ShipCO == null))
				{
					ship2.ShipCO.SetCondAmount("StationMaintLvl", 0.8, 0.0);
					if (ship2.strRegID == "OKLG")
					{
						ship2.ShipCO.SetCondAmount("StationMaxPirate", 8.0, 0.0);
						ship2.ShipCO.SetCondAmount("StationMinPirate", 2.0, 0.0);
					}
					else if (ship2.strRegID == "FLOT")
					{
						ship2.ShipCO.SetCondAmount("StationMaintLvl", 0.5, 0.0);
					}
				}
			}
		}
		this.UpdateICOs();
		CrewSim.SetCashButton(CrewSim.coPlayer.GetCondAmount("StatUSD"));
		LoadingScreen.SetProgressBar(0.95f, "Set Camera");
		yield return null;
		TileUtils.goPartTiles.SetActive(!TileUtils.goPartTiles.activeInHierarchy);
		CrewSim.ClearSavedNavInputs();
		this.CamCenter(CrewSim.coPlayer);
		CrewSim.AIManual(CrewSim.coPlayer.HasCond("IsAIManual"));
		CrewSim.coPlayer.LogMessage("Welcome back, Captain.", "Neutral", "Game");
		MonoSingleton<ObjectiveTracker>.Instance.LoadObjectives(jGS);
		this.workManager.LoadTasksFromSave(jGS);
		AIShipManager.Init(jGS.objAIShipManager);
		CollisionManager.strATCClosest = AIShipManager.strATCLast;
		LoadingScreen.SetProgressBar(1f, "Load gigs");
		yield return null;
		GigManager.Init(jGS.aJobs);
		MonoSingleton<RacingLeagueManager>.Instance.InitFromSave(jGS.objRacingManager);
		LoadingScreen.SetProgressBar(1f, "Patching old save data");
		yield return null;
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			13,
			0,
			7
		}))
		{
			string[] array2 = new string[]
			{
				"PSPAIReplyAdd",
				"PSPAISurviveO2Add",
				"PSPAISurviveCO2Add",
				"PSPAIEatFoodAdd",
				"PSPAIDrinkAdd",
				"PSPAIStandUp"
			};
			foreach (string strName in array2)
			{
				Interaction interaction = DataHandler.GetInteraction(strName, null, false);
				Interaction interaction2 = interaction;
				CondOwner condOwner2 = CrewSim.coPlayer;
				interaction.objThem = condOwner2;
				interaction2.objUs = condOwner2;
				interaction.ApplyChain(null);
			}
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			0,
			3
		}))
		{
			Ship shipByRegID2 = CrewSim.system.GetShipByRegID("OKLG");
			if (shipByRegID2 != null)
			{
				shipByRegID2.ShipCO.SetCondAmount("StationIsDerelictSpawner", 1.0, 0.0);
				shipByRegID2.ShipCO.SetCondAmount("StationAllowsAnchoring", 1.0, 0.0);
				shipByRegID2.ShipCO.SetCondAmount("StationIsDerelictCollector", 1.0, 0.0);
			}
			shipByRegID2 = CrewSim.system.GetShipByRegID("VORB");
			if (shipByRegID2 != null)
			{
				shipByRegID2.ShipCO.SetCondAmount("StationIsDerelictSpawner", 1.0, 0.0);
				shipByRegID2.ShipCO.SetCondAmount("StationIsDerelictCollector", 1.0, 0.0);
			}
		}
		if (CrewSim.bSaveHasCondRuleDupes)
		{
			Ship[] array4 = CrewSim.system.GetAllLoadedShips().ToArray<Ship>();
			foreach (Ship ship3 in array4)
			{
				foreach (CondOwner condOwner3 in ship3.GetPeople(false))
				{
					condOwner3.DebugFixOldCondRules();
				}
			}
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			0,
			15
		}) && !CrewSim.coPlayer.HasCond("IsDebugMovWorkPenaltyFixed"))
		{
			Debug.Log(string.Concat(new object[]
			{
				"Work/mov penalty fix before: StatFatigueCoeff: ",
				CrewSim.coPlayer.GetCondAmount("StatFatigueCoeff"),
				"; StatMovSpeedPenalty: ",
				CrewSim.coPlayer.GetCondAmount("StatMovSpeedPenalty"),
				"; StatWorkSpeedPenalty: ",
				CrewSim.coPlayer.GetCondAmount("StatWorkSpeedPenalty")
			}));
			CrewSim.coPlayer.AddCondAmount("IsDebugMovWorkPenaltyFixed", 1.0, 0.0, 0f);
			foreach (Ship ship4 in CrewSim.system.GetAllLoadedShips())
			{
				foreach (CondOwner condOwner4 in ship4.GetPeople(false))
				{
					condOwner4.AddCondAmount("StatFatigueCoeff", -0.25, 0.0, 0f);
					condOwner4.ZeroCondAmount("StatMovSpeedPenalty");
					condOwner4.ZeroCondAmount("StatWorkSpeedPenalty");
				}
			}
			Debug.Log(string.Concat(new object[]
			{
				"Work/mov penalty fix after: StatFatigueCoeff: ",
				CrewSim.coPlayer.GetCondAmount("StatFatigueCoeff"),
				"; StatMovSpeedPenalty: ",
				CrewSim.coPlayer.GetCondAmount("StatMovSpeedPenalty"),
				"; StatWorkSpeedPenalty: ",
				CrewSim.coPlayer.GetCondAmount("StatWorkSpeedPenalty")
			}));
			Debug.Log("Fixed work and movement penalty bug in saves 0.14.0.15 and earlier.");
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			2,
			8
		}) && CrewSim.coPlayer.GetCondRule("StatAntiMeat") == null)
		{
			CrewSim.coPlayer.AddCondRule("DcAntiMeat", true);
			Debug.Log("Adding missing StatAntiMeat CondRule for saves 0.14.2.7 and earlier.");
		}
		JsonStarSystemSave jsonStarSystemSave;
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			3,
			8
		}) && DataHandler.dictStarSystems.TryGetValue("NewGame", out jsonStarSystemSave) && jsonStarSystemSave != null && jsonStarSystemSave.aSpawnStations != null)
		{
			foreach (JsonSpawnStation jsonSpawnStation in jsonStarSystemSave.aSpawnStations)
			{
				if (jsonSpawnStation.aStartingConds != null)
				{
					Ship shipByRegID3 = CrewSim.system.GetShipByRegID(jsonSpawnStation.strName);
					if (shipByRegID3 != null)
					{
						if (shipByRegID3.MaxPopulation == 0.0)
						{
							string text2 = jsonSpawnStation.aStartingConds.FirstOrDefault((string x) => x.Contains("StationMaxPopulation"));
							if (!string.IsNullOrEmpty(text2))
							{
								shipByRegID3.ShipCO.ParseCondEquation(text2, 1.0, 0f);
							}
						}
					}
				}
			}
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			3,
			9
		}))
		{
			if (CrewSim.coPlayer.HasCond("Plot_Meat_GoatBoss_CFinal") && CrewSim.coPlayer.GetCondRule("StatCO2Poison") != null)
			{
				CrewSim.coPlayer.AddCondRule("-DcCO2Poison", true);
				Debug.Log("Removing DcCO2Poison CondRule for MeatPlot cultist saves 0.14.3.8 and earlier.");
			}
			foreach (Ship ship5 in CrewSim.system.GetAllLoadedShips())
			{
				if (ship5 != null)
				{
					if (ship5.objSS.bBOLocked && (ship5.objSS.strBOPORShip == null || ship5.objSS.strBOPORShip.Contains("-")))
					{
						ship5.objSS.LockToBO(-1.0, false);
						BodyOrbit nearestBO = CrewSim.system.GetNearestBO(ship5.objSS, StarSystem.fEpoch, false);
						if (nearestBO == null)
						{
							ship5.objSS.UnlockFromBO();
						}
						else
						{
							ship5.objSS.LockToBO(nearestBO, -1.0);
						}
					}
				}
			}
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			3,
			10
		}))
		{
			foreach (Ship ship6 in CrewSim.system.GetAllLoadedShips())
			{
				if (ship6 != null && ship6.objSS != null && ship6.objSS.HasNavData())
				{
					foreach (NavDataPoint navDataPoint in ship6.objSS.NavData.GetNavDataPoints())
					{
						if (navDataPoint.ObjSS.strBOPORShip != null && navDataPoint.ObjSS.strBOPORShip.Contains("-"))
						{
							BodyOrbit nearestBO2 = CrewSim.system.GetNearestBO(ship6.objSS, StarSystem.fEpoch, false);
							if (nearestBO2 != null)
							{
								navDataPoint.ObjSS.LockToBO(nearestBO2, -1.0);
							}
							else
							{
								navDataPoint.ObjSS.strBOPORShip = "Sol";
							}
						}
					}
				}
			}
		}
		int[] aVersion = CrewSim.aSaveVersion;
		int[] array6 = new int[4];
		array6[1] = 14;
		array6[2] = 4;
		if (this.VersionIsOlderThan(aVersion, array6))
		{
			foreach (Ship ship7 in CrewSim.system.GetAllLoadedShips())
			{
				if (ship7 != null)
				{
					foreach (CondOwner condOwner5 in ship7.GetPeople(false))
					{
						Interaction interaction3 = DataHandler.GetInteraction("CGAssignAttraction", null, false);
						if (interaction3 == null)
						{
							break;
						}
						interaction3.objUs = condOwner5;
						interaction3.objThem = condOwner5;
						if (interaction3.Triggered(false, false, false))
						{
							interaction3.ApplyChain(null);
						}
					}
				}
			}
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			4,
			5
		}) && !CrewSim.coPlayer.HasCond("IsDebugPDACartFixed"))
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsPDA");
			CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsPDACartContainer");
			foreach (Ship ship8 in CrewSim.system.GetAllLoadedShips())
			{
				foreach (CondOwner condOwner6 in ship8.GetCOs(condTrigger, true, false, true))
				{
					if (condOwner6.GetCOs(true, condTrigger2).Count <= 0)
					{
						CondOwner condOwner7 = DataHandler.GetCondOwner("PocketPDACarts01");
						CondOwner condOwner8 = condOwner6.AddCO(condOwner7, true, false, true);
						if (condOwner8 != null)
						{
							condOwner8.Destroy();
						}
						else
						{
							Loot loot;
							if (condOwner6.HasCond("IsLocked"))
							{
								loot = DataHandler.GetLoot("ItmRandomPDACart");
							}
							else
							{
								loot = DataHandler.GetLoot("ItmRandomPDACartNewPlayer");
							}
							List<CondOwner> coloot = loot.GetCOLoot(null, false, null);
							foreach (CondOwner objCO in coloot)
							{
								condOwner7.AddCO(objCO, false, true, true);
							}
						}
					}
				}
			}
			List<string> strApps = DataHandler.dictSettings["UserSettings"].strApps;
			int num2 = strApps.IndexOf("power");
			if (num2 >= 0)
			{
				strApps[num2] = "viz";
			}
			DataHandler.SaveUserSettings();
			CrewSim.guiPDA.RefreshHotbar();
			CrewSim.coPlayer.AddCondAmount("IsDebugPDACartFixed", 1.0, 0.0, 0f);
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, new int[]
		{
			0,
			14,
			5,
			14
		}))
		{
			List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(CrewSim.coPlayer.strID);
			foreach (string regId in shipsForOwner)
			{
				List<MarketActorConfig> cargoPods = MarketManager.GetCargoPods(regId);
				foreach (MarketActorConfig marketActorConfig in cargoPods)
				{
					if (!marketActorConfig.IsEmpty)
					{
						if (marketActorConfig.GetFlatStockList().Count == 0)
						{
							MarketManager.ResetCargoPod(regId, marketActorConfig);
						}
					}
				}
			}
		}
		if (this.VersionIsOlderThan(CrewSim.aSaveVersion, CrewSim.aReqVersion))
		{
			CrewSim.coPlayer.LogMessage("Warning: This Save File uses an old format (v" + strSaveVersionTemp + ") which may cause problems.", "Bad", CrewSim.coPlayer.strID);
			CrewSim.coPlayer.LogMessage("For best results, you should begin a new game, or opt-in to a Steam 'legacy_X' beta branch compatible with v" + strSaveVersionTemp + ".", "Bad", CrewSim.coPlayer.strID);
		}
		this.CrewSimTut = base.gameObject.AddComponent<CrewSimTut>();
		MonoSingleton<AsyncShipLoader>.Instance.LoadDockedBarterZoneShips(CrewSim.GetSelectedCrew());
		if (CrewSim.OnGameFinishedLoading != null)
		{
			CrewSim.OnGameFinishedLoading.Invoke();
		}
		LoadingScreen.DestroyLoadingInstance();
		CrewSim.Paused = false;
		this.ForceUpdateAnimators();
		yield return null;
		CrewSim.Paused = true;
		this._finishedLoading = true;
		yield break;
	}

	private void ForceUpdateAnimators()
	{
		if (CrewSim.COAnimators == null)
		{
			return;
		}
		foreach (Animator animator in CrewSim.COAnimators)
		{
			if (!(animator == null) && !(animator.gameObject == null) && animator.gameObject.activeInHierarchy)
			{
				AnimatorCullingMode cullingMode = animator.cullingMode;
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				animator.Update(0.1f);
				animator.Update(0.1f);
				animator.cullingMode = cullingMode;
			}
		}
	}

	private static void ClearSavedNavInputs()
	{
		if (CrewSim.coPlayer.ship != null && CrewSim.coPlayer.ship.LoadState > Ship.Loaded.Shallow && CrewSim.coPlayer.ship.NavPlayerManned)
		{
			CrewSim.coPlayer.ship.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
		}
	}

	public void DebugSetPlayerStartingPosition(string spawnNearStation, float pushbackDistance = 2f, string startingLoot = null)
	{
		base.StartCoroutine(this.SetPlayerStartingPositionNearStation(spawnNearStation, pushbackDistance, startingLoot, null));
	}

	public void DebugSetPlayerStartingPosition(string spawnNearStation, Action callback = null)
	{
		base.StartCoroutine(this.SetPlayerStartingPositionNearStation(spawnNearStation, 73f, null, callback));
	}

	public void DebugSetPlayerStartingPosition(Point spawnPos, Action callback = null)
	{
		base.StartCoroutine(this.SetPlayerStartingPositionToCoords(spawnPos, callback));
	}

	private IEnumerator SetPlayerStartingPositionNearStation(string spawnNearStation, float pushbackDistance, string startingLoot = null, Action callback = null)
	{
		yield return new WaitUntil(() => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading);
		Ship ship = CrewSim.coPlayer.ship;
		ship.MoveShip(-ship.vShipPos);
		Ship station = null;
		CrewSim.system.dictShips.TryGetValue(spawnNearStation, out station);
		if (station != null)
		{
			station.objSS.UpdateTime(StarSystem.fEpoch, false);
			ship.objSS.vPosx = station.objSS.vPosx;
			ship.objSS.vPosy = station.objSS.vPosy;
			if (pushbackDistance > 0f)
			{
				Vector2 a = MathUtils.GetPushbackVector(ship, station);
				float num = 3.3422936E-08f + pushbackDistance;
				a *= num / 149597870f;
				ship.objSS.vPosx = station.objSS.vPosx + (double)a.x;
				ship.objSS.vPosy = station.objSS.vPosy + (double)a.y;
			}
			else
			{
				CrewSim.DockShip(ship, station.strRegID);
			}
			ship.objSS.vVelX = station.objSS.vVelX;
			ship.objSS.vVelY = station.objSS.vVelY;
		}
		if (!string.IsNullOrEmpty(startingLoot))
		{
			List<CondOwner> coloot = DataHandler.GetLoot(startingLoot).GetCOLoot(CrewSim.coPlayer, false, null);
			foreach (CondOwner condOwner in coloot)
			{
				if (!(condOwner == null))
				{
					CondOwner condOwner2 = CrewSim.coPlayer.AddCO(condOwner, true, true, true);
					if (condOwner2 != null)
					{
						CrewSim.coPlayer.DropCO(condOwner2, false, null, 0f, 0f, true, null);
					}
					if (condOwner.HasCond("IsSocialItem"))
					{
						CanvasManager.instance.goCanvasFloaties.GetComponent<GUISocialItemAnimator>().SpawnSocialItemAnimation(condOwner.strName, condOwner.strPortraitImg);
					}
				}
			}
		}
		if (callback != null)
		{
			callback();
		}
		yield break;
	}

	private IEnumerator SetPlayerStartingPositionToCoords(Point spawnPos, Action callback)
	{
		yield return new WaitUntil(() => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading);
		Ship ship = CrewSim.coPlayer.ship;
		ship.MoveShip(-ship.vShipPos);
		ship.objSS.UpdateTime(StarSystem.fEpoch, false);
		ship.objSS.vPosx = spawnPos.X;
		ship.objSS.vPosy = spawnPos.Y;
		BodyOrbit bo = CrewSim.system.GetNearestBO(ship.objSS, StarSystem.fEpoch, false);
		ship.objSS.LockToBO(bo, -1.0);
		ship.objSS.bBOLocked = false;
		if (callback != null)
		{
			callback();
		}
		yield break;
	}

	public bool VersionIsOlderThan(int[] aVersion, int[] aOlderThan)
	{
		if (aVersion == null || aOlderThan == null)
		{
			return true;
		}
		int num = Mathf.Min(aVersion.Length, aOlderThan.Length);
		for (int i = 0; i < num; i++)
		{
			if (aVersion[i] > aOlderThan[i])
			{
				return false;
			}
			if (aVersion[i] < aOlderThan[i])
			{
				return true;
			}
		}
		return false;
	}

	public void StartShipEdit()
	{
		base.StartCoroutine(this.LoadShipEdit());
	}

	private IEnumerator LoadShipEdit()
	{
		CrewSim.chkPwrSE.isOn = false;
		LoadingScreen.SetProgressBar(0.2f, "Loading scene");
		yield return null;
		bool bLoadSystem = false;
		CrewSim.bShipEdit = true;
		CrewSim.CanvasManager.ShipEdit();
		if (!CrewSim.bWarnedShipEdit)
		{
			CanvasManager.ShowCanvasGroup(CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/GUIWarning").gameObject);
			CrewSim.bWarnedShipEdit = true;
		}
		this.EmptyScene();
		LoadingScreen.SetProgressBar(0.4f, "Init system");
		yield return null;
		CrewSim.system = new StarSystem();
		yield return CrewSim.system.Init(bLoadSystem, bLoadSystem);
		LoadingScreen.SetProgressBar(0.5f, "Init ship manager");
		yield return null;
		MarketManager.Init(null);
		AIShipManager.Init(null);
		GameObject goShip = new GameObject("goShip");
		goShip.transform.SetParent(base.transform, false);
		LoadingScreen.SetProgressBar(0.7f, "Load ship");
		yield return null;
		Ship ship = new Ship(goShip);
		if (CrewSim.jsonShip != null)
		{
			ship.json = CrewSim.jsonShip.Clone();
			if (CrewSim.jsonShip.nConstructionProgress < 100)
			{
				JsonShipConstructionTemplate shipConstructionTemplate = DataHandler.GetShipConstructionTemplate(CrewSim.jsonShip);
				ship.json.aItems = shipConstructionTemplate.aItems;
				ship.json.aShallowPSpecs = shipConstructionTemplate.aShallowPSpecs;
			}
			ship.InitShip(true, Ship.Loaded.Edit, null);
			ship.gameObject.name = CrewSim.jsonShip.strName;
		}
		else
		{
			ship.json = new JsonShip();
			ship.json.aItems = new JsonItem[0];
			ship.json.aCrew = new JsonItem[0];
			ship.InitShip(true, Ship.Loaded.Edit, null);
		}
		MonoSingleton<GUIShipEdit>.Instance.LoadShipEdit(CrewSim.jsonShip);
		LoadingScreen.SetProgressBar(1f, "Populate tiles");
		yield return null;
		if (this.pnlPartsContent.childCount < 10)
		{
			this.PopulatePartList(null);
			this.PopulatePartsListSmall();
		}
		this.UpdateICOs();
		if (CrewSim.PowerVizVisible)
		{
			this.TogglePowerUI(ship, null);
		}
		TileUtils.goPartTiles.SetActive(!TileUtils.goPartTiles.activeInHierarchy);
		this.camMain.GetComponent<GameRenderer>().StencilCam.backgroundColor = Color.white;
		this._finishedLoading = true;
		LoadingScreen.DestroyLoadingInstance();
		yield break;
	}

	public static void QueueEncounter(Interaction ia)
	{
		if (ia.objUs == null || ia.objThem == null)
		{
			return;
		}
		CrewSim.objInstance.StartCoroutine(CrewSim.ShowEncounter(ia));
	}

	private static IEnumerator ShowEncounter(Interaction ia)
	{
		if (ia.objUs == null || ia.objThem == null)
		{
			yield break;
		}
		while (CrewSim.bRaiseUI)
		{
			yield return null;
		}
		ia.objUs.QueueInteraction(ia.objThem, ia, false);
		yield break;
	}

	private static IEnumerator ShowScene(string strScene, float time)
	{
		yield return new WaitForSeconds(time);
		CrewSim.objInstance.EmptyScene();
		if (CrewSim.OnGameEnd != null)
		{
			CrewSim.OnGameEnd.Invoke();
		}
		CrewSim.objInstance = null;
		SceneManager.LoadScene(strScene);
		yield break;
	}

	public void QuitImmediate()
	{
		Application.Quit();
	}

	public static void QueueScene(string strScene, float time)
	{
		CrewSim.objInstance.StartCoroutine(CrewSim.ShowScene(strScene, time));
	}

	private static IEnumerator PauseScene(bool bPause, float time)
	{
		yield return new WaitForSeconds(time);
		CrewSim.Paused = bPause;
		yield break;
	}

	public static void QueuePause(bool bPause, float time)
	{
		CrewSim.objInstance.StartCoroutine(CrewSim.PauseScene(bPause, time));
	}

	public void LoadShipEdit(string str)
	{
		CrewSim.jsonShip = DataHandler.GetShip(str);
		if (CrewSim.jsonShip != null)
		{
			this.StartShipEdit();
		}
	}

	public void StartBulkSave()
	{
		this._bulkSaver = base.StartCoroutine(this.BulkSave());
	}

	public bool IsBulkSaverRunning()
	{
		return this._bulkSaver != null;
	}

	private IEnumerator BulkSave()
	{
		List<JsonShip> allShips = DataHandler.dictShips.Values.ToList<JsonShip>();
		foreach (JsonShip jShip in allShips)
		{
			yield return new WaitUntil(() => !MonoSingleton<ScreenshotUtil>.Instance.IsRunning);
			CrewSim.jsonShip = jShip;
			yield return this.LoadShipEdit();
			MonoSingleton<GUIShipEdit>.Instance.SaveShipEdit(jShip.strName);
		}
		CrewSim.jsonShip = null;
		string strRegID = CrewSim.shipCurrentLoaded.strRegID;
		if (strRegID != null)
		{
			CrewSim.system.dictShips.Remove(strRegID);
		}
		this.StartShipEdit();
		this._bulkSaver = null;
		yield break;
	}

	private void ReplaceSelection(bool bWalls)
	{
		if (this.goSelPart == null)
		{
			return;
		}
		CondTrigger condTrigger;
		if (bWalls)
		{
			condTrigger = DataHandler.GetCondTrigger("TIsWall1x1Installed");
		}
		else
		{
			condTrigger = DataHandler.GetCondTrigger("TIsFloorGrate01Installed");
		}
		CondOwner component = this.goSelPart.GetComponent<CondOwner>();
		if (!condTrigger.Triggered(component, null, true))
		{
			return;
		}
		float fLastRotation = component.GetComponent<Item>().fLastRotation;
		CondOwner[] array = new CondOwner[CrewSim.aSelected.Count];
		CrewSim.aSelected.CopyTo(array);
		foreach (CondOwner condOwner in array)
		{
			if (!(condOwner == null) && condTrigger.Triggered(condOwner, null, true))
			{
				CondOwner condOwner2 = DataHandler.GetCondOwner(component.strCODef, null, null, false, null, null, condOwner.strID, null);
				condOwner.ModeSwitch(condOwner2, condOwner.transform.position);
				condOwner2.GetComponent<Item>().fLastRotation = fLastRotation;
			}
		}
		this.SetBracketTarget(null, false, false);
	}

	public void ScheduleCODestruction(CondOwner coToDestroy)
	{
		base.StartCoroutine(this.DestroyCO(coToDestroy));
	}

	private IEnumerator DestroyCO(CondOwner coToDestroy)
	{
		yield return null;
		if (coToDestroy != null)
		{
			coToDestroy.Destroy();
		}
		yield break;
	}

	public void SearchParts(string partText)
	{
		if (partText == string.Empty)
		{
			this.PopulatePartsListSmall();
			return;
		}
		if (partText.Length < 3)
		{
			return;
		}
		IEnumerator enumerator = this.pnlPartsContent2.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
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
		List<string> list = new List<string>();
		foreach (JsonCOOverlay jsonCOOverlay in DataHandler.dictCOOverlays.Values)
		{
			if (jsonCOOverlay.strNameFriendly != null && jsonCOOverlay.strNameFriendly.ToLower().Contains(partText.ToLower()))
			{
				list.Add(jsonCOOverlay.strName);
			}
			else if (jsonCOOverlay.strName != null && jsonCOOverlay.strName.ToLower().Contains(partText.ToLower()))
			{
				list.Add(jsonCOOverlay.strName);
			}
		}
		foreach (JsonCondOwner jsonCondOwner in DataHandler.dictCOs.Values)
		{
			if (jsonCondOwner.strNameFriendly != null && jsonCondOwner.strNameFriendly.ToLower().Contains(partText.ToLower()))
			{
				list.Add(jsonCondOwner.strName);
			}
			else if (jsonCondOwner.strName != null && jsonCondOwner.strName.ToLower().Contains(partText.ToLower()))
			{
				list.Add(jsonCondOwner.strName);
			}
		}
		foreach (string strCO in list)
		{
			CondOwner condOwner = DataHandler.GetCondOwner(strCO);
			if (!(condOwner == null))
			{
				this.PartListBtn(condOwner, this.pnlPartsContent2);
				condOwner.Destroy();
			}
		}
	}

	public static void StartTyping()
	{
		CrewSim.Typing = true;
		Debug.Log("Started typing");
	}

	public static void EndTyping()
	{
		CrewSim.Typing = false;
		Debug.Log("Stopped typing");
	}

	public void PopulatePartsListSmall()
	{
		IEnumerator enumerator = this.pnlPartsContent2.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
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
		List<string> lootNames = DataHandler.GetLoot("ItmShipEditQuickCOs").GetLootNames(null, false, null);
		foreach (string strCO in lootNames)
		{
			CondOwner condOwner = DataHandler.GetCondOwner(strCO);
			if (!(condOwner == null))
			{
				this.PartListBtn(condOwner, this.pnlPartsContent2);
				condOwner.Destroy();
			}
		}
	}

	public void PopulatePartList(string strCT)
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger(strCT);
		IEnumerator enumerator = this.pnlPartsContent.transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform = (Transform)obj;
				UnityEngine.Object.Destroy(transform.gameObject);
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
		foreach (KeyValuePair<string, JsonCOOverlay> keyValuePair in DataHandler.dictCOOverlays)
		{
			CondOwner condOwner = DataHandler.GetCondOwner(keyValuePair.Value.strCOBase);
			if (!(condOwner == null))
			{
				COOverlay cooverlay = condOwner.gameObject.AddComponent<COOverlay>();
				cooverlay.Init(keyValuePair.Key);
				bool flag = condTrigger != null && !condTrigger.Triggered(condOwner, null, true);
				if (flag)
				{
					condOwner.Destroy();
				}
				else
				{
					this.PartListBtn(condOwner, this.pnlPartsContent);
					condOwner.Destroy();
				}
			}
		}
		foreach (KeyValuePair<string, JsonCondOwner> keyValuePair2 in DataHandler.dictCOs)
		{
			CondOwner condOwner2 = DataHandler.GetCondOwner(keyValuePair2.Key);
			bool flag2 = condTrigger != null && !condTrigger.Triggered(condOwner2, null, true);
			if (flag2)
			{
				condOwner2.Destroy();
			}
			else
			{
				this.PartListBtn(condOwner2, this.pnlPartsContent);
				condOwner2.Destroy();
			}
		}
	}

	private void PartListBtn(CondOwner co, Transform pnlParent)
	{
		if (co.strPortraitImg == string.Empty)
		{
			Debug.Log(co.strName);
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.btnPartTemplate);
		ButtonPart btn = gameObject.GetComponent<ButtonPart>();
		btn.lblBtn.text = co.FriendlyName;
		Texture2D texture2D = DataHandler.LoadPNG(co.strPortraitImg + ".png", false, false);
		float num = (float)texture2D.width / (float)texture2D.height;
		btn.bmpBtnRaw.GetComponent<AspectRatioFitter>().aspectRatio = num;
		if (num >= 3f)
		{
			btn.bmpBtnRaw.GetComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
		}
		btn.bmpBtnRaw.texture = texture2D;
		btn.strPartName = co.strName;
		btn.btn.onClick.AddListener(delegate()
		{
			this.SetPartCursor(btn.strPartName);
		});
		gameObject.transform.SetParent(pnlParent, false);
	}

	public void ToggleFinances()
	{
		if (CrewSim.bUILock)
		{
			return;
		}
		if (!CrewSim.bRaiseUI)
		{
			CrewSim.RaiseUI("Finance", CrewSim.coPlayer);
		}
		else if (CrewSim.goUI != null)
		{
			GUIFinance component = CrewSim.goUI.GetComponent<GUIFinance>();
			if (component != null)
			{
				CrewSim.LowerUI(false);
			}
			else
			{
				CrewSim.RaiseUI("Finance", CrewSim.coPlayer);
			}
		}
		else
		{
			CrewSim.LowerUI(false);
		}
	}

	public CondOwner GetRandomCrew(Tile til = null)
	{
		PersonSpec personSpec2;
		if (CrewSim.coPlayer == null)
		{
			JsonPersonSpec personSpec = DataHandler.GetPersonSpec("PlayerNew");
			personSpec2 = new PersonSpec(personSpec, true);
			personSpec2.nAgeMin = (personSpec2.nAgeMax = 18);
		}
		else
		{
			JsonPersonSpec personSpec3 = DataHandler.GetPersonSpec("NPCNew");
			personSpec2 = new PersonSpec(personSpec3, true);
		}
		return this.AddCrew(personSpec2, til);
	}

	public void AddNpcToRoster(CondOwner co)
	{
		if (co == null)
		{
			return;
		}
		Hire hire = new Hire();
		Interaction interaction = DataHandler.GetInteraction("SOCHire2BasicAllow", null, false);
		interaction.objThem = co;
		interaction.objUs = CrewSim.coPlayer;
		hire.UpdateUs(interaction);
		interaction.objThem = CrewSim.coPlayer;
		interaction.objUs = co;
		hire.UpdateThem(interaction);
		List<CondTrigger> ctloot = interaction.LootCTsUs.GetCTLoot(null, null);
		foreach (CondTrigger condTrigger in ctloot)
		{
			if (condTrigger != null && condTrigger.Triggered(co, null, true))
			{
				condTrigger.ApplyChanceID(true, co, 1f, 0f);
			}
		}
		List<string> lootNames = DataHandler.GetLoot(interaction.strLootRELChangeThemSeesUs).GetLootNames(null, false, null);
		if (lootNames.Count > 0 && co.pspec != null && CrewSim.coPlayer.pspec != null)
		{
			foreach (string text in lootNames)
			{
				if (text.IndexOf("-") == 0)
				{
					CrewSim.coPlayer.socUs.RemovePerson(co.pspec, new List<string>
					{
						text.Substring(1)
					});
				}
				else
				{
					string strNameFriendly = DataHandler.GetCond(text).strNameFriendly;
					CrewSim.coPlayer.socUs.AddPerson(new Relationship(co.pspec, new List<string>
					{
						text
					}, new List<string>
					{
						"Became " + strNameFriendly + " during: " + interaction.strTitle
					}));
					string text2 = string.Concat(new string[]
					{
						co.strName,
						" becomes a ",
						strNameFriendly,
						" to ",
						CrewSim.coPlayer.strName,
						"."
					});
					interaction.LogSocial(CrewSim.coPlayer.strName, text, interaction.strTitle);
				}
			}
		}
		lootNames = DataHandler.GetLoot(interaction.strLootRELChangeUsSeesThem).GetLootNames(null, false, null);
		if (lootNames.Count > 0 && co.pspec != null && CrewSim.coPlayer.pspec != null)
		{
			foreach (string text3 in lootNames)
			{
				if (text3.IndexOf("-") == 0)
				{
					co.socUs.RemovePerson(CrewSim.coPlayer.pspec, new List<string>
					{
						text3.Substring(1)
					});
				}
				else
				{
					string strNameFriendly2 = DataHandler.GetCond(text3).strNameFriendly;
					co.socUs.AddPerson(new Relationship(CrewSim.coPlayer.pspec, new List<string>
					{
						text3
					}, new List<string>
					{
						"Became " + strNameFriendly2 + " during: " + interaction.strTitle
					}));
					string text4 = string.Concat(new string[]
					{
						CrewSim.coPlayer.strName,
						" becomes a ",
						strNameFriendly2,
						" to ",
						co.strName,
						"."
					});
					interaction.LogSocial(CrewSim.coPlayer.strName, text3, interaction.strTitle);
				}
			}
		}
	}

	private CondOwner AddCrew(PersonSpec pspec, Tile til)
	{
		if (pspec == null || CrewSim.shipCurrentLoaded == null)
		{
			return null;
		}
		CondOwner condOwner = pspec.MakeCondOwner(PersonSpec.StartShip.OLD, CrewSim.shipCurrentLoaded);
		if (condOwner == null)
		{
			return null;
		}
		if (CrewSim.coPlayer == null)
		{
			CrewSim.coPlayer = condOwner;
			if (DataHandler.GetUserSettings().strNewPlayer == "true")
			{
				CrewSim.coPlayer.AddCondAmount("IsNewPlayer", 1.0, 0.0, 0f);
			}
			CrewSim.coPlayer.AddCondAmount(GUIFinance.strCondCurr, MathUtils.Rand(95.2, 103.7, MathUtils.RandType.Flat, null), 0.0, 0f);
			CrewSim.coPlayer.Company = new JsonCompany();
			CrewSim.coPlayer.Company.strName = CrewSim.coPlayer.strName + "'s Company";
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID] = new JsonCompanyRules();
			CrewSim.coPlayer.Company.strRegID = CrewSim.shipCurrentLoaded.strRegID;
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].bShoreLeave = true;
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].bAirlockPermission = false;
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].bRestorePermission = true;
			CrewSim.coPlayer.LogMessage("Welcome, Captain.", "Neutral", "Game");
			int nUTCHour = StarSystem.nUTCHour;
			CrewSim.coPlayer.Company.mapRoster[CrewSim.coPlayer.strID].SetAllHours(2);
			CrewSim.coPlayer.ShiftChange(CrewSim.coPlayer.Company.GetShift(nUTCHour, CrewSim.coPlayer), true);
			CrewSim.AIManual(CrewSim.coPlayer.HasCond("IsAIManual"));
			MonoSingleton<ObjectiveTracker>.Instance.AddShipSubscription(CrewSim.shipCurrentLoaded.strRegID);
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsLootSpawner");
			List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(condTrigger, false, false, true);
			for (int i = cos.Count - 1; i >= 0; i--)
			{
				if (!(cos[i].mapGUIPropMaps["Panel A"]["strType"] == "Loot"))
				{
					string a = cos[i].mapGUIPropMaps["Panel A"]["strLoot"];
					if (!(a != "PlayerNew"))
					{
						til = cos[i].GetComponent<LootSpawner>().GetSpawnTile(CrewSim.shipCurrentLoaded);
						CrewSim.shipCurrentLoaded.RemoveCO(cos[i], false);
						break;
					}
				}
			}
		}
		if (til == null)
		{
			til = CrewSim.shipCurrentLoaded.GetCrewSpawnTile(condOwner);
		}
		condOwner.ship.RemoveCO(condOwner, false);
		Vector3 position = default(Vector3);
		if (til != null)
		{
			position.Set(til.transform.position.x, til.transform.position.y, condOwner.transform.position.z);
		}
		condOwner.transform.position = position;
		CrewSim.shipCurrentLoaded.AddCO(condOwner, true);
		condOwner.SetCrewZOffset();
		return condOwner;
	}

	public static void AddTicker(CondOwner co)
	{
		CrewSim.aTickers.Add(co);
	}

	public static void RemoveTicker(CondOwner co)
	{
		CrewSim.aTickers.Remove(co);
	}

	private void UpdateICOs()
	{
		CondOwner.nEndTurnsThisFrame = 0;
		CrewSim.aTickersTemp.AddRange(CrewSim.aTickers);
		foreach (CondOwner condOwner in CrewSim.aTickersTemp)
		{
			if (condOwner == null || condOwner.ship == null || condOwner.ship.bDestroyed || !condOwner.ship.gameObject.activeInHierarchy)
			{
				CrewSim.RemoveTicker(condOwner);
			}
			else
			{
				condOwner.UpdateManual(10);
			}
		}
		CrewSim.aTickersTemp.Clear();
		if (CrewSim.fTotalGameSec - this.fUIUpdateLast > this.fUIUpdateHeartbeat)
		{
			this.fUIUpdateLast = CrewSim.fTotalGameSec;
		}
	}

	public void SetPartCursor(string strName)
	{
		if (strName != null)
		{
			if (this.goSelPart != null && this.goSelPart.transform.parent == null)
			{
				UnityEngine.Object.Destroy(this.goSelPart);
			}
			JsonItem jsonItem = new JsonItem();
			jsonItem.strName = strName;
			jsonItem.fX = this.vMouse.x;
			jsonItem.fY = this.vMouse.y;
			jsonItem.fRotation = 0f;
			if (CrewSim.bShipEditBG)
			{
				Item background = DataHandler.GetBackground(strName);
				this.goSelPart = background.gameObject;
			}
			else
			{
				this.goSelPart = CrewSim.shipCurrentLoaded.CreatePart(jsonItem, null, true);
			}
			this.goSelPart.layer = LayerMask.NameToLayer("Tile Helpers");
			CrewSim.cgRotate.alpha = 0.7f;
			CrewSim.rectRotate.Find("txt").GetComponent<TMP_Text>().text = GUIActionKeySelector.commandRotateItem.KeyName;
		}
		else
		{
			UnityEngine.Object.Destroy(this.goSelPart);
			this.goSelPart = null;
			CrewSim.cgRotate.alpha = 0f;
		}
		TileUtils.ResetItemGridSprites();
	}

	public void UnselectNonHumanTargets()
	{
		for (int i = CrewSim.aSelected.Count - 1; i >= 0; i--)
		{
			if (!CrewSim.aSelected[i].HasCond("IsHuman"))
			{
				this.SelectCO(CrewSim.aSelected[i], true);
			}
		}
	}

	public void SetBracketTarget(string strID, bool bUpdateOnly, bool noAuto = false)
	{
		if (bUpdateOnly && (CrewSim.aSelected.Count != 1 || CrewSim.aSelected[0] == null || CrewSim.aSelected[0].strID != strID))
		{
			return;
		}
		CondOwner[] array = new CondOwner[CrewSim.aSelected.Count];
		CrewSim.aSelected.CopyTo(array);
		foreach (CondOwner condOwner in array)
		{
			if (!(strID == condOwner.strID))
			{
				if (!CrewSim.ZoneMenuOpen || !condOwner.HasCond("IsHuman"))
				{
					this.SelectCO(condOwner, true);
				}
			}
		}
		CrewSim.txtBtnReplaceFloors.text = DataHandler.GetString("GUI_SHIPEDIT_REPLACE_FLOORS", false).Replace("XXX", "0");
		CrewSim.txtBtnReplaceWalls.text = DataHandler.GetString("GUI_SHIPEDIT_REPLACE_WALLS", false).Replace("XXX", "0");
		if (strID == null)
		{
			return;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsWall1x1Installed");
		CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsFloorGrate01Installed");
		CondOwner condOwner2 = null;
		DataHandler.mapCOs.TryGetValue(strID, out condOwner2);
		if (condOwner2 != null)
		{
			this.SelectCO(condOwner2, false);
			if (noAuto)
			{
				CrewSim.AIManual(true);
			}
			if (condTrigger.Triggered(condOwner2, null, true))
			{
				CrewSim.txtBtnReplaceWalls.text = DataHandler.GetString("GUI_SHIPEDIT_REPLACE_WALLS", false).Replace("XXX", "1");
			}
			if (condTrigger2.Triggered(condOwner2, null, true))
			{
				CrewSim.txtBtnReplaceFloors.text = DataHandler.GetString("GUI_SHIPEDIT_REPLACE_FLOORS", false).Replace("XXX", "1");
			}
		}
	}

	public static CondOwner GetBracketTarget()
	{
		if (CrewSim.aSelected.Count == 1)
		{
			return CrewSim.aSelected[0];
		}
		return null;
	}

	public static CondOwner GetSelectedCrew()
	{
		if (CrewSim.aSelected.Count == 1 && CrewSim.aSelected[0].HasCond("IsHuman"))
		{
			return CrewSim.aSelected[0];
		}
		return CrewSim.coPlayer;
	}

	public void UpdateLog(CondOwner co, string strColor)
	{
		if (co == null)
		{
			return;
		}
		CondOwner condOwner = CrewSim.coPlayer;
		if (CrewSim.aSelected.Count > 0)
		{
			condOwner = CrewSim.aSelected[0];
		}
		if (condOwner == co)
		{
			if (condOwner.Crew != null)
			{
				MonoSingleton<GUIRenderTargets>.Instance.SetFace(condOwner, false);
			}
			string text = CrewSim.txtMessageLog.text;
			string messageLog = condOwner.GetMessageLog(-1);
			if (text != messageLog)
			{
				CrewSim.txtMessageLog.text = messageLog;
				if (strColor == "Bad" || strColor == "GoodRemove")
				{
					AudioManager.am.PlayAudioEmitter("UIMessageLogBad", false, true);
				}
				else if (strColor == "Dialogue")
				{
					AudioManager.am.PlayAudioEmitter("UIMessageLogSocial", false, true);
				}
				base.StartCoroutine(this.ScrollBottom(this.srMessageLog));
			}
		}
	}

	public void ShowInputSelector(CondTrigger ctVisible, GUIData igd)
	{
		CrewSim.Paused = true;
		CrewSim.bPauseLock = true;
		CrewSim.CanvasManager.CrewSimNormal();
		this.aHidden.Clear();
		foreach (Ship ship in CrewSim.aLoadedShips)
		{
			List<CondOwner> icos = ship.GetICOs1(null, false, false, false);
			foreach (CondOwner condOwner in icos)
			{
				if (ctVisible.Triggered(condOwner, null, true))
				{
					condOwner.Highlight = true;
				}
				else
				{
					condOwner.DimLights = true;
				}
				this.aHidden.Add(condOwner.strID);
			}
		}
		GameObject original = Resources.Load<GameObject>("prefabSignalLine");
		this.lineSignal = UnityEngine.Object.Instantiate<GameObject>(original).GetComponent<VectorObject2D>().vectorLine;
		this.lineSignal.SetCanvas(CrewSim.CanvasManager.goCanvasGUI, false);
		this.coConnectLastCrew = CrewSim.GetSelectedCrew();
		this.coConnectMode = igd.COSelf;
		this.ctSelectFilter = ctVisible;
		this.igdConnectMode = igd;
		this.nLastClickIndex = 0;
	}

	public void HideInputSelector()
	{
		foreach (string key in this.aHidden)
		{
			CondOwner condOwner = null;
			DataHandler.mapCOs.TryGetValue(key, out condOwner);
			if (condOwner != null)
			{
				condOwner.Highlight = false;
				condOwner.DimLights = false;
			}
		}
		VectorLine.Destroy(ref this.lineSignal);
		CrewSim.CanvasManager.ShipGUI(null);
		this.coConnectMode = null;
		this.ctSelectFilter = null;
		this.igdConnectMode = null;
		CrewSim.bPauseLock = false;
		CrewSim.Paused = false;
		this.nLastClickIndex = 0;
	}

	public List<CondOwner> GetMouseOverCOsExternal(Vector3? mousePosition = null)
	{
		CondTrigger condTrigger = this.ctSelectFilter;
		this.ctSelectFilter = DataHandler.GetCondTrigger("TCanBeSelectedMTT");
		List<CondOwner> mouseOverCO = this.GetMouseOverCO(this._layerMaskDefLosTileHelpers, this.ctSelectFilter, mousePosition);
		Room room = null;
		if (CrewSim.shipCurrentLoaded != null)
		{
			room = CrewSim.shipCurrentLoaded.GetRoomAtWorldCoords1(this.vMouse, true);
		}
		if (room == null)
		{
			room = MonoSingleton<AsyncShipLoader>.Instance.GetRoomAtWorldCoords(this.vMouse);
		}
		if (room != null)
		{
			mouseOverCO.Remove(room.CO);
			mouseOverCO.Add(room.CO);
		}
		this.ctSelectFilter = condTrigger;
		return mouseOverCO;
	}

	private List<CondOwner> GetMouseOverCO(string[] aLayerMaskNames, CondTrigger ctFilter, Vector3? mousePosition = null)
	{
		int mask = LayerMask.GetMask(aLayerMaskNames);
		Ray ray = (mousePosition == null) ? this.ActiveCam.ScreenPointToRay(Input.mousePosition) : this.ActiveCam.ScreenPointToRay(mousePosition.Value);
		RaycastHit[] source = Physics.RaycastAll(ray, 100f, mask);
		List<CondOwner> list = new List<CondOwner>();
		foreach (RaycastHit raycastHit in from go in source
		orderby go.distance
		select go)
		{
			CondOwner component = raycastHit.transform.GetComponent<CondOwner>();
			if (component != null)
			{
				if (ctFilter == null || ctFilter.Triggered(component, null, false))
				{
					list.Add(component);
				}
			}
		}
		return list;
	}

	private List<Item> GetMouseOverBG(string[] aLayerMaskNames)
	{
		int mask = LayerMask.GetMask(aLayerMaskNames);
		Ray ray = this.camMain.ScreenPointToRay(Input.mousePosition);
		RaycastHit[] array = Physics.RaycastAll(ray, 100f, mask);
		List<Item> list = new List<Item>();
		array = (from go in array
		orderby go.distance
		select go).ToArray<RaycastHit>();
		foreach (RaycastHit raycastHit in array)
		{
			if (!(raycastHit.transform.GetComponent<CondOwner>() != null))
			{
				Item component = raycastHit.transform.GetComponent<Item>();
				if (component != null)
				{
					list.Add(component);
				}
			}
		}
		return list;
	}

	private GameObject ClickSelectScenePart(string[] aLayerMaskNames)
	{
		List<CondOwner> mouseOverCO = this.GetMouseOverCO(aLayerMaskNames, this.ctSelectFilter, null);
		Ray ray = this.camMain.ScreenPointToRay(Input.mousePosition);
		CondOwner condOwner = null;
		Vector2 a = new Vector2(TileUtils.GridAlign(ray.origin.x), TileUtils.GridAlign(ray.origin.y));
		if ((a - this.vLastClick).SqrMagnitude() < 0.1f)
		{
			this.nLastClickIndex++;
			if (this.nLastClickIndex >= mouseOverCO.Count)
			{
				this.nLastClickIndex = 0;
			}
			if (this.nLastClickIndex < mouseOverCO.Count)
			{
				condOwner = mouseOverCO[this.nLastClickIndex];
			}
		}
		else
		{
			this.nLastClickIndex = mouseOverCO.Count - 1;
			this.vLastClick = a;
			while (this.nLastClickIndex >= 0)
			{
				condOwner = mouseOverCO[this.nLastClickIndex];
				if (condOwner.objCOParent != null)
				{
					this.nLastClickIndex--;
				}
				else
				{
					if (condOwner.Crew != null)
					{
						break;
					}
					this.nLastClickIndex--;
				}
			}
		}
		if (!(condOwner != null))
		{
			return null;
		}
		if (CrewSim.CanvasManager.State == CanvasManager.GUIState.SOCIAL)
		{
			return GUISocialCombat2.coUs.gameObject;
		}
		this.SetBracketTarget(condOwner.strID, false, false);
		return condOwner.gameObject;
	}

	public static void RaiseUI(string strCOGUIKey, CondOwner coSelf)
	{
		if (CrewSim.objInstance.coConnectMode != null || CrewSim.CanvasManager.State == CanvasManager.GUIState.SOCIAL || CrewSim.CanvasManager.State == CanvasManager.GUIState.GAMEOVER)
		{
			return;
		}
		if (GUIInventory.instance.Selected != null)
		{
			CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("GUI_INV_NO_CLOSE", false), "Bad", CrewSim.GetSelectedCrew().strID);
			AudioManager.am.PlayAudioEmitter("UIMessageLogBad", false, true);
			return;
		}
		CrewSim.guiPDA.State = GUIPDA.UIState.Closed;
		CrewSim.CanvasManager.ShowOrbits(true);
		CrewSim.objInstance.LowerContextMenu();
		if (strCOGUIKey != "Inventory")
		{
			CrewSim.OnRightClick.Invoke(new List<CondOwner>());
		}
		if (coSelf.mapGUIPropMaps == null || !coSelf.mapGUIPropMaps.ContainsKey(strCOGUIKey))
		{
			Debug.Log(string.Concat(new object[]
			{
				"No such GUI Key found on ",
				coSelf,
				"'s GUIPropMaps: ",
				strCOGUIKey
			}));
			return;
		}
		Dictionary<string, string> dictionary = coSelf.mapGUIPropMaps[strCOGUIKey];
		CrewSim.RefreshTooltipEvent.Invoke();
		if (!dictionary.ContainsKey("strGUIPrefab"))
		{
			Debug.Log(string.Concat(new object[]
			{
				"No strGUIPrefab Key found on ",
				coSelf,
				"'s GUIPropMap named ",
				strCOGUIKey,
				"."
			}));
			return;
		}
		CrewSim.tplLastUI = CrewSim.tplCurrentUI;
		CrewSim.tplCurrentUI = new Tuple<string, CondOwner>(strCOGUIKey, coSelf);
		if (dictionary["strGUIPrefab"] == "SocialCombat")
		{
			if (CrewSim.goUI != null)
			{
				CrewSim.LowerUI(false);
			}
			Interaction interactionCurrent = coSelf.GetInteractionCurrent();
			CondOwner objThem = interactionCurrent.objThem;
			GUISocialCombat2.strSubUI = interactionCurrent.strSubUI;
			if (coSelf.socUs != null)
			{
				Relationship relationship = coSelf.socUs.GetRelationship(objThem.strName);
				if (relationship != null && interactionCurrent.strLootContextUs != null)
				{
					relationship.strContext = interactionCurrent.strLootContextUs;
				}
			}
			if (objThem.socUs != null)
			{
				Relationship relationship2 = objThem.socUs.GetRelationship(coSelf.strName);
				if (relationship2 != null && interactionCurrent.strLootContextThem != null)
				{
					relationship2.strContext = interactionCurrent.strLootContextThem;
				}
			}
			if (interactionCurrent.strThemType == Interaction.TARGET_OTHER && objThem == CrewSim.GetSelectedCrew())
			{
				CrewSim.CanvasManager.SocialCombat(objThem, coSelf, false);
				GUISocialCombat2.fPauseDelay = interactionCurrent.fDuration * 3600.0 + 0.5;
				GUISocialCombat2.objInstance.ClearActions();
			}
			else
			{
				GUISocialCombat2.fPauseDelay = interactionCurrent.fDuration * 3600.0 + 0.5;
				CrewSim.CanvasManager.SocialCombat(coSelf, objThem, true);
			}
			return;
		}
		if (strCOGUIKey == "Inventory")
		{
			if (coSelf != CrewSim.GetSelectedCrew())
			{
				return;
			}
			if (!CrewSim.inventoryGUI.IsOpen)
			{
				CommandInventory.ToggleInventory(coSelf, false);
			}
			CrewSim.inventoryGUI.SpawnInventoryWindow(coSelf.GetInteractionCurrent().objThem, InventoryWindowType.Container, null, null);
			return;
		}
		else
		{
			if (strCOGUIKey == "Finance")
			{
				CrewSim.chkWallet.isOn = true;
			}
			string text = dictionary["strGUIPrefab"];
			if (text == "GUITrade/GUITrade2")
			{
				text = "GUITrade/GUITrade";
				dictionary["strGUIPrefab"] = text;
			}
			GameObject gameObject = Resources.Load<GameObject>("GUIShip/" + text);
			if (gameObject != null)
			{
				CrewSim.CloseGUIData();
				CrewSim.goUI = UnityEngine.Object.Instantiate<GameObject>(gameObject);
				CrewSim.goUI.transform.SetParent(CrewSim.goIntUIPanel.transform, false);
				coSelf.mapGUIRefs[strCOGUIKey] = CrewSim.goUI.GetComponent<IGUIHarness>();
				string text2 = dictionary["strGUIPrefab"];
				text2 = text2.Substring(text2.IndexOf("/") + 1);
				GUIData guidata = CrewSim.goUI.GetComponent(text2) as GUIData;
				if (guidata == null)
				{
					GUIData[] components = CrewSim.goUI.GetComponents<GUIData>();
					if (components != null)
					{
						GUIData[] array = components;
						int num = 0;
						if (num < array.Length)
						{
							GUIData guidata2 = array[num];
							guidata = guidata2;
						}
					}
					if (guidata == null)
					{
						Debug.Log(string.Concat(new string[]
						{
							"Component ",
							text2,
							" not found on GUIShip/",
							dictionary["strGUIPrefab"],
							"."
						}));
					}
				}
				guidata.Init(coSelf, dictionary, strCOGUIKey);
				CrewSim.goUI.GetComponent<Animator>().SetInteger("AnimState", 5);
				guidata.bActive = true;
				EventSystem.current.sendNavigationEvents = false;
				UnityEngine.Object.Destroy(CrewSim.goDialogue);
				if (GUIModal.Instance)
				{
					GUIModal.Instance.Hide();
				}
				if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("IsInChargen"))
				{
					CrewSim.CanvasManager.ShipGUI(new List<GameObject>
					{
						CanvasManager.instance.goCanvasPDA
					});
				}
				else
				{
					CrewSim.CanvasManager.ShipGUI(null);
				}
				CrewSim.SetUIArrows();
				return;
			}
			Debug.Log("Unable to load resource: GUIShip/" + dictionary["strGUIPrefab"]);
			return;
		}
	}

	public static void SetToggleWithoutNotify(Toggle chk, bool bValue)
	{
		if (chk == null)
		{
			return;
		}
		Toggle.ToggleEvent onValueChanged = chk.onValueChanged;
		chk.onValueChanged = new Toggle.ToggleEvent();
		chk.isOn = bValue;
		chk.onValueChanged = onValueChanged;
	}

	public static void LowerUI(bool bRestoreLastUI = false)
	{
		if (CrewSim.bUILock)
		{
			return;
		}
		if (CrewSim.goUI == null)
		{
			return;
		}
		GUIFinance component = CrewSim.goUI.GetComponent<GUIFinance>();
		if (component != null && CrewSim.chkWallet != null)
		{
			CrewSim.chkWallet.isOn = false;
		}
		if (!CrewSim.CloseGUIData())
		{
			return;
		}
		EventSystem.current.sendNavigationEvents = true;
		EventSystem.current.SetSelectedGameObject(null);
		if (bRestoreLastUI && CrewSim.tplLastUI != null)
		{
			CrewSim.RaiseUI(CrewSim.tplLastUI.Item1, CrewSim.tplLastUI.Item2);
			return;
		}
		CrewSim.CanvasManager.ShowOrbits(false);
		if (CrewSim.bShipEdit)
		{
			CrewSim.CanvasManager.ShipEdit();
		}
		else
		{
			CrewSim.CanvasManager.CrewSimNormal();
		}
		CrewSim.RefreshTooltipEvent.Invoke();
	}

	private static bool CloseGUIData()
	{
		if (CrewSim.goUI == null)
		{
			return true;
		}
		GUIData component = CrewSim.goUI.GetComponent<GUIData>();
		if (component != null)
		{
			if ((GUIActionKeySelector.commandEscape.Down || GUIActionKeySelector.commandEscape.Held) && component.CloseOutermostWindow())
			{
				return false;
			}
			component.SaveAndClose();
		}
		UnityEngine.Object.Destroy(CrewSim.goUI);
		CrewSim.goUI = null;
		return true;
	}

	public static void SetMainMenuOff()
	{
		CrewSim.tgMenu.SetAllTogglesOff();
	}

	public static void SwitchUI(string strDirection)
	{
		if (CrewSim.goUI == null)
		{
			return;
		}
		CrewSim.goUI = CrewSim.goUI.GetComponent<IGUIHarness>().GoDir(strDirection);
		CrewSim.SetUIArrows();
		GUIData component = CrewSim.goUI.GetComponent<GUIData>();
		if (component != null)
		{
			component.UpdateUI();
		}
		AudioManager.am.PlayAudioEmitter("UIPanelSwitch", false, false);
	}

	public static void SetUIArrows()
	{
		CrewSim.btnCPBottom.gameObject.SetActive(false);
		CrewSim.btnCPTop.gameObject.SetActive(false);
		CrewSim.btnCPLeft.gameObject.SetActive(false);
		CrewSim.btnCPRight.gameObject.SetActive(false);
		CrewSim.btnCPExit.gameObject.SetActive(!CrewSim.bUILock);
		string @string = DataHandler.GetString("GUI_NAV_SWITCH", false);
		if (CrewSim.goUI.GetComponent<IGUIHarness>().goUIBottom != null)
		{
			CrewSim.btnCPBottom.gameObject.SetActive(true);
			GUIData component = CrewSim.goUI.GetComponent<IGUIHarness>().goUIBottom.GetComponent<GUIData>();
			CrewSim.btnCPBottom.transform.Find("txt").GetComponent<TMP_Text>().text = @string + component.strFriendlyName;
		}
		if (CrewSim.goUI.GetComponent<IGUIHarness>().goUITop != null)
		{
			CrewSim.btnCPTop.gameObject.SetActive(true);
			GUIData component2 = CrewSim.goUI.GetComponent<IGUIHarness>().goUITop.GetComponent<GUIData>();
			CrewSim.btnCPTop.transform.Find("txt").GetComponent<TMP_Text>().text = @string + component2.strFriendlyName;
		}
		if (CrewSim.goUI.GetComponent<IGUIHarness>().goUILeft != null)
		{
			CrewSim.btnCPLeft.gameObject.SetActive(true);
			GUIData component3 = CrewSim.goUI.GetComponent<IGUIHarness>().goUILeft.GetComponent<GUIData>();
			CrewSim.btnCPLeft.transform.Find("txt").GetComponent<TMP_Text>().text = @string + component3.strFriendlyName;
		}
		if (CrewSim.goUI.GetComponent<IGUIHarness>().goUIRight != null)
		{
			CrewSim.btnCPRight.gameObject.SetActive(true);
			GUIData component4 = CrewSim.goUI.GetComponent<IGUIHarness>().goUIRight.GetComponent<GUIData>();
			CrewSim.btnCPRight.transform.Find("txt").GetComponent<TMP_Text>().text = @string + component4.strFriendlyName;
		}
	}

	public static void DockAndDespawn(Ship shipAI, Ship shipStation, string issuerRegId = null)
	{
		if (shipAI == null || shipStation == null)
		{
			return;
		}
		List<CondOwner> people = shipAI.GetPeople(true);
		for (int i = people.Count - 1; i >= 0; i--)
		{
			CondOwner condOwner = people[i];
			condOwner.CatchUp();
			condOwner.UnclaimShip(shipAI.strRegID);
			List<string> aFactionsThem = (from x in shipStation.GetShipFactions()
			select x.strName).ToList<string>();
			float factionScore = condOwner.GetFactionScore(aFactionsThem);
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsValidStationDropOff");
			if ((condTrigger != null && !condTrigger.Triggered(condOwner, null, true)) || JsonFaction.GetReputation(factionScore) == JsonFaction.Reputation.Dislikes)
			{
				condOwner.Destroy();
			}
			else
			{
				condOwner.LogMove(shipAI.strRegID, shipStation.strRegID, MoveReason.DOCKED, issuerRegId);
				CrewSim.MoveCO(condOwner, shipStation, false);
				condOwner.ClaimShip(shipStation.strRegID);
				condOwner.ZeroCondAmount("IsShakedownModeActive");
				if (condOwner.Company != null)
				{
					condOwner.Company.SetPermissionAirlock(condOwner.strID, false);
					condOwner.Company.SetPermissionShore(condOwner.strID, false);
					condOwner.Company.SetPermissionRestore(condOwner.strID, false);
					if (condOwner.Company != CrewSim.coPlayer.Company && condOwner.Company.jcrDefaultRules != null)
					{
						condOwner.Company.jcrDefaultRules.SetAllHours(0);
					}
				}
				if (!condOwner.HasQueuedInteraction("WanderSoon"))
				{
					Interaction interaction = DataHandler.GetInteraction("WanderSoon", null, false);
					condOwner.QueueInteraction(condOwner, interaction, false);
				}
			}
		}
		shipAI.LogAdd(DataHandler.GetString("NAV_LOG_DOCKDESPAWN", false) + shipStation.strRegID + DataHandler.GetString("NAV_LOG_TERMINATOR", false), StarSystem.fEpoch, true);
		shipAI.ToggleVis(false, true);
		shipAI.HideFromSystem = true;
		AIShipManager.UnregisterShip(shipAI);
	}

	public static Ship DockShip(Ship shipUs, string strRegID)
	{
		bool flag = false;
		Ship ship = CrewSim.system.GetShipByRegID(strRegID);
		if (ship != null)
		{
			flag = (ship.LoadState >= Ship.Loaded.Edit);
		}
		ship = CrewSim.system.SpawnShip(strRegID, Ship.Loaded.Full);
		CrewSim.objInstance.SyncFuelDelayed(ship);
		if (shipUs.GetAllDockedShips().IndexOf(ship) >= 0)
		{
			return ship;
		}
		if (!flag)
		{
			CondOwnerVisitorCatchUp visitor = new CondOwnerVisitorCatchUp();
			ship.VisitCOs(visitor, true, false, true);
		}
		GameObject gameObject = ship.gameObject;
		gameObject.transform.SetParent(CrewSim.objInstance.transform, false);
		ship.ToggleVis(true, true);
		if (ship.fLastVisit == 0.0)
		{
			ship.fLastVisit = -1.0;
		}
		if (ship.fFirstVisit == 0.0)
		{
			ship.fFirstVisit = StarSystem.fEpoch;
			ship.nInitConstructionProgress = ship.nConstructionProgress;
		}
		shipUs.Dock(ship, false);
		if (GUIDockSys.instance != null)
		{
			GUIDockSys.instance.ShowTarget();
		}
		shipUs.Comms.Clearance = null;
		ship.Comms.Clearance = null;
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsNavStationNotOff");
		List<CondOwner> icos = shipUs.GetICOs1(condTrigger, false, false, false);
		ShipInfo si = new ShipInfo(ship, true);
		foreach (CondOwner condOwner in icos)
		{
			Dictionary<string, string> dict = null;
			condOwner.mapGUIPropMaps.TryGetValue("Panel A", out dict);
			ShipInfo.SetShipInfo(si, dict);
		}
		GUIOrbitDraw guiorbitDraw = null;
		if (CrewSim.goUI != null)
		{
			guiorbitDraw = CrewSim.goUI.GetComponent<GUIOrbitDraw>();
		}
		if (guiorbitDraw != null)
		{
			guiorbitDraw.UpdateShipInfo(si);
		}
		GUIDockSys guidockSys = null;
		if (CrewSim.goUI != null)
		{
			guidockSys = CrewSim.goUI.GetComponent<GUIDockSys>();
		}
		if (guidockSys != null)
		{
			guidockSys.UpdateShipInfo(si);
		}
		icos = ship.GetICOs1(condTrigger, false, false, false);
		si = new ShipInfo(shipUs, true);
		foreach (CondOwner condOwner2 in icos)
		{
			Dictionary<string, string> dict2 = null;
			condOwner2.mapGUIPropMaps.TryGetValue("Panel A", out dict2);
			ShipInfo.SetShipInfo(si, dict2);
		}
		condTrigger = new CondTrigger();
		condTrigger.aReqs = new string[]
		{
			"IsDockSys"
		};
		icos = shipUs.GetICOs1(condTrigger, false, false, false);
		if (icos.Count == 0)
		{
			return null;
		}
		CondOwner condOwner3 = icos[0];
		icos = ship.GetICOs1(condTrigger, false, false, false);
		if (icos.Count == 0)
		{
			return null;
		}
		CondOwner condOwner4 = icos[0];
		float num = condOwner3.transform.rotation.eulerAngles.z;
		float num2 = condOwner4.transform.rotation.eulerAngles.z;
		int i = 0;
		num = (num % 360f + 360f) % 360f;
		num2 = (num2 % 360f + 360f) % 360f;
		if (num2 < num)
		{
			num2 += 360f;
		}
		while (num - num2 < 180f)
		{
			num2 -= 90f;
			i++;
		}
		while (i > 0)
		{
			ship.RotateCW();
			i--;
		}
		Vector2 pos = condOwner3.GetPos("DockA", false);
		Vector2 pos2 = condOwner4.GetPos("DockB", false);
		ship.MoveShip(new Vector2(pos.x - pos2.x, pos.y - pos2.y));
		CrimeManager.ClearCrimeFlags(ship.strLaw, shipUs.GetPeople(false));
		if (ship.json != null)
		{
			string strName = ship.json.strName;
		}
		if (ship.objSS.bIsBO && CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("TutorialNoStationYet"))
		{
			CrewSim.coPlayer.AddCondAmount("TutorialNoStationYet", -CrewSim.coPlayer.GetCondAmount("TutorialNoStationYet"), 0.0, 0f);
		}
		if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("TutorialNoDockedYet"))
		{
			CrewSim.coPlayer.AddCondAmount("TutorialNoDockedYet", -CrewSim.coPlayer.GetCondAmount("TutorialNoDockedYet"), 0.0, 0f);
		}
		if (ship.DMGStatus == Ship.Damage.Derelict)
		{
			bool flag2 = BeatManager.RunEncounter("ENCFirstDockDerelict", true);
			if (flag2)
			{
				DataHandler.GetUserSettings().strNewPlayer = "false";
				DataHandler.SaveUserSettings();
			}
			if (!flag2 && CrewSim.coPlayer != null && !CrewSim.coPlayer.HasCond("TutorialZonesStart"))
			{
				string keyName = GUIActionKeySelector.commandToggleZoneUI.KeyName;
				Objective objective = new Objective(CrewSim.coPlayer, "Open Zones Menu", "TIsTutorialOpenZonesComplete");
				objective.strDisplayDesc = "Press the \"" + keyName + "\" key to open the zones menu";
				objective.strDisplayDescComplete = "Zone menu opened";
				objective.bTutorial = true;
				MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
				CrewSim.coPlayer.AddCondAmount("TutorialZonesStart", 1.0, 0.0, 0f);
			}
		}
		else if (ship.IsStation(false))
		{
			BeatManager.ResetReleaseTimer();
		}
		CrewSim.objInstance.ForceUpdateAnimators();
		return ship;
	}

	private void SyncFuelDelayed(Ship ship)
	{
		base.StartCoroutine(this._SyncFuel(ship));
	}

	private IEnumerator _SyncFuel(Ship ship)
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		if (ship == null || ship.LoadState < Ship.Loaded.Full)
		{
			yield break;
		}
		ship.SyncFuel();
		yield break;
	}

	public static void UndockShip(Ship shipUs, Ship objShipThem, bool bPushback, bool keepClearance = false)
	{
		shipUs.Undock(objShipThem);
		if (!keepClearance)
		{
			shipUs.Comms.Clearance = null;
			objShipThem.Comms.Clearance = null;
		}
		if (objShipThem.IsStation(false))
		{
			CrewSim.coPlayer.AddCondAmount("IsUndockGracePeriodMajor", 1.0, 0.0, 0f);
		}
		else
		{
			CrewSim.coPlayer.AddCondAmount("IsUndockGracePeriodMinor", 1.0, 0.0, 0f);
		}
		AudioManager.am.SuggestMusic("Undocking", false);
		if (bPushback)
		{
			Vector2 pushbackVector = MathUtils.GetPushbackVector(shipUs, objShipThem);
			shipUs.objSS.vVelX += (double)pushbackVector.x * 3.342293532089815E-11;
			shipUs.objSS.vVelY += (double)pushbackVector.y * 3.342293532089815E-11;
			shipUs.objSS.PlaceOrbitPosition(objShipThem.objSS);
			CrewSim.system.SetSituToSafeCoords(shipUs.objSS);
		}
		if (objShipThem.IsAIShip)
		{
			AIShipManager.LEOCheckIllegalUndock(shipUs, objShipThem);
			AIShipManager.ValidateCrew(objShipThem);
		}
		CrewSim.objInstance.SaveToShallow(objShipThem);
	}

	public void SaveToShallow(Ship ship)
	{
		if (ship == null || ship.bDestroyed)
		{
			return;
		}
		string strRegID = ship.strRegID;
		JsonShip json = ship.GetJSON(strRegID, true, null);
		DataHandler.dictShips[json.strName] = json;
		ship.json = null;
		List<JsonCondOwnerSave> list = new List<JsonCondOwnerSave>();
		List<CondOwner> icos = ship.GetICOs1(null, true, false, true);
		List<CondOwner> list2 = new List<CondOwner>();
		foreach (CondOwner condOwner in icos)
		{
			if (ship.LoadState < Ship.Loaded.Edit && condOwner.jCOS != null)
			{
				list.Add(condOwner.jCOS);
			}
			else
			{
				JsonCondOwnerSave jsonsave = condOwner.GetJSONSave();
				if (jsonsave != null)
				{
					list.Add(jsonsave);
				}
				list2.AddRange(condOwner.GetLotCOs(true));
			}
		}
		foreach (CondOwner condOwner2 in list2)
		{
			JsonCondOwnerSave jsonsave2 = condOwner2.GetJSONSave();
			if (jsonsave2 != null)
			{
				list.Add(jsonsave2);
			}
		}
		foreach (JsonCondOwnerSave jsonCondOwnerSave in list)
		{
			DataHandler.dictCOSaves[jsonCondOwnerSave.strID] = jsonCondOwnerSave;
			if (DataHandler.mapCOs.ContainsKey(jsonCondOwnerSave.strID))
			{
				DataHandler.mapCOs.Remove(jsonCondOwnerSave.strID);
			}
		}
		this.DestroyAndReload(ship, json);
		MonoSingleton<GUIRenderTargets>.Instance.ResetLoadedPortraits();
	}

	private void DestroyAndReload(Ship ship, JsonShip json)
	{
		string strID = CrewSim.coPlayer.strID;
		string strRegID = ship.strRegID;
		JsonAIShipSave jsonaishipSave = AIShipManager.GetJSONAIShipSave(ship.strRegID);
		string shipOwner = CrewSim.system.GetShipOwner(strRegID);
		ship.Destroy(true);
		MonoSingleton<TargetVisController>.Instance.ClearTargetVis();
		if (!string.IsNullOrEmpty(shipOwner))
		{
			CrewSim.system.RegisterShipOwner(strRegID, shipOwner);
		}
		Transform transform = GameObject.Find("PlayState").transform;
		GameObject gameObject = new GameObject("goShip");
		gameObject.transform.SetParent(transform, false);
		ship = new Ship(gameObject);
		ship.json = json;
		ship.InitShip(false, Ship.Loaded.Shallow, null);
		CrewSim.system.dictShips[ship.strRegID] = ship;
		if (!DataHandler.mapCOs.TryGetValue(strID, out CrewSim.coPlayer))
		{
			Debug.LogError("ERROR: Unable to reacquire player after removing ship " + strRegID);
		}
		ship.ToggleVis(false, false);
		if (jsonaishipSave != null)
		{
			AIShipManager.AddAIToShip(ship, jsonaishipSave.enumAIType, jsonaishipSave.strATCLast, jsonaishipSave);
		}
	}

	public void LaunchShip(float fTime, CondOwner coUser, GUIChargenStack cgs)
	{
		base.StartCoroutine(this._LaunchShip(1f, coUser, cgs));
	}

	private IEnumerator _LaunchShip(float fTime, CondOwner coUser, GUIChargenStack cgs)
	{
		while (fTime > 0f)
		{
			fTime -= CrewSim.TimeElapsedUnscaled();
			yield return null;
		}
		Ship objShipOld = coUser.ship;
		Ship objShipNew = CrewSim.system.SpawnShip(cgs.strRegIDChosen, Ship.Loaded.Full);
		CareerChosen cc = cgs.GetLatestCareer();
		if (cc.fShipDmgMax > 0f)
		{
			objShipNew.DamageAllCOs(cc.fShipDmgMax, true, null);
		}
		objShipNew.ToggleVis(true, true);
		objShipOld.ToggleVis(false, true);
		objShipNew.MoveShip(-objShipNew.vShipPos);
		CrewSim.MoveCO(coUser, objShipNew, true);
		coUser.ClaimShip(objShipNew.strRegID);
		if (cc.fStartATCRange == 0f)
		{
			CrewSim.DockShip(objShipNew, cc.strStartATC);
		}
		else
		{
			Ship ship = null;
			CrewSim.system.dictShips.TryGetValue(cc.strStartATC, out ship);
			if (ship == null)
			{
				ship = objShipOld;
			}
			objShipNew.objSS.vPosx = ship.objSS.vPosx;
			objShipNew.objSS.vPosy = ship.objSS.vPosy;
			Vector2 a = MathUtils.GetPushbackVector(objShipNew, ship);
			a *= cc.fStartATCRange;
			objShipNew.objSS.vPosx = ship.objSS.vPosx + (double)(a.x / 149597870f);
			objShipNew.objSS.vPosy = ship.objSS.vPosy + (double)(a.y / 149597870f);
			objShipNew.objSS.vVelX = ship.objSS.vVelX;
			objShipNew.objSS.vVelY = ship.objSS.vVelY;
			objShipNew.objSS.vAccEx = Vector2.zero;
		}
		MonoSingleton<ObjectiveTracker>.Instance.AddShipSubscription(objShipNew.strRegID);
		objShipOld.Destroy(true);
		objShipOld = null;
		yield break;
	}

	public void TeleportCO(CondOwner coRider, string strDestRegID)
	{
		if (coRider == null || string.IsNullOrEmpty(strDestRegID))
		{
			return;
		}
		Ship shipByRegID = CrewSim.system.GetShipByRegID(strDestRegID);
		if (shipByRegID != null)
		{
			if (coRider.ship == shipByRegID)
			{
				List<CondOwner> followers = coRider.GetFollowers();
				CrewSim.MoveCO(coRider, shipByRegID, false);
				foreach (CondOwner objCO in followers)
				{
					CrewSim.MoveCO(objCO, shipByRegID, false);
				}
				return;
			}
			if (coRider == CrewSim.GetSelectedCrew())
			{
				MonoSingleton<AsyncShipLoader>.Instance.Unload(null);
			}
			base.StartCoroutine(this._TeleportCO(coRider, shipByRegID));
		}
	}

	private IEnumerator _TeleportCO(CondOwner coUser, Ship objShipNew)
	{
		if (coUser == null || objShipNew == null)
		{
			yield break;
		}
		bool bPlayer = coUser == CrewSim.GetSelectedCrew();
		if (bPlayer)
		{
			MonoSingleton<GUILoadingPopUp>.Instance.ShowTooltip(DataHandler.GetString("LOAD_SHIPLOAD", false), objShipNew.publicName);
			yield return new WaitForSecondsRealtime(0.1f);
		}
		Ship.Loaded nLoad = Ship.Loaded.Full;
		if (!bPlayer)
		{
			nLoad = Ship.Loaded.Shallow;
		}
		objShipNew = CrewSim.system.SpawnShip(objShipNew.strRegID, nLoad);
		if (bPlayer)
		{
			CrewSim.LowerUI(false);
		}
		Ship objShipOld = coUser.ship;
		List<CondOwner> aFollows = coUser.GetFollowers();
		CrewSim.MoveCO(coUser, objShipNew, false);
		if (objShipNew.LoadState >= Ship.Loaded.Edit)
		{
			CondOwnerVisitorCatchUp visitor = new CondOwnerVisitorCatchUp();
			objShipNew.VisitCOs(visitor, true, true, true);
		}
		foreach (CondOwner objCO in aFollows)
		{
			CrewSim.MoveCO(objCO, objShipNew, false);
		}
		if (objShipNew.LoadState >= Ship.Loaded.Edit)
		{
			foreach (Ship ship in objShipOld.GetAllDockedShips())
			{
				if (ship.gameObject.activeInHierarchy)
				{
					CrewSim.objInstance.SaveToShallow(ship);
				}
			}
			CrewSim.objInstance.SaveToShallow(objShipOld);
			objShipNew.ToggleVis(true, true);
		}
		if (bPlayer)
		{
			CrewSim.objInstance.CamCenter(CrewSim.GetSelectedCrew());
			MonoSingleton<GUILoadingPopUp>.Instance.FadeOutToolTip(1.5f);
		}
		yield break;
	}

	public static void MoveCO(CondOwner objCO, Ship objShipNew, bool bPlayerShip)
	{
		if (objCO == null || objShipNew == null)
		{
			return;
		}
		objCO.RemoveFromCurrentHome(false);
		Pathfinder pathfinder = objCO.Pathfinder;
		if (pathfinder != null)
		{
			pathfinder.HideFootprints();
		}
		if (objShipNew.aTiles != null && objShipNew.aTiles.Count > 0)
		{
			Tile crewSpawnTile = objShipNew.GetCrewSpawnTile(objCO);
			if (crewSpawnTile != null)
			{
				objCO.transform.position = new Vector3(crewSpawnTile.tf.position.x, crewSpawnTile.tf.position.y, objCO.tf.position.z);
				objCO.currentRoom = crewSpawnTile.room;
			}
		}
		else
		{
			Vector3 crewSpawnPosition = objShipNew.GetCrewSpawnPosition(objCO);
			objCO.transform.position = new Vector3(crewSpawnPosition.x, crewSpawnPosition.y, crewSpawnPosition.z);
			objCO.currentRoom = null;
		}
		objShipNew.AddCO(objCO, true);
		if (objCO == CrewSim.coPlayer)
		{
			CrewSim.objInstance.CamCenter(objCO);
		}
		if (bPlayerShip)
		{
			CrewSim.shipPlayerOwned = objShipNew;
			if (CrewSim.coPlayer.Company != null)
			{
				CrewSim.coPlayer.Company.strRegID = CrewSim.shipPlayerOwned.strRegID;
			}
		}
	}

	public void QueueForTransit(JsonTransitConnection jsonTransit, List<CondOwner> cosToMove, Action<JsonTransitConnection, List<CondOwner>> callback)
	{
		List<string> list = new List<string>();
		string originShip = string.Empty;
		foreach (CondOwner condOwner in cosToMove)
		{
			if (!(condOwner == null))
			{
				if (condOwner.ship != null)
				{
					originShip = condOwner.ship.strRegID;
				}
				list.Add(condOwner.name);
			}
		}
		base.StartCoroutine(this.QueueForTransit(originShip, jsonTransit, list, callback));
	}

	private IEnumerator QueueForTransit(string originShip, JsonTransitConnection jsonTransit, List<string> cosToMove, Action<JsonTransitConnection, List<CondOwner>> moveToTransitCallback)
	{
		double startingTime = StarSystem.fEpoch;
		yield return new WaitUntil(() => StarSystem.fEpoch - startingTime > 8.0);
		Ship destination = CrewSim.system.GetShipByRegID(jsonTransit.strTargetRegID);
		Ship origin = CrewSim.system.GetShipByRegID(originShip);
		if (destination == null || destination.bDestroyed || origin == null || origin.bDestroyed)
		{
			yield break;
		}
		List<CondOwner> allPeople = origin.GetPeople(false);
		List<CondOwner> pplToMove = new List<CondOwner>();
		foreach (CondOwner condOwner in allPeople)
		{
			if (!(condOwner == null) && cosToMove.Contains(condOwner.name))
			{
				condOwner.UnclaimShip(condOwner.ship.strRegID);
				condOwner.ClaimShip(destination.strRegID);
				condOwner.CatchUp();
				CrewSim.MoveCO(condOwner, destination, false);
				pplToMove.Add(condOwner);
			}
		}
		if (pplToMove.Count > 0)
		{
			moveToTransitCallback(jsonTransit, pplToMove);
		}
		yield break;
	}

	public void CamZoom(float fAmount)
	{
		if ((double)fAmount < 1.0)
		{
			float num = 0.8f;
			if (CrewSim.bShipEdit)
			{
				num = 0.04f;
			}
			float num2 = Mathf.Max(this.camMain.orthographicSize - num, 0f);
			this.camMain.orthographicSize = num2 * fAmount + num;
		}
		else
		{
			float num3 = 150f;
			if (CrewSim.bShipEdit)
			{
				num3 = 150f;
			}
			float num4 = Mathf.Min(this.camMain.orthographicSize / num3, 1f);
			float num5 = num4 * fAmount / (1f + num4 * (fAmount - 1f));
			this.camMain.orthographicSize = num5 * num3;
		}
		this.camMain.GetComponent<GameRenderer>().SetZoom(this.camMain.orthographicSize);
	}

	public void CamCenter(CondOwner co)
	{
		this.camFollow = true;
		CrewSim.coCamCenter = co;
	}

	public void CamCenterTravel()
	{
		Vector2 v = Vector2.zero;
		if (!CrewSim.bShipEdit)
		{
			if (CrewSim.coCamCenter == null)
			{
				return;
			}
			v = CrewSim.coCamCenter.tf.position;
		}
		if (this.camMain.transform.position.x == 0f && this.camMain.transform.position.y == 0f)
		{
			this.camMain.transform.Translate(v);
		}
		this.camTravel.x = v.x - this.camMain.transform.position.x;
		this.camTravel.y = v.y - this.camMain.transform.position.y;
	}

	public static void ResetTimeScale()
	{
		Time.timeScale = 1f;
		CrewSim.OnTimeScaleUpdated.Invoke();
	}

	public static void TimeScaleMult(float fMultiplier)
	{
		Time.timeScale = MathUtils.Clamp(Time.timeScale * fMultiplier, 0.25f, 16f);
		CrewSim.OnTimeScaleUpdated.Invoke();
	}

	public static void ToggleSFF()
	{
		if (CrewSim.goUI == null)
		{
			CrewSim.tplCurrentUI = null;
		}
		if (CrewSim.tplCurrentUI != null && CrewSim.tplCurrentUI.Item1 == "FFWD" && CrewSim.goUI != null)
		{
			CrewSim.LowerUI(CrewSim.tplLastUI != null && CrewSim.tplLastUI.Item1 != "FFWD");
		}
		else if (CrewSim.GetSelectedCrew() != null && !CrewSim.GetSelectedCrew().HasCond("IsInChargen"))
		{
			CrewSim.RaiseUI("FFWD", CrewSim.GetSelectedCrew());
		}
	}

	public static float TimeElapsedScaled()
	{
		return Time.deltaTime * CrewSim.fTimeCoeffPause;
	}

	public static float TimeElapsedUnscaled()
	{
		return Mathf.Min(Time.unscaledDeltaTime, 0.1f);
	}

	private void ToggleAutotask(bool autoMode)
	{
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew == null)
		{
			return;
		}
		if (autoMode && selectedCrew.HasCond("IsAIManual"))
		{
			CrewSim.AIManual(false);
		}
		else if (!autoMode && !selectedCrew.HasCond("IsAIManual"))
		{
			CrewSim.AIManual(true);
		}
	}

	public static void AIManual(bool manualMode)
	{
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew == null)
		{
			return;
		}
		if (manualMode)
		{
			if (!selectedCrew.HasCond("IsAIManual"))
			{
				selectedCrew.AddCondAmount("IsAIManual", 1.0, 0.0, 0f);
				selectedCrew.ZeroCondAmount("IsFollowCommand");
				PlayerMarker.AddMarker(selectedCrew);
			}
		}
		else if (selectedCrew.HasCond("IsAIManual"))
		{
			selectedCrew.ZeroCondAmount("IsAIManual");
			PlayerMarker.AddMarker(selectedCrew);
		}
		CrewSim.chkAIAuto.onValueChanged.RemoveListener(new UnityAction<bool>(CrewSim.objInstance.ToggleAutotask));
		CrewSim.chkAIAuto.isOn = !manualMode;
		CrewSim.chkAIAuto.onValueChanged.AddListener(new UnityAction<bool>(CrewSim.objInstance.ToggleAutotask));
	}

	public static void SetCashButton(double dfAmount)
	{
		CrewSim.txtCash.text = "$" + dfAmount.ToString("n");
		if (CrewSim.bRaiseUI && CrewSim.goUI != null)
		{
			GUIFinance component = CrewSim.goUI.GetComponent<GUIFinance>();
			if (component != null)
			{
				component.Refresh();
			}
		}
	}

	public List<CondOwner> FindCOsAtMousePosition(CondOwner coUs, bool bInteractive)
	{
		return this.FindCOsAtWorldPosition(this.ActiveCam.ScreenPointToRay(Input.mousePosition).origin, coUs, bInteractive);
	}

	public List<CondOwner> FindCOsAtWorldPosition(Vector3 vPos, CondOwner coUs, bool bInteractive)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (bInteractive)
		{
			if (coUs != null && coUs.bAlive)
			{
				List<CondOwner> list2 = new List<CondOwner>();
				CrewSim.shipCurrentLoaded.GetCOsAtWorldCoords1(vPos, null, true, true, list2);
				foreach (CondOwner condOwner in list2)
				{
					Debug.Log(condOwner.strName);
					if (!(condOwner.objCOParent != null))
					{
						foreach (string strName in condOwner.aInteractions)
						{
							Interaction interaction = DataHandler.GetInteraction(strName, null, false);
							if (interaction != null)
							{
								interaction.objThem = condOwner;
								if (interaction.Triggered(coUs, condOwner, false, true, false, true, null) && !list.Contains(condOwner))
								{
									list.Add(condOwner);
								}
							}
						}
					}
				}
			}
		}
		else
		{
			if (CrewSim.shipCurrentLoaded != null)
			{
				CrewSim.shipCurrentLoaded.GetCOsAtWorldCoords1(vPos, null, true, true, list);
			}
			if (list.Count == 0)
			{
				foreach (Ship ship in MonoSingleton<AsyncShipLoader>.Instance.GetLoadedShips())
				{
					if (ship != null)
					{
						ship.GetCOsAtWorldCoords1(vPos, null, true, true, list);
					}
				}
			}
		}
		return list;
	}

	public static List<AvailableActionDTO> GetAvailActionsForCO(CondOwner coUs, CondOwner coTarget)
	{
		List<AvailableActionDTO> list = new List<AvailableActionDTO>();
		if (coUs == null || coTarget == null)
		{
			return list;
		}
		Interaction interactionCurrent = coUs.GetInteractionCurrent();
		bool flag = false;
		if (interactionCurrent != null && interactionCurrent.strName != "Walk")
		{
			Interaction interaction = DataHandler.GetInteraction("CancelAction", null, false);
			if (interaction != null)
			{
				interaction.objUs = coUs;
				interaction.objThem = coUs;
				interaction.bManual = true;
				CustomActionDTO dto = new CustomActionDTO(interaction);
				CrewSim.TryAddAction(list, dto);
				flag = true;
			}
		}
		Pathfinder pathfinder = coUs.Pathfinder;
		Tile tilDest = null;
		if (pathfinder != null)
		{
			tilDest = pathfinder.tilDest;
		}
		bool flag2 = coTarget.Crew == null && coTarget.HasCond("IsSolid");
		foreach (string strName in coUs.aAttackIAs)
		{
			Interaction interaction = DataHandler.GetInteraction(strName, null, false);
			if (interaction != null)
			{
				interaction.objUs = coUs;
				interaction.objThem = coTarget;
				interaction.bManual = true;
				CustomActionDTO customActionDTO = null;
				if (interaction.bPassThrough && coTarget.objContainer != null)
				{
					List<CondOwner> cos = coTarget.objContainer.GetCOs(false, interaction.CTTestThem);
					if (cos.Count > 0)
					{
						interaction.objThem = cos[0];
					}
					customActionDTO = new CustomActionDTO(interaction);
				}
				if (flag2 && interaction.attackMode != null && interaction.attackMode.bAllowOnInanimate)
				{
					interaction.CTTestThem = null;
					interaction.PSpecTestThem = null;
				}
				if (interaction.Triggered(interaction.objUs, interaction.objThem, false, false, false, true, null))
				{
					if (interaction.aLootItemUseContract != null && interaction.aLootItemUseContract.Count > 0)
					{
						Interaction interaction2 = interaction;
						interaction2.strTitle += interaction.aLootItemUseContract[0].ShortName;
					}
					if (customActionDTO != null)
					{
						CrewSim.TryAddAction(list, customActionDTO);
					}
					else
					{
						CrewSim.TryAddAction(list, new AvailableActionDTO(interaction, false)
						{
							IsClickable = !flag
						});
					}
				}
			}
		}
		List<string> lootNames = DataHandler.GetLoot("TXTPlayerCombatInteractions").GetLootNames(null, false, null);
		foreach (string strName2 in lootNames)
		{
			Interaction interaction = DataHandler.GetInteraction(strName2, null, false);
			if (interaction != null)
			{
				interaction.objUs = coUs;
				interaction.objThem = coTarget;
				interaction.bManual = true;
				if (interaction.Triggered(coUs, coTarget, false, false, false, true, null))
				{
					CrewSim.TryAddAction(list, new AvailableActionDTO(interaction, false)
					{
						IsClickable = !flag
					});
				}
			}
		}
		foreach (JsonJobSave jsonJobSave in GigManager.aJobs)
		{
			if (jsonJobSave != null && jsonJobSave.bTaken)
			{
				Interaction interactionDo = jsonJobSave.GetInteractionDo(coUs, coTarget);
				if (interactionDo != null)
				{
					interactionDo.bManual = true;
					AvailableActionDTO availableActionDTO = new AvailableActionDTO(interactionDo, false);
					availableActionDTO.IsClickable = !flag;
					availableActionDTO.IsGig = true;
					if (interactionDo.nMoveType == Interaction.MoveType.DEFAULT)
					{
						interactionDo.nMoveType = Interaction.MoveType.GIG;
					}
					CrewSim.TryAddAction(list, availableActionDTO);
				}
			}
		}
		bool flag3 = false;
		if (coTarget.socUs != null)
		{
			foreach (ReplyThread replyThread in coTarget.aReplies)
			{
				if (replyThread.strID == coUs.strName && !replyThread.bDone)
				{
					Interaction interaction = DataHandler.GetInteraction(replyThread.jis.strName, null, false);
					if (interaction != null && interaction.bSocial)
					{
						AvailableActionDTO availableActionDTO2 = CrewSim.InjectRecoveryIa(coUs);
						if (availableActionDTO2 != null)
						{
							CrewSim.TryAddAction(list, availableActionDTO2);
						}
						else
						{
							interaction = DataHandler.GetInteraction("WaitReply", null, false);
							if (interaction != null)
							{
								interaction.objUs = coUs;
								interaction.objThem = coTarget;
								interaction.bManual = true;
								CrewSim.TryAddAction(list, new AvailableActionDTO(interaction, false)
								{
									IsClickable = !flag
								});
								flag3 = true;
							}
						}
						break;
					}
				}
			}
		}
		bool flag4 = false;
		if (!flag3)
		{
			foreach (ReplyThread replyThread2 in coUs.aReplies)
			{
				if (replyThread2.strID == coTarget.strName && !replyThread2.bDone)
				{
					Interaction interaction3 = DataHandler.GetInteraction(replyThread2.jis.strName, replyThread2.jis, true);
					if (interaction3 != null)
					{
						foreach (string text in interaction3.aInverse)
						{
							string[] array = text.Split(new char[]
							{
								','
							});
							Interaction interaction4 = DataHandler.GetInteraction(array[0], null, false);
							if (!(array[0] == "SOCBlank"))
							{
								if (interaction4 != null)
								{
									interaction3.AssignReplyRoles(interaction4, array, false);
									interaction4.bManual = true;
									interaction4.strPlot = interaction3.strPlot;
									if (interaction4.objUs == coUs && interaction4.Triggered(coUs, coTarget, false, !interaction4.bSocial, false, true, null))
									{
										CrewSim.TryAddAction(list, new AvailableActionDTO(interaction4, true)
										{
											IsClickable = !flag
										});
										flag4 = true;
									}
								}
							}
						}
					}
					DataHandler.ReleaseTrackedInteraction(interaction3);
				}
			}
		}
		if (!flag4 && !flag3 && coUs.socUs != null)
		{
			Relationship relationship = coUs.socUs.GetRelationship(coTarget.strName);
			if (relationship != null && relationship.strContext != null && relationship.strContext != "Default")
			{
				foreach (string strName3 in DataHandler.GetLoot(relationship.strContext).GetLootNames(null, false, null))
				{
					Interaction interaction = DataHandler.GetInteraction(strName3, null, false);
					if (interaction != null)
					{
						interaction.objUs = coUs;
						interaction.objThem = coTarget;
						interaction.bManual = true;
						if (interaction.Triggered(interaction.objUs, interaction.objThem, false, !interaction.bSocial, false, true, null))
						{
							CrewSim.TryAddAction(list, new AvailableActionDTO(interaction, true)
							{
								IsClickable = !flag
							});
						}
						flag4 = true;
					}
				}
			}
		}
		if (!flag4 && !flag3)
		{
			if (PlotManager.bDebugLogging)
			{
				Debug.Log("<color=#992299ff>--------Starting QAB Checks--------</color> ");
			}
			foreach (Interaction interaction5 in PlotManager.GetAllPlotQABs(coUs, coTarget))
			{
				if (interaction5 != null)
				{
					CrewSim.TryAddAction(list, new AvailableActionDTO(interaction5, false)
					{
						IsClickable = !flag
					});
				}
			}
			if (PlotManager.bDebugLogging)
			{
				Debug.Log("<color=#992299ff>--------Ending QAB Checks--------</color> ");
			}
			foreach (string text2 in coTarget.aInteractions)
			{
				if (text2 != null)
				{
					if (text2.IndexOf("Drop") != 0 || coTarget.HasCond("IsHuman") || coTarget.HasCond("IsRobot"))
					{
						Interaction interaction = DataHandler.GetInteraction(text2, null, false);
						if (interaction != null)
						{
							interaction.objUs = coUs;
							interaction.objThem = coTarget;
							interaction.bManual = true;
							CustomActionDTO customActionDTO2 = null;
							if (interaction.bPassThrough && coTarget.objContainer != null)
							{
								List<CondOwner> cos2 = coTarget.objContainer.GetCOs(false, interaction.CTTestThem);
								if (cos2.Count > 0)
								{
									interaction.objThem = cos2[0];
								}
								customActionDTO2 = new CustomActionDTO(interaction);
							}
							if (interaction.Triggered(interaction.objUs, interaction.objThem, false, !interaction.bSocial, false, true, null))
							{
								if (customActionDTO2 != null)
								{
									CrewSim.TryAddAction(list, customActionDTO2);
								}
								else
								{
									CrewSim.TryAddAction(list, new AvailableActionDTO(interaction, false)
									{
										IsClickable = !flag
									});
								}
							}
						}
					}
				}
			}
		}
		if (!(coTarget.socUs != null) || coUs == CrewSim.coPlayer)
		{
		}
		if (DataHandler.GetCondTrigger("TIsJettisonableBody").Triggered(coTarget, null, true) && TileUtils.IsExposedToSpace(coTarget) && coTarget != CrewSim.coPlayer)
		{
			Interaction interaction6 = null;
			if (coTarget.HasCond("IsHuman"))
			{
				interaction6 = DataHandler.GetInteraction("JettisonCorpse", null, false);
			}
			else if (coTarget.HasCond("IsRobot"))
			{
				interaction6 = DataHandler.GetInteraction("JettisonRobot", null, false);
			}
			if (interaction6 != null)
			{
				interaction6.objUs = coUs;
				interaction6.objThem = coTarget;
				interaction6.bManual = true;
				CrewSim.TryAddAction(list, new AvailableActionDTO(interaction6, false)
				{
					IsClickable = !flag
				});
			}
		}
		if (CrewSim.objInstance.workManager.COIDHasTasks(coTarget.strID))
		{
			bool flag5 = false;
			foreach (AvailableActionDTO availableActionDTO3 in list)
			{
				Interaction ia = availableActionDTO3.Ia;
				if (ia.strName == "ACTCancelTaskThem")
				{
					flag5 = true;
					break;
				}
			}
			Interaction interaction;
			if (!flag5)
			{
				interaction = DataHandler.GetInteraction("ACTCancelTaskThem", null, false);
				if (interaction != null)
				{
					interaction.objUs = coUs;
					interaction.objThem = coTarget;
					interaction.bManual = true;
					AvailableActionDTO dto2 = new AvailableActionDTO(interaction, false);
					CrewSim.TryAddAction(list, dto2);
				}
			}
			interaction = CrewSim.GetSelectedCrew().GetInteractionCurrent();
			if (interaction == null || interaction.objThem != coTarget)
			{
				interaction = DataHandler.GetInteraction("ACTResumeTaskThem", null, false);
				if (interaction != null)
				{
					interaction.objUs = coUs;
					interaction.objThem = coTarget;
					interaction.bManual = true;
					AvailableActionDTO dto3 = new AvailableActionDTO(interaction, false);
					CrewSim.TryAddAction(list, dto3);
				}
			}
		}
		CrewSim.TryAddAction(list, CrewSim.InjectRecoveryIa(coUs));
		if (pathfinder != null)
		{
			pathfinder.tilDest = tilDest;
		}
		return list;
	}

	private static AvailableActionDTO InjectRecoveryIa(CondOwner coUs)
	{
		if (!coUs.HasCond("Stunned") && !coUs.HasCond("Recovering"))
		{
			return null;
		}
		Interaction interaction;
		if (coUs.HasCond("Stunned"))
		{
			interaction = DataHandler.GetInteraction("QABStunned", null, false);
		}
		else
		{
			interaction = DataHandler.GetInteraction("QABRecovering", null, false);
		}
		interaction.objUs = coUs;
		interaction.objThem = coUs;
		interaction.bManual = true;
		return new AvailableActionDTO(interaction, false)
		{
			IsClickable = false
		};
	}

	private static void TryAddAction(List<AvailableActionDTO> interactions, AvailableActionDTO dto)
	{
		if (interactions == null || dto == null || dto.Ia == null)
		{
			return;
		}
		foreach (AvailableActionDTO availableActionDTO in interactions)
		{
			Interaction ia = availableActionDTO.Ia;
			if (ia.strName == dto.Ia.strName && ia.objThem == dto.Ia.objThem)
			{
				availableActionDTO.IsReply = (availableActionDTO.IsReply || dto.IsReply);
				return;
			}
		}
		interactions.Add(dto);
	}

	public void LowerContextMenu()
	{
		this.contextMenuPool.Reset();
	}

	private void ToggleBGMode(Toggle chk)
	{
		CrewSim.bShipEditBG = chk.isOn;
		CanvasGroup component = this.goShipEdit.transform.Find("txtBG").GetComponent<CanvasGroup>();
		if (CrewSim.bShipEditBG)
		{
			component.alpha = 1f;
		}
		else
		{
			component.alpha = 0f;
		}
		this.SetPartCursor(null);
		this.SetBracketTarget(null, false, false);
	}

	public void ToggleAmbientLight(Toggle chk)
	{
		if (chk.isOn)
		{
			this.camMain.cullingMask |= 1 << LayerMask.NameToLayer("Default");
			this.camMain.GetComponent<GameRenderer>().StencilCam.cullingMask |= 1 << LayerMask.NameToLayer("Default");
		}
		else
		{
			this.camMain.cullingMask &= ~(1 << LayerMask.NameToLayer("Default"));
			this.camMain.GetComponent<GameRenderer>().StencilCam.cullingMask &= ~(1 << LayerMask.NameToLayer("Default"));
		}
	}

	public void TogglePowerUI(Ship objShip, Toggle toggle = null)
	{
		if (toggle != null)
		{
			CrewSim.PowerVizVisible = toggle.isOn;
		}
		else
		{
			CrewSim.PowerVizVisible = !CrewSim.PowerVizVisible;
		}
		if (GUIPDA.instance != null)
		{
			GUIPDA.instance.pdaVisualisers.TogglePowerQuietly(CrewSim.PowerVizVisible);
		}
		if (CrewSim.PowerVizVisible)
		{
			GameObject original = Resources.Load<GameObject>("prefabPwrLine");
			this.linePower = UnityEngine.Object.Instantiate<GameObject>(original).GetComponent<VectorObject2D>().vectorLine;
			this.linePower.color = Color.yellow;
			this.linePower.points2.Clear();
			this.linePower.SetCanvas(CrewSim.CanvasManager.goCanvasWorldGUI, false);
			this.linePower.rectTransform.gameObject.name = this.linePower.rectTransform.gameObject.name + "Instance";
			this.camMain.cullingMask |= 1 << LayerMask.NameToLayer("Power Overlay");
			this.camMain.GetComponent<GameRenderer>().StencilCam.cullingMask |= 1 << LayerMask.NameToLayer("Power Overlay");
			if (CrewSim.coPlayer != null)
			{
				CrewSim.coPlayer.ZeroCondAmount("TutorialPowerVizWaiting");
				MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			}
		}
		else
		{
			VectorLine.Destroy(ref this.linePower);
			this.camMain.cullingMask &= ~(1 << LayerMask.NameToLayer("Power Overlay"));
			this.camMain.GetComponent<GameRenderer>().StencilCam.cullingMask &= ~(1 << LayerMask.NameToLayer("Power Overlay"));
		}
		CrewSim.SetToggleWithoutNotify(CrewSim.chkPwrSE, CrewSim.PowerVizVisible);
	}

	private void Options()
	{
		CanvasGroup component = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/prefabGUIOptions").GetComponent<CanvasGroup>();
		component.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, true, false);
		component.interactable = true;
		component.blocksRaycasts = true;
	}

	private void Manual()
	{
		CanvasGroup component = CrewSim.CanvasManager.goCanvasQuit.transform.Find("GUIQuit/pnlManual").GetComponent<CanvasGroup>();
		component.GetComponent<GUIPanelFade>().Reset(0.25f, 0f, true, false);
		component.interactable = true;
		component.blocksRaycasts = true;
	}

	public void PopupQuitToMenu()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this._confirmationDialoguePrefab, CrewSim.CanvasManager.goCanvasQuit.transform);
		Color clrBg = new Color(0.10980392f, 0.10980392f, 0.10980392f);
		Color clrFg = new Color(0.05882353f, 0.05882353f, 0.05882353f);
		Color clrFont = new Color(0.78431374f, 0.78431374f, 0.78431374f);
		gameObject.GetComponent<GUIConfirmationDialogue>().Setup(DataHandler.GetString("GUI_CONFIRM_QTM", false), delegate()
		{
			this.QuitToMenu(false);
		}, clrBg, clrFg, clrFont);
	}

	public void QuitToMenu(bool bSave)
	{
		if (CanvasManager.instance.State != CanvasManager.GUIState.GAMEOVER && CrewSim.objGUISaveOnClose.isOn)
		{
			bSave = true;
		}
		AudioManager.am.ShutDown();
		CrewSim.ResetTimeScale();
		CanvasManager.instance.Black();
		MainMenu.bCueMenuMusic = true;
		CrewSim.bPauseLock = false;
		if (!CrewSim.bShipEdit && bSave)
		{
			MonoSingleton<GUILoadingPopUp>.Instance.ShowTooltip(DataHandler.GetString("LOAD_SAVING", false), DataHandler.GetString("LOAD_WAIT", false));
			base.StartCoroutine(this.QueueSaveAndExit());
		}
		else
		{
			this.QueueMainMenu();
		}
	}

	public void PopupQuitToDesktop()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this._confirmationDialoguePrefab, CrewSim.CanvasManager.goCanvasQuit.transform);
		Color clrBg = new Color(0.10980392f, 0.10980392f, 0.10980392f);
		Color clrFg = new Color(0.05882353f, 0.05882353f, 0.05882353f);
		Color clrFont = new Color(0.78431374f, 0.78431374f, 0.78431374f);
		gameObject.GetComponent<GUIConfirmationDialogue>().Setup(DataHandler.GetString("GUI_CONFIRM_QTD", false), delegate()
		{
			this.QuitToDesktop(false);
		}, clrBg, clrFg, clrFont);
	}

	public void QuitToDesktop(bool bSave)
	{
		if (CrewSim.objGUISaveOnClose.isOn)
		{
			bSave = true;
		}
		AudioManager.am.ShutDown();
		CrewSim.ResetTimeScale();
		CanvasManager.instance.Black();
		MainMenu.bCueMenuMusic = true;
		if (!CrewSim.bShipEdit && bSave)
		{
			MonoSingleton<GUILoadingPopUp>.Instance.ShowTooltip(DataHandler.GetString("LOAD_SAVING", false), DataHandler.GetString("LOAD_WAIT", false));
			base.StartCoroutine(this.QueueSaveAndDesktop());
		}
		else
		{
			this.QueueDesktop();
		}
	}

	private IEnumerator QueueSaveAndExit()
	{
		yield return null;
		LoadManager.OnSaveFinished.AddListener(new UnityAction(this.QueueMainMenu));
		MonoSingleton<LoadManager>.Instance.AutoSave(false);
		yield break;
	}

	private void QueueMainMenu()
	{
		LoadManager.OnSaveFinished.RemoveListener(new UnityAction(this.QueueMainMenu));
		CrewSim.QueueScene("MainMenu2", 1.5f);
	}

	private IEnumerator QueueSaveAndDesktop()
	{
		yield return null;
		LoadManager.OnSaveFinished.AddListener(new UnityAction(this.QueueDesktop));
		MonoSingleton<LoadManager>.Instance.AutoSave(false);
		yield break;
	}

	private void QueueDesktop()
	{
		LoadManager.OnSaveFinished.RemoveListener(new UnityAction(this.QueueDesktop));
		Application.Quit();
	}

	public Bounds GetViewportBounds(Camera camera, Vector3 v1, Vector3 v2)
	{
		Vector3 min = Vector3.Min(v1, v2);
		Vector3 max = Vector3.Max(v1, v2);
		min.z = -100f;
		max.z = 100f;
		Bounds result = default(Bounds);
		result.SetMinMax(min, max);
		return result;
	}

	private void SelectBounds(Bounds bnd, bool bTiles)
	{
		List<CondOwner> list = new List<CondOwner>();
		if (bTiles)
		{
			foreach (Ship ship in CrewSim.aLoadedShips)
			{
				foreach (Tile tile in ship.aTiles)
				{
					list.Add(tile.coProps);
				}
			}
		}
		else
		{
			list = CrewSim.shipCurrentLoaded.GetICOs1(null, false, true, false);
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsWall1x1Installed");
		CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsFloorGrate01Installed");
		Vector3 point = default(Vector3);
		int num = 0;
		int num2 = 0;
		foreach (CondOwner condOwner in list)
		{
			if (!(condOwner == null))
			{
				point = condOwner.transform.position;
				if (bnd.Contains(point))
				{
					this.SelectCO(condOwner, false);
					if (condTrigger.Triggered(condOwner, null, true))
					{
						num++;
					}
					if (condTrigger2.Triggered(condOwner, null, true))
					{
						num2++;
					}
				}
			}
		}
		CrewSim.txtBtnReplaceFloors.text = DataHandler.GetString("GUI_SHIPEDIT_REPLACE_FLOORS", false).Replace("XXX", num2.ToString());
		CrewSim.txtBtnReplaceWalls.text = DataHandler.GetString("GUI_SHIPEDIT_REPLACE_WALLS", false).Replace("XXX", num.ToString());
		if (bTiles)
		{
			CrewSim.OnTileSelectionUpdated.Invoke(CrewSim.aSelected);
		}
	}

	private void PaintBounds(Bounds bnd)
	{
		Vector3 min = bnd.min;
		for (float num = TileUtils.GridAlign(bnd.min.x); num <= bnd.max.x; num += 1f)
		{
			for (float num2 = TileUtils.GridAlign(bnd.min.y); num2 <= bnd.max.y; num2 += 1f)
			{
				min.x = num;
				min.y = num2;
				this.PaintPos(min);
			}
		}
	}

	private void PaintPos(Vector2 vPos)
	{
		if (this.goSelPart != null)
		{
			float z = this.goSelPart.transform.rotation.eulerAngles.z;
			Item component = this.goSelPart.GetComponent<Item>();
			if (component == null)
			{
				return;
			}
			component.SetToMousePosition(vPos);
			if (CrewSim.bShipEditBG)
			{
				if (CrewSim.shipCurrentLoaded.BGItemFits(component))
				{
					CrewSim.shipCurrentLoaded.BGItemAdd(component);
					Debug.Log(string.Concat(new object[]
					{
						"Placing BG at: ",
						component.transform.position,
						"; local: ",
						component.transform.localPosition
					}));
				}
				this.goSelPart.layer = LayerMask.NameToLayer("Default");
				this.SetPartCursor(this.goSelPart.name);
				component = this.goSelPart.GetComponent<Item>();
				component.fLastRotation = z;
			}
			else if (GUIInventory.instance.Selected == null && component.CheckFit(component.rend.bounds.center, CrewSim.shipCurrentLoaded, TileUtils.aSelPartTiles, null))
			{
				this.nLastClickIndex = 0;
				this.vLastClick = default(Vector2);
				this.goSelPart.layer = LayerMask.NameToLayer("Default");
				if (CrewSim.iaItmInstall != null)
				{
					this.InstallFinish();
					if (CrewSim.bContinuePaintingJob)
					{
						this.StartPaintingJob(CrewSim.jiLast);
						component = this.goSelPart.GetComponent<Item>();
						component.fLastRotation = z;
					}
					else
					{
						CondOwner selectedCrew = CrewSim.GetSelectedCrew();
						int hourFromS = MathUtils.GetHourFromS(StarSystem.fEpoch);
						if (selectedCrew != null && selectedCrew.Company.GetShift(hourFromS, selectedCrew).nID != 2)
						{
							selectedCrew.LogMessage(selectedCrew.FriendlyName + DataHandler.GetString("SHIFT_WARN_NONWORK", false), "Bad", selectedCrew.strID);
						}
					}
					CrewSim.bContinuePaintingJob = true;
				}
				else if (CrewSim.iaItmInstall == null)
				{
					CrewSim.bPoolShipUpdates = true;
					Debug.Log("bPoolShipUpdates = true");
					CrewSim.shipCurrentLoaded.AddCO(this.goSelPart.GetComponent<CondOwner>(), true);
					this.SetPartCursor(this.goSelPart.GetComponent<CondOwner>().strName);
					component = this.goSelPart.GetComponent<Item>();
					component.fLastRotation = z;
				}
			}
		}
		else if (CrewSim.jiLast != null)
		{
			if (CrewSim.jiLast.strName == "Cancel")
			{
				List<CondOwner> list = this.FindCOsAtWorldPosition(vPos, null, false);
				foreach (CondOwner condOwner in list)
				{
					Placeholder component2 = condOwner.GetComponent<Placeholder>();
					if (component2 != null)
					{
						CondOwner condOwner2 = DataHandler.GetCondOwner(component2.strInstalledCO);
						if (GUIPDA.ctJobFilter != null && !GUIPDA.ctJobFilter.Triggered(condOwner2, null, true))
						{
							this.ScheduleCODestruction(condOwner2);
							continue;
						}
						this.ScheduleCODestruction(condOwner2);
					}
					else if (component2 == null && GUIPDA.ctJobFilter != null && !GUIPDA.ctJobFilter.Triggered(condOwner, null, true))
					{
						continue;
					}
					this.workManager.RemoveTask(condOwner.strID);
					if (component2 != null)
					{
						component2.Cancel(null);
					}
				}
			}
			else if (CrewSim.jiLast.strName == "Uninstall" || CrewSim.jiLast.strName == "Scrap" || CrewSim.jiLast.strName == "Repair" || CrewSim.jiLast.strName == "Dismantle")
			{
				List<CondOwner> list2 = this.FindCOsAtWorldPosition(vPos, null, false);
				foreach (CondOwner condOwner3 in list2)
				{
					if (GUIPDA.ctJobFilter == null || GUIPDA.ctJobFilter.Triggered(condOwner3, null, true))
					{
						List<string> jobActions = condOwner3.GetJobActions(CrewSim.jiLast.strName);
						foreach (string text in jobActions)
						{
							Interaction interaction = DataHandler.GetInteraction(text, null, false);
							if (interaction != null && interaction.Triggered(CrewSim.GetSelectedCrew(), condOwner3, false, true, false, true, null))
							{
								Task2 task = new Task2();
								task.strDuty = "Construct";
								task.strInteraction = text;
								task.strTargetCOID = condOwner3.strID;
								task.strName = CrewSim.jiLast.strName + "Job" + condOwner3.strID;
								this.workManager.AddTask(task, 1);
								break;
							}
						}
					}
				}
			}
			else if (CrewSim.jiLast.strName == "Haul")
			{
				List<CondOwner> list3 = this.FindCOsAtWorldPosition(vPos, null, false);
				foreach (CondOwner condOwner4 in list3)
				{
					if (WorkManager.CTHaul.Triggered(condOwner4, null, true))
					{
						Task2 task2 = new Task2();
						task2.strDuty = "Haul";
						task2.strInteraction = "ACTHaulItem";
						task2.strTargetCOID = condOwner4.strID;
						task2.strName = "HaulJob" + condOwner4.strID;
						this.workManager.AddTask(task2, 1);
					}
				}
			}
		}
	}

	private void SelectCO(CondOwner co, bool bUnselect = false)
	{
		Debug.Log("Selecting: " + co.strName);
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (bUnselect)
		{
			CrewSim.aSelected.Remove(co);
			if (co != null)
			{
				co.Selected = false;
			}
			CrewSim.inventoryGUI.UnsetDoll();
		}
		else if (co != null && CrewSim.aSelected.IndexOf(co) < 0)
		{
			CrewSim.aSelected.Add(co);
			co.Selected = true;
			CrewSim.inventoryGUI.UnsetDoll();
		}
		this.UpdateLog(co, null);
		if (CrewSim.bShipEdit)
		{
			foreach (GameObject obj in this.aFields)
			{
				UnityEngine.Object.Destroy(obj);
			}
			this.aFields.Clear();
			if (CrewSim.aSelected.Count == 1)
			{
				Text text = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/scrPartEdit/Viewport/pnlPartEdit/lblName").GetComponent<Text>();
				if (co.strNameFriendly != null)
				{
					text.text = co.strNameFriendly;
				}
				else
				{
					text.text = co.strName;
				}
				Texture2D texture2D = DataHandler.LoadPNG(co.strPortraitImg + ".png", false, false);
				Image component = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/scrPartEdit/Viewport/pnlPartEdit/bmpImage").GetComponent<Image>();
				component.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
				Text component2 = CrewSim.CanvasManager.goCanvasShipEdit.transform.Find("ShipEdit/scrPartEdit/Viewport/pnlPartEdit/lblID").GetComponent<Text>();
				component2.text = "ID: " + co.strID;
				component2.transform.SetParent(null, false);
				Button original = Resources.Load<Button>("prefabBtnPartEdit");
				if (co.mapGUIPropMaps != null)
				{
					using (Dictionary<string, Dictionary<string, string>>.KeyCollection.Enumerator enumerator2 = co.mapGUIPropMaps.Keys.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							string strKey = enumerator2.Current;
							Dictionary<string, string> dictionary = co.mapGUIPropMaps[strKey];
							if (dictionary != null && dictionary.ContainsKey("strGUIPrefab"))
							{
								Button button = UnityEngine.Object.Instantiate<Button>(original);
								text = button.GetComponentInChildren<Text>();
								text.text = dictionary["strFriendlyName"];
								button.transform.SetParent(this.pnlPartEdit.transform, false);
								button.onClick.AddListener(delegate()
								{
									if (co != null)
									{
										CrewSim.RaiseUI(strKey, co);
									}
								});
								this.aFields.Add(button.gameObject);
							}
						}
					}
				}
				component2.transform.SetParent(this.pnlPartEdit.transform, false);
			}
		}
		if (CrewSim.inventoryGUI.IsOpen && (selectedCrew != co || CrewSim.inventoryGUI.PaperDollManager.strCOIDLast != co.strID))
		{
			CommandInventory.ToggleInventory(co, false);
			CommandInventory.ToggleInventory(co, false);
		}
		if (selectedCrew != co)
		{
			CrewSim.ResetAutoPause();
		}
		if ((co.HasCond("IsPlayerCrew") || co.HasCond("IsPlayer")) && !bUnselect)
		{
			CrewSim.AIManual(co.HasCond("IsAIManual"));
		}
	}

	public void ShowBlocksAndLights(CondOwner objCO, bool bShow)
	{
		if (objCO == null)
		{
			return;
		}
		this.ShowBlocksAndLights(objCO.Item, bShow);
	}

	public void ShowBlocksAndLights(Item item, bool bShow)
	{
		if (item == null)
		{
			return;
		}
		bool flag = false;
		foreach (Block block in item.aBlocks)
		{
			if (bShow)
			{
				if (CrewSim.blocks.Add(block))
				{
					block.UpdateStats();
					flag = true;
				}
			}
			else
			{
				flag |= CrewSim.blocks.Remove(block);
			}
		}
		float num = -1f;
		foreach (global::Visibility visibility in item.aLights)
		{
			if (visibility.Radius > num)
			{
				num = visibility.Radius;
			}
			if (bShow)
			{
				CrewSim.aLights.Add(visibility);
			}
			else
			{
				CrewSim.aLights.Remove(visibility);
			}
		}
		if (flag && !CrewSim.bPoolVisUpdates)
		{
			num = ((num >= 0f) ? num : global::Visibility.DEFAULTVISIBILITYRANGE);
			this.UpdateVisLights(item.transform.position.ToVector2(), num);
		}
	}

	private void UpdateVisLights(Vector2 positionOther, float range = 0f)
	{
		float num = range * range;
		foreach (global::Visibility visibility in CrewSim.aLights)
		{
			if (!(visibility.GO == null) && visibility.GO.activeInHierarchy)
			{
				if (MathUtils.GetDistanceSquared(positionOther, visibility.transform.position.ToVector2()) <= num)
				{
					visibility.bRedraw = true;
				}
			}
		}
		if (MathUtils.GetDistanceSquared(positionOther, this.visPlayer.transform.position.ToVector2()) <= num)
		{
			this.visPlayer.bRedraw = true;
		}
		CrewSim.bPoolVisUpdates = false;
	}

	private void UpdateVisLights()
	{
		foreach (global::Visibility visibility in CrewSim.aLights)
		{
			if (!(visibility.GO == null) && visibility.GO.activeInHierarchy)
			{
				visibility.bRedraw = true;
			}
		}
		this.visPlayer.bRedraw = true;
		CrewSim.bPoolVisUpdates = false;
	}

	private static bool ShouldCondOwnerHighlight(CondOwner co, bool bIgnoreGlass)
	{
		if (co == null || co.bDestroyed || CrewSim.GetSelectedCrew() == null)
		{
			return false;
		}
		if (co.aInteractions.Count == 0)
		{
			return false;
		}
		float num = CrewSim.GetSelectedCrew().tf.position.x - co.tf.position.x;
		float num2 = CrewSim.GetSelectedCrew().tf.position.y - co.tf.position.y;
		return num * num + num2 * num2 <= 100f && global::Visibility.IsCondOwnerLOSVisibleFromPlayer(co, bIgnoreGlass);
	}

	private static bool HighlightRemover(CondOwner co)
	{
		if (co == null || co.bDestroyed)
		{
			return true;
		}
		if (CrewSim.ShouldCondOwnerHighlight(co, true))
		{
			return false;
		}
		co.Highlight = false;
		return true;
	}

	private void UpdateHighlight()
	{
		if (!this.bHighlightInteractable)
		{
			foreach (CondOwner condOwner in this.currentHighlight)
			{
				if (condOwner != null)
				{
					condOwner.Highlight = false;
				}
			}
			this.currentHighlight.Clear();
			return;
		}
		HashSet<CondOwner> hashSet = this.currentHighlight;
		if (CrewSim.<>f__mg$cache1 == null)
		{
			CrewSim.<>f__mg$cache1 = new Predicate<CondOwner>(CrewSim.HighlightRemover);
		}
		hashSet.RemoveWhere(CrewSim.<>f__mg$cache1);
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsHighlightable");
		CrewSim.shipCurrentLoaded.VisitCOs(condTrigger, false, true, false, delegate(CondOwner co)
		{
			if (CrewSim.ShouldCondOwnerHighlight(co, true))
			{
				co.Highlight = true;
				this.currentHighlight.Add(co);
			}
		});
	}

	public Transform MakeGenericVisibility(JsonLight jl, bool lightSprite = false)
	{
		GameObject gameObject = new GameObject();
		Transform transform = gameObject.transform;
		transform.SetParent(CrewSim.shipCurrentLoaded.gameObject.transform);
		transform.name = "Generic Light";
		string cookie = "ItmLitSphere01";
		global::Visibility visibility = UnityEngine.Object.Instantiate<global::Visibility>(global::Visibility.visTemplate);
		visibility.LightColor = DataHandler.GetColor(jl.strColor);
		visibility.GO.name = jl.strName;
		visibility.Parent = transform;
		visibility.tfParent = transform;
		visibility.transform.localScale = new Vector3(2f, 2f, 2f);
		visibility.SetCookie(cookie);
		Vector2 vector = default(Vector2);
		vector = jl.ptPos;
		float x = 1f * vector.x / 16f;
		float y = 1f * vector.y / 16f;
		visibility.ptOffset = new Vector2(x, y);
		if (lightSprite)
		{
			Transform transform2 = DataHandler.GetMesh("prefabQuadLightSprite", null).transform;
			transform2.SetParent(transform);
			transform2.rotation = Quaternion.Euler(-90f, 0f, 0f);
			transform2.localPosition = Vector3.zero;
			Renderer component = transform2.GetComponent<Renderer>();
			component.sharedMaterial = DataHandler.GetMaterial(component, jl.strImg, "blank", "blank", "blank");
			transform2.gameObject.SetActive(true);
		}
		CrewSim.aLights.Add(visibility);
		visibility.GO.SetActive(true);
		return transform;
	}

	public void AddLight(global::Visibility vis)
	{
		CrewSim.aLights.Add(vis);
	}

	public void RemoveLight(global::Visibility vis)
	{
		CrewSim.aLights.Remove(vis);
	}

	public void InstallStart(Interaction iaInstall)
	{
		if (iaInstall == null)
		{
			return;
		}
		if (!DataHandler.dictCOs.ContainsKey(iaInstall.strStartInstall) && !DataHandler.dictCOOverlays.ContainsKey(iaInstall.strStartInstall))
		{
			Debug.Log("Error: Cannot install item of type: " + iaInstall.strStartInstall);
			return;
		}
		this.SetPartCursor(iaInstall.strStartInstall);
		MeshRenderer component = this.goSelPart.GetComponent<MeshRenderer>();
		Vector2 mainTextureOffset = component.sharedMaterial.mainTextureOffset;
		Vector2 mainTextureScale = component.sharedMaterial.mainTextureScale;
		Material material = DataHandler.GetMaterial(component, "GUIGrid16", "blank", "blank", "blank");
		Texture mainTexture = component.sharedMaterial.mainTexture;
		component.material = UnityEngine.Object.Instantiate<Material>(material);
		component.sharedMaterial.mainTexture = mainTexture;
		component.sharedMaterial.mainTextureOffset = mainTextureOffset;
		component.sharedMaterial.mainTextureScale = mainTextureScale;
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		component.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetColor("_Color", new Color(1f, 1f, 1f, 0.5f));
		component.SetPropertyBlock(materialPropertyBlock);
		TileUtils.goPartTiles.SetActive(true);
		iaInstall.objThem.gameObject.SetActive(false);
		CrewSim.iaItmInstall = iaInstall;
		if (CrewSim.inventoryGUI.IsOpen)
		{
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
		}
		if (this.bcombatAutoPauseAllowed)
		{
			CrewSim.Paused = true;
		}
		CrewSim.bUnpauseShield = true;
	}

	private void InstallFinish()
	{
		if (CrewSim.iaItmInstall == null)
		{
			return;
		}
		if (this.goSelPart != null)
		{
			CrewSim.iaItmInstall.objThem.transform.position = this.goSelPart.transform.position;
		}
		CrewSim.iaItmInstall.objThem.strPersistentCO = CrewSim.iaItmInstall.objThem.strPersistentCO;
		CrewSim.iaItmInstall.objThem.strPersistentCT = CrewSim.iaItmInstall.CTTestThem.strName;
		CondOwner coplaceholder = DataHandler.GetCOPlaceholder(this.goSelPart.GetComponent<CondOwner>(), CrewSim.iaItmInstall.objThem, CrewSim.iaItmInstall.strName);
		MeshRenderer component = coplaceholder.GetComponent<MeshRenderer>();
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		component.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetColor("_Color", new Color(1f, 1f, 1f, 0.5f));
		component.SetPropertyBlock(materialPropertyBlock);
		Tile tileAtWorldCoords = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(CrewSim.iaItmInstall.objThem.transform.position.x, CrewSim.iaItmInstall.objThem.transform.position.y, true, true);
		if (tileAtWorldCoords == null)
		{
			tileAtWorldCoords = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(this.vMouse.x, this.vMouse.y, true, true);
		}
		Ship ship;
		if (tileAtWorldCoords == null)
		{
			if (CrewSim.shipCurrentLoaded == null)
			{
				coplaceholder.Destroy();
				return;
			}
			ship = CrewSim.shipCurrentLoaded;
		}
		else
		{
			ship = tileAtWorldCoords.coProps.ship;
		}
		ship.AddCO(coplaceholder, true);
		this.SetPartCursor(null);
		TileUtils.goPartTiles.SetActive(false);
		CrewSim.bContinuePaintingJob = (coplaceholder.strPersistentCO == null);
		CondOwner condOwner = null;
		if (!CrewSim.bContinuePaintingJob && CrewSim.GetSelectedCrew() != null)
		{
			CrewSim.iaItmInstall.objUs = CrewSim.GetSelectedCrew();
			condOwner = CrewSim.iaItmInstall.objThem;
			CrewSim.iaItmInstall.objThem = coplaceholder;
			CrewSim.iaItmInstall.bManual = true;
			this.workManager.ClaimTaskDirect(CrewSim.iaItmInstall);
		}
		else
		{
			Task2 task = new Task2();
			task.strDuty = "Construct";
			task.strInteraction = CrewSim.iaItmInstall.strName;
			task.strName = CrewSim.iaItmInstall.strTitle;
			task.strTargetCOID = coplaceholder.strID;
			this.workManager.AddTask(task, 1);
		}
		if (condOwner != null && !condOwner.gameObject.activeInHierarchy)
		{
			condOwner.Destroy();
		}
		CrewSim.iaItmInstall = null;
		if (ship.DMGStatus == Ship.Damage.Derelict && CrewSim.system.GetShipOwner(ship.strRegID) != CrewSim.coPlayer.strID)
		{
			BeatManager.RunEncounter("ENCFirstInstallDerelict", false);
		}
		CrewSim.Paused = false;
	}

	public void StartAction(string strImgCursor)
	{
		if (this.goPaintJob == null)
		{
			this.goPaintJob = (Resources.Load("prefabBuildPaintGUI") as GameObject);
			this.goPaintJob = UnityEngine.Object.Instantiate<GameObject>(this.goPaintJob, CrewSim.CanvasManager.goCanvasGUI.transform);
		}
		this.goPaintJob.GetComponent<RawImage>().texture = DataHandler.LoadPNG(strImgCursor, false, false);
	}

	public IEnumerator ScrollBottom(ScrollRect sr)
	{
		LayoutRebuilder.MarkLayoutForRebuild(sr.GetComponent<RectTransform>());
		yield return new WaitForSeconds(0.3f);
		sr.verticalNormalizedPosition = 0f;
		yield break;
	}

	public IEnumerator ScrollTop(ScrollRect sr)
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(sr.GetComponent<RectTransform>());
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		sr.verticalNormalizedPosition = 1f;
		yield break;
	}

	public IEnumerator ScrollPos(ScrollRect sr, float fPos)
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate(sr.GetComponent<RectTransform>());
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();
		sr.verticalNormalizedPosition = fPos;
		yield break;
	}

	public void StartPaintingJob(JsonInstallable ji)
	{
		if (ji == null)
		{
			this.FinishPaintingJob();
		}
		else if (ji.strName == "Cancel")
		{
			this.StartAction("GUIActionCancel.png");
		}
		else if (ji.strName == "Uninstall")
		{
			this.StartAction("GUIActionUninstall.png");
		}
		else if (ji.strName == "Scrap")
		{
			this.StartAction("GUIActionScrap.png");
		}
		else if (ji.strName == "Repair")
		{
			this.StartAction("GUIActionRepair.png");
		}
		else if (ji.strName == "Dismantle")
		{
			this.StartAction("GUIActionDismantle.png");
		}
		else if (ji.strName == "Haul")
		{
			this.StartAction("GUIActionHaul.png");
		}
		else
		{
			Interaction interaction = DataHandler.GetInteraction(ji.strInteractionName, null, false);
			if (interaction == null)
			{
				return;
			}
			interaction.objThem = DataHandler.GetCondOwner(ji.strActionCO);
			interaction.objThem.strPersistentCO = ji.strPersistentCO;
			this.InstallStart(interaction);
		}
		CrewSim.jiLast = ji;
	}

	public void FinishPaintingJob()
	{
		UnityEngine.Object.Destroy(this.goPaintJob);
		this.SetPartCursor(null);
		TileUtils.goPartTiles.SetActive(false);
		if (CrewSim.iaItmInstall != null)
		{
			CrewSim.iaItmInstall.objThem.Destroy();
			CrewSim.iaItmInstall.Destroy();
			CrewSim.iaItmInstall = null;
		}
	}

	private static int GetMouseButtonIndex(int which)
	{
		if (which == 2)
		{
			return which;
		}
		if (GUIActionKeySelector.commandSingleItem.Held)
		{
			return which - 1;
		}
		return which;
	}

	private static bool GetMouseButtonDown(int which)
	{
		int mouseButtonIndex = CrewSim.GetMouseButtonIndex(which);
		return mouseButtonIndex >= 0 && Input.GetMouseButtonDown(mouseButtonIndex);
	}

	public static bool GetMouseButtonUp(int which)
	{
		int mouseButtonIndex = CrewSim.GetMouseButtonIndex(which);
		if (CrewSim.bPoolShipUpdates && which == 0)
		{
			bool flag = mouseButtonIndex >= 0 && Input.GetMouseButtonUp(mouseButtonIndex);
		}
		return mouseButtonIndex >= 0 && Input.GetMouseButtonUp(mouseButtonIndex);
	}

	private static bool GetMouseButton(int which)
	{
		int mouseButtonIndex = CrewSim.GetMouseButtonIndex(which);
		return mouseButtonIndex >= 0 && Input.GetMouseButton(mouseButtonIndex);
	}

	private static bool IsMouseOverGameWindow()
	{
		float x = Input.mousePosition.x;
		float y = Input.mousePosition.y;
		return (0f <= x && 0f <= y) || (x < (float)Screen.width && y < (float)Screen.height);
	}

	private bool InGroundRange()
	{
		return Mathf.Abs(Mathf.RoundToInt(CrewSim.inventoryGUI.CODoll.tfVector2Position.x) - Mathf.RoundToInt(this.vMouse.x)) <= 2 && Mathf.Abs(Mathf.RoundToInt(CrewSim.inventoryGUI.CODoll.tfVector2Position.y) - Mathf.RoundToInt(this.vMouse.y)) <= 2;
	}

	private void MouseHandler()
	{
		List<CondOwner> list = this.FindCOsAtMousePosition(null, false);
		bool flag = false;
		CondOwner selected = GUIMegaToolTip.Selected;
		if (selected != null && list.Remove(selected))
		{
			list.Insert(0, selected);
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (CrewSim.bRaiseUI || CanvasManager.IsCanvasQuitShowing())
			{
				break;
			}
			if (list[i].IsHumanOrRobot)
			{
				this.tooltip.SetTooltipCrew(list[i], GUITooltip.TooltipWindow.Crew);
				flag = true;
				break;
			}
			if (this.workManager.COIDHasTasks(list[i].strID))
			{
				this.tooltip.SetTooltipMulti(list, GUITooltip.TooltipWindow.Task);
				flag = true;
				break;
			}
		}
		if (!flag && this.tooltip.window != GUITooltip.TooltipWindow.QAB && this.tooltip.window != GUITooltip.TooltipWindow.MTT)
		{
			this.tooltip.SetTooltip(null, GUITooltip.TooltipWindow.Hide);
		}
		this.vLastMouse = Input.mousePosition;
		bool flag2 = (CrewSim.bRaiseUI && CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPGUI) || GUIQuickBar.IsBeingDragged || Info.focused;
		bool flag3 = this.goSelPart != null || this.goPaintJob != null;
		if (this.goSelPart != null)
		{
			bool flag4 = true;
			if (GUIInventory.instance.Selected != null && GUIInventory.instance.bLastMouseInInv)
			{
				flag4 = false;
			}
			if (flag4)
			{
				this.goSelPart.SetActive(true);
				flag3 = true;
				TileUtils.goSelPartTiles.SetActive(true);
				CrewSim.cgRotate.alpha = 1f;
			}
			else
			{
				this.goSelPart.SetActive(false);
				flag3 = false;
				TileUtils.goSelPartTiles.SetActive(false);
				CrewSim.cgRotate.alpha = 0f;
			}
		}
		if (flag3)
		{
			Canvas component = CrewSim.CanvasManager.goCanvasGUI.GetComponent<Canvas>();
			RectTransform rectTransform = component.transform as RectTransform;
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(component.transform as RectTransform, Input.mousePosition, component.worldCamera, out vector);
			if (this.goSelPart != null)
			{
				Item component2 = this.goSelPart.GetComponent<Item>();
				if (component2 != null)
				{
					CrewSim.rectRotate.localPosition = vector + new Vector2(0f, (float)(component2.nHeightInTiles * 16 * 2));
				}
				else
				{
					CrewSim.rectRotate.localPosition = vector + new Vector2(0f, 0f);
				}
				if (CrewSim.bShipEdit)
				{
					global::Visibility[] componentsInChildren = this.goSelPart.GetComponentsInChildren<global::Visibility>();
					if (componentsInChildren != null)
					{
						foreach (global::Visibility visibility in componentsInChildren)
						{
							visibility.bRedraw = true;
						}
					}
				}
			}
			if (this.goPaintJob != null)
			{
				this.goPaintJob.transform.localPosition = vector;
			}
		}
		if (!flag2)
		{
			if (Input.mouseScrollDelta.y != 0f && !EventSystem.current.IsPointerOverGameObject() && CrewSim.IsMouseOverGameWindow())
			{
				this.vPanVelocity.z = this.vPanVelocity.z - 0.2f * Input.mouseScrollDelta.y;
			}
			if (CrewSim.GetMouseButtonDown(0))
			{
				this.vDragStart = this.camMain.ScreenToWorldPoint(Input.mousePosition);
				this.vDragStartScreen = Input.mousePosition;
				if (!flag3 && GUIInventory.instance.IsOpen && !EventSystem.current.IsPointerOverGameObject() && !GUIQuickBar.IsBeingDragged && !Info.focused && this.InGroundRange() && !GUIActionKeySelector.commandForceWalk.Held)
				{
					List<CondOwner> mouseOverCO = this.GetMouseOverCO(this._layerMaskDefault, GUIInventory.CTGroundItem, null);
					foreach (CondOwner condOwner in mouseOverCO)
					{
						if (CrewSim.ShouldCondOwnerHighlight(condOwner, false))
						{
							GUIInventoryItem guiinventoryItem = GUIInventoryItem.SpawnInventoryItem(condOwner.strID, null);
							if (guiinventoryItem != null)
							{
								if (GUIActionKeySelector.commandQuickMove.Held)
								{
									guiinventoryItem.OnShiftPointerDown();
									break;
								}
								guiinventoryItem.AttachToCursor(null);
								condOwner.RemoveFromCurrentHome(false);
								condOwner.Visible = false;
								GUIInventory.instance.JustClickedItem = true;
								break;
							}
						}
					}
				}
			}
			if (CrewSim.GetMouseButtonDown(2))
			{
				this.vDragStartScreen = Input.mousePosition;
			}
			else if (!CrewSim.GetMouseButtonDown(1))
			{
				if (CrewSim.GetMouseButton(0))
				{
					if (!EventSystem.current.IsPointerOverGameObject() && EventSystem.current.currentSelectedGameObject == null && !GUIQuickBar.IsBeingDragged && !Info.focused && !GUIActionKeySelector.commandForceWalk.Held)
					{
						if (CrewSim.bShipEdit && GUIActionKeySelector.commandEyedropper.Held)
						{
							List<CondOwner> mouseOverCO2 = this.GetMouseOverCO(this._layerMaskDefault, null, null);
							if (mouseOverCO2.Count > 0)
							{
								this.SetPartCursor(mouseOverCO2[0].strCODef);
							}
							else
							{
								this.SetPartCursor(null);
							}
						}
						else if (flag3 && GUIInventory.instance.Selected == null && CrewSim.chkFill.isOn)
						{
							if (CrewSim.chkFill.isOn)
							{
								this.FloodFill();
							}
						}
						else if (!CrewSim.inventoryGUI.IsOpen && (CrewSim.iaItmInstall == null || CrewSim.iaItmInstall.objThem.strPersistentCO == null))
						{
							Vector3 vector2 = this.vMouse - this.vDragStart;
							if ((Mathf.Abs(vector2.y) > 1f || Mathf.Abs(vector2.x) > 1f) && this.lineSelectRect == null)
							{
								this.lineSelectRect = new VectorLine("SelectionRect", new List<Vector2>(5), 1.5f, LineType.Continuous, Joins.Weld);
								this.lineSelectRect.color = Color.white;
								this.lineSelectRect.SetCanvas(CrewSim.CanvasManager.goCanvasGUI, false);
							}
						}
					}
				}
				else if (CrewSim.GetMouseButtonUp(0))
				{
					CrewSim.bPoolShipUpdates = false;
					GameObject gameObject = null;
					if (GUIActionKeySelector.commandForceWalk.Held)
					{
						this.Walk();
						CrewSim.bJustClickedInput = true;
					}
					else if (this.contextMenuPool.IsRaised)
					{
						this.LowerContextMenu();
					}
					else if (this.lineSelectRect != null)
					{
						VectorLine.Destroy(ref this.lineSelectRect);
						this.SetBracketTarget(null, false, false);
						Bounds viewportBounds = this.GetViewportBounds(this.camMain, this.vDragStart, this.camMain.ScreenToWorldPoint(Input.mousePosition));
						if (TileUtils.bShowTiles || (CrewSim.bShipEdit && this.goSelPart == null))
						{
							this.SelectBounds(viewportBounds, TileUtils.bShowTiles);
						}
						else if (flag3)
						{
							this.PaintBounds(viewportBounds);
						}
					}
					else if (!EventSystem.current.IsPointerOverGameObject())
					{
						if (TileUtils.bShowTiles)
						{
							List<CondOwner> mouseOverCO3 = this.GetMouseOverCO(new string[]
							{
								"Tile Helpers"
							}, this.ctSelectFilter, null);
							gameObject = null;
							foreach (CondOwner condOwner2 in mouseOverCO3)
							{
								if (gameObject != null)
								{
									if (condOwner2.ship == CrewSim.shipCurrentLoaded)
									{
										gameObject = condOwner2.gameObject;
									}
								}
								else
								{
									gameObject = condOwner2.gameObject;
								}
							}
							if (gameObject != null)
							{
								CondOwner component3 = gameObject.GetComponent<CondOwner>();
								Tile component4 = gameObject.GetComponent<Tile>();
								this.SetBracketTarget(component3.strID, false, false);
								this.SelectCO(component3, false);
								int value = CrewSim.shipCurrentLoaded.aTiles.IndexOf(component4);
								if (!GUIActionKeySelector.commandZoneAlternate.Held)
								{
									foreach (JsonZone jsonZone in CrewSim.shipCurrentLoaded.mapZones.Values)
									{
										if (jsonZone.aTiles.Contains(value))
										{
											foreach (int index in jsonZone.aTiles)
											{
												this.SelectCO(CrewSim.shipCurrentLoaded.aTiles[index].coProps, false);
											}
											break;
										}
									}
								}
							}
							else
							{
								this.SetBracketTarget(null, false, false);
							}
							CrewSim.OnTileSelectionUpdated.Invoke(CrewSim.aSelected);
						}
						else if (flag3)
						{
							this.PaintPos(this.ActiveCam.ScreenPointToRay(Input.mousePosition).origin);
						}
						else if (!CrewSim.bJustClickedInput)
						{
							if (GUIInventory.instance.Selected == null && !GUIInventory.instance.JustClickedItem)
							{
								if (this.coConnectMode != null)
								{
									gameObject = this.ClickSelectScenePart(new string[]
									{
										"Tile Helpers"
									});
								}
								else if (!CrewSim.bShipEditBG)
								{
									CondTrigger condTrigger = this.ctSelectFilter;
									if (CrewSim.bShipEdit || CrewSim.bDebugShow)
									{
										this.ctSelectFilter = null;
									}
									else
									{
										this.ctSelectFilter = DataHandler.GetCondTrigger("TCanBeSelected");
									}
									List<CondOwner> list2 = this.GetMouseOverCO(this._layerMaskDefault, null, null).ToList<CondOwner>();
									Room room = null;
									if (CrewSim.shipCurrentLoaded != null)
									{
										room = CrewSim.shipCurrentLoaded.GetRoomAtWorldCoords1(this.vMouse, true);
									}
									if (room != null)
									{
										list2.Remove(room.CO);
										list2.Add(room.CO);
									}
									if (!CrewSim.bShipEdit && list2.Count > 0 && list2.IndexOf(GUIMegaToolTip.Selected) >= 0)
									{
										list2.Clear();
										CrewSim.OnRightClick.Invoke(list2);
									}
									gameObject = this.ClickSelectScenePart(new string[]
									{
										"Default"
									});
									if (gameObject == null)
									{
										if (CrewSim.bShipEdit)
										{
											this.SetBracketTarget(null, false, false);
										}
										else if (list2.Count > 0)
										{
											this.Walk();
										}
									}
									this.ctSelectFilter = condTrigger;
								}
								if (this.coConnectMode != null)
								{
									CondOwner condOwner3 = null;
									if (gameObject != null)
									{
										condOwner3 = gameObject.GetComponent<CondOwner>();
										if (!this.ctSelectFilter.Triggered(condOwner3, null, true))
										{
											condOwner3 = null;
										}
									}
									this.igdConnectMode.SetInput(condOwner3);
									if (this.coConnectLastCrew != null)
									{
										this.SetBracketTarget(this.coConnectLastCrew.strID, false, true);
										this.coConnectLastCrew = null;
										this.coConnectMode = null;
									}
									else
									{
										this.SetBracketTarget(null, false, false);
									}
									this.HideInputSelector();
									GUIModal.Instance.Hide();
								}
								else if (gameObject != null)
								{
									CondOwner component5 = gameObject.GetComponent<CondOwner>();
									if (component5.strCODef.IndexOf("Closed") >= 0 || component5.strCODef.IndexOf("Open") >= 0)
									{
										Ship ship = component5.ship;
										string text = component5.strCODef;
										if (text.IndexOf("Open") >= 0)
										{
											text = text.Replace("Open", "Closed");
										}
										else
										{
											text = text.Replace("Closed", "Open");
										}
										CondOwner condOwner4 = DataHandler.GetCondOwner(text, component5.strID);
										if (condOwner4 != null)
										{
											component5.ModeSwitch(condOwner4, component5.tf.position);
										}
									}
								}
								else if (CrewSim.CanvasManager.State == CanvasManager.GUIState.SOCIAL && GUISocialCombat2.coUs == CrewSim.GetSelectedCrew() && GUISocialCombat2.coUs != GUISocialCombat2.coThem)
								{
									if (GUISocialCombat2.coUs.bAlive)
									{
										Interaction interaction = DataHandler.GetInteraction("SOCSnub", null, false);
										interaction.objUs = GUISocialCombat2.coUs;
										interaction.objThem = GUISocialCombat2.coThem;
										interaction.bManual = true;
										GUISocialCombat2.coUs.AIIssueOrder(interaction.objThem, interaction, true, null, 0f, 0f);
										CrewSim.Paused = false;
									}
									else
									{
										GUISocialCombat2.objInstance.EndSocialCombat();
									}
								}
							}
						}
					}
					else if (GUIInventory.instance.Selected != null && !GUIInventory.instance.JustClickedItem && !GUIInventory.instance.bLastMouseInInv)
					{
						this.Walk();
					}
				}
				else if (CrewSim.bShipEdit && CrewSim.GetMouseButtonDown(1))
				{
					if (this.goSelPart != null)
					{
						this.SetPartCursor(null);
						return;
					}
					if (TileUtils.bShowTiles)
					{
						this.SetBracketTarget(null, false, false);
						return;
					}
					if (CrewSim.bShipEditBG)
					{
						List<Item> mouseOverBG = this.GetMouseOverBG(new string[]
						{
							"Default"
						});
						if (mouseOverBG != null && mouseOverBG.Count > 0)
						{
							CrewSim.shipCurrentLoaded.BGItemRemove(mouseOverBG[0]);
						}
					}
					else
					{
						this.nLastClickIndex = 0;
						this.vLastClick = default(Vector2);
						CondTrigger condTrigger2 = this.ctSelectFilter;
						this.ctSelectFilter = new CondTrigger();
						List<string> list3 = new List<string>();
						if (condTrigger2 != null)
						{
							list3.AddRange(condTrigger2.aForbids);
							Array.Copy(condTrigger2.aReqs, this.ctSelectFilter.aReqs, condTrigger2.aReqs.Length);
						}
						list3.Add("IsRoom");
						this.ctSelectFilter.aForbids = list3.ToArray();
						GameObject gameObject2 = this.ClickSelectScenePart(new string[]
						{
							"Default"
						});
						if (gameObject2 != null)
						{
							CrewSim.shipCurrentLoaded.RemoveCO(gameObject2.GetComponent<CondOwner>(), false);
							UnityEngine.Object.Destroy(gameObject2);
						}
						this.ctSelectFilter = condTrigger2;
					}
				}
				else if (CrewSim.GetMouseButtonUp(1))
				{
					if ((this.goSelPart != null && CrewSim.inventoryGUI.Selected == null) || this.goPaintJob != null || CrewSim.guiPDA.JobsActive)
					{
						CrewSim.guiPDA.HideJobPaintUI();
						return;
					}
					if (!CrewSim.bJustClickedInput)
					{
						if (CrewSim.ZoneMenuOpen)
						{
							return;
						}
						if (CrewSim.objInstance.coConnectMode != null)
						{
							this.CloseConnectionMode();
						}
						else if ((this.contextMenuPool.IsRaised && !this.bRaisedMenuThisFrame) || (double)this.RightMouseButtonDownTimer > 0.3)
						{
							this.RightMouseButtonDownTimer = 0f;
							this.LowerContextMenu();
						}
						else if (EventSystem.current.IsPointerOverGameObject())
						{
							if (CanvasManager.IsOverUIElement(CrewSim.goCrewBar) && CanvasManager.IsOverUIElement(CrewSim.goCrewBarPortraitButton))
							{
								CrewSim.OnRightClick.Invoke(new List<CondOwner>
								{
									CrewSim.GetSelectedCrew()
								});
							}
						}
						else if (!CrewSim.bRaiseUI && !CrewSim.inventoryGUI.ClickedInventory(Input.mousePosition))
						{
							this.RightMouseButtonDownTimer = 0f;
							if (CrewSim.bShipEdit || CrewSim.bDebugShow)
							{
								this.ctSelectFilter = null;
							}
							else
							{
								this.ctSelectFilter = DataHandler.GetCondTrigger("TCanBeSelectedMTT");
							}
							List<CondOwner> mouseOverCO4 = this.GetMouseOverCO(this._layerMaskDefaultLos, this.ctSelectFilter, null);
							Room room2 = null;
							if (CrewSim.shipCurrentLoaded != null)
							{
								room2 = CrewSim.shipCurrentLoaded.GetRoomAtWorldCoords1(this.vMouse, true);
							}
							if (room2 != null)
							{
								mouseOverCO4.Remove(room2.CO);
								mouseOverCO4.Add(room2.CO);
							}
							if (mouseOverCO4 != null && mouseOverCO4.Count > 0)
							{
								CrewSim.OnRightClick.Invoke(mouseOverCO4);
							}
							this.ctSelectFilter = null;
						}
					}
				}
				else if (CrewSim.GetMouseButton(2))
				{
					float num = Input.mousePosition.x - this.vDragStartScreen.x;
					float num2 = Input.mousePosition.y - this.vDragStartScreen.y;
					float num3 = 1f;
					if (this.camMain != null)
					{
						num3 = this.camMain.aspect;
					}
					this.delX += num / 15f * num3;
					this.delY += num2 / 15f;
				}
				else if (!flag3 && GUIInventory.instance.IsOpen && !EventSystem.current.IsPointerOverGameObject() && !GUIQuickBar.IsBeingDragged && !Info.focused)
				{
					bool flag5 = this.InGroundRange();
					bool flag6 = false;
					List<CondOwner> mouseOverCO5 = this.GetMouseOverCO(this._layerMaskDefault, GUIInventory.CTGroundItem, null);
					foreach (CondOwner condOwner5 in mouseOverCO5)
					{
						if (CrewSim.ShouldCondOwnerHighlight(condOwner5, true))
						{
							condOwner5.Highlight = true;
							this.currentHighlight.Add(condOwner5);
							flag6 = CrewSim.ShouldCondOwnerHighlight(condOwner5, false);
						}
					}
					if (GUIActionKeySelector.commandForceWalk.Held)
					{
						this.SetCursor(1);
					}
					else if (flag6 && flag5)
					{
						this.SetCursor(2);
					}
					else
					{
						this.SetCursor(1);
					}
				}
				else
				{
					this.SetCursor(0);
				}
			}
			if (this.goSelPart != null)
			{
				Item component6 = this.goSelPart.GetComponent<Item>();
				if (component6 != null)
				{
					component6.SetToMousePosition(this.vMouse);
					if (component6.jid.strName != "Cancel")
					{
						component6.CheckFit(component6.rend.bounds.center, CrewSim.shipCurrentLoaded, TileUtils.aSelPartTiles, null);
					}
				}
				CondOwner component7 = this.goSelPart.GetComponent<CondOwner>();
				if (component7 == null)
				{
					return;
				}
				Powered component8 = this.goSelPart.GetComponent<Powered>();
				if (component8 != null && !component7.HasCond("IsPowerInputIgnore") && component8.jsonPI.aInputPts != null && component8.jsonPI.aInputPts.Length > 0)
				{
					for (int l = 0; l < component8.jsonPI.aInputPts.Length; l++)
					{
						Vector3 position = component7.GetPos(component8.jsonPI.aInputPts[l], false);
						position.z = -8f;
						TileUtils.GetPowerInputGridSprite(l).transform.position = position;
						TileUtils.GetPowerInputGridSprite(l).SetActive(true);
					}
				}
				Vector3 position2 = Vector3.zero;
				if (component7.mapPoints != null && component7.mapPoints.ContainsKey("PowerOutput"))
				{
					position2 = component7.GetPos("PowerOutput", false);
					position2.z = -8f;
					TileUtils.GetPowerOutputGridSprite().transform.position = position2;
					TileUtils.GetPowerOutputGridSprite().SetActive(true);
				}
				position2 = Vector3.zero;
				if (component7.mapPoints.ContainsKey("use"))
				{
					position2 = component7.mapPoints["use"];
					if (position2.x != 0f || position2.y != 0f)
					{
						position2 = component7.GetPos("use", false);
						position2.z = -8f;
						TileUtils.GetUseGridSprite().transform.position = position2;
						TileUtils.GetUseGridSprite().SetActive(true);
					}
				}
				position2 = Vector3.zero;
				if (component7.mapPoints.ContainsKey("ReactorPlug"))
				{
					position2 = component7.GetPos("ReactorPlug", false);
					position2.z = -8f;
					TileUtils.GetReactorGridSprite().transform.position = position2;
					TileUtils.GetReactorGridSprite().SetActive(true);
				}
			}
		}
		CrewSim.bJustClickedInput = false;
	}

	public void CloseConnectionMode()
	{
		if (this.coConnectLastCrew != null)
		{
			this.SetBracketTarget(this.coConnectLastCrew.strID, false, true);
			this.coConnectLastCrew = null;
			this.coConnectMode = null;
		}
		else
		{
			this.SetBracketTarget(null, false, false);
		}
		this.HideInputSelector();
		GUIModal.Instance.Hide();
	}

	public void OnHoldRMB(Action callback)
	{
		if (this.RightMouseButtonDownTimer < this.RightMouseButtonDownMax)
		{
			this.RightMouseButtonDownTimer += Time.deltaTime;
			return;
		}
		if (this.RightMouseButtonDownTimer >= this.RightMouseButtonDownMax && this.RightMouseButtonDownTimer < 5f && !this.contextMenuPool.IsRaised)
		{
			if (this.goSelPart != null)
			{
				this.SetPartCursor(null);
				CrewSim.iaItmInstall = null;
			}
			this.RightMouseButtonDownTimer = 10f;
			if (callback != null)
			{
				callback();
			}
			this.cursorRoundel.ResetFill();
		}
	}

	private void Walk()
	{
		CondOwner bracketTarget = CrewSim.GetBracketTarget();
		if (bracketTarget == null)
		{
			bracketTarget = CrewSim.coPlayer;
		}
		if (bracketTarget != null && bracketTarget.bAlive)
		{
			Ray ray = this.camMain.ScreenPointToRay(Input.mousePosition);
			Tile tileAtWorldCoords = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(ray.origin.x, ray.origin.y, true, true);
			bracketTarget.AIIssueOrder(null, null, true, tileAtWorldCoords, ray.origin.x, ray.origin.y);
			CrewSim.AIManual(true);
			if (CrewSim.Paused)
			{
				CrewSim.fPauseFlashExtra = (double)(Time.realtimeSinceStartup + 3f);
				AudioManager.am.PlayAudioEmitter("UIMessageLogBad", false, true);
			}
		}
	}

	private void MoveViewHandler()
	{
		if (CrewSim.coCamCenter == null)
		{
			CrewSim.coCamCenter = CrewSim.coPlayer;
		}
		float num = 1f;
		if (GUIActionKeySelector.commandPanFaster.Held)
		{
			num *= 3f;
		}
		if (GUIActionKeySelector.commandPanSlower.Held)
		{
			num /= 3f;
		}
		float num2 = CrewSim.TimeElapsedUnscaled();
		float num3 = this.camMain.orthographicSize * num2;
		if (CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPGUI)
		{
			this.delX = 0f;
			this.delY = 0f;
			this.delZ = 0f;
		}
		this.delX = Mathf.Clamp(this.delX, -1f, 1f);
		this.delY = Mathf.Clamp(this.delY, -1f, 1f);
		this.delZ = Mathf.Clamp(this.delZ, -1f, 1f);
		this.vPanVelocity.x = this.vPanVelocity.x + this.delX * num2 * this.camMain.orthographicSize;
		this.vPanVelocity.y = this.vPanVelocity.y + this.delY * num2 * this.camMain.orthographicSize;
		this.vPanVelocity.z = this.vPanVelocity.z + this.delZ * num2;
		if (this.delX != 0f || this.delY != 0f)
		{
			this.camFollow = false;
		}
		float num4 = -3f;
		if (this.delX == 0f && this.delY == 0f && this.delZ == 0f)
		{
			num4 = -15f;
		}
		this.vPanVelocity.x = this.vPanVelocity.x * Mathf.Exp(num4 * num2);
		this.vPanVelocity.y = this.vPanVelocity.y * Mathf.Exp(num4 * num2);
		this.vPanVelocity.z = this.vPanVelocity.z * Mathf.Exp(num4 * num2);
		float num5 = num * this.fCamSpeed * this.vPanVelocity.x * num2;
		float num6 = num * this.fCamSpeed * this.vPanVelocity.y * num2;
		float magnitude = this.camTravel.magnitude;
		if (magnitude > 1E-06f)
		{
			if (magnitude > 4f * this.camTravelVelocity)
			{
				this.camTravelVelocity = Mathf.Min(this.camTravelVelocity + num3, 10f);
			}
			else
			{
				this.camTravelVelocity = Mathf.Max(this.camTravelVelocity - num3, 0.1f);
			}
			float num7 = Mathf.Min(this.camTravelVelocity * num3, magnitude) / magnitude;
			float num8 = this.camTravel.x * num7;
			float num9 = this.camTravel.y * num7;
			this.camTravel.x = this.camTravel.x - num8;
			this.camTravel.y = this.camTravel.y - num9;
		}
		else
		{
			this.camTravelVelocity = 0f;
		}
		Vector3 vector = Vector3.zero;
		if (!CrewSim.bShipEdit && CrewSim.coCamCenter != null)
		{
			vector = CrewSim.coCamCenter.tf.position;
		}
		if (Vector3.Distance(this.camMain.transform.position, vector) > 0.1f && this.camFollow)
		{
			Vector3 vector2 = this.camMain.transform.position + (vector - this.camMain.transform.position) * 0.05f;
			num5 += vector2.x - this.camMain.transform.position.x;
			num6 += vector2.y - this.camMain.transform.position.y;
		}
		num5 += this.vShake.x * this.fShakeAmp;
		num6 += this.vShake.y * this.fShakeAmp;
		if (num5 != 0f || num6 != 0f)
		{
			this.camMain.transform.Translate(num5, num6, 0f);
		}
		this.CamZoom(Mathf.Exp(this.vPanVelocity.z * 5f * num2));
		this.delY = 0f;
		this.delX = 0f;
		this.delZ = 0f;
	}

	public void CamShake(float fAmount)
	{
		if (fAmount > this.fShakeAmp)
		{
			this.fShakeAmp = Mathf.Min(fAmount, 1f);
		}
	}

	private IEnumerator turnPlayer()
	{
		for (;;)
		{
			CrewSim.coPlayer.tf.Rotate(new Vector3(0f, 0f, 1f));
			yield return null;
		}
		yield break;
	}

	private bool DebugCodeHandler()
	{
		if (CrewSim.bEnableDebugCommands)
		{
			return true;
		}
		if (!Input.anyKeyDown)
		{
			return false;
		}
		if (!Input.GetKeyDown((KeyCode)Enum.Parse(typeof(KeyCode), CrewSim.sDebugCode[CrewSim.nDebugIndex].ToString())))
		{
			CrewSim.nDebugIndex = 0;
			return false;
		}
		if (++CrewSim.nDebugIndex == CrewSim.sDebugCode.Length)
		{
			this.UnlockDebug();
		}
		return false;
	}

	public static void ToggleDebug()
	{
		if (CrewSim.bEnableDebugCommands)
		{
			if (CrewSim.objInstance != null)
			{
				CrewSim.objInstance.ExitDebug();
			}
			else
			{
				CrewSim.bEnableDebugCommands = false;
			}
		}
		else if (CrewSim.objInstance != null)
		{
			CrewSim.objInstance.UnlockDebug();
		}
		else
		{
			CrewSim.bEnableDebugCommands = true;
		}
	}

	public void ExitDebug()
	{
		Debug.Log("Debug Command exited");
		CrewSim.coPlayer.LogMessage("**Debug Commands Have Been Disabled**", "Neutral", "Game");
		TMP_Text component = CrewSim.CanvasManager.goCanvasGUI.transform.Find("txtVersion").GetComponent<TMP_Text>();
		if (IntPtr.Size == 8)
		{
			component.text = DataHandler.strBuild + " (64)";
		}
		else
		{
			component.text = DataHandler.strBuild + " (32)";
		}
		CrewSim.bEnableDebugCommands = false;
		CrewSim.nDebugIndex = 0;
	}

	public void UnlockDebug()
	{
		Debug.Log("Debug Command enabled");
		CrewSim.coPlayer.LogMessage("**Debug Commands Have Been Activated**", "Neutral", "Game");
		TMP_Text component = CrewSim.CanvasManager.goCanvasGUI.transform.Find("txtVersion").GetComponent<TMP_Text>();
		if (IntPtr.Size == 8)
		{
			component.text = "DB" + DataHandler.strBuild + " (64)";
		}
		else
		{
			component.text = "DB" + DataHandler.strBuild + " (32)";
		}
		CrewSim.bEnableDebugCommands = true;
		CrewSim.nDebugIndex = 0;
	}

	private void FloodFill()
	{
		if (CrewSim.bShipEdit && this.goSelPart)
		{
			Item component = this.goSelPart.GetComponent<Item>();
			float fLastRotation = component.fLastRotation;
			CondTrigger condTrigger = new CondTrigger();
			condTrigger.aForbids = new string[]
			{
				"IsWall",
				"IsFloor"
			};
			Tile tileAtWorldCoords = CrewSim.shipCurrentLoaded.GetTileAtWorldCoords1(this.vMouse.x, this.vMouse.y, false, true);
			List<Tile> floodTiles = TileUtils.GetFloodTiles(tileAtWorldCoords, 100, condTrigger);
			foreach (Tile tile in floodTiles)
			{
				Vector3 position = tile.tf.position;
				position.z = component.transform.position.z;
				if (CrewSim.bShipEditBG)
				{
					Item background = DataHandler.GetBackground(this.goSelPart.name);
					background.transform.position = position;
					background.fLastRotation = fLastRotation;
					CrewSim.shipCurrentLoaded.BGItemAdd(background);
				}
				else
				{
					CondOwner component2 = this.goSelPart.GetComponent<CondOwner>();
					CondOwner condOwner = DataHandler.GetCondOwner(component2.strCODef);
					condOwner.transform.position = position;
					condOwner.GetComponent<Item>().fLastRotation = fLastRotation;
					CrewSim.shipCurrentLoaded.AddCO(condOwner, true);
				}
			}
		}
	}

	private void KeyHandler()
	{
		bool flag = (CrewSim.bRaiseUI && CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPGUI) || CrewSim.bTyping;
		if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Space))
		{
			Ledger.GetCRO(CrewSim.CanvasManager.goCanvasGUI);
		}
		if (flag)
		{
			return;
		}
		if (CanvasManager.instance.State == CanvasManager.GUIState.GAMEOVER)
		{
			return;
		}
		if (CrewSim.bRaiseUI)
		{
			return;
		}
		if (CrewSim.bShipEdit && Input.GetKeyDown(KeyCode.Delete))
		{
			CondOwner[] array = new CondOwner[CrewSim.aSelected.Count];
			CrewSim.aSelected.CopyTo(array);
			foreach (CondOwner condOwner in array)
			{
				if (!(condOwner == null))
				{
					CrewSim.shipCurrentLoaded.RemoveCO(condOwner, false);
					if (DataHandler.mapCOs.ContainsKey(condOwner.strID))
					{
						DataHandler.mapCOs.Remove(condOwner.strID);
					}
					UnityEngine.Object.Destroy(condOwner.gameObject);
				}
			}
			this.SetBracketTarget(null, false, false);
			this.SetPartCursor(null);
		}
		if (!this.DebugCodeHandler())
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Alpha1) && Input.GetKey(KeyCode.LeftShift))
		{
			CrewSim.coPlayer.AddCondAmount(GUIFinance.strCondCurr, 50000.0, 0.0, 0f);
			CrewSim.SetCashButton(CrewSim.coPlayer.GetCondAmount("StatUSD"));
		}
		if (Input.GetKeyDown(KeyCode.Alpha2) && Input.GetKey(KeyCode.LeftShift))
		{
			CondOwner condOwner2 = DataHandler.GetCondOwner("SysExplosionFusion");
			condOwner2.tf.position = this.ActiveCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, condOwner2.tf.position.z));
			CrewSim.shipCurrentLoaded.AddCO(condOwner2, false);
		}
		if (Input.GetKeyDown(KeyCode.Alpha3) && Input.GetKey(KeyCode.LeftShift))
		{
			AIShipManager.CheckLocalAuthorityScenario();
		}
		if (Input.GetKeyDown(KeyCode.Alpha4) && Input.GetKey(KeyCode.LeftShift))
		{
			Debug.Log(CrewSim.shipCurrentLoaded.strRegID + " Value = " + CrewSim.shipCurrentLoaded.GetShipValue());
			foreach (Room room in CrewSim.shipCurrentLoaded.aRooms)
			{
				Debug.Log(string.Concat(new object[]
				{
					"  ",
					room.GetRoomSpec().ToString(),
					" Value = ",
					room.RoomValue
				}));
			}
		}
		if (Input.GetKeyDown(KeyCode.Alpha5) && Input.GetKey(KeyCode.LeftShift))
		{
			PlotManager.bDebugCheckAll = true;
			PlotManager.CheckPlots(CrewSim.GetSelectedCrew(), (PlotManager.PlotTensionType)3);
		}
		if (Input.GetKeyDown(KeyCode.Alpha6) && Input.GetKey(KeyCode.LeftShift))
		{
			AIShipManager.SpawnAI(AIType.Pirate, null);
		}
		if (Input.GetKeyDown(KeyCode.Alpha7) && Input.GetKey(KeyCode.LeftShift))
		{
			CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsBodyPart");
			foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
			{
				foreach (CondOwner condOwner3 in ship.GetPeople(false))
				{
					if (condOwner3.GetCOs(true, DataHandler.GetCondTrigger("TIsBodyPart")).Count == 0)
					{
						Debug.Log("Found limbless " + condOwner3.strName + " on " + ship.ToString());
					}
				}
				foreach (CondOwner condOwner4 in ship.GetCOs(condTrigger, true, false, true))
				{
					if (condOwner4.slotNow == null)
					{
						Debug.Log("Found loose limb " + condOwner4.strName + " on " + ship.ToString());
					}
				}
			}
		}
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.J))
		{
			Debug.Log("Verifying Json references:");
			DataHandler.ScanDictionaries();
		}
	}

	private void DebugFloorUseAudit()
	{
		List<string> list = new List<string>
		{
			"_StorageAll",
			"0-12 Floor batch 1",
			"0-14_HullBatch",
			"00_Chargen",
			"3Pilots",
			"Aerostat Scaffold Dock 02",
			"Aerostat Scaffold Dock",
			"Aerostat Scaffold X",
			"Aerostat Scaffold",
			"AI Training",
			"AllTiles",
			"AllTilesUnused",
			"break zone",
			"CargoField",
			"Combat Room Alpha",
			"CrateRoom",
			"Doors",
			"Kiosk Room",
			"Light Container Testing",
			"Normals",
			"OKLG",
			"PAX2020Salvage",
			"PAX2020Social",
			"PAX2020Start",
			"Reactor",
			"ReactorComponents",
			"Repairshop",
			"Small",
			"Station",
			"StationChargen",
			"TorchPartsTest",
			"Waypoint",
			"_9x9 Air Test",
			"_Atmo Breathing Test",
			"_box",
			"_Chromastronauts",
			"_lootSpawn",
			"_meatTest",
			"_RobotTestFacility",
			"_test",
			"_Wall Test"
		};
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (JsonCondOwner jsonCondOwner in DataHandler.dictCOs.Values)
		{
			if (jsonCondOwner.strName.IndexOf("ItmFloor") == 0 || jsonCondOwner.strName.IndexOf("ItmWall") == 0)
			{
				if (jsonCondOwner.strName.IndexOf("Patch") < 0 && jsonCondOwner.strName.IndexOf("Loose") < 0 && jsonCondOwner.strName.IndexOf("Dmg") < 0)
				{
					dictionary[jsonCondOwner.strName + "\t" + jsonCondOwner.strNameFriendly] = 0;
				}
			}
		}
		foreach (JsonCOOverlay jsonCOOverlay in DataHandler.dictCOOverlays.Values)
		{
			if (jsonCOOverlay.strName.IndexOf("ItmFloor") == 0 || jsonCOOverlay.strName.IndexOf("ItmWall") == 0)
			{
				if (jsonCOOverlay.strName.IndexOf("Patch") < 0 && jsonCOOverlay.strName.IndexOf("Loose") < 0 && jsonCOOverlay.strName.IndexOf("Dmg") < 0)
				{
					dictionary[jsonCOOverlay.strName + "\t" + jsonCOOverlay.strNameFriendly] = 0;
				}
			}
		}
		Dictionary<string, Dictionary<string, int>> dictionary2 = new Dictionary<string, Dictionary<string, int>>();
		foreach (JsonShip jsonShip in DataHandler.dictShips.Values)
		{
			if (!list.Contains(jsonShip.strName))
			{
				Dictionary<string, int> dictionary3 = new Dictionary<string, int>();
				foreach (JsonItem jsonItem in jsonShip.aItems)
				{
					if (jsonItem.strName.IndexOf("ItmFloor") == 0 || jsonItem.strName.IndexOf("ItmWall") == 0)
					{
						if (jsonItem.strName.IndexOf("Patch") < 0 && jsonItem.strName.IndexOf("Loose") < 0 && jsonItem.strName.IndexOf("Dmg") < 0)
						{
							string text = jsonItem.strName + "\t";
							if (DataHandler.dictCOs.ContainsKey(jsonItem.strName))
							{
								text += DataHandler.dictCOs[jsonItem.strName].strNameFriendly;
							}
							else
							{
								text += DataHandler.dictCOOverlays[jsonItem.strName].strNameFriendly;
							}
							if (!dictionary.ContainsKey(text))
							{
								dictionary[text] = 1;
							}
							else
							{
								Dictionary<string, int> dictionary4;
								string key;
								(dictionary4 = dictionary)[key = text] = dictionary4[key] + 1;
							}
							if (!dictionary3.ContainsKey(text))
							{
								dictionary3[text] = 1;
							}
							else
							{
								Dictionary<string, int> dictionary4;
								string key2;
								(dictionary4 = dictionary3)[key2 = text] = dictionary4[key2] + 1;
							}
						}
					}
				}
				dictionary2[jsonShip.strName] = dictionary3;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Total Hull Use Count:");
		foreach (KeyValuePair<string, int> keyValuePair in dictionary)
		{
			stringBuilder.Append("\t");
			stringBuilder.Append(keyValuePair.Key);
			stringBuilder.Append("\t");
			stringBuilder.AppendLine(keyValuePair.Value.ToString());
		}
		foreach (KeyValuePair<string, Dictionary<string, int>> keyValuePair2 in dictionary2)
		{
			stringBuilder.Append(keyValuePair2.Key);
			stringBuilder.AppendLine(" Hull Use Count:");
			foreach (KeyValuePair<string, int> keyValuePair3 in keyValuePair2.Value)
			{
				stringBuilder.Append("\t");
				stringBuilder.Append(keyValuePair3.Key);
				stringBuilder.Append("\t");
				stringBuilder.AppendLine(keyValuePair3.Value.ToString());
			}
		}
		DataHandler.WriteFile("HullPartAudit.txt", stringBuilder.ToString());
		Debug.Log(stringBuilder.ToString());
	}

	private void DebugReportCauseOfDeath()
	{
		foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
		{
			foreach (CondOwner condOwner in ship.GetPeople(false))
			{
				condOwner.DebugReportCauseOfDeath();
			}
		}
	}

	private void DebugRefreshPlayerStats()
	{
		CrewSim.coPlayer.AddCondAmount("StatHydration", -100.0, 0.0, 0f);
		CrewSim.coPlayer.AddCondAmount("StatFood", -100.0, 0.0, 0f);
		CrewSim.coPlayer.AddCondAmount("StatSatiety", 8.0, 0.0, 0f);
		CrewSim.coPlayer.AddCondAmount("StatDefecate", -100.0, 0.0, 0f);
		CrewSim.coPlayer.AddCondAmount("StatSleep", -100.0, 0.0, 0f);
		CrewSim.coPlayer.AddCondAmount("StatHygiene", -100.0, 0.0, 0f);
	}

	private void DebugWeaponSlotAudit()
	{
		foreach (JsonInteraction jsonInteraction in DataHandler.dictInteractions.Values)
		{
			if (jsonInteraction.aAModesAddedThem != null && jsonInteraction.aAModesAddedThem.Length != 0)
			{
				if (jsonInteraction.strName.IndexOf("SLOT") == 0)
				{
					string text = "UN" + jsonInteraction.strName;
					JsonInteraction jsonInteraction2 = null;
					if (!DataHandler.dictInteractions.TryGetValue(text, out jsonInteraction2))
					{
						Debug.Log("No UNSLOT for " + text);
					}
					else
					{
						for (int i = 0; i < jsonInteraction.aAModesAddedThem.Length; i++)
						{
							if (jsonInteraction2.aAModesAddedThem.Length <= i)
							{
								Debug.Log(string.Concat(new object[]
								{
									text,
									" missing index ",
									i,
									"; Should be -",
									jsonInteraction.aAModesAddedThem[i]
								}));
							}
							else
							{
								string text2 = jsonInteraction2.aAModesAddedThem[i];
								if (string.IsNullOrEmpty(text2) || text2 != "-" + jsonInteraction.aAModesAddedThem[i])
								{
									Debug.Log(string.Concat(new object[]
									{
										text,
										" mismatch index ",
										i,
										"; Should be -",
										jsonInteraction.aAModesAddedThem[i]
									}));
								}
							}
						}
					}
				}
			}
		}
	}

	private void DebugListCOContainers()
	{
		CrewSim.dictCOConts = new Dictionary<string, CondOwner>();
		foreach (JsonCondOwner jsonCondOwner in DataHandler.dictCOs.Values)
		{
			CondOwner condOwner = DataHandler.GetCondOwner(jsonCondOwner.strName);
			if (!(condOwner == null))
			{
				CrewSim.dictCOConts[condOwner.strName] = condOwner;
			}
		}
		foreach (JsonCOOverlay jsonCOOverlay in DataHandler.dictCOOverlays.Values)
		{
			CondOwner condOwner2 = DataHandler.GetCondOwner(jsonCOOverlay.strName);
			if (!(condOwner2 == null))
			{
				CrewSim.dictCOConts[condOwner2.strName] = condOwner2;
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (CondOwner condOwner3 in CrewSim.dictCOConts.Values)
		{
			stringBuilder.Append(condOwner3.strName);
			stringBuilder.Append("\t");
			stringBuilder.Append(condOwner3.FriendlyName);
			stringBuilder.Append("\t");
			if (condOwner3.objContainer != null)
			{
				if (condOwner3.objContainer.ctAllowed != null)
				{
					stringBuilder.Append(condOwner3.objContainer.ctAllowed.strName);
				}
				else
				{
					stringBuilder.Append("null");
				}
			}
			else
			{
				stringBuilder.Append("n/a");
			}
			stringBuilder.Append("\t");
			stringBuilder.AppendLine("ENDCONT");
			if (condOwner3.objContainer != null)
			{
				CondTrigger ctAllowed = null;
				if (condOwner3.objContainer.ctAllowed != null)
				{
					ctAllowed = condOwner3.objContainer.ctAllowed.Clone();
				}
				this.DebugListCOsThatFitInside(stringBuilder, ctAllowed);
			}
		}
		DataHandler.WriteFile("COContainers.txt", stringBuilder.ToString());
	}

	private void DebugListCOsThatFitInside(StringBuilder sb, CondTrigger ctAllowed)
	{
		if (sb == null)
		{
			return;
		}
		foreach (CondOwner condOwner in CrewSim.dictCOConts.Values)
		{
			if (ctAllowed == null || ctAllowed.Triggered(condOwner, null, true))
			{
				sb.Append("\t");
				sb.Append(condOwner.strName);
				if (condOwner.objContainer != null)
				{
					sb.Append("*");
				}
				sb.Append("\t");
				sb.Append(condOwner.FriendlyName);
				sb.Append("\t");
				sb.AppendLine("END");
			}
		}
	}

	public static void ScheduleAutoPause(double fDuration, string strReason = null)
	{
		if (CrewSim.tplAutoPause.Item1 == 0.0 || CrewSim.tplAutoPause.Item1 > StarSystem.fEpoch + fDuration)
		{
			CrewSim.tplAutoPause.Item1 = StarSystem.fEpoch + fDuration;
			CrewSim.tplAutoPause.Item2 = strReason;
		}
	}

	public static void TriggerAutoPause(string strReason = null)
	{
		if (!CrewSim.objInstance.bcombatAutoPauseAllowed || GUISocialCombat2.coUs == CrewSim.GetSelectedCrew())
		{
			return;
		}
		if (strReason != null && CrewSim.GetSelectedCrew() != null)
		{
			CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("AUTOPAUSE_PREFIX", false) + strReason, "Neutral", CrewSim.GetSelectedCrew().strName);
		}
		CrewSim.Paused = true;
		MonoSingleton<GUIQuickBar>.Instance.Refresh(false);
		AudioManager.am.PlayAudioEmitter("UIGameplayPause", false, false);
	}

	public static void ResetAutoPause()
	{
		CrewSim.tplAutoPause.Item1 = 0.0;
		CrewSim.tplAutoPause.Item2 = null;
	}

	public static bool Paused
	{
		get
		{
			return CrewSim.fTimeCoeffPause == 0f;
		}
		set
		{
			if (CrewSim.coPlayer != null)
			{
				CrewSim.coPlayer.ZeroCondAmount("TutorialPauseWaiting");
				MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			}
			if (CrewSim.bPauseLock)
			{
				CrewSim.OnTimeScaleUpdated.Invoke();
				return;
			}
			CrewSim.fTimeCoeffPause = ((!value) ? 1f : 0f);
			Time.timeScale = MathUtils.Clamp(Time.timeScale, 0.25f, 16f);
			CrewSim.OnTimeScaleUpdated.Invoke();
			foreach (Animator animator in CrewSim.COAnimators)
			{
				if (!(animator == null))
				{
					animator.enabled = !value;
				}
			}
			if (Math.Abs(CrewSim.tplAutoPause.Item1 - StarSystem.fEpoch) <= 0.75)
			{
				CrewSim.tplAutoPause.Item1 = 0.0;
				CrewSim.tplAutoPause.Item2 = null;
			}
		}
	}

	private void GenerateAITrainingJson()
	{
		CondTrigger objCondTrig = new CondTrigger("Humans", new string[]
		{
			"IsHuman"
		}, null, null, null);
		List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(objCondTrig, false, true, false);
		Dictionary<string, CondHistory> dictionary = new Dictionary<string, CondHistory>();
		foreach (CondOwner condOwner in cos)
		{
			foreach (KeyValuePair<string, CondHistory> keyValuePair in condOwner.mapIAHist)
			{
				string key = keyValuePair.Key;
				CondHistory value = keyValuePair.Value;
				if (!dictionary.ContainsKey(key))
				{
					dictionary[key] = new CondHistory(value.strCondName);
				}
				CondHistory condHistory = dictionary[key];
				foreach (InteractionHistory interactionHistory in value.mapInteractions.Values)
				{
					for (int i = interactionHistory.nIterations; i > 0; i--)
					{
						condHistory.AddInteractionScore(interactionHistory.strName, interactionHistory.fAverage, true);
					}
				}
			}
		}
		Dictionary<string, JsonAIPersonality> dictionary2 = new Dictionary<string, JsonAIPersonality>();
		dictionary2["Darlene"] = new JsonAIPersonality
		{
			strName = "Darlene",
			mapIAHist2 = dictionary
		};
		DataHandler.DataToJsonStreaming<JsonAIPersonality>(dictionary2, "ai_training3.json", false, string.Empty);
	}

	public void SetResolution(int width, int height)
	{
	}

	public float AspectRatioMod()
	{
		return this.camMain.aspect / 1.7777778f;
	}

	public static void AddLoadedShip(Ship ship)
	{
		if (ship == null || CrewSim.aLoadedShips.Contains(ship))
		{
			return;
		}
		CrewSim.aLoadedShips.Add(ship);
	}

	public static void RemoveLoadedShip(Ship ship)
	{
		if (ship == null)
		{
			return;
		}
		CrewSim.aLoadedShips.Remove(ship);
	}

	public static Ship GetLoadedShipByRegId(string regId)
	{
		if (string.IsNullOrEmpty(regId) || CrewSim.aLoadedShips == null)
		{
			return null;
		}
		return CrewSim.aLoadedShips.FirstOrDefault((Ship ship) => ship.strRegID == regId);
	}

	public static void UpdateEyedropperKey(string strKey)
	{
		if (CrewSim.txtEyedropper == null)
		{
			return;
		}
		CrewSim.txtEyedropper.text = DataHandler.GetString("GUI_SHIPEDIT_EYEDROPPER1", false) + strKey + DataHandler.GetString("GUI_SHIPEDIT_EYEDROPPER2", false);
	}

	public static bool Typing
	{
		get
		{
			return CrewSim.bTyping;
		}
		set
		{
			CrewSim.bTyping = value;
		}
	}

	public static Ship shipCurrentLoaded
	{
		get
		{
			if (CrewSim.GetSelectedCrew() != null && CrewSim.GetSelectedCrew().ship != null)
			{
				return CrewSim.GetSelectedCrew().ship;
			}
			if (CrewSim.aLoadedShips.Count > 0)
			{
				return CrewSim.aLoadedShips[0];
			}
			return null;
		}
	}

	public static List<Ship> GetAllLoadedShips()
	{
		return CrewSim.aLoadedShips;
	}

	private void DebugAudioAudit()
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		DirectoryInfo directoryInfo = new DirectoryInfo("Assets/Resources/Audio");
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			if (!(fileInfo.Extension != ".ogg") || !(fileInfo.Extension != ".wav"))
			{
				dictionary[fileInfo.Name.Substring(0, fileInfo.Name.Length - 4)] = new List<string>();
			}
		}
		foreach (KeyValuePair<string, JsonAudioEmitter> keyValuePair in DataHandler.dictAudioEmitters)
		{
			if (keyValuePair.Value.strClipSteady != null && dictionary.ContainsKey(keyValuePair.Value.strClipSteady))
			{
				dictionary[keyValuePair.Value.strClipSteady].Add(keyValuePair.Key + ".strClipSteady");
			}
			if (keyValuePair.Value.strClipTrans != null && dictionary.ContainsKey(keyValuePair.Value.strClipTrans))
			{
				dictionary[keyValuePair.Value.strClipTrans].Add(keyValuePair.Key + ".strClipTrans");
			}
		}
		string text = string.Empty;
		foreach (KeyValuePair<string, List<string>> keyValuePair2 in dictionary)
		{
			text = text + keyValuePair2.Key + "\t";
			foreach (string str in keyValuePair2.Value)
			{
				text = text + str + ", ";
			}
			text += "\n";
		}
		Debug.Log(text);
	}

	private void DebugSocialAudit()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		for (int i = 0; i < 100; i++)
		{
			CondOwner condOwner = new PersonSpec
			{
				strLootConds = "CONDNPCRandom"
			}.MakeCondOwner(PersonSpec.StartShip.OLD, CrewSim.shipCurrentLoaded);
			foreach (JsonInteraction jsonInteraction in DataHandler.dictInteractions.Values)
			{
				if (jsonInteraction.bSocial)
				{
					Interaction interaction = DataHandler.GetInteraction(jsonInteraction.strName, null, false);
					if (!dictionary2.ContainsKey(interaction.strName))
					{
						dictionary2[interaction.strName] = 0;
					}
					if (interaction.CTTestUs.Triggered(condOwner, null, true))
					{
						Dictionary<string, int> dictionary3;
						string strName;
						(dictionary3 = dictionary2)[strName = interaction.strName] = dictionary3[strName] + 1;
					}
					else
					{
						string text = interaction.CTTestUs.strFailReasonLast;
						int num = text.IndexOf("/");
						if (num >= 0)
						{
							text = "Chance: " + text.Substring(num);
						}
						if (!dictionary.ContainsKey(text))
						{
							dictionary[text] = 0;
						}
						Dictionary<string, int> dictionary3;
						string key;
						(dictionary3 = dictionary)[key = text] = dictionary3[key] + 1;
					}
				}
			}
			condOwner.Destroy();
		}
		string text2 = string.Empty;
		foreach (KeyValuePair<string, int> keyValuePair in dictionary2)
		{
			string text3 = text2;
			text2 = string.Concat(new object[]
			{
				text3,
				keyValuePair.Key,
				"\t",
				keyValuePair.Value,
				"\n"
			});
		}
		DataHandler.WriteFile("DebugSocialAudit.csv", text2);
		Debug.Log(text2);
	}

	private void DebugSocialAudit2()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (JsonInteraction jsonInteraction in DataHandler.dictInteractions.Values)
		{
			if (jsonInteraction.bSocial)
			{
				Interaction interaction = DataHandler.GetInteraction(jsonInteraction.strName, null, false);
				if (interaction != null)
				{
					stringBuilder.Append(interaction.strName + "\t");
					stringBuilder.Append(this.DebugGetStatLootCSV(interaction.LootCTsUs));
					stringBuilder.Append(this.DebugGetStatLootCSV(interaction.LootCTsThem));
					if (interaction.CTTestUs != null)
					{
						List<string> allReqNames = interaction.CTTestUs.GetAllReqNames(false);
						foreach (string str in allReqNames)
						{
							stringBuilder.Append(str + ", ");
						}
						stringBuilder.Append("\t");
						allReqNames = interaction.CTTestUs.GetAllReqNames(true);
						foreach (string str2 in allReqNames)
						{
							stringBuilder.Append(str2 + ", ");
						}
						stringBuilder.Append("\t");
						stringBuilder.Append(interaction.CTTestUs.bAND.ToString());
					}
					else
					{
						stringBuilder.Append("\t");
						stringBuilder.Append("\t");
						stringBuilder.Append("false");
					}
					stringBuilder.AppendLine();
				}
			}
		}
		DataHandler.WriteFile("DebugSocialAudit2.csv", stringBuilder.ToString());
	}

	private string DebugGetStatLootCSV(Loot LootCTsUs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Dictionary<string, double> dictionary = new Dictionary<string, double>();
		dictionary["StatAchievement"] = 0.0;
		dictionary["StatAltruism"] = 0.0;
		dictionary["StatAutonomy"] = 0.0;
		dictionary["StatContact"] = 0.0;
		dictionary["StatEsteem"] = 0.0;
		dictionary["StatFamily"] = 0.0;
		dictionary["StatIntimacy"] = 0.0;
		dictionary["StatMeaning"] = 0.0;
		dictionary["StatPrivacy"] = 0.0;
		dictionary["StatSecurity"] = 0.0;
		dictionary["StatSelfRespect"] = 0.0;
		if (LootCTsUs != null)
		{
			List<CondTrigger> ctloot = LootCTsUs.GetCTLoot(null, null);
			foreach (CondTrigger condTrigger in ctloot)
			{
				if (!dictionary.ContainsKey(condTrigger.strCondName))
				{
					dictionary[condTrigger.strCondName] = 0.0;
				}
				Dictionary<string, double> dictionary2;
				string strCondName;
				(dictionary2 = dictionary)[strCondName = condTrigger.strCondName] = dictionary2[strCondName] + (double)condTrigger.fCount;
			}
		}
		stringBuilder.Append(dictionary["StatAchievement"] + "\t");
		stringBuilder.Append(dictionary["StatAltruism"] + "\t");
		stringBuilder.Append(dictionary["StatAutonomy"] + "\t");
		stringBuilder.Append(dictionary["StatContact"] + "\t");
		stringBuilder.Append(dictionary["StatEsteem"] + "\t");
		stringBuilder.Append(dictionary["StatFamily"] + "\t");
		stringBuilder.Append(dictionary["StatIntimacy"] + "\t");
		stringBuilder.Append(dictionary["StatMeaning"] + "\t");
		stringBuilder.Append(dictionary["StatPrivacy"] + "\t");
		stringBuilder.Append(dictionary["StatSecurity"] + "\t");
		stringBuilder.Append(dictionary["StatSelfRespect"] + "\t");
		return stringBuilder.ToString();
	}

	private void OnApplicationQuit()
	{
	}

	public void CycleCrew(CondOwner becomes = null)
	{
		if (GUIInventory.instance.IsOpen && GUIInventory.instance.Selected != null)
		{
			CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("GUI_INV_NO_CYCLE", false), "Bad", CrewSim.GetSelectedCrew().strID);
			AudioManager.am.PlayAudioEmitter("UIMessageLogBad", false, true);
			return;
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		bool flag = false;
		if (CrewSim.guiPDA != null && CrewSim.guiPDA.pdaVisualisers != null)
		{
			selectedCrew.ApplyGPMChanges(new string[]
			{
				"PDAVizSettings,strSettings," + CrewSim.guiPDA.pdaVisualisers.CreateCustomInfo()
			});
		}
		if (selectedCrew.Company == null || selectedCrew.Company != CrewSim.coPlayer.Company || CrewSim.GetSelectedCrew().ship.LoadState < Ship.Loaded.Edit)
		{
			CrewSim.OnRightClick.Invoke(null);
			CrewSim.objInstance.StartCoroutine(this.CycleCrewPart2(selectedCrew, CrewSim.coPlayer));
			return;
		}
		Dictionary<string, JsonCompanyRules> mapRoster = selectedCrew.Company.mapRoster;
		int count = mapRoster.Count;
		if (count > 1)
		{
			if (becomes == null)
			{
				foreach (KeyValuePair<string, JsonCompanyRules> keyValuePair in mapRoster)
				{
					if (flag)
					{
						Debug.LogWarning("Trying 1st: " + keyValuePair.Key);
						if (becomes == null)
						{
							becomes = DataHandler.GetCondOwner(null, keyValuePair.Key, null, true, null, null, null, null);
						}
						if (!(becomes == null) && becomes.ship != null && becomes.bAlive)
						{
							flag = false;
							break;
						}
						becomes = null;
					}
					if (!flag && keyValuePair.Key == selectedCrew.strID)
					{
						becomes = null;
						flag = true;
					}
				}
			}
			if (flag)
			{
				foreach (KeyValuePair<string, JsonCompanyRules> keyValuePair2 in mapRoster)
				{
					Debug.LogWarning("Trying 2nd: " + keyValuePair2.Key);
					if (becomes == null)
					{
						becomes = DataHandler.GetCondOwner(null, keyValuePair2.Key, null, true, null, null, null, null);
					}
					if (!(becomes == null) && becomes.ship != null)
					{
						break;
					}
					becomes = null;
				}
			}
			if (becomes == null)
			{
				becomes = CrewSim.coPlayer;
			}
			CrewSim.objInstance.StartCoroutine(this.CycleCrewPart2(selectedCrew, becomes));
		}
		else
		{
			Debug.LogWarning("Warning! Only 1 CO in the mapRoster!");
		}
	}

	private IEnumerator CycleCrewPart2(CondOwner currently, CondOwner becomes)
	{
		if (currently != becomes)
		{
			CrewSim.ResetAutoPause();
		}
		MonoSingleton<GUILoadingPopUp>.Instance.ShowTooltip(DataHandler.GetString("LOAD_SHIPLOAD", false), becomes.ship.publicName);
		yield return null;
		if (becomes.ship.LoadState != Ship.Loaded.Full)
		{
			MonoSingleton<AsyncShipLoader>.Instance.Unload(null);
			Ship ship = CrewSim.system.SpawnShip(becomes.ship.strRegID, Ship.Loaded.Full);
			if (ship != null)
			{
				CondOwnerVisitorCatchUp visitor = new CondOwnerVisitorCatchUp();
				ship.VisitCOs(visitor, true, true, true);
				CrewSim.LowerUI(false);
				Ship ship2 = currently.ship;
				List<Ship> list = CrewSim.system.GetAllLoadedShips().ToList<Ship>();
				if (list != null)
				{
					foreach (Ship ship3 in list)
					{
						if (ship3 != ship && ship3.GetDockedShip(ship.strRegID) == null)
						{
							if (ship3.gameObject.activeInHierarchy || ship3.LoadState > Ship.Loaded.Shallow)
							{
								this.SaveToShallow(ship3);
							}
						}
					}
				}
				MonoSingleton<AsyncShipLoader>.Instance.LoadDockedBarterZoneShips(CrewSim.coPlayer);
				ship.ToggleVis(true, true);
			}
		}
		CrewSim.objInstance.SetBracketTarget(becomes.strID, false, false);
		CrewSim.objInstance.CamCenter(becomes);
		if (currently != becomes)
		{
			CrewSim.CanvasManager.helmet.TunnelOpacity(CrewSim.CanvasManager.helmet.GetTunnelAmount(becomes), true);
		}
		MonoSingleton<GUILoadingPopUp>.Instance.FadeOutToolTip(1.5f);
		MonoSingleton<GUICrewStatus>.Instance.Refresh();
		CrewSim.OnRightClick.Invoke(null);
		if (CrewSim.guiPDA != null && CrewSim.guiPDA.pdaVisualisers != null)
		{
			CrewSim.guiPDA.pdaVisualisers.ResolveCustomInfo(becomes.GetGPMInfo("PDAVizSettings", "strSettings"));
			CrewSim.guiPDA.pdaVisualisers.AssembleUI();
		}
		yield break;
	}

	public IEnumerator SpawnTrail(TrailRenderer trail, Vector3 vEnd)
	{
		if (trail == null)
		{
			yield break;
		}
		float fTime = 0f;
		Vector3 vStart = trail.transform.position;
		while (fTime < 1f)
		{
			trail.transform.position = Vector3.Lerp(vStart, vEnd, fTime);
			fTime += CrewSim.TimeElapsedScaled() / trail.time;
			yield return null;
		}
		trail.transform.position = vEnd;
		UnityEngine.Object.Destroy(trail.gameObject, trail.time);
		yield break;
	}

	internal static string[] CustomInfosString()
	{
		string text = "MeatState=" + CrewSim.eMeatState.ToString();
		string text2 = "PDAOverlay=" + CrewSim.guiPDA.pdaVisualisers.CreateCustomInfo();
		string text3 = "PDANotes=" + CrewSim.guiPDA.pdaNotes.CreateCustomInfo();
		string text4 = "PDATimer=" + CrewSim.guiPDA.pdaTimer.CreateCustomInfo();
		string text5 = "PDAPresets=" + CrewSim.guiPDA.pdaVisualisers.PresetsToCustomInfos();
		string text6 = "PDASocialFilters=" + CrewSim.guiPDA.GetFilterSave();
		return new string[]
		{
			text,
			text5,
			text2,
			text3,
			text4,
			text6
		};
	}

	internal static void SetCustomInfos(string[] inputs)
	{
		if (inputs == null)
		{
			return;
		}
		foreach (string text in inputs)
		{
			string[] array = text.Split(new char[]
			{
				'='
			});
			if (array.Length > 1)
			{
				string text2 = array[0];
				switch (text2)
				{
				case "MeatState":
					CrewSim.ResolveMeatState(array[1]);
					goto IL_18F;
				case "PDAPresets":
					CrewSim.guiPDA.pdaVisualisers.PresetsFromCustomInfos(array[1]);
					goto IL_18F;
				case "PDAOverlay":
					CrewSim.guiPDA.pdaVisualisers.ResolveCustomInfo(array[1]);
					goto IL_18F;
				case "PDANotes":
					CrewSim.guiPDA.pdaNotes.ResolveCustomInfo(array[1]);
					goto IL_18F;
				case "PDANotesAdd":
					CrewSim.guiPDA.pdaNotes.CustomInfoAddition(array[1]);
					goto IL_18F;
				case "PDATimer":
					CrewSim.guiPDA.pdaTimer.ResolveCustomInfo(array[1]);
					goto IL_18F;
				case "PDASocialFilters":
					CrewSim.guiPDA.BuildFilters(array[1]);
					goto IL_18F;
				}
				Debug.LogWarning("Custom Infos not recognised! Skipping.");
			}
			IL_18F:;
		}
	}

	internal static void ResolveMeatState(string meatState)
	{
		switch (meatState)
		{
		case "Inert":
		case "inert":
		case "0":
			CrewSim.eMeatState = MeatState.Inert;
			return;
		case "Dormant":
		case "dormant":
		case "1":
			CrewSim.eMeatState = MeatState.Dormant;
			return;
		case "Spread":
		case "spread":
		case "2":
			CrewSim.eMeatState = MeatState.Spread;
			return;
		case "Decay":
		case "decay":
		case "3":
			CrewSim.eMeatState = MeatState.Decay;
			return;
		case "Eradicate":
		case "eradicate":
		case "4":
			CrewSim.eMeatState = MeatState.Eradicate;
			return;
		case "Hell":
		case "hell":
		case "5":
			CrewSim.eMeatState = MeatState.Hell;
			return;
		}
		Debug.LogWarning("MeatState not recognised when loading! Leaving Dormant");
		CrewSim.eMeatState = MeatState.Dormant;
	}

	// Note: this type is marked as 'beforefieldinit'.
	static CrewSim()
	{
		int[] array = new int[4];
		array[1] = 14;
		CrewSim.aReqVersion = array;
		CrewSim.PowerVizVisible = false;
		CrewSim.COAnimators = new List<Animator>();
		CrewSim.OnLeftClick = new OnMouseDownEvent();
		CrewSim.OnRightClick = new OnMouseDownEvent();
		CrewSim.bEnableDebugCommands = false;
		CrewSim.sDebugCode = "UNLOCKDEBUG";
	}

	public static OnTileSelectionEvent OnTileSelectionUpdated;

	public static RefreshTooltipEvent RefreshTooltipEvent = new RefreshTooltipEvent();

	public static UnityEvent OnFinishLoading = new UnityEvent();

	public static UnityEvent OnGameFinishedLoading = new UnityEvent();

	public static UnityEvent OnGameEnd = new UnityEvent();

	public static UnityEvent OnTimeScaleUpdated = new UnityEvent();

	public const float M_PER_TILE = 0.32f;

	public const float PRESSURE_ATMOSPHERIC = 101.3f;

	public const float GAS_CONSTANT = 0.008314f;

	public const float TILE_VOLUME = 0.25599998f;

	public const float KM_PER_PC = 3.0856777E+13f;

	public const float KM_PER_AU = 149597870f;

	public const float M_PER_AU = 1.4959786E+11f;

	public const float AU_PER_M = 6.684587E-12f;

	public const float MOS_PER_YEAR = 12f;

	public const float DAYS_PER_YEAR = 360f;

	public const float SEC_PER_YEAR = 31556926f;

	public const float SEC_PER_DAY = 87658.125f;

	public const float GRAV_CONSTANT = 6.67408E-11f;

	public const float DOCKING_RANGE = 3.3422936E-08f;

	public const float NO_WAKE_ZONE_RANGE = 2.005376E-06f;

	public const float CLOSE_APPROACH_RANGE = 3.3422937E-05f;

	public const double DOCKING_CLAMP_RANGE_COEFF = 1.1;

	public const double DOCKING_PUSHBACK_V = 3.342293532089815E-11;

	public const double ATC_SPEED_LIMIT = 5.013440183831985E-09;

	public const double LIGHT_SPEED = 0.0020039887409959503;

	public const double DEEP_SPACE_SPEED_LIMIT = 0.00020039887409959505;

	public const double MIN_TORCH_SPEED_LIMIT = 1E-08;

	public const double AI_SAFE_ACCEL_LIMIT = 2.9;

	public const double TEMPERATURE_BACKGROUND = 2.725480079650879;

	public const float EARTH_G = 9.81f;

	public const float MICRO_G = 1E-05f;

	private Coroutine _bulkSaver;

	public static CrewSim objInstance;

	public static float fTotalGameSec;

	public static float fTotalGameSecUnscaled;

	public static float fTotalGameSecSession;

	private static float fTimeCoeffPause;

	public static bool bRaiseUI = false;

	public static bool ZoneMenuOpen = false;

	public static bool bPauseLock = false;

	public static bool bUILock = false;

	public static int nRetogglePwr = 0;

	public static JsonShip jsonShip;

	public static GUISaveIndicator objGUISaveIndicator;

	public static Toggle objGUISaveOnClose;

	private static Text txtDebug;

	private static TMP_Text txtDebug2;

	private static TMP_Text txtDialogue;

	private static RectTransform rectRotate;

	private static CanvasGroup cgRotate;

	private static Text txtQueue;

	private static Text txtPriorities;

	private static Text txtAnim;

	private static TMP_Text txtMessageLog;

	private static Toggle chkZones;

	private static Toggle chkAutoPause;

	private static Toggle chkFill;

	private static Toggle chkDraw;

	private static Toggle chkPwrSE;

	private static Toggle chkAIAuto;

	private static Toggle chkWallet;

	private static TMP_Text txtCash;

	private static ToggleGroup tgMenu;

	private static Button btnCPLeft;

	private static Button btnCPRight;

	private static Button btnCPTop;

	private static Button btnCPBottom;

	private static Button btnCPExit;

	public static List<CondOwner> aSelected;

	public static List<CondOwner> aCrew;

	private global::Visibility visPlayer;

	public static GameObject goSun;

	public static HashSet<global::Visibility> aLights;

	public static HashSet<Block> blocks;

	private static GameObject goCrewBar;

	private static GameObject goCrewBarPortraitButton;

	public static GameObject goUI;

	private static GameObject goCanvasGUI;

	public static GameObject goIntUIPanel;

	private static GameObject goDialogue;

	private static Transform tfGUIInputsContent;

	private static Transform tfDialogContent;

	private static Transform tfCondContent;

	private static TMP_Text txtBtnReplaceFloors;

	private static TMP_Text txtBtnReplaceWalls;

	private static TMP_Text txtEyedropper;

	public static Ship shipPlayerOwned;

	private static List<Ship> aLoadedShips;

	public static StarSystem system;

	public static CondOwner coPlayer;

	public static VFXGasPuffs vfxPuffs;

	public static VFXSparks vfxSparks;

	private static CondOwner coCamCenter;

	private static bool bWarnedShipEdit = false;

	public static bool bShipEdit = false;

	public static bool bShipEditTest = false;

	public static bool bShipEditHide = false;

	public static bool bShipEditBG = false;

	public static bool bDebugShow = false;

	public static bool bJustClickedInput = false;

	public static bool bPoolVisUpdates = false;

	public static bool bPoolShipUpdates = false;

	public static bool bContinuePaintingJob = true;

	public static Interaction iaItmInstall = null;

	public static JsonInstallable jiLast;

	private static UniqueList<CondOwner> aTickers;

	private static List<CondOwner> aTickersTemp;

	public static GUIInventory inventoryGUI;

	public static GUIPDA guiPDA;

	public static List<Pathfinder> pathfinders;

	private static bool bTyping = false;

	public static string strSaveVersion;

	public static int[] aSaveVersion;

	public static bool bSaveUsesOldDockCount = false;

	public static bool bSaveUsesOldContainerGrids = false;

	public static bool bSaveHasENCPoliceBoard = false;

	public static bool bSaveHasCondRuleDupes = false;

	public static bool bSaveHasMissingPledgeUs = false;

	public static bool bSaveHasMissingPledgePayloads = false;

	public static bool bIsQuickstartSession;

	public static MeatState eMeatState = MeatState.Dormant;

	public static bool bUnpauseShield = false;

	public static double fPauseFlashExtra = 0.0;

	private bool bcombatAutoPauseAllowed = true;

	private static Tuple<double, string> tplAutoPause;

	private Vector3 vMouse;

	private Vector3 vPanVelocity = new Vector3(0f, 0f, 0f);

	private Transform pnlPartsContent;

	private Transform pnlPartsContent2;

	private Transform pnlPartsSearch;

	private Transform pnlPartsSearchText;

	private GameObject goShipEdit;

	private GameObject pnlPartEdit;

	private GameObject btnPartTemplate;

	public GameObject goSelPart;

	public GameObject goPaintJob;

	private List<GameObject> aFields;

	public ContextMenuPool contextMenuPool;

	private ScrollRect srMessageLog;

	public CODicts coDicts;

	public WorkManager workManager;

	public static CanvasManager CanvasManager;

	public static int resolutionX;

	public static int resolutionY;

	public bool checkResolution;

	public GameObject tooltipGO;

	public GUITooltip tooltip;

	public CrewSimTut CrewSimTut;

	public GUILetterbox LetterboxTop;

	public GUILetterbox LetterboxBottom;

	private float fCamSpeed = 7f;

	private float fUIUpdateHeartbeat = 1f;

	private float fUIUpdateLast;

	public float RightMouseButtonDownTimer;

	public float RightMouseButtonDownMax = 0.5f;

	public bool bRaisedMenuThisFrame;

	private int nLastClickIndex;

	private Vector2 vLastClick;

	private Vector3 vLastMouse;

	public Vector3 vDragStart;

	public Vector3 vDragStartScreen;

	[SerializeField]
	private GUICursorRoundel cursorRoundel;

	[SerializeField]
	private GameObject _loadingPrefab;

	[SerializeField]
	private GameObject _confirmationDialoguePrefab;

	public Camera camMain;

	public Camera UICamera;

	public Camera camHighlight;

	public Camera ScreenShotCam;

	public Camera ActiveCam;

	public VHSPostProcessEffect vhs;

	private Vector3 camTravel = new Vector3(0f, 0f, 0f);

	private float camTravelVelocity;

	public bool camFollow;

	public CameraFocusZoom camZoom;

	private float fShakeRaw;

	[NonSerialized]
	public float fShakeUserPref = 1f;

	private Vector3 vShake;

	private List<string> aHidden;

	private VectorLine lineSignal;

	private VectorLine linePower;

	private VectorLine lineSelectRect;

	private CondOwner coConnectLastCrew;

	public CondOwner coConnectMode;

	private CondTrigger ctSelectFilter;

	private GUIData igdConnectMode;

	public static Tuple<string, CondOwner> tplCurrentUI = null;

	public static Tuple<string, CondOwner> tplLastUI = null;

	private GameObject goStatus;

	private CanvasGroup cgPause;

	public static TrailRenderer BulletTrail;

	private static Texture2D[] aCursors;

	private static int nCursor = -1;

	private bool _finishedLoading;

	public static bool bDebug01 = false;

	public static bool bDebug02 = false;

	public static bool bDebug03 = false;

	public static bool bDebug04 = false;

	public static bool bDebug05 = false;

	public static bool bSoakTest = true;

	public static string strDebugOut;

	public static int[] aReqVersion;

	public static bool chargenOnNewGame;

	public static bool tutorialOnNewGame;

	public static bool PowerVizVisible;

	public float delX;

	public float delY;

	public float delZ;

	[SerializeField]
	private CanvasGroup LoadFailure;

	private readonly string[] _layerMaskDefLosTileHelpers = new string[]
	{
		"Default",
		"LoS",
		"Tile Helpers",
		"Placeholder"
	};

	private readonly string[] _layerMaskTileHelpers = new string[]
	{
		"Tile Helpers"
	};

	private readonly string[] _layerMaskDefault = new string[]
	{
		"Default"
	};

	private readonly string[] _layerMaskDefaultLos = new string[]
	{
		"Default",
		"LoS",
		"Placeholder"
	};

	public static List<Animator> COAnimators;

	private bool startedChargen;

	public bool bHighlightInteractable;

	public KeyCode kHighlightInteractableKey;

	private HashSet<CondOwner> currentHighlight = new HashSet<CondOwner>();

	public static OnMouseDownEvent OnLeftClick;

	public static OnMouseDownEvent OnRightClick;

	public static bool bEnableDebugCommands;

	private static string sDebugCode;

	private static int nDebugIndex;

	private static Dictionary<string, CondOwner> dictCOConts;

	[CompilerGenerated]
	private static UnityAction <>f__mg$cache0;

	[CompilerGenerated]
	private static Predicate<CondOwner> <>f__mg$cache1;
}
