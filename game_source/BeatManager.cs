using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Ships.AIPilots;
using Ostranauts.UI.Loading;
using Ostranauts.Utils.Models;
using UnityEngine;

public class BeatManager
{
	public static void Init()
	{
		if (DataHandler.dictPlotManager != null)
		{
			JsonPlotManagerSettings jsonPlotManagerSettings = null;
			if (DataHandler.dictPlotManager.TryGetValue("Plot Manager Settings", out jsonPlotManagerSettings))
			{
				if (jsonPlotManagerSettings.fTensionPeriod >= 0.0)
				{
					BeatManager.fTensionPeriod = jsonPlotManagerSettings.fTensionPeriod;
				}
				if (jsonPlotManagerSettings.fReleasePeriod >= 0.0)
				{
					BeatManager.fReleasePeriod = jsonPlotManagerSettings.fReleasePeriod;
				}
				if (jsonPlotManagerSettings.fSocialPeriod >= 0.0)
				{
					BeatManager.fSocialPeriod = jsonPlotManagerSettings.fSocialPeriod;
				}
				if (jsonPlotManagerSettings.dictEventChances != null)
				{
					BeatManager.dictEventChances = jsonPlotManagerSettings.dictEventChances;
				}
			}
		}
		if (BeatManager.dictEventChances == null)
		{
			BeatManager.dictEventChances = new Dictionary<string, float>();
		}
		BeatManager.ResetReleaseTimer();
		BeatManager.ResetTensionTimer();
		BeatManager.ResetAutosaveTimer();
		if (GUISaveMenu.OnCreateSave == null)
		{
			GUISaveMenu.OnCreateSave = new CreateNewSaveEvent();
		}
		if (GUISaveMenu.OnOverwriteSelected == null)
		{
			GUISaveMenu.OnOverwriteSelected = new OverwriteSaveEvent();
		}
		GUISaveMenu.OnCreateSave.AddListener(delegate(string _)
		{
			BeatManager.ResetAutosaveTimer();
		});
		GUISaveMenu.OnOverwriteSelected.AddListener(delegate(SaveInfo _)
		{
			BeatManager.ResetAutosaveTimer();
		});
	}

	public static void Update(double fTimeElapsed)
	{
		if (CrewSim.bShipEdit || CrewSim.bShipEditTest || CrewSim.Paused)
		{
			return;
		}
		BeatManager.fTensionRemain -= fTimeElapsed;
		BeatManager.fReleaseRemain -= fTimeElapsed;
		BeatManager.fSocialRemain -= fTimeElapsed;
		if (!CrewSim.bUILock)
		{
			BeatManager.fAutosaveRemain -= fTimeElapsed;
		}
		if (BeatManager.fTensionRemain < 0.0)
		{
			BeatManager.GenerateTension();
		}
		if (BeatManager.fReleaseRemain < 0.0)
		{
			BeatManager.GenerateRelease();
		}
		if (LoadManager.IsAutoSaveEnabled)
		{
			if (BeatManager.bAutosave)
			{
				string @string = DataHandler.GetString("LOG_AUTOSAVING_GAME", false);
				if (CrewSim.GetSelectedCrew() != null)
				{
					CrewSim.GetSelectedCrew().LogMessage(@string, "Neutral", "Game");
				}
				BeatManager.bAutosave = false;
				MonoSingleton<LoadManager>.Instance.AutoSave(true);
			}
			if (BeatManager.fAutosaveRemain < 0.0 && CrewSim.CanvasManager.State != CanvasManager.GUIState.SOCIAL)
			{
				BeatManager.ResetAutosaveTimer();
				BeatManager.bAutosave = true;
				if (CrewSim.GetSelectedCrew() != null)
				{
					CrewSim.GetSelectedCrew().LogMessage(DataHandler.GetString("LOG_AUTOSAVING_GAME", false), "Neutral", "Game");
				}
				MonoSingleton<GUILoadingPopUp>.Instance.ShowTooltip(DataHandler.GetString("LOAD_AUTOSAVING", false), DataHandler.GetString("LOAD_WAIT", false));
			}
		}
	}

	public static bool RunEncounter(string strIA, bool bInterrupt)
	{
		if (strIA == null || CrewSim.GetSelectedCrew() == null)
		{
			return false;
		}
		if (GUISocialCombat2.coUs == CrewSim.GetSelectedCrew())
		{
			Debug.Log("Warning: Cannot run encounter " + strIA + " while in social combat UI.");
			return false;
		}
		Interaction interaction = DataHandler.GetInteraction(strIA, null, false);
		if (interaction != null && interaction.Triggered(CrewSim.GetSelectedCrew(), CrewSim.GetSelectedCrew(), false, false, false, true, null))
		{
			Interaction interaction2 = interaction;
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			interaction.objThem = selectedCrew;
			interaction2.objUs = selectedCrew;
			if (bInterrupt)
			{
				if ((double)Time.timeScale > 1.0)
				{
					CrewSim.ResetTimeScale();
				}
				CrewSim.GetSelectedCrew().AICancelAll(null);
				CrewSim.QueueEncounter(interaction);
			}
			else
			{
				interaction.objUs.QueueInteraction(interaction.objThem, interaction, false);
			}
			return true;
		}
		return false;
	}

	public static void ResetReleaseTimer()
	{
		BeatManager.fReleaseRemain = BeatManager.fReleasePeriod;
	}

	public static void ResetTensionTimer()
	{
		BeatManager.fTensionRemain = BeatManager.fTensionPeriod;
	}

	public static void ResetSocialTimer()
	{
		BeatManager.fSocialRemain = BeatManager.fSocialPeriod;
	}

	private static void ResetAutosaveTimer()
	{
		BeatManager.fAutosaveRemain = (double)LoadManager.AutoSaveInterval;
		BeatManager.bAutosave = false;
	}

	public static void ResetAutosaveTimer(double fOverride)
	{
		BeatManager.fAutosaveRemain = fOverride;
		BeatManager.bAutosave = false;
	}

	public static void AutoSaveBeforePirateEncounter(Ship otherShip)
	{
		if (otherShip == null)
		{
			return;
		}
		List<CondOwner> people = otherShip.GetPeople(false);
		foreach (CondOwner condOwner in people)
		{
			if (!(condOwner == null))
			{
				float factionScore = condOwner.GetFactionScore(CrewSim.GetSelectedCrew().GetAllFactions());
				if (JsonFaction.GetReputation(factionScore) == JsonFaction.Reputation.Dislikes || condOwner.HasFaction("OKLGPirates"))
				{
					BeatManager.ResetAutosaveTimer(0.0);
					BeatManager.ResetTensionTimer();
					break;
				}
			}
		}
	}

	public static void GenerateTutorialDerelict()
	{
		Ship shipATCLast = AIShipManager.ShipATCLast;
		shipATCLast.objSS.UpdateTime(StarSystem.fEpoch, false);
		Ship ship = CrewSim.system.AddDerelict("TutorialDerelict", CrewSim.system.GetShipOwner(AIShipManager.strATCLast));
		if (ship == null)
		{
			return;
		}
		BodyOrbit nearestBO = CrewSim.system.GetNearestBO(shipATCLast.objSS, StarSystem.fEpoch, false);
		CondOwner selectedCrew = CrewSim.GetSelectedCrew();
		Ship ship2 = (selectedCrew.ship != null && !selectedCrew.ship.IsStation(false)) ? selectedCrew.ship : shipATCLast;
		Point point = ship2.objSS.vPos - nearestBO.vPos;
		double magnitude = point.magnitude;
		Point point2 = ship2.objSS.vPos + point.normalized * 6.684587064179629E-08;
		CrewSim.system.SetSituToRandomSafeCoords(ship.objSS, 6.684587064179629E-09, 5.3476696513437033E-08, point2.X, point2.Y, MathUtils.RandType.Low);
		ship.objSS.fRot = MathUtils.Rand(0f, 6.2831855f, MathUtils.RandType.Flat, null);
		ship.objSS.LockToBO(-1.0, false);
		ship.bXPDRAntenna = false;
		ship.strXPDR = null;
		ship.objSS.bIsNoFees = true;
		ship.fBreakInMultiplier = 0.7f;
		ship.ShipCO.SetCondAmount("IsTutorialDerelict", 1.0, 0.0);
		ship.ShipCO.SetCondAmount("IsTutorialDerelictHidden", 1.0, 0.0);
		CrewSimTut.tutorialShipInstanceRef = ship;
	}

	public static void GenerateSocial(Interaction objInteractionBest)
	{
		if (objInteractionBest == null || BeatManager.fSocialRemain >= 0.0)
		{
			return;
		}
		objInteractionBest.strRaiseUI = "SocialCombat";
		BeatManager.ResetSocialTimer();
	}

	public static void GenerateTension()
	{
		if (CrewSim.coPlayer == null || CrewSim.coPlayer.ship == null)
		{
			return;
		}
		if (CrewSim.coPlayer.HasCond("IsTensionCooldown") || CrewSim.coPlayer.HasCond("IsInChargen"))
		{
			BeatManager.ResetTensionTimer();
			return;
		}
		BeatManager.bOnStation = CrewSim.coPlayer.ship.objSS.bIsBO;
		BeatManager.bDockedWithStation = CrewSim.coPlayer.ship.objSS.bBOLocked;
		BeatManager.fRoll = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
		if (BeatManager.Mariner())
		{
			CrewSim.coPlayer.AddCondAmount("IsTensionCooldown", 1.0, 0.0, 0f);
			BeatManager.fTensionRemain = 3.0;
			return;
		}
		bool flag = false;
		if (!flag)
		{
			flag = BeatManager.Plot(true);
		}
		if (!flag)
		{
			flag = BeatManager.PartFailure();
		}
		if (!flag)
		{
			flag = BeatManager.Police();
		}
		if (!flag)
		{
			flag = BeatManager.Micrometeoroid();
		}
		if (!flag)
		{
			flag = BeatManager.EmotionClobber();
		}
		if (!flag)
		{
			flag = BeatManager.MeatDerelict(true);
		}
		if (!flag)
		{
			flag = BeatManager.MessageFromDerelict();
		}
		if (!flag)
		{
			flag = BeatManager.Pirate();
		}
		if (flag)
		{
			CrewSim.coPlayer.AddCondAmount("IsTensionCooldown", 1.0, 0.0, 0f);
		}
		if (flag)
		{
			BeatManager.ResetTensionTimer();
		}
	}

	public static void GenerateRelease()
	{
		if (CrewSim.coPlayer == null || CrewSim.coPlayer.ship == null)
		{
			return;
		}
		if (CrewSim.coPlayer.HasCond("IsReleaseCooldown") || CrewSim.coPlayer.HasCond("IsInChargen"))
		{
			BeatManager.ResetReleaseTimer();
			return;
		}
		BeatManager.fRoll = MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
		bool flag = false;
		if (!flag)
		{
			flag = BeatManager.Plot(false);
		}
		if (!flag)
		{
			flag = BeatManager.BonusDerelict();
		}
		if (!flag)
		{
			flag = BeatManager.MeatDerelict(false);
		}
		if (!flag)
		{
			flag = BeatManager.ReactorPart();
		}
		if (flag)
		{
			CrewSim.coPlayer.AddCondAmount("IsReleaseCooldown", 1.0, 0.0, 0f);
		}
		BeatManager.ResetReleaseTimer();
	}

	private static bool Plot(bool bTension)
	{
		float num = 0f;
		if (bTension)
		{
			BeatManager.dictEventChances.TryGetValue("tension_plot", out num);
		}
		else
		{
			BeatManager.dictEventChances.TryGetValue("release_plot", out num);
		}
		if (BeatManager.fRoll < num && PlotManager.CheckPlots(CrewSim.GetSelectedCrew(), (!bTension) ? PlotManager.PlotTensionType.RELEASE : PlotManager.PlotTensionType.TENSION))
		{
			Debug.Log("GenerateTension() triggered plot. Roll: " + BeatManager.fRoll);
			return true;
		}
		Debug.Log("GenerateTension() failed triggering plot. Roll: " + BeatManager.fRoll);
		BeatManager.fRoll -= num;
		return false;
	}

	public static void DebugPirate()
	{
		BeatManager.fRoll = 0f;
		BeatManager.Pirate();
	}

	private static bool Pirate()
	{
		float num = 0f;
		BeatManager.dictEventChances.TryGetValue("tension_pirate_spawn", out num);
		List<AIShip> shipsOfTypeForRegion = AIShipManager.GetShipsOfTypeForRegion(AIType.Pirate);
		Ship ship = CrewSim.GetSelectedCrew().ship;
		if (BeatManager.fRoll < num)
		{
			bool flag = false;
			float num2 = 0f;
			if (shipsOfTypeForRegion.Count >= AIShipManager.PiratesMax)
			{
				num2 = 1f;
			}
			else if (shipsOfTypeForRegion.Count > AIShipManager.PiratesMin)
			{
				num2 = 1f * (float)shipsOfTypeForRegion.Count / (float)AIShipManager.PiratesMax;
				flag = true;
			}
			else
			{
				flag = true;
			}
			if (MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null) <= num2 && ship != null && !ship.IsDocked())
			{
				foreach (AIShip aiship in shipsOfTypeForRegion)
				{
					if (!aiship.Ship.bDestroyed && !aiship.Ship.HideFromSystem && !aiship.Ship.IsDocked() && !AIShipManager.BingoFuelCheck(aiship.Ship, ship, aiship.AICharacter.MaxSpeed(null)))
					{
						Debug.Log("GenerateTension() redirected pirate to player. Roll: " + BeatManager.fRoll);
						aiship.AddCommandLoot("Lurk", new string[]
						{
							CrewSim.GetSelectedCrew().ship.strRegID
						});
						return true;
					}
				}
			}
			if (flag)
			{
				AIShip aiship2 = AIShipManager.SpawnAI(AIType.Pirate, null);
				if (aiship2 != null)
				{
					Debug.Log("GenerateTension() spawned pirate. Roll: " + BeatManager.fRoll);
					return true;
				}
			}
		}
		Debug.Log("GenerateTension() failed spawning pirate. Roll: " + BeatManager.fRoll);
		BeatManager.fRoll -= num;
		return false;
	}

	private static bool Police()
	{
		float num = 0f;
		bool result = false;
		BeatManager.dictEventChances.TryGetValue("tension_police_patrol", out num);
		if ((CrewSim.coPlayer.HasCond("IsDueCopSpawn") || BeatManager.fRoll < num) && AIShipManager.CheckLocalAuthorityScenario())
		{
			result = true;
			Debug.Log("GenerateTension() spawned police. Roll: " + BeatManager.fRoll);
			CrewSim.coPlayer.ZeroCondAmount("IsDueCopSpawn");
		}
		else
		{
			Debug.Log("GenerateTension() failed spawning police. Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return result;
	}

	private static bool Mariner()
	{
		float num = 0f;
		BeatManager.dictEventChances.TryGetValue("tension_mariner", out num);
		if (MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null) < num && !BeatManager.bOnStation && !BeatManager.bDockedWithStation)
		{
			ParanormalSpawner.SpawnMarinerNearestDark();
			Debug.Log("GenerateTension() spawned mariner. Roll: " + BeatManager.fRoll);
			return true;
		}
		Debug.Log("GenerateTension() failed spawning mariner. Roll: " + BeatManager.fRoll);
		return false;
	}

	private static bool Micrometeoroid()
	{
		float num = 0f;
		bool result = false;
		BeatManager.dictEventChances.TryGetValue("tension_micrometeoroid", out num);
		if (BeatManager.fRoll < num && !BeatManager.bOnStation && !BeatManager.bDockedWithStation)
		{
			StarSystem.SpawnMicroMeteoroid(CrewSim.coPlayer.ship, 1f, (double)Time.timeScale > 1.0);
			CrewSim.coPlayer.ship.LogAdd(DataHandler.GetString("NAV_LOG_IMPACT", false) + "MicroMeteoroid", StarSystem.fEpoch, true);
			result = true;
			Debug.Log("GenerateTension() spawned micrometeoroid. Roll: " + BeatManager.fRoll);
		}
		else
		{
			Debug.Log("GenerateTension() failed spawning micrometeoroid. Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return result;
	}

	private static bool EmotionClobber()
	{
		float num = 0f;
		bool result = false;
		Loot loot = DataHandler.GetLoot("TXTClobberEmotionEnc");
		Interaction interaction = DataHandler.GetInteraction(loot.GetLootNameSingle(null), null, false);
		BeatManager.dictEventChances.TryGetValue("tension_emotional_clobber", out num);
		if (!(GUISocialCombat2.coUs == CrewSim.GetSelectedCrew()) && BeatManager.fRoll < num && interaction != null && interaction.Triggered(CrewSim.coPlayer, CrewSim.coPlayer, false, false, false, true, null))
		{
			CrewSim.coPlayer.QueueInteraction(CrewSim.coPlayer, interaction, false);
			result = true;
			Debug.Log("GenerateTension() clobbered by emotion. Roll: " + BeatManager.fRoll);
		}
		else
		{
			Debug.Log("GenerateTension() failed clobbered by emotion. Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return result;
	}

	private static bool MessageFromDerelict()
	{
		float num = 0f;
		bool result = false;
		BeatManager.dictEventChances.TryGetValue("tension_message_from_derelict", out num);
		if (BeatManager.fRoll < num && !BeatManager.bOnStation && !BeatManager.bDockedWithStation)
		{
			List<Ship> list = new List<Ship>();
			foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
			{
				if (ship.IsDerelict() && ship.fLastVisit == 0.0 && !ship.IsDocked())
				{
					List<CondOwner> people = ship.GetPeople(false);
					if (people.Count > 0)
					{
						list.Add(ship);
					}
				}
			}
			if (list.Count <= 0)
			{
				return result;
			}
			Ship ship2 = list[UnityEngine.Random.Range(0, list.Count)];
			ship2.Comms.SendMessage("SHIPDerelictHailsPlayer", CrewSim.coPlayer.ship.strRegID, null);
			result = true;
			Debug.Log("GenerateTension() derelict sends message. Roll: " + BeatManager.fRoll);
		}
		else
		{
			Debug.Log("GenerateTension() failed derelict sends message. Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return result;
	}

	private static bool PartFailure()
	{
		float num = 0f;
		bool flag = false;
		BeatManager.dictEventChances.TryGetValue("tension_part_failure", out num);
		if (BeatManager.fRoll < num)
		{
			CondTrigger condTrigger = new CondTrigger();
			condTrigger.aReqs = new string[]
			{
				"StatDamage"
			};
			List<CondOwner> list = new List<CondOwner>();
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if (selectedCrew != null && selectedCrew.ship != null)
			{
				list.AddRange(selectedCrew.ship.GetCOs(condTrigger, true, false, true));
			}
			double num2 = 0.15;
			List<CondOwner> list2 = new List<CondOwner>();
			foreach (CondOwner condOwner in list)
			{
				double damageState = condOwner.GetDamageState();
				if (damageState < num2)
				{
					list2.Insert(0, condOwner);
					num2 = damageState;
				}
			}
			if (list2.Count > 0)
			{
				CondOwner condOwner2 = list2[0];
				string strName = condOwner2.strName;
				if (Ship.ctSparkable.Triggered(condOwner2, null, true))
				{
					CrewSim.vfxSparks.AddSparkAt(condOwner2.tf.position);
				}
				condOwner2.SetCondAmount("StatDamage", condOwner2.GetCondAmount("StatDamageMax"), 0.0);
				flag = true;
				Debug.Log(string.Concat(new object[]
				{
					"GenerateTension() made part fail: ",
					strName,
					". Roll: ",
					BeatManager.fRoll
				}));
			}
		}
		if (!flag)
		{
			Debug.Log("GenerateTension() failed part failure. Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return flag;
	}

	private static bool BonusDerelict()
	{
		float num = 0f;
		bool result = false;
		BeatManager.dictEventChances.TryGetValue("release_bonus_derelict", out num);
		if (BeatManager.fRoll < num)
		{
			CrewSim.coPlayer.AddCondAmount("IsDueBonusDerelict", 1.0, 0.0, 0f);
			result = true;
			Debug.Log("GenerateRelease() added bonus derelict cond. Roll: " + BeatManager.fRoll);
		}
		else
		{
			Debug.Log("GenerateRelease() failed adding bonus derelict. Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return result;
	}

	private static bool MeatDerelict(bool bTension)
	{
		float num = 0f;
		bool flag = false;
		if (bTension)
		{
			BeatManager.dictEventChances.TryGetValue("tension_meat_derelict", out num);
		}
		else
		{
			BeatManager.dictEventChances.TryGetValue("release_meat_derelict", out num);
		}
		if (BeatManager.fRoll < num)
		{
			CrewSim.coPlayer.AddCondAmount("IsDueMeatProgression", 1.0, 0.0, 0f);
			if (!CrewSim.coPlayer.HasCond("StatNoMeat"))
			{
				flag = true;
				Debug.Log("GenerateRelease() added meat derelict cond. Roll: " + BeatManager.fRoll);
			}
		}
		if (!flag)
		{
			Debug.Log("GenerateRelease() failed adding meat derelict. Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return flag;
	}

	private static bool ReactorPart()
	{
		float num = 0f;
		bool result = false;
		CondOwner coPlayer = CrewSim.coPlayer;
		if (coPlayer.HasCond("IsDueReactorPart") || coPlayer.ship == null || coPlayer.ship.bFusionReactorRunning)
		{
			return false;
		}
		BeatManager.dictEventChances.TryGetValue("release_reactor_part", out num);
		if (BeatManager.fRoll < num)
		{
			CrewSim.coPlayer.AddCondAmount("IsDueReactorPart", 1.0, 0.0, 0f);
			result = true;
			Debug.Log("GenerateRelease() added reactor part due cond. Roll: " + BeatManager.fRoll);
		}
		else
		{
			Debug.Log("GenerateRelease() failed adding reactor part cond Roll: " + BeatManager.fRoll);
			BeatManager.fRoll -= num;
		}
		return result;
	}

	private static double fTensionRemain;

	private static double fReleaseRemain;

	private static double fSocialRemain;

	private static double fAutosaveRemain;

	private static bool bAutosave;

	private static Dictionary<string, float> dictEventChances;

	private static double fTensionPeriod = 540.0;

	private static double fReleasePeriod = 650.0;

	private static double fSocialPeriod = 300.0;

	private static float fRoll;

	private static bool bOnStation;

	private static bool bDockedWithStation;
}
