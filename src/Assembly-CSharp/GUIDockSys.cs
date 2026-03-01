using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Objectives;
using Ostranauts.ShipGUIs.MFD;
using Ostranauts.ShipGUIs.NavStation;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Docking computer UI for ship approach and clamp alignment.
// This appears to be the in-cockpit instrument that reads ship comms
// clearance, computes VREL/RNG/BRG guidance, and manages docking clamps.
public class GUIDockSys : GUIData
{
	// Picks the current docking target from either the docked partner or ATC clearance.
	private string DockingTarget
	{
		get
		{
			Ship ship = this.COSelf.ship;
			if (ship.bDocked)
			{
				return ship.GetAllDockedShips().First<Ship>().strRegID;
			}
			return (ship.Comms.Clearance == null) ? null : ship.Comms.Clearance.TargetRegId;
		}
	}

	// Prevents accidental docking with ships other than the current clearance target.
	public bool HasActiveDockingProtection(string collidingShip)
	{
		return this.bActive && !(this.COSelf == null) && this.COSelf.ship != null && !this.COSelf.ship.bDocked && this.COSelf.ship.Comms.Clearance != null && !(this.COSelf.ship.Comms.Clearance.TargetRegId == collidingShip);
	}

	// True when ATC or docking comms has granted a live clearance target.
	private bool Cleared
	{
		get
		{
			return this.COSelf.ship != null && this.COSelf.ship.Comms.Clearance != null;
		}
	}

	// Caches UI widgets and localized strings after the GUI is instantiated.
	protected override void Awake()
	{
		GUIDockSys.instance = this;
		base.Awake();
		this.rectRingInner = GUIRenderTargets.goDockSys.transform.Find("bmpDockRingInner").GetComponent<RectTransform>();
		this.rectAlignedShip = GUIRenderTargets.goDockSys.transform.parent.Find("goAlignedShip").GetComponent<RectTransform>();
		this.rectShipLine = GUIRenderTargets.goDockSys.transform.parent.Find("goShipLine").GetComponent<RectTransform>();
		this.imgShipLine = this.rectShipLine.GetComponent<Image>();
		this.rectRotateShip = this.rectAlignedShip.transform.Find("goRotateShip").GetComponent<RectTransform>();
		this.rectVCRS = GUIRenderTargets.goDockSys.transform.parent.Find("goCrossMarkers/VCRS").GetComponent<RectTransform>();
		this.imgVCRS = this.rectVCRS.GetComponent<Image>();
		this.rectBearing = GUIRenderTargets.goDockSys.transform.parent.Find("goCrossMarkers/BEARING").GetComponent<RectTransform>();
		this.imgBearing = this.rectBearing.GetComponent<Image>();
		this.goClamps = GUIRenderTargets.goDockSys.transform.Find("goClamps").gameObject;
		this.goClamps.SetActive(false);
		this.cgNoATCUplink = GUIRenderTargets.goDockSys.transform.parent.Find("pnlNoTarget").GetComponent<CanvasGroup>();
		this.cgNoATCUplink.alpha = 0f;
		this.cgNoClearance = GUIRenderTargets.goDockSys.transform.Find("pnlNoClearance").GetComponent<CanvasGroup>();
		this.cgNoClearance.alpha = 0f;
		this.txtWarning = this.cgNoClearance.transform.Find("txtDockNoClearance").GetComponent<TMP_Text>();
		this.cgWrongWay = GUIRenderTargets.goDockSys.transform.Find("pnlWrongWay").GetComponent<CanvasGroup>();
		this.cgWrongWay.alpha = 0f;
		TMP_Text component = this.cgWrongWay.transform.Find("txt").GetComponent<TMP_Text>();
		component.text = DataHandler.GetString("GUI_DOCKSYS_WRONGWAY", false);
		this.cgLeft = GUIRenderTargets.goDockSys.transform.Find("bmpLeft").GetComponent<CanvasGroup>();
		this.cgLeft.alpha = 0f;
		this.cgRight = GUIRenderTargets.goDockSys.transform.Find("bmpRight").GetComponent<CanvasGroup>();
		this.cgRight.alpha = 0f;
		this.rectLeft = this.cgLeft.GetComponent<RectTransform>();
		this.rectRight = this.cgRight.GetComponent<RectTransform>();
		this.eClamp = delegate(bool A_1)
		{
			this.ClampEngage(false);
		};
		this.chkEngage.onValueChanged.AddListener(this.eClamp);
		this.txtTimeUTC = GUIRenderTargets.goDockSys.transform.Find("pnlTitle/txtTime").GetComponent<TMP_Text>();
		this.txtRNGETA = GUIRenderTargets.goDockSys.transform.Find("pnlRNGETA/txtRNGETA").GetComponent<TMP_Text>();
		this.txtBRGVCRS = GUIRenderTargets.goDockSys.transform.Find("pnlBRGVCRS/txtBRGVCRS").GetComponent<TMP_Text>();
		this.txtError = GUIRenderTargets.goDockSys.transform.Find("pnlError/txtError").GetComponent<TMP_Text>();
		this.cgError = GUIRenderTargets.goDockSys.transform.Find("pnlError").GetComponent<CanvasGroup>();
		if (SceneManager.GetActiveScene().name != "CrewSim")
		{
			Debug.Log("ERROR: Test mode no longer supported in GUIDockSys, use CrewSim instead.");
			Debug.Break();
		}
		this.UpdateWASDCluster();
		this.imgVCRS.material.mainTextureOffset = new Vector2(0f, 0f);
		if (GUIDockSys.strNoClearance == null)
		{
			GUIDockSys.strNoClearance = DataHandler.GetString("GUI_DOCKSYS_NOCLEARANCE", false);
		}
		if (GUIDockSys.strTowBrace == null)
		{
			GUIDockSys.strTowBrace = DataHandler.GetString("GUI_DOCKSYS_TOW_BRACE", false);
		}
		GUIDockSys.strNoEmptyPort = DataHandler.GetString("GUI_DOCKSYS_NODOCKINGPORT", false);
	}

	// Per-frame instrument update.
	// Reads the target ship, updates relative motion data, and redraws docking
	// guidance markers while the panel is active.
	private void Update()
	{
		if (!this.bActive)
		{
			return;
		}
		if (this.COSelf.HasCond("IsDamagedSoftware"))
		{
			this.PrintErrors();
			return;
		}
		CanvasManager.HideCanvasGroup(this.cgError);
		bool flag = (int)Time.realtimeSinceStartup % 2 == 0;
		this.bUplinkMissing = true;
		if (this.fClampDisengageGraceEnded - StarSystem.fEpoch > 0.0)
		{
			this.bUplinkMissing = false;
		}
		else
		{
			this.strRegIDClampGrace = null;
		}
		if ((this.COSelf.ship != null && this.COSelf.ship.bDocked) || (this.COSelf.ship != null && this.COSelf.ship.Comms.Clearance != null))
		{
			this.bUplinkMissing = false;
		}
		if (GUIOrbitDraw.bUpdateWASD)
		{
			this.UpdateWASDCluster();
		}
		this.KeyHandler();
		this.objSS.TimeAdvance((double)CrewSim.TimeElapsedScaled(), false);
		Ship ship = null;
		if (this.DockingTarget != null && CrewSim.system.dictShips.TryGetValue(this.DockingTarget, out ship))
		{
			if (this.bLocked)
			{
				this.DockAlign(true);
			}
			else
			{
				this.ConvertShipToRing(ship);
			}
		}
		else if (!string.IsNullOrEmpty(this.strRegIDClampGrace) && CrewSim.system.dictShips.TryGetValue(this.strRegIDClampGrace, out ship))
		{
			this.ConvertShipToRing(ship);
		}
		string text = StarSystem.sUTCEpoch;
		string text2 = "No Target Selected";
		string text3 = "No Target Selected";
		if (ship != null)
		{
			this.COSelf.ship.UnlockFromOrbit(true);
			text += "\n";
			double num = ship.objSS.vPosx - this.COSelf.ship.objSS.vPosx;
			double num2 = ship.objSS.vPosy - this.COSelf.ship.objSS.vPosy;
			double num3 = ship.objSS.vVelX - this.COSelf.ship.objSS.vVelX;
			double num4 = ship.objSS.vVelY - this.COSelf.ship.objSS.vVelY;
			float collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(this.COSelf.ship, ship);
			Color color = this.clrText;
			double magnitude = MathUtils.GetMagnitude(num3, num4);
			if (magnitude / 6.6845869117759804E-12 > CollisionManager.dMaxSafeV)
			{
				color = ((!flag) ? this.clrOrange01 : this.clrRed01);
			}
			text2 = MathUtils.ColorToColorTag(color);
			this.imgShipLine.color = color;
			text2 += "VREL ";
			text2 += MathUtils.GetDistUnits(magnitude);
			text2 += "</color>";
			text2 += "\n";
			text3 = "VCRS ";
			double num5 = 1.0 / MathUtils.GetMagnitude(num, num2);
			if (double.IsNaN(num5))
			{
				num5 = 0.0;
			}
			double num6 = (num * num4 - num2 * num3) * num5;
			text3 += MathUtils.GetDistUnits(num6);
			text3 += "\n";
			text2 += "RNG ";
			double num7 = MathUtils.GetMagnitude(num, num2) - (double)collisionDistanceAU;
			text2 += MathUtils.GetDistUnits(num7);
			text2 += "\n";
			double vPosx = ship.objSS.vPosx;
			double vPosy = ship.objSS.vPosy;
			float num8 = (float)(vPosx - this.COSelf.ship.objSS.vPosx);
			float num9 = (float)(vPosy - this.COSelf.ship.objSS.vPosy);
			float num10 = Mathf.Cos(-this.COSelf.ship.objSS.fRot);
			float num11 = Mathf.Sin(-this.COSelf.ship.objSS.fRot);
			float y = num8 * num10 - num9 * num11;
			float x = num8 * num11 + num9 * num10;
			float num12 = Mathf.Atan2(y, x) * 57.295776f;
			num12 = MathUtils.NormalizeAngleDegrees(num12);
			text3 = "BRG " + num12.ToString("#.00") + "°\n" + text3;
			double num13 = 1.0;
			if (Vector2.Dot(new Vector2((float)num3, (float)num4), new Vector2(num8, num9)) > 0f)
			{
				num13 = -1.0;
			}
			text2 = text2 + "ETA " + MathUtils.GetTimeNAV(num13 * num7 / magnitude);
			this.rectRotateShip.localRotation = Quaternion.Euler(0f, 0f, num12);
			this.imgBearing.material.mainTextureOffset = new Vector2(-num12 / 360f, 0f);
			Color color2 = Color.cyan;
			if (this.fObjPosX > 256f || this.fObjPosX < -256f)
			{
				color2 = ((!flag) ? this.clrOrange01 : this.clrRed01);
			}
			this.imgBearing.color = color2;
			float num14 = 1f;
			num14 *= Time.deltaTime * (float)num6 * 149597870f;
			if (num14 == float.NaN)
			{
				this.imgVCRS.material.mainTextureOffset = new Vector2(0f, 0f);
			}
			else
			{
				this.imgVCRS.material.mainTextureOffset += new Vector2(num14, 0f);
			}
			float num15 = 5f;
			float num16 = (float)(num7 * 149597872.0);
			if ((double)num16 <= 5.0)
			{
				if (num16 <= 0f)
				{
					num16 = 0f;
					this.imgVCRS.material.mainTextureOffset = new Vector2(0f, 0f);
				}
				num15 += (5f - num16) / 5f * 420f;
			}
			this.rectAlignedShip.offsetMax = new Vector2(-5f, num15 + 40f);
			this.rectAlignedShip.offsetMin = new Vector2(5f, num15);
			this.rectShipLine.offsetMax = new Vector2(0f, 465f);
			this.rectShipLine.offsetMin = new Vector2(0f, num15 + 40f);
			this.imgShipLine.material.mainTextureScale = new Vector2(1f, num16 / 5f);
		}
		this.txtTimeUTC.text = text;
		this.txtRNGETA.text = text2;
		this.txtBRGVCRS.text = text3;
		if (this.fObjScale <= 0.01f)
		{
			this.rectRingInner.gameObject.SetActive(false);
		}
		else if (!this.rectRingInner.gameObject.activeInHierarchy)
		{
			this.rectRingInner.gameObject.SetActive(true);
		}
		float y2 = 0f;
		this.rectRingInner.localPosition = new Vector3(this.fObjPosX, y2, 0f);
		this.rectRingInner.localScale = new Vector3(this.fObjScale, this.fObjScale, 1f);
		this.UpdateLamps(false);
		if (this.fClampDisengageWarningTimer > 0f)
		{
			this.fClampDisengageWarningTimer -= CrewSim.TimeElapsedUnscaled();
			if (this.fClampDisengageWarningTimer <= 0f)
			{
				this.cgNoClearance.alpha = 0f;
			}
		}
	}

	private void UpdateLamps(bool bOverride = false)
	{
		bool flag = (int)Time.realtimeSinceStartup % 2 == 0;
		bool flag2 = this.CanDock();
		if (bOverride || this.goClamps.activeInHierarchy != flag2)
		{
			this.goClamps.SetActive(flag2);
		}
		if (this.bLocked || bOverride)
		{
			this.bmpEngageDock.State = 3;
		}
		else
		{
			this.bmpEngageDock.State = 0;
		}
		if (flag2 || bOverride)
		{
			this.bmpClampAlign.State = 3;
		}
		else
		{
			this.bmpClampAlign.State = 0;
		}
		this.cgLeft.alpha = 0f;
		this.cgRight.alpha = 0f;
		if (this.bUplinkMissing)
		{
			this.cgNoATCUplink.alpha = 1f;
		}
		else
		{
			this.cgNoATCUplink.alpha = 0f;
			if (flag && this.Cleared)
			{
				if (this.fObjPosX > 256f)
				{
					this.cgRight.alpha = 1f;
					this.rectRight.localPosition = new Vector3(this.rectRight.localPosition.x, Convert.ToSingle(this.objSS.vPosy), this.rectRight.localPosition.z);
				}
				else if (this.fObjPosX < -256f)
				{
					this.cgLeft.alpha = 1f;
					this.rectLeft.localPosition = new Vector3(this.rectLeft.localPosition.x, Convert.ToSingle(this.objSS.vPosy), this.rectLeft.localPosition.z);
				}
			}
		}
		this.cgWrongWay.alpha = this.cgRight.alpha + this.cgLeft.alpha;
	}

	private void CGClampWarning(string strMessage)
	{
		this.cgNoClearance.alpha = 1f;
		this.fClampDisengageWarningTimer = 1.5f;
		this.txtWarning.text = strMessage;
		AudioManager.am.PlayAudioEmitter("ShipUIBtnDCNoClearance", false, false);
		if (strMessage == GUIDockSys.strNoClearance && Ledger.HasUnpaidDockingFees(this.DockingTarget, CrewSim.GetSelectedCrew()))
		{
			BeatManager.RunEncounter("ENCFirstUndockFees", true);
			if (CrewSim.coPlayer.HasCond("TutorialNoUndockFeesYet"))
			{
				JsonPersonSpec personSpec = DataHandler.GetPersonSpec("OKLGAdminPortAuthority");
				PersonSpec person = StarSystem.GetPerson(personSpec, null, false, null, null);
				if (person != null && person.GetCO() != null)
				{
					CondOwner co = person.GetCO();
					Objective objective = new Objective(co, "Visit OKLG Commercial Port Authority", "TNotTutorialNoUndockFeesYet");
					objective.strDisplayDesc = "Convince the clerk there to let you undock, or just pay your docking fees.";
					objective.bTutorial = true;
					MonoSingleton<ObjectiveTracker>.Instance.AddObjective(objective);
				}
			}
		}
	}

	private void PrintErrors()
	{
		if (CrewSim.TimeElapsedScaled() == 0f)
		{
			return;
		}
		if (this.cgError.alpha == 0f)
		{
			this.cgError.alpha = 1f;
			this.txtError.text = string.Empty;
		}
		TMP_Text tmp_Text = this.txtError;
		tmp_Text.text += DataHandler.GetString("GUI_ORBIT_ERROR", false);
		if (GUIMFDPageHost.OnRequestMFDChange != null)
		{
			GUIMFDPageHost.OnRequestMFDChange.Invoke(new MFDError());
		}
	}

	public void ShowTarget()
	{
		Ship ship = (!(this.COSelf != null)) ? null : this.COSelf.ship;
		if (ship != null && ship.bDocked)
		{
			if (!this.bLocked)
			{
				this.DockAlign(true);
				this.chkEngage.onValueChanged.RemoveListener(this.eClamp);
				this.chkEngage.isOn = true;
				this.chkEngage.onValueChanged.AddListener(this.eClamp);
				this.bLocked = true;
				this.UpdateLamps(true);
			}
		}
		else if (this.Cleared)
		{
			this.StartDockApproach(UnityEngine.Random.Range(0.3f, 0.6f));
		}
		else
		{
			this.objSS.fRot = -1f;
		}
	}

	private bool CanDock()
	{
		if (this.COSelf.ship.Comms.Clearance == null)
		{
			return false;
		}
		Ship ship = null;
		CrewSim.system.dictShips.TryGetValue(this.COSelf.ship.Comms.Clearance.TargetRegId, out ship);
		if (ship == null)
		{
			return false;
		}
		if (!this.COSelf.ship.bDocked && !ship.CanBeDockedWith())
		{
			if (this.cgNoClearance.alpha <= 0f)
			{
				this.CGClampWarning(GUIDockSys.strNoEmptyPort);
			}
			return false;
		}
		double dX = ship.objSS.vPosx - this.COSelf.ship.objSS.vPosx;
		double dY = ship.objSS.vPosy - this.COSelf.ship.objSS.vPosy;
		double magnitude = MathUtils.GetMagnitude(dX, dY);
		return magnitude <= (double)CollisionManager.GetCollisionDistanceAU(ship, this.COSelf.ship) * 1.1 && this.fObjPosX * this.fObjPosX < 100f && this.objSS.vPosy * this.objSS.vPosy < 100.0;
	}

	private void StartDockApproach(float fRange)
	{
		this.objSS.vVelX = (double)UnityEngine.Random.Range(-0.1f, 0.1f);
		this.objSS.fRot = fRange;
		this.objSS.fW = UnityEngine.Random.Range(0f, 0.03f);
	}

	private void ConvertShipToRing(Ship ship)
	{
		if (ship == null)
		{
			return;
		}
		double num = ship.objSS.vPosx - this.COSelf.ship.objSS.vPosx;
		double num2 = ship.objSS.vPosy - this.COSelf.ship.objSS.vPosy;
		double magnitude = MathUtils.GetMagnitude(num, num2);
		if (GUIDockSys.curveDockingRingSize == null)
		{
			GUIDockSys.curveDockingRingSize = Resources.Load<AnimationCurveAsset>("Curves/DockingRingSize");
		}
		float collisionDistanceAU = CollisionManager.GetCollisionDistanceAU(this.COSelf.ship, ship);
		float num3 = 6.684587E-08f - collisionDistanceAU;
		float num4 = (float)(magnitude - (double)collisionDistanceAU) / num3;
		num4 = Mathf.Clamp(num4, 0f, 1f);
		this.fObjScale = GUIDockSys.curveDockingRingSize.curve.Evaluate(1f - num4);
		float num5 = Mathf.Cos(this.COSelf.ship.objSS.fRot);
		float num6 = Mathf.Sin(this.COSelf.ship.objSS.fRot);
		double num7 = num * (double)num5 + num2 * (double)num6;
		double val = -num * (double)num6 + num2 * (double)num5;
		this.fObjPosX = (float)(1024.0 * num7 / Math.Max(val, 9.9999998245167E-15));
	}

	private void DockAlign(bool bCenter)
	{
		if (bCenter)
		{
			this.objSS.vVelX = 0.0;
			this.objSS.vVelY = 0.0;
			this.objSS.fW = 0f;
			this.objSS.vPosx = 0.0;
			this.objSS.vPosy = 0.0;
			this.objSS.fRot = 1f;
			this.fObjPosX = 0f;
			return;
		}
		this.objSS.vVelX = (double)UnityEngine.Random.Range(-0.1f, 0.1f);
		this.objSS.fW = 0.01f;
	}

	public void ForceUndock()
	{
		if (!this.bLocked)
		{
			return;
		}
		this.COSelf.ship.Comms.DebugCreateClearance(null);
		this.chkEngage.onValueChanged.RemoveListener(this.eClamp);
		this.chkEngage.isOn = false;
		this.ClampEngage(true);
		this.chkEngage.onValueChanged.AddListener(this.eClamp);
	}

	public void ForceDock(string strATC)
	{
		if (this.bLocked)
		{
			return;
		}
		this.DockAlign(true);
		this.COSelf.ship.Comms.DebugCreateClearance(strATC);
		this.chkEngage.isOn = true;
		this.ShowTarget();
	}

	private void ScheduleClearance(bool bValue, float fDelay)
	{
		base.StartCoroutine(this._ScheduleClearance(bValue, fDelay));
	}

	private IEnumerator _ScheduleClearance(bool bValue, float fDelay)
	{
		float fTime = 0f;
		while (fTime < fDelay)
		{
			fTime += CrewSim.TimeElapsedScaled();
			this.fObjPosX = 0f;
			yield return null;
		}
		this.COSelf.ship.shipScanTarget = null;
		yield break;
	}

	private IEnumerator TemporarilyLockClampButton()
	{
		this.chkEngage.interactable = false;
		yield return new WaitForSeconds(0.5f);
		this.chkEngage.interactable = true;
		yield break;
	}

	private void ClampEngage(bool forced = false)
	{
		if (this.COSelf.HasCond("IsDamagedSoftware"))
		{
			return;
		}
		base.StartCoroutine(this.TemporarilyLockClampButton());
		if (this.bLocked)
		{
			if (this.Cleared)
			{
				MonoSingleton<GUIMessageDisplay>.Instance.HidePanel();
				if (this.COSelf.ship.TowBraceSecured())
				{
					this.chkEngage.isOn = true;
					this.CGClampWarning(GUIDockSys.strTowBrace);
				}
				else
				{
					this.CheckForCrimeStolenShip();
					this.ScheduleUnDock(forced);
				}
			}
			else
			{
				this.chkEngage.isOn = true;
				this.CGClampWarning(GUIDockSys.strNoClearance);
			}
		}
		else if (this.DockingTarget != null && this.CanDock())
		{
			if (this.COSelf.ship.TowBraceSecured())
			{
				this.chkEngage.isOn = true;
				this.CGClampWarning(GUIDockSys.strTowBrace);
			}
			else
			{
				this.DockAlign(true);
				if (this.COSelf.ship.GetDockedShip(this.DockingTarget) == null)
				{
					CondOwner selectedCrew = CrewSim.GetSelectedCrew();
					if (selectedCrew != null)
					{
						selectedCrew.ZeroCondAmount("TutorialNavDockWithDerelictWaiting");
						selectedCrew.ZeroCondAmount("TutorialNavSeriesInProgress");
						MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(selectedCrew.strID);
						this.CheckForCrimeIllegalSalvagingOKLG(this.DockingTarget, selectedCrew);
					}
					this.ScheduleDock();
					GUIDockSys.DockEvent.Invoke(this.DockingTarget);
				}
			}
		}
		else
		{
			this.chkEngage.isOn = false;
		}
		this.bLocked = this.chkEngage.isOn;
		this.UpdateLamps(true);
	}

	private void ScheduleUnDock(bool forced)
	{
		base.StartCoroutine(this.Undock(forced));
	}

	private void ScheduleDock()
	{
		base.StartCoroutine(this.Dock());
	}

	private IEnumerator Dock()
	{
		Ship otherShip = CrewSim.system.GetShipByRegID(this.DockingTarget);
		if (otherShip != null && otherShip.IsStation(false))
		{
			yield return MonoSingleton<ScreenshotUtil>.Instance.CreateShipImages(this.COSelf.ship);
		}
		otherShip = CrewSim.DockShip(this.COSelf.ship, this.DockingTarget);
		BeatManager.AutoSaveBeforePirateEncounter(otherShip);
		if (GUIMFDPageHost.OnRequestMFDChange != null)
		{
			GUIMFDPageHost.OnRequestMFDChange.Invoke(new MFDDockInfo());
		}
		yield break;
	}

	private IEnumerator Undock(bool forced = false)
	{
		Ship objShipDocked = this.COSelf.ship.GetDockedShip(this.DockingTarget);
		this.strRegIDClampGrace = objShipDocked.strRegID;
		this.fClampDisengageGraceEnded = StarSystem.fEpoch + this.fDefaultGracePeriodAmount;
		if (!forced && objShipDocked != null && !objShipDocked.bDestroyed && !objShipDocked.IsStation(false))
		{
			yield return MonoSingleton<ScreenshotUtil>.Instance.CreateShipImages(objShipDocked);
		}
		this.COSelf.ship.UnlockFromOrbit(true);
		this.DockAlign(false);
		if (!this.COSelf.ship.aProxIgnores.Contains(objShipDocked.strRegID))
		{
			this.COSelf.ship.aProxIgnores.Add(objShipDocked.strRegID);
		}
		if (objShipDocked.objSS.bIsBO && objShipDocked.IsGroundStation())
		{
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(objShipDocked.objSS, StarSystem.fEpoch, false);
			if (nearestBO != null && !this.COSelf.ship.aProxIgnores.Contains(nearestBO.strName))
			{
				this.COSelf.ship.aProxIgnores.Add(nearestBO.strName);
			}
		}
		CrewSim.UndockShip(this.COSelf.ship, objShipDocked, true, false);
		CondOwner coUser = CrewSim.GetSelectedCrew();
		if (coUser != null)
		{
			coUser.ZeroCondAmount("IsDockingFreePass");
			GUIDockSys.UndockEvent.Invoke();
		}
		yield break;
	}

	private void CheckForCrimeIllegalSalvagingOKLG(string targetShip, CondOwner selectedCo)
	{
		Ship shipByRegID = CrewSim.system.GetShipByRegID(targetShip);
		if (shipByRegID == null || shipByRegID.bDestroyed)
		{
			return;
		}
		List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(CrewSim.coPlayer.strName);
		if (AIShipManager.strATCLast == "OKLG" && shipByRegID.IsDerelict() && !shipsForOwner.Contains(targetShip) && !this.COSelf.ship.HasOKLGSalvageLicense())
		{
			MonoSingleton<ObjectiveTracker>.Instance.CreateCrimeWarning(selectedCo, DataHandler.GetString("OBJV_CRIME_NO_LICENSE", false), false);
		}
	}

	private void CheckForCrimeStolenShip()
	{
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		Ship ship = selectedCrew.ship;
		if (ship == null || ship.bDestroyed)
		{
			return;
		}
		List<string> shipsForOwner = CrewSim.system.GetShipsForOwner(CrewSim.coPlayer.strName);
		if (ship.strXPDR != ship.strRegID || !shipsForOwner.Contains(ship.strRegID))
		{
			MonoSingleton<ObjectiveTracker>.Instance.CreateCrimeWarning(selectedCrew, DataHandler.GetString("OBJV_CRIME_UNREGISTERED_SHIP", false), false);
		}
	}

	public void UpdateShipInfo(ShipInfo si)
	{
		if (si == null)
		{
			return;
		}
		GUIOrbitDraw component = this.igh.goUILeft.GetComponent<GUIOrbitDraw>();
		component.UpdateShipInfo(si);
	}

	private IEnumerator ATCConnectMessage(string strRegID)
	{
		yield return null;
		this.PrintConnectionStart(strRegID);
		yield return new WaitForSeconds(MathUtils.Rand(0f, 4f, MathUtils.RandType.Mid, null));
		this.PrintConnectionMid(strRegID);
		yield return new WaitForSeconds(MathUtils.Rand(0f, 1f, MathUtils.RandType.Mid, null));
		this.PrintConnectionEnd();
		yield break;
	}

	private void PrintConnectionStart(string strRegID)
	{
		List<string> list = new List<string>();
		list.Add(string.Empty);
		list.Add("CONNECT BIELER REMOTE DOCKSYS");
		list.Add("HANDSHAKE MODE=Q; LANG=US,ZH,BR; TZ=" + CollisionManager.strATCClosest);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
	}

	private void PrintConnectionMid(string strRegID)
	{
		List<string> list = new List<string>();
		list.Add(string.Empty);
		list.Add("CONNECT BIELER REMOTE DOCKSYS");
		list.Add("HANDSHAKE MODE=Q; LANG=US,ZH,BR; TZ=" + CollisionManager.strATCClosest);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add("CONNECTION ESTABLISHED : 200 OK");
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
		list.Add(string.Empty);
	}

	private void PrintConnectionEnd()
	{
	}

	private void UpdateWASDCluster()
	{
		if (this.dictWASD == null)
		{
			this.dictWASD = new Dictionary<string, GUIBtnPressHold>();
		}
		this.UpdateWASDBtn("W", "prefabPnlWASD/bmpKeyW");
		this.UpdateWASDBtn("S", "prefabPnlWASD/bmpKeyS");
		this.UpdateWASDBtn("A", "prefabPnlWASD/bmpKeyA");
		this.UpdateWASDBtn("D", "prefabPnlWASD/bmpKeyD");
		this.UpdateWASDBtn("Q", "prefabPnlWASD/bmpKeyQ");
		this.UpdateWASDBtn("E", "prefabPnlWASD/bmpKeyE");
		GUIOrbitDraw.bUpdateWASD = false;
	}

	private void UpdateWASDBtn(string strKey, string strBtn)
	{
		Transform transform = base.transform.Find(strBtn);
		if (transform == null)
		{
			return;
		}
		GUIBtnPressHold component = transform.GetComponent<GUIBtnPressHold>();
		if (component != null)
		{
			this.dictWASD[strKey] = component;
		}
	}

	private void KeyHandler()
	{
		if (CrewSim.Typing)
		{
			return;
		}
		this.objSS.vAccIn.x = 0f;
		this.objSS.vAccIn.y = 0f;
		this.objSS.fA = 0f;
		if (this.bLocked)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		bool flag = false;
		if (GUIActionKeySelector.commandFlyUp.Held || this.dictWASD["W"].bPressed)
		{
			num2 += 1f;
			flag = true;
		}
		if (GUIActionKeySelector.commandFlyDown.Held || this.dictWASD["S"].bPressed)
		{
			num2 -= 1f;
			flag = true;
		}
		if (GUIActionKeySelector.commandFlyLeft.Held || this.dictWASD["A"].bPressed)
		{
			num -= 1f;
			flag = true;
		}
		if (GUIActionKeySelector.commandFlyRight.Held || this.dictWASD["D"].bPressed)
		{
			num += 1f;
			flag = true;
		}
		if (GUIActionKeySelector.commandShipAttitude.Held)
		{
			if (this.COSelf.ship.objSS.fW > 0f)
			{
				num3 = -1f;
			}
			else if (this.COSelf.ship.objSS.fW < 0f)
			{
				num3 = 1f;
			}
			flag = true;
		}
		else
		{
			if (GUIActionKeySelector.commandShipCCW.Held || this.dictWASD["Q"].bPressed)
			{
				num3 += 1f;
				flag = true;
			}
			if (GUIActionKeySelector.commandShipCW.Held || this.dictWASD["E"].bPressed)
			{
				num3 -= 1f;
				flag = true;
			}
		}
		float num4 = CrewSim.TimeElapsedScaled();
		Ship ship = null;
		if (this.DockingTarget != null)
		{
			CrewSim.system.dictShips.TryGetValue(this.DockingTarget, out ship);
		}
		if (ship != null)
		{
			num = MathUtils.Clamp(num - num3, -1f, 1f);
			num3 = 0f;
			if (this.fXBoost * num <= 0f)
			{
				this.fXBoost = 0f;
			}
			this.fXBoost += num * num4 * 2f;
			num = MathUtils.Clamp(this.fXBoost, -1f, 1f);
			if (num != 0f)
			{
				if (this.COSelf.ship.objSS.fW * num > 0f)
				{
					num3 = -num;
					num = 0f;
				}
				else
				{
					float num5 = (float)(ship.objSS.vPosx - this.COSelf.ship.objSS.vPosx);
					float num6 = (float)(ship.objSS.vPosy - this.COSelf.ship.objSS.vPosy);
					double num7 = ship.objSS.vVelX - this.COSelf.ship.objSS.vVelX;
					double num8 = ship.objSS.vVelY - this.COSelf.ship.objSS.vVelY;
					double num9 = (double)num5 * num8 - (double)num6 * num7;
					float num10 = 0.02f;
					if (num9 * (double)num > 0.0)
					{
						num10 = 0.5f;
					}
					num3 = -num * num10;
					num *= 1f - num10;
				}
			}
		}
		int engineMode = 1;
		float num11 = 0.25f;
		if (this.COSelf.ship.aNavs.Count > 0)
		{
			string gpminfo = this.COSelf.ship.aNavs[0].GetGPMInfo("Panel A", "nKnobEngineMode");
			engineMode = ((!string.IsNullOrEmpty(gpminfo)) ? int.Parse(gpminfo) : 1);
			gpminfo = this.COSelf.ship.aNavs[0].GetGPMInfo("Panel A", "slidThrottle");
			num11 = ((!string.IsNullOrEmpty(gpminfo)) ? float.Parse(gpminfo) : 0.25f);
		}
		if (flag || this.PlayerThrusting)
		{
			this.COSelf.ship.UnlockFromOrbit(true);
			if (CrewSim.Paused)
			{
				this.CGClampWarning("GUI_ORBIT_WARN_PAUSED");
			}
		}
		AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.COSelf.ship.strRegID);
		if (aishipByRegID == null || aishipByRegID.ActiveCommandName != "HoldStationAutoPilot")
		{
			this.COSelf.ship.Maneuver(num * num11, num2 * num11, num3 * num11, 0, CrewSim.TimeElapsedScaled(), (Ship.EngineMode)engineMode);
		}
		else if (flag || this.PlayerThrusting)
		{
			this.COSelf.ship.Maneuver(num * num11, num2 * num11, num3 * num11, 0, CrewSim.TimeElapsedScaled(), (Ship.EngineMode)engineMode);
		}
		this.PlayerThrusting = flag;
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> dict, string strCOKey)
	{
		base.Init(coSelf, dict, strCOKey);
		GUIDockSys.DictGPM = ((!(this.COSelf != null)) ? new Dictionary<string, string>() : this.COSelf.mapGUIPropMaps["Panel A"]);
		string s;
		if (this.dictPropMap.TryGetValue("MFDDisplayMode", out s))
		{
			int.TryParse(s, out this.MFDDisplayMode);
		}
		this.ShowTarget();
	}

	public override void UpdateUI()
	{
		if (this._lastTimeOpened > 0.0 && StarSystem.fEpoch - this._lastTimeOpened > 120.0 && GUIMFDPageHost.OnRequestMFDChange != null)
		{
			GUIMFDPageHost.OnRequestMFDChange.Invoke(new MFDMainMenu());
		}
		this._lastTimeOpened = StarSystem.fEpoch;
	}

	public bool PlayerThrusting { get; private set; }

	public override void SaveAndClose()
	{
		GUIDockSys.instance = null;
		if (this.dictPropMap == null)
		{
			return;
		}
		base.SetPropMapData("MFDDisplayMode", this.MFDDisplayMode.ToString());
		bool flag = false;
		GUIOrbitDraw component = this.igh.goUILeft.GetComponent<GUIOrbitDraw>();
		if (component != null)
		{
			flag = component.HoldingThrustActive;
		}
		if (this.COSelf != null && this.COSelf.ship != null && !flag)
		{
			this.COSelf.ship.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
		}
		this.fClampDisengageWarningTimer = 0f;
		if (this.cgNoClearance != null)
		{
			this.cgNoClearance.alpha = 0f;
		}
		if (this.cgWrongWay != null)
		{
			this.cgWrongWay.alpha = 0f;
		}
		base.SaveAndClose();
	}

	private RectTransform rectRingInner;

	private RectTransform rectRight;

	private RectTransform rectLeft;

	private RectTransform rectAlignedShip;

	private RectTransform rectRotateShip;

	private RectTransform rectShipLine;

	private Image imgShipLine;

	private RectTransform rectVCRS;

	private Image imgVCRS;

	private RectTransform rectBearing;

	private Image imgBearing;

	private ShipSitu objSS = new ShipSitu();

	private float fObjPosX;

	private float fObjScale = 1f;

	private float fPan = 3f;

	private float fXBoost;

	private float fClampDisengageWarningTimer;

	private double fDefaultGracePeriodAmount = 7.5;

	private double fClampDisengageGraceEnded;

	private string strRegIDClampGrace;

	private GameObject goClamps;

	[SerializeField]
	private GUILamp bmpEngageDock;

	[SerializeField]
	private GUILamp bmpClampAlign;

	private CanvasGroup cgNoATCUplink;

	private CanvasGroup cgNoClearance;

	private CanvasGroup cgWrongWay;

	private CanvasGroup cgRight;

	private CanvasGroup cgLeft;

	private CanvasGroup cgError;

	private bool bLocked;

	private bool bUplinkMissing = true;

	[SerializeField]
	private Toggle chkEngage;

	private UnityAction<bool> eClamp;

	private TMP_Text txtTimeUTC;

	private TMP_Text txtRNGETA;

	private TMP_Text txtBRGVCRS;

	private TMP_Text txtError;

	private TMP_Text txtWarning;

	private Dictionary<string, GUIBtnPressHold> dictWASD;

	private static AnimationCurveAsset curveDockingRingSize = null;

	public static GUIDockSys instance;

	private static string strNoClearance;

	private static string strNoEmptyPort;

	private static string strTowBrace;

	private Color clrText = new Color(0.46484375f, 0.99609375f, 1f, 1f);

	private Color clrBlue01 = new Color(0.1484375f, 0.57421875f, 0.984375f, 0.9f);

	private Color clrBlue02 = new Color(0.07421875f, 0.28515625f, 0.4921875f, 0.9f);

	private Color clrGreen01 = new Color(0.2890625f, 0.99609375f, 0.6953125f, 0.9f);

	private Color clrGreen02 = new Color(0.5f, 0.890625f, 0.609375f, 0.9f);

	private Color clrWhite01 = new Color(0.7421875f, 0.7421875f, 0.7421875f, 0.9f);

	private Color clrWhite02 = new Color(0.46875f, 0.46875f, 0.46875f, 0.9f);

	private Color clrRed01 = new Color(0.94921875f, 0.24609375f, 0.1796875f, 0.9f);

	private Color clrRed02 = new Color(0.234375f, 0.05859375f, 0.04296875f, 0.9f);

	private Color clrOrange01 = new Color(0.99609375f, 0.703125f, 0f, 0.9f);

	private Color clrLocalAuthority = new Color(0.78125f, 0.3125f, 0.1171875f, 0.9f);

	public static Dictionary<string, string> DictGPM;

	public int MFDDisplayMode;

	private double _lastTimeOpened;

	public static UnityEvent UndockEvent = new UnityEvent();

	public static UnityEventString DockEvent = new UnityEventString();
}
