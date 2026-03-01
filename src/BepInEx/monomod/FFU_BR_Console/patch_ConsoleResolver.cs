using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoMod;
using Ostranauts.UI.MegaToolTip;
// Adds FFU_BR debug commands on top of the base in-game console resolver.
// The README lists helpers like trigger testing, condition dumps, inventory
// opens, and repair shortcuts that are likely registered here.
public class patch_ConsoleResolver : ConsoleResolver
{
	private static bool KeywordCondTrigTest(ref string strInput)
	{
		string[] array = strInput.Split(new char[]
		{
			' '
		});
		bool flag = array.Length < 3;
		bool result;
		if (flag)
		{
			strInput += "\nMissing command arguments.";
			result = false;
		}
		else
		{
			string text = array[1];
			bool flag2 = !DataHandler.dictCTs.ContainsKey(text);
			if (flag2)
			{
				strInput += "\nCondition trigger not found.";
				result = false;
			}
			else
			{
				bool flag3 = CrewSim.objInstance == null;
				if (flag3)
				{
					strInput += "\nCrewSim instance not found.";
					result = false;
				}
				else
				{
					CondTrigger condTrigger = DataHandler.dictCTs[text].Clone();
					string text2 = array[2];
					bool flag4 = text2 == "[them]";
					if (flag4)
					{
						bool flag5 = GUIMegaToolTip.Selected == null;
						if (flag5)
						{
							strInput += "\nNo target selected or highlighted.";
							return false;
						}
						CondOwner selected = GUIMegaToolTip.Selected;
						strInput = string.Concat(new string[]
						{
							strInput,
							"\nTriggering '",
							text,
							"' against '",
							selected.strName,
							":",
							selected.strID,
							"' object."
						});
						bool flag6 = !condTrigger.Triggered(selected, null, true);
						if (flag6)
						{
							strInput = strInput + "\nOutcome => " + condTrigger.strFailReasonLast;
						}
						else
						{
							strInput += "\nOutcome => Success!";
						}
						condTrigger.Destroy();
					}
					else
					{
						JsonCondOwner jsonCondOwner;
						bool flag7 = patch_DataHandler.TryGetCOValue(text2, out jsonCondOwner);
						if (!flag7)
						{
							strInput += "\nCondition owner template not found.";
							return false;
						}
						CondOwner condOwner = DataHandler.GetCondOwner(text2);
						strInput = string.Concat(new string[]
						{
							strInput,
							"\nTriggering '",
							text,
							"' against '",
							text2,
							"' template."
						});
						bool flag8 = !condTrigger.Triggered(condOwner, null, true);
						if (flag8)
						{
							strInput = strInput + "\nOutcome => " + condTrigger.strFailReasonLast;
						}
						else
						{
							strInput += "\nOutcome => Success!";
						}
						condOwner.Destroy();
						condTrigger.Destroy();
					}
					result = true;
				}
			}
		}
		return result;
	}
	private static bool KeywordFindCondCOs(ref string strInput)
	{
		string[] array = (from x in strInput.Split(new char[]
		{
			' '
		}).Skip(1)
		where !x.StartsWith("!")
		select x).ToArray<string>();
		string[] array2 = (from x in strInput.Split(new char[]
		{
			' '
		}).Skip(1)
		where x.StartsWith("!")
		select x.Substring(1)).ToArray<string>();
		bool flag = array.Length != 0 || array2.Length != 0;
		if (flag)
		{
			int num = 0;
			strInput += "\nCOs with corresponding conditions:";
			bool flag2 = array2.Length == 0;
			if (flag2)
			{
				foreach (JsonCondOwner jsonCondOwner in DataHandler.dictCOs.Values)
				{
					string[] aCondsList = (from x in jsonCondOwner.aStartingConds
					select x.Split(new char[]
					{
						'='
					})[0]).ToArray<string>();
					bool flag3 = array.All((string x) => aCondsList.Contains(x));
					if (flag3)
					{
						strInput = string.Concat(new string[]
						{
							strInput,
							"\n> ",
							jsonCondOwner.strNameFriendly,
							" (",
							jsonCondOwner.strName,
							")"
						});
						num++;
					}
				}
				foreach (JsonCOOverlay jsonCOOverlay in DataHandler.dictCOOverlays.Values)
				{
					JsonCondOwner jsonCondOwner2;
					bool flag4 = DataHandler.dictCOs.TryGetValue(jsonCOOverlay.strCOBase, out jsonCondOwner2);
					if (flag4)
					{
						string[] aCondsList = (from x in jsonCondOwner2.aStartingConds
						select x.Split(new char[]
						{
							'='
						})[0]).ToArray<string>();
						bool flag5 = array.All((string x) => aCondsList.Contains(x));
						if (flag5)
						{
							strInput = string.Concat(new string[]
							{
								strInput,
								"\n> ",
								jsonCOOverlay.strNameFriendly,
								" (",
								jsonCOOverlay.strName,
								")"
							});
							num++;
						}
					}
				}
			}
			else
			{
				bool flag6 = array.Length == 0;
				if (flag6)
				{
					foreach (JsonCondOwner jsonCondOwner3 in DataHandler.dictCOs.Values)
					{
						string[] aCondsList = (from x in jsonCondOwner3.aStartingConds
						select x.Split(new char[]
						{
							'='
						})[0]).ToArray<string>();
						bool flag7 = !array2.Any((string x) => aCondsList.Contains(x));
						if (flag7)
						{
							strInput = string.Concat(new string[]
							{
								strInput,
								"\n> ",
								jsonCondOwner3.strNameFriendly,
								" (",
								jsonCondOwner3.strName,
								")"
							});
							num++;
						}
					}
					foreach (JsonCOOverlay jsonCOOverlay2 in DataHandler.dictCOOverlays.Values)
					{
						JsonCondOwner jsonCondOwner4;
						bool flag8 = DataHandler.dictCOs.TryGetValue(jsonCOOverlay2.strCOBase, out jsonCondOwner4);
						if (flag8)
						{
							string[] aCondsList = (from x in jsonCondOwner4.aStartingConds
							select x.Split(new char[]
							{
								'='
							})[0]).ToArray<string>();
							bool flag9 = !array2.Any((string x) => aCondsList.Contains(x));
							if (flag9)
							{
								strInput = string.Concat(new string[]
								{
									strInput,
									"\n> ",
									jsonCOOverlay2.strNameFriendly,
									" (",
									jsonCOOverlay2.strName,
									")"
								});
								num++;
							}
						}
					}
				}
				else
				{
					foreach (JsonCondOwner jsonCondOwner5 in DataHandler.dictCOs.Values)
					{
						string[] aCondsList = (from x in jsonCondOwner5.aStartingConds
						select x.Split(new char[]
						{
							'='
						})[0]).ToArray<string>();
						bool flag10 = array.All((string x) => aCondsList.Contains(x)) && !array2.Any((string x) => aCondsList.Contains(x));
						if (flag10)
						{
							strInput = string.Concat(new string[]
							{
								strInput,
								"\n> ",
								jsonCondOwner5.strNameFriendly,
								" (",
								jsonCondOwner5.strName,
								")"
							});
							num++;
						}
					}
					foreach (JsonCOOverlay jsonCOOverlay3 in DataHandler.dictCOOverlays.Values)
					{
						JsonCondOwner jsonCondOwner6;
						bool flag11 = DataHandler.dictCOs.TryGetValue(jsonCOOverlay3.strCOBase, out jsonCondOwner6);
						if (flag11)
						{
							string[] aCondsList = (from x in jsonCondOwner6.aStartingConds
							select x.Split(new char[]
							{
								'='
							})[0]).ToArray<string>();
							bool flag12 = array.All((string x) => aCondsList.Contains(x)) && !array2.Any((string x) => aCondsList.Contains(x));
							if (flag12)
							{
								strInput = string.Concat(new string[]
								{
									strInput,
									"\n> ",
									jsonCOOverlay3.strNameFriendly,
									" (",
									jsonCOOverlay3.strName,
									")"
								});
								num++;
							}
						}
					}
				}
			}
			strInput += string.Format("\nFound {0} COs in total.", num);
		}
		return true;
	}
	[MonoModReplace]
	private static bool KeywordGetCond(ref string strInput, string[] strings)
	{
		bool flag = CrewSim.objInstance == null;
		bool result;
		if (flag)
		{
			strInput += "\nCrewSim instance not found.";
			result = false;
		}
		else
		{
			CondOwner condOwner = null;
			string text = string.Empty;
			bool flag2 = strings.Length == 2;
			if (flag2)
			{
				condOwner = CrewSim.GetSelectedCrew();
				bool flag3 = condOwner == null;
				if (flag3)
				{
					strInput += "\nCondOwner not found.";
				}
				text = strings[1];
			}
			else
			{
				bool flag4 = strings.Length == 3;
				if (flag4)
				{
					bool flag5 = strings[1] == "[us]" || strings[1] == "player";
					if (flag5)
					{
						condOwner = CrewSim.GetSelectedCrew();
					}
					else
					{
						bool flag6 = strings[1].Contains("[them]");
						if (flag6)
						{
							bool flag7 = !(GUIMegaToolTip.Selected != null);
							if (flag7)
							{
								strInput += "\nNo target selected for [them].";
								return false;
							}
							condOwner = GUIMegaToolTip.Selected;
							bool flag8 = strings[1].Contains("-");
							if (flag8)
							{
								string text2 = strings[1].Split(new char[]
								{
									'-'
								})[1];
								int num;
								int.TryParse(text2, out num);
								bool flag9 = num > 0;
								if (flag9)
								{
									CondOwner condOwner2 = condOwner;
									while (condOwner2 != null && num > 0)
									{
										condOwner2 = condOwner2.objCOParent;
										num--;
									}
									bool flag10 = condOwner2 == null;
									if (flag10)
									{
										strInput = strInput + "\nNo parent exists for [them] at depth " + text2 + ".";
										return false;
									}
									condOwner = condOwner2;
								}
							}
						}
						else
						{
							string text3 = strings[1].Replace('_', ' ');
							bool flag11 = !DataHandler.mapCOs.TryGetValue(text3, out condOwner);
							if (flag11)
							{
								List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(null, true, true, true);
								foreach (CondOwner condOwner3 in cos)
								{
									bool flag12 = condOwner3.strNameFriendly == strings[1] || condOwner3.strNameFriendly == text3 || condOwner3.strName == strings[1] || condOwner3.strName == text3 || condOwner3.strID == strings[1];
									if (flag12)
									{
										condOwner = condOwner3;
										break;
									}
								}
							}
						}
					}
					text = strings[2];
					bool flag13 = condOwner == null;
					if (flag13)
					{
						strInput += "\nCondOwner not found.";
					}
				}
				else
				{
					bool flag14 = strings.Length < 2;
					if (flag14)
					{
						strInput += "\nNot enough parameters.";
						return false;
					}
					bool flag15 = strings.Length > 3;
					if (flag15)
					{
						strInput += "\nToo many parameters.";
						return false;
					}
				}
			}
			bool flag16 = condOwner != null && text != string.Empty;
			if (flag16)
			{
				bool flag17 = false;
				bool flag18 = text == "*coParents";
				if (flag18)
				{
					strInput = string.Concat(new string[]
					{
						strInput,
						"\nFound condowner ",
						condOwner.strNameFriendly,
						" (",
						condOwner.strName,
						")"
					});
					CondOwner objCOParent = condOwner.objCOParent;
					while (objCOParent != null)
					{
						strInput = string.Concat(new string[]
						{
							strInput,
							"\nIn condowner ",
							objCOParent.strNameFriendly,
							" (",
							objCOParent.strName,
							")"
						});
						objCOParent = objCOParent.objCOParent;
					}
					result = true;
				}
				else
				{
					bool flag19 = text == "*coRules";
					if (flag19)
					{
						bool flag20 = condOwner.mapCondRules.Count > 0;
						if (flag20)
						{
							strInput = string.Concat(new string[]
							{
								strInput,
								"\nFound condrules for ",
								condOwner.strNameFriendly,
								" (",
								condOwner.strName,
								"):"
							});
							foreach (KeyValuePair<string, CondRule> keyValuePair in condOwner.mapCondRules)
							{
								bool flag21 = keyValuePair.Value != null;
								if (flag21)
								{
									CondRule value = keyValuePair.Value;
									strInput = string.Concat(new string[]
									{
										strInput,
										"\n",
										value.strName,
										": ",
										Array.IndexOf<CondRuleThresh>(value.aThresholds, value.GetCurrentThresh(condOwner)).ToString(),
										" (",
										keyValuePair.Key,
										")"
									});
								}
							}
						}
						else
						{
							strInput = string.Concat(new string[]
							{
								strInput,
								"\nThe condowner ",
								condOwner.strNameFriendly,
								" (",
								condOwner.strName,
								") has no attached condrules."
							});
						}
						result = true;
					}
					else
					{
						bool flag22 = text == "*coTickers";
						if (flag22)
						{
							bool flag23 = condOwner.aTickers.Count > 0;
							if (flag23)
							{
								strInput = string.Concat(new string[]
								{
									strInput,
									"\nFound tickers for ",
									condOwner.strNameFriendly,
									" (",
									condOwner.strName,
									"):"
								});
								foreach (JsonTicker jsonTicker in condOwner.aTickers)
								{
									bool flag24 = jsonTicker != null;
									if (flag24)
									{
										strInput = string.Concat(new string[]
										{
											strInput,
											"\n",
											jsonTicker.strName,
											": ",
											patch_ConsoleResolver.SmartString(jsonTicker.fTimeLeft * 3600.0),
											"s (",
											patch_ConsoleResolver.SmartString(jsonTicker.fPeriod * 3600.0),
											"s)"
										});
									}
								}
							}
							else
							{
								strInput = string.Concat(new string[]
								{
									strInput,
									"\nThe condowner ",
									condOwner.strNameFriendly,
									" (",
									condOwner.strName,
									") has no attached tickers."
								});
							}
							result = true;
						}
						else
						{
							bool flag25 = condOwner.IsThreshold(text);
							if (flag25)
							{
								string text4 = text.Substring(6);
								CondRule condRule = condOwner.GetCondRule(text4);
								bool flag26 = condRule != null;
								if (flag26)
								{
									strInput += string.Format("\n{0} ({1}) {2} = x{3}", new object[]
									{
										condOwner.strNameFriendly,
										condOwner.strName,
										text,
										condRule.Modifier
									});
									return true;
								}
							}
							bool flag27 = true;
							foreach (Condition condition in condOwner.mapConds.Values)
							{
								bool flag28 = text == "*" || condition.strName.IndexOf(text) >= 0;
								if (flag28)
								{
									bool flag29 = flag27;
									if (flag29)
									{
										strInput = string.Concat(new string[]
										{
											strInput,
											"\nFound stats for ",
											condOwner.strNameFriendly,
											" (",
											condOwner.strName,
											"):"
										});
										flag27 = false;
									}
									strInput += string.Format("\n{0} = {1}", condition.strName, condition.fCount);
									flag17 = true;
								}
							}
							bool flag30 = !flag17;
							if (flag30)
							{
								strInput = strInput + "\nNo matching cond(s) on " + condOwner.strNameFriendly;
							}
							result = flag17;
						}
					}
				}
			}
			else
			{
				result = false;
			}
		}
		return result;
	}
	public static string SmartString(double number)
	{
		int num = (number % 1.0 == 0.0) ? 0 : 1;
		string text;
		do
		{
			text = number.ToString(string.Format("N{0}", num));
			num++;
		}
		while (number < 0.1 && text.EndsWith("0") && num <= 5);
		return text;
	}
	public static extern bool orig_ResolveString(ref string strInput);
	public static bool ResolveString(ref string strInput)
	{
		strInput = strInput.Trim();
		string[] array = strInput.Split(new char[]
		{
			' '
		});
		array[0] = array[0].ToLower();
		string text = array[0];
		string a = text;
		bool result;
		if (!(a == "findcondcos"))
		{
			if (!(a == "repairship"))
			{
				if (!(a == "openinventory"))
				{
					if (!(a == "triggerinfo"))
					{
						if (!(a == "triggertest"))
						{
							result = patch_ConsoleResolver.orig_ResolveString(ref strInput);
						}
						else
						{
							result = patch_ConsoleResolver.KeywordCondTrigTest(ref strInput);
						}
					}
					else
					{
						result = patch_ConsoleResolver.KeywordCondTrigInfo(ref strInput);
					}
				}
				else
				{
					result = patch_ConsoleResolver.KeywordOpenInventory(ref strInput);
				}
			}
			else
			{
				result = patch_ConsoleResolver.KeywordRepairShip(ref strInput);
			}
		}
		else
		{
			result = patch_ConsoleResolver.KeywordFindCondCOs(ref strInput);
		}
		return result;
	}
	private static extern bool orig_KeywordHelp(ref string strInput, string[] strings);
	private static bool KeywordHelp(ref string strInput, string[] strings)
	{
		bool flag = strings.Length == 1;
		bool result;
		if (flag)
		{
			strInput += "\nWelcome to the Ostranauts console.";
			strInput += "\nAvailable Commands:";
			strInput += "\nhelp";
			strInput += "\necho";
			strInput += "\ncrewsim";
			strInput += "\naddcond";
			strInput += "\ngetcond (FFU modified)";
			strInput += "\nspawn";
			strInput += "\nunlockdebug";
			strInput += "\nbugform";
			strInput += "\nclear";
			strInput += "\nverify";
			strInput += "\nkill";
			strInput += "\naddcrew";
			strInput += "\naddnpc";
			strInput += "\ndamageship";
			strInput += "\nbreakinship";
			strInput += "\noxygen";
			strInput += "\nmeteor";
			strInput += "\nlookup";
			strInput += "\ntoggle";
			strInput += "\nship";
			strInput += "\nrel";
			strInput += "\nsummon";
			strInput += "\nskywalk";
			strInput += "\nplot";
			strInput += "\nmeatstate";
			strInput += "\nrename";
			strInput += "\nfindcondcos (FFU only)";
			strInput += "\nrepairship (FFU only)";
			strInput += "\nopeninventory (FFU only)";
			strInput += "\ntriggerinfo (FFU only)";
			strInput += "\ntriggertest (FFU only)";
			strInput += "\n\ntype command name after help to see more details about command";
			strInput += "\n";
			result = true;
		}
		else
		{
			bool flag2 = strings.Length == 2;
			if (flag2)
			{
				string text = strings[1];
				string a = text;
				if (!(a == "getcond"))
				{
					if (!(a == "findcondcos"))
					{
						if (!(a == "repairship"))
						{
							if (!(a == "openinventory"))
							{
								if (!(a == "triggerinfo"))
								{
									if (!(a == "triggertest"))
									{
										result = patch_ConsoleResolver.orig_KeywordHelp(ref strInput, strings);
									}
									else
									{
										strInput += "\ntriggertest fires condition trigger against template or selected object and logs outcome";
										strInput += "\n<i>works only if there is initialized CrewSim instance (i.e. only when game was loaded)</i>";
										strInput += "\ne.g. triggertest TIsYourConditionTriggerName [them] (only in-game and with selected object)";
										strInput += "\ne.g. triggertest TIsYourConditionTriggerName ItmYourCondownerName";
										result = true;
									}
								}
								else
								{
									strInput += "\ntriggerinfo shows rules of condition trigger either as JSON (0) or as friendly (1) text";
									strInput += "\ne.g. triggerinfo TIsYourConditionTriggerName 0";
									strInput += "\nor triggerinfo TIsYourConditionTriggerName 1";
									result = true;
								}
							}
							else
							{
								strInput += "\nopeninventory opens inventory window from the perspective of the selected condowner";
								strInput += "\ne.g. select target with right mouse button and enter 'openinventory' command";
								result = true;
							}
						}
						else
						{
							strInput += "\nrepairship zeroes all StatDamage values on all IsInstalled condowners of all loaded ships";
							result = true;
						}
					}
					else
					{
						strInput += "\nfindcondcos lists all condowner templates that meet listed conditions";
						strInput += "\nworks from main menu and can support any amount of conditions";
						strInput += "\n'IsCondition' will only list templates that have that condition";
						strInput += "\n'!IsCondition' will only list templates that don't have that condition";
						strInput += "\n'using both will only list templates that meet both conditions";
						strInput += "\ne.g. 'findcondcos IsScrap !IsFlexible'";
						result = true;
					}
				}
				else
				{
					strInput += "\ngetcond lists a condition's value on a condowner";
					strInput += "\ne.g. 'getcond Joshu IsHuman'";
					strInput += "\nwill find condowner with name/friendlyName/ID of 'Joshu'";
					strInput += "\nwill check for all condtions including partial string 'IsHuman'";
					strInput += "\nif condowner is valid and conditions are found, it will list their current names and values on Joshu";
					strInput += "\n<i>spaces within names must be replaced with underscores: '_'</i>";
					strInput += "\n'getcond *' will list all stats that condowner has";
					strInput += "\n'getcond-NUM' will execute command for NUM parent of the condowner";
					strInput += "\n'getcond *coParents' will list all parents of the condowner";
					strInput += "\n'getcond *coRules' will list all rules attached to the condowner";
					strInput += "\n'getcond *coTickers' will list all tickers attached to the condowner";
					strInput += "\n";
					result = true;
				}
			}
			else
			{
				result = false;
			}
		}
		return result;
	}
	private static string GetRulesInfoDev(CondTrigger refTrigger)
	{
		refTrigger.strFailReason = null;
		Type type = refTrigger.GetType();
		PropertyInfo property = type.GetProperty("RulesInfoDev");
		return (((property != null) ? property.GetValue(refTrigger, null) : null) as string) ?? refTrigger.RulesInfo;
	}
	private static string GetRulesInfoTxt(CondTrigger refTrigger)
	{
		refTrigger.strFailReason = null;
		Type type = refTrigger.GetType();
		return refTrigger.RulesInfo;
	}
	private static bool KeywordCondTrigInfo(ref string strInput)
	{
		string[] array = strInput.Split(new char[]
		{
			' '
		});
		bool flag = array.Length < 3;
		bool result;
		if (flag)
		{
			strInput += "\nMissing command arguments.";
			result = false;
		}
		else
		{
			string text = array[1];
			bool flag2 = !DataHandler.dictCTs.ContainsKey(text);
			if (flag2)
			{
				strInput += "\nCondition trigger not found.";
				result = false;
			}
			else
			{
				int num;
				int.TryParse(array[2], out num);
				CondTrigger refTrigger = DataHandler.dictCTs[text].Clone();
				int num2 = num;
				int num3 = num2;
				if (num3 != 0)
				{
					if (num3 != 1)
					{
						strInput += "\nInvalid rule info rendering option.";
						return false;
					}
					strInput = string.Concat(new string[]
					{
						strInput,
						"\nCondition Trigger '",
						text,
						"' Rules: ",
						patch_ConsoleResolver.GetRulesInfoTxt(refTrigger)
					});
				}
				else
				{
					strInput = string.Concat(new string[]
					{
						strInput,
						"\nCondition Trigger '",
						text,
						"' Rules: ",
						patch_ConsoleResolver.GetRulesInfoDev(refTrigger)
					});
				}
				result = true;
			}
		}
		return result;
	}
	private static bool KeywordOpenInventory(ref string strInput)
	{
		bool flag = CrewSim.objInstance == null;
		bool result;
		if (flag)
		{
			strInput += "\nCrewSim instance not found.";
			result = false;
		}
		else
		{
			bool flag2 = GUIMegaToolTip.Selected == null;
			if (flag2)
			{
				strInput += "\nNo target selected or highlighted.";
				result = false;
			}
			else
			{
				CondOwner selected = GUIMegaToolTip.Selected;
				bool flag3 = Container.GetSpace(selected) < 1 && !selected.compSlots.aSlots.Any<Slot>();
				if (flag3)
				{
					strInput += "\nTarget is not valid or has no inventory.";
					result = false;
				}
				else
				{
					strInput = string.Concat(new string[]
					{
						strInput,
						"\nAccessed inventory: ",
						selected.FriendlyName,
						" (",
						selected.strName,
						") ",
						selected.strID
					});
					bool flag4 = selected.HasCond("IsHuman") || selected.HasCond("IsRobot");
					if (flag4)
					{
						CommandInventory.ToggleInventory(selected, false);
					}
					else
					{
						CrewSim.inventoryGUI.SpawnInventoryWindow(selected, 1, null, null);
					}
					patch_ConsoleResolver.bInvokedInventory = true;
					result = true;
				}
			}
		}
		return result;
	}
	private static bool KeywordRepairShip(ref string strInput)
	{
		bool flag = CrewSim.objInstance == null;
		bool result;
		if (flag)
		{
			strInput += "\nCrewSim instance not found.";
			result = false;
		}
		else
		{
			bool flag2 = CrewSim.shipCurrentLoaded == null;
			if (flag2)
			{
				strInput += "\nNo ship currently loaded.";
				result = false;
			}
			else
			{
				CondTrigger condTrigger = new CondTrigger("TIsInstalledObject", new string[]
				{
					"IsInstalled"
				}, null, null, null);
				List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(condTrigger, true, false, true);
				foreach (CondOwner condOwner in cos)
				{
					condOwner.SetUpBehaviours();
					condOwner.ZeroCondAmount("StatDamage");
					bool flag3 = condOwner.Item != null;
					if (flag3)
					{
						condOwner.Item.VisualizeOverlays(false);
					}
					condOwner.UpdateStats();
				}
				strInput += string.Format("\nRepaired {0} installed COs on the ship.", cos.Count);
				result = true;
			}
		}
		return result;
	}
	public static bool bInvokedInventory;
}
