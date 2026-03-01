using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ostranauts.Events;
using TMPro;
using UnityEngine;

// Keybinding configuration UI and runtime command registry.
// This builds the list of bindable actions, associates each GameAction with a
// Command object, and persists/loads the player's custom key combos.
public class GUIActionKeySelector : MonoBehaviour
{
	// Builds the bindable action list and seeds the shared static command references.
	// This mirrors the GameAction enum grouping into camera, ship, time, inventory,
	// special, and meta control sections.
	private void Awake()
	{
		if (GUIActionKeySelector.OnKeyDown == null)
		{
			GUIActionKeySelector.OnKeyDown = new OnKeyDownEvent();
		}
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prefabGUIActionKey, base.transform.Find("SpawnPoint"));
		this.nothing = gameObject.GetComponent<GUIActionKey>();
		this.nothing.command = new DoNothing();
		gameObject.AddComponent<CanvasGroup>();
		CanvasManager.HideCanvasGroup(gameObject);
		GUIActionKeySelector.dictActionKeysStaticRef = this.dictActionKeys;
		this.InitSpacer("Camera Controls");
		GUIActionKeySelector.commandPanCameraUp = new CommandPanCameraUp();
		this.InitKey(GameAction.cmdPanCamUp, GUIActionKeySelector.commandPanCameraUp, true);
		GUIActionKeySelector.commandToggleToolTipDetail = new CommandToogleTooltipDetail();
		this.InitKey(GameAction.cmdToggleToolTipDetail, GUIActionKeySelector.commandToggleToolTipDetail, true);
		GUIActionKeySelector.commandPanCameraLeft = new CommandPanCameraLeft();
		this.InitKey(GameAction.cmdPanCamLeft, GUIActionKeySelector.commandPanCameraLeft, true);
		GUIActionKeySelector.commandPanCameraDown = new CommandPanCameraDown();
		this.InitKey(GameAction.cmdPanCamDown, GUIActionKeySelector.commandPanCameraDown, true);
		GUIActionKeySelector.commandPanCameraRight = new CommandPanCameraRight();
		this.InitKey(GameAction.cmdPanCamRight, GUIActionKeySelector.commandPanCameraRight, true);
		GUIActionKeySelector.commandCentrePlayer = new CommandCentrePlayer();
		this.InitKey(GameAction.cmdCentrePlayer, GUIActionKeySelector.commandCentrePlayer, true);
		GUIActionKeySelector.commandZoomIn = new CommandZoomIn();
		this.InitKey(GameAction.cmdZoomIn, GUIActionKeySelector.commandZoomIn, true);
		GUIActionKeySelector.commandZoomOut = new CommandZoomOut();
		this.InitKey(GameAction.cmdZoomOut, GUIActionKeySelector.commandZoomOut, true);
		GUIActionKeySelector.commandRotateCCW = new CommandRotateCCW();
		this.InitKey(GameAction.cmdRotateCCW, GUIActionKeySelector.commandRotateCCW, true);
		GUIActionKeySelector.commandRotateCW = new CommandRotateCW();
		this.InitKey(GameAction.cmdRotateCW, GUIActionKeySelector.commandRotateCW, true);
		GUIActionKeySelector.commandPanFaster = new CommandPanFaster();
		this.InitKey(GameAction.cmdPanFaster, GUIActionKeySelector.commandPanFaster, false);
		GUIActionKeySelector.commandPanSlower = new CommandPanSlower();
		this.InitKey(GameAction.cmdPanSlower, GUIActionKeySelector.commandPanSlower, false);
		this.InitSpacer("Ship Controls");
		GUIActionKeySelector.commandFlyUp = new CommandFlyUp();
		this.InitKey(GameAction.cmdFlyUp, GUIActionKeySelector.commandFlyUp, false);
		GUIActionKeySelector.commandFlyDown = new CommandFlyDown();
		this.InitKey(GameAction.cmdFlyDown, GUIActionKeySelector.commandFlyDown, false);
		GUIActionKeySelector.commandFlyLeft = new CommandFlyLeft();
		this.InitKey(GameAction.cmdFlyLeft, GUIActionKeySelector.commandFlyLeft, false);
		GUIActionKeySelector.commandFlyRight = new CommandFlyRight();
		this.InitKey(GameAction.cmdFlyRight, GUIActionKeySelector.commandFlyRight, false);
		GUIActionKeySelector.commandShipCCW = new CommandShipCCW();
		this.InitKey(GameAction.cmdRotateShipCCW, GUIActionKeySelector.commandShipCCW, false);
		GUIActionKeySelector.commandShipCW = new CommandShipCW();
		this.InitKey(GameAction.cmdRotateShipCW, GUIActionKeySelector.commandShipCW, false);
		GUIActionKeySelector.commandShipLockW = new CommandShipLockW();
		this.InitKey(GameAction.cmdLockW, GUIActionKeySelector.commandShipLockW, false);
		GUIActionKeySelector.commandShipAttitude = new CommandShipAttitude();
		this.InitKey(GameAction.cmdAttitude, GUIActionKeySelector.commandShipAttitude, false);
		GUIActionKeySelector.commandShipMatchSpeed = new CommandShipMatchSpeed();
		this.InitKey(GameAction.cmdMatchSpeed, GUIActionKeySelector.commandShipMatchSpeed, true);
		this.InitSpacer("Time Controls");
		GUIActionKeySelector.commandPause = new CommandPause();
		this.InitKey(GameAction.cmdPause, GUIActionKeySelector.commandPause, true);
		GUIActionKeySelector.commandTimeFaster = new CommandIncreaseTimeScale();
		this.InitKey(GameAction.cmdTimeFaster, GUIActionKeySelector.commandTimeFaster, true);
		GUIActionKeySelector.commandTimeSlower = new CommandDecreaseTimeScale();
		this.InitKey(GameAction.cmdTimeSlower, GUIActionKeySelector.commandTimeSlower, true);
		this.InitSpacer("Inventory Controls");
		GUIActionKeySelector.commandInventory = new CommandInventory();
		this.InitKey(GameAction.cmdInventory, GUIActionKeySelector.commandInventory, true);
		GUIActionKeySelector.commandQuickMove = new CommandQuickMove();
		this.InitKey(GameAction.cmdQuickMove, GUIActionKeySelector.commandQuickMove, false);
		GUIActionKeySelector.commandSingleItem = new CommandSingleItem();
		this.InitKey(GameAction.cmdSingleItem, GUIActionKeySelector.commandSingleItem, false);
		GUIActionKeySelector.commandEyedropper = new CommandEyedropper();
		this.InitKey(GameAction.cmdEyedropper, GUIActionKeySelector.commandEyedropper, false);
		GUIActionKeySelector.commandRotateItem = new CommandRotateItem();
		this.InitKey(GameAction.cmdRotateItem, GUIActionKeySelector.commandRotateItem, true);
		this.InitSpacer("Special Controls");
		GUIActionKeySelector.commandPDAToggle = new CommandPDAToggle();
		this.InitKey(GameAction.cmdPDAToggle, GUIActionKeySelector.commandPDAToggle, true);
		GUIActionKeySelector.commandTogglePowerVis = new CommandTogglePowerVis();
		this.InitKey(GameAction.cmdTogglePowerVis, GUIActionKeySelector.commandTogglePowerVis, true);
		GUIActionKeySelector.commandToggleZoneUI = new CommandToggleZoneUI();
		this.InitKey(GameAction.cmdToggleZoneUI, GUIActionKeySelector.commandToggleZoneUI, true);
		GUIActionKeySelector.commandZoneAlternate = new CommandZoneAlternate();
		this.InitKey(GameAction.cmdZoneAlternate, GUIActionKeySelector.commandZoneAlternate, true);
		GUIActionKeySelector.commandToggleGasVis = new CommandToggleGasVis();
		this.InitKey(GameAction.cmdToggleGasVis, GUIActionKeySelector.commandToggleGasVis, true);
		GUIActionKeySelector.commandCycleCrew = new CommandCycleCrew();
		this.InitKey(GameAction.cmdCycleCrew, GUIActionKeySelector.commandCycleCrew, true);
		GUIActionKeySelector.commandToggleDamage = new CommandToggleDamage();
		this.InitKey(GameAction.cmdToggleDamage, GUIActionKeySelector.commandToggleDamage, true);
		GUIActionKeySelector.commandQuickAction1 = new CommandQuickAction1();
		this.InitKey(GameAction.cmdQuickAction1, GUIActionKeySelector.commandQuickAction1, true);
		GUIActionKeySelector.commandQuickAction2 = new CommandQuickAction2();
		this.InitKey(GameAction.cmdQuickAction2, GUIActionKeySelector.commandQuickAction2, true);
		GUIActionKeySelector.commandQuickAction3 = new CommandQuickAction3();
		this.InitKey(GameAction.cmdQuickAction3, GUIActionKeySelector.commandQuickAction3, true);
		GUIActionKeySelector.commandQuickAction4 = new CommandQuickAction4();
		this.InitKey(GameAction.cmdQuickAction4, GUIActionKeySelector.commandQuickAction4, true);
		GUIActionKeySelector.commandQuickActionExpand = new CommandQuickActionExpand();
		this.InitKey(GameAction.cmdQuickActionExpand, GUIActionKeySelector.commandQuickActionExpand, true);
		GUIActionKeySelector.commandQuickActionReset = new CommandQuickActionReset();
		this.InitKey(GameAction.cmdQuickActionReset, GUIActionKeySelector.commandQuickActionReset, true);
		GUIActionKeySelector.CommandQuickActionPin = new CommandQuickActionPin();
		this.InitKey(GameAction.cmdQuickActionPin, GUIActionKeySelector.CommandQuickActionPin, true);
		GUIActionKeySelector.CommandShowHotkeys = new CommandShowHotkeys();
		this.InitKey(GameAction.cmdLAlt, GUIActionKeySelector.CommandShowHotkeys, true);
		this.InitSpacer("Meta Controls");
		GUIActionKeySelector.commandDebug = new CommandDebug();
		this.InitKey(GameAction.cmdDebug, GUIActionKeySelector.commandDebug, true);
		GUIActionKeySelector.commandEscape = new CommandEscape();
		this.InitKey(GameAction.cmdEscape, GUIActionKeySelector.commandEscape, false);
		GUIActionKeySelector.commandAccept = new CommandAccept();
		this.InitKey(GameAction.cmdAccept, GUIActionKeySelector.commandAccept, true);
		GUIActionKeySelector.commandForceWalk = new CommandForceWalk();
		this.InitKey(GameAction.cmdForce, GUIActionKeySelector.commandForceWalk, true);
		GUIActionKeySelector.commandToggleFog = new CommandToggleFog();
		this.InitKey(GameAction.cmdToggleFog, GUIActionKeySelector.commandToggleFog, false);
		GUIActionKeySelector.commandScreenshot = new CommandScreenshot();
		this.InitKey(GameAction.cmdScreenshot, GUIActionKeySelector.commandScreenshot, false);
		GUIActionKeySelector.commandToggleConsole = new CommandToggleConsole();
		this.InitKey(GameAction.cmdConsole, GUIActionKeySelector.commandToggleConsole, false);
		GUIActionKeySelector.commandSave = new CommandSave();
		this.InitKey(GameAction.cmdSave, GUIActionKeySelector.commandSave, true);
		GUIActionKeySelector.commandUIPanelBottom = new CommandUIPanelBottom();
		this.InitKey(GameAction.cmdUIPanelBottom, GUIActionKeySelector.commandUIPanelBottom, true);
		GUIActionKeySelector.commandUIPanelLeft = new CommandUIPanelLeft();
		this.InitKey(GameAction.cmdUIPanelLeft, GUIActionKeySelector.commandUIPanelLeft, true);
		GUIActionKeySelector.commandUIPanelRight = new CommandUIPanelRight();
		this.InitKey(GameAction.cmdUIPanelRight, GUIActionKeySelector.commandUIPanelRight, true);
		GUIActionKeySelector.commandUIPanelTop = new CommandUIPanelTop();
		this.InitKey(GameAction.cmdUIPanelTop, GUIActionKeySelector.commandUIPanelTop, true);
		GUIActionKeySelector.commandNudgeSelUp = new CommandNudgeSelUp();
		this.InitKey(GameAction.cmdNudgeSelUp, GUIActionKeySelector.commandNudgeSelUp, true);
		GUIActionKeySelector.commandNudgeSelLeft = new CommandNudgeSelLeft();
		this.InitKey(GameAction.cmdNudgeSelLeft, GUIActionKeySelector.commandNudgeSelLeft, true);
		GUIActionKeySelector.commandNudgeSelDown = new CommandNudgeSelDown();
		this.InitKey(GameAction.cmdNudgeSelDown, GUIActionKeySelector.commandNudgeSelDown, true);
		GUIActionKeySelector.commandNudgeSelRight = new CommandNudgeSelRight();
		this.InitKey(GameAction.cmdNudgeSelRight, GUIActionKeySelector.commandNudgeSelRight, true);
		this.InitSpacer(string.Empty);
		this.InitSpacer(string.Empty);
		this.InitSpacer(string.Empty);
		GUIActionKeySelector.commandPanCameraController = new CommandPanCameraController();
		GUIActionKeySelector.commandMovePlayer = new CommandMovePlayer();
		GUIActionKeySelector.commandToggleFilters = new CommandToggleFilters();
		GUIActionKeySelector.commandToggleFilters.gameRenderer = GameObject.Find("Main Camera").GetComponent<GameRenderer>();
		this.Load();
	}

	// Spawns one bindable key row and links it to the Command instance for that action.
	private void InitKey(GameAction action, Command command, bool runOnFrame = true)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prefabGUIActionKey, base.transform);
		GUIActionKey component = gameObject.GetComponent<GUIActionKey>();
		this.dictActionKeys[action] = component;
		command.gameAction = action;
		component.command = command;
		component.runOnFrame = runOnFrame;
		component.actionLabel.text = component.command.commandDisplayLabel;
		component.SetKeyText(component.command.KeyName);
		this.nCount++;
	}

	// Inserts a section header row for readability in the keybinding list.
	private void InitSpacer(string title)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prefabSectionHeader, base.transform);
		TextMeshProUGUI componentInChildren = gameObject.GetComponentInChildren<TextMeshProUGUI>();
		componentInChildren.text = title;
	}

	// Replaces the primary combo list for an action and marks the bindings dirty.
	public void SetCombo(List<KeyCode> combo, GUIActionKey gak)
	{
		if (gak == null)
		{
			gak = this.nothing;
		}
		else if (gak.command != null)
		{
			gak.command.currentCombos.Clear();
			gak.command.currentCombos.Add(combo);
		}
		this.dictActionKeys[gak.command.gameAction] = gak;
		GUIActionKeySelector.bKeyChanged = true;
	}

	// Appends a combo when it is not already present.
	public void AddCombo(List<KeyCode> combo, GUIActionKey gak)
	{
		if (gak == null)
		{
			gak = this.nothing;
		}
		else if (gak.command != null)
		{
			bool flag = false;
			foreach (List<KeyCode> first in gak.command.currentCombos)
			{
				if (first.SequenceEqual(combo))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				gak.command.currentCombos.Add(combo);
			}
		}
		this.dictActionKeys[gak.command.gameAction] = gak;
		GUIActionKeySelector.bKeyChanged = true;
	}

	public void RemoveCombo(List<KeyCode> combo, GUIActionKey gak)
	{
		if (gak == null)
		{
			gak = this.nothing;
		}
		else if (gak.command != null)
		{
			gak.command.currentCombos.Remove(combo);
		}
		this.dictActionKeys[gak.command.gameAction] = gak;
		GUIActionKeySelector.bKeyChanged = true;
	}

	public void RemoveCombo(int num, GUIActionKey gak)
	{
		if (gak == null)
		{
			gak = this.nothing;
		}
		else if (gak.command != null)
		{
			gak.command.currentCombos.RemoveAt(num);
		}
		this.dictActionKeys[gak.command.gameAction] = gak;
		GUIActionKeySelector.bKeyChanged = true;
	}

	public void KeyHandler()
	{
		GUIActionKeySelector.commandEscape.Execute();
		if (CrewSim.Typing)
		{
			return;
		}
		foreach (GUIActionKey guiactionKey in this.dictActionKeys.Values)
		{
			if (guiactionKey.runOnFrame)
			{
				guiactionKey.command.Execute();
			}
		}
		if (GUIActionKeySelector.controllerXBOX)
		{
			GUIActionKeySelector.commandPanCameraController.Execute();
			GUIActionKeySelector.commandMovePlayer.Execute();
		}
	}

	public void Save()
	{
		Dictionary<string, JsonActionKey> dictionary = new Dictionary<string, JsonActionKey>();
		foreach (GUIActionKey guiactionKey in this.dictActionKeys.Values)
		{
			JsonActionKey jsonActionKey = new JsonActionKey();
			jsonActionKey.strName = guiactionKey.command.gameAction.ToString();
			jsonActionKey.nEnum = (int)guiactionKey.command.gameAction;
			jsonActionKey.lKeyCodes = guiactionKey.command.currentCombos;
			dictionary[jsonActionKey.strName] = jsonActionKey;
		}
		DataHandler.DataToJsonStreaming<JsonActionKey>(dictionary, "controls.json", true, string.Empty);
		ConsoleToGUI.instance.LogInfo("Saved control layout!");
	}

	public void Load()
	{
		Dictionary<string, JsonActionKey> dictionary = new Dictionary<string, JsonActionKey>();
		if (File.Exists(Application.persistentDataPath + "/controls.json"))
		{
			DataHandler.JsonToData<JsonActionKey>(Application.persistentDataPath + "/controls.json", dictionary);
		}
		else if (File.Exists(Application.dataPath + "/controls.json"))
		{
			DataHandler.JsonToData<JsonActionKey>(Application.dataPath + "/controls.json", dictionary);
		}
		else if (dictionary.Count == 0)
		{
			return;
		}
		List<GameAction> list = Enum.GetValues(typeof(GameAction)).Cast<GameAction>().ToList<GameAction>();
		foreach (GameAction key in list)
		{
			if (!this.dictActionKeys.ContainsKey(key))
			{
				this.dictActionKeys.Add(key, this.nothing);
			}
		}
		foreach (JsonActionKey jsonActionKey in dictionary.Values)
		{
			if (!this.dictActionKeys.ContainsKey((GameAction)jsonActionKey.nEnum))
			{
				GUIActionKeySelector.bKeyChanged = true;
				Debug.LogWarning("Incorrect Enum value loaded from saved controls - command not recognised!");
			}
			else
			{
				GameAction nEnum = (GameAction)jsonActionKey.nEnum;
				if (jsonActionKey.lKeyCodes != null)
				{
					this.dictActionKeys[nEnum].command.currentCombos = jsonActionKey.lKeyCodes;
				}
				else
				{
					this.dictActionKeys[nEnum].command.currentCombos = new List<List<KeyCode>>();
				}
			}
		}
		foreach (GUIActionKey guiactionKey in this.dictActionKeys.Values)
		{
			if (!dictionary.ContainsKey(guiactionKey.command.gameAction.ToString()) && guiactionKey != null && guiactionKey.command != null)
			{
				if (guiactionKey.command.currentCombos == null)
				{
					guiactionKey.command.currentCombos = new List<List<KeyCode>>();
				}
				if (guiactionKey.command.currentCombos.Count == 0)
				{
					guiactionKey.command.currentCombos.Add(guiactionKey.command.defaultCombo);
				}
			}
			guiactionKey.SetKeyText(guiactionKey.command.KeyName);
		}
		if (DataHandler.dictSettings != null && DataHandler.dictSettings["UserSettings"] != null)
		{
			if (DataHandler.dictSettings["UserSettings"].strVerbose == "True")
			{
				GUIActionKeySelector.verbose = true;
			}
			else
			{
				GUIActionKeySelector.verbose = false;
			}
			if (DataHandler.dictSettings["UserSettings"].strUseAxis == "True")
			{
				GUIActionKeySelector.controllerXBOX = true;
			}
			else
			{
				GUIActionKeySelector.controllerXBOX = false;
			}
		}
	}

	public void Reset()
	{
		foreach (GUIActionKey guiactionKey in this.dictActionKeys.Values.ToList<GUIActionKey>())
		{
			guiactionKey.Reset();
		}
		DataHandler.RemoveFile(Application.persistentDataPath + "/controls.json");
		this.Load();
	}

	private void Update()
	{
		if (GUIActionKeySelector.bKeyChanged)
		{
			this.Save();
			CrewSim.UpdateEyedropperKey(GUIActionKeySelector.commandEyedropper.KeyName);
			GUIActionKeySelector.bKeyChanged = false;
			GUIOrbitDraw.bUpdateWASD = true;
		}
		this.KeyHandler();
	}

	public static OnKeyDownEvent OnKeyDown;

	public GameObject prefabSectionHeader;

	public GameObject prefabGUIActionKey;

	public Dictionary<GameAction, GUIActionKey> dictActionKeys = new Dictionary<GameAction, GUIActionKey>();

	private int nCount;

	public GUIActionKey nothing;

	public static bool bKeyChanged;

	public RectTransform spawnPoint;

	public static Command commandPanCameraUp;

	public static Command commandToggleToolTipDetail;

	public static Command commandPanCameraLeft;

	public static Command commandPanCameraDown;

	public static Command commandPanCameraRight;

	public static Command commandNudgeSelUp;

	public static Command commandNudgeSelLeft;

	public static Command commandNudgeSelDown;

	public static Command commandNudgeSelRight;

	public static Command commandZoomIn;

	public static Command commandZoomOut;

	public static CommandDebug commandDebug;

	public static CommandInventory commandInventory;

	public static CommandPause commandPause;

	public static CommandForceWalk commandForceWalk;

	public static CommandIncreaseTimeScale commandTimeFaster;

	public static CommandDecreaseTimeScale commandTimeSlower;

	public static CommandCentrePlayer commandCentrePlayer;

	public static CommandRotateCCW commandRotateCCW;

	public static CommandRotateCW commandRotateCW;

	public static CommandEscape commandEscape;

	public static CommandRotateItem commandRotateItem;

	public static CommandQuickMove commandQuickMove;

	public static CommandScreenshot commandScreenshot;

	public static CommandToggleFog commandToggleFog;

	public static CommandPanFaster commandPanFaster;

	public static CommandPanSlower commandPanSlower;

	public static CommandToggleConsole commandToggleConsole;

	public static CommandFlyUp commandFlyUp;

	public static CommandFlyDown commandFlyDown;

	public static CommandFlyLeft commandFlyLeft;

	public static CommandFlyRight commandFlyRight;

	public static CommandShipLockW commandShipLockW;

	public static CommandSingleItem commandSingleItem;

	public static CommandEyedropper commandEyedropper;

	public static CommandShipCCW commandShipCCW;

	public static CommandShipCW commandShipCW;

	public static CommandShipAttitude commandShipAttitude;

	public static CommandShipMatchSpeed commandShipMatchSpeed;

	public static CommandTogglePowerVis commandTogglePowerVis;

	public static CommandToggleZoneUI commandToggleZoneUI;

	public static CommandZoneAlternate commandZoneAlternate;

	public static CommandToggleGasVis commandToggleGasVis;

	public static CommandPDAToggle commandPDAToggle;

	public static CommandAccept commandAccept;

	public static CommandSave commandSave;

	public static CommandCycleCrew commandCycleCrew;

	public static CommandToggleDamage commandToggleDamage;

	public static CommandUIPanelLeft commandUIPanelLeft;

	public static CommandUIPanelRight commandUIPanelRight;

	public static CommandUIPanelTop commandUIPanelTop;

	public static CommandUIPanelBottom commandUIPanelBottom;

	public static CommandQuickAction1 commandQuickAction1;

	public static CommandQuickAction2 commandQuickAction2;

	public static CommandQuickAction3 commandQuickAction3;

	public static CommandQuickAction4 commandQuickAction4;

	public static CommandQuickActionPin CommandQuickActionPin;

	public static CommandQuickActionReset commandQuickActionReset;

	public static CommandQuickActionExpand commandQuickActionExpand;

	public static CommandToggleFilters commandToggleFilters;

	public static CommandShowHotkeys CommandShowHotkeys;

	public static CommandPanCameraController commandPanCameraController;

	public static CommandMovePlayer commandMovePlayer;

	public static bool controllerXBOX = true;

	public static bool verbose;

	public static Dictionary<GameAction, GUIActionKey> dictActionKeysStaticRef;
}
