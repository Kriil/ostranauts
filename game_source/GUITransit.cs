using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Events;
using Ostranauts.ShipGUIs.Transit;
using Ostranauts.ShipGUIs.Transit.Interfaces;
using Ostranauts.Ships;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// Transit kiosk UI. Handles station-to-station/ship transit, boarding flow,
// teleport placement, and moving the player/followers between ships.
public class GUITransit : GUIData, ITransitUI
{
	// Initializes the base GUI, pause behavior, and shared transit button event.
	private new void Awake()
	{
		base.Awake();
		this.bPausesGame = true;
		if (GUITransit.OnButtonPressed == null)
		{
			GUITransit.OnButtonPressed = new OnTransitButtonDownEvent();
		}
		GUITransit.OnButtonPressed.AddListener(new UnityAction<JsonTransitConnection>(this.OnButtonDown));
		this.goTransitLine.SetActive(false);
	}

	// Cleans up the shared transit button event listener.
	private void OnDestroy()
	{
		if (GUITransit.OnButtonPressed != null)
		{
			GUITransit.OnButtonPressed.RemoveAllListeners();
		}
		GUITransit.OnButtonPressed = null;
	}

	// Begins a transit if the destination is valid and different from the current ship.
	private void Go(JsonTransitConnection jDestination)
	{
		if (this.COSelf.ship.strRegID == jDestination.strTargetRegID || string.IsNullOrEmpty(jDestination.ctKioskDestination))
		{
			return;
		}
		MonoSingleton<AsyncShipLoader>.Instance.Unload(null);
		base.StartCoroutine(this._Go(jDestination));
	}

	// Main transit coroutine: loads the destination ship, moves the player/followers,
	// updates crime flags, and refreshes visibility/loading state.
	private IEnumerator _Go(JsonTransitConnection jDestination)
	{
		CondOwner coUser = this.COSelf.GetInteractionCurrent().objThem;
		if (coUser == null)
		{
			yield break;
		}
		CanvasManager.ShowCanvasGroup(this.cgBoarding);
		yield return new WaitForSecondsRealtime(0.1f);
		bool bPlayer = coUser == CrewSim.GetSelectedCrew();
		Ship.Loaded nLoad = Ship.Loaded.Full;
		if (!bPlayer)
		{
			nLoad = Ship.Loaded.Shallow;
		}
		Ship objShipNew = this.LoadShip(jDestination.strTargetRegID, nLoad);
		if (objShipNew == null)
		{
			yield break;
		}
		CrewSim.LowerUI(false);
		Ship objShipOld = coUser.ship;
		List<CondOwner> aFollows = new List<CondOwner>();
		if (bPlayer)
		{
			aFollows = coUser.GetFollowers();
		}
		CrewSim.MoveCO(coUser, objShipNew, false);
		if (bPlayer)
		{
			this.MoveChasingNpcs(coUser, jDestination, objShipOld);
			foreach (CondOwner objCO in aFollows)
			{
				CrewSim.MoveCO(objCO, objShipNew, false);
			}
		}
		aFollows.Add(coUser);
		this.MoveToTransitPoint(jDestination, aFollows);
		this.COSelf.AICancelCurrent();
		CrimeManager.ClearCrimeFlags(this.COSelf.ship.strLaw, aFollows);
		if (nLoad >= Ship.Loaded.Edit)
		{
			foreach (Ship ship in objShipOld.GetAllDockedShips())
			{
				if (ship.gameObject.activeInHierarchy)
				{
					CrewSim.objInstance.SaveToShallow(ship);
				}
			}
			CrewSim.objInstance.SaveToShallow(objShipOld);
			MonoSingleton<AsyncShipLoader>.Instance.LoadDockedBarterZoneShips(CrewSim.coPlayer);
			objShipNew.ToggleVis(true, true);
		}
		CrewSim.objInstance.CamCenter(CrewSim.GetSelectedCrew());
		yield break;
	}

	// Teleports the specified CondOwners to the destination kiosk trigger point.
	private void MoveToTransitPoint(JsonTransitConnection jsonTransit, List<CondOwner> cosToTeleport)
	{
		if (jsonTransit == null)
		{
			return;
		}
		Ship shipByRegID = CrewSim.system.GetShipByRegID(jsonTransit.strTargetRegID);
		if (shipByRegID.LoadState < Ship.Loaded.Edit)
		{
			return;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger(jsonTransit.ctKioskDestination);
		List<CondOwner> cos = shipByRegID.GetCOs(condTrigger, false, false, false);
		foreach (CondOwner condOwner in cos)
		{
			Vector2 pos = condOwner.GetPos("teleport", false);
			Tile tileAtWorldCoords = shipByRegID.GetTileAtWorldCoords1(pos.x, pos.y, true, true);
			if (!(tileAtWorldCoords == null))
			{
				foreach (CondOwner condOwner2 in cosToTeleport)
				{
					condOwner2.tf.position = new Vector3(pos.x, pos.y, condOwner2.tf.position.z);
					condOwner2.currentRoom = tileAtWorldCoords.room;
					Pathfinder pathfinder = condOwner2.Pathfinder;
					if (pathfinder != null)
					{
						pathfinder.tilCurrent = tileAtWorldCoords;
					}
				}
				break;
			}
		}
	}

	// If the player is being arrested, queues nearby arresting NPCs to transit too.
	private void MoveChasingNpcs(CondOwner coUser, JsonTransitConnection jsonTransit, Ship objShipOld)
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsCrimeArrest");
		if (!condTrigger.Triggered(coUser, null, true))
		{
			return;
		}
		List<CondOwner> list = new List<CondOwner>();
		List<CondOwner> people = objShipOld.GetPeople(false);
		CondTrigger condTrigger2 = DataHandler.GetCondTrigger("TIsAICrimeArresting");
		foreach (CondOwner condOwner in people)
		{
			if (!(condOwner == coUser))
			{
				if (condTrigger2.Triggered(condOwner, null, true))
				{
					list.Add(condOwner);
				}
			}
		}
		if (list.Count > 0)
		{
			CrewSim.objInstance.QueueForTransit(jsonTransit, list, new Action<JsonTransitConnection, List<CondOwner>>(this.MoveToTransitPoint));
		}
	}

	private Ship LoadShip(string strRegID, Ship.Loaded nLoad)
	{
		Ship ship = CrewSim.system.SpawnShip(strRegID, nLoad);
		if (ship == null)
		{
			return null;
		}
		if (nLoad >= Ship.Loaded.Edit)
		{
			CondOwnerVisitorCatchUp visitor = new CondOwnerVisitorCatchUp();
			ship.VisitCOs(visitor, true, true, true);
		}
		return ship;
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		this._jsonTransit = DataHandler.GetTransitConnections(coSelf.ship.strRegID);
		if (this._jsonTransit == null)
		{
			return;
		}
		this._connections = this._jsonTransit.GetConnectionsForKiosk(coSelf);
		this._transitUI = this.SpawnUIInstance(this._jsonTransit);
		this._transitUI.SetData(this._connections, coSelf);
	}

	private ITransitUI SpawnUIInstance(JsonTransit transitConnections)
	{
		if (string.IsNullOrEmpty(transitConnections.strCustomPrefabPathOptional))
		{
			return this;
		}
		GameObject gameObject = Resources.Load<GameObject>("GUIShip/GUITransit/" + transitConnections.strCustomPrefabPathOptional);
		if (gameObject != null)
		{
			GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, this.tfCustomPrefabParent);
			ITransitUI component = gameObject2.GetComponent<ITransitUI>();
			if (component != null)
			{
				return component;
			}
		}
		return this;
	}

	public void SetData(List<JsonTransitConnection> transitConnections, CondOwner coKiosk)
	{
		this.lblTitle.text = coKiosk.ship.publicName + " Transit";
		for (int i = 0; i < transitConnections.Count; i++)
		{
			JsonTransitConnection jsonTransitConnection = transitConnections[i];
			bool isEnabled = jsonTransitConnection.IsValidUser(CrewSim.coPlayer);
			GUITransitButton guitransitButton = UnityEngine.Object.Instantiate<GUITransitButton>(this.prefabBtnTransitStop, this.tfContent);
			guitransitButton.SetData(jsonTransitConnection, isEnabled, jsonTransitConnection.strTargetRegID == coKiosk.ship.strRegID);
			this.goButtons.Add(guitransitButton.gameObject);
		}
		this.GenerateMainPanelImage(transitConnections, coKiosk);
	}

	private void GenerateMainPanelImage(List<JsonTransitConnection> transitConnections, CondOwner coKiosk)
	{
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsTransitLift");
		bool flag = condTrigger.Triggered(coKiosk, null, true);
		if (flag)
		{
			int num = 0;
			for (int i = transitConnections.Count - 1; i >= 0; i--)
			{
				GUITransitStop guitransitStop = UnityEngine.Object.Instantiate<GUITransitStop>(this.prefabGUIElevatorStop, this.tfElevatorContainer);
				guitransitStop.SetData(transitConnections[num], i);
				num++;
			}
		}
		else
		{
			for (int j = transitConnections.Count - 1; j >= 0; j--)
			{
				GUITransitStop guitransitStop2 = UnityEngine.Object.Instantiate<GUITransitStop>(this.prefabGUITransitStop, this.tfTransitContainer);
				guitransitStop2.SetData(transitConnections[j], j);
			}
			this.goTransitLine.SetActive(true);
		}
	}

	private void OnButtonDown(JsonTransitConnection jDestination)
	{
		this.Go(jDestination);
	}

	public static OnTransitButtonDownEvent OnButtonPressed;

	[SerializeField]
	private TMP_Text lblTitle;

	[SerializeField]
	private CanvasGroup cgBoarding;

	[SerializeField]
	private Transform tfCustomPrefabParent;

	[Header("Button panel")]
	[SerializeField]
	private GUITransitButton prefabBtnTransitStop;

	[SerializeField]
	private Transform tfContent;

	[Header("Elevator")]
	[SerializeField]
	private GUITransitStop prefabGUIElevatorStop;

	[SerializeField]
	private Transform tfElevatorContainer;

	[Header("Transit")]
	[SerializeField]
	private GUITransitStop prefabGUITransitStop;

	[SerializeField]
	private Transform tfTransitContainer;

	[SerializeField]
	private GameObject goTransitLine;

	private JsonTransit _jsonTransit;

	private List<JsonTransitConnection> _connections;

	private ITransitUI _transitUI;

	private List<GameObject> goButtons = new List<GameObject>();
}
