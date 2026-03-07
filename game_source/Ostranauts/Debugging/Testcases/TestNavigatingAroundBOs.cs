using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Events;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Utils.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Ostranauts.Debugging.Testcases
{
	public class TestNavigatingAroundBOs : ITestCase
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
			if (TestNavigatingAroundBOs.<>f__mg$cache0 == null)
			{
				TestNavigatingAroundBOs.<>f__mg$cache0 = new UnityAction(TestNavigatingAroundBOs.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestNavigatingAroundBOs.<>f__mg$cache0);
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
			CrewSim.objInstance.NewGame("OKLG_FullRegion");
			CrewSim.objInstance.DebugSetPlayerStartingPosition("OKLG", 60f, null);
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestNavigatingAroundBOs.<>f__mg$cache1 == null)
			{
				TestNavigatingAroundBOs.<>f__mg$cache1 = new UnityAction(TestNavigatingAroundBOs.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestNavigatingAroundBOs.<>f__mg$cache1);
		}

		[TestCase(Title = "GoAroundTest", Description = "Spawns 4 Scavs around Ganymed and sends them to destinations opposite of the BO", Order = 1)]
		private IEnumerator GoAroundTest()
		{
			yield return new WaitForSeconds(5f);
			yield return this.SpawnAI(190f, 25.0, "OKLG_FLOT");
			yield return this.SpawnAI(300f, 30.0, "OKLG_NAV1");
			yield return this.SpawnAI(60f, 35.0, "OKLG_NAV2");
			yield return this.SpawnAI(0f, 40.0, "OKLG_SEC");
			yield return new WaitUntil(delegate()
			{
				bool result;
				if (!this._aiShips.All((Ship x) => x.HideFromSystem && !x.bDestroyed))
				{
					result = this._aiShips.Any((Ship x) => x.bDestroyed);
				}
				else
				{
					result = true;
				}
				return result;
			});
			if (this._aiShips.Any((Ship x) => x.bDestroyed))
			{
				TestManager.AssertIsTrue(false, "At least one AI was destroyed");
			}
			else
			{
				TestManager.Log("All AIs docked successfully", LogColor.Green);
			}
			yield break;
		}

		private IEnumerator SpawnAI(float angle, double distanceFromGanyMed, string targetStation)
		{
			Point coords = this.GetSpawnCoordinates(angle, distanceFromGanyMed);
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, coords.X, coords.Y, false);
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			Ship target = CrewSim.system.GetShipByRegID(targetStation);
			ai.Ship.shipSituTarget = target.objSS;
			ai.Ship.shipScanTarget = target;
			this._aiShips.Add(ai.Ship);
			Debug.LogWarning(ai.Ship + " on the way to " + targetStation);
			yield break;
		}

		private Point GetSpawnCoordinates(float angleDegrees, double distanceKm)
		{
			BodyOrbit bo = CrewSim.system.GetBO("1036 Ganymed");
			double num = (bo.fRadiusKM + distanceKm) / 149597872.0;
			double x = bo.dXReal + num * Math.Cos((double)(angleDegrees * 0.017453292f));
			double y = bo.dYReal + num * Math.Sin((double)(angleDegrees * 0.017453292f));
			return new Point(x, y);
		}

		public void Teardown()
		{
			AIShipManager.ShowDebugLogs = false;
		}

		private List<Ship> _aiShips = new List<Ship>();

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache1;
	}
}
