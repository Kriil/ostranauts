using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Components;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.UI.PDA;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// PDA shell UI that switches between objectives, jobs, socials, notes, and tools.
// This is likely the player's handheld app container layered over several
// feature-specific panels such as ObjectivesApp, PDANotes, and PDATimer.
public class GUIPDA : MonoBehaviour
{
	// Converts persisted/string-driven UI state names into the enum used at runtime.
	public static GUIPDA.UIState GetStateFromString(string strState)
	{
		GUIPDA.UIState result = GUIPDA.UIState.Closed;
		if (strState == null)
		{
			return result;
		}
		switch (strState)
		{
		case "Loading":
			return GUIPDA.UIState.Loading;
		case "Closed":
			return GUIPDA.UIState.Closed;
		case "Home":
			return GUIPDA.UIState.Home;
		case "Objectives":
			return GUIPDA.UIState.Objectives;
		case "JobOrder":
			return GUIPDA.UIState.JobOrder;
		case "JobBuild":
			return GUIPDA.UIState.JobBuild;
		case "JobBuildScroll":
			return GUIPDA.UIState.JobBuildScroll;
		case "Tasks":
			return GUIPDA.UIState.Tasks;
		case "Socials":
			return GUIPDA.UIState.Socials;
		case "GigNexus":
			return GUIPDA.UIState.GigNexus;
		case "Ferry":
			return GUIPDA.UIState.Ferry;
		case "NavLink":
			return GUIPDA.UIState.NavLink;
		case "Viz":
			return GUIPDA.UIState.Viz;
		case "Notes":
			return GUIPDA.UIState.Notes;
		case "Timer":
			return GUIPDA.UIState.Timer;
		}
		return result;
	}

	private void Awake()
	{
	}

	// Caches child panels, wires button/toggle events, and initializes the default closed state.
	public void Init()
	{
		GUIPDA.txtPDAUTC = base.transform.Find("pnlBackground/pnlBezel/pnlTopBar/txtTime").GetComponent<TMP_Text>();
		this.goJobTypes = base.transform.Find("pnlJobs/pnlJobTypes").gameObject;
		this.goJobOptions = base.transform.Find("pnlJobs/scrollJobOptions/Viewport/pnlJobOptions").gameObject;
		Button component = base.transform.Find("pnlGigNexus/pnlGig").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			this.ToggleGig(false);
		});
		this.cgObjectives = base.transform.Find("pnlObjectives").GetComponent<CanvasGroup>();
		this.objectivesApp = this.cgObjectives.GetComponent<ObjectivesApp>();
		this.cgJobOptionsScroll = base.transform.Find("pnlJobs/scrollJobOptions").GetComponent<CanvasGroup>();
		this.cgJobs = base.transform.Find("pnlJobs").GetComponent<CanvasGroup>();
		this.cgTasks = base.transform.Find("pnlTasks").GetComponent<CanvasGroup>();
		this.cgGigNexus = base.transform.Find("pnlGigNexus").GetComponent<CanvasGroup>();
		this.cgFerry = base.transform.Find("pnlFerry").GetComponent<CanvasGroup>();
		this.cgHome = base.transform.Find("pnlHome").GetComponent<CanvasGroup>();
		this.cgJobFilters = base.transform.Find("pnlJobs/pnlJobFilters").GetComponent<CanvasGroup>();
		this.cgViz = base.transform.Find("pnlViz").GetComponent<CanvasGroup>();
		this.pdaVisualisers = this.cgViz.GetComponent<PDAVisualisers>();
		this.cgNotes = base.transform.Find("pnlNotes").GetComponent<CanvasGroup>();
		this.pdaNotes = this.cgNotes.GetComponent<PDANotes>();
		this.cgTimer = base.transform.Find("pnlTimer").GetComponent<CanvasGroup>();
		this.pdaTimer = this.cgTimer.GetComponent<PDATimer>();
		this.chkFilterWalls = base.transform.Find("pnlJobs/pnlJobFilters/pnlToggles/chkWalls").GetComponent<Toggle>();
		this.chkFilterWalls.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.UpdateFilterCT();
		});
		this.chkFilterFloors = base.transform.Find("pnlJobs/pnlJobFilters/pnlToggles/chkFloors").GetComponent<Toggle>();
		this.chkFilterFloors.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.UpdateFilterCT();
		});
		this.chkFilterConduits = base.transform.Find("pnlJobs/pnlJobFilters/pnlToggles/chkConduits").GetComponent<Toggle>();
		this.chkFilterConduits.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.UpdateFilterCT();
		});
		this.chkFilterCans = base.transform.Find("pnlJobs/pnlJobFilters/pnlToggles/chkCans").GetComponent<Toggle>();
		this.chkFilterCans.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.UpdateFilterCT();
		});
		this.chkFilterEquip = base.transform.Find("pnlJobs/pnlJobFilters/pnlToggles/chkEquip").GetComponent<Toggle>();
		this.chkFilterEquip.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.UpdateFilterCT();
		});
		this.chkFilterLoose = base.transform.Find("pnlJobs/pnlJobFilters/pnlToggles/chkLoose").GetComponent<Toggle>();
		this.chkFilterLoose.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.UpdateFilterCT();
		});
		Toggle toggle = this.chkFilterWalls;
		bool flag = true;
		this.chkFilterLoose.isOn = flag;
		flag = flag;
		this.chkFilterEquip.isOn = flag;
		flag = flag;
		this.chkFilterCans.isOn = flag;
		flag = flag;
		this.chkFilterConduits.isOn = flag;
		flag = flag;
		this.chkFilterFloors.isOn = flag;
		toggle.isOn = flag;
		AudioManager.AddBtnAudio(this.chkFilterWalls.gameObject, null, "ShipUIBtnPDAClick02");
		AudioManager.AddBtnAudio(this.chkFilterFloors.gameObject, null, "ShipUIBtnPDAClick02");
		AudioManager.AddBtnAudio(this.chkFilterConduits.gameObject, null, "ShipUIBtnPDAClick02");
		AudioManager.AddBtnAudio(this.chkFilterCans.gameObject, null, "ShipUIBtnPDAClick02");
		AudioManager.AddBtnAudio(this.chkFilterEquip.gameObject, null, "ShipUIBtnPDAClick02");
		AudioManager.AddBtnAudio(this.chkFilterLoose.gameObject, null, "ShipUIBtnPDAClick02");
		this.cgHotbar = base.transform.Find("pnlHotbar").GetComponent<CanvasGroup>();
		this.cgHotbar.GetComponent<GUIPDAHotBar>().Activate();
		this.homepage = this.cgHome.GetComponent<GUIPDAHomepage>();
		this.homepage.UpdateAppNotifs();
		this.btnSort.onClick.AddListener(new UnityAction(this.UpdateSocialSortButton));
		AudioManager.AddBtnAudio(this.btnSort.gameObject, null, "ShipUIBtnPDAClick02");
		this.BuildFilters(null);
		this.State = GUIPDA.UIState.Closed;
		this.TogglePDAFrame(false);
		GUIPDA.instance = this;
	}

	// Serializes the currently selected social filters for persistence.
	public string GetFilterSave()
	{
		string text = string.Empty;
		foreach (MultiSelectDTO multiSelectDTO in this._socialFilters)
		{
			if (multiSelectDTO.IsOn)
			{
				if (text != string.Empty)
				{
					text += ",";
				}
				text += multiSelectDTO.Id;
			}
		}
		return text;
	}

	// Rebuilds the social filter dropdown from loot definitions.
	// `RELSocialFilters` is likely a loot-table id from `data/loot` that supplies
	// the condition ids used to hide/show relationship tags in the PDA UI.
	public void BuildFilters(string savedFilter = null)
	{
		this._socialFilters.Clear();
		List<string> allLootNames = DataHandler.GetLoot("RELSocialFilters").GetAllLootNames();
		string[] array = null;
		if (!string.IsNullOrEmpty(savedFilter))
		{
			array = savedFilter.Split(new char[]
			{
				','
			});
		}
		foreach (string text in allLootNames)
		{
			Condition cond = DataHandler.GetCond(text);
			if (cond != null)
			{
				this._socialFilters.Add(new MultiSelectDTO
				{
					Id = text,
					FriendlyName = "Hide " + cond.strNameFriendly,
					IsOn = (array != null && array.Contains(text))
				});
			}
		}
		this.ddFilter.Init(this._socialFilters, new UnityAction<List<MultiSelectDTO>>(this.OnSocialFilterDropDown));
	}

	// Applies updated dropdown selections and refreshes the social panel.
	private void OnSocialFilterDropDown(List<MultiSelectDTO> filters)
	{
		this._socialFilters = filters;
		this.ShowSocials();
	}

	// Cycles the active social sorting mode and refreshes the icon.
	private void UpdateSocialSortButton()
	{
		this._currentSortMethod++;
		if (!Enum.IsDefined(typeof(GUIPDA.SortMethod), this._currentSortMethod))
		{
			this._currentSortMethod = GUIPDA.SortMethod.AlphabeticalAscending;
		}
		if (this._currentSortMethod < (GUIPDA.SortMethod)this._filterSprites.Length)
		{
			this.btnSort.image.sprite = this._filterSprites[(int)this._currentSortMethod];
		}
		this.ShowSocials();
	}

	private void UpdateAppNotifs()
	{
		if (this.cgHotbar != null)
		{
			this.cgHotbar.GetComponent<GUIPDAHotBar>().UpdateAppNotifs();
		}
		if (this.homepage != null)
		{
			this.homepage.UpdateAppNotifs();
		}
	}

	public void RefreshHotbar()
	{
		if (this.cgHotbar != null)
		{
			this.cgHotbar.GetComponent<GUIPDAHotBar>().Refresh();
		}
	}

	public static void OpenApp(string appName)
	{
		if (GUIPDA.instance == null)
		{
			return;
		}
		if (appName == string.Empty)
		{
			return;
		}
		string[] array = appName.Split(new char[]
		{
			':'
		});
		string text = array[0];
		string strPage = string.Empty;
		if (array.Length > 1)
		{
			strPage = array[1];
		}
		string text2 = array[0];
		switch (text2)
		{
		case "home":
			Debug.Log("Opening PDA home screen");
			GUIPDA.instance.ToggleHome();
			return;
		case "zones":
			GUIPDA.instance.State = GUIPDA.UIState.Closed;
			CrewSim.LowerUI(false);
			GUIActionKeySelector.commandToggleZoneUI.ExternalExecute();
			return;
		case "goals":
			Debug.Log("Toggling PDA objectives screen");
			GUIPDA.instance.ToggleObjectives(strPage);
			return;
		case "gigs":
			Debug.Log("Opening PDA gigs screen");
			GUIPDA.instance.ToggleGigNexus();
			return;
		case "ferry":
			Debug.Log("Opening PDA ferry screen");
			GUIPDA.instance.ToggleFerry();
			return;
		case "files":
		{
			Debug.Log("Opening PDA files screen");
			CrewSim.LowerUI(false);
			List<CondOwner> cos = CrewSim.GetSelectedCrew().GetCOs(false, GUIPDA.instance.CTPDA);
			if (cos != null && cos.Count > 0)
			{
				cos[0].AddCondAmount("IsPDAModeFiles", 1.0, 0.0, 0f);
				CrewSim.RaiseUI("Computer", cos[0]);
			}
			else
			{
				CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("GUI_PDA_FILES_ERROR", false), "Bad", CrewSim.GetSelectedCrew().strID);
			}
			return;
		}
		case "navmap":
			Debug.Log("Opening PDA nav map screen");
			GUIPDA.instance.ToggleNAV(null);
			return;
		case "navlink":
			Debug.Log("Opening PDA nav link screen");
			GUIPDA.instance.ToggleNAVLink();
			return;
		case "socials":
			Debug.Log("Opening PDA socials screen");
			GUIPDA.instance.ToggleSocials();
			return;
		case "tasks":
			Debug.Log("Opening PDA tasks screen");
			GUIPDA.instance.ToggleTasks();
			return;
		case "roster":
			Debug.Log("Opening PDA roster screen");
			GUIPDA.instance.ToggleRosterUI();
			return;
		case "vote":
			Debug.Log("Opening PDA vote screen");
			GUIPDA.instance.ToggleVoteUI();
			return;
		case "exit":
			Debug.Log("Closing PDA screen");
			GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
			GUIPDA.instance.State = GUIPDA.UIState.Closed;
			return;
		case "power":
			Debug.Log("Toggling PDA power view");
			CrewSim.objInstance.TogglePowerUI(CrewSim.shipCurrentLoaded, null);
			return;
		case "duties":
			Debug.Log("Toggling PDA duties screen");
			GUIPDA.instance.ToggleDutiesUI();
			return;
		case "build":
			Debug.Log("Toggling PDA build screen");
			GUIPDA.instance.ShowJobPaintUI("build");
			return;
		case "orders":
			Debug.Log("Toggling PDA actions screen");
			GUIPDA.instance.ShowJobPaintUI("actions");
			return;
		case "inventory":
			Debug.Log("Toggling inventory from PDA");
			GUIPDA.instance.State = GUIPDA.UIState.Closed;
			CrewSim.LowerUI(false);
			CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
			return;
		case "viz":
			Debug.Log("Toggling viz from PDA");
			GUIPDA.instance.ToggleVizUI();
			return;
		case "notes":
			Debug.Log("Toggling notes from PDA");
			GUIPDA.instance.ToggleNotesUI();
			return;
		case "timer":
			Debug.Log("Toggling timer from PDA");
			GUIPDA.instance.ToggleTimerUI();
			return;
		}
		Debug.LogWarning("Tried to open unrecognised app: " + appName + ", opening home screen!");
		GUIPDA.instance.ToggleHome();
	}

	private void TogglePDAFrame(bool show)
	{
		if (show)
		{
			this.bmpFrame.SetActive(true);
			this.bmpScreen.SetActive(true);
			this.objForeground.SetActive(true);
			this.objBackground.SetActive(true);
			this.objNavigation.SetActive(true);
			this.objHotbar.SetActive(false);
		}
		else
		{
			this.bmpFrame.SetActive(false);
			this.bmpScreen.SetActive(false);
			this.objForeground.SetActive(false);
			this.objBackground.SetActive(false);
			this.objNavigation.SetActive(false);
			this.objHotbar.SetActive(true);
			GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
		}
	}

	private void SlidePDA(int nStateNew)
	{
		Animator component = base.GetComponent<Animator>();
		component.speed = 1f / Time.timeScale;
		int integer = component.GetComponent<Animator>().GetInteger("AnimState");
		this.TogglePDAFrame(nStateNew != 1);
		if (integer == nStateNew)
		{
			return;
		}
		if (integer == 1)
		{
			component.SetInteger("AnimState", 0);
			AudioManager.am.PlayAudioEmitter("PDAOpen", false, false);
		}
		else
		{
			component.SetInteger("AnimState", 1);
			AudioManager.am.PlayAudioEmitter("PDAClose", false, false);
			if (this.cgObjectives.interactable)
			{
				this.ToggleObjectives("current");
			}
		}
	}

	private void UpdateFilterCT()
	{
		List<string> list = new List<string>();
		if (this.chkFilterWalls.isOn)
		{
			list.Add("TIsWall1x1Installed");
		}
		if (this.chkFilterFloors.isOn)
		{
			list.Add("TIsFloorGrateOrFloorBinInstalled");
		}
		if (this.chkFilterConduits.isOn)
		{
			list.Add("TIsConduit00Installed");
		}
		if (this.chkFilterCans.isOn)
		{
			list.Add("TIsRCSValidInput");
		}
		if (this.chkFilterEquip.isOn)
		{
			list.Add("TIsInstalledEquipment");
		}
		if (this.chkFilterLoose.isOn)
		{
			list.Add("TIsLoose");
		}
		if (list.Count == 0)
		{
			list.Add("TNever");
		}
		GUIPDA.ctJobFilter = new CondTrigger("GUIJobFilter", new string[0], new string[0], list.ToArray(), new string[0]);
		GUIPDA.ctJobFilter.bAND = false;
	}

	private void ToggleRosterUI()
	{
		if (CrewSim.goUI != null && CrewSim.goUI.GetComponent<GUIRoster>() != null)
		{
			CrewSim.LowerUI(false);
		}
		else
		{
			CrewSim.RaiseUI("Roster", CrewSim.GetSelectedCrew());
		}
	}

	private void ToggleDutiesUI()
	{
		CrewSim.LowerUI(false);
		CrewSim.RaiseUI("Duties", CrewSim.coPlayer);
	}

	private void ToggleVoteUI()
	{
		CrewSim.LowerUI(false);
		CrewSim.RaiseUI("Vote", CrewSim.coPlayer);
	}

	public void ToggleObjectives(string strPage = "current")
	{
		if (this.cgObjectives.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.Objectives;
			ObjectivesApp componentInChildren = base.gameObject.GetComponentInChildren<ObjectivesApp>();
			if (componentInChildren != null)
			{
				if (strPage != null)
				{
					if (!(strPage == "current"))
					{
						if (!(strPage == "finished"))
						{
							if (strPage == "settings")
							{
								componentInChildren.SetPage(ObjectivesAppPage.Settings);
							}
						}
						else
						{
							componentInChildren.SetPage(ObjectivesAppPage.Finished);
						}
					}
					else
					{
						componentInChildren.SetPage(ObjectivesAppPage.Current);
					}
				}
			}
		}
	}

	public void ToggleTasks()
	{
		if (this.cgTasks.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.Tasks;
		}
	}

	public void ToggleSocials()
	{
		if (this.cgSocials.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.Socials;
		}
	}

	public void ToggleGigNexus()
	{
		if (this.cgGigNexus.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.GigNexus;
		}
	}

	public void ToggleFerry()
	{
		if (this.cgFerry.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.Ferry;
		}
	}

	public void ToggleNAV(string strRegID = null)
	{
		CrewSim.LowerUI(false);
		Ship ship = CrewSim.GetSelectedCrew().ship;
		CondOwner condOwner = null;
		if (MonoSingleton<ObjectiveTracker>.Instance.subscribedShips.Contains(ship.strRegID) && ship.aNavs != null && ship.aNavs.Count > 0)
		{
			condOwner = ship.aNavs[0];
		}
		if (condOwner != null)
		{
			CrewSim.RaiseUI("PDANAV", condOwner);
			GUIOrbitDraw component = CrewSim.goUI.GetComponent<GUIOrbitDraw>();
			component.ToggleNavModeExt(true);
			if (!string.IsNullOrEmpty(strRegID))
			{
				string text = strRegID;
				Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
				if (shipByRegID != null && shipByRegID.IsStationHidden(false))
				{
					JsonTransit transitConnections = DataHandler.GetTransitConnections(strRegID);
					if (transitConnections != null && transitConnections.aConnections != null)
					{
						foreach (JsonTransitConnection jsonTransitConnection in transitConnections.aConnections)
						{
							if (jsonTransitConnection != null && !(jsonTransitConnection.strTargetRegID == strRegID))
							{
								shipByRegID = CrewSim.system.GetShipByRegID(jsonTransitConnection.strTargetRegID);
								if (shipByRegID == null || !shipByRegID.IsStationHidden(false))
								{
									text = jsonTransitConnection.strTargetRegID;
									break;
								}
							}
						}
					}
				}
				if (component.IsTargetKnown(text))
				{
					component.LockTarget(text);
				}
				else
				{
					CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("GUI_PDA_ERROR_SHIP_NOT_FOUND", false), "Bad", CrewSim.GetSelectedCrew().strID);
					component.LockTarget(ship.strRegID);
				}
			}
		}
		else
		{
			CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("GUI_PDA_ERROR_NAV_NOT_FOUND", false), "Bad", CrewSim.GetSelectedCrew().strID);
			this.ToggleNAVLink();
		}
	}

	public void ToggleNAVLink()
	{
		CrewSim.LowerUI(false);
		List<CondOwner> cos = CrewSim.GetSelectedCrew().GetCOs(false, DataHandler.GetCondTrigger("TIsComputerPDANotDamaged"));
		if (cos != null && cos.Count > 0)
		{
			cos[0].AddCondAmount("IsPDAModeNAVLink", 1.0, 0.0, 0f);
			cos[0].ZeroCondAmount("IsPDAModeFiles");
			CrewSim.RaiseUI("Computer", cos[0]);
		}
		else
		{
			CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("GUI_PDA_NAVLINK_ERROR", false), "Bad", CrewSim.GetSelectedCrew().strID);
		}
	}

	public void ToggleClosed()
	{
		GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
		this.State = GUIPDA.UIState.Closed;
	}

	public void ToggleHome()
	{
		if (this.cgHome.interactable)
		{
			GUITooltip2.SetToolTip(string.Empty, string.Empty, false);
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.Home;
		}
	}

	public void ToggleVizUI()
	{
		if (this.cgViz.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.Viz;
		}
		this.pdaVisualisers.AssembleUI();
	}

	public void CycleVizMode()
	{
		this.pdaVisualisers.Cycle();
	}

	public void ToggleNotesUI()
	{
		if (this.cgNotes.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
			this.pdaNotes.SaveApp();
		}
		else
		{
			this.State = GUIPDA.UIState.Notes;
		}
		this.pdaNotes.LoadApp();
	}

	public void ToggleTimerUI()
	{
		if (this.cgTimer.interactable)
		{
			this.State = GUIPDA.UIState.Closed;
		}
		else
		{
			this.State = GUIPDA.UIState.Timer;
		}
	}

	public void ToggleGig(bool bShow)
	{
		CanvasGroup component = base.transform.Find("pnlGigNexus/pnlGig").GetComponent<CanvasGroup>();
		if (bShow)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnPDAClick02", false, false);
			CanvasManager.ShowCanvasGroup(component);
		}
		else
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnPDAClick01", false, false);
			CanvasManager.HideCanvasGroup(component);
		}
	}

	private void ShowJobPaintUI(string btn)
	{
		this.State = GUIPDA.UIState.JobBuild;
		CrewSim.objInstance.FinishPaintingJob();
		IEnumerator enumerator = this.goJobTypes.transform.GetEnumerator();
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
		this.goJobTypes.transform.DetachChildren();
		if (btn == "actions")
		{
			GUIJobItem guijobItem = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobTypes.transform);
			guijobItem.SetData("CANC", "GUIActionCancel", delegate()
			{
				CrewSim.objInstance.StartPaintingJob(new JsonInstallable
				{
					strName = "Cancel"
				});
			});
			guijobItem = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobTypes.transform);
			guijobItem.SetData("UNIN", "GUIActionUninstall", delegate()
			{
				CrewSim.objInstance.StartPaintingJob(new JsonInstallable
				{
					strName = "Uninstall"
				});
			});
			guijobItem = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobTypes.transform);
			guijobItem.SetData("SCRA", "GUIActionScrap", delegate()
			{
				CrewSim.objInstance.StartPaintingJob(new JsonInstallable
				{
					strName = "Scrap"
				});
			});
			guijobItem = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobTypes.transform);
			guijobItem.SetData("REPR", "GUIActionRepair", delegate()
			{
				CrewSim.objInstance.StartPaintingJob(new JsonInstallable
				{
					strName = "Repair"
				});
			});
			guijobItem = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobTypes.transform);
			guijobItem.SetData("DISM", "GUIActionDismantle", delegate()
			{
				CrewSim.objInstance.StartPaintingJob(new JsonInstallable
				{
					strName = "Dismantle"
				});
			});
			guijobItem = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobTypes.transform);
			guijobItem.SetData("HAUL", "GUIActionHaul", delegate()
			{
				CrewSim.objInstance.StartPaintingJob(new JsonInstallable
				{
					strName = "Haul"
				});
			});
			CanvasManager.ShowCanvasGroup(this.cgJobFilters);
		}
		else
		{
			string[] array = new string[]
			{
				"HULL",
				"HVAC",
				"POWR",
				"SENS",
				"CTRL",
				"FURN",
				"APPS",
				"MISC"
			};
			string[] array2 = new string[]
			{
				"GUIBuildHull",
				"GUIBuildHVAC",
				"GUIBuildPower",
				"GUIBuildSensors",
				"GUIBuildControls",
				"GUIBuildFurniture",
				"GUIBuildAppliances",
				"GUIBuildOther"
			};
			for (int i = 0; i < array.Length; i++)
			{
				string strType = array[i];
				GUIJobItem guijobItem2 = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobTypes.transform);
				guijobItem2.SetData(array[i], array2[i], delegate()
				{
					this.ShowJobOptions(strType);
				});
			}
			CanvasManager.HideCanvasGroup(this.cgJobFilters);
		}
		EventSystem.current.SetSelectedGameObject(null);
	}

	public void HideJobPaintUI()
	{
		bool flag = CrewSim.objInstance.goSelPart != null || CrewSim.objInstance.goPaintJob != null;
		CrewSim.objInstance.FinishPaintingJob();
		AudioManager.am.PlayAudioEmitter("ShipUIBtnPDAClick01", false, false);
		if (flag)
		{
			return;
		}
		if (this.State == GUIPDA.UIState.JobBuild || this.State == GUIPDA.UIState.JobBuildScroll || this.State == GUIPDA.UIState.JobOrder)
		{
			this.State = GUIPDA.UIState.Closed;
		}
	}

	private void ShowJobOptions(string strType)
	{
		this.State = GUIPDA.UIState.JobBuildScroll;
		IEnumerator enumerator = this.goJobOptions.transform.GetEnumerator();
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
		foreach (JsonInstallable data in Installables.dictJobBuildOptionsListed[strType].Values)
		{
			GUIJobItem guijobItem = UnityEngine.Object.Instantiate<GUIJobItem>(this.prefabGUIJobItem, this.goJobOptions.transform);
			guijobItem.SetData(data);
		}
		EventSystem.current.SetSelectedGameObject(null);
		base.StartCoroutine(CrewSim.objInstance.ScrollTop(this.cgJobOptionsScroll.GetComponent<ScrollRect>()));
	}

	private void ShowSocials()
	{
		IEnumerator enumerator = this.tfpnlListContent.GetEnumerator();
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
		this.tfpnlListContent.DetachChildren();
		if (CrewSim.GetSelectedCrew() == null)
		{
			return;
		}
		GUISocialsRow component = Resources.Load<GameObject>("GUIShip/GUISocials/prefabSocialsRow").GetComponent<GUISocialsRow>();
		List<Relationship> list = GUIPDA.GetKnownSocialContacts(CrewSim.GetSelectedCrew());
		if (this._currentSortMethod == GUIPDA.SortMethod.AlphabeticalAscending)
		{
			list = (from x in list
			orderby x.pspec.strLastName
			select x).ToList<Relationship>();
		}
		else if (this._currentSortMethod == GUIPDA.SortMethod.AlphabeticalDescending)
		{
			list = (from x in list
			orderby x.pspec.strLastName descending
			select x).ToList<Relationship>();
		}
		else if (this._currentSortMethod == GUIPDA.SortMethod.RelationAscending)
		{
			list = (from x in list
			orderby x.fFamiliarity
			select x).ToList<Relationship>();
		}
		else
		{
			list = (from x in list
			orderby x.fFamiliarity descending
			select x).ToList<Relationship>();
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		foreach (Relationship relationship in list)
		{
			CondOwner co = relationship.pspec.GetCO();
			if (!this.FilterResult(relationship, co))
			{
				GUISocialsRow guisocialsRow = UnityEngine.Object.Instantiate<GUISocialsRow>(component, this.tfpnlListContent);
				guisocialsRow.SetContact(selectedCrew.socUs, relationship.pspec, co);
			}
		}
	}

	private bool FilterResult(Relationship rel, CondOwner coContact)
	{
		foreach (MultiSelectDTO multiSelectDTO in this._socialFilters)
		{
			if (multiSelectDTO.IsOn)
			{
				if (multiSelectDTO.Id == "IsDead" && coContact == null)
				{
					return true;
				}
				if (rel.aRelationships != null && rel.aRelationships.Contains(multiSelectDTO.Id))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void ScrollSocials(string strName)
	{
		if (this.State != GUIPDA.UIState.Socials || string.IsNullOrEmpty(strName))
		{
			return;
		}
		float num = 0f;
		float num2 = 1f;
		List<Relationship> knownSocialContacts = GUIPDA.GetKnownSocialContacts(CrewSim.GetSelectedCrew());
		if (knownSocialContacts.Count > 0)
		{
			num2 = (float)knownSocialContacts.Count;
		}
		foreach (Relationship relationship in knownSocialContacts)
		{
			if (relationship.pspec.FullName == strName)
			{
				break;
			}
			num += 1f;
		}
		float num3 = 1f - num / num2;
		if (num3 <= 1f / num2)
		{
			num3 = 0f;
		}
		ScrollRect component = base.transform.Find("pnlSocials/pnlList").GetComponent<ScrollRect>();
		base.StartCoroutine(CrewSim.objInstance.ScrollPos(component, num3));
	}

	public static List<Relationship> GetKnownSocialContacts(CondOwner coUs)
	{
		List<Relationship> list = new List<Relationship>();
		if (coUs == null || coUs.socUs == null)
		{
			return list;
		}
		foreach (Relationship relationship in coUs.socUs.GetAllPeople())
		{
			if (GUIPDA.CTSocials.TriggeredREL(relationship))
			{
				list.Insert(0, relationship);
			}
			else
			{
				list.Add(relationship);
			}
		}
		return list;
	}

	private void ShowGigNexus()
	{
		Transform transform = base.transform.Find("pnlGigNexus/pnlList/Viewport/pnlListContent");
		IEnumerator enumerator = transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform2 = (Transform)obj;
				UnityEngine.Object.Destroy(transform2.gameObject);
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
		transform.DetachChildren();
		if (GigManager.aJobs == null)
		{
			Debug.LogError("ERROR: GigManager aJobs is null.");
			return;
		}
		GigManager.GetJobs();
		GUIPDAGigRow component = Resources.Load<GameObject>("GUIShip/prefabGigNexusPDARow").GetComponent<GUIPDAGigRow>();
		bool flag = true;
		using (List<JsonJobSave>.Enumerator enumerator2 = GigManager.aJobs.GetEnumerator())
		{
			while (enumerator2.MoveNext())
			{
				GUIPDA.<ShowGigNexus>c__AnonStorey1 <ShowGigNexus>c__AnonStorey = new GUIPDA.<ShowGigNexus>c__AnonStorey1();
				<ShowGigNexus>c__AnonStorey.jjs = enumerator2.Current;
				<ShowGigNexus>c__AnonStorey.$this = this;
				if (<ShowGigNexus>c__AnonStorey.jjs.bTaken)
				{
					GUIPDAGigRow ggr = UnityEngine.Object.Instantiate<GUIPDAGigRow>(component, transform);
					ggr.SetJob(<ShowGigNexus>c__AnonStorey.jjs);
					ggr.onPointerClick.AddListener(delegate()
					{
						<ShowGigNexus>c__AnonStorey.$this.ShowGig(<ShowGigNexus>c__AnonStorey.jjs, ggr);
					});
					flag = false;
				}
			}
		}
		if (flag)
		{
			TMP_Text tmp_Text = base.transform.Find("pnlGigNexus/pnlGig/Viewport/txt").GetComponent<TMP_Text>();
			tmp_Text = UnityEngine.Object.Instantiate<TMP_Text>(tmp_Text, transform);
			tmp_Text.text = DataHandler.GetString("GUI_JOBSPDA_ROW_EMPTY1", false);
			tmp_Text = UnityEngine.Object.Instantiate<TMP_Text>(tmp_Text, transform);
			tmp_Text.text = DataHandler.GetString("GUI_JOBSPDA_ROW_EMPTY2", false);
		}
	}

	private void ShowGig(JsonJobSave jjs, GUIPDAGigRow ggr)
	{
		if (jjs == null)
		{
			return;
		}
		this.ToggleGig(true);
		TMP_Text component = base.transform.Find("pnlGigNexus/pnlGig/Viewport/txt").GetComponent<TMP_Text>();
		component.text = GUIJobs.GetDisplayGigText(jjs);
		base.StartCoroutine(CrewSim.objInstance.ScrollTop(base.transform.Find("pnlGigNexus/pnlGig").GetComponent<ScrollRect>()));
		if (ggr != null && ggr.coFocus != null)
		{
			CrewSim.objInstance.CamCenter(ggr.coFocus);
		}
	}

	private void ShowTasks()
	{
		Transform transform = base.transform.Find("pnlTasks/pnlList/Viewport/pnlListContent");
		IEnumerator enumerator = transform.GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				object obj = enumerator.Current;
				Transform transform2 = (Transform)obj;
				UnityEngine.Object.Destroy(transform2.gameObject);
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
		transform.DetachChildren();
		GUITaskRow component = Resources.Load<GameObject>("GUIShip/GUITaskList/pnlTaskRowPDA").GetComponent<GUITaskRow>();
		List<Task2> allTasks = CrewSim.objInstance.workManager.GetAllTasks();
		GUITaskRow guitaskRow = null;
		foreach (Task2 task in allTasks)
		{
			if (guitaskRow == null)
			{
				guitaskRow = UnityEngine.Object.Instantiate<GUITaskRow>(component, transform);
			}
			if (guitaskRow.SetTask(task))
			{
				guitaskRow = null;
			}
		}
		if (guitaskRow != null)
		{
			UnityEngine.Object.Destroy(guitaskRow.gameObject);
		}
	}

	public void AddTask(Task2 task)
	{
		if (task == null)
		{
			return;
		}
		if (this.State == GUIPDA.UIState.Tasks)
		{
			Transform transform = base.transform.Find("pnlTasks/pnlList/Viewport/pnlListContent");
			IEnumerator enumerator = transform.GetEnumerator();
			GUITaskRow guitaskRow;
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform2 = (Transform)obj;
					guitaskRow = transform2.GetComponent<GUITaskRow>();
					if (guitaskRow.Task == task)
					{
						guitaskRow.SetTask(task);
						return;
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
			GUITaskRow component = Resources.Load<GameObject>("GUIShip/GUITaskList/pnlTaskRowPDA").GetComponent<GUITaskRow>();
			guitaskRow = UnityEngine.Object.Instantiate<GUITaskRow>(component, transform);
			guitaskRow.SetTask(task);
		}
	}

	public void RemoveTask(Task2 task)
	{
		if (task == null)
		{
			return;
		}
		if (this.State == GUIPDA.UIState.Tasks)
		{
			Transform transform = base.transform.Find("pnlTasks/pnlList/Viewport/pnlListContent");
			IEnumerator enumerator = transform.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform2 = (Transform)obj;
					GUITaskRow component = transform2.GetComponent<GUITaskRow>();
					if (component.Task == task)
					{
						UnityEngine.Object.Destroy(transform2.gameObject);
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
	}

	public GUIPDA.UIState State
	{
		get
		{
			return this.nState;
		}
		set
		{
			if (value == this.nState)
			{
				return;
			}
			CanvasManager.HideCanvasGroup(this.cgJobs);
			CanvasManager.HideCanvasGroup(this.cgJobOptionsScroll);
			CanvasManager.HideCanvasGroup(this.cgObjectives);
			CanvasManager.HideCanvasGroup(this.cgTasks);
			CanvasManager.HideCanvasGroup(this.cgSocials);
			this.ddFilter.Reset();
			CanvasManager.HideCanvasGroup(this.cgGigNexus);
			CanvasManager.HideCanvasGroup(this.cgFerry);
			CanvasManager.HideCanvasGroup(this.cgHome);
			CanvasManager.HideCanvasGroup(this.cgViz);
			CanvasManager.HideCanvasGroup(this.cgNotes);
			CanvasManager.HideCanvasGroup(this.cgTimer);
			this.nState = value;
			if (this.nState == GUIPDA.UIState.Closed)
			{
				this.SlidePDA(1);
				return;
			}
			if (CrewSim.inventoryGUI.IsOpen)
			{
				CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
			}
			this.SlidePDA(0);
			switch (this.nState)
			{
			case GUIPDA.UIState.Home:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgHome);
				this.homepage.Activate();
				break;
			case GUIPDA.UIState.Objectives:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgObjectives);
				break;
			case GUIPDA.UIState.JobOrder:
				CanvasManager.ShowCanvasGroup(this.cgJobs);
				break;
			case GUIPDA.UIState.JobBuild:
				CanvasManager.ShowCanvasGroup(this.cgJobs);
				break;
			case GUIPDA.UIState.JobBuildScroll:
				CanvasManager.ShowCanvasGroup(this.cgJobs);
				CanvasManager.ShowCanvasGroup(this.cgJobOptionsScroll);
				break;
			case GUIPDA.UIState.Tasks:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgTasks);
				this.ShowTasks();
				break;
			case GUIPDA.UIState.Socials:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgSocials);
				this.ShowSocials();
				break;
			case GUIPDA.UIState.GigNexus:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgGigNexus);
				this.ShowGigNexus();
				this.ToggleGig(false);
				break;
			case GUIPDA.UIState.Ferry:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgFerry);
				this.uiFerry.Init();
				break;
			case GUIPDA.UIState.NavLink:
				this.ToggleNAVLink();
				this.State = GUIPDA.UIState.Closed;
				break;
			case GUIPDA.UIState.Viz:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgViz);
				break;
			case GUIPDA.UIState.Notes:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgNotes);
				break;
			case GUIPDA.UIState.Timer:
				this.HideJobPaintUI();
				CanvasManager.ShowCanvasGroup(this.cgTimer);
				break;
			}
		}
	}

	public int NewObjectives
	{
		get
		{
			return this.nNewObjectives;
		}
		set
		{
			this.nNewObjectives = value;
			this.UpdateAppNotifs();
		}
	}

	public int IdleCrew
	{
		get
		{
			return this.nIdleCrew;
		}
		set
		{
			this.nIdleCrew = value;
			this.UpdateAppNotifs();
		}
	}

	public bool JobsActive
	{
		get
		{
			return this.State == GUIPDA.UIState.JobBuild || this.State == GUIPDA.UIState.JobOrder;
		}
	}

	public static CondTrigger CTSocials
	{
		get
		{
			if (GUIPDA._ctSocials == null)
			{
				GUIPDA._ctSocials = DataHandler.GetCondTrigger("TRELFamiliar");
			}
			return GUIPDA._ctSocials;
		}
	}

	private CondTrigger CTPDA
	{
		get
		{
			if (this._ctPDA == null)
			{
				this._ctPDA = DataHandler.GetCondTrigger("TIsComputerPDA");
			}
			return this._ctPDA;
		}
	}

	public static GUIPDA instance;

	public static TMP_Text txtPDAUTC;

	public static CondTrigger ctJobFilter;

	private static CondTrigger _ctSocials;

	[SerializeField]
	private GUIPDAFerry uiFerry;

	public GameObject goJobTypes;

	public GameObject goJobOptions;

	private Toggle chkFilterWalls;

	private Toggle chkFilterFloors;

	private Toggle chkFilterConduits;

	private Toggle chkFilterEquip;

	private Toggle chkFilterCans;

	private Toggle chkFilterLoose;

	private CanvasGroup cgObjectives;

	private CanvasGroup cgTasks;

	private CanvasGroup cgGigNexus;

	private CanvasGroup cgFerry;

	private CanvasGroup cgJobs;

	private CanvasGroup cgJobOptionsScroll;

	private CanvasGroup cgJobFilters;

	private CanvasGroup cgHome;

	private CanvasGroup cgViz;

	public PDAVisualisers pdaVisualisers;

	private CanvasGroup cgNotes;

	public PDANotes pdaNotes;

	private CanvasGroup cgTimer;

	public PDATimer pdaTimer;

	private CanvasGroup cgHotbar;

	private ObjectivesApp objectivesApp;

	[SerializeField]
	private ScrollRect srSocials;

	[SerializeField]
	private Transform tfSocialsList;

	[SerializeField]
	private GUIJobItem prefabGUIJobItem;

	[SerializeField]
	public GUIPDAHomepage homepage;

	private CondTrigger _ctPDA;

	private int nNewObjectives;

	private int nIdleCrew;

	private GUIPDA.UIState nState;

	private double fLastTaskUpdate;

	[Header("PDA Frame Components")]
	[SerializeField]
	private GameObject bmpFrame;

	[SerializeField]
	private GameObject objBackground;

	[SerializeField]
	private GameObject objForeground;

	[SerializeField]
	private GameObject objHotbar;

	[SerializeField]
	private GameObject objNavigation;

	[SerializeField]
	private GameObject bmpScreen;

	[Header("Socials")]
	[SerializeField]
	private CanvasGroup cgSocials;

	[SerializeField]
	private Transform tfpnlListContent;

	[SerializeField]
	private Button btnSort;

	[SerializeField]
	private MultiSelectDropDown ddFilter;

	[SerializeField]
	private Sprite[] _filterSprites;

	private GUIPDA.SortMethod _currentSortMethod;

	private List<MultiSelectDTO> _socialFilters = new List<MultiSelectDTO>();

	public enum UIState
	{
		Loading,
		Closed,
		Home,
		Objectives,
		JobOrder,
		JobBuild,
		JobBuildScroll,
		Tasks,
		Socials,
		GigNexus,
		Ferry,
		NavLink,
		Viz,
		Notes,
		Timer
	}

	private enum SortMethod
	{
		AlphabeticalAscending,
		AlphabeticalDescending,
		RelationAscending,
		RelationDescending
	}
}
