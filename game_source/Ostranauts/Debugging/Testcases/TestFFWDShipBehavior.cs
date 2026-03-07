using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Utils.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ostranauts.Debugging.Testcases
{
	public class TestFFWDShipBehavior : ITestCase
	{
		public bool SetupComplete
		{
			get
			{
				return CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading;
			}
		}

		public void SetupTest()
		{
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestFFWDShipBehavior.<>f__mg$cache0 == null)
			{
				TestFFWDShipBehavior.<>f__mg$cache0 = new UnityAction(TestFFWDShipBehavior.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestFFWDShipBehavior.<>f__mg$cache0);
			SceneManager.LoadScene("Loading");
			AIShipManager.ShowDebugLogs = true;
		}

		private static void OnFinishedLoading()
		{
			CrewSim.jsonShip = DataHandler.GetShip("Whistler Hot-Rod");
			AIShipManager.IsTestCase = true;
			CrewSim.objInstance.NewGame("OKLG_AND_FLOT");
			CrewSim.objInstance.DebugSetPlayerStartingPosition("OKLG", 60f, null);
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestFFWDShipBehavior.<>f__mg$cache1 == null)
			{
				TestFFWDShipBehavior.<>f__mg$cache1 = new UnityAction(TestFFWDShipBehavior.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestFFWDShipBehavior.<>f__mg$cache1);
		}

		[TestCase(Title = "ScavAI Instadock to station", Description = "Spawns a ScavAI, sets Flotilla as target and calls SFFWD on it while in FlyTo command.", Order = 1)]
		private IEnumerator ScavInstaDockTest()
		{
			this.SetPlayerPosition();
			yield return null;
			this._flotillaStation = CrewSim.system.GetShipByRegID("OKLG_FLOT");
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			TestManager.Log("Scav FlyTo command active", LogColor.Green);
			ai.Ship.shipSituTarget = this._flotillaStation.objSS;
			ai.Ship.shipScanTarget = this._flotillaStation;
			Debug.Log("Set Flotilla as Scav target");
			this.TriggerSFFWD(4f);
			yield return new WaitUntil(() => ai.Ship.HideFromSystem);
			CrewSim.LowerUI(false);
			TestManager.Log("Scav instadocked successfully", LogColor.Green);
			AIShip.ClearShipTarget(ai.Ship);
			ai.Ship.shipScanTarget = this._flotillaStation;
			ai.Ship.shipSituTarget = this._flotillaStation.objSS;
			ai.Ship.Destroy(true);
			yield break;
		}

		[TestCase(Title = "ScavAI Instadock to derelict", Description = "Spawns a ScavAI and a derelict, SFFWD gets called while Scav flies to derelict", Order = 1)]
		private IEnumerator ScavInstaDockDerelictTest()
		{
			this.ResetPlayerPosition();
			yield return null;
			Ship derelict = this.SpawnDerelictAtMidPoint();
			yield return null;
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			TestManager.Log("Scav FlyTo command active", LogColor.Green);
			ai.Ship.shipSituTarget = derelict.objSS;
			ai.Ship.shipScanTarget = derelict;
			Debug.Log("Set Derelict as Scav target");
			this.TriggerSFFWD(4f);
			yield return new WaitUntil(() => ai.Ship.IsDocked());
			TestManager.Log("Scav instadocked successfully", LogColor.Green);
			CrewSim.LowerUI(false);
			ai.Ship.Undock(derelict);
			yield return null;
			derelict.Destroy(false);
			AIShipManager.UnregisterShip(ai.Ship);
			AIShip.ClearShipTarget(ai.Ship);
			ai.Ship.Destroy(false);
			yield break;
		}

		[TestCase(Title = "ScavAI Undocking response", Description = "Triggers SFFWD while the Scav is in the middle of undocking", Order = 1)]
		private IEnumerator ScavUndockHandling()
		{
			this.ResetPlayerPosition();
			yield return null;
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "Undock");
			TestManager.Log("Scav Undock command active", LogColor.Green);
			this.TriggerSFFWD(1f);
			yield return new WaitUntil(() => ai.Ship.HideFromSystem);
			CrewSim.LowerUI(false);
			TestManager.Log("Scav instadocked successfully", LogColor.Green);
			AIShip.ClearShipTarget(ai.Ship);
			ai.Ship.shipScanTarget = this._flotillaStation;
			ai.Ship.shipSituTarget = this._flotillaStation.objSS;
			ai.Ship.Destroy(false);
			yield break;
		}

		[TestCase(Title = "Hauler Deploying derelict test", Description = "FFWD while deploying hauler has a derelict payload", Order = 1)]
		private IEnumerator HaulerDeployerTest()
		{
			this.ResetPlayerPosition();
			yield return null;
			ShipSitu _middlePos = this.GetMiddlePosSitu();
			AIShip ai = AIShipManager.SpawnAI(AIType.HaulerDeployer, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "HaulShip");
			ai.Ship.shipSituTarget = _middlePos;
			Ship derelict = ai.Ship.GetAllDockedShips().FirstOrDefault<Ship>();
			TestManager.Log("HaulShip command active", LogColor.Green);
			this.TriggerSFFWD(4f);
			this.ResetPlayerPosition();
			yield return new WaitUntil(() => derelict.objSS.bBOLocked);
			CrewSim.LowerUI(false);
			TestManager.Log("Derelict anchored successfully", LogColor.Green);
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			this.TriggerSFFWD(4f);
			yield return new WaitUntil(() => ai.Ship.HideFromSystem);
			TestManager.AssertIsTrue(ai.Ship.HideFromSystem, "Hauler redocked!");
			ai.Ship.Destroy(true);
			yield break;
		}

		[TestCase(Title = "Pirate test", Description = "FFWD while on the way to a lurking spot then FFWD while doing a shakedown on a Scav", Order = 1)]
		private IEnumerator PirateTest()
		{
			this.ResetPlayerPosition();
			yield return null;
			AIShip ai = AIShipManager.SpawnAI(AIType.Pirate, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo" && ai.Ship.shipSituTarget != null);
			TestManager.Log("Pirate on the way to lurking spot", LogColor.Green);
			this.TriggerSFFWD(4f);
			this.ResetPlayerPosition();
			yield return new WaitUntil(() => ai.ActiveCommandName == "Lurk");
			TestManager.Log("Pirate lurking", LogColor.Green);
			ai.Ship.AIRefuel();
			AIShip victim = AIShipManager.SpawnAI(AIType.Scav, null);
			int oldSize = victim.Ship.objSS.Size / 20;
			victim.Ship.objSS.SetSize(oldSize * 3);
			victim.Ship.objSS.PlaceOrbitPosition(ai.Ship.objSS);
			victim.Ship.objSS.SetSize(oldSize);
			victim.Ship.fAIPauseTimer = StarSystem.fEpoch + 1000.0;
			yield return new WaitUntil(() => ai.ActiveCommandName == "PirateShakeDown");
			TestManager.Log("Pirate shake down ongoing", LogColor.Green);
			this.TriggerSFFWD(1f);
			yield return new WaitUntil(() => ai.ActiveCommandName == "Lurk");
			TestManager.AssertIsTrue(ai.Ship.objSS.bBOLocked, "Pirate boLocked");
			ai.Ship.Destroy(true);
			victim.Ship.Destroy(true);
			TestManager.Log("Finished running tests", LogColor.Neutral);
			yield break;
		}

		private void TriggerSFFWD(float hoursToJump)
		{
			CrewSim.RaiseUI("FFWD", CrewSim.GetSelectedCrew());
			GUIFFWD guiffwd = UnityEngine.Object.FindObjectOfType<GUIFFWD>();
			Button componentInChildren = guiffwd.transform.GetComponentInChildren<Button>();
			Slider componentInChildren2 = guiffwd.transform.GetComponentInChildren<Slider>();
			componentInChildren2.value = hoursToJump;
			componentInChildren.onClick.Invoke();
		}

		private ShipSitu GetMiddlePosSitu()
		{
			ShipSitu shipSitu = new ShipSitu();
			Point point = this.CalculateDeviatedMidPoint();
			shipSitu.vPosx = point.X;
			shipSitu.vPosy = point.Y;
			shipSitu.LockToBO(CrewSim.system.GetNearestBO(shipSitu, StarSystem.fEpoch, false), -1.0);
			return shipSitu;
		}

		private Ship SpawnDerelictAtMidPoint()
		{
			Ship ship = CrewSim.system.AddDerelict("RandomShip", "OKLG");
			Point point = this.CalculateDeviatedMidPoint();
			ship.objSS.vPosx = point.X;
			ship.objSS.vPosy = point.Y;
			ship.objSS.LockToBO(CrewSim.system.GetNearestBO(ship.objSS, StarSystem.fEpoch, false), -1.0);
			return ship;
		}

		private void SetPlayerPosition()
		{
			this._playerPosition = new ShipSitu();
			this._playerPosition.CopyFrom(CrewSim.coPlayer.ship.objSS, false);
			BodyOrbit nearestBO = CrewSim.system.GetNearestBO(this._playerPosition, StarSystem.fEpoch, false);
			this._playerPosition.LockToBO(nearestBO, -1.0);
		}

		private void ResetPlayerPosition()
		{
			this._playerPosition.UpdateTime(StarSystem.fEpoch, false);
			CrewSim.coPlayer.ship.objSS.UpdateTime(StarSystem.fEpoch, false);
			CrewSim.coPlayer.ship.objSS.vPosx = this._playerPosition.vPosx;
			CrewSim.coPlayer.ship.objSS.vPosy = this._playerPosition.vPosy;
		}

		public void Teardown()
		{
			AIShipManager.ShowDebugLogs = false;
		}

		private Point CalculateDeviatedMidPoint()
		{
			double num = 0.0;
			double num2 = 0.0;
			int num3 = 0;
			foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
			{
				if (keyValuePair.Value.IsStation(false))
				{
					num3++;
					Ship value = keyValuePair.Value;
					value.objSS.UpdateTime(StarSystem.fEpoch, false);
					num += value.objSS.vPosx;
					num2 += value.objSS.vPosy;
				}
			}
			return new Point(num / (double)num3, num2 / (double)num3);
		}

		private Ship _flotillaStation;

		private ShipSitu _playerPosition;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache1;
	}
}
