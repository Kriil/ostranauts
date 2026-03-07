using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Ostranauts.Core.Models;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Tools.ExtensionMethods;
using Ostranauts.Trading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Ostranauts.Debugging.Testcases
{
	public class TestLongRangeCargoCoursePlotting : ITestCase
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
			if (TestLongRangeCargoCoursePlotting.<>f__mg$cache0 == null)
			{
				TestLongRangeCargoCoursePlotting.<>f__mg$cache0 = new UnityAction(TestLongRangeCargoCoursePlotting.OnFinishedLoading);
			}
			onFinishLoading.AddListener(TestLongRangeCargoCoursePlotting.<>f__mg$cache0);
			SceneManager.LoadScene("Loading");
			AIShipManager.ShowDebugLogs = true;
		}

		private static void OnFinishedLoading()
		{
			CrewSim.jsonShip = DataHandler.GetShip("Whistler Hot-Rod");
			AIShipManager.IsTestCase = true;
			CrewSim.objInstance.NewGame("NewGame");
			CrewSim.objInstance.DebugSetPlayerStartingPosition("VORB", 60f, null);
			UnityEvent onFinishLoading = CrewSim.OnFinishLoading;
			if (TestLongRangeCargoCoursePlotting.<>f__mg$cache1 == null)
			{
				TestLongRangeCargoCoursePlotting.<>f__mg$cache1 = new UnityAction(TestLongRangeCargoCoursePlotting.OnFinishedLoading);
			}
			onFinishLoading.RemoveListener(TestLongRangeCargoCoursePlotting.<>f__mg$cache1);
		}

		public void Teardown()
		{
			AIShipManager.ShowDebugLogs = false;
		}

		[TestCase(Title = "Regional Venus Plotting Small Hauler", Description = "Tests all Venus routes, uses 2 cargo pods", Order = 1)]
		private IEnumerator VenusRoutesSmall()
		{
			List<string> venusStationIds = new List<string>
			{
				"VNCA",
				"VENC",
				"VCBR",
				"VORB"
			};
			List<Ship> venusStations = new List<Ship>();
			foreach (string strRegID in venusStationIds)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
				venusStations.Add(shipByRegID);
			}
			yield return this.RunRoute(venusStations, 2);
			TestManager.Log("Venus regional plotting, Small Hauler finished", LogColor.Neutral);
			yield break;
		}

		[TestCase(Title = "Regional Venus Plotting Medium Hauler", Description = "Tests all Venus routes, uses 6 cargo pods", Order = 1)]
		private IEnumerator VenusRoutesMedium()
		{
			List<string> venusStationIds = new List<string>
			{
				"VNCA",
				"VENC",
				"VCBR",
				"VORB"
			};
			List<Ship> venusStations = new List<Ship>();
			foreach (string strRegID in venusStationIds)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
				venusStations.Add(shipByRegID);
			}
			yield return this.RunRoute(venusStations, 6);
			TestManager.Log("Venus regional plotting, Medium Hauler finished", LogColor.Neutral);
			yield break;
		}

		[TestCase(Title = "Regional Venus Plotting Large Hauler", Description = "Tests all Venus routes, uses 15 cargo pods", Order = 1)]
		private IEnumerator VenusRoutesLarge()
		{
			List<string> venusStationIds = new List<string>
			{
				"VNCA",
				"VENC",
				"VCBR",
				"VORB"
			};
			List<Ship> venusStations = new List<Ship>();
			foreach (string strRegID in venusStationIds)
			{
				Ship shipByRegID = CrewSim.system.GetShipByRegID(strRegID);
				venusStations.Add(shipByRegID);
			}
			yield return this.RunRoute(venusStations, 15);
			TestManager.Log("Venus regional plotting, Large Hauler finished", LogColor.Neutral);
			yield break;
		}

		[TestCase(Title = "Interregional Course Plotting 18 Pods", Description = "Tests all ATC to ATC routes, uses 18 cargo pods", Order = 1)]
		private IEnumerator InterregionalTestPods18()
		{
			List<Ship> atcStations = new List<Ship>();
			foreach (Ship ship in CrewSim.system.GetAllLoadedShips())
			{
				if (ship.objSS.bIsRegion)
				{
					atcStations.Add(ship);
				}
			}
			yield return this.RunRoute(atcStations, 18);
			TestManager.Log("Long range plotting finished", LogColor.Neutral);
			yield break;
		}

		private IEnumerator RunRoute(List<Ship> stations, int podCount)
		{
			float progressStep = 100f / (float)(stations.Count * stations.Count - stations.Count);
			float currentProgress = 0f;
			int outputCounter = 5;
			AIShip ai = null;
			foreach (Ship atc in stations)
			{
				atc.objSS.UpdateTime(StarSystem.fEpoch, false);
				foreach (Ship atc2 in stations)
				{
					if (atc != atc2)
					{
						currentProgress += progressStep;
						if (currentProgress / (float)outputCounter >= 1f)
						{
							Debug.LogWarning("Progress: " + currentProgress.ToString("N0") + "%");
							outputCounter += 5;
						}
						TradeRouteDTO tradeRoute = this.CreateTradeRoute(atc.strRegID, atc2.strRegID, podCount);
						string shipLoot = AIShipManager.GetCargoShipForRoute(new List<TradeRouteDTO>
						{
							tradeRoute
						});
						if (ai == null || ai.Ship == null || (ai.Ship.objSS == null | ai.Ship.json.strName != shipLoot))
						{
							if (ai != null && ai.Ship != null)
							{
								ai.Ship.Destroy(false);
							}
							yield return null;
							yield return null;
							yield return null;
							ai = new AIShip("OKLG", AIType.HaulerCargo, shipLoot);
						}
						yield return null;
						yield return null;
						if (ai.Ship == null || ai.Ship.objSS == null)
						{
							TestManager.Log(string.Concat(new string[]
							{
								"Could not spawn ship from loot ",
								shipLoot,
								" Route: ",
								atc.strRegID,
								" to ",
								atc2.strRegID
							}), LogColor.Red);
						}
						if (ai.Ship != null && ai.Ship.objSS != null)
						{
							ai.Ship.objSS.UpdateTime(StarSystem.fEpoch, false);
							ai.Ship.objSS.PlaceOrbitPosition(atc.objSS);
							ai.Ship.objSS.vVelX = atc.objSS.vVelX;
							ai.Ship.objSS.vVelY = atc.objSS.vVelY;
							ai.Ship.shipScanTarget = atc2;
							ai.Ship.shipSituTarget = atc2.objSS;
							yield return null;
							bool canReach = AIShipManager.CanReachTarget(ai.Ship, atc2);
							TestManager.AssertIsTrue(canReach, string.Concat(new string[]
							{
								"Route ",
								atc.strRegID,
								" to ",
								atc2.strRegID,
								" ship: ",
								ai.Ship.json.strName
							}));
							yield return null;
						}
					}
				}
			}
			yield break;
		}

		private TradeRouteDTO CreateTradeRoute(string origin, string destination, int podCount)
		{
			TradeRouteDTO tradeRouteDTO = new TradeRouteDTO();
			tradeRouteDTO.OriginStation = origin;
			tradeRouteDTO.DestinationStation = destination;
			tradeRouteDTO.CoCollection = DataHandler.GetDataCoCollection("AnyConsumerGoods");
			tradeRouteDTO.Amount = (int)((float)podCount * ((float)MarketManager.CARGOPOD_DEFAULTMASSCAPACITY / (float)tradeRouteDTO.CoCollection.GetAverageMass()));
			return tradeRouteDTO;
		}

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache0;

		[CompilerGenerated]
		private static UnityAction <>f__mg$cache1;
	}
}
