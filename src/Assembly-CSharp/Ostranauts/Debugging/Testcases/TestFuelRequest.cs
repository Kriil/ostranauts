using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.Comms;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Ostranauts.Debugging.Testcases
{
	public class TestFuelRequest : ITestCase
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
			if (TestFuelRequest.<>f__mg$cache0 == null)
			{
				TestFuelRequest.<>f__mg$cache0 = new UnityAction(TestFuelRequest.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestFuelRequest.<>f__mg$cache0);
			SceneManager.LoadScene("Loading");
			AIShipManager.ShowDebugLogs = true;
		}

		private static void OnFinishedLoading()
		{
			CrewSim.jsonShip = DataHandler.GetShip("Whistler Hot-Rod");
			AIShipManager.IsTestCase = true;
			CrewSim.objInstance.NewGame("OKLG_AND_VENUS");
			CrewSim.objInstance.DebugSetPlayerStartingPosition("OKLG", 80f, null);
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestFuelRequest.<>f__mg$cache1 == null)
			{
				TestFuelRequest.<>f__mg$cache1 = new UnityAction(TestFuelRequest.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestFuelRequest.<>f__mg$cache1);
		}

		[TestCase(Title = "Player Fuel Request", Description = "Spawns a ScavAI near the player and sends a request for help; Agree, dock and transfer to finish test", Order = 1)]
		private IEnumerator FuelRequestFromPlayer()
		{
			yield return null;
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, null);
			yield return null;
			TestManager.AssertIsTrue(ai != null, "Scav spawned near player");
			ai.Ship.objSS.PlaceOrbitPosition(CrewSim.coPlayer.ship.objSS);
			ai.Ship.fShallowRCSRemass = 0.0;
			TestManager.Log("Scav Waiting for refuel", LogColor.Green);
			yield return new WaitUntil(() => ai.ActiveCommandName == "Hold");
			TestManager.AssertIsTrue(ai.Ship.Comms.GetMessages(null).Any((ShipMessage x) => x.ID.Contains("SHIPSosAskPlayer")), "Fuel request was sent");
			yield return new WaitUntil(new Func<bool>(CrewSim.coPlayer.ship.IsDocked));
			TestManager.Log("Docked and ready for refueling", LogColor.Green);
			yield return new WaitUntil(() => !CrewSim.coPlayer.ship.IsDocked());
			bool passed;
			if (ai.Ship.fShallowRCSRemass > 0.0)
			{
				passed = ai.Ship.Comms.GetMessages(null).Any((ShipMessage x) => x.ID.Contains("SHIPTransferFuelComplete"));
			}
			else
			{
				passed = false;
			}
			TestManager.AssertIsTrue(passed, "Fuel transfer complete");
			yield break;
		}

		public void Teardown()
		{
			AIShipManager.ShowDebugLogs = false;
		}

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache1;
	}
}
