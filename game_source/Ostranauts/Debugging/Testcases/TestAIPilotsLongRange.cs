using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Core.Models;
using Ostranauts.Events;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Utils.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Ostranauts.Debugging.Testcases
{
	public class TestAIPilotsLongRange : ITestCase
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
			if (TestAIPilotsLongRange.<>f__mg$cache0 == null)
			{
				TestAIPilotsLongRange.<>f__mg$cache0 = new UnityAction(TestAIPilotsLongRange.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestAIPilotsLongRange.<>f__mg$cache0);
			SceneManager.LoadScene("Loading");
			AIShipManager.ShowDebugLogs = true;
		}

		private static void OnFinishedLoading()
		{
			CrewSim.jsonShip = DataHandler.GetShip("Whistler Hot-Rod");
			AIShipManager.IsTestCase = true;
			if (CollisionManager.CollisionEvent == null)
			{
				CollisionManager.CollisionEvent = new CollisionEvent();
			}
			UnityEvent<Tuple<string, string>> collisionEvent = CollisionManager.CollisionEvent;
			if (TestAIPilotsLongRange.<>f__mg$cache1 == null)
			{
				TestAIPilotsLongRange.<>f__mg$cache1 = new UnityAction<Tuple<string, string>>(TestAIPilotsLongRange.OnCollision);
			}
			collisionEvent.AddListener(TestAIPilotsLongRange.<>f__mg$cache1);
			CrewSim.objInstance.NewGame("OKLG_AND_2STATIONS");
			CrewSim.objInstance.DebugSetPlayerStartingPosition("OKLG", 60f, null);
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestAIPilotsLongRange.<>f__mg$cache2 == null)
			{
				TestAIPilotsLongRange.<>f__mg$cache2 = new UnityAction(TestAIPilotsLongRange.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestAIPilotsLongRange.<>f__mg$cache2);
		}

		[TestCase(Title = "ScavAI", Description = "Spawns a ScavAI and a derelict, Scav docks with derelict and then continues to 2nd station", Order = 1)]
		private IEnumerator ScavPilotTest()
		{
			yield return null;
			this._otherStation = this.GetNonOKLGStation();
			this._derelict = this.SpawnDerelictAtMidPoint();
			this._middlePos = new ShipSitu();
			yield return null;
			this._middlePos.CopyFrom(this._derelict.objSS, false);
			TestManager.AssertIsTrue(this._derelict != null, "Derelict was spawned");
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			ai.Ship.shipSituTarget = this._derelict.objSS;
			ai.Ship.shipScanTarget = this._derelict;
			Debug.Log("Set Scav target; Waiting for dockingExpire");
			yield return new WaitUntil(() => ai.Ship.fAIDockingExpire > 0.0);
			TestManager.Log("Scav docked with derelict", LogColor.Green);
			ai.Ship.fAIDockingExpire = 1E-10;
			Debug.Log("Cleared dockingExpire; Waiting for FlyTo");
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			TestManager.Log("Scav undocked from derelict", LogColor.Green);
			AIShip.ClearShipTarget(ai.Ship);
			ai.Ship.shipScanTarget = this._otherStation;
			ai.Ship.shipSituTarget = this._otherStation.objSS;
			yield return new WaitUntil(() => ai.Ship.HideFromSystem && !ai.Ship.bDestroyed);
			TestManager.Log("Scav DockedAndDespawned", LogColor.Green);
			yield break;
		}

		[TestCase(Title = "HaulerRetriever", Description = "Spawns Hauler, has it pick up the derelict and haul it back to ATC", Order = 2)]
		private IEnumerator HaulerRetrieverPilotTest()
		{
			yield return null;
			AIShip ai = AIShipManager.SpawnAI(AIType.HaulerRetriever, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			ai.Ship.shipScanTarget = this._derelict;
			ai.Ship.shipSituTarget = this._derelict.objSS;
			Debug.Log("Set Hauler target; Waiting for docking");
			yield return new WaitUntil(() => ai.Ship.IsDocked());
			TestManager.Log("Hauler docked with derelict", LogColor.Green);
			yield return new WaitUntil(() => ai.Ship.HideFromSystem && !ai.Ship.bDestroyed);
			TestManager.Log("Hauler DockedAndDespawned", LogColor.Green);
			yield break;
		}

		[TestCase(Title = "HaulerDeployer", Description = "Spawns Hauler that ships out a new derelict and anchors it", Order = 3)]
		private IEnumerator HaulerDeployingPilotTest()
		{
			yield return null;
			AIShip ai = AIShipManager.SpawnAI(AIType.HaulerDeployer, null);
			yield return new WaitUntil(() => ai.ActiveCommandName == "HaulShip");
			ai.Ship.shipSituTarget = this._middlePos;
			this._derelict = ai.Ship.GetAllDockedShips().FirstOrDefault<Ship>();
			Debug.Log("Set Hauler target; Waiting for anchoring");
			yield return new WaitUntil(() => !ai.Ship.IsDocked());
			TestManager.Log("Hauler undocked from derelict", LogColor.Green);
			TestManager.AssertIsTrue(this._derelict.objSS.bBOLocked, "Derelict was anchored");
			yield return new WaitUntil(() => ai.Ship.HideFromSystem && !ai.Ship.bDestroyed);
			TestManager.Log("Hauler DockedAndDespawned", LogColor.Green);
			yield break;
		}

		[TestCase(Title = "HaulerDeployerCrash", Description = "Spawns Hauler to dock with derelict and crashes it into OKLG", Order = 4)]
		private IEnumerator HaulerDeployingCrashTest()
		{
			yield return null;
			if (this._middlePos == null)
			{
				this._middlePos = this.GetMiddlePosSitu();
			}
			this._middlePos.UpdateTime(StarSystem.fEpoch, false);
			AIShip ai = AIShipManager.SpawnAI(AIType.HaulerRetriever, this._middlePos.vPosx, this._middlePos.vPosy + 2E-09, false);
			TestManager.AssertIsTrue(ai != null && ai.Ship != null, "Ship was spawned");
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			ai.Ship.shipScanTarget = this._derelict;
			ai.Ship.shipSituTarget = this._derelict.objSS;
			Debug.Log("Set Hauler target; Waiting for docking");
			yield return new WaitUntil(() => ai.Ship.IsDocked());
			TestManager.Log("Hauler docked with derelict", LogColor.Green);
			Ship shipOKLG = CrewSim.system.GetShipByRegID("OKLG");
			TestManager.Log("Hauler Created! Steering it into OKLG", LogColor.Green);
			while (ai.Ship != null && !ai.Ship.bDestroyed)
			{
				AIShipManager.AIIntercept2(ai.Ship, shipOKLG.objSS.vPosx, shipOKLG.objSS.vPosy, ai.Ship.objSS.vVelX, ai.Ship.objSS.vVelY, 0.0, null, 0.0, 0.0);
				yield return null;
			}
			TestManager.AssertIsTrue(ai.Ship == null || ai.Ship.bDestroyed, "Ship destroyed");
			yield break;
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

		private Ship GetNonOKLGStation()
		{
			foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
			{
				if (keyValuePair.Value.IsStation(false) && !(keyValuePair.Value.strRegID == "OKLG"))
				{
					return keyValuePair.Value;
				}
			}
			return null;
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

		private static void OnCollision(Tuple<string, string> collision)
		{
			if (collision == null)
			{
				return;
			}
			TestManager.AssertIsFalse(true, "Collision registered between: " + collision.Item1 + " and " + collision.Item2);
			Debug.LogWarning("Collision registered between: " + collision.Item1 + " and " + collision.Item2);
		}

		public void Teardown()
		{
			AIShipManager.ShowDebugLogs = false;
			if (CollisionManager.CollisionEvent != null)
			{
				UnityEvent<Tuple<string, string>> collisionEvent = CollisionManager.CollisionEvent;
				if (TestAIPilotsLongRange.<>f__mg$cache3 == null)
				{
					TestAIPilotsLongRange.<>f__mg$cache3 = new UnityAction<Tuple<string, string>>(TestAIPilotsLongRange.OnCollision);
				}
				collisionEvent.RemoveListener(TestAIPilotsLongRange.<>f__mg$cache3);
			}
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

		private Ship _derelict;

		private Ship _otherStation;

		private ShipSitu _middlePos;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction<Tuple<string, string>> <>f__mg$cache1;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache2;

		[CompilerGenerated]
		private static UnityAction<Tuple<string, string>> <>f__mg$cache3;
	}
}
