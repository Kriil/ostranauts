using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIChargenLaunch : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.lblMessage = base.transform.Find("bmpPDABG/lblMessage").GetComponent<TMP_Text>();
		this.bmpHand = base.transform.Find("pnlHandMask/bmpHand").GetComponent<RawImage>();
		this.bmpWarn = base.transform.Find("bmpWarn").GetComponent<GUILamp>();
		this.bmpReady = base.transform.Find("bmpReady").GetComponent<GUILamp>();
		this.btnOpen = base.transform.Find("btnOpen").GetComponent<Button>();
		this.btnOpen.onClick.AddListener(delegate()
		{
			this.Launch();
		});
		AudioManager.AddBtnAudio(this.btnOpen.gameObject, "ShipUIBtnLaunchIn", "ShipUIBtnLaunchOut");
		this.btnQuit = base.transform.Find("btnQuit").GetComponent<Button>();
		this.btnQuit.onClick.AddListener(delegate()
		{
			this.Quit();
		});
		AudioManager.AddBtnAudio(this.btnQuit.gameObject, "ShipUIBtnLaunchIn", "ShipUIBtnLaunchOut");
	}

	private void Update()
	{
		if (this.bNoLaunch)
		{
			this.fWarnBlinkCountdown -= (double)CrewSim.TimeElapsedScaled();
			if (this.fWarnBlinkCountdown > 0.0)
			{
				this.bmpWarn.State = 2;
			}
			else
			{
				this.bmpWarn.State = 3;
			}
			this.lblMessage.gameObject.SetActive(this.bmpWarn.ImageIndex != 0);
		}
	}

	private void SetUI()
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		if (objThem == null)
		{
			CrewSim.LowerUI(false);
			return;
		}
		GUIChargenStack component = objThem.GetComponent<GUIChargenStack>();
		if (component == null)
		{
			CrewSim.LowerUI(false);
			return;
		}
		this.bmpReady.State = 0;
		this.bmpWarn.State = 3;
		this.GetHand();
		if (component.bCareerEnded)
		{
			this.lblMessage.text = DataHandler.GetString("GUI_LAUNCH_CONFIRM", false);
			this.bmpReady.State = 3;
			this.bmpWarn.State = 0;
			this.bNoLaunch = false;
		}
		else
		{
			this.lblMessage.text = DataHandler.GetString("GUI_LAUNCH_NO_SHIP", false);
		}
	}

	private void GetHand()
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		Crew component = objThem.GetComponent<Crew>();
		string[] faceGroups = FaceAnim2.GetFaceGroups(component.FaceParts);
		string str = "GUIPDAHand" + faceGroups[0];
		this.bmpHand.texture = DataHandler.LoadPNG(str + ".png", false, false);
	}

	private void Quit()
	{
		CrewSim.LowerUI(false);
	}

	private void Launch()
	{
		CondOwner objThem = this.COSelf.GetInteractionCurrent().objThem;
		if (objThem == null)
		{
			return;
		}
		GUIChargenStack component = objThem.GetComponent<GUIChargenStack>();
		if (component == null)
		{
			return;
		}
		if (this.bNoLaunch)
		{
			AudioManager.am.PlayAudioEmitter("ShipUIBtnLaunchError", false, false);
			this.fWarnBlinkCountdown = 2.0;
			return;
		}
		if (!CrewSim.system.CheckShipSpawned(component.strRegIDChosen))
		{
			Debug.Log("Unable to find starting ship: " + component.strRegIDChosen + ".");
			return;
		}
		AudioManager.am.SuggestMusic("Undocking", true);
		BeatManager.RunEncounter("ENCLaunchShip", true);
		CrewSim.objInstance.LaunchShip(1f, objThem, component);
		CrewSim.coPlayer.AddCondAmount("IsInChargen", -1.0, 0.0, 0f);
		BeatManager.ResetTensionTimer();
		BeatManager.ResetReleaseTimer();
		BeatManager.ResetAutosaveTimer(1.0);
		AudioManager.am.PlayAudioEmitter("ItmDoor01OpenSound", false, false);
	}

	public static void DebugStart()
	{
		Ship ship = CrewSim.system.SpawnShip("SalvagePod", null, Ship.Loaded.Shallow, Ship.Damage.New, CrewSim.coPlayer.strID, 100, false);
		Ship ship2 = CrewSim.coPlayer.ship;
		ship = CrewSim.system.SpawnShip(ship.strRegID, Ship.Loaded.Full);
		ship.ToggleVis(true, true);
		ship2.ToggleVis(false, true);
		ship.MoveShip(-ship.vShipPos);
		CrewSim.MoveCO(CrewSim.coPlayer, ship, true);
		CrewSim.coPlayer.ClaimShip(ship.strRegID);
		CrewSim.coPlayer.UnclaimShip(ship2.strRegID);
		Ship ship3 = null;
		CrewSim.system.dictShips.TryGetValue("OKLG", out ship3);
		if (ship3 == null)
		{
			ship3 = ship2;
		}
		ship.objSS.vPosx = ship3.objSS.vPosx;
		ship.objSS.vPosy = ship3.objSS.vPosy;
		Vector2 a = MathUtils.GetPushbackVector(ship, ship3);
		a *= 4.679211E-08f;
		ship.objSS.vPosx = ship3.objSS.vPosx + (double)a.x;
		ship.objSS.vPosy = ship3.objSS.vPosy + (double)a.y;
		ship.objSS.vVelX = ship3.objSS.vVelX;
		ship.objSS.vVelY = ship3.objSS.vVelY;
		MonoSingleton<ObjectiveTracker>.Instance.RemoveShipSubscription(ship2.strRegID);
		MonoSingleton<ObjectiveTracker>.Instance.AddShipSubscription(ship.strRegID);
		CrewSim.system.dictShips.Remove(ship2.strRegID);
		ship2.Destroy(true);
		CrewSim.coPlayer.AddCondAmount("IsInChargen", -1.0, 0.0, 0f);
		BeatManager.ResetTensionTimer();
		BeatManager.ResetReleaseTimer();
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.SetUI();
	}

	private Button btnOpen;

	private Button btnQuit;

	private TMP_Text lblMessage;

	private GUILamp bmpWarn;

	private GUILamp bmpReady;

	private RawImage bmpHand;

	private bool bNoLaunch = true;

	private double fWarnBlinkCountdown;
}
