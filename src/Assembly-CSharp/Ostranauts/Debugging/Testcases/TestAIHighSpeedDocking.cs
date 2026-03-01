using System;
using System.Collections;
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
	public class TestAIHighSpeedDocking : ITestCase
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
			if (TestAIHighSpeedDocking.<>f__mg$cache0 == null)
			{
				TestAIHighSpeedDocking.<>f__mg$cache0 = new UnityAction(TestAIHighSpeedDocking.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestAIHighSpeedDocking.<>f__mg$cache0);
			SceneManager.LoadScene("Loading");
			AIShipManager.ShowDebugLogs = true;
		}

		public void Teardown()
		{
			AIShipManager.ShowDebugLogs = false;
			if (CollisionManager.CollisionEvent != null)
			{
				UnityEvent<Tuple<string, string>> collisionEvent = CollisionManager.CollisionEvent;
				if (TestAIHighSpeedDocking.<>f__mg$cache1 == null)
				{
					TestAIHighSpeedDocking.<>f__mg$cache1 = new UnityAction<Tuple<string, string>>(TestAIHighSpeedDocking.OnCollision);
				}
				collisionEvent.RemoveListener(TestAIHighSpeedDocking.<>f__mg$cache1);
			}
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
			if (TestAIHighSpeedDocking.<>f__mg$cache2 == null)
			{
				TestAIHighSpeedDocking.<>f__mg$cache2 = new UnityAction<Tuple<string, string>>(TestAIHighSpeedDocking.OnCollision);
			}
			collisionEvent.AddListener(TestAIHighSpeedDocking.<>f__mg$cache2);
			CrewSim.objInstance.NewGame("VORB_VNCA");
			CrewSim.objInstance.DebugSetPlayerStartingPosition("VORB", 60f, null);
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestAIHighSpeedDocking.<>f__mg$cache3 == null)
			{
				TestAIHighSpeedDocking.<>f__mg$cache3 = new UnityAction(TestAIHighSpeedDocking.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestAIHighSpeedDocking.<>f__mg$cache3);
		}

		[TestCase(Title = "Vorb eastern chase dock", Description = "", Order = 1)]
		private IEnumerator HighSpeedDockingTestEast()
		{
			yield return null;
			Ship vorb = CrewSim.system.GetShipByRegID("VORB");
			yield return new WaitForSeconds(10f);
			Point spawnPos = new Point(vorb.objSS.vPos.X + 3.342293532089815E-07, vorb.objSS.vPos.Y);
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, spawnPos.X, spawnPos.Y, false);
			ai.Ship.objSS.vVelX = vorb.objSS.vVelX;
			ai.Ship.objSS.vVelY = vorb.objSS.vVelY;
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			AIShip.ClearShipTarget(ai.Ship);
			ai.Ship.shipSituTarget = vorb.objSS;
			ai.Ship.shipScanTarget = vorb;
			yield return new WaitUntil(() => ai.Ship.HideFromSystem && !ai.Ship.bDestroyed);
			TestManager.Log("Scav DockedAndDespawned", LogColor.Green);
			yield break;
		}

		[TestCase(Title = "Vorb frontal approach dock", Description = "", Order = 1)]
		private IEnumerator HighSpeedDockingTestSouth()
		{
			yield return null;
			Ship vorb = CrewSim.system.GetShipByRegID("VORB");
			yield return new WaitForSeconds(10f);
			Point spawnPos = new Point(vorb.objSS.vPos.X, vorb.objSS.vPos.Y - 5.347669651343703E-07);
			AIShip ai = AIShipManager.SpawnAI(AIType.Scav, spawnPos.X, spawnPos.Y, false);
			ai.Ship.objSS.vVelX = vorb.objSS.vVelX;
			ai.Ship.objSS.vVelY = vorb.objSS.vVelY;
			yield return new WaitUntil(() => ai.ActiveCommandName == "FlyTo");
			AIShip.ClearShipTarget(ai.Ship);
			ai.Ship.shipSituTarget = vorb.objSS;
			ai.Ship.shipScanTarget = vorb;
			yield return new WaitUntil(() => ai.Ship.HideFromSystem && !ai.Ship.bDestroyed);
			TestManager.Log("Scav DockedAndDespawned", LogColor.Green);
			yield break;
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

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction<Tuple<string, string>> <>f__mg$cache1;

		[CompilerGenerated]
		private static UnityAction<Tuple<string, string>> <>f__mg$cache2;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache3;
	}
}
