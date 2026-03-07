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
	public class TestDocking : ITestCase
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
			if (TestDocking.<>f__mg$cache0 == null)
			{
				TestDocking.<>f__mg$cache0 = new UnityAction(TestDocking.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestDocking.<>f__mg$cache0);
			SceneManager.LoadScene("Loading");
		}

		private static void OnFinishedLoading()
		{
			CrewSim.jsonShip = DataHandler.GetShip("Whistler Hot-Rod");
			CrewSim.objInstance.NewGame("OKLG_ONLY");
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestDocking.<>f__mg$cache1 == null)
			{
				TestDocking.<>f__mg$cache1 = new UnityAction(TestDocking.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestDocking.<>f__mg$cache1);
		}

		public void Teardown()
		{
		}

		[TestCase(Title = "Shallow to shallow", Description = "Docks a shallow loaded scav AI to a shallow loaded derelict", Order = 1)]
		private IEnumerator DockShallowToShallow()
		{
			Ship scav = this.GetRandomUndockedScav();
			Ship derelict = this.GetRandomUndockedDerelict();
			if (derelict == null || scav == null)
			{
				Debug.LogError("<color=#008080>Test: can't find ships</color>");
				yield break;
			}
			TestManager.Log("Found scav: " + scav.strRegID + " & derelict: " + derelict.strRegID, LogColor.Neutral);
			TestManager.AssertIsFalse(scav.IsDocked(), scav.strRegID + " is not docked");
			TestManager.AssertIsFalse(derelict.IsDocked(), derelict.strRegID + " is not docked");
			TestManager.Log("Docking both ships", LogColor.Neutral);
			scav.Dock(derelict, false);
			yield return null;
			TestManager.AssertIsTrue(scav.IsDocked(), scav.strRegID + " is docked");
			TestManager.AssertIsTrue(derelict.IsDocked(), derelict.strRegID + " is docked");
			this._dockedDerelict = derelict;
			this._dockedScav = scav;
			yield break;
		}

		[TestCase(Title = "Save + Load shallow to shallow", Description = "Docks a shallow loaded scav to a derelict,saves and loads them again; AIs auto dock on load, so docking should not get saved", Order = 2)]
		private IEnumerator LoadShallowDockedShips()
		{
			Ship scav = this._dockedScav;
			Ship derelict = this._dockedDerelict;
			if (derelict == null || scav == null)
			{
				TestManager.Log("Test: can't find ships", LogColor.Red);
				yield break;
			}
			TestManager.AssertIsTrue(scav.IsDocked(), scav.strRegID + " is docked");
			TestManager.AssertIsTrue(derelict.IsDocked(), derelict.strRegID + " is docked");
			TestManager.Log("Getting ship JSONs", LogColor.Neutral);
			JsonShip saveScav = scav.GetJSON(scav.json.strName, true, null);
			JsonShip saveDerelict = derelict.GetJSON(derelict.json.strName, true, null);
			yield return null;
			TestManager.AssertIsFalse(saveScav.aDocked != null && saveScav.aDocked.Contains(saveDerelict.strRegID), saveScav.strRegID + " is not json docked");
			TestManager.AssertIsFalse(saveDerelict.aDocked != null && saveDerelict.aDocked.Contains(saveScav.strRegID), saveDerelict.strRegID + " is not json docked");
			DataHandler.dictShips.Add("testScav", saveScav);
			DataHandler.dictShips.Add("testDerelict", saveDerelict);
			TestManager.Log("Loading new ships from JSONs", LogColor.Neutral);
			scav = CrewSim.system.SpawnShip("testScav", "O-Scav", Ship.Loaded.Shallow, Ship.Damage.Used, null, 100, false);
			derelict = CrewSim.system.SpawnShip("testDerelict", "O-Dere", Ship.Loaded.Shallow, Ship.Damage.Derelict, null, 100, false);
			yield return null;
			TestManager.AssertIsFalse(scav.IsDocked(), scav.strRegID + " is not docked");
			TestManager.AssertIsFalse(derelict.IsDocked(), derelict.strRegID + " is not docked");
			derelict.Destroy(true);
			scav.Destroy(true);
			yield break;
		}

		[TestCase(Title = "Ferry to docked ship", Description = "will dock a scav to a derelict and then move the player to the scav ship", Order = 3)]
		private IEnumerator FerryToDocked()
		{
			Ship scav = this.GetRandomUndockedScav();
			Ship derelict = this.GetRandomUndockedDerelict();
			if (derelict == null || scav == null)
			{
				TestManager.Log("Test: can't find ships", LogColor.Red);
				yield break;
			}
			TestManager.Log("Found scav: " + scav.strRegID + " & derelict: " + derelict.strRegID, LogColor.Neutral);
			scav.Dock(derelict, false);
			TestManager.AssertIsTrue(scav.IsDocked(), scav.strRegID + " is docked");
			TestManager.Log("Moving player & fully loading both ships", LogColor.Neutral);
			CrewSim.objInstance.TeleportCO(CrewSim.coPlayer, scav.strRegID);
			yield return new WaitForSeconds(1f);
			TestManager.AssertIsTrue(scav.IsDocked(), scav.strRegID + " is docked");
			TestManager.AssertIsTrue(derelict.IsDocked(), derelict.strRegID + " is docked");
			TestManager.AssertIsTrue(CrewSim.GetAllLoadedShips().Contains(derelict), derelict.strRegID + " is fully loaded");
			TestManager.AssertIsTrue(CrewSim.GetAllLoadedShips().Contains(scav), scav.strRegID + " is fully loaded");
			yield break;
		}

		[TestCase(Title = "Buy ship and dock at OKLG", Description = "spawns a hidden vendor ship and docks it at OKLG", Order = 4)]
		private IEnumerator BuyUsedShipDockedAtOKLG()
		{
			Ship oklg = CrewSim.system.GetShipByRegID("OKLG");
			TestManager.AssertIsFalse(oklg.IsDocked(), oklg.strRegID + " is not docked");
			Ship vendorShip = this.SpawnVendorShip();
			TestManager.Log("Spawned new vendorship: " + vendorShip.strRegID, LogColor.Neutral);
			yield return new WaitForSeconds(1f);
			if (oklg.CanBeDockedWith())
			{
				vendorShip.Dock(oklg, false);
				TestManager.Log("Docking both ships", LogColor.Neutral);
			}
			yield return null;
			TestManager.AssertIsTrue(vendorShip.IsDocked(), vendorShip.strRegID + " is shallow docked");
			TestManager.AssertIsTrue(oklg.IsDocked(), oklg.strRegID + " is shallow docked");
			CrewSim.objInstance.TeleportCO(CrewSim.coPlayer, oklg.strRegID);
			yield return new WaitForSeconds(1f);
			TestManager.Log("Moving player & fully loading both ships", LogColor.Neutral);
			TestManager.AssertIsTrue(CrewSim.GetAllLoadedShips().Contains(vendorShip), vendorShip.strRegID + " is fully loaded");
			TestManager.AssertIsTrue(CrewSim.GetAllLoadedShips().Contains(oklg), oklg.strRegID + " is fully loaded");
			yield break;
		}

		private Ship SpawnVendorShip()
		{
			List<string> lootNames = DataHandler.GetLoot("RandomShipBrokerSpecialOffer").GetLootNames(null, false, null);
			if (lootNames == null || lootNames.Count == 0)
			{
				return null;
			}
			JsonShip ship = DataHandler.GetShip(lootNames[0]);
			Ship ship2 = CrewSim.system.SpawnShip(ship.strName, null, Ship.Loaded.Shallow, Ship.Damage.New, "vendor-temp", 100, false);
			ship2.HideFromSystem = true;
			ship2.gameObject.SetActive(true);
			return ship2;
		}

		private Ship GetRandomUndockedScav()
		{
			foreach (KeyValuePair<string, List<AIShip>> keyValuePair in AIShipManager.dictAIs)
			{
				foreach (AIShip aiship in keyValuePair.Value)
				{
					if (aiship.AIType == AIType.Scav)
					{
						Ship ship = aiship.Ship;
						if (!ship.IsDocked() && (ship.json == null || ship.json.aDocked == null || ship.json.aDocked.Length <= 0))
						{
							AIShipManager.UnregisterShip(ship);
							return ship;
						}
					}
				}
			}
			return null;
		}

		private Ship GetRandomUndockedDerelict()
		{
			foreach (Ship ship in CrewSim.system.dictShips.Values)
			{
				if (ship.IsDerelict() && !ship.IsDocked() && (ship.json == null || ship.json.aDocked == null || ship.json.aDocked.Length <= 0))
				{
					return ship;
				}
			}
			return null;
		}

		private Ship _dockedScav;

		private Ship _dockedDerelict;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache1;
	}
}
