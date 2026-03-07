using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Objectives;
using Ostranauts.Racing;
using Ostranauts.ShipGUIs.MFD;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Utils.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Vectrosity;

// Navigation/orbit display used by ship MFDs and PDA nav screens.
// This panel converts solar coordinates into UI space, draws bodies/ships,
// and appears to drive targeting, autopilot cues, and docking handoff UI.
public class GUIOrbitDraw : GUIData
{
	// Crosshair or manually selected nav point shared across orbit-draw instances.
	public static NavPOI CrossHairTarget { get; private set; }

	// Marks the PDA/nav-link variant of the display versus a ship console version.
	public bool IsPDANav { get; private set; }

	// Uses ship-specific prop overrides when present, otherwise falls back to the GUI defaults.
	public Dictionary<string, string> ShipPropMap
	{
		get
		{
			return this._shipPropMap ?? this.dictPropMap;
		}
	}

	// Reads the current follow-mode knob state from the GUI prop map.
	private int GetKnobFollowState
	{
		get
		{
			return base.GetPropMapData("nFollow", 0);
		}
	}

	// Reads the current reference-frame knob state.
	private int GetKnobRefState
	{
		get
		{
			return base.GetPropMapData("nRef", 0);
		}
	}

	// Reads the label-display mode for nav markers.
	private int GetKnobLabelsState
	{
		get
		{
			return base.GetPropMapData("nLabels", 3);
		}
	}

	// Likely decompilation damage here: this should probably map a direction input to a real keybind.
	public static KeyCode GetKCForDirection(char c)
	{
		return KeyCode.Exclaim;
	}

	// Projects solar-system coordinates into the current canvas transform.
	public void SolarToCanvas(double sx, double sy, out double cx, out double cy)
	{
		double num = sx + this.dOffsetSX;
		double num2 = sy + this.dOffsetSY;
		cx = num * this.dCanvasSolarXX - num2 * this.dCanvasSolarXY;
		cy = num * this.dCanvasSolarXY + num2 * this.dCanvasSolarXX;
	}

	// Inverse projection from canvas space back into solar coordinates.
	private void CanvasToSolar(double cx, double cy, out double sx, out double sy)
	{
		double num = cx * this.dCanvasSolarXX + cy * this.dCanvasSolarXY;
		double num2 = -cx * this.dCanvasSolarXY + cy * this.dCanvasSolarXX;
		double num3 = this.dCanvasSolarXX * this.dCanvasSolarXX + this.dCanvasSolarXY * this.dCanvasSolarXY;
		sx = num / num3 - this.dOffsetSX;
		sy = num2 / num3 - this.dOffsetSY;
	}

	// Convenience wrapper for nav targets that already know their solar coordinates.
	private void GetCanvasXY(NavPOI navPOI, out double cx, out double cy)
	{
		double sx;
		double sy;
		navPOI.GetSXY(out sx, out sy);
		this.SolarToCanvas(sx, sy, out cx, out cy);
	}

	// Caches render targets, hooks nav events, and initializes shared draw state.
	protected override void Awake()
	{
		base.Awake();
		if (GUIOrbitDraw.NavModMessageEvent == null)
		{
			GUIOrbitDraw.NavModMessageEvent = new NavModMessageEvent();
		}
		GUIOrbitDraw.NavModMessageEvent.AddListener(new UnityAction<NavModMessageType, object>(this.OnNavModMessage));
		if (GUIOrbitDraw.UpdateShipSelection == null)
		{
			GUIOrbitDraw.UpdateShipSelection = new UpdateShipSelectionEvent();
		}
		GUIOrbitDraw.UpdateShipSelection.AddListener(new UnityAction<string>(this.LockTarget));
		if (CrewSim.coPlayer != null)
		{
			this._playerOwnedShips = CrewSim.system.GetShipsForOwner(CrewSim.coPlayer.strID);
			this._hasNotSeenRefuelingTutorial = !CrewSim.coPlayer.HasCond("TutorialRefuelStart");
		}
		this.follow = new NavPOI(0.0, 0.0);
		GUIOrbitDraw.CrossHairTarget = new NavPOI(0.0, 0.0);
		this.autoPilotDest = new NavPOI(0.0, 0.0);
		this.aUIs = new List<VectorLine>();
		this.aShipDraws = new List<ShipDraw>();
		this.aBODraws = new List<BODraw>();
		this.objSSEngage = new ShipSitu();
		if (this.btnNote != null)
		{
			this.btnNote.onClick.AddListener(new UnityAction(this.ToggleNote));
			this.btnNote.transform.Find("bmp").GetComponent<RawImage>().texture = DataHandler.LoadPNG("manuals/DON'T CRASH/000.png", false, false);
			this.btnNote.transform.Find("bmp").GetComponent<RawImage>().texture.filterMode = FilterMode.Bilinear;
		}
		this.cgClampWarning = GUIRenderTargets.goThis.transform.Find("CanvasOrbitDraw/goLines/ClampDisengageWarning").GetComponent<CanvasGroup>();
		this.tfPanelIn = base.transform.Find("pnlInside");
		CanvasManager.HideCanvasGroup(this.tfPanelIn.GetComponent<CanvasGroup>());
		if (this.btnRescue != null)
		{
			this.btnRescue.onClick.AddListener(new UnityAction(this.ToggleInnerPanel));
		}
		if (this.btnDone != null)
		{
			this.btnDone.onClick.AddListener(new UnityAction(this.ToggleInnerPanel));
		}
		if (this.btnScrew01 != null)
		{
			this.btnScrew01.onClick.AddListener(new UnityAction(this.ToggleInnerPanel));
		}
		if (this.btnScrew02 != null)
		{
			this.btnScrew02.onClick.AddListener(new UnityAction(this.ToggleInnerPanel));
		}
		if (this.btnScrew03 != null)
		{
			this.btnScrew03.onClick.AddListener(new UnityAction(this.ToggleInnerPanel));
		}
		if (this.btnScrew04 != null)
		{
			this.btnScrew04.onClick.AddListener(new UnityAction(this.ToggleInnerPanel));
		}
		Button component = base.transform.Find("btnDockArrow").GetComponent<Button>();
		component.onClick.AddListener(delegate()
		{
			CrewSim.SwitchUI("strGUIPrefabRight");
		});
		this.ledWLock = base.transform.Find("Controls/Container/prefabPnlWASD/bmpKeyW/LedWLock").GetComponent<GUILamp>();
		if (this.ledWLock != null)
		{
			this.ledWLock.State = 0;
		}
		this.chkStationKeeping.isOn = false;
		this.chkStationKeeping.onValueChanged.AddListener(new UnityAction<bool>(this.ToggleStationKeeping));
		this.txtTimeUTC = GUIRenderTargets.goLines.transform.Find("pnlTitle/txtTime").GetComponent<Text>();
		this.tfOrbitLabel = Resources.Load<Transform>("GUIShip/lblOrbit");
		this.ddTravel = this.tfPanelIn.Find("DebugFastTravel/Dropdown").GetComponent<TMP_Dropdown>();
		this.btnTravel = this.tfPanelIn.Find("DebugFastTravel/TravelButton").GetComponent<Button>();
		this.btnTravel.onClick.AddListener(new UnityAction(this.OnTravelClick));
		AudioManager.AddBtnAudio(this.btnTravel.gameObject, "ShipUIBtnNSInstaDockIn", "ShipUIBtnNSInstaDockOut");
		this.goOrbitPanel = GUIRenderTargets.goLines.transform.Find("pnlOrbits").gameObject;
		this.txtRange = GUIRenderTargets.goLines.transform.Find("pnlOrbits/txtRange").GetComponent<TMP_Text>();
		this.chkNavMode = base.transform.Find("chkNavMode").GetComponent<Toggle>();
		this.ToggleNavMode(false);
		this.chkNavMode.onValueChanged.AddListener(delegate(bool A_1)
		{
			this.ToggleNavMode(this.chkNavMode.isOn);
		});
		this.rectDrawPanel = this.goOrbitPanel.GetComponent<RectTransform>();
		Camera component2 = GUIRenderTargets.goLines.transform.parent.parent.Find("CameraOrbitDraw").GetComponent<Camera>();
		this.vCanvasOffset = new Vector3(this.rectDrawPanel.rect.width * this.rectDrawPanel.lossyScale.x, this.rectDrawPanel.rect.height * this.rectDrawPanel.lossyScale.y, 0f);
		this.rectDisplayPanel = base.transform.Find("bmpDisplay01").GetComponent<RectTransform>();
		this.rtLines = GUIRenderTargets.goLines.GetComponent<RectTransform>();
		this.cgStatus = GUIRenderTargets.goLines.transform.Find("pnlStatus").GetComponent<CanvasGroup>();
		this.srLog = this.cgStatus.transform.Find("pnlLog").GetComponent<ScrollRect>();
		this.cgNag = GUIRenderTargets.goLines.transform.Find("pnlNag").GetComponent<CanvasGroup>();
		TMP_Text component3 = this.cgStatus.transform.Find("txtTitle").GetComponent<TMP_Text>();
		component3.text = DataHandler.GetString("GUI_ORBIT_NAG_TITLE", false);
		component3 = this.cgNag.transform.Find("txtDesc").GetComponent<TMP_Text>();
		component3.text = DataHandler.GetString("GUI_ORBIT_NAG_DESC", false);
		this.txtNagTimer = this.cgNag.transform.Find("txtDots").GetComponent<TMP_Text>();
		this.InitOrbitArea(this.goOrbitPanel);
		this.InitTitleArea(GUIRenderTargets.goLines.transform.Find("pnlTitle").gameObject);
		this.InitSideArea(GUIRenderTargets.goLines.transform.Find("pnlSide").gameObject);
		this.InitFrame(GUIRenderTargets.goLines.transform.Find("pnlFrame").gameObject);
		base.SetPropMapData("bRCS", "true");
		base.SetPropMapData("bTorchSafety", "true");
		base.SetPropMapData("bTorchSafetyCovered", "true");
		base.SetPropMapData("bCycleSafetyCovered", "true");
		base.SetPropMapData("nProjSteps", this.nProjSteps.ToString());
		base.SetPropMapData("nFollow", "1");
		base.SetPropMapData("nLabels", "3");
		base.SetPropMapData("fTimeRate", this.fTimeFuture.ToString());
		base.SetPropMapData("shipPOR", string.Empty);
		base.SetPropMapData("boPOR", string.Empty);
		base.SetPropMapData("objSSEngage.fA", this.objSSEngage.fA.ToString());
		base.SetPropMapData("objSSEngage.fW", this.objSSEngage.fW.ToString());
		base.SetPropMapData("objSSEngage.fRot", this.objSSEngage.fRot.ToString());
		base.SetPropMapData("objSSEngage.vPosx", this.objSSEngage.vPosx.ToString());
		base.SetPropMapData("objSSEngage.vPosy", this.objSSEngage.vPosy.ToString());
		base.SetPropMapData("objSSEngage.vVel.x", this.objSSEngage.vVelX.ToString());
		base.SetPropMapData("objSSEngage.vVel.y", this.objSSEngage.vVelY.ToString());
		base.SetPropMapData("objSSEngage.vAccEx.x", this.objSSEngage.vAccEx.x.ToString());
		base.SetPropMapData("objSSEngage.vAccEx.y", this.objSSEngage.vAccEx.y.ToString());
		base.SetPropMapData("objSSEngage.vAccIn.x", this.objSSEngage.vAccIn.x.ToString());
		base.SetPropMapData("objSSEngage.vAccIn.y", this.objSSEngage.vAccIn.y.ToString());
		base.SetPropMapData("bShowNWZ", "false");
		this.dCanvasSolarXX = 0.5;
		this.dCanvasSolarXY = 0.0;
		this.dOffsetSX = 0.0;
		this.dOffsetSY = 0.0;
		this.dEpoch = 0.0;
		this.dOffsetSX = (double)(this.rectDrawPanel.rect.width * 0.5f) / this.dCanvasSolarXX;
		this.dOffsetSY = (double)(this.rectDrawPanel.rect.height * 0.5f) / this.dCanvasSolarXX;
		this.ssTemp = new ShipSitu();
		this.UpdateWASDCluster();
	}

	private void OnDestroy()
	{
		if (GUIOrbitDraw.NavModMessageEvent != null)
		{
			GUIOrbitDraw.NavModMessageEvent.RemoveListener(new UnityAction<NavModMessageType, object>(this.OnNavModMessage));
		}
		GUIOrbitDraw.Instance = null;
	}

	private void OnNavModMessage(NavModMessageType messageType, object arg)
	{
		if (messageType != NavModMessageType.UpdateUI)
		{
			if (messageType == NavModMessageType.WarnClampEngaged)
			{
				this.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_CLAMP", false);
			}
		}
		else
		{
			this.UpdateUI();
		}
	}

	private VectorLine GetLineBodyForShip(Ship objShip, ShipDraw sd, bool known)
	{
		bool flag = this._playerOwnedShips.Contains(objShip.strRegID);
		if (objShip.IsStation(false) || objShip.Classification == Ship.TypeClassification.Waypoint)
		{
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(objShip.objSS, StarSystem.fEpoch, false);
			bool flag2 = false;
			if (nearestBO != null)
			{
				flag2 = (nearestBO.GravRadius > objShip.objSS.GetDistance(nearestBO.dXReal, nearestBO.dYReal));
			}
			Color c = (!flag) ? GUIOrbitDraw.clrWhite01 : GUIOrbitDraw.clrBlue01;
			List<Vector2> aVerts;
			switch (objShip.Classification)
			{
			case Ship.TypeClassification.OrbitalStation:
				aVerts = NavIcon.OrbitalStation(sd.fRadiusM);
				break;
			case Ship.TypeClassification.OrbitalStationUnfinished:
				aVerts = NavIcon.OrbitalStationUnfinished(sd.fRadiusM);
				c = GUIOrbitDraw.clrWhite02;
				break;
			case Ship.TypeClassification.GroundStation:
				aVerts = NavIcon.GroundStation(sd.fRadiusM);
				break;
			case Ship.TypeClassification.GroundStationUnfinished:
				aVerts = NavIcon.GroundStationUnfinished(sd.fRadiusM);
				c = GUIOrbitDraw.clrWhite02;
				break;
			case Ship.TypeClassification.Buoy:
			case Ship.TypeClassification.Outpost:
				aVerts = NavIcon.Outpost(sd.fRadiusM);
				break;
			case Ship.TypeClassification.Waypoint:
				c = RacingLeagueManager.ColorWaypoint;
				aVerts = NavIcon.Circle(sd.fRadiusM);
				break;
			default:
				aVerts = ((!flag2 && objShip.Classification != Ship.TypeClassification.GroundStation) ? NavIcon.OrbitalStation(sd.fRadiusM) : NavIcon.GroundStation(sd.fRadiusM));
				break;
			}
			if (objShip.IsUnderConstruction)
			{
				if (!known)
				{
					aVerts = NavIcon.Asterisk();
				}
				return NavIcon.SetupVectorLine(objShip.strRegID, GUIOrbitDraw.clrWhite02, this.goOrbitPanel, aVerts);
			}
			return NavIcon.SetupVectorLine(objShip.strRegID, c, this.goOrbitPanel, aVerts);
		}
		else
		{
			if (!known && objShip.IsDerelict())
			{
				Color c2 = (!flag) ? GUIOrbitDraw.clrWhite02 : GUIOrbitDraw.clrBlue01;
				VectorLine vectorLine = NavIcon.SetupVectorLine(objShip.strRegID, c2, this.goOrbitPanel, NavIcon.Asterisk());
				vectorLine.lineType = LineType.Discrete;
				return vectorLine;
			}
			VectorLine vectorLine2;
			if (objShip.DMGStatus == Ship.Damage.Derelict)
			{
				Color c3 = (!flag) ? GUIOrbitDraw.clrWhite02 : GUIOrbitDraw.clrBlue01;
				vectorLine2 = NavIcon.SetupVectorLine(objShip.strRegID, c3, this.goOrbitPanel, NavIcon.Ship(sd.fRadiusM));
				vectorLine2.lineType = LineType.Discrete;
			}
			else
			{
				AIType shipType = AIShipManager.GetShipType(objShip);
				Color c4;
				if (flag)
				{
					c4 = GUIOrbitDraw.clrBlue01;
				}
				else if (objShip.IsLocalAuthority)
				{
					c4 = GUIOrbitDraw.clrLocalAuthority;
				}
				else if (shipType == AIType.HaulerDeployer || shipType == AIType.HaulerRetriever || shipType == AIType.HaulerCargo)
				{
					c4 = GUIOrbitDraw.clrHauler;
				}
				else
				{
					c4 = ((objShip.GetPeople(false).Count != 0) ? GUIOrbitDraw.clrOrange01 : this.clrOrange01Half);
				}
				if (objShip.IsFlyingDark())
				{
					c4 = GUIOrbitDraw.clrWhite02;
				}
				if (shipType == AIType.Pirate && objShip.fLastVisit != 0.0)
				{
					c4 = this.clrRed01;
				}
				List<Vector2> aVerts2 = (!objShip.IsUsingTorchDrive) ? NavIcon.Ship(sd.fRadiusM) : NavIcon.ShipActiveTorch(sd.fRadiusM);
				vectorLine2 = NavIcon.SetupVectorLine(objShip.strRegID, c4, this.goOrbitPanel, aVerts2);
				vectorLine2.lineType = LineType.Discrete;
			}
			sd.silhouetteDrawPoints = NavIcon.GetSilhouette(objShip, sd.fRadiusM);
			return vectorLine2;
		}
	}

	private ShipDraw SetupShipDraw(Ship objShip)
	{
		if (objShip == null)
		{
			Debug.Log("ERROR: No ship provided for SetupShipDraw!");
			Debug.Break();
			return null;
		}
		ShipDraw shipDraw = new ShipDraw(objShip);
		ShipInfo shipInfo = ShipInfo.GetShipInfo(this.GetNavStationShip(), shipDraw.ship, this.ShipPropMap);
		shipDraw.lineBody = this.GetLineBodyForShip(objShip, shipDraw, shipInfo.Known || objShip.IsLocalAuthority);
		if (objShip.IsStation(false) && !objShip.IsNotAFullStation)
		{
			shipDraw.lineNoWakeRange = this.CreateBodyLine(objShip.strRegID + "_NWZ", this.clrOrange02Half, 2.005376E-06f, this.goOrbitPanel);
		}
		shipDraw.DoNotRotate = (!shipInfo.Known || shipDraw.ship.IsStation(false));
		Color c;
		if (objShip == this.GetNavStationShip())
		{
			this.sdNS = shipDraw;
			for (int i = 0; i < this.nProjStepsSelf; i++)
			{
				c = shipDraw.lineBody.color;
				c.a = 1f - 1f * (float)i / (float)this.nProjStepsSelf;
				VectorLine lineBodyForShip = this.GetLineBodyForShip(objShip, this.sdNS, true);
				lineBodyForShip.color = c;
				this.sdNS.AddProjection(lineBodyForShip);
			}
		}
		else
		{
			for (int j = 0; j < this.nProjSteps; j++)
			{
				c = shipDraw.lineBody.color;
				c.a = 1f - 1f * (float)j / (float)this.nProjSteps;
				VectorLine lineBodyForShip2 = this.GetLineBodyForShip(objShip, shipDraw, shipInfo.Known || objShip.IsLocalAuthority);
				lineBodyForShip2.color = c;
				shipDraw.AddProjection(lineBodyForShip2);
			}
		}
		if (shipDraw.ship.ShipCO.HasCond("IsTutorialDerelict"))
		{
			shipInfo.isTutorialDerelict = true;
			shipDraw.sDisplayName = "(TUTORIAL DERELICT)";
			shipDraw.sDisplayRegID = "(TUTORIAL DERELICT)";
		}
		shipDraw.SetShipInfo(shipInfo);
		shipDraw.SetupLabel(this.tfOrbitLabel, this.goOrbitPanel.transform);
		shipDraw.linePath = new VectorLine(objShip.strRegID + "-Path", new List<Vector2>(), GUIOrbitDraw.fLineWidth, LineType.Continuous, Joins.Weld);
		c = shipDraw.lineBody.color;
		c.a /= 2f;
		shipDraw.linePath.color = c;
		shipDraw.linePath.SetCanvas(this.goOrbitPanel, false);
		return shipDraw;
	}

	private ShipDraw FindShipDraw(Ship objShip)
	{
		return this.FindShipDraw(objShip.strRegID);
	}

	private ShipDraw FindShipDraw(string regId)
	{
		foreach (ShipDraw shipDraw in this.aShipDraws)
		{
			if (shipDraw != null && shipDraw.ship != null)
			{
				if (shipDraw.ship.strRegID == regId)
				{
					return shipDraw;
				}
			}
		}
		return null;
	}

	private Ship GetNavStationShip()
	{
		return this.COSelf.ship;
	}

	private ShipSitu GetNavStationShipSitu()
	{
		return this.GetNavStationShip().objSS;
	}

	public static void TriggerShipRedraw(string regId)
	{
		if (GUIOrbitDraw.Instance == null)
		{
			return;
		}
		GUIOrbitDraw.Instance.InvalidateShipDraw(regId);
	}

	public static void RemoveBODraw(string boName)
	{
		if (GUIOrbitDraw.Instance == null)
		{
			return;
		}
		GUIOrbitDraw.Instance.RemoveOrbital(boName);
	}

	public static void AddDebugDraw(string name, ShipSitu situ, Color color, bool isPrediction = false)
	{
		if (GUIOrbitDraw.Instance == null || GUIOrbitDraw.Instance.dictPropMap == null)
		{
			return;
		}
		GUIOrbitDraw.Instance.SetupDebugDraw(name, situ, color, isPrediction);
	}

	public static void AddDebugDraw(string name, Point pos)
	{
		ShipSitu shipSitu = new ShipSitu
		{
			vPosx = pos.X,
			vPosy = pos.Y
		};
		shipSitu.LockToBO(-1.0, false);
		GUIOrbitDraw.AddDebugDraw(name, shipSitu, false, null);
	}

	public static void AddDebugDraw(string name, ShipSitu situ, bool isPrediction = false, string reg = null)
	{
		Color color = Color.cyan;
		if (reg != null)
		{
			int num = 0;
			foreach (char c in reg)
			{
				num += (int)c;
			}
			int num2 = num % GUIOrbitDraw.colorArray.Length;
			color = GUIOrbitDraw.colorArray[num2];
		}
		GUIOrbitDraw.AddDebugDraw(name, situ, color, isPrediction);
	}

	private void SetupDebugDraw(string drawName, ShipSitu situ, Color color, bool isPrediction)
	{
		foreach (DebugDraw debugDraw in this.aDebugDraws)
		{
			if (debugDraw._displayName == drawName)
			{
				debugDraw.MarkForRemoval();
			}
		}
		DebugDraw debugDraw2 = new DebugDraw(drawName, situ, isPrediction, NavIcon.SetupVectorLine(drawName, color, this.goOrbitPanel, NavIcon.Diamond((float)DebugDraw.SIZE)));
		debugDraw2.SetupLabel(this.tfOrbitLabel, this.goOrbitPanel);
		this.aDebugDraws.Add(debugDraw2);
	}

	public static void ClearDebugDrawsForRegId(string regId)
	{
		if (GUIOrbitDraw.Instance == null || GUIOrbitDraw.Instance.dictPropMap == null)
		{
			return;
		}
		GUIOrbitDraw.Instance.ClearDebugDraws(regId);
	}

	public void ClearDebugDraws(string regId = null)
	{
		foreach (DebugDraw debugDraw in this.aDebugDraws)
		{
			if (string.IsNullOrEmpty(regId) || debugDraw._displayName.Contains(regId))
			{
				debugDraw.MarkForRemoval();
			}
		}
	}

	private bool VisibleFromNavStation(Ship objShip)
	{
		if (objShip == null || objShip.bDestroyed)
		{
			return false;
		}
		if (objShip.IsStation(false) && !objShip.IsUnderConstruction)
		{
			return true;
		}
		if (objShip == this.GetNavStationShip())
		{
			return true;
		}
		if (objShip.HideFromSystem)
		{
			return false;
		}
		double num = this.GetNavStationShipSitu().GetRangeTo(objShip.objSS) * 149597872.0;
		float num2 = Mathf.Min(this.GetNavStationShip().fVisibilityRangeMod, objShip.fVisibilityRangeMod);
		if (objShip.bFusionReactorRunning && num > 1500000000.0 * (double)num2)
		{
			return false;
		}
		if (objShip.IsDerelict() && num > 2000.0 * (double)num2)
		{
			return false;
		}
		if (objShip.IsUnderConstruction)
		{
			ShipInfo shipInfo = ShipInfo.GetShipInfo(this.GetNavStationShip(), objShip, this.ShipPropMap);
			if (shipInfo != null && shipInfo.Known)
			{
				return true;
			}
			if (num > 2000.0 * (double)num2)
			{
				return false;
			}
		}
		return (!objShip.IsActive() || objShip.bFusionReactorRunning || num <= 20000.0 * (double)num2) && (this.boMainOccluder == null || !StarSystem.IsLOSBlockedByBO(this.boMainOccluder.bo, this.COSelf.ship, objShip));
	}

	public void FlashNWZCircle()
	{
		if (this._nwzRoutine != null)
		{
			base.StopCoroutine(this._nwzRoutine);
		}
		this._nwzRoutine = base.StartCoroutine(this._FlashNWZCircle());
	}

	private IEnumerator _FlashNWZCircle()
	{
		bool oldValue = base.GetPropMapData("bShowNWZ", false);
		for (int i = 3; i >= 0; i--)
		{
			this.bShowNWZ = true;
			yield return new WaitForSecondsRealtime(0.2f);
			this.bShowNWZ = false;
			yield return new WaitForSecondsRealtime(0.2f);
		}
		this.bShowNWZ = true;
		yield return new WaitForSecondsRealtime(1.5f);
		base.SetPropMapData("bShowNWZ", oldValue.ToString());
		this.bShowNWZ = oldValue;
		this._nwzRoutine = null;
		yield break;
	}

	private void LoadSystem(StarSystem ss)
	{
		this.objSystem = ss;
		foreach (KeyValuePair<string, BodyOrbit> keyValuePair in this.objSystem.aBOs)
		{
			if (keyValuePair.Value.nDrawFlagsBody != 1 || keyValuePair.Value.nDrawFlagsTrack != 1)
			{
				this.AddOrbital(keyValuePair.Value, this.goOrbitPanel);
			}
		}
		this.UpdateShipDraw();
	}

	private void PanCanvasImmediateS(double sdx, double sdy)
	{
		this.dOffsetSX += sdx;
		this.dOffsetSY += sdy;
	}

	private void PanCanvasImmediateC(double cdx, double cdy)
	{
		double num;
		double num2;
		this.CanvasToSolar(0.0, 0.0, out num, out num2);
		double num3;
		double num4;
		this.CanvasToSolar(cdx, cdy, out num3, out num4);
		this.PanCanvasImmediateS(num - num3, num2 - num4);
	}

	public void ResetCrosshair()
	{
		GUIOrbitDraw.CrossHairTarget.fOffsetSX = 0.0;
		GUIOrbitDraw.CrossHairTarget.fOffsetSY = 0.0;
		AudioManager.am.PlayAudioEmitter("ShipUINSMapPan04", false, true);
	}

	public void MoveCrosshair(double cdx, double cdy)
	{
		double num;
		double num2;
		this.CanvasToSolar(0.0, 0.0, out num, out num2);
		double num3;
		double num4;
		this.CanvasToSolar(cdx, cdy, out num3, out num4);
		GUIOrbitDraw.CrossHairTarget.fOffsetSX -= num - num3;
		GUIOrbitDraw.CrossHairTarget.fOffsetSY -= num2 - num4;
		AudioManager.am.PlayAudioEmitter("ShipUINSMapPan04", false, true);
	}

	private float GetDeltaTime()
	{
		float num = Math.Min(Time.unscaledDeltaTime, 0.06666667f);
		float num2 = 0.016666668f;
		if (0.9f * num2 < num && num < 1.1f * num2)
		{
			num = num2;
		}
		return num;
	}

	public void ResetTime()
	{
		this.dEpoch = StarSystem.fEpoch;
		this.fTimeFuture = 1f;
		this.fTimeFutureTarget = 1f;
	}

	private void UpdateTime()
	{
		if (this.fTimeFutureTarget == 0f || this.fTimeFuture == 0f)
		{
			this.ResetTime();
		}
		float num = Mathf.Log(this.fTimeFuture);
		float num2 = Mathf.Log(this.fTimeFutureTarget);
		float num3 = 0.9f;
		num = num * num3 + num2 * (1f - num3);
		this.fTimeFuture = Mathf.Exp(num);
		if (this.fTimeFuture < 1.02f)
		{
			this.dEpoch = StarSystem.fEpoch;
		}
		else
		{
			this.dEpoch = StarSystem.fEpoch + (double)this.fTimeFuture;
		}
		if (this.follow.bodyOrbit != null)
		{
			double num4 = this.dEpoch - StarSystem.fEpoch;
			num4 %= this.follow.bodyOrbit.fPeriod;
			this.dEpoch = StarSystem.fEpoch + num4;
		}
		this.txtTimeUTC.text = StarSystem.sUTCEpoch + "\n" + MathUtils.GetUTCFromS(this.dEpoch - StarSystem.fEpoch);
		if (this.objSystem != null)
		{
			if (this.dEpoch < StarSystem.fEpoch)
			{
				this.dEpoch = StarSystem.fEpoch;
			}
			this.sb.Length = 0;
			this.sb.AppendLine(StarSystem.sUTCEpoch);
			this.sb.Append(MathUtils.GetUTCFromS(this.dEpoch - StarSystem.fEpoch));
			this.txtTimeUTC.text = this.sb.ToString();
		}
		if (GUIOrbitDraw.CrossHairTarget.fTargetFuture > 0.0)
		{
			GUIOrbitDraw.CrossHairTarget.fTargetFuture = this.dEpoch - StarSystem.fEpoch;
		}
	}

	private void MoveTowards(ref float x, float dir)
	{
		if (x * dir < 0f)
		{
			x *= 0.5f;
		}
		x += dir;
	}

	private float GetDeltaVRemaining(bool bAllowDocked)
	{
		if (!bAllowDocked)
		{
			return (float)this.GetNavStationShip().DeltaVRemainingRCS;
		}
		double num = this.GetNavStationShip().Mass;
		foreach (Ship ship in this.GetNavStationShip().GetAllDockedShips())
		{
			num += ship.Mass;
		}
		double num2 = this.GetNavStationShip().DeltaVRemainingRCS * this.GetNavStationShip().Mass / num;
		return (float)num2;
	}

	private float GetRCSReactionMass()
	{
		return (float)this.GetNavStationShip().GetRCSRemain();
	}

	private float GetPowerConnected()
	{
		if (this.COSelf == null)
		{
			return 0f;
		}
		Powered component = this.COSelf.GetComponent<Powered>();
		if (component == null)
		{
			return 0f;
		}
		return (float)component.PowerConnected;
	}

	private void KeyboardInput(float delX, float delY)
	{
		if (this.COSelf.HasCond("IsDamagedSoftware"))
		{
			return;
		}
		if (this.GetKnobFollowState > 0)
		{
			double num;
			double num2;
			this.CanvasToSolar(0.0, 0.0, out num, out num2);
			double num3;
			double num4;
			this.CanvasToSolar((double)delX, (double)delY, out num3, out num4);
			float num5 = 0.2f;
			double num6 = (num3 - num) * (double)num5;
			double num7 = (num4 - num2) * (double)num5;
			this.dFollowOffsetSX += num6;
			this.dFollowOffsetSY += num7;
			this.oldFollowCX += (double)(delX * num5);
			this.oldFollowCY += (double)(delY * num5);
			return;
		}
		if ((double)delX != 0.0)
		{
			this.MoveTowards(ref this.fVelocityX, delX);
		}
		if ((double)delY != 0.0)
		{
			this.MoveTowards(ref this.fVelocityY, delY);
		}
	}

	private void Update()
	{
		if (this.cgNag.alpha > 0f)
		{
			if (StarSystem.fEpoch < this.fEpochNagEnd)
			{
				string text = string.Empty;
				for (double num = this.fEpochNagEnd - StarSystem.fEpoch; num > 0.0; num -= 1.0)
				{
					text += ".";
				}
				this.txtNagTimer.text = text;
				return;
			}
			CanvasManager.HideCanvasGroup(this.cgNag);
		}
		if (!this.bActive)
		{
			this.StopMapAudio();
			return;
		}
		if (this.COSelf == null || this.COSelf.HasCond("IsOff"))
		{
			CrewSim.LowerUI(false);
			return;
		}
		if (GUIOrbitDraw.bUpdateWASD)
		{
			this.UpdateWASDCluster();
		}
		this.MouseHandler();
		this.KeyHandler();
		if (this.chkStationKeeping.isOn && this.COSelf.ship != null)
		{
			AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.COSelf.ship.strRegID);
			if (aishipByRegID == null || aishipByRegID.ActiveCommandName != "HoldStationAutoPilot")
			{
				this.chkStationKeeping.isOn = false;
			}
		}
		if (!this.follow.IsShipOrOrbit())
		{
			this.SetOldFollow();
		}
		this.UpdateTime();
		this.UpdateShipDraw();
		float num2 = 15f * this.GetDeltaTime();
		float num3 = 7f * this.GetDeltaTime();
		if (!this.bRCS)
		{
			if (GUIActionKeySelector.commandShipCCW.Held || this.dictWASD["Q"].bPressed)
			{
				this.MoveTowards(ref this.fVelocityYaw, num2);
			}
			if (GUIActionKeySelector.commandShipCW.Held || this.dictWASD["E"].bPressed)
			{
				this.MoveTowards(ref this.fVelocityYaw, -num2);
			}
		}
		if (GUIActionKeySelector.commandZoomIn.Held || this.dictWASD["+"].bPressed)
		{
			this.fVelocityZ += num3;
		}
		if (GUIActionKeySelector.commandZoomOut.Held || this.dictWASD["-"].bPressed)
		{
			this.fVelocityZ -= num3;
		}
		if (GUIOrbitDraw.OnZoom != null)
		{
			GUIOrbitDraw.OnZoom(this.dScopeRadius);
		}
		float num4 = Mathf.Cos(this.fVelocityYaw * this.GetDeltaTime()) * Mathf.Exp(this.fVelocityZ * this.GetDeltaTime());
		float num5 = Mathf.Sin(this.fVelocityYaw * this.GetDeltaTime()) * Mathf.Exp(this.fVelocityZ * this.GetDeltaTime());
		double num6 = this.dCanvasSolarXX * (double)num4 - this.dCanvasSolarXY * (double)num5;
		double num7 = this.dCanvasSolarXX * (double)num5 + this.dCanvasSolarXY * (double)num4;
		this.dCanvasSolarXX = num6;
		this.dCanvasSolarXY = num7;
		float num8 = 1500f * this.GetDeltaTime();
		if (!this.bRCS)
		{
			if (GUIActionKeySelector.commandFlyLeft.Held || this.dictWASD["A"].bPressed)
			{
				this.KeyboardInput(-num8, 0f);
			}
			if (GUIActionKeySelector.commandFlyRight.Held || this.dictWASD["D"].bPressed)
			{
				this.KeyboardInput(num8, 0f);
			}
			if (GUIActionKeySelector.commandFlyUp.Held || this.dictWASD["W"].bPressed)
			{
				this.KeyboardInput(0f, num8);
			}
			if (GUIActionKeySelector.commandFlyDown.Held || this.dictWASD["S"].bPressed)
			{
				this.KeyboardInput(0f, -num8);
			}
		}
		this.fZoomTimer -= this.GetDeltaTime();
		if (0f < this.fZoomTimer)
		{
			float num9 = (float)(this.dCanvasSolarXX * this.dCanvasSolarXX + this.dCanvasSolarXY * this.dCanvasSolarXY);
			float num10 = num9 * Mathf.Exp(this.fVelocityZ * 2f / 8f);
			float num11 = Mathf.Log((float)this.dMagTarget / num10);
			this.fVelocityZ += num11 * 15f * this.GetDeltaTime();
			if (float.IsPositiveInfinity(this.fVelocityZ))
			{
				this.fVelocityZ = 100f;
			}
			else if (float.IsNegativeInfinity(this.fVelocityZ))
			{
				this.fVelocityZ = -100f;
			}
		}
		if (this.GetKnobRefState > 0 && this.GetKnobFollowState > 0)
		{
			double num12 = 0.0;
			double num13 = 0.0;
			double sx = 0.0;
			double sy = 0.0;
			this.follow.UpdateTime(this.dEpoch);
			double num14;
			double num15;
			this.follow.GetSXY(out num14, out num15);
			this.follow.GetParentSXY(this.dEpoch, out sx, out sy);
			this.SolarToCanvas(sx, sy, out num12, out num13);
			double num16;
			double num17;
			this.SolarToCanvas(num14, num15, out num16, out num17);
			double num18 = (double)Mathf.Atan2((float)(num16 - num12), (float)(num13 - num17));
			num18 *= -0.4000000059604645;
			num4 = Mathf.Cos((float)num18);
			num5 = Mathf.Sin((float)num18);
			num6 = this.dCanvasSolarXX * (double)num4 - this.dCanvasSolarXY * (double)num5;
			num7 = this.dCanvasSolarXX * (double)num5 + this.dCanvasSolarXY * (double)num4;
			this.dCanvasSolarXX = num6;
			this.dCanvasSolarXY = num7;
			double num19;
			double num20;
			this.CanvasToSolar(num16, num17, out num19, out num20);
			this.dOffsetSX -= num14 - num19;
			this.dOffsetSY -= num15 - num20;
		}
		this.follow.UpdateTime(this.dEpoch);
		double num21;
		double num22;
		this.follow.GetSXY(out num21, out num22);
		num21 += this.dFollowOffsetSX;
		num22 += this.dFollowOffsetSY;
		double num23;
		double num24;
		this.SolarToCanvas(num21, num22, out num23, out num24);
		this.PanCanvasImmediateC(num23 - this.oldFollowCX, num24 - this.oldFollowCY);
		if (this.GetKnobFollowState > 0)
		{
			double num25;
			double num26;
			this.follow.GetSXY(out num25, out num26);
			num25 += this.dFollowOffsetSX;
			num26 += this.dFollowOffsetSY;
			double num27;
			double num28;
			this.SolarToCanvas(num25, num26, out num27, out num28);
			float num29 = 0.2f;
			double num30 = (double)(this.rectDrawPanel.rect.width * 0.5f + this.fVelocityX * num29);
			double num31 = (double)(this.rectDrawPanel.rect.height * 0.5f + this.fVelocityY * num29);
			if (this.GetKnobRefState > 0)
			{
				num31 -= (double)(this.rectDrawPanel.rect.height * 0.25f);
			}
			double num32 = (num27 - num30) * (double)this.GetDeltaTime() * 25.0;
			double num33 = (num28 - num31) * (double)this.GetDeltaTime() * 25.0;
			this.fVelocityX += (float)num32;
			this.fVelocityY += (float)num33;
		}
		this.PanCanvasImmediateC((double)(this.fVelocityX * this.GetDeltaTime()), (double)(this.fVelocityY * this.GetDeltaTime()));
		this.SetOldFollow();
		double num34;
		double num35;
		this.CanvasToSolar(0.0, 0.0, out num34, out num35);
		double num36;
		double num37;
		this.CanvasToSolar((double)(this.rectDrawPanel.rect.width * 0.5f), 0.0, out num36, out num37);
		this.dScopeRadius = (double)((float)MathUtils.GetMagnitude(num36 - num34, num37 - num35));
		this.fVelocityX *= Mathf.Exp(-5f * this.GetDeltaTime());
		this.fVelocityY *= Mathf.Exp(-5f * this.GetDeltaTime());
		this.fVelocityZ *= Mathf.Exp(-8f * this.GetDeltaTime());
		this.fVelocityYaw *= Mathf.Exp(-8f * this.GetDeltaTime());
		if (this.cgStatus.alpha != 1f)
		{
			if (Mathf.Abs(this.fVelocityX) > 10f || Mathf.Abs(this.fVelocityY) > 10f)
			{
				AudioManager.am.PlayAudioEmitter("ShipUINSMapPan01", true, false);
			}
			else
			{
				AudioManager.am.StopAudioEmitter("ShipUINSMapPan01");
			}
			if (Mathf.Abs(this.fVelocityZ) > 0.1f || Mathf.Abs(this.fVelocityYaw) > 0.1f)
			{
				AudioManager.am.PlayAudioEmitter("ShipUINSMapPan03", true, false);
			}
			else
			{
				AudioManager.am.StopAudioEmitter("ShipUINSMapPan03");
			}
		}
		else
		{
			this.StopMapAudio();
		}
		this.DrawSystem();
		this.UpdateUIs();
	}

	private void StopMapAudio()
	{
		AudioManager.am.StopAudioEmitter("ShipUINSMapPan01");
		AudioManager.am.StopAudioEmitter("ShipUINSMapPan02");
		AudioManager.am.StopAudioEmitter("ShipUINSMapPan03");
	}

	public void SetOldFollow()
	{
		this.follow.UpdateTime(this.dEpoch);
		double num;
		double num2;
		this.follow.GetSXY(out num, out num2);
		num += this.dFollowOffsetSX;
		num2 += this.dFollowOffsetSY;
		this.SolarToCanvas(num, num2, out this.oldFollowCX, out this.oldFollowCY);
	}

	private void UpdateUIs()
	{
		GUIOrbitDraw.NavModMessageEvent.Invoke(NavModMessageType.UpdateUI, null);
		bool flag = (int)Time.realtimeSinceStartup % 2 == 0;
		for (int i = 0; i < this.aUIs.Count; i++)
		{
			this.aUIs[i].Draw();
		}
		this.txtRange.text = "ZOOM RANGE: " + MathUtils.GetDistUnits(this.dScopeRadius);
		bool flag2 = this.COSelf.ship.IsDocked();
		this.sb.Length = 0;
		BodyOrbit bodyOrbit = null;
		if (this.COSelf.ship.objSS != null)
		{
			bodyOrbit = CrewSim.system.GetBO(this.COSelf.ship.objSS.strBOPORShip);
			if (bodyOrbit != null && bodyOrbit.IsPlaceholder())
			{
				if (bodyOrbit.boParent != null)
				{
					bodyOrbit = bodyOrbit.boParent;
				}
				else
				{
					bodyOrbit = null;
				}
			}
		}
		if (bodyOrbit == null)
		{
			bodyOrbit = CrewSim.system.GetBO("Sol");
		}
		this.SetPointOfReferenceUI(ref this.sb, flag, flag2, bodyOrbit);
		this.sb.AppendLine("CURRENT TRG");
		int num = 0;
		if (GUIOrbitDraw.CrossHairTarget.Ship != null && !GUIOrbitDraw.CrossHairTarget.Ship.bDestroyed)
		{
			if (GUIOrbitDraw.crossHairInfo == null || GUIOrbitDraw.crossHairInfo._strRegID != GUIOrbitDraw.CrossHairTarget.name)
			{
				GUIOrbitDraw.crossHairInfo = ShipInfo.GetShipInfo(this.GetNavStationShip(), GUIOrbitDraw.CrossHairTarget.Ship, this.ShipPropMap);
			}
			this.sb.Append("<color=orange>");
			if (GUIOrbitDraw.crossHairInfo.strRegID != string.Empty)
			{
				this.sb.AppendLine("I: " + GUIOrbitDraw.crossHairInfo.strRegID);
			}
			if (GUIOrbitDraw.crossHairInfo.publicName != string.Empty)
			{
				this.sb.AppendLine("N: " + GUIOrbitDraw.crossHairInfo.publicName);
				int num2 = Mathf.FloorToInt((float)GUIOrbitDraw.crossHairInfo.publicName.Length / 13f);
				num += num2;
			}
			if (GUIOrbitDraw.crossHairInfo.designation != string.Empty)
			{
				this.sb.AppendLine("C: " + GUIOrbitDraw.crossHairInfo.designation);
			}
			string text = string.Concat(new string[]
			{
				GUIOrbitDraw.crossHairInfo.year,
				" ",
				GUIOrbitDraw.crossHairInfo.make,
				" ",
				GUIOrbitDraw.crossHairInfo.model
			});
			if (text.Length > 2)
			{
				this.sb.AppendLine("M: " + text);
				int num3 = Mathf.FloorToInt((float)text.Length / 13f);
				num += num3;
			}
			if (GUIOrbitDraw.crossHairInfo.dimensions != string.Empty)
			{
				this.sb.AppendLine("D: " + GUIOrbitDraw.crossHairInfo.dimensions);
				int num4 = Mathf.FloorToInt((float)GUIOrbitDraw.crossHairInfo.dimensions.Length / 13f);
				num += num4;
			}
			this.sb.Append("</color>");
			if (!this.VisibleFromNavStation(GUIOrbitDraw.CrossHairTarget.Ship))
			{
				double num5;
				double num6;
				GUIOrbitDraw.CrossHairTarget.GetSXY(out num5, out num6);
				GUIOrbitDraw.CrossHairTarget = new NavPOI(num5, num6);
				AudioManager.am.PlayAudioEmitter("ShipUINSMapPan04", false, false);
			}
		}
		else
		{
			this.sb.AppendLine("I: " + GUIOrbitDraw.CrossHairTarget.name);
			this.sb.AppendLine();
			this.sb.AppendLine();
			this.sb.AppendLine();
			this.sb.AppendLine();
		}
		for (int j = num; j <= 2; j++)
		{
			this.sb.AppendLine();
		}
		if (GUIOrbitDraw.CrossHairTarget.IsShipOrOrbit())
		{
			double num5;
			double num6;
			GUIOrbitDraw.CrossHairTarget.GetSXY(out num5, out num6);
			double num7;
			double num8;
			GUIOrbitDraw.CrossHairTarget.GetVSXY(out num7, out num8);
			this.sb.Append("VREL ");
			this.fVRel = MathUtils.GetDistance(num7, num8, this.COSelf.ship.objSS.vVelX, this.COSelf.ship.objSS.vVelY);
			this.sb.Append(MathUtils.GetDistUnits(this.fVRel));
			this.sb.AppendLine("/s");
			float num9 = (float)(num5 - this.COSelf.ship.objSS.vPosx);
			float num10 = (float)(num6 - this.COSelf.ship.objSS.vPosy);
			double num11 = num7 - this.COSelf.ship.objSS.vVelX;
			double num12 = num8 - this.COSelf.ship.objSS.vVelY;
			MathUtils.SetLength(ref num9, ref num10, 1f);
			this.sb.Append("VCRS ");
			double num13 = (double)num9 * num12 - (double)num10 * num11;
			if (Math.Abs(num13) < 6.684586911775981E-14)
			{
				num13 = 0.0;
			}
			this.sb.Append(MathUtils.GetDistUnits(num13));
			this.sb.AppendLine("/s");
			num13 = (double)Mathf.Abs((float)((double)num9 * num12 - (double)num10 * num11));
			Color color = this.clrText;
			if (GUIOrbitDraw.CrossHairTarget.Ship != null)
			{
				this.dRNG = CollisionManager.GetRangeToCollisionAU(this.COSelf.ship, GUIOrbitDraw.CrossHairTarget.Ship);
			}
			else
			{
				this.dRNG = this.COSelf.ship.objSS.GetDistance(num5, num6);
				this.dRNG -= CollisionManager.GetCollisionDistanceAU(this.COSelf.ship, GUIOrbitDraw.CrossHairTarget.bodyOrbit);
			}
			if (this.dRNG < 1E-15)
			{
				this.dRNG = 0.0;
			}
			if (this.dRNG <= 3.342293553032505E-08)
			{
				color = ((!flag) ? GUIOrbitDraw.clrOrange01 : this.clrRed01);
			}
			this.sb.Append(MathUtils.ColorToColorTag(color));
			this.sb.Append("RNG ");
			this.sb.Append(MathUtils.GetDistUnits(this.dRNG));
			this.sb.AppendLine("</color>");
			float num14 = Mathf.Cos(-this.COSelf.ship.objSS.fRot);
			float num15 = Mathf.Sin(-this.COSelf.ship.objSS.fRot);
			float y = num9 * num14 - num10 * num15;
			float x = num9 * num15 + num10 * num14;
			this.fBRG = Mathf.Atan2(y, x) * 57.295776f;
			this.fBRG = MathUtils.NormalizeAngleDegrees(this.fBRG);
			this.sb.Append("BRG " + this.fBRG.ToString("#.00") + "°");
			this.sb.AppendLine();
			double num16 = 1.0;
			if (Vector2.Dot(new Vector2((float)num11, (float)num12), new Vector2(num9, num10)) > 0f)
			{
				num16 = -1.0;
			}
			bool propMapData = base.GetPropMapData("chkEngage", false);
			double num17;
			if (propMapData && this.COSelf.ship.objSS.HasNavData())
			{
				num17 = this.COSelf.ship.objSS.NavData.GetArrivalEpoch() - StarSystem.fEpoch;
			}
			else
			{
				num17 = num16 * this.dRNG / this.fVRel;
			}
			if (num17 < 0.0 || Math.Abs(this.fVRel) < 6.684586911775981E-14)
			{
				this.sb.Append("ETA -");
			}
			else if (num17 > 87658.125)
			{
				this.sb.Append("ETA " + MathUtils.GetDurationFromS(num17, 1));
			}
			else
			{
				this.sb.Append("ETA " + MathUtils.GetDurationFromS(num17, 4));
			}
		}
		else
		{
			double num5;
			double num6;
			GUIOrbitDraw.CrossHairTarget.GetSXY(out num5, out num6);
			Color color2 = this.clrText;
			this.dRNG = (double)((float)this.COSelf.ship.objSS.GetDistance(num5, num6));
			if (this.dRNG <= 3.342293553032505E-08)
			{
				color2 = ((!flag) ? GUIOrbitDraw.clrOrange01 : this.clrRed01);
			}
			this.sb.Append(MathUtils.ColorToColorTag(color2));
			this.sb.AppendLine("VREL -");
			this.fVRel = 0.0;
			this.sb.AppendLine("VCRS -");
			this.sb.Append("RNG ");
			this.sb.Append(MathUtils.GetDistUnits(this.dRNG));
			this.sb.AppendLine("</color>");
			this.sb.AppendLine("BRG -");
			this.sb.Append("ETA -");
		}
		this.sb.AppendLine();
		float deltaVRemaining = this.GetDeltaVRemaining(true);
		this.fRemass = this.GetRCSReactionMass();
		Color color3 = this.clrText;
		float num18 = this.fRemass / this.fRCSMax * 100f;
		this.sb.AppendLine();
		if (num18 < 25f)
		{
			if (flag)
			{
				this.sb.AppendLine("FUEL: " + MathUtils.ColorToColorTag(this.clrRed01) + "LOW</color>");
			}
			else
			{
				this.sb.AppendLine("FUEL: " + MathUtils.ColorToColorTag(GUIOrbitDraw.clrOrange01) + "LOW</color>");
			}
		}
		else
		{
			this.sb.AppendLine("FUEL: ");
		}
		int num19 = Mathf.CeilToInt(num18 / 10f);
		string text2 = string.Empty;
		for (int k = 1; k <= 10; k++)
		{
			text2 += ((k <= num19) ? "+" : "_");
		}
		this.sb.Append("RCS: ");
		Color color4 = this.clrGreen01;
		if (num18 < 50f)
		{
			color4 = GUIOrbitDraw.clrOrange01;
		}
		else if (num18 < 25f)
		{
			color4 = this.clrRed01;
		}
		this.sb.AppendLine("[" + MathUtils.ColorToColorTag(color4) + text2 + "</color>] ");
		if (flag)
		{
			this.sb.AppendLine(num18.ToString("N1") + "%");
		}
		else
		{
			this.sb.AppendLine(this.fRemass.ToString("#.00 kg"));
		}
		this.sb.Append("</color>");
		if ((double)deltaVRemaining <= this.fVRel && !flag2)
		{
			color3 = ((!flag) ? GUIOrbitDraw.clrOrange01 : this.clrRed01);
		}
		this.sb.Append(MathUtils.ColorToColorTag(color3));
		this.sb.AppendLine("DELTA-V:");
		this.sb.AppendLine(MathUtils.GetDistUnits((double)deltaVRemaining) + "/s");
		this.sb.Append("</color>");
		double num20 = (double)this.GetPowerConnected();
		Color color5 = this.clrText;
		this.sb.AppendLine();
		if (num20 <= 20.0)
		{
			color5 = ((!flag) ? GUIOrbitDraw.clrOrange01 : this.clrRed01);
		}
		this.sb.Append(MathUtils.ColorToColorTag(color5));
		this.sb.AppendLine("PWR RESERVE");
		this.sb.Append(num20.ToString("#.00 kWh"));
		this.sb.Append("</color>");
		this.txtSide.text = this.sb.ToString();
		if (this.COSelf.ship != null)
		{
			Vector2 vIn = this.objSystem.GetGravAccel(bodyOrbit, this.GetNavStationShipSitu());
			this.lineGrav.points2 = new List<Vector2>();
			this.lineCourse.points2 = new List<Vector2>();
			if (this.COSelf.ship.objSS.HasNavData())
			{
				this.lineCourse.points2 = this.COSelf.ship.objSS.NavData.GetPoints(this);
			}
			else if (this.navPlan != null)
			{
				this.lineCourse.points2 = this.navPlan.GetPoints(this);
			}
			VectorLine vectorLine = this.lineCourse;
			vectorLine.matrix = Matrix4x4.Scale(Vector3.one);
			vectorLine.Draw();
			this.lineGrav.points2.Add(new Vector2(this.sdNS.fCanvasX, this.sdNS.fCanvasY));
			vIn = MathUtils.NormalizeVector(vIn);
			double num21;
			double num22;
			this.SolarToCanvas(this.sdNS.ship.objSS.vPosx + (double)vIn.x, this.sdNS.ship.objSS.vPosy + (double)vIn.y, out num21, out num22);
			this.lineGrav.points2.Add(new Vector2((float)num21, (float)num22));
			vectorLine = this.lineGrav;
			vectorLine.matrix = Matrix4x4.Scale(Vector3.one);
			vectorLine.Draw();
		}
		this.DrawCrossHair();
		this.DrawStationKeepingCrossHair();
		if (this.fClampDisengageWarningTimer > 0f)
		{
			this.fClampDisengageWarningTimer -= CrewSim.TimeElapsedUnscaled();
			if (this.fClampDisengageWarningTimer <= 0f)
			{
				this.cgClampWarning.alpha = 0f;
			}
		}
	}

	private void SetPointOfReferenceUI(ref StringBuilder sb, bool bBlink, bool docked, BodyOrbit bo)
	{
		bool flag = this.chkStationKeeping.isOn && this.COSelf.ship != null && this.COSelf.ship.shipStationKeepingTarget != null && !this.COSelf.ship.shipStationKeepingTarget.bDestroyed && !this.COSelf.ship.shipStationKeepingTarget.HideFromSystem;
		sb.AppendLine("Point of reference:");
		if (flag)
		{
			ShipDraw shipDraw = this.FindShipDraw(this.COSelf.ship.shipStationKeepingTarget);
			bool flag2 = shipDraw != null && shipDraw.shipInfo != null && shipDraw.shipInfo.Known;
			sb.AppendLine((!flag2) ? "?" : this.COSelf.ship.shipStationKeepingTarget.strRegID);
			sb.Append("VREL: ");
			this.fVRel = MathUtils.GetMagnitude(this.COSelf.ship.shipStationKeepingTarget.objSS.vVel, this.COSelf.ship.objSS.vVel);
			sb.Append(MathUtils.GetDistUnits(this.fVRel));
			sb.AppendLine("/s");
			Color color = this.clrText;
			this.dRNG = (double)((float)this.COSelf.ship.objSS.GetDistance(this.COSelf.ship.shipStationKeepingTarget.objSS));
			float collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(this.COSelf.ship, this.COSelf.ship.shipStationKeepingTarget);
			this.dRNG -= (double)collisionDistanceAU;
			if (this.dRNG < 1E-15)
			{
				this.dRNG = 0.0;
			}
			if (this.dRNG <= 3.342293553032505E-08 && !docked)
			{
				color = ((!bBlink) ? GUIOrbitDraw.clrOrange01 : this.clrRed01);
			}
			sb.Append(MathUtils.ColorToColorTag(color));
			sb.Append("RNG ");
			sb.Append(MathUtils.GetDistUnits(this.dRNG));
		}
		else
		{
			sb.AppendLine(bo.strName);
			sb.Append("VREL: ");
			this.fVRel = MathUtils.GetDistance(bo.dVelX, bo.dVelY, this.COSelf.ship.objSS.vVelX, this.COSelf.ship.objSS.vVelY);
			sb.Append(MathUtils.GetDistUnits(this.fVRel));
			sb.AppendLine("/s");
			Color color2 = this.clrText;
			this.dRNG = (double)((float)MathUtils.GetDistance(bo.dXReal, bo.dYReal, this.COSelf.ship.objSS.vPosx, this.COSelf.ship.objSS.vPosy));
			double collisionDistanceAU2 = CollisionManager.GetCollisionDistanceAU(this.COSelf.ship, bo);
			this.dRNG -= collisionDistanceAU2;
			this.dRNG = Math.Max(this.dRNG, 0.0);
			if (this.dRNG <= 3.342293553032505E-08 && !docked)
			{
				color2 = ((!bBlink) ? GUIOrbitDraw.clrOrange01 : this.clrRed01);
			}
			sb.Append(MathUtils.ColorToColorTag(color2));
			sb.Append("RNG ");
			sb.Append(MathUtils.GetDistUnits(this.dRNG));
		}
		sb.AppendLine("</color>");
		sb.AppendLine();
	}

	public void FlashStationWarn()
	{
		this.fEpochStationBegin = StarSystem.fEpoch;
		this.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_NWZ", false);
	}

	public void FlashCycleSafety()
	{
		this.fEpochCycleSafetyBegin = StarSystem.fEpoch;
		this.CGClampWarning("GUI_ORBIT_WARN_CYCLE_SAFETY", false);
	}

	private void DrawCrossHair()
	{
		double sx;
		double sy;
		GUIOrbitDraw.CrossHairTarget.GetSXY(out sx, out sy);
		double num = Math.Atan2(this.dCanvasSolarXX, this.dCanvasSolarXY);
		float num2 = (float)(0.004 / MathUtils.GetMagnitude(this.dCanvasSolarXX, this.dCanvasSolarXY));
		float num3 = 6.684587E-12f;
		float fScale = num3 + num2;
		this.DrawVectorLine(this.lineCross, sx, sy, (float)num, fScale);
	}

	private void DrawStationKeepingCrossHair()
	{
		if ((!this.chkStationKeeping.isOn && this.lineStationKeepingTarget != null) || this.COSelf.ship.shipStationKeepingTarget == null)
		{
			VectorLine.Destroy(ref this.lineStationKeepingTarget);
		}
		else
		{
			if (this.lineStationKeepingTarget == null)
			{
				this.lineStationKeepingTarget = new VectorLine("Station Keeping", NavIcon.BracketSquared((float)this.COSelf.ship.shipStationKeepingTarget.objSS.Size * 1.5f), GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
				this.lineStationKeepingTarget.color = this.clrGreen01;
				this.lineStationKeepingTarget.SetCanvas(this.goOrbitPanel, false);
			}
			ShipDraw shipDraw = this.FindShipDraw(this.COSelf.ship.shipStationKeepingTarget);
			if (shipDraw == null)
			{
				return;
			}
			NavPOI navPOI = new NavPOI(shipDraw, this.GetNavStationShip(), this.ShipPropMap);
			double sx;
			double sy;
			navPOI.GetSXY(out sx, out sy);
			double num = Math.Atan2(this.dCanvasSolarXX, this.dCanvasSolarXY);
			num += 1.5707963705062866;
			float num2 = (float)(0.004 / MathUtils.GetMagnitude(this.dCanvasSolarXX, this.dCanvasSolarXY));
			float num3 = 6.684587E-12f;
			float fScale = num3 + num2;
			this.DrawVectorLine(this.lineStationKeepingTarget, sx, sy, (float)num, fScale);
		}
	}

	private void UpdateShipDraw()
	{
		double num = 100000000.0;
		foreach (BODraw bodraw in this.aBODraws)
		{
			if (bodraw.bo.fRadiusKM >= 5.0)
			{
				double distance = this.COSelf.ship.objSS.GetDistance(bodraw.bo.dXReal, bodraw.bo.dYReal);
				if (num > distance)
				{
					num = distance;
					this.boMainOccluder = bodraw;
				}
			}
		}
		for (int i = this.aShipDraws.Count - 1; i >= 0; i--)
		{
			if (this.aShipDraws[i].ship.bChangedStatus || this.aShipDraws[i].ship.bDestroyed || !this.VisibleFromNavStation(this.aShipDraws[i].ship) || (this.aShipDraws[i].ship == this.COSelf.ship && (this.COSelf.ship.strXPDR == null || this.COSelf.ship.strXPDR == "?")))
			{
				this.aShipDraws[i].ship.bChangedStatus = false;
				ShipDraw shipDraw = this.aShipDraws[i];
				this.aShipDraws.RemoveAt(i);
				shipDraw.Destroy();
			}
		}
		if (this.aDebugDraws != null)
		{
			for (int j = this.aDebugDraws.Count - 1; j >= 0; j--)
			{
				if (this.aDebugDraws[j] == null || this.aDebugDraws[j].Evaluate())
				{
					this.aDebugDraws.RemoveAt(j);
				}
			}
		}
		ShipDraw.blink = false;
		ShipDraw.KnobState = this.GetKnobLabelsState;
		ShipDraw.ActiveLabel = ShipDraw.KnobState;
		if (ShipDraw.KnobState == 3)
		{
			double num2 = StarSystem.fEpoch % 10.0;
			if (num2 < 4.7)
			{
				ShipDraw.blink = false;
				ShipDraw.ActiveLabel = 2;
			}
			else if (num2 < 5.0)
			{
				ShipDraw.blink = true;
				ShipDraw.ActiveLabel = 0;
			}
			else if (num2 < 9.7)
			{
				ShipDraw.blink = false;
				ShipDraw.ActiveLabel = 1;
			}
			else
			{
				ShipDraw.blink = true;
				ShipDraw.ActiveLabel = 0;
			}
		}
		foreach (KeyValuePair<string, Ship> keyValuePair in this.objSystem.dictShips)
		{
			if (!keyValuePair.Value.bDestroyed)
			{
				if (keyValuePair.Value != this.COSelf.ship)
				{
					if (keyValuePair.Value.IsStationHidden(false) || keyValuePair.Value.IsSubStation() || !this.VisibleFromNavStation(keyValuePair.Value))
					{
						continue;
					}
				}
				ShipDraw shipDraw2 = this.FindShipDraw(keyValuePair.Value);
				if (shipDraw2 != null)
				{
					shipDraw2.ToggleStatusSymbol();
				}
				else
				{
					shipDraw2 = this.SetupShipDraw(keyValuePair.Value);
					this.aShipDraws.Add(shipDraw2);
				}
			}
		}
	}

	private void DrawSystem()
	{
		this.boundsAddedThisFrame.Clear();
		this.boundsToRects.Clear();
		this.VisibleShipDraws.Clear();
		this.OverlappingShipDraws.Clear();
		double num = this.dEpoch - StarSystem.fEpoch;
		this.ShowProjections(num > 0.0);
		foreach (BODraw bodraw in this.aBODraws)
		{
			if (bodraw.bo.nDrawFlagsTrack != 1)
			{
				bodraw.bo.UpdateTime(this.dEpoch, true, false);
				this.DrawOrbitTrack(bodraw, num > 0.0);
			}
			if (bodraw.bo.nDrawFlagsBody != 1)
			{
				bodraw.bo.UpdateTime(StarSystem.fEpoch, true, false);
				this.DrawBody(bodraw, -1, 12f);
			}
			if (num > 0.0)
			{
				if (bodraw.bo.nDrawFlagsBody != 1)
				{
					bodraw.bo.UpdateTime(this.dEpoch, true, false);
					this.DrawBody(bodraw, 0, 12f);
				}
			}
		}
		BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this.GetNavStationShip().objSS, StarSystem.fEpoch, false);
		double dVelX = nearestBO.dVelX;
		double dVelY = nearestBO.dVelY;
		float num2 = -(float)Math.Atan2(this.dCanvasSolarXY, this.dCanvasSolarXX);
		foreach (ShipDraw shipDraw in this.aShipDraws)
		{
			bool showSilhouette = this.dScopeRadius * 149597872.0 < 2.5;
			shipDraw.ToggleSilhouetteDrawMode(showSilhouette);
			shipDraw.lineBody.active = true;
			shipDraw.linePath.active = true;
			ShipSitu objSS = shipDraw.ship.objSS;
			BodyOrbit bodyOrbit = null;
			if (objSS.bBOLocked || objSS.bIsBO || objSS.bOrbitLocked)
			{
				bodyOrbit = CrewSim.system.GetBO(objSS.strBOPORShip);
			}
			if (bodyOrbit != null)
			{
				bodyOrbit.UpdateTime(StarSystem.fEpoch, true, true);
			}
			objSS.TimeAdvance(0.0, true);
			ShipInfo shipInfo = ShipInfo.GetShipInfo(this.GetNavStationShip(), shipDraw.ship, this.ShipPropMap);
			float num3 = ((!shipDraw.ship.IsDerelict() && !shipDraw.ship.IsUnderConstruction) || shipInfo.Known) ? shipDraw.fRadiusM : ((float)GUIOrbitDraw.DERELICTSIZE);
			float num4 = (float)((double)(6f / num3) / MathUtils.GetMagnitude(this.dCanvasSolarXX, this.dCanvasSolarXY));
			float num5 = 6.684587E-12f;
			float num6 = num5 + num4;
			float fRot = (!shipDraw.DoNotRotate) ? objSS.fRot : num2;
			this.DrawVectorLine(shipDraw.lineBody, objSS.vPosx, objSS.vPosy, fRot, num6);
			this.DrawVectorLine(shipDraw.lineStatus, objSS.vPosx, objSS.vPosy, num2, num6 * 2f);
			if (shipDraw.lineNoWakeRange != null)
			{
				if (this.bShowNWZ)
				{
					shipDraw.lineNoWakeRange.active = true;
					this.DrawCarCircle(shipDraw.lineNoWakeRange, objSS.vPosx, objSS.vPosy);
				}
				else
				{
					shipDraw.lineNoWakeRange.active = false;
				}
			}
			double num7;
			double num8;
			this.SolarToCanvas(objSS.vPosx, objSS.vPosy, out num7, out num8);
			shipDraw.fCanvasX = (float)num7;
			shipDraw.fCanvasY = (float)num8;
			shipDraw.linePath.points2 = new List<Vector2>();
			foreach (Tuple<double, Point> tuple in objSS.aPathRecent)
			{
				double num9 = StarSystem.fEpoch - tuple.Item1;
				num9 *= this.fModTimeDiff;
				double num10;
				double num11;
				this.SolarToCanvas(tuple.Item2.X + num9 * dVelX, tuple.Item2.Y + num9 * dVelY, out num10, out num11);
				shipDraw.linePath.points2.Add(new Vector2((float)num10, (float)num11));
			}
			shipDraw.linePath.matrix = Matrix4x4.Scale(Vector3.one);
			shipDraw.linePath.Draw();
			if (num > 0.0)
			{
				bool flag = false;
				if (objSS.HasNavData())
				{
					ShipSitu shipSituAtTime = objSS.NavData.GetShipSituAtTime(this.dEpoch, true);
					if (shipSituAtTime != null)
					{
						this.ssTemp.CopyFrom(shipSituAtTime, false);
						flag = true;
					}
				}
				if (!flag)
				{
					this.ssTemp.CopyFrom(objSS, false);
					if (bodyOrbit != null)
					{
						bodyOrbit.UpdateTime(this.dEpoch, true, false);
					}
					this.ssTemp.TimeAdvance((double)((float)num), false);
				}
				float fScale = (this.dScopeRadius * 149597872.0 >= 2.5) ? num6 : (num6 / 5f);
				this.DrawVectorLine(shipDraw.GetProjection()[0], this.ssTemp.vPosx, this.ssTemp.vPosy, this.ssTemp.fRot, fScale);
			}
			else if (shipDraw == this.sdNS)
			{
				double num12;
				double num13;
				this.follow.GetVSXY(out num12, out num13);
				if (GUIOrbitDraw.CrossHairTarget.IsShipOrOrbit() && this.GetKnobFollowState == 1)
				{
					GUIOrbitDraw.CrossHairTarget.GetVSXY(out num12, out num13);
				}
				this.ssTemp.CopyFrom(this.GetNavStationShipSitu(), false);
				this.ssTemp.vVelX -= num12;
				this.ssTemp.vVelY -= num13;
				this.ssTemp.fW = this.ssTemp.fA * 0.05f;
				this.ssTemp.fA = 0f;
				float fScale2 = (this.dScopeRadius * 149597872.0 >= 2.5) ? num6 : (num6 / 5f);
				for (int i = 0; i < this.nProjStepsSelf; i++)
				{
					this.ssTemp.bOrbitLocked = false;
					this.ssTemp.TimeAdvance((double)(2f * Time.timeScale), false);
					this.DrawVectorLine(shipDraw.GetProjection()[i], this.ssTemp.vPosx, this.ssTemp.vPosy, this.ssTemp.fRot, fScale2);
				}
			}
			double num14;
			double num15;
			this.SolarToCanvas(objSS.vPosx, objSS.vPosy, out num14, out num15);
			num15 += (double)((!(objSS.strBOPORShip == string.Empty)) ? -20f : 20f);
			shipDraw.desiredCanvasPosFromSitu = new Vector3((float)(num14 - (double)this.vCanvasOffset.x), (float)(num15 - (double)this.vCanvasOffset.y), 0f);
			this.TryAddLabelToCanvas(shipDraw);
		}
		this.OnlyDrawTopLabels();
		if (num > 0.0)
		{
			foreach (BODraw bodraw2 in this.aBODraws)
			{
				bodraw2.bo.UpdateTime(StarSystem.fEpoch, true, false);
			}
		}
		if (this.aDebugDraws != null)
		{
			foreach (DebugDraw debugDraw in this.aDebugDraws)
			{
				debugDraw.LineBody.active = true;
				debugDraw.goLabel.SetActive(true);
				debugDraw.TargetSitu.TimeAdvance(0.0, true);
				float fScale3 = 6.684587E-12f + (float)((double)(6f / (float)DebugDraw.SIZE) / MathUtils.GetMagnitude(this.dCanvasSolarXX, this.dCanvasSolarXY));
				if (debugDraw.IsPrediction)
				{
					debugDraw.LineBody.points2 = new List<Vector2>();
					double num16;
					double num17;
					this.SolarToCanvas(debugDraw.TargetSitu.vPosx, debugDraw.TargetSitu.vPosy, out num16, out num17);
					debugDraw.LineBody.points2.Add(new Vector2((float)num16, (float)num17));
					Point predictedPosition = debugDraw.TargetSitu.GetPredictedPosition(30f);
					this.SolarToCanvas(predictedPosition.X, predictedPosition.Y, out num16, out num17);
					debugDraw.LineBody.points2.Add(new Vector2((float)num16, (float)num17));
					debugDraw.LineBody.matrix = Matrix4x4.Scale(Vector3.one);
					debugDraw.LineBody.Draw();
				}
				else
				{
					this.DrawVectorLine(debugDraw.LineBody, debugDraw.TargetSitu.vPosx, debugDraw.TargetSitu.vPosy, debugDraw.TargetSitu.fRot, fScale3);
				}
				double num18;
				double num19;
				this.SolarToCanvas(debugDraw.TargetSitu.vPosx, debugDraw.TargetSitu.vPosy, out num18, out num19);
				num19 += (double)((!(debugDraw.TargetSitu.strBOPORShip == string.Empty)) ? -20f : 20f);
				debugDraw.tfLabel.anchoredPosition = new Vector3((float)(num18 - (double)this.vCanvasOffset.x), (float)(num19 - (double)this.vCanvasOffset.y), 0f);
			}
		}
	}

	public void OnlyDrawTopLabels()
	{
		float smoothTime = 0.016f;
		if (ShipDraw.blink)
		{
			smoothTime = 0.008f;
		}
		for (int i = 0; i < this.VisibleShipDraws.Count; i++)
		{
			ShipDraw shipDraw = this.VisibleShipDraws[i];
			Vector3 vector;
			Vector3 vector2;
			if (shipDraw.desiredCanvasPosFromSitu.magnitude > 5000f)
			{
				vector = new Vector3(3000f, 3000f);
				vector2 = vector;
			}
			else
			{
				vector2 = shipDraw.desiredCanvasPosFromSitu;
			}
			vector = vector2;
			shipDraw.sharedLabelOffsetCurrent = Vector3.SmoothDamp(shipDraw.sharedLabelOffsetCurrent, shipDraw.sharedLabelOffsetTarget, ref shipDraw.sharedLabelVelocity, 0.12f);
			shipDraw.LabelID.labelRect.localPosition = vector + shipDraw.sharedLabelOffsetCurrent;
			shipDraw.LabelName.labelRect.localPosition = vector + shipDraw.sharedLabelOffsetCurrent;
			shipDraw.lastDrawnLabelPos = shipDraw.LabelActive.labelRect.localPosition;
			int activeLabel = ShipDraw.ActiveLabel;
			if (activeLabel != 0)
			{
				if (activeLabel != 1)
				{
					if (activeLabel == 2)
					{
						shipDraw.LabelID.cg.alpha = Mathf.SmoothDamp(shipDraw.LabelID.cg.alpha, 0f, ref shipDraw.sharedAlphaVelocity, smoothTime);
						shipDraw.LabelName.cg.alpha = Mathf.SmoothDamp(shipDraw.LabelName.cg.alpha, 1f, ref shipDraw.sharedAlphaVelocity, smoothTime);
					}
				}
				else
				{
					shipDraw.LabelID.cg.alpha = Mathf.SmoothDamp(shipDraw.LabelID.cg.alpha, 1f, ref shipDraw.sharedAlphaVelocity, smoothTime);
					shipDraw.LabelName.cg.alpha = Mathf.SmoothDamp(shipDraw.LabelName.cg.alpha, 0f, ref shipDraw.sharedAlphaVelocity, smoothTime);
				}
			}
			else
			{
				shipDraw.LabelID.cg.alpha = Mathf.SmoothDamp(shipDraw.LabelID.cg.alpha, 0f, ref shipDraw.sharedAlphaVelocity, smoothTime);
				shipDraw.LabelName.cg.alpha = Mathf.SmoothDamp(shipDraw.LabelName.cg.alpha, 0f, ref shipDraw.sharedAlphaVelocity, smoothTime);
			}
		}
		for (int j = 0; j < this.OverlappingShipDraws.Count; j++)
		{
			ShipDraw shipDraw2 = this.OverlappingShipDraws[j];
			shipDraw2.LabelName.cg.alpha = Mathf.SmoothDamp(shipDraw2.LabelName.cg.alpha, 0f, ref shipDraw2.sharedAlphaVelocity, 0f);
			shipDraw2.LabelID.cg.alpha = Mathf.SmoothDamp(shipDraw2.LabelName.cg.alpha, 0f, ref shipDraw2.sharedAlphaVelocity, 0f);
		}
	}

	public bool FoundIntersection(Bounds toTest, List<Bounds> tested, out Bounds intersected)
	{
		for (int i = 0; i < tested.Count; i++)
		{
			if (toTest.Intersects(tested[i]))
			{
				intersected = tested[i];
				return true;
			}
		}
		intersected = default(Bounds);
		return false;
	}

	public void TryAddLabelToCanvas(ShipDraw shipDraw)
	{
		if (shipDraw.LabelActive == null || shipDraw.LabelActive.labelRect == null)
		{
			return;
		}
		Bounds bounds = new Bounds(shipDraw.desiredCanvasPosFromSitu, shipDraw.LabelActive.labelRect.sizeDelta);
		Bounds bounds2;
		bool flag = this.FoundIntersection(bounds, this.boundsAddedThisFrame, out bounds2);
		bool flag2 = false;
		Bounds bounds3 = bounds;
		Vector3 vector = Vector3.zero;
		if (flag)
		{
			Vector3 a = -(bounds2.center - bounds.center).normalized;
			for (int i = 1; i < 32; i *= 2)
			{
				Bounds bounds4 = bounds3;
				vector = a * (float)i;
				bounds3 = new Bounds(bounds.center + vector, bounds.size);
				if (!this.FoundIntersection(bounds3, this.boundsAddedThisFrame, out bounds4))
				{
					flag2 = true;
					shipDraw.sharedLabelOffsetTarget = vector;
				}
				i++;
			}
		}
		if (flag2)
		{
			flag = false;
			bounds = bounds3;
		}
		else
		{
			shipDraw.sharedLabelOffsetTarget = Vector3.zero;
		}
		if (flag)
		{
			this.OverlappingShipDraws.Add(shipDraw);
		}
		else
		{
			this.VisibleShipDraws.Add(shipDraw);
			this.boundsAddedThisFrame.Add(bounds);
		}
	}

	private void ShowProjections(bool bShowMapFuture)
	{
		if (bShowMapFuture == this.bShowMapProjs)
		{
			return;
		}
		foreach (BODraw bodraw in this.aBODraws)
		{
			foreach (VectorLine vectorLine in bodraw.aProjs)
			{
				vectorLine.active = (bShowMapFuture && bodraw.Active);
			}
		}
		foreach (ShipDraw shipDraw in this.aShipDraws)
		{
			if (shipDraw.GetProjection() != null)
			{
				int num = 0;
				foreach (VectorLine vectorLine2 in shipDraw.GetProjection())
				{
					if (shipDraw == this.sdNS)
					{
						if (bShowMapFuture)
						{
							vectorLine2.active = (shipDraw.lineBody.active && num == 0);
						}
						else
						{
							vectorLine2.active = shipDraw.lineBody.active;
						}
					}
					else
					{
						vectorLine2.active = (bShowMapFuture && shipDraw.lineBody.active);
					}
					num++;
				}
			}
		}
		this.bShowMapProjs = bShowMapFuture;
	}

	public void UpdateShipInfo(ShipInfo si)
	{
		if (si == null)
		{
			return;
		}
		ShipInfo.SetShipInfo(si, this.dictPropMap);
		GUIOrbitDraw.crossHairInfo = null;
		this.InvalidateShipDraw(si._strRegID);
	}

	private void InvalidateShipDraw(string regId)
	{
		for (int i = 0; i < this.aShipDraws.Count; i++)
		{
			ShipDraw shipDraw = this.aShipDraws[i];
			if (shipDraw == null)
			{
				this.aShipDraws.RemoveAt(i);
			}
			else if (shipDraw.ship == null || shipDraw.ship.strRegID == regId)
			{
				this.aShipDraws.RemoveAt(i);
				shipDraw.Destroy();
			}
		}
	}

	private void InitOrbitArea(GameObject go)
	{
		this.txtSide = GUIRenderTargets.goLines.transform.Find("pnlSide/txtSide").GetComponent<TextMeshProUGUI>();
		Rect rect = ((RectTransform)go.transform).rect;
		List<Vector2> list = new List<Vector2>();
		int num = 4;
		float num2 = rect.width / (float)num;
		float num3 = rect.height / (float)num;
		int num4 = 2;
		for (int i = 1; i < num; i++)
		{
			int num5 = 0;
			while ((float)num5 < rect.height)
			{
				list.Add(new Vector2((float)i * num2, (float)num5));
				list.Add(new Vector2((float)i * num2, (float)(num5 + num4)));
				num5 += 2 * num4;
			}
		}
		for (int j = 1; j < num; j++)
		{
			int num6 = 0;
			while ((float)num6 < rect.width)
			{
				list.Add(new Vector2((float)num6, (float)j * num3));
				list.Add(new Vector2((float)(num6 + num4), (float)j * num3));
				num6 += 2 * num4;
			}
		}
		list.Add(new Vector2(1f, 1f));
		list.Add(new Vector2(rect.width - 1f, 1f));
		list.Add(new Vector2(rect.width - 1f, 1f));
		list.Add(new Vector2(rect.width - 1f, rect.height - 1f));
		list.Add(new Vector2(rect.width - 1f, rect.height - 1f));
		list.Add(new Vector2(1f, rect.height - 1f));
		list.Add(new Vector2(1f, rect.height - 1f));
		list.Add(new Vector2(1f, 1f));
		VectorLine vectorLine = new VectorLine("OrbitAxes", list, GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
		vectorLine.color = GUIOrbitDraw.clrBlue01;
		vectorLine.SetCanvas(go, false);
		this.aUIs.Add(vectorLine);
		this.lineCross = new VectorLine("Target Cross", NavIcon.Cross(125f, null), GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
		this.lineCross.color = GUIOrbitDraw.clrWhite01;
		this.lineCross.SetCanvas(go, false);
		this.lineCourse = new VectorLine("Course Vector", new List<Vector2>(new Vector2[]
		{
			default(Vector2),
			new Vector2(1f, 1f)
		}), GUIOrbitDraw.fLineWidth, LineType.Continuous, Joins.Weld);
		this.lineCourse.color = GUIOrbitDraw.clrOrange01;
		this.lineCourse.SetCanvas(go, false);
		this.lineGrav = new VectorLine("Grav Vector", new List<Vector2>(new Vector2[]
		{
			default(Vector2),
			new Vector2(1f, 1f)
		}), GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
		this.lineGrav.color = this.clrRed01;
		this.lineGrav.SetCanvas(go, false);
	}

	private void InitTitleArea(GameObject go)
	{
		Rect rect = ((RectTransform)go.transform).rect;
		VectorLine vectorLine = new VectorLine("Title", new List<Vector2>
		{
			new Vector2(1f, 1f),
			new Vector2(rect.width - 1f, 1f),
			new Vector2(rect.width - 1f, 1f),
			new Vector2(rect.width - 1f, rect.height - 1f),
			new Vector2(rect.width - 1f, rect.height - 1f),
			new Vector2(1f, rect.height - 1f),
			new Vector2(1f, rect.height - 1f),
			new Vector2(1f, 1f)
		}, GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
		vectorLine.color = GUIOrbitDraw.clrBlue01;
		vectorLine.SetCanvas(go, false);
		this.aUIs.Add(vectorLine);
	}

	private void InitSideArea(GameObject go)
	{
		Rect rect = ((RectTransform)go.transform).rect;
		VectorLine vectorLine = new VectorLine("Side", new List<Vector2>
		{
			new Vector2(1f, 1f),
			new Vector2(rect.width - 1f, 1f),
			new Vector2(rect.width - 1f, 1f),
			new Vector2(rect.width - 1f, rect.height - 1f),
			new Vector2(rect.width - 1f, rect.height - 1f),
			new Vector2(1f, rect.height - 1f),
			new Vector2(1f, rect.height - 1f),
			new Vector2(1f, 1f)
		}, GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
		vectorLine.color = GUIOrbitDraw.clrBlue01;
		vectorLine.SetCanvas(go, false);
		this.aUIs.Add(vectorLine);
	}

	private void InitFrame(GameObject go)
	{
		Rect rect = ((RectTransform)go.transform).rect;
		VectorLine vectorLine = new VectorLine("Frame", new List<Vector2>
		{
			new Vector2(1f, 1f),
			new Vector2(rect.width - 1f, 1f),
			new Vector2(rect.width - 1f, 1f),
			new Vector2(rect.width - 1f, rect.height - 1f),
			new Vector2(rect.width - 1f, rect.height - 1f),
			new Vector2(1f, rect.height - 1f),
			new Vector2(1f, rect.height - 1f),
			new Vector2(1f, 1f)
		}, GUIOrbitDraw.fLineWidth, LineType.Discrete, Joins.Weld);
		vectorLine.color = GUIOrbitDraw.clrBlue01;
		vectorLine.SetCanvas(go, false);
		this.aUIs.Add(vectorLine);
	}

	private Color GetBOColor(BodyOrbit bo)
	{
		if (bo.fMass >= 9.99999944211969E+27)
		{
			return GUIOrbitDraw.clrOrange01;
		}
		if (bo.fMass >= 9.999999778196308E+22)
		{
			return GUIOrbitDraw.clrBlue01;
		}
		return this.clrGreen01;
	}

	private Color GetTrackColor(BodyOrbit bo)
	{
		if (bo.fMass >= 9.99999944211969E+27)
		{
			return this.clrOrange01Half;
		}
		if (bo.fMass >= 9.999999778196308E+22)
		{
			return this.clrBlue01Half;
		}
		if (bo.IsPlaceholder())
		{
			return this.clrShipOrbit;
		}
		return this.clrGreen01Half;
	}

	private void RemoveOrbital(string boName)
	{
		foreach (BODraw bodraw in this.aBODraws)
		{
			if (!(bodraw.bo.strName != boName))
			{
				bodraw.Destroy();
				this.aBODraws.Remove(bodraw);
				break;
			}
		}
	}

	private void AddOrbital(BodyOrbit bo, GameObject go)
	{
		BODraw bodraw = new BODraw(bo);
		bodraw.lineBody = this.CreateBodyTexture(bo.strName + "BodyS", this.GetBOColor(bo), (float)bo.fRadius, go);
		for (int i = 0; i < this.nProjSteps; i++)
		{
			bodraw.aProjs.Add(this.CreateBodyTexture(bo.strName + "BodyS_" + i, this.GetBOColor(bo), (float)bo.fRadius, go));
		}
		bodraw.lineGrav = this.CreateBodyLine(bo.strName + "Grav", this.clrRed02, (float)(bo.fRadius + bo.GravRadius), go);
		if (bo.fParallaxRadius > 0.0 && bo.fParallaxRadius < 1E+20 && bo.fParallaxRadius < bo.GravRadius)
		{
			bodraw.lineGravInner = this.CreateBodyLine(bo.strName + "GravInner", this.clrRed01, (float)bo.fParallaxRadius, go);
		}
		this.aBODraws.Add(bodraw);
	}

	private VectorLine CreateBodyTexture(string strName, Color c, float fRad, GameObject go)
	{
		VectorLine vectorLine = new VectorLine(strName, new List<Vector2>(65), this.texLine01, 32f, LineType.Continuous, Joins.Weld);
		vectorLine.textureScale = 1f;
		vectorLine.capLength = 32f;
		vectorLine.color = c;
		vectorLine.SetCanvas(go, false);
		vectorLine.MakeSpline(NavIcon.Circle(fRad, 64), true);
		return vectorLine;
	}

	private VectorLine CreateBodyLine(string strName, Color c, float fRad, GameObject go)
	{
		VectorLine vectorLine = new VectorLine(strName, new List<Vector2>(65), GUIOrbitDraw.fLineWidth, LineType.Continuous, Joins.Weld);
		vectorLine.color = c;
		vectorLine.SetCanvas(go, false);
		vectorLine.MakeSpline(NavIcon.Circle(fRad, 512), true);
		return vectorLine;
	}

	private double GetBOTErr(BODraw bod, double t)
	{
		bool bCorrectTimes = true;
		bod.bo.UpdateTime(t, bCorrectTimes, true);
		double num;
		double num2;
		this.SolarToCanvas(bod.bo.dXReal, bod.bo.dYReal, out num, out num2);
		num -= (double)(this.rectDrawPanel.rect.width * 0.5f);
		num2 -= (double)(this.rectDrawPanel.rect.height * 0.5f);
		return num * num + num2 * num2;
	}

	private void ImproveTrackEpoch(BODraw bod)
	{
		double num = (bod.dTrackEpoch - this.dEpoch) % bod.bo.fPeriod + this.dEpoch;
		double num2 = this.GetBOTErr(bod, num);
		double num3 = 86400.0;
		while (Mathf.Abs((float)num3) > 6000f)
		{
			double num4 = num + num3;
			double boterr = this.GetBOTErr(bod, num4);
			if (num2 > boterr)
			{
				num2 = boterr;
				num = num4;
				num3 *= 1.100000023841858;
			}
			else
			{
				num3 *= -0.30000001192092896;
			}
		}
		if (Mathf.Abs((float)(bod.dTrackEpoch - num)) > 6000f)
		{
			bod.dTrackEpoch = num;
			VectorLine.Destroy(ref bod.lineTrackPartial);
			bod.lineTrackPartial = null;
		}
	}

	private VectorLine GetLineTrack(BODraw bod, double centreTime, double originX, double originY, bool arcOnly, VectorLine lineOrbit)
	{
		BodyOrbit bo = bod.bo;
		double num = 0.0;
		double num2 = 0.0;
		int num3 = 64;
		if (bod.bo.IsShipOrbit())
		{
			if (arcOnly)
			{
				num3 = 512;
			}
			else if (bod.bo.fAxis2 > 3.42250857685997E-06)
			{
				num3 = 1024;
			}
			else if (bod.bo.fAxis2 > 1.711254288429985E-06)
			{
				num3 = 512;
			}
			else if (bod.bo.fAxis2 > 8.556271442149925E-07)
			{
				num3 = 256;
			}
			else
			{
				num3 = 128;
			}
		}
		else if (bod.bo.IsPlaceholder())
		{
			num3 = ((!arcOnly) ? 256 : 128);
		}
		Vector2[] array = new Vector2[num3 + 1];
		for (int i = 0; i <= num3; i++)
		{
			double dTime;
			if (arcOnly)
			{
				int num4 = i - num3 / 2;
				dTime = centreTime + (double)(num4 * num4 * num4) * bo.fPeriod / Math.Pow((double)num3, 3.0);
			}
			else
			{
				dTime = centreTime + (double)i * bo.fPeriod / (double)num3;
				if (bod.bo.boParent != null)
				{
					bod.bo.boParent.UpdateTime(dTime, false, false);
					num = bod.bo.boParent.dXReal;
					num2 = bod.bo.boParent.dYReal;
				}
			}
			bo.UpdateTime(dTime, arcOnly, false);
			array[i] = new Vector2((float)(bo.dXReal - num - originX), (float)(bo.dYReal - num2 - originY));
		}
		if (lineOrbit == null)
		{
			lineOrbit = new VectorLine(bo.strName + "Orbit", new List<Vector2>(num3), GUIOrbitDraw.fLineWidth, LineType.Continuous, Joins.Weld);
		}
		lineOrbit.color = this.GetTrackColor(bo);
		lineOrbit.SetCanvas(this.goOrbitPanel, false);
		lineOrbit.MakeSpline(array, false);
		if (bod.bo.IsShipOrbit() && !arcOnly)
		{
			this.RepairTrackGlitches(array, lineOrbit);
		}
		return lineOrbit;
	}

	private void RepairTrackGlitches(Vector2[] aPointsNew, VectorLine lineOrbit)
	{
		Vector2 b = Vector2.zero;
		int num = 0;
		while (num < aPointsNew.Length && num < lineOrbit.points2.Count)
		{
			if (num != 0 && Vector2.Distance(aPointsNew[num], lineOrbit.points2[num]) > (aPointsNew[num] - b).magnitude * 3f)
			{
				lineOrbit.points2[num] = aPointsNew[num] - b;
			}
			else
			{
				b = aPointsNew[num] - lineOrbit.points2[num];
			}
			num++;
		}
	}

	private void DrawOrbitTrack(BODraw bod, bool showProjections)
	{
		if (bod.bo.nDrawFlagsTrack == 1)
		{
			return;
		}
		float num = Mathf.Sqrt((float)(this.dCanvasSolarXX * this.dCanvasSolarXX + this.dCanvasSolarXY * this.dCanvasSolarXY));
		double fAxis = bod.bo.fAxis1;
		bool flag = bod.bo != null && bod.bo.IsShipOrbit();
		if ((double)num * fAxis < (double)(this.fMinOrbitDiam * 0.5f) && !flag)
		{
			bod.SetState(false, false);
			return;
		}
		bod.SetState(true, showProjections);
		bool flag2 = true;
		if (!bod.bo.IsMoon() && (double)num * fAxis > 1000.0)
		{
			flag2 = false;
		}
		double num2 = 0.0;
		double num3 = 0.0;
		if (flag2)
		{
			if (bod.lineTrackPartial != null)
			{
				bod.lineTrackPartial.active = false;
			}
			if (bod.lineTrackFull == null)
			{
				bod.lineTrackFull = this.GetLineTrack(bod, 0.0, 0.0, 0.0, false, bod.lineTrackFull);
			}
			bod.lineTrackFull.active = true;
			if (bod.bo.boParent != null)
			{
				num2 += bod.bo.boParent.dXReal;
				num3 += bod.bo.boParent.dYReal;
			}
		}
		else
		{
			double cx = (double)(this.rectDrawPanel.rect.width * 0.5f);
			double cy = (double)(this.rectDrawPanel.rect.height * 0.5f);
			this.CanvasToSolar(cx, cy, out num2, out num3);
			if (bod.lineTrackFull != null)
			{
				bod.lineTrackFull.active = false;
			}
			this.ImproveTrackEpoch(bod);
			bod.lineTrackPartial = this.GetLineTrack(bod, bod.dTrackEpoch, num2, num3, true, bod.lineTrackPartial);
			bod.lineTrackPartial.active = true;
		}
		double num4;
		double num5;
		this.SolarToCanvas(num2, num3, out num4, out num5);
		double num6;
		double num7;
		this.SolarToCanvas(num2 + 1.0, num3, out num6, out num7);
		double num8;
		double num9;
		this.SolarToCanvas(num2, num3 + 1.0, out num8, out num9);
		Matrix4x4 matrix = Matrix4x4.Scale(Vector3.one);
		matrix.SetRow(0, new Vector4((float)(num6 - num4), (float)(num8 - num4), 0f, (float)num4));
		matrix.SetRow(1, new Vector4((float)(num7 - num5), (float)(num9 - num5), 0f, (float)num5));
		if (flag2)
		{
			bod.lineTrackFull.matrix = matrix;
			bod.lineTrackFull.Draw();
		}
		else
		{
			bod.lineTrackPartial.matrix = matrix;
			bod.lineTrackPartial.Draw();
		}
	}

	private void ToggleOrbitalMode(bool show)
	{
		Ship ship = this.COSelf.ship;
		if ((show && ship.IsDocked()) || ship.IsUsingTorchDrive)
		{
			return;
		}
		BodyOrbit bodyOrbit = CrewSim.system.GetBO(ship.strRegID);
		if (show)
		{
			if (bodyOrbit == null)
			{
				bodyOrbit = ship.LockToOrbit();
			}
			else
			{
				ship.objSS.LockToOrbit(bodyOrbit, -1.0);
			}
			if (bodyOrbit != null)
			{
				this.AddOrbital(bodyOrbit, this.goOrbitPanel);
			}
		}
		else if (bodyOrbit != null)
		{
			if (ship.objSS.strBOPORShip == bodyOrbit.strName)
			{
				ship.UnlockFromOrbit(true);
			}
			CrewSim.system.RemoveBO(bodyOrbit);
		}
	}

	private void DrawBody(BODraw bod, int nIndex, float fMinDiam)
	{
		if (!bod.Active || bod.lineBody == null)
		{
			return;
		}
		if (nIndex < 0)
		{
			bod.bo.UpdateTime(StarSystem.fEpoch, true, true);
		}
		else
		{
			bod.bo.UpdateTime(this.dEpoch, true, true);
		}
		double num;
		double num2;
		this.SolarToCanvas(bod.bo.dXReal, bod.bo.dYReal, out num, out num2);
		double num3;
		double num4;
		this.SolarToCanvas(bod.bo.dXReal, bod.bo.dYReal + 1.0, out num3, out num4);
		double num5 = num3 - num;
		double num6 = num4 - num2;
		float num7 = Mathf.Sqrt((float)(num5 * num5 + num6 * num6));
		Matrix4x4 matrix = Matrix4x4.Scale(Vector3.one);
		matrix.SetRow(0, new Vector4(num7, 0f, 0f, (float)num));
		matrix.SetRow(1, new Vector4(0f, num7, 0f, (float)num2));
		if (nIndex < 0)
		{
			if (bod.lineGrav != null)
			{
				bod.lineGrav.matrix = matrix;
				bod.lineGrav.Draw();
			}
			if (bod.lineGravInner != null)
			{
				bod.lineGravInner.matrix = matrix;
				bod.lineGravInner.Draw();
			}
		}
		VectorLine vectorLine = bod.lineBody;
		if (nIndex >= 0)
		{
			vectorLine = bod.aProjs[nIndex];
		}
		float num8 = fMinDiam * 0.5f / vectorLine.points2[0].x;
		if (num7 < num8)
		{
			num7 = num8;
			vectorLine.textureScale = 1f;
			vectorLine.capLength = GUIOrbitDraw.fLineWidth;
			vectorLine.lineWidth = GUIOrbitDraw.fLineWidth;
			vectorLine.texture = this.texLine04;
		}
		else
		{
			vectorLine.textureScale = 1f;
			vectorLine.capLength = 32f;
			vectorLine.lineWidth = 32f;
			vectorLine.texture = this.texLine01;
		}
		matrix.SetRow(0, new Vector4(num7, 0f, 0f, (float)num));
		matrix.SetRow(1, new Vector4(0f, num7, 0f, (float)num2));
		vectorLine.matrix = matrix;
		vectorLine.Draw();
	}

	private void DrawVectorLine(VectorLine vectorLine, double sx, double sy, float fRot, float fScale = -1f)
	{
		if (vectorLine == null)
		{
			return;
		}
		double num;
		double num2;
		this.SolarToCanvas(sx, sy, out num, out num2);
		double num3;
		double num4;
		this.SolarToCanvas(sx + (double)Mathf.Cos(fRot), sy + (double)Mathf.Sin(fRot), out num3, out num4);
		float num5 = (float)(num3 - num);
		float num6 = (float)(num4 - num2);
		if (fScale <= 0f)
		{
			MathUtils.SetLength(ref num5, ref num6, 1f);
		}
		else
		{
			num5 *= fScale;
			num6 *= fScale;
		}
		Matrix4x4 matrix = Matrix4x4.Scale(Vector3.one);
		matrix.SetRow(0, new Vector4(num5, -num6, 0f, (float)num));
		matrix.SetRow(1, new Vector4(num6, num5, 0f, (float)num2));
		vectorLine.matrix = matrix;
		vectorLine.Draw();
	}

	private void DrawCarCircle(VectorLine vectorLine, double sx, double sy)
	{
		if (vectorLine == null)
		{
			return;
		}
		double num;
		double num2;
		this.SolarToCanvas(sx, sy, out num, out num2);
		double num3;
		double num4;
		this.SolarToCanvas(sx, sy + 1.0, out num3, out num4);
		double num5 = num3 - num;
		double num6 = num4 - num2;
		float num7 = Mathf.Sqrt((float)(num5 * num5 + num6 * num6));
		Matrix4x4 matrix = Matrix4x4.Scale(Vector3.one);
		matrix.SetRow(0, new Vector4(num7, 0f, 0f, (float)num));
		matrix.SetRow(1, new Vector4(0f, num7, 0f, (float)num2));
		vectorLine.matrix = matrix;
		vectorLine.Draw();
	}

	private void ToggleInnerPanel()
	{
		if (this.tfPanelIn.GetComponent<CanvasGroup>().alpha != 1f)
		{
			CanvasManager.ShowCanvasGroup(this.tfPanelIn.GetComponent<CanvasGroup>());
			AudioManager.am.PlayAudioEmitter("ShipUIScrew", false, false);
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.tfPanelIn.GetComponent<CanvasGroup>());
			AudioManager.am.PlayAudioEmitter("ShipUIScrew", false, false);
		}
	}

	private void ToggleNote()
	{
		if (this.btnNote == null)
		{
			this.bNoteOpen = false;
			return;
		}
		if (this.bNoteAnimating)
		{
			return;
		}
		if (this.bNoteOpen)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIPaperRustle02", false, false);
			base.StartCoroutine(this.AnimateNote(!this.bNoteOpen));
		}
		else
		{
			AudioManager.am.PlayAudioEmitter("ShipUIPaperRustle01", false, false);
			base.StartCoroutine(this.AnimateNote(!this.bNoteOpen));
		}
	}

	private void SetNoteInitial(bool open)
	{
		if (open)
		{
			this.bNoteOpen = true;
			if (this.btnNote == null)
			{
				return;
			}
			(this.btnNote.transform as RectTransform).anchoredPosition = this.noteRaised;
			this.btnNote.transform.rotation = Quaternion.identity;
		}
		else
		{
			this.bNoteOpen = false;
			if (this.btnNote == null)
			{
				return;
			}
			(this.btnNote.transform as RectTransform).anchoredPosition = this.noteLowered;
			this.btnNote.transform.rotation = Quaternion.Euler(this.eulerLowered);
		}
	}

	private IEnumerator AnimateNote(bool open)
	{
		this.bNoteOpen = open;
		this.bNoteAnimating = true;
		CondOwner coUser = CrewSim.GetSelectedCrew();
		if (coUser != null)
		{
			coUser.ZeroCondAmount("TutorialNavNoteWaiting");
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(coUser.strID);
		}
		float duration = 0.25f;
		float t = 0f;
		Vector3 origin = this.noteRaised;
		Vector3 destination = this.noteLowered;
		Vector3 startRot = Vector3.zero;
		Vector3 destRot = this.eulerLowered;
		if (open)
		{
			origin = this.noteLowered;
			destination = this.noteRaised;
			startRot = this.eulerLowered;
			destRot = Vector3.zero;
		}
		RectTransform noteRect = this.btnNote.transform as RectTransform;
		if (noteRect == null)
		{
			yield break;
		}
		while (t <= duration)
		{
			float step = t / duration;
			noteRect.anchoredPosition = Vector3.Lerp(origin, destination, Mathf.SmoothStep(0f, 1f, step));
			this.btnNote.transform.rotation = Quaternion.Euler(Vector3.Lerp(startRot, destRot, Mathf.SmoothStep(0f, 1f, step)));
			t += Time.unscaledDeltaTime;
			yield return null;
		}
		this.bNoteAnimating = false;
		noteRect.anchoredPosition = destination;
		this.btnNote.transform.rotation = Quaternion.Euler(destRot);
		yield break;
	}

	public void ToggleNavModeExt(bool bValue)
	{
		this.chkNavMode.isOn = bValue;
	}

	private void ToggleStationKeeping(bool isOn)
	{
		base.SetPropMapData("chkStationKeeping", isOn.ToString());
		AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.COSelf.ship.strRegID);
		if (isOn && GUIOrbitDraw.CrossHairTarget != null && GUIOrbitDraw.CrossHairTarget.Ship != null)
		{
			if (aishipByRegID == null || aishipByRegID.ActiveCommandName != "HoldStationAutoPilot")
			{
				this.COSelf.ship.shipStationKeepingTarget = GUIOrbitDraw.CrossHairTarget.Ship;
				this.ToggleHoldThrust(false);
				Debug.Log("Setting station keeping to " + GUIOrbitDraw.CrossHairTarget.Ship.strRegID);
				AIShipManager.UnregisterShip(this.COSelf.ship);
				AIShip aiship = AIShipManager.AddAIToShip(this.COSelf.ship, AIType.Auto, "INTERREGIONAL", new JsonAIShipSave
				{
					strATCLast = AIShipManager.strATCLast,
					strRegId = this.COSelf.ship.strRegID,
					strHomeStation = "OKLG",
					enumAIType = AIType.Auto,
					strActiveCommand = "HoldStationAutoPilot",
					strActiveCommandPayload = new string[0]
				});
			}
		}
		else if (isOn && (GUIOrbitDraw.CrossHairTarget == null || GUIOrbitDraw.CrossHairTarget.Ship == null || GUIOrbitDraw.CrossHairTarget.Ship == this.COSelf.ship))
		{
			this.chkStationKeeping.isOn = false;
			if (aishipByRegID != null && aishipByRegID.ActiveCommandName == "HoldStationAutoPilot")
			{
				AIShipManager.UnregisterShip(this.COSelf.ship);
			}
			this.CGClampWarning("GUI_ORBIT_WARN_STATIONKEEP", false);
		}
		else
		{
			this.COSelf.ship.shipStationKeepingTarget = null;
			if (aishipByRegID != null && aishipByRegID.ActiveCommandName == "HoldStationAutoPilot")
			{
				AIShipManager.UnregisterShip(this.COSelf.ship);
			}
		}
	}

	private void ToggleHoldThrust(bool turnOn)
	{
		if (StarSystem.fEpoch - this._holdthrustTimeStamp < 1.0 || this.IsPDANav)
		{
			return;
		}
		this._holdthrustTimeStamp = StarSystem.fEpoch;
		base.SetPropMapData("chkHoldThrust", turnOn.ToString());
		AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.COSelf.ship.strRegID);
		if (turnOn)
		{
			this.ledWLock.State = 3;
			if (aishipByRegID == null || aishipByRegID.ActiveCommandName != "HoldThrustAutoPilot")
			{
				AIShipManager.UnregisterShip(this.COSelf.ship);
				AIShip aiship = AIShipManager.AddAIToShip(this.COSelf.ship, AIType.Auto, "INTERREGIONAL", new JsonAIShipSave
				{
					strATCLast = AIShipManager.strATCLast,
					strRegId = this.COSelf.ship.strRegID,
					strHomeStation = AIShipManager.strATCLast,
					enumAIType = AIType.Auto,
					strActiveCommand = "HoldThrustAutoPilot",
					strActiveCommandPayload = new string[0]
				});
			}
		}
		else
		{
			if (aishipByRegID != null && aishipByRegID.ActiveCommandName == "HoldThrustAutoPilot")
			{
				AIShipManager.UnregisterShip(this.COSelf.ship);
			}
			this.ledWLock.State = 0;
		}
	}

	private void ToggleNavMode(bool bValue)
	{
		this.bRCS = !bValue;
		string str = "PAN";
		if (this.bRCS)
		{
			str = "RCS";
		}
		TMP_Text component = GUIRenderTargets.goLines.transform.Find("pnlTitle/txtNavMode").GetComponent<TMP_Text>();
		component.text = "NAV MODE: " + str;
	}

	private void SetupTravel()
	{
		this.ddTravel.ClearOptions();
		if (CrewSim.system == null)
		{
			return;
		}
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		foreach (string text in CrewSim.system.dictShips.Keys)
		{
			if (!(this.COSelf.ship.strRegID == text))
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
				if (shipByRegID.DockCount > 0)
				{
					TMP_Dropdown.OptionData item = new TMP_Dropdown.OptionData(text);
					list.Add(item);
				}
			}
		}
		this.ddTravel.AddOptions(list);
	}

	private void OnTravelClick()
	{
		Debug.Log("OnTravelClick");
		GUIDockSys component = this.igh.goUIRight.GetComponent<GUIDockSys>();
		component.ForceUndock();
		Ship ship;
		if (CrewSim.system.dictShips.TryGetValue(this.ddTravel.options[this.ddTravel.value].text, out ship))
		{
			this.COSelf.ship.objSS.CopyFrom(ship.objSS, true);
			component.ForceDock(ship.strRegID);
			this.COSelf.ship.objSS.PlaceOrbitPosition(ship.objSS);
		}
		this.SetOldFollow();
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		this.initNoAudio = true;
		if (CrewSim.system != null)
		{
			GUIOrbitDraw.RevealStartingVessels(coSelf);
		}
		base.Init(coSelf, dict, strCOKey);
		if (strCOKey != "Panel A")
		{
			this._shipPropMap = coSelf.mapGUIPropMaps["Panel A"];
			this.IsPDANav = (strCOKey == "PDANAV");
		}
		if (this.objSystem == null && CrewSim.system != null)
		{
			this.LoadSystem(CrewSim.system);
		}
		this.SetupTravel();
		string text;
		if (this.dictPropMap.TryGetValue("fTimeRate", out text))
		{
			this.fTimeFuture = float.Parse(text);
			this.fTimeFutureTarget = this.fTimeFuture;
		}
		if (this.dictPropMap.TryGetValue("strPOIShip", out text))
		{
			foreach (ShipDraw shipDraw in this.aShipDraws)
			{
				if (shipDraw.ship.strRegID == text)
				{
					this.LockTarget(new NavPOI(shipDraw, this.GetNavStationShip(), this.ShipPropMap));
					break;
				}
			}
		}
		else if (this.dictPropMap.TryGetValue("strPOIBO", out text))
		{
			this.LockTarget(new NavPOI(this.objSystem.GetBO(text)));
		}
		else if (this.dictPropMap.TryGetValue("strPOIX", out text))
		{
			double sx = double.Parse(text);
			this.dictPropMap.TryGetValue("strPOIY", out text);
			double sy = double.Parse(text);
			this.LockTarget(new NavPOI(sx, sy));
		}
		if (this.dictPropMap.TryGetValue("chkStationKeeping", out text))
		{
			this.chkStationKeeping.isOn = bool.Parse(text);
		}
		if (this.dictPropMap.TryGetValue("chkHoldThrust", out text))
		{
			this.ToggleHoldThrust(bool.Parse(text));
		}
		if (this.dictPropMap.TryGetValue("dMagTarget", out text))
		{
			float num = float.Parse(text);
			if (num != 0f)
			{
				this.dMagTarget = (double)num;
			}
		}
		if (this.dictPropMap.TryGetValue("fZoomTimer", out text))
		{
			float num2 = float.Parse(text);
			if (this.fZoomTimer != 0f)
			{
				this.fZoomTimer = num2;
			}
		}
		this.SetNoteInitial(true);
		if (this.dictPropMap.TryGetValue("bNote", out text) && bool.Parse(text) != this.bNoteOpen)
		{
			this.SetNoteInitial(false);
		}
		if (this.dictPropMap.TryGetValue("bXPDROn", out text) && bool.Parse(text) != this.bNoteOpen)
		{
			this.SetNoteInitial(false);
		}
		float num3 = 0.25f;
		if (this.dictPropMap.TryGetValue("slidThrottle", out text))
		{
			num3 = float.Parse(text);
		}
		else
		{
			double num4 = this.GetNavStationShip().RCSAccelMaxUndocked;
			if (num4 == 0.0)
			{
				num4 = (double)(this.GetNavStationShip().LiftRotorsThrustStrength / 149597870f);
			}
			if (num4 != 0.0)
			{
				num3 = Mathf.Clamp((float)(1.9016982118751132E-10 / num4), 0f, 1f);
			}
		}
		base.SetPropMapData("slidThrottle", num3.ToString());
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew != null)
		{
			if (selectedCrew.HasCond("TutorialNavDockingSwitchNavShow"))
			{
				selectedCrew.ZeroCondAmount("TutorialNavDockingSwitchNavShow");
				MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(selectedCrew.strID);
			}
			selectedCrew.SetCondAmount("IsNavStationUsed", 1.0, 0.0);
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(selectedCrew.strID);
			selectedCrew.ZeroCondAmount("IsNavStationUsed");
		}
		if (this.dictPropMap.TryGetValue("bRCS", out text))
		{
			this.chkNavMode.isOn = !bool.Parse(text);
		}
		this.fRCSMax = (float)this.GetNavStationShip().GetRCSMax();
		this.fZoomTimer = 2f;
		AudioManager.am.SuggestMusic("Map", false);
		GUIOrbitDrawTut guiorbitDrawTut = base.gameObject.AddComponent<GUIOrbitDrawTut>();
		guiorbitDrawTut.SetNewGameObjectives(this, selectedCrew, this.sdNS);
		GUIOrbitDraw.OpenedNavStationUI.Invoke();
		string shipOwner = CrewSim.system.GetShipOwner(this.COSelf.ship.strRegID);
		string str = DataHandler.GetString("GUI_ORBIT_USER_UNKNOWN", false);
		bool flag = false;
		if (selectedCrew != null)
		{
			flag = (shipOwner == selectedCrew.strID);
			if (!flag && selectedCrew.socUs != null)
			{
				Relationship relationship = selectedCrew.socUs.GetRelationship(shipOwner);
				flag = (relationship != null && relationship.aRelationships.Contains("RELCaptain"));
			}
			str = selectedCrew.FriendlyName;
		}
		if (!flag)
		{
			CanvasManager.ShowCanvasGroup(this.cgNag);
			this.fEpochNagEnd = StarSystem.fEpoch + 10.0;
			AudioManager.am.PlayAudioEmitter("ShipUIBtnDCNoClearance", false, false);
		}
		else
		{
			CanvasManager.HideCanvasGroup(this.cgNag);
		}
		this.COSelf.ship.LogAdd(DataHandler.GetString("NAV_LOG_USER_SESSION", false) + str + DataHandler.GetString("NAV_LOG_TERMINATOR", false), StarSystem.fEpoch, true);
		this.initNoAudio = false;
		GUIOrbitDraw.Instance = this;
	}

	public override void SaveAndClose()
	{
		GUIOrbitDraw.UpdateShipSelection.RemoveListener(new UnityAction<string>(this.LockTarget));
		this._shipPropMap = null;
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SetPropMapData("bRCS", this.bRCS.ToString().ToLower());
		base.SetPropMapData("bNote", this.bNoteOpen.ToString().ToLower());
		base.SetPropMapData("nProjSteps", this.nProjSteps.ToString());
		base.SetPropMapData("fTimeRate", this.fTimeFuture.ToString());
		base.SetPropMapData("strPOIShip", null);
		base.SetPropMapData("strPOIBO", null);
		base.SetPropMapData("strPOIX", null);
		base.SetPropMapData("strPOIY", null);
		base.SetPropMapData("dMagTarget", ((float)(this.dCanvasSolarXX * this.dCanvasSolarXX + this.dCanvasSolarXY * this.dCanvasSolarXY)).ToString());
		base.SetPropMapData("fZoomTimer", "2.0");
		if (GUIOrbitDraw.CrossHairTarget.Ship != null)
		{
			base.SetPropMapData("strPOIShip", GUIOrbitDraw.CrossHairTarget.Ship.strRegID);
		}
		else if (GUIOrbitDraw.CrossHairTarget.bodyOrbit != null)
		{
			base.SetPropMapData("strPOIBO", GUIOrbitDraw.CrossHairTarget.bodyOrbit.strName);
		}
		else
		{
			double num;
			double num2;
			GUIOrbitDraw.CrossHairTarget.GetSXY(out num, out num2);
			base.SetPropMapData("strPOIX", num.ToString());
			base.SetPropMapData("strPOIY", num2.ToString());
		}
		VectorLine.Destroy(this.aUIs);
		VectorLine.Destroy(ref this.lineCross);
		VectorLine.Destroy(ref this.lineCourse);
		VectorLine.Destroy(ref this.lineGrav);
		if (this.lineStationKeepingTarget != null)
		{
			VectorLine.Destroy(ref this.lineStationKeepingTarget);
		}
		foreach (BODraw bodraw in this.aBODraws)
		{
			bodraw.Destroy();
		}
		foreach (ShipDraw shipDraw in this.aShipDraws)
		{
			shipDraw.Destroy();
		}
		foreach (DebugDraw debugDraw in this.aDebugDraws)
		{
			debugDraw.Destroy();
		}
		this.aDebugDraws.Clear();
		if (this.COSelf != null && this.COSelf.ship != null && !this.IsPDANav && !this.HoldingThrustActive)
		{
			this.COSelf.ship.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
		}
		this.fClampDisengageWarningTimer = 0f;
		if (this.cgClampWarning != null)
		{
			this.cgClampWarning.alpha = 0f;
		}
		this.StopMapAudio();
		base.StopAllCoroutines();
		base.SaveAndClose();
		GUIOrbitDraw.Instance = null;
	}

	private bool HasAncestor(BodyOrbit target, BodyOrbit hit)
	{
		return hit != null && target != null && (target == hit || this.HasAncestor(target.boParent, hit));
	}

	private BodyOrbit FindNearestBodyOrbit(Vector2 canvasPos, float clickRadius, double fTargetEpoch)
	{
		BodyOrbit bodyOrbit = null;
		float num = clickRadius;
		foreach (BODraw bodraw in this.aBODraws)
		{
			if (!this.HasAncestor(bodraw.bo, bodyOrbit))
			{
				bodraw.bo.UpdateTime(fTargetEpoch, true, true);
				double num2;
				double num3;
				this.SolarToCanvas(bodraw.bo.dXReal, bodraw.bo.dYReal, out num2, out num3);
				double x;
				double y;
				this.SolarToCanvas(bodraw.bo.dXReal, bodraw.bo.dYReal + bodraw.bo.fRadius, out x, out y);
				double distance = MathUtils.GetDistance(num2, num3, x, y);
				float num4 = (float)(MathUtils.GetDistance((double)canvasPos.x, (double)canvasPos.y, num2, num3) - distance);
				if (num > num4)
				{
					num = num4;
					bodyOrbit = bodraw.bo;
				}
			}
		}
		return bodyOrbit;
	}

	private ShipDraw FindNearestShipDraw(Vector2 canvasPos, float radius, double fTargetEpoch)
	{
		ShipDraw result = null;
		float num = radius;
		foreach (ShipDraw shipDraw in this.aShipDraws)
		{
			if (shipDraw == null || shipDraw.ship == null || shipDraw.ship.objSS == null)
			{
				Debug.LogWarning("ShipDraws contained a null");
			}
			else
			{
				Point predictedPosition = shipDraw.ship.objSS.GetPredictedPosition((float)(fTargetEpoch - StarSystem.fEpoch));
				double num2;
				double num3;
				this.SolarToCanvas(predictedPosition.X, predictedPosition.Y, out num2, out num3);
				double x;
				double y;
				this.SolarToCanvas(predictedPosition.X + (double)shipDraw.fRadiusAU, predictedPosition.Y, out x, out y);
				double distance = MathUtils.GetDistance(num2, num3, x, y);
				float num4 = (float)(MathUtils.GetDistance((double)canvasPos.x, (double)canvasPos.y, num2, num3) - distance);
				if (num > num4)
				{
					num = num4;
					result = shipDraw;
				}
			}
		}
		return result;
	}

	private NavPOI FindNearestNavPOI(Vector2 canvasPos, float radius)
	{
		ShipDraw shipDraw = this.FindNearestShipDraw(canvasPos, radius, StarSystem.fEpoch);
		if (shipDraw != null)
		{
			return new NavPOI(shipDraw, this.GetNavStationShip(), this.ShipPropMap);
		}
		if (this.dEpoch != StarSystem.fEpoch)
		{
			shipDraw = this.FindNearestShipDraw(canvasPos, radius, this.dEpoch);
			if (shipDraw != null)
			{
				return new NavPOI(shipDraw, this.GetNavStationShip(), this.ShipPropMap)
				{
					fTargetFuture = this.dEpoch - StarSystem.fEpoch
				};
			}
		}
		BodyOrbit bodyOrbit = this.FindNearestBodyOrbit(canvasPos, radius, StarSystem.fEpoch);
		if (bodyOrbit != null)
		{
			return new NavPOI(bodyOrbit);
		}
		if (this.dEpoch != StarSystem.fEpoch)
		{
			bodyOrbit = this.FindNearestBodyOrbit(canvasPos, radius, this.dEpoch);
			if (bodyOrbit != null)
			{
				return new NavPOI(bodyOrbit)
				{
					fTargetFuture = this.dEpoch - StarSystem.fEpoch
				};
			}
		}
		double sx;
		double sy;
		this.CanvasToSolar((double)canvasPos.x, (double)canvasPos.y, out sx, out sy);
		return new NavPOI(sx, sy);
	}

	private void LockTarget(NavPOI poi)
	{
		if (poi == null)
		{
			return;
		}
		GUIOrbitDraw.CrossHairTarget = poi;
		GUIOrbitDraw.CrossHairTarget.GetSXY(out this.dDragStartSX, out this.dDragStartSY);
		this.SelectTravelDropDown(GUIOrbitDraw.CrossHairTarget.name);
		if (!this.initNoAudio)
		{
			AudioManager.am.PlayAudioEmitter("ShipUINSMapPan04", false, false);
		}
		if (this.GetKnobFollowState == 2)
		{
			this.follow = GUIOrbitDraw.CrossHairTarget;
			this.SetOldFollow();
		}
	}

	public void LockTarget(string regID)
	{
		ShipDraw shipDraw = this.FindShipDraw(regID);
		if (shipDraw == null)
		{
			return;
		}
		NavPOI navPOI = new NavPOI(shipDraw, this.GetNavStationShip(), this.ShipPropMap);
		base.SetPropMapData("nFollow", 2.ToString());
		GUIOrbitDraw.crossHairInfo = ShipInfo.GetShipInfo(this.GetNavStationShip(), navPOI.Ship, this.ShipPropMap);
		this.LockTarget(navPOI);
	}

	public bool IsTargetKnown(string strRegID)
	{
		if (string.IsNullOrEmpty(strRegID))
		{
			return false;
		}
		ShipInfo shipInfo = ShipInfo.GetShipInfo(this.GetNavStationShip(), CrewSim.system.GetShipByRegID(strRegID), this.ShipPropMap);
		return shipInfo != null && shipInfo.Known;
	}

	private void SelectTravelDropDown(string strTargetID)
	{
		if (strTargetID == null)
		{
			return;
		}
		for (int i = 0; i < this.ddTravel.options.Count; i++)
		{
			if (this.ddTravel.options[i].text == strTargetID)
			{
				this.ddTravel.value = i;
			}
		}
	}

	private void UpdateWASDCluster()
	{
		if (this.dictWASD == null)
		{
			this.dictWASD = new Dictionary<string, GUIBtnPressHold>();
		}
		this.UpdateWASDBtn("W", "Controls/Container/prefabPnlWASD/bmpKeyW");
		this.UpdateWASDBtn("S", "Controls/Container/prefabPnlWASD/bmpKeyS");
		this.UpdateWASDBtn("A", "Controls/Container/prefabPnlWASD/bmpKeyA");
		this.UpdateWASDBtn("D", "Controls/Container/prefabPnlWASD/bmpKeyD");
		this.UpdateWASDBtn("Q", "Controls/Container/prefabPnlWASD/bmpKeyQ");
		this.UpdateWASDBtn("E", "Controls/Container/prefabPnlWASD/bmpKeyE");
		this.UpdateWASDBtn("+", "Controls/Container/prefabPnlWASD/bmpKeyPlus");
		this.UpdateWASDBtn("-", "Controls/Container/prefabPnlWASD/bmpKeyMinus");
		GUIOrbitDraw.strQEKey = GUIActionKeySelector.commandShipAttitude.KeyName;
		this.UpdateWASDBtn(GUIOrbitDraw.strQEKey, "Controls/Container/prefabPnlWASD/bmpKeyQE");
		GUIOrbitDraw.bUpdateWASD = false;
	}

	private void UpdateWASDBtn(string strKey, string strBtn)
	{
		if (strKey == null || strBtn == null)
		{
			return;
		}
		Transform transform = base.transform.Find(strBtn);
		if (transform != null)
		{
			GUIBtnPressHold component = transform.GetComponent<GUIBtnPressHold>();
			if (component != null)
			{
				this.dictWASD[strKey] = component;
			}
			Button component2 = transform.GetComponent<Button>();
			AudioManager.AddBtnAudio(component2.gameObject, "ShipUIBtnNSDockSysClampIn", "ShipUIBtnNSDockSysClampOut");
		}
	}

	private void ScrollLog(float fAmount)
	{
		float verticalNormalizedPosition = Mathf.Clamp(this.srLog.verticalNormalizedPosition + fAmount * this.srLog.verticalScrollbar.size, 0f, 1f);
		this.srLog.verticalNormalizedPosition = verticalNormalizedPosition;
		AudioManager.am.PlayAudioEmitter("ShipUINSMapPan02", false, false);
	}

	private void MouseHandler()
	{
		Vector2 v;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectDisplayPanel, Input.mousePosition, base.GetComponentInParent<Canvas>().worldCamera, out v);
		v.x = (v.x - this.rectDisplayPanel.rect.x) * 512f / this.rectDisplayPanel.rect.width;
		v.y = (v.y - this.rectDisplayPanel.rect.y) * 512f / this.rectDisplayPanel.rect.height;
		Camera component = GUIRenderTargets.goLines.transform.parent.parent.Find("CameraOrbitDraw").GetComponent<Camera>();
		if (component == null)
		{
			return;
		}
		Vector3 vector = component.ScreenToWorldPoint(v);
		RectTransform component2 = this.rectDrawPanel;
		if (this.cgStatus.alpha > 0f)
		{
			component2 = this.cgStatus.GetComponent<RectTransform>();
		}
		Vector3[] array = new Vector3[4];
		component2.GetWorldCorners(array);
		Vector2 canvasPos;
		canvasPos.x = (vector.x - array[0].x) / (array[2].x - array[0].x) * component2.rect.width;
		canvasPos.y = (vector.y - array[0].y) / (array[2].y - array[0].y) * component2.rect.height;
		bool flag = 0f <= canvasPos.x && canvasPos.x <= component2.rect.width && 0f <= canvasPos.y && canvasPos.y <= component2.rect.height;
		if (this.GetKnobFollowState == 0)
		{
			double sx;
			double sy;
			if (flag)
			{
				this.CanvasToSolar((double)canvasPos.x, (double)canvasPos.y, out sx, out sy);
			}
			else
			{
				this.CanvasToSolar((double)(this.rectDrawPanel.rect.width * 0.5f), (double)(this.rectDrawPanel.rect.height * 0.5f), out sx, out sy);
			}
			this.follow = new NavPOI(sx, sy);
		}
		if (Input.GetMouseButtonDown(0) && !this.bNoteOpen)
		{
			this.fTimeMouseDown = Time.realtimeSinceStartup;
			this.bDragValid = flag;
			this.CanvasToSolar((double)canvasPos.x, (double)canvasPos.y, out this.dDragStartSX, out this.dDragStartSY);
			this.dDragStartCX = (double)canvasPos.x;
			this.dDragStartCY = (double)canvasPos.y;
		}
		else if (Input.GetMouseButtonUp(0) && !this.bNoteOpen)
		{
			if (!this.bNoteOpen && flag && Time.realtimeSinceStartup - this.fTimeMouseDown < 0.25f)
			{
				NavPOI navPOI = this.FindNearestNavPOI(canvasPos, 8f);
				this.LockTarget(navPOI);
				CondOwner selectedCrew = CrewSim.GetSelectedCrew();
				if (navPOI.Ship != null && selectedCrew != null)
				{
					GUIOrbitDraw.SelectedShipDraw.Invoke(navPOI.Ship.strRegID);
				}
			}
		}
		else if (Input.GetMouseButton(0) && this.bDragValid)
		{
			if (this.cgStatus.alpha > 0f)
			{
				if ((double)canvasPos.y - this.dDragStartCY > 0.0)
				{
					this.ScrollLog(this.fLogScrollRate);
				}
				else if ((double)canvasPos.y - this.dDragStartCY < 0.0)
				{
					this.ScrollLog(-this.fLogScrollRate);
				}
			}
			else
			{
				double num;
				double num2;
				this.SolarToCanvas(this.dDragStartSX, this.dDragStartSY, out num, out num2);
				double num3 = num - (double)canvasPos.x;
				double num4 = num2 - (double)canvasPos.y;
				float num5 = 0.5f;
				float num6 = 3.75f;
				this.fVelocityX *= 0.5f;
				this.fVelocityY *= 0.5f;
				this.fVelocityX += (float)(num3 * (double)num6);
				this.fVelocityY += (float)(num4 * (double)num6);
				this.PanCanvasImmediateC(num3 * (double)num5, num4 * (double)num5);
			}
		}
		if (Input.GetMouseButton(1) && flag)
		{
			this.autoPilotDest = this.FindNearestNavPOI(canvasPos, 8f);
		}
		if (Input.GetMouseButtonDown(2))
		{
			this.dLastMiddleX = (double)canvasPos.x;
		}
		else if (Input.GetMouseButton(2))
		{
			this.fVelocityYaw += (float)((double)canvasPos.x - this.dLastMiddleX) * 0.05f;
			this.dLastMiddleX = (double)canvasPos.x;
		}
		if (flag)
		{
			if (Input.mouseScrollDelta.y != 0f)
			{
				this.fZoomTimer = -1f;
			}
			this.fVelocityZ -= Input.mouseScrollDelta.y * -0.3f;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				this.fVelocityZ -= Input.mouseScrollDelta.y * -1.7f;
			}
		}
		if (Input.mouseScrollDelta.y > 0f)
		{
			this.ScrollLog(this.fLogScrollRate);
		}
		else if (Input.mouseScrollDelta.y < 0f)
		{
			this.ScrollLog(-this.fLogScrollRate);
		}
	}

	public bool PlayerThrusting { get; private set; }

	public bool HoldingThrustActive
	{
		get
		{
			return this.ledWLock != null && this.ledWLock.State == 3;
		}
	}

	private void KeyHandler()
	{
		if (this.IsPDANav || CrewSim.Typing)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		int nNoiseOnly = 0;
		bool playerThrusting = this.PlayerThrusting;
		this.PlayerThrusting = false;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		if (CrewSim.TimeElapsedScaled() > 0f && this.bRCS)
		{
			if (GUIActionKeySelector.commandFlyUp.Held || this.dictWASD["W"].bPressed)
			{
				num2 += 1f;
				this.PlayerThrusting = true;
				flag = true;
			}
			if (GUIActionKeySelector.commandFlyDown.Held || this.dictWASD["S"].bPressed)
			{
				num2 -= 1f;
				this.PlayerThrusting = true;
				flag = true;
			}
			if (GUIActionKeySelector.commandFlyLeft.Held || this.dictWASD["A"].bPressed)
			{
				num -= 1f;
				this.PlayerThrusting = true;
			}
			if (GUIActionKeySelector.commandFlyRight.Held || this.dictWASD["D"].bPressed)
			{
				num += 1f;
				this.PlayerThrusting = true;
			}
			flag2 = (GUIActionKeySelector.commandShipCCW.Held || this.dictWASD["Q"].bPressed || this.dictWASD[GUIOrbitDraw.strQEKey].bPressed);
			flag3 = (GUIActionKeySelector.commandShipCW.Held || this.dictWASD["E"].bPressed || this.dictWASD[GUIOrbitDraw.strQEKey].bPressed);
			if (GUIActionKeySelector.commandShipAttitude.Held || (flag2 && flag3))
			{
				if (this.COSelf.ship.objSS.fW > 0f)
				{
					num3 = -1f;
				}
				else if (this.COSelf.ship.objSS.fW < 0f)
				{
					num3 = 1f;
				}
				this.PlayerThrusting = true;
				flag2 = true;
				flag3 = true;
			}
			else if (flag2)
			{
				num3 += 1f;
				this.PlayerThrusting = true;
			}
			else if (flag3)
			{
				num3 -= 1f;
				this.PlayerThrusting = true;
			}
			if (this.PlayerThrusting)
			{
				if (!this.bClaimed)
				{
					this.bClaimed = true;
					CondOwner selectedCrew = CrewSim.GetSelectedCrew();
					if (selectedCrew != null)
					{
						selectedCrew.ClaimShip(this.COSelf.ship.strRegID);
						if (this.COSelf.ship.objSS.bBOLocked && !this.COSelf.ship.objSS.bIsBO)
						{
							bool flag4 = false;
							foreach (Ship ship in this.COSelf.ship.GetAllDockedShips())
							{
								if (ship != null && !ship.bDestroyed)
								{
									if (ship.objSS.bIsBO)
									{
										flag4 = true;
										break;
									}
								}
							}
							if (!flag4)
							{
								this.COSelf.ship.objSS.UnlockFromBO();
							}
						}
					}
				}
				if (num2 == 0f && this.HoldingThrustActive)
				{
					num2 = 1f;
				}
				if (this.COSelf.ship.objSS.bBOLocked || this.COSelf.ship.objSS.bIsBO)
				{
					nNoiseOnly = 1;
					num2 = (num = (num3 = 0f));
				}
				if (this.COSelf.ship.IsDocked() && !this.COSelf.ship.TowBraceSecured())
				{
					this.CGClampWarning("GUI_ORBIT_WARN_CLAMP", false);
				}
				base.SetPropMapData("chkEngage", false.ToString());
			}
		}
		else if (CrewSim.Paused && this.bRCS)
		{
			bool flag5 = false;
			if (GUIActionKeySelector.commandFlyUp.Held || this.dictWASD["W"].bPressed)
			{
				flag5 = true;
			}
			if (GUIActionKeySelector.commandFlyDown.Held || this.dictWASD["S"].bPressed)
			{
				flag5 = true;
			}
			if (GUIActionKeySelector.commandFlyLeft.Held || this.dictWASD["A"].bPressed)
			{
				flag5 = true;
			}
			if (GUIActionKeySelector.commandFlyRight.Held || this.dictWASD["D"].bPressed)
			{
				flag5 = true;
			}
			bool flag6 = GUIActionKeySelector.commandShipCCW.Held || this.dictWASD["Q"].bPressed || this.dictWASD[GUIOrbitDraw.strQEKey].bPressed;
			bool flag7 = GUIActionKeySelector.commandShipCW.Held || this.dictWASD["E"].bPressed || this.dictWASD[GUIOrbitDraw.strQEKey].bPressed;
			if (GUIActionKeySelector.commandShipAttitude.Held || flag6 || flag7)
			{
				flag5 = true;
			}
			if (flag5)
			{
				this.CGClampWarning("GUI_ORBIT_WARN_PAUSED", false);
			}
		}
		if (GUIActionKeySelector.commandShipLockW.Down)
		{
			this.ToggleHoldThrust(this.ledWLock.State != 3);
		}
		else if (this.ledWLock.State == 3 && flag)
		{
			this.ToggleHoldThrust(false);
		}
		num3 /= Time.timeScale;
		bool flag8 = false;
		if (this.COSelf.ship.objSS.fW == 0f)
		{
			flag8 = (num3 * this.fPreviousSpin < 0f);
		}
		bool flag9 = false;
		if (this.COSelf.ship.IsDocked() && num == 0f && num2 == 0f)
		{
			flag9 = this.COSelf.ship.GetAllDockedShips().Any((Ship dShip) => dShip != null && dShip.bTowBraceSecured);
			if (flag9 && playerThrusting && !this.PlayerThrusting)
			{
				this.COSelf.ship.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
			}
		}
		bool propMapData = base.GetPropMapData("chkEngage", false);
		bool flag10 = this.chkStationKeeping.isOn && !this.PlayerThrusting;
		if (!this.PlayerThrusting && playerThrusting)
		{
			flag10 = false;
		}
		bool flag11 = this.HoldingThrustActive && !this.PlayerThrusting;
		if (propMapData || flag9 || flag10 || flag11 || this.PlayerThrusting)
		{
			if (!this.PlayerThrusting || (num3 == 0f && (!flag2 || !flag3)) || num != 0f || num2 != 0f)
			{
				this.ToggleOrbitalMode(false);
			}
		}
		if (!propMapData && !flag9 && !flag10 && !flag11)
		{
			string s;
			int engineMode = (!this.dictPropMap.TryGetValue("nKnobEngineMode", out s)) ? 1 : int.Parse(s);
			float num4 = (!this.dictPropMap.TryGetValue("slidThrottle", out s)) ? 0.25f : float.Parse(s);
			this.COSelf.ship.Maneuver(num * num4, num2 * num4, (!flag8) ? (num3 * num4) : 0f, nNoiseOnly, CrewSim.TimeElapsedScaled(), (Ship.EngineMode)engineMode);
			if (!this.PlayerThrusting && playerThrusting && !this.chkStationKeeping.isOn)
			{
				this.ToggleOrbitalMode(true);
			}
		}
		if (this.fPreviousSpin * num3 >= 0f)
		{
			this.fPreviousSpin = this.COSelf.ship.objSS.fW;
		}
		if (this.PlayerThrusting && this._hasNotSeenRefuelingTutorial)
		{
			this.CheckRefuelingTutorial();
		}
	}

	private void CheckRefuelingTutorial()
	{
		if (this.fRemass <= 0f || (double)this.fRemass > (double)this.fRCSMax * 0.25)
		{
			return;
		}
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		if (selectedCrew == null)
		{
			return;
		}
		MonoSingleton<ObjectiveTracker>.Instance.AddObjective(new Objective(selectedCrew, "Low fuel! Refuel your ship", "TIsTutorialRefuelComplete")
		{
			strDisplayDesc = "Dock with a station and refuel your ship at a refueling kiosk",
			strDisplayDescComplete = "Ship refueled",
			CTFocus = DataHandler.GetCondTrigger("TIsRefuelKiosk"),
			bTutorial = true
		});
		selectedCrew.AddCondAmount("TutorialRefuelStart", 1.0, 0.0, 0f);
		this._hasNotSeenRefuelingTutorial = false;
	}

	public void CGClampWarning(string strMessageName, bool bSkipDH = false)
	{
		string text = (!bSkipDH) ? DataHandler.GetString(strMessageName, false) : strMessageName;
		this.cgClampWarning.GetComponentInChildren<TMP_Text>().text = text;
		this.cgClampWarning.alpha = 1f;
		this.fClampDisengageWarningTimer = 5f;
		AudioManager.am.PlayAudioEmitter("ShipUIBtnNSProxWarn", false, false);
	}

	public static CondOwner GenerateNavDataCO(CondOwner coDevice)
	{
		CondOwner condOwner = DataHandler.GetCondOwner("DataBINNavStationData");
		condOwner.mapGUIPropMaps["DataBINNAV"] = new Dictionary<string, string>();
		if (coDevice == null)
		{
			return condOwner;
		}
		GUIOrbitDraw.RevealStartingVessels(coDevice);
		Dictionary<string, string> dictionary;
		if (coDevice.mapGUIPropMaps.TryGetValue("Panel A", out dictionary))
		{
			foreach (KeyValuePair<string, string> keyValuePair in dictionary)
			{
				if (keyValuePair.Key.IndexOf("Contact_") == 0)
				{
					condOwner.mapGUIPropMaps["DataBINNAV"][keyValuePair.Key] = keyValuePair.Value;
				}
			}
		}
		return condOwner;
	}

	public static void ImportNavDataCO(CondOwner coDevice, CondOwner coNavData)
	{
		if (coDevice == null || coNavData == null)
		{
			return;
		}
		Dictionary<string, string> dictionary;
		if (!coNavData.mapGUIPropMaps.TryGetValue("DataBINNAV", out dictionary))
		{
			return;
		}
		Dictionary<string, string> guipropMap;
		if (!coDevice.mapGUIPropMaps.TryGetValue("Panel A", out guipropMap))
		{
			guipropMap = DataHandler.GetGUIPropMap("NavStation");
			coDevice.mapGUIPropMaps["Panel A"] = guipropMap;
		}
		foreach (KeyValuePair<string, string> keyValuePair in dictionary)
		{
			if (keyValuePair.Key.IndexOf("Contact_") == 0)
			{
				coDevice.mapGUIPropMaps["Panel A"][keyValuePair.Key] = keyValuePair.Value;
			}
		}
	}

	private static void RevealStartingVessels(CondOwner coDevice)
	{
		if (coDevice == null || !coDevice.HasCond("IsReadyNAVReveals"))
		{
			return;
		}
		coDevice.ZeroCondAmount("IsReadyNAVReveals");
		if (coDevice.ship == null || coDevice.ship.DMGStatus == Ship.Damage.New)
		{
			return;
		}
		Dictionary<string, string> guipropMap;
		if (!coDevice.mapGUIPropMaps.TryGetValue("Panel A", out guipropMap))
		{
			guipropMap = DataHandler.GetGUIPropMap("NavStation");
			coDevice.mapGUIPropMaps["Panel A"] = guipropMap;
		}
		foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
		{
			if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) <= 0.1)
			{
				ShipInfo si = new ShipInfo(ship, true);
				ShipInfo.SetShipInfo(si, guipropMap);
			}
		}
	}

	public bool NoteShowing
	{
		get
		{
			return this.bNoteOpen;
		}
	}

	public static GUIOrbitDraw Instance;

	public static NavModMessageEvent NavModMessageEvent = new NavModMessageEvent();

	public double fModTimeDiff = 1.0;

	public static readonly float fLineWidth = 1.5f;

	private const string NAV_DATA_KEY = "DataBINNAV";

	public static UpdateShipSelectionEvent UpdateShipSelection;

	public Texture texLine01;

	public Texture texLine02;

	public Texture texLine03;

	public Texture texLine04;

	public Texture texLine05;

	private double dCanvasSolarXX;

	private double dCanvasSolarXY;

	private double dOffsetSX;

	private double dOffsetSY;

	public NavPOI follow;

	private NavPOI autoPilotDest;

	public static ShipInfo crossHairInfo;

	private double fEpochAutopilotUpdate;

	public double dFollowOffsetSX;

	public double dFollowOffsetSY;

	private double oldFollowCX;

	private double oldFollowCY;

	private double dDragStartCX;

	private double dDragStartCY;

	private double dDragStartSX;

	private double dDragStartSY;

	private bool bDragValid;

	private double dLastMiddleX;

	private float fVelocityX;

	private float fVelocityY;

	public float fVelocityYaw;

	public float fVCRS;

	public double fVRel;

	public float fBRG;

	public double dRNG;

	private float fRemass;

	private float fRCSMax;

	public float fVelocityZ;

	public double dMagTarget = 1.2E+18;

	public float fZoomTimer;

	private double dScopeRadius = 1.0;

	private float fTimeMouseDown;

	public FlightPlan livePlan;

	private double dEpoch;

	public double fEpochStationBegin;

	public float fLogScrollRate = 0.1f;

	public float fTimeFuture = 1f;

	public float fTimeFutureTarget = 1f;

	private List<DebugDraw> aDebugDraws = new List<DebugDraw>();

	public Vector3 vCanvasOffset;

	[SerializeField]
	private Button btnDone;

	[SerializeField]
	private Button btnRescue;

	[SerializeField]
	private Button btnScrew01;

	[SerializeField]
	private Button btnScrew02;

	[SerializeField]
	private Button btnScrew03;

	[SerializeField]
	private Button btnScrew04;

	private StarSystem objSystem;

	private ShipSitu objSSEngage;

	private ShipSitu ssTemp;

	private GameObject goOrbitPanel;

	private CanvasGroup cgStatus;

	private Toggle chkNavMode;

	private UnityAction<bool> eEngage;

	private TextMeshProUGUI txtSide;

	private Text txtTimeUTC;

	private TMP_Text txtRange;

	private Transform tfPanelIn;

	private Transform tfOrbitLabel;

	private TMP_Dropdown ddTravel;

	private Button btnTravel;

	[SerializeField]
	public Toggle chkStationKeeping;

	private GUILamp ledWLock;

	[SerializeField]
	private Button btnNote;

	private ScrollRect srLog;

	private CanvasGroup cgNag;

	private TMP_Text txtNagTimer;

	private Dictionary<string, GUIBtnPressHold> dictWASD;

	private bool bRCS = true;

	public bool bShowNWZ;

	private float fPreviousSpin;

	private bool bShowMapProjs = true;

	private bool bNoteOpen = true;

	private bool bNoteAnimating;

	private bool initNoAudio;

	private bool bClaimed;

	private double fEpochNagEnd;

	private int nProjSteps = 1;

	private int nProjStepsSelf = 5;

	private const int nSegmentsBody = 64;

	private const int nSamples = 64;

	public float fMinOrbitDiam = 12f;

	private const double VIS_RANGE_TORCH = 1500000000.0;

	public const double VIS_RANGE_RCS = 20000.0;

	private const double VIS_RANGE_DEAD = 2000.0;

	public static readonly int DERELICTSIZE = 300;

	private Color clrText = new Color(0.46484375f, 0.99609375f, 1f, 1f);

	private Color clrShipOrbit = new Color(0.34765625f, 0.46875f, 0.5294118f, 0.25f);

	public static Color clrBlue01 = new Color(0.1484375f, 0.57421875f, 0.984375f, 0.9f);

	private Color clrBlue01Half = new Color(0.1484375f, 0.57421875f, 0.984375f, 0.45f);

	private Color clrBlue02 = new Color(0.07421875f, 0.28515625f, 0.4921875f, 0.9f);

	private Color clrGreen01 = new Color(0.2890625f, 0.99609375f, 0.6953125f, 0.9f);

	private Color clrGreen01Half = new Color(0.2890625f, 0.99609375f, 0.6953125f, 0.45f);

	private Color clrGreen02 = new Color(0.5f, 0.890625f, 0.609375f, 0.9f);

	public static Color clrWhite01 = new Color(0.7421875f, 0.7421875f, 0.7421875f, 0.9f);

	public static Color clrWhite02 = new Color(0.46875f, 0.46875f, 0.46875f, 0.9f);

	public Color clrRed01 = new Color(0.94921875f, 0.24609375f, 0.1796875f, 0.9f);

	private Color clrRed02 = new Color(0.234375f, 0.05859375f, 0.04296875f, 0.9f);

	public static Color clrOrange01 = new Color(0.99609375f, 0.703125f, 0f, 0.9f);

	private Color clrOrange01Half = new Color(0.99609375f, 0.703125f, 0f, 0.45f);

	private Color clrOrange02Half = new Color(0.99609375f, 0.40625f, 0f, 0.45f);

	public static Color clrHauler = new Color(0.609375f, 0.53125f, 0.38671875f, 1f);

	public static Color clrLocalAuthority = new Color(0.78125f, 0.3125f, 0.1171875f, 0.9f);

	private List<VectorLine> aUIs;

	private List<ShipDraw> aShipDraws;

	private List<BODraw> aBODraws;

	public ShipDraw sdNS;

	private BODraw boMainOccluder;

	private VectorLine lineCross;

	private VectorLine lineStationKeepingTarget;

	private VectorLine lineCourse;

	private VectorLine lineGrav;

	private RectTransform rectDrawPanel;

	private RectTransform rectDisplayPanel;

	private RectTransform rtLines;

	private StringBuilder sb = new StringBuilder();

	public CanvasGroup cgClampWarning;

	public float fClampDisengageWarningTimer;

	public static bool bUpdateWASD;

	private static string strQEKey = "Q+E";

	private List<string> _playerOwnedShips = new List<string>();

	public NavData navPlan;

	private Dictionary<string, string> _shipPropMap;

	private Coroutine _nwzRoutine;

	private Vector3 noteLowered = new Vector3(252f, -383f);

	private Vector3 noteRaised = new Vector3(-299f, 123.3f);

	private Vector3 eulerLowered = new Vector3(0f, 0f, -6.37f);

	public static UnityEvent OpenedNavStationUI = new UnityEvent();

	public static UnityEventString SelectedShipDraw = new UnityEventString();

	public static UnityAction<double> OnZoom;

	private static Color[] colorArray = new Color[]
	{
		new Color(1f, 0f, 0f),
		Color.yellow,
		Color.blue,
		Color.green,
		Color.magenta,
		new Color(0.5019608f, 0.3529412f, 0f),
		new Color(0.5019608f, 0.5019608f, 0f),
		new Color(0.5882353f, 0.39215687f, 0.5882353f),
		new Color(0f, 0.5019608f, 0.5019608f),
		new Color(0.54509807f, 0f, 0.54509807f),
		new Color(1f, 0.5019608f, 0f),
		new Color(1f, 0.078431375f, 0.5764706f),
		new Color(0.8039216f, 0.52156866f, 0.24705882f),
		new Color(0.4392157f, 0.5019608f, 0.5647059f)
	};

	public double fEpochCycleSafetyBegin;

	public List<Bounds> boundsAddedThisFrame = new List<Bounds>();

	public Dictionary<Bounds, List<ShipDraw>> boundsToRects = new Dictionary<Bounds, List<ShipDraw>>();

	public List<ShipDraw> VisibleShipDraws = new List<ShipDraw>();

	public List<ShipDraw> OverlappingShipDraws = new List<ShipDraw>();

	private double _holdthrustTimeStamp;

	private bool _hasNotSeenRefuelingTutorial = true;
}
