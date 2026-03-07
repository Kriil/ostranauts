using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using Ostranauts.Objectives;
using UnityEngine;

public class PlotManager
{
	public static void Init(JsonGameSave jGS = null)
	{
		PlotManager.dictPlotsActive = new Dictionary<string, JsonPlotSave>();
		PlotManager.dictPlotsOld = new Dictionary<string, JsonPlotSave>();
		PlotManager.pRecheckPlotType = PlotManager.PlotTensionType.NONE;
		PlotManager.bDebugCheckAll = false;
		PlotManager.nPlayerNoticed = 0;
		if (jGS == null)
		{
			return;
		}
		if (jGS.aPlots != null)
		{
			foreach (JsonPlotSave jsonPlotSave in jGS.aPlots)
			{
				if (jsonPlotSave != null && !string.IsNullOrEmpty(jsonPlotSave.strPlotName))
				{
					PlotManager.dictPlotsActive.Add(jsonPlotSave.strPlotName, jsonPlotSave);
				}
			}
		}
		if (jGS.aPlotsOld != null)
		{
			foreach (JsonPlotSave jsonPlotSave2 in jGS.aPlotsOld)
			{
				if (jsonPlotSave2 != null && !string.IsNullOrEmpty(jsonPlotSave2.strPlotName))
				{
					PlotManager.dictPlotsOld.Add(jsonPlotSave2.strPlotName, jsonPlotSave2);
				}
			}
		}
	}

	public static void Update()
	{
		if (PlotManager.pRecheckPlotType == PlotManager.PlotTensionType.NONE)
		{
			return;
		}
		PlotManager.CheckPlots(CrewSim.GetSelectedCrew(), PlotManager.pRecheckPlotType);
		PlotManager.pRecheckPlotType = PlotManager.PlotTensionType.NONE;
	}

	public static void AddPlotCheck(PlotManager.PlotTensionType pType)
	{
		PlotManager.pRecheckPlotType |= pType;
	}

	private static bool PlotTypeMatch(JsonPlotBeat jpb, PlotManager.PlotTensionType pType)
	{
		return pType != PlotManager.PlotTensionType.NONE && (pType == (PlotManager.PlotTensionType)3 || (jpb.bTension && (pType & PlotManager.PlotTensionType.TENSION) == PlotManager.PlotTensionType.TENSION) || (jpb.bRelease && (pType & PlotManager.PlotTensionType.RELEASE) == PlotManager.PlotTensionType.RELEASE));
	}

	public static bool CheckPlots(CondOwner coProtag, PlotManager.PlotTensionType pType)
	{
		if (coProtag == null || pType == PlotManager.PlotTensionType.NONE)
		{
			return false;
		}
		PlotManager.nPlayerNoticed = 0;
		List<string> list = new List<string>(DataHandler.dictPlots.Keys);
		MathUtils.ShuffleList<string>(list);
		for (int i = 0; i < list.Count; i++)
		{
			string strPlotName = list[i];
			if (!PlotManager.CheckPlot(strPlotName, coProtag, pType, null, false) || !PlotManager.bDebugCheckAll)
			{
			}
		}
		PlotManager.bDebugCheckAll = false;
		return PlotManager.nPlayerNoticed > 0;
	}

	public static bool CheckPlot(string strPlotName, CondOwner coProtag, PlotManager.PlotTensionType pType, JsonPlotSave jps = null, bool bForcePlot = false)
	{
		if (coProtag == null || coProtag.ship == null || string.IsNullOrEmpty(strPlotName) || pType == PlotManager.PlotTensionType.NONE)
		{
			return false;
		}
		JsonPlot jsonPlot;
		if (!DataHandler.dictPlots.TryGetValue(strPlotName, out jsonPlot))
		{
			if (PlotManager.bDebugLogging)
			{
				Debug.Log("<color=#992299ff>Missing Plot: " + strPlotName + "; Skipping.</color> ");
			}
			return false;
		}
		if (jsonPlot.bNoCheck || string.IsNullOrEmpty(jsonPlot.strName))
		{
			if (PlotManager.bDebugLogging)
			{
				if (jsonPlot.bNoCheck)
				{
					Debug.Log("<color=#992299ff>bNoCheck Plot: " + strPlotName + "; Skipping.</color> ");
				}
				else
				{
					Debug.Log("<color=#992299ff>Empty Plot name: " + strPlotName + "; Skipping.</color> ");
				}
			}
			return false;
		}
		int num = 0;
		if (jps == null && PlotManager.dictPlotsActive.TryGetValue(jsonPlot.strName, out jps) && coProtag.strID != jps.dictCOTokens["[protag]"])
		{
			if (PlotManager.bDebugLogging)
			{
				Debug.Log(string.Concat(new string[]
				{
					"<color=#992299ff>Plot already started with OLD [protag]: ",
					jps.dictCOTokens["[protag]"],
					"; Skipping NEW [protag]: ",
					coProtag.strID,
					"</color> "
				}));
			}
			return false;
		}
		if (jps != null)
		{
			num = jps.nPhase;
		}
		if (num >= jsonPlot.aPhases.Length)
		{
			if (PlotManager.bDebugLogging)
			{
				Debug.Log(string.Concat(new object[]
				{
					"<color=#992299ff>Phase ",
					num,
					" doesn't exist on ",
					jsonPlot.strName,
					"; Skipping.</color> "
				}));
			}
			return false;
		}
		string[] array = jsonPlot.aPhases[num];
		if (array == null)
		{
			if (PlotManager.bDebugLogging)
			{
				Debug.Log(string.Concat(new object[]
				{
					"<color=#992299ff>null phase ",
					num,
					" on ",
					jsonPlot.strName,
					"; Skipping.</color> "
				}));
			}
			return false;
		}
		foreach (string text in array)
		{
			if (string.IsNullOrEmpty(text))
			{
				if (PlotManager.bDebugLogging)
				{
					Debug.Log("<color=#992299ff>Null or empty Plot Beat name on: " + jsonPlot.strName + "; Skipping.</color> ");
				}
			}
			else
			{
				JsonPlotBeat plotBeat = DataHandler.GetPlotBeat(text);
				if (plotBeat == null)
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log("<color=#992299ff>Null Plot Beat: " + text + "; Skipping.</color> ");
					}
				}
				else if (!PlotManager.PlotTypeMatch(plotBeat, pType))
				{
					if (PlotManager.bDebugLogging)
					{
						string text2 = "; Wanted: ";
						text2 += "+NONE";
						if ((pType & PlotManager.PlotTensionType.TENSION) == PlotManager.PlotTensionType.TENSION)
						{
							text2 += "+Tension";
						}
						if ((pType & PlotManager.PlotTensionType.RELEASE) == PlotManager.PlotTensionType.RELEASE)
						{
							text2 += "+Release";
						}
						Debug.Log("<color=#992299ff>Wrong Plot Beat Tension/Release Type on " + text + text2 + "; Skipping.</color> ");
					}
				}
				else
				{
					if (plotBeat.bNoticeable && PlotManager.nPlayerNoticed > 0)
					{
						if (bForcePlot)
						{
							Debug.Log("Ordinarily would have too many noticeable plots! But ForcePlot is ignoring this");
						}
						else if (!PlotManager.bDebugCheckAll)
						{
							if (PlotManager.bDebugLogging)
							{
								Debug.Log("<color=#992299ff>Too Many Noticeable Plots Already: " + plotBeat.strName + "; Skipping.</color> ");
							}
							goto IL_7B9;
						}
					}
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new object[]
						{
							"<color=#992299ff>Checking Plot Beat Trigger: ",
							jsonPlot.strName,
							".",
							plotBeat.strName,
							"; Phase: ",
							num,
							"</color> "
						}));
					}
					Interaction interaction = (!bForcePlot) ? PlotManager.GetIAFromString(plotBeat.strIATrigger, coProtag, jps, null, false) : PlotManager.GetIAFromString(plotBeat.strIATrigger, coProtag, jps, null, true);
					if (interaction != null)
					{
						interaction.strPlot = jsonPlot.strName;
						if (plotBeat.bNoticeable)
						{
							PlotManager.nPlayerNoticed++;
							if (plotBeat.bRelease)
							{
								BeatManager.ResetReleaseTimer();
							}
							if (plotBeat.bTension)
							{
								BeatManager.ResetTensionTimer();
							}
						}
						if (jps == null)
						{
							jps = new JsonPlotSave();
							jps.Init(jsonPlot, coProtag);
							PlotManager.dictPlotsActive[jsonPlot.strName] = jps;
						}
						if (PlotManager.bDebugLogging)
						{
							string text3 = string.Concat(new object[]
							{
								"Plot Triggered: ",
								jsonPlot.strName,
								".",
								plotBeat.strName,
								"; Phase: ",
								num
							});
							foreach (KeyValuePair<string, string> keyValuePair in jps.dictCOTokens)
							{
								string text4 = text3;
								text3 = string.Concat(new string[]
								{
									text4,
									"; ",
									keyValuePair.Key,
									": ",
									keyValuePair.Value
								});
							}
							Debug.Log("<color=magenta>" + text3 + "</color> ");
						}
						PlotManager.RunPlotBeat(plotBeat, interaction, jps);
						if (jps.nPhase >= jsonPlot.aPhases.Length)
						{
							string text5 = null;
							if (jsonPlot.strLootNextPlot != null)
							{
								if (PlotManager.bDebugLogging)
								{
									Debug.Log("<color=#992299ff>strLootNextPlot Found. Checking: " + jsonPlot.strLootNextPlot + " Plots.</color> ");
								}
								List<string> lootNames = DataHandler.GetLoot(jsonPlot.strLootNextPlot).GetLootNames(null, false, null);
								foreach (string text6 in lootNames)
								{
									JsonPlotSave jsonPlotSave = new JsonPlotSave();
									jsonPlotSave.Init(text6);
									foreach (KeyValuePair<string, string> keyValuePair2 in jps.dictCOTokens)
									{
										jsonPlotSave.dictCOTokens.Add(keyValuePair2.Key, keyValuePair2.Value);
									}
									if (PlotManager.CheckPlot(text6, coProtag, pType, jsonPlotSave, false))
									{
										PlotManager.dictPlotsActive[text6] = jsonPlotSave;
										text5 = text6;
										break;
									}
								}
								if (text5 == null)
								{
									if (PlotManager.bDebugLogging)
									{
										Debug.Log("<color=#992299ff>strLootNextPlot: " + jsonPlot.strLootNextPlot + " couldn't continue. Pausing for now.</color> ");
									}
									return false;
								}
							}
							if (text5 == null || text5 != jsonPlot.strName)
							{
								PlotManager.dictPlotsActive.Remove(jsonPlot.strName);
							}
							PlotManager.dictPlotsOld[jsonPlot.strName] = jps;
							if (PlotManager.bDebugLogging)
							{
								string text7 = string.Concat(new object[]
								{
									"Plot Ended: ",
									jsonPlot.strName,
									".",
									plotBeat.strName,
									"; Phase: ",
									num
								});
								foreach (KeyValuePair<string, string> keyValuePair3 in jps.dictCOTokens)
								{
									string text4 = text7;
									text7 = string.Concat(new string[]
									{
										text4,
										"; ",
										keyValuePair3.Key,
										": ",
										keyValuePair3.Value
									});
								}
								Debug.Log("<color=magenta>" + text7 + "</color> ");
							}
						}
						MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(false);
						return true;
					}
					if (PlotManager.bDebugLogging)
					{
						Debug.Log("<color=#992299ff>strIATrigger failed: " + plotBeat.strIATrigger + "; Skipping.</color> ");
					}
				}
			}
			IL_7B9:;
		}
		return false;
	}

	private static void RunPlotBeat(JsonPlotBeat jpb, Interaction iaTrigger, JsonPlotSave jps)
	{
		if (jpb == null || jps == null || iaTrigger == null)
		{
			return;
		}
		jps.nPhase += jpb.nPhaseChange;
		if (!string.IsNullOrEmpty(jpb.strTokenSetUs) && iaTrigger.objUs != null)
		{
			jps.dictCOTokens[jpb.strTokenSetUs] = iaTrigger.objUs.strID;
		}
		if (!string.IsNullOrEmpty(jpb.strTokenSetThem) && iaTrigger.objThem != null)
		{
			jps.dictCOTokens[jpb.strTokenSetThem] = iaTrigger.objThem.strID;
		}
		if (!string.IsNullOrEmpty(jpb.strTokenSet3rd) && iaTrigger.obj3rd != null)
		{
			jps.dictCOTokens[jpb.strTokenSet3rd] = iaTrigger.obj3rd.strID;
		}
		List<string> list = (jps.aCompletedBeats == null) ? new List<string>() : jps.aCompletedBeats.ToList<string>();
		if (!string.IsNullOrEmpty(jps.strCurrentBeat) && !list.Contains(jps.strCurrentBeat))
		{
			list.Add("<s>" + jps.strCurrentBeat + "</s>");
		}
		jps.aCompletedBeats = list.ToArray();
		CondOwner condOwner = iaTrigger.objUs;
		if (PlotManager.GetAllPlotQABs(iaTrigger.objUs, iaTrigger.objThem).Count > 0)
		{
			condOwner = iaTrigger.objThem;
		}
		else if (PlotManager.GetAllPlotQABs(iaTrigger.objUs, iaTrigger.obj3rd).Count > 0)
		{
			condOwner = iaTrigger.obj3rd;
		}
		jps.strCOFocusID = condOwner.strID;
		if (jpb.bNoticeable)
		{
			jps.strCurrentBeat = iaTrigger.GetTextLinked(condOwner, "<color=#FFCC00>", "</color>");
		}
		MonoSingleton<ObjectiveTracker>.Instance.CompleteExistingPlotObjective(jps);
		MonoSingleton<ObjectiveTracker>.Instance.AddPlotObjective(jpb, jps);
		iaTrigger.ApplyChain(null);
	}

	public static List<Interaction> GetAllPlotQABs(CondOwner coUs, CondOwner coTarget)
	{
		List<Interaction> list = new List<Interaction>();
		if (coUs == null || coUs.ship == null || coTarget == null)
		{
			return list;
		}
		foreach (JsonPlot jsonPlot in DataHandler.dictPlots.Values)
		{
			if (!jsonPlot.bNoCheck && !string.IsNullOrEmpty(jsonPlot.strName))
			{
				JsonPlotSave jsonPlotSave = null;
				if (PlotManager.dictPlotsActive.TryGetValue(jsonPlot.strName, out jsonPlotSave))
				{
					if (!(coUs.strID != jsonPlotSave.dictCOTokens["[protag]"]))
					{
						int nPhase = jsonPlotSave.nPhase;
						if (nPhase < jsonPlot.aIADos.Length)
						{
							string[] array = jsonPlot.aIADos[nPhase];
							if (array != null)
							{
								foreach (string strIA in array)
								{
									Interaction iafromString = PlotManager.GetIAFromString(strIA, coUs, jsonPlotSave, coTarget, false);
									if (iafromString != null)
									{
										iafromString.strPlot = jsonPlot.strName;
										list.Add(iafromString);
									}
								}
							}
						}
					}
				}
			}
		}
		return list;
	}

	public static string GetIANameFromString(string strIA)
	{
		if (string.IsNullOrEmpty(strIA))
		{
			return null;
		}
		string[] array = strIA.Split(new char[]
		{
			','
		});
		return array[0];
	}

	private static Interaction GetIAFromString(string strIA, CondOwner coProtag, JsonPlotSave jps = null, CondOwner coTarget = null, bool bForcePlot = false)
	{
		if (string.IsNullOrEmpty(strIA))
		{
			if (PlotManager.bDebugLogging)
			{
				Debug.Log("<color=#992299ff>Trigger failed: null strIATrigger requested.</color> ");
			}
			return null;
		}
		string[] array = strIA.Split(new char[]
		{
			','
		});
		Interaction interaction = DataHandler.GetInteraction(array[0], null, false);
		if (interaction == null)
		{
			if (PlotManager.bDebugLogging)
			{
				Debug.Log("<color=#992299ff>Trigger failed: strIATrigger not found: " + strIA + "</color> ");
			}
			return null;
		}
		string text = null;
		CondOwner condOwner = null;
		if (array.Length > 1)
		{
			if (jps == null)
			{
				if (PlotManager.bDebugLogging)
				{
					Debug.Log("<color=#992299ff>Trigger failed: Plot not started yet, and requires an [us] token: " + strIA + "</color> ");
				}
				return null;
			}
			condOwner = null;
			if (!string.IsNullOrEmpty(array[1]) && !(array[1] == "null"))
			{
				if (!jps.dictCOTokens.TryGetValue(array[1], out text))
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new string[]
						{
							"<color=#992299ff>Trigger failed: Plot trigger ",
							strIA,
							" requires unknown token: ",
							array[1],
							"</color> "
						}));
					}
					return null;
				}
				if (!DataHandler.mapCOs.TryGetValue(text, out condOwner))
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new string[]
						{
							"<color=#992299ff>Trigger failed: Plot trigger ",
							strIA,
							" cannot find CO ",
							text,
							" for token: ",
							array[1],
							"</color> "
						}));
					}
					return null;
				}
			}
			interaction.objUs = condOwner;
		}
		if (array.Length > 2)
		{
			condOwner = null;
			if (!string.IsNullOrEmpty(array[2]) && !(array[2] == "null"))
			{
				if (!jps.dictCOTokens.TryGetValue(array[2], out text))
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new string[]
						{
							"<color=#992299ff>Trigger failed: Plot trigger ",
							strIA,
							" requires unknown token: ",
							array[2],
							"</color> "
						}));
					}
					return null;
				}
				if (!DataHandler.mapCOs.TryGetValue(text, out condOwner))
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new string[]
						{
							"<color=#992299ff>Trigger failed: Plot trigger ",
							strIA,
							" cannot find CO ",
							text,
							" for token: ",
							array[2],
							"</color> "
						}));
					}
					return null;
				}
				if (coTarget != null && condOwner != coTarget)
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new string[]
						{
							"<color=#992299ff>Trigger failed: Plot trigger ",
							strIA,
							" target CO ",
							coTarget.strID,
							" doesn't match token CO: ",
							text,
							"</color> "
						}));
					}
					return null;
				}
			}
			interaction.objThem = condOwner;
		}
		if (array.Length > 3)
		{
			condOwner = null;
			if (!string.IsNullOrEmpty(array[3]) && !(array[3] == "null"))
			{
				if (!jps.dictCOTokens.TryGetValue(array[3], out text))
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new string[]
						{
							"<color=#992299ff>Trigger failed: Plot trigger ",
							strIA,
							" requires unknown token: ",
							array[3],
							"</color> "
						}));
					}
					return null;
				}
				if (!DataHandler.mapCOs.TryGetValue(text, out condOwner))
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log(string.Concat(new string[]
						{
							"<color=#992299ff>Trigger failed: Plot trigger ",
							strIA,
							" cannot find CO ",
							text,
							" for token: ",
							array[3],
							"</color> "
						}));
					}
					return null;
				}
			}
			interaction.obj3rd = condOwner;
		}
		if (interaction.objUs == null)
		{
			interaction.objUs = coProtag;
		}
		List<string> list = null;
		if (jps != null && jps.dictCOTokens != null)
		{
			list = new List<string>(jps.dictCOTokens.Values);
		}
		else
		{
			list = new List<string>
			{
				coProtag.strID
			};
		}
		if (interaction.objThem == null)
		{
			if (interaction.strThemType == "Self")
			{
				interaction.objThem = coProtag;
			}
			else
			{
				if (interaction.PSpecTestThem != null)
				{
					if (coTarget != null)
					{
						if (coTarget.pspec == null)
						{
							if (PlotManager.bDebugLogging)
							{
								Debug.Log(string.Concat(new string[]
								{
									"<color=#992299ff>Trigger failed: Plot trigger ",
									strIA,
									" no pspec on ",
									coTarget.strID,
									"</color> "
								}));
							}
							return null;
						}
						if (interaction.ShipTestThem != null && !interaction.ShipTestThem.Matches(coTarget.ship, interaction.objUs))
						{
							if (PlotManager.bDebugLogging)
							{
								Debug.Log(string.Concat(new string[]
								{
									"<color=#992299ff>Trigger failed: Plot trigger ",
									strIA,
									" ShipTestThem failed for ",
									coTarget.strID,
									"</color> "
								}));
							}
							return null;
						}
						if (interaction.objUs.pspec != null)
						{
							if (interaction.objUs.pspec.IsCOMyMother(interaction.PSpecTestThem, coTarget))
							{
								interaction.objThem = coTarget;
							}
						}
						else if (interaction.PSpecTestThem.Matches(coTarget))
						{
							interaction.objThem = coTarget;
						}
					}
					else
					{
						PersonSpec personSpec;
						if (jps != null && jps.dictCOTokens != null)
						{
							personSpec = StarSystem.GetPerson(interaction.PSpecTestThem, coProtag.socUs, false, new List<string>(jps.dictCOTokens.Values), interaction.ShipTestThem);
						}
						else
						{
							personSpec = StarSystem.GetPerson(interaction.PSpecTestThem, coProtag.socUs, false, null, interaction.ShipTestThem);
						}
						if (personSpec == null || personSpec.GetCO() == null)
						{
							if (!bForcePlot)
							{
								if (PlotManager.bDebugLogging)
								{
									Debug.Log("<color=#992299ff>Trigger failed: Plot trigger " + strIA + " PSpecTestThem/ShipTestThem failed to find a valid [them]</color> ");
								}
								return null;
							}
							Debug.Log("Force-making a person to fit the PSpecTest: " + interaction.PSpecTestThem.strName);
							personSpec = new PersonSpec(interaction.PSpecTestThem, true);
							Debug.Log(string.Concat(new object[]
							{
								"#NPC# Creating new NPC ",
								personSpec.FullName,
								"; NewRegion: ",
								AIShipManager.NewRegion,
								"; ps: ",
								personSpec
							}));
							CondOwner condOwner2 = personSpec.MakeCondOwner(PersonSpec.StartShip.OLD, coProtag.ship);
							if (condOwner2.ship != coProtag.ship)
							{
								CrewSim.MoveCO(condOwner2, coProtag.ship, false);
							}
							string[] aReqs = DataHandler.GetCondTrigger(interaction.PSpecTestThem.strCTRelFind).aReqs;
							coProtag.socUs.AddPerson(new Relationship(personSpec, new List<string>(aReqs), new List<string>()));
						}
						interaction.objThem = personSpec.GetCO();
					}
				}
				else if (interaction.CTTestThem != null)
				{
					if (coTarget != null)
					{
						if (interaction.ShipTestThem != null && !interaction.ShipTestThem.Matches(coTarget.ship, interaction.objUs))
						{
							if (PlotManager.bDebugLogging)
							{
								Debug.Log(string.Concat(new string[]
								{
									"<color=#992299ff>Trigger failed: Plot trigger ",
									strIA,
									" ShipTestThem failed for ",
									coTarget.strID,
									"</color> "
								}));
							}
							return null;
						}
						if (interaction.CTTestThem.Triggered(coTarget, null, true))
						{
							interaction.objThem = coTarget;
						}
					}
					else
					{
						List<CondOwner> list2 = new List<CondOwner>();
						if (interaction.ShipTestThem == null || interaction.ShipTestThem.Matches(coProtag.ship, coProtag))
						{
							list2.AddRange(coProtag.ship.GetCOs(interaction.CTTestThem, true, false, true));
						}
						foreach (Ship ship in coProtag.ship.GetAllDockedShipsFull())
						{
							if (interaction.ShipTestThem == null || interaction.ShipTestThem.Matches(ship, coProtag))
							{
								list2.AddRange(ship.GetCOs(interaction.CTTestThem, true, false, true));
							}
						}
						if (jps != null && jps.dictCOTokens != null)
						{
							for (int i = list2.Count - 1; i >= 0; i--)
							{
								if (jps.dictCOTokens.ContainsValue(list2[i].strID))
								{
									list2.RemoveAt(i);
								}
							}
						}
						if (list2.Count > 0)
						{
							if (bForcePlot || interaction.CTTestThem.nFilterMultiple > 0)
							{
								CondOwner objThem = list2[0];
								float num = float.MaxValue;
								for (int j = 0; j < Mathf.Min(list2.Count, 50); j++)
								{
									Tile tileAtWorldCoords = coProtag.ship.GetTileAtWorldCoords1(coProtag.tf.position.x, coProtag.tf.position.y, true, true);
									Tile tileAtWorldCoords2 = coProtag.ship.GetTileAtWorldCoords1(list2[j].tf.position.x, list2[j].tf.position.y, true, true);
									if ((float)TileUtils.TileRange(tileAtWorldCoords, tileAtWorldCoords2) < num)
									{
										num = (float)TileUtils.TileRange(tileAtWorldCoords, tileAtWorldCoords2);
										objThem = list2[j];
									}
								}
								interaction.objThem = objThem;
							}
							else
							{
								interaction.objThem = list2[MathUtils.Rand(0, list2.Count - 1, MathUtils.RandType.Flat, null)];
							}
						}
						else if (PlotManager.bDebugLogging)
						{
							Debug.Log("<color=#992299ff>Trigger failed: Plot trigger " + strIA + " PSpecTestThem/ShipTestThem failed to find a valid [them]</color> ");
						}
					}
				}
				if (interaction.objThem == null)
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log("<color=#992299ff>Trigger failed: Plot trigger " + strIA + " failed to find a valid [them]</color> ");
					}
					return null;
				}
				if (!list.Contains(interaction.objThem.strID))
				{
					list.Add(interaction.objThem.strID);
				}
			}
		}
		interaction.bVerboseTrigger = true;
		Interaction interaction2 = interaction;
		CondOwner objUs = interaction.objUs;
		CondOwner objThem2 = interaction.objThem;
		List<string> aForbid3rds = list;
		if (interaction2.Triggered(objUs, objThem2, false, false, false, true, aForbid3rds))
		{
			return interaction;
		}
		if (PlotManager.bDebugLogging)
		{
			Debug.Log(string.Concat(new string[]
			{
				"<color=#992299ff>",
				interaction.strName,
				": ",
				interaction.FailReasons(true, true, true),
				"</color> "
			}));
		}
		return null;
	}

	public static string GetAllPlots(string plotname = "")
	{
		string text = string.Empty;
		foreach (JsonPlotSave jsonPlotSave in PlotManager.dictPlotsActive.Values)
		{
			if (plotname == string.Empty || jsonPlotSave.strPlotName.ToLower().Contains(plotname))
			{
				string text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					"\n",
					jsonPlotSave.strPlotName,
					" : ",
					jsonPlotSave.nPhase.ToString()
				});
				foreach (KeyValuePair<string, string> keyValuePair in jsonPlotSave.dictCOTokens)
				{
					text2 = text;
					text = string.Concat(new string[]
					{
						text2,
						"\n- ",
						keyValuePair.Key,
						" : ",
						keyValuePair.Value
					});
				}
			}
		}
		return text;
	}

	public static JsonPlotSave GetActivePlot(string strPlot)
	{
		if (string.IsNullOrEmpty(strPlot))
		{
			return null;
		}
		if (PlotManager.dictPlotsActive.ContainsKey(strPlot))
		{
			return PlotManager.dictPlotsActive[strPlot];
		}
		return null;
	}

	public static JsonPlotSave GetOldPlot(string strPlot)
	{
		if (string.IsNullOrEmpty(strPlot))
		{
			return null;
		}
		if (PlotManager.dictPlotsOld.ContainsKey(strPlot))
		{
			return PlotManager.dictPlotsOld[strPlot];
		}
		return null;
	}

	public static bool IsPlotActive(string plotName)
	{
		return !string.IsNullOrEmpty(plotName) && PlotManager.dictPlotsActive.ContainsKey(plotName);
	}

	public static void CancelPlot(string strPlot)
	{
		if (string.IsNullOrEmpty(strPlot) || !PlotManager.dictPlotsActive.ContainsKey(strPlot))
		{
			return;
		}
		JsonPlotSave jsonPlotSave = PlotManager.dictPlotsActive[strPlot];
		if (jsonPlotSave == null)
		{
			return;
		}
		JsonPlot plot = DataHandler.GetPlot(strPlot);
		if (plot != null && !string.IsNullOrEmpty(plot.strCancelBeat))
		{
			JsonPlotBeat plotBeat = DataHandler.GetPlotBeat(plot.strCancelBeat);
			string text = null;
			CondOwner condOwner = null;
			Interaction interaction = null;
			if (plotBeat != null && !string.IsNullOrEmpty(plotBeat.strIATrigger))
			{
				jsonPlotSave.dictCOTokens.TryGetValue("[protag]", out text);
				if (!string.IsNullOrEmpty(text))
				{
					DataHandler.mapCOs.TryGetValue(text, out condOwner);
				}
				if (condOwner != null)
				{
					interaction = PlotManager.GetIAFromString(plotBeat.strIATrigger, condOwner, jsonPlotSave, null, false);
				}
				if (interaction == null)
				{
					if (PlotManager.bDebugLogging)
					{
						Debug.Log("<color=#992299ff>Plot Cancel failed: " + plotBeat.strIATrigger + ".</color> ");
					}
				}
				else
				{
					interaction.strPlot = plot.strName;
					PlotManager.RunPlotBeat(plotBeat, interaction, jsonPlotSave);
				}
			}
		}
		PlotManager.dictPlotsActive.Remove(strPlot);
		PlotManager.dictPlotsOld[strPlot] = jsonPlotSave;
		if (PlotManager.bDebugLogging)
		{
			string text2 = string.Concat(new object[]
			{
				"Plot Cancelled: ",
				strPlot,
				". Phase: ",
				jsonPlotSave.nPhase
			});
			foreach (KeyValuePair<string, string> keyValuePair in jsonPlotSave.dictCOTokens)
			{
				string text3 = text2;
				text2 = string.Concat(new string[]
				{
					text3,
					"; ",
					keyValuePair.Key,
					": ",
					keyValuePair.Value
				});
			}
			Debug.Log("<color=magenta>" + text2 + "</color> ");
		}
		MonoSingleton<GUIQuickBar>.Instance.BuildButtonList(false);
		List<string> list = jsonPlotSave.aCompletedBeats.ToList<string>();
		if (!string.IsNullOrEmpty(jsonPlotSave.strCurrentBeat) && !list.Contains(jsonPlotSave.strCurrentBeat))
		{
			list.Add("<s>" + jsonPlotSave.strCurrentBeat + "</s>");
		}
		jsonPlotSave.aCompletedBeats = list.ToArray();
		jsonPlotSave.strCurrentBeat = DataHandler.GetString("PLOT_CANCELLED", false);
	}

	public static void DebugAudit()
	{
		foreach (JsonPlotSave jsonPlotSave in PlotManager.dictPlotsActive.Values)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> keyValuePair in jsonPlotSave.dictCOTokens)
			{
				if (dictionary.ContainsKey(keyValuePair.Value))
				{
					Debug.LogWarning(string.Concat(new string[]
					{
						"Plot ",
						jsonPlotSave.strPlotName,
						" dupe CO ",
						keyValuePair.Value,
						": ",
						keyValuePair.Key,
						" & ",
						dictionary[keyValuePair.Value]
					}));
				}
				else
				{
					dictionary[keyValuePair.Value] = keyValuePair.Key;
				}
			}
		}
	}

	public static List<string> GetShipsInPlots()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, JsonPlotSave> keyValuePair in PlotManager.dictPlotsActive)
		{
			JsonPlotSave value = keyValuePair.Value;
			if (value != null && value.dictCOTokens != null)
			{
				foreach (string key in value.dictCOTokens.Values)
				{
					CondOwner condOwner;
					if (DataHandler.mapCOs.TryGetValue(key, out condOwner) && !(condOwner == null) && condOwner.ship != null)
					{
						list.Add(condOwner.ship.strRegID);
					}
				}
			}
		}
		return list;
	}

	public static JsonPlotSave[] GetJSONSave()
	{
		if (PlotManager.dictPlotsActive == null)
		{
			return null;
		}
		return PlotManager.dictPlotsActive.Values.ToArray<JsonPlotSave>();
	}

	public static JsonPlotSave[] GetJSONSaveOld()
	{
		if (PlotManager.dictPlotsOld == null)
		{
			return null;
		}
		return PlotManager.dictPlotsOld.Values.ToArray<JsonPlotSave>();
	}

	public static Dictionary<string, JsonPlotSave> dictPlotsActive;

	private static Dictionary<string, JsonPlotSave> dictPlotsOld;

	private const string LOG_START = "<color=magenta>";

	public const string LOG_END = "</color> ";

	public const string LOG_START_DIM = "<color=#992299ff>";

	public static PlotManager.PlotTensionType pRecheckPlotType;

	public static bool bDebugCheckAll;

	public static bool bDebugLogging;

	public static int nPlayerNoticed;

	public enum PlotTensionType
	{
		NONE,
		TENSION,
		RELEASE
	}
}
