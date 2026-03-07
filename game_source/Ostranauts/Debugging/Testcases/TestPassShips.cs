using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Ostranauts.Debugging.Testcases
{
	public class TestPassShips : ITestCase
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
			if (TestPassShips.<>f__mg$cache0 == null)
			{
				TestPassShips.<>f__mg$cache0 = new UnityAction(TestPassShips.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestPassShips.<>f__mg$cache0);
			SceneManager.LoadScene("Loading");
			AIShipManager.ShowDebugLogs = true;
		}

		private static void OnFinishedLoading()
		{
			CrewSim.jsonShip = DataHandler.GetShip("Whistler Hot-Rod");
			CrewSim.objInstance.NewGame("NewGame");
			CrewSim.objInstance.DebugSetPlayerStartingPosition("OKLG", 60f, null);
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestPassShips.<>f__mg$cache1 == null)
			{
				TestPassShips.<>f__mg$cache1 = new UnityAction(TestPassShips.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestPassShips.<>f__mg$cache1);
		}

		public void Teardown()
		{
			AIShipManager.ShowDebugLogs = false;
		}

		[TestCase(Title = "Flotilla overpopulated", Description = "Tests if ship can spawn and move ppl to Flot", Order = 1)]
		private IEnumerator FlotOverpopulated()
		{
			yield return new WaitForSeconds(2f);
			List<AIShip> interregionals;
			if (AIShipManager.dictAIs.TryGetValue("INTERREGIONAL", out interregionals))
			{
				interregionals = interregionals.ToList<AIShip>();
			}
			Ship flot = CrewSim.system.GetShipByRegID("OKLG_FLOT");
			double maxDesired = flot.MaxPopulation;
			int currentCount = flot.GetPeople(false).Count;
			while ((double)currentCount < maxDesired + 10.0)
			{
				CondOwner condOwner = AIShip.AddCrew("OKLGScavCrew", flot, CrewSim.coPlayer.ship.strRegID, false);
				currentCount++;
				Debug.LogWarning(flot.strRegID + " adding " + condOwner.strName);
			}
			AIShipManager.fTimeOfNextPassShip = 1.0;
			yield return new WaitUntil(() => AIShipManager.fTimeOfNextPassShip > 1.0);
			int newCurrentCount = flot.GetPeople(false).Count;
			TestManager.AssertIsTrue(newCurrentCount < currentCount, " Flot pop reduced by" + (newCurrentCount - currentCount));
			List<AIShip> updatedInterregionals;
			AIShipManager.dictAIs.TryGetValue("INTERREGIONAL", out updatedInterregionals);
			AIShip passShip = null;
			if (updatedInterregionals != null)
			{
				foreach (AIShip aiship in updatedInterregionals)
				{
					if (interregionals == null || !interregionals.Contains(aiship))
					{
						if (!(aiship.Ship.origin != "OKLG_FLOT"))
						{
							passShip = aiship;
							break;
						}
					}
				}
			}
			TestManager.AssertIsTrue(passShip != null, " Ship successfully departed");
			yield break;
		}

		[TestCase(Title = "All stations overpopulated", Description = "Tests npc pruning when all stations are full", Order = 1)]
		private IEnumerator Overpopulation()
		{
			Dictionary<Ship, int> dictPop = new Dictionary<Ship, int>();
			int totalpopulation = 0;
			foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
			{
				if (ship != null && !ship.bDestroyed && ship.IsStation(false))
				{
					double maxPopulation = ship.MaxPopulation;
					int num = ship.GetPeople(false).Count;
					while ((double)num < maxPopulation)
					{
						CondOwner condOwner = AIShip.AddCrew("OKLGScavCrew", ship, CrewSim.coPlayer.ship.strRegID, false);
						num++;
					}
					totalpopulation += ship.GetPeople(false).Count;
					dictPop[ship] = ship.GetPeople(false).Count;
				}
			}
			AIShipManager.fTimeOfNextPassShip = 1.0;
			yield return new WaitUntil(() => AIShipManager.fTimeOfNextPassShip > 1.0);
			foreach (KeyValuePair<Ship, int> keyValuePair in dictPop)
			{
				int count = keyValuePair.Key.GetPeople(false).Count;
				if (count != keyValuePair.Value)
				{
					TestManager.AssertIsTrue(count < keyValuePair.Value, keyValuePair.Key.strRegID + " changed pop by " + (count - keyValuePair.Value));
				}
			}
			TestManager.Log("Overpopulation Test finished", LogColor.Neutral);
			yield break;
		}

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache1;
	}
}
