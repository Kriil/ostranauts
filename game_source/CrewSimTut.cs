using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Core.Tutorials;
using Ostranauts.Objectives;
using Ostranauts.UI.MegaToolTip;
using UnityEngine;
using UnityEngine.Events;

public class CrewSimTut : MonoBehaviour
{
	public static Ship playerShipRef
	{
		get
		{
			return CrewSimTut.PlayerShip();
		}
		set
		{
			CrewSimTut._playerShipRef = value;
		}
	}

	public static CondOwner playerShipNavStationRef
	{
		get
		{
			return CrewSimTut.PlayerShipNavStation();
		}
		set
		{
			CrewSimTut._playerShipNavStation = value;
		}
	}

	public static Ship tutorialShipInstanceRef
	{
		get
		{
			return CrewSimTut.TutorialDerelict();
		}
		set
		{
			CrewSimTut._tutorialDerelictRef = value;
		}
	}

	public static CondOwner tutorialPermitRef
	{
		get
		{
			return CrewSimTut.TutorialPermit();
		}
		set
		{
			CrewSimTut._tutorialPermitRef = value;
		}
	}

	public static void OverrideHallwayConduit(CondOwner condOwner)
	{
		CrewSimTut._tutorialHallwayConduit = condOwner;
	}

	public void SetNewGameObjectives()
	{
		if (CrewSim.coPlayer.HasCond("IsInChargen"))
		{
			CrewSimTut.BeginTutorialBeat<WaitForChargen>();
		}
		else if (CrewSimTut.forceTutorialNoChargen)
		{
			CrewSimTut.BeginTutorialBeat<UnpauseWorld>();
		}
	}

	private static Ship PlayerShip()
	{
		if (CrewSimTut._playerShipRef != null)
		{
			return CrewSimTut._playerShipRef;
		}
		foreach (string strRegID in CrewSim.coPlayer.GetShipsOwned())
		{
			CrewSimTut._playerShipRef = CrewSim.system.GetShipByRegID(strRegID);
			if (CrewSimTut._playerShipRef != null)
			{
				break;
			}
		}
		return CrewSimTut._playerShipRef;
	}

	private static CondOwner PlayerShipNavStation()
	{
		if (CrewSimTut._playerShipNavStation != null)
		{
			return CrewSimTut._playerShipNavStation;
		}
		if (CrewSimTut.playerShipRef == null)
		{
			return null;
		}
		List<CondOwner> cos = CrewSimTut.playerShipRef.GetCOs(DataHandler.GetCondTrigger("TIsStationNavInstalledNotDmg"), false, false, false);
		if (cos.Count > 0)
		{
			CrewSimTut._playerShipNavStation = cos[0];
		}
		return CrewSimTut._playerShipNavStation;
	}

	private static CondOwner TutorialPermit()
	{
		if (CrewSimTut._tutorialPermitRef != null)
		{
			return CrewSimTut._tutorialPermitRef;
		}
		if (CrewSimTut.tutorialShipInstanceRef == null)
		{
			return null;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsPermitOKLGSalvage");
		List<CondOwner> cos = CrewSimTut.tutorialShipInstanceRef.GetCOs(condTrigger, false, false, false);
		if (cos.Count > 0)
		{
			CrewSimTut._tutorialPermitRef = cos[0];
			return cos[0];
		}
		cos = CrewSim.coPlayer.GetCOs(true, condTrigger);
		if (cos.Count > 0)
		{
			CrewSimTut._tutorialPermitRef = cos[0];
			return cos[0];
		}
		return null;
	}

	private static Ship TutorialDerelict()
	{
		if (CrewSimTut._tutorialDerelictRef != null)
		{
			return CrewSimTut._tutorialDerelictRef;
		}
		foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
		{
			if (keyValuePair.Value.ShipCO.HasCond("IsTutorialDerelictHidden"))
			{
				CrewSimTut._tutorialDerelictRef = keyValuePair.Value;
			}
		}
		return CrewSimTut._tutorialDerelictRef;
	}

	private void Update()
	{
		bool flag = false;
		for (int i = 0; i < CrewSimTut.TutorialBeats.Count; i++)
		{
			CrewSimTut.TutorialBeats[i].Process();
			if (CrewSimTut.TutorialBeats[i].Finished)
			{
				CrewSimTut.TutorialBeats.RemoveAt(i);
				i--;
				flag = true;
			}
		}
		if (flag)
		{
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
		}
	}

	private void OnDestroy()
	{
		CrewSimTut.UniqueToStrID.Clear();
		for (int i = 0; i < CrewSimTut.TutorialBeats.Count; i++)
		{
			CrewSimTut.TutorialBeats[i].RemoveAllListeners();
		}
		CrewSimTut.TutorialBeats.Clear();
		CrewSimTut._tutorialPermitRef = null;
		CrewSimTut._tutorialRack = null;
		CrewSimTut._tutorialHallwayConduit = null;
		CrewSimTut._playerShipNavStation = null;
		CrewSimTut._tutorialDerelictRef = null;
		CrewSimTut._playerShipRef = null;
		CrewSimTut.playerShipRef = null;
		CrewSimTut.playerShipNavStationRef = null;
		CrewSimTut.tutorialShipInstanceRef = null;
		CrewSimTut.tutorialPermitRef = null;
		CrewSimTut.HasCompletedHelmetAtmoTutorial = false;
		CrewSimTut.forceTutorialNoChargen = false;
	}

	private void TrimNullAndNonPlayerShipCOs(List<CondOwner> aCOs)
	{
		if (aCOs == null || CrewSim.coPlayer == null)
		{
			return;
		}
		for (int i = aCOs.Count - 1; i >= 0; i--)
		{
			if (aCOs[i] == null || aCOs[i].ship == null || !CrewSim.coPlayer.OwnsShip(aCOs[i].ship.strRegID))
			{
				aCOs.RemoveAt(i);
			}
		}
	}

	private void OnMTTExpand()
	{
		CrewSim.coPlayer.ZeroCondAmount("TutorialMTTExpandWaiting");
		MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
		if (ModuleHost.ToggleShowMore != null)
		{
			ModuleHost.ToggleShowMore.RemoveListener(new UnityAction(this.OnMTTExpand));
		}
	}

	private void OnMTTSelectionChanged(CondOwner selected)
	{
		if (selected == null)
		{
			return;
		}
		if (CrewSim.coPlayer.HasCond("TutorialMTTRightClick"))
		{
			CrewSim.coPlayer.AddCondAmount("TutorialMTTSelectShow", 1.0, 0.0, 0f);
			CrewSim.coPlayer.ZeroCondAmount("TutorialMTTRightClick");
			return;
		}
		if (CrewSim.coPlayer.HasCond("TutorialMTTSelectWaiting") && selected.HasCond("IsRoom"))
		{
			CrewSim.coPlayer.ZeroCondAmount("TutorialMTTSelectWaiting");
			MonoSingleton<ObjectiveTracker>.Instance.CheckObjective(CrewSim.coPlayer.strID);
			if (TooltipPreviewButton.OnPreviewButtonClicked != null)
			{
				TooltipPreviewButton.OnPreviewButtonClicked.RemoveListener(new UnityAction<CondOwner>(this.OnMTTSelectionChanged));
			}
			return;
		}
	}

	public static void CheckHelmetAtmoTutorial()
	{
	}

	public static void BeginTutorialBeat<T>() where T : TutorialBeat, new()
	{
		bool flag = false;
		foreach (TutorialBeat tutorialBeat in CrewSimTut.TutorialBeats)
		{
			if (tutorialBeat is T)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			T t = Activator.CreateInstance<T>();
			CrewSimTut.TutorialBeats.Add(t);
		}
	}

	public static void BeginTutorialBeat<T>(T beat) where T : TutorialBeat
	{
		bool flag = false;
		foreach (TutorialBeat tutorialBeat in CrewSimTut.TutorialBeats)
		{
			if (tutorialBeat is T)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			CrewSimTut.TutorialBeats.Add(beat);
		}
	}

	private static CondOwner _tutorialPermitRef = null;

	private static CondOwner _tutorialRack = null;

	private static CondOwner _tutorialHallwayConduit = null;

	private static CondOwner _playerShipNavStation = null;

	private static Ship _tutorialDerelictRef = null;

	private static Ship _playerShipRef = null;

	public static bool HasCompletedHelmetAtmoTutorial = false;

	public static bool forceTutorialNoChargen;

	public static Dictionary<string, string> UniqueToStrID = new Dictionary<string, string>();

	public static List<TutorialBeat> TutorialBeats = new List<TutorialBeat>();

	private static CondTrigger _ctWearsSuit;
}
