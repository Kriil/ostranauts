using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Ostranauts.Core;
using Ostranauts.Core.Tutorials;
using Ostranauts.Objectives;
using Ostranauts.UI.MegaToolTip;
using UnityEngine;

public class ConsoleResolver
{
	public static TextInfo TextInfo
	{
		get
		{
			if (ConsoleResolver._textInfo == null)
			{
				ConsoleResolver._textInfo = new CultureInfo("en-US", false).TextInfo;
			}
			return ConsoleResolver._textInfo;
		}
	}

	public static bool ResolveString(ref string strInput)
	{
		strInput.Trim();
		string[] array = strInput.Split(new char[]
		{
			' '
		});
		array[0] = array[0].ToLower();
		string text = array[0];
		switch (text)
		{
		case "help":
			return ConsoleResolver.KeywordHelp(ref strInput, array);
		case "echo":
			strInput = strInput + "\n" + strInput.Remove(0, 5);
			return true;
		case "unlockdebug":
			return ConsoleResolver.KeywordUnlockDebug(ref strInput, array);
		case "crewsim":
			return ConsoleResolver.KeywordCrewSim(ref strInput, array);
		case "addcond":
			return ConsoleResolver.KeywordAddCond(ref strInput, array);
		case "getcond":
			return ConsoleResolver.KeywordGetCond(ref strInput, array);
		case "bugform":
			return ConsoleResolver.KeywordBugForm(ref strInput);
		case "spawn":
			return ConsoleResolver.KeywordSpawn(ref strInput, array);
		case "verify":
			return ConsoleResolver.KeywordVerify(ref strInput);
		case "kill":
			return ConsoleResolver.KeywordKill(ref strInput, array);
		case "addcrew":
			return ConsoleResolver.KeywordAddCrew(ref strInput, array, true);
		case "addnpc":
			return ConsoleResolver.KeywordAddCrew(ref strInput, array, false);
		case "damageship":
			return ConsoleResolver.KeywordDamageShip(ref strInput, array);
		case "breakinship":
			return ConsoleResolver.KeywordBreakInShip(ref strInput, array);
		case "meteor":
			return ConsoleResolver.KeywordMeteor(ref strInput, array);
		case "oxygen":
			return ConsoleResolver.KeywordOxygen(ref strInput, array);
		case "toggle":
			return ConsoleResolver.KeywordToggle(ref strInput, array);
		case "ship":
			return ConsoleResolver.KeywordShip(ref strInput, array);
		case "shipvis":
			return ConsoleResolver.KeywordShipVis(ref strInput, array);
		case "lookup":
			return ConsoleResolver.KeywordLookup(ref strInput, array);
		case "plot":
			return ConsoleResolver.KeywordPlot(ref strInput, array);
		case "summon":
			return ConsoleResolver.KeywordSummon(ref strInput, array);
		case "rel":
			return ConsoleResolver.KeywordRelationship(ref strInput, array);
		case "skywalk":
			return ConsoleResolver.KeywordSkywalk(ref strInput, array);
		case "detach":
			return ConsoleResolver.KeywordDetach(ref strInput, array);
		case "attach":
			return ConsoleResolver.KeywordAttach(ref strInput, array);
		case "meatstate":
			return ConsoleResolver.KeywordMeatState(ref strInput, array);
		case "priceflips":
			return ConsoleResolver.KeywordPriceFlips(ref strInput, array);
		case "tutorial":
			return ConsoleResolver.KeywordAddTutorial(ref strInput, array);
		case "stopTutorial":
			return ConsoleResolver.KeywordStopTutorial(ref strInput, array);
		case "stop":
			return ConsoleResolver.KeywordStopTutorial(ref strInput, array);
		case "complete":
			return ConsoleResolver.KeywordCompleteTutorial(ref strInput, array);
		case "pda":
			return ConsoleResolver.KeywordPDA(ref strInput, array);
		case "rename":
			return ConsoleResolver.KeywordRename(ref strInput, array);
		case "clear":
		case "clr":
			return ConsoleResolver.KeywordClear(ref strInput, array);
		case "wipe":
			return ConsoleResolver.KeywordWipe(ref strInput, array);
		}
		strInput += "\nFailed to recognise command.";
		return false;
	}

	private static bool KeywordCrewSim(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		strInput = strInput + "\nCrewSim instance has been running for " + CrewSim.fTotalGameSecSession.ToString("N2") + " seconds.";
		return true;
	}

	private static bool KeywordCompleteTutorial(ref string strInput, string[] strings)
	{
		if (strings.Length > 1)
		{
			foreach (TutorialBeat tutorialBeat in CrewSimTut.TutorialBeats)
			{
				if (tutorialBeat.ToString() == "Ostranauts.Core.Tutorials." + strings[1])
				{
					tutorialBeat.Finished = true;
					return true;
				}
			}
			return false;
		}
		return false;
	}

	private static bool KeywordAddTutorial(ref string strInput, string[] strings)
	{
		if (strings.Length > 1)
		{
			if (string.IsNullOrEmpty(strings[1]))
			{
				strInput += "No tutorial name.";
				return false;
			}
			Type type = Type.GetType("Ostranauts.Core.Tutorials." + strings[1]);
			if (type != null && type.IsSubclassOf(typeof(TutorialBeat)))
			{
				TutorialBeat tutorialBeat = Activator.CreateInstance(type) as TutorialBeat;
				CrewSimTut.TutorialBeats.Add(tutorialBeat);
				strInput = strInput + "\nAdded " + tutorialBeat.ObjectiveName + " to current objectives.";
			}
			else
			{
				strInput += "\nInvalid tutorial name.";
			}
		}
		return false;
	}

	private static bool KeywordStopTutorial(ref string strInput, string[] strings)
	{
		if (strings.Length > 1)
		{
			if (string.IsNullOrEmpty(strings[1]))
			{
				strInput += "No tutorial name.";
				return false;
			}
			Type type = Type.GetType("Ostranauts.Core.Tutorials." + strings[1]);
			if (type != null && type.IsSubclassOf(typeof(TutorialBeat)))
			{
				bool flag = false;
				for (int i = 0; i < CrewSimTut.TutorialBeats.Count; i++)
				{
					if (type.Name == CrewSimTut.TutorialBeats[i].GetType().Name)
					{
						flag = true;
						TutorialBeat tutorialBeat = CrewSimTut.TutorialBeats[i];
						CrewSimTut.TutorialBeats.RemoveAt(i--);
						if (tutorialBeat.AssociatedObjective != null)
						{
							ObjectiveTracker.OnObjectiveClosed.Invoke(tutorialBeat.AssociatedObjective);
						}
						strInput = strInput + "\nRemoved " + tutorialBeat.ObjectiveName + " from current objectives without completing it.";
						break;
					}
				}
				if (!flag)
				{
					strInput = strInput + "\nNo objective matching name of " + strings[1] + ". Try 'lookup tutorials' to see active tutorial objectives.";
				}
			}
			else
			{
				strInput += "\nInvalid tutorial name.";
			}
		}
		return false;
	}

	private static bool KeywordAddCond(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		CondOwner condOwner = null;
		string text = string.Empty;
		double num = 0.0;
		if (strings.Length == 3)
		{
			condOwner = CrewSim.GetSelectedCrew();
			if (condOwner == null)
			{
				strInput += "\nCondOwner not found.";
			}
			if (DataHandler.GetCond(strings[1]) != null)
			{
				text = strings[1];
			}
			else if (condOwner.HasCond(strings[1]))
			{
				text = strings[1];
			}
			else
			{
				strInput += "\nCondition not found.";
			}
			if (!double.TryParse(strings[2], out num))
			{
				strInput += "\nCold not parse amount.";
			}
		}
		else if (strings.Length == 4)
		{
			if (strings[1] == "[us]" || strings[1] == "player")
			{
				condOwner = CrewSim.GetSelectedCrew();
			}
			else if (strings[1] == "[them]")
			{
				if (!(GUIMegaToolTip.Selected != null))
				{
					strInput += "\nNo target selected for [them].";
					return false;
				}
				condOwner = GUIMegaToolTip.Selected;
			}
			else
			{
				string text2 = strings[1].Replace('_', ' ');
				if (!DataHandler.mapCOs.TryGetValue(text2, out condOwner))
				{
					List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(null, true, true, true);
					foreach (CondOwner condOwner2 in cos)
					{
						if (condOwner2.strNameFriendly == strings[1] || condOwner2.strNameFriendly == text2 || condOwner2.strName == strings[1] || condOwner2.strName == text2 || condOwner2.strID == strings[1])
						{
							condOwner = condOwner2;
							break;
						}
					}
				}
			}
			if (condOwner == null)
			{
				strInput += "\nCondOwner not found.";
			}
			if (DataHandler.GetCond(strings[2]) != null)
			{
				text = strings[2];
			}
			else if (condOwner.HasCond(strings[2]))
			{
				text = strings[2];
			}
			else
			{
				strInput += "\nCondition not found.";
			}
			if (!double.TryParse(strings[3], out num))
			{
				strInput += "\nCould not parse amount.";
			}
		}
		else
		{
			if (strings.Length < 3)
			{
				strInput += "\nNot enough parameters.";
				return false;
			}
			if (strings.Length > 4)
			{
				strInput += "\nToo many parameters.";
				return false;
			}
		}
		if (condOwner != null && text != string.Empty)
		{
			condOwner.AddCondAmount(text, num, 0.0, 0f);
			string text3 = strInput;
			strInput = string.Concat(new object[]
			{
				text3,
				"\nAdded ",
				num,
				" ",
				text,
				" to ",
				condOwner.strNameFriendly
			});
			return true;
		}
		return false;
	}

	private static bool KeywordGetCond(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		CondOwner condOwner = null;
		string text = string.Empty;
		if (strings.Length == 2)
		{
			condOwner = CrewSim.GetSelectedCrew();
			if (condOwner == null)
			{
				strInput += "\nCondOwner not found.";
			}
			text = strings[1];
		}
		else if (strings.Length == 3)
		{
			if (strings[1] == "[us]" || strings[1] == "player")
			{
				condOwner = CrewSim.GetSelectedCrew();
			}
			else if (strings[1] == "[them]")
			{
				if (!(GUIMegaToolTip.Selected != null))
				{
					strInput += "\nNo target selected for [them].";
					return false;
				}
				condOwner = GUIMegaToolTip.Selected;
			}
			else
			{
				string text2 = strings[1].Replace('_', ' ');
				if (!DataHandler.mapCOs.TryGetValue(text2, out condOwner))
				{
					List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(null, true, true, true);
					foreach (CondOwner condOwner2 in cos)
					{
						if (condOwner2.strNameFriendly == strings[1] || condOwner2.strNameFriendly == text2 || condOwner2.strName == strings[1] || condOwner2.strName == text2 || condOwner2.strID == strings[1])
						{
							condOwner = condOwner2;
							break;
						}
					}
				}
			}
			text = strings[2];
			if (condOwner == null)
			{
				strInput += "\nCondOwner not found.";
			}
		}
		else
		{
			if (strings.Length < 2)
			{
				strInput += "\nNot enough parameters.";
				return false;
			}
			if (strings.Length > 3)
			{
				strInput += "\nToo many parameters.";
				return false;
			}
		}
		if (condOwner != null && text != string.Empty)
		{
			bool flag = false;
			if (condOwner.IsThreshold(text))
			{
				string strCond = text.Substring(6);
				CondRule condRule = condOwner.GetCondRule(strCond);
				if (condRule != null)
				{
					flag = true;
					string text3 = strInput;
					strInput = string.Concat(new object[]
					{
						text3,
						"\n",
						condOwner.strNameFriendly,
						".",
						text,
						" = ",
						condRule.Modifier
					});
				}
			}
			else
			{
				foreach (Condition condition in condOwner.mapConds.Values)
				{
					if (condition.strName.IndexOf(text) >= 0)
					{
						string text3 = strInput;
						strInput = string.Concat(new object[]
						{
							text3,
							"\n",
							condOwner.strNameFriendly,
							".",
							condition.strName,
							" = ",
							condition.fCount,
							" (",
							MathUtils.GetTimeUnits(condition.GetRemain() * 3600f, "NaN"),
							")"
						});
						flag = true;
					}
				}
			}
			if (!flag)
			{
				strInput = strInput + "\nNo matching cond(s) on " + condOwner.strNameFriendly;
			}
			return flag;
		}
		return false;
	}

	private static bool KeywordHelp(ref string strInput, string[] strings)
	{
		if (strings.Length == 1)
		{
			strInput += "\nWelcome to the Ostranauts console.";
			strInput += "\nAvailable Commands:";
			strInput += "\naddcrew";
			strInput += "\naddcond";
			strInput += "\naddnpc";
			strInput += "\nbreakinship";
			strInput += "\nbugform";
			strInput += "\nclear";
			strInput += "\ncrewsim";
			strInput += "\ndamageship";
			strInput += "\necho";
			strInput += "\ngetcond";
			strInput += "\nhelp";
			strInput += "\nkill";
			strInput += "\nlookup";
			strInput += "\nmeatstate";
			strInput += "\nmeteor";
			strInput += "\noxygen";
			strInput += "\nplot";
			strInput += "\nrel";
			strInput += "\nrename";
			strInput += "\nship";
			strInput += "\nskywalk";
			strInput += "\nspawn";
			strInput += "\nsummon";
			strInput += "\ntoggle";
			strInput += "\nunlockdebug";
			strInput += "\nverify";
			strInput += "\nwipe";
			strInput += "\n\ntype command name after help to see more details about command";
			strInput += "\n";
			return true;
		}
		if (strings.Length == 2)
		{
			string text = strings[1];
			switch (text)
			{
			case "help":
				strInput += "\nhelp explains which commands are available and what they do";
				strInput += "\n<i>you seem to have figured this one out already</i>";
				strInput += "\n";
				return true;
			case "echo":
				strInput += "\necho repeats the text given.";
				strInput += "\nthis serves as a test of the console command resolver itself";
				strInput += "\ne.g. 'echo hello world' returns 'hello world'";
				strInput += "\n";
				return true;
			case "crewsim":
				strInput += "\ncrewsim shows how long crewsim has been running for";
				strInput += "\n<i>will not find an instance if in main menu</i>";
				strInput += "\n";
				return true;
			case "addcond":
				strInput += "\naddcond adds a condition to a condowner";
				strInput += "\ne.g. 'addcond Joshu IsHuman 1.0'";
				strInput += "\nwill find condowner with name/friendlyName/ID of 'Joshu'";
				strInput += "\nwill check the validity of the condtion 'IsHuman'";
				strInput += "\nif both are valid it will add 1.0 IsHuman to Joshu";
				strInput += "\n<i>spaces within names must be replaced with underscores: '_'</i>";
				strInput += "\n";
				return true;
			case "getcond":
				strInput += "\ngetcond lists a condition's value on a condowner";
				strInput += "\ne.g. 'getcond Joshu IsHuman'";
				strInput += "\nwill find condowner with name/friendlyName/ID of 'Joshu'";
				strInput += "\nwill check for all condtions including partial string 'IsHuman'";
				strInput += "\nif condowner is valid and conditions are found, it will list their current names and values on Joshu";
				strInput += "\n<i>spaces within names must be replaced with underscores: '_'</i>";
				strInput += "\n";
				return true;
			case "unlockdebug":
				strInput += "\nunlockdebug unlocks special debug hotkeys";
				strInput += "\nalso allows for the debug overlay to be enabled";
				strInput = strInput + "\nDebug overlay hotkey is: " + GUIActionKeySelector.commandDebug.KeyName;
				return true;
			case "bugform":
				strInput += "\ndisplays a link to the form to submit bugs";
				return true;
			case "clear":
				strInput += "\nclears the log of past lines";
				strInput += "\nleaving blank will clear all previous lines";
				strInput += "\noptional param number will clear that many previous lines from the bottom of the log";
				strInput += "\ne.g. 'clear 5'";
				return true;
			case "spawn":
				strInput += "\nspawns a given loot into the game";
				strInput += "\ntries to put it into the player's inventory or onto the ground next to the player";
				strInput += "\ne.g. 'spawn ItmAICargo01'";
				return true;
			case "verify":
				strInput += "\nverifies game json files";
				return true;
			case "kill":
				strInput += "\nkills given CO";
				strInput += "\nadds Death condition to CO with matching name (Replace spaces in human names with _)";
				strInput += "\ne.g. 'kill Joshu_Lastname'";
				return true;
			case "addcrew":
				strInput += "\nadds randomly selected crew to current ship";
				strInput += "\ncan spawn multiple random crew at once with optional number at the end";
				strInput += "\ne.g. 'addcrew 3'";
				return true;
			case "addnpc":
				strInput += "\nadds a randomly selected npc to current ship";
				strInput += "\ncan spawn multiple random npcs at once with optional number at the end";
				strInput += "\ne.g. 'addnpc 3'";
				return true;
			case "meteor":
				strInput += "\nhits the ship with meteor";
				strInput += "\ncan spawn multiple meteors at once with optional number at the end";
				strInput += "\ne.g. 'meteor 4'";
				return true;
			case "damageship":
				strInput += "\nadds random damage to all tiles on current ship";
				strInput += "\nnumber specifies max amount of random damage to apply";
				strInput += "\ne.g. 'damageship 0.3' = random upto 30% damage to all items";
				return true;
			case "breakinship":
				strInput += "\nperforms standard derelict break-in pass on current ship";
				strInput += "\nnumber specifies max amount of random damage to apply";
				strInput += "\ne.g. 'damageship 0.3' = up to 30% damage to all items, 30% of valuables removed, etc.";
				return true;
			case "oxygen":
				strInput += "\nadds oxygen to all people on current ship";
				strInput += "\nnumber specifies amount of oxygen to add to each person";
				strInput += "\ne.g. 'oxygen 1.5'";
				return true;
			case "lookup":
				strInput += "\nallows the user to look up ships and plots";
				strInput += "\nuse the followup keyword 'ships' or 'plots' to get data";
				strInput += "\ne.g. 'lookup ships'";
				return true;
			case "toggle":
				strInput += "\nallows the user to toggle certain game settings";
				strInput += "\ne.g. 'toggle aoshow' turns ambient occlusion on or off";
				strInput += "\ne.g. 'toggle aozoom' swaps ambient occlusion between screen space and world space";
				strInput += "\ne.g. 'toggle aospread 11.25' allows numeric tuning of ambient occlusion spread/width";
				strInput += "\ne.g. 'toggle aointensity 0.66' allows numeric tuning of how dark ambient occlusion appears";
				return true;
			case "ship":
				strInput += "\nallows the user to spawn in and teleport to a specific ship";
				strInput += "\ne.g. 'ship Volatile Aero' will spawn in the Volatile Aero ship";
				return true;
			case "rel":
				strInput += "\nallows the user to set social relationships between humans";
				strInput += "\ne.g. 'rel condition elvis_presley' adds a condition to the player/selected NPC's relationship with Elvis Presley";
				strInput += "\ne.g. 'rel condition elvis_presley joanna_dark' adds a condition to the relationship between Elvis Presley and Joanna Dark";
				return true;
			case "plot":
				strInput += "\nallows the user to fast forward through a specific named plot, spawning appropriate quest givers and passing quest flags";
				strInput += "\ne.g. 'plot Messenger' starts the Messenger quest chain, spawning a Messenger questgiver on the current ship";
				return true;
			case "skywalk":
				strInput += "\nallows the user to teleport to a named ship ID";
				strInput += "\ne.g. 'skywalk OKLG' teleports the player to K-LEG.";
				return true;
			case "summon":
				strInput += "\nallows the user to summon an existing NPC from another ship to the current ship";
				strInput += "\ne.g. 'summon elvis_presley' teleports Elvis Presley to the current ship.";
				return true;
			case "meatstate":
				strInput += "\nallows player to check and change the current state of meat in their game";
				strInput += "\nuse alone to check status or put numeric value after keyword to change value";
				strInput += "\n0 = Inert (Meat does nothing).";
				strInput += "\n1 = Dormant (Meat does nothing until fought).";
				strInput += "\n2 = Spread (Meat spreads and fights).";
				strInput += "\n3 = Decay (Meat spreads fights and slowly dies).";
				strInput += "\n4 = Eradicate (Meat quickly dies).";
				strInput += "\n5 = Hell (Meat gets 2 actions instead of 1).";
				return true;
			case "rename":
				strInput += "\nallows player to rename selected items";
				strInput += "\ne.g. 'rename Naiba' will change the item's friendly and short names to 'Naiba'";
				return true;
			case "wipe":
				strInput += "\nallows player to clear electrical signal connections on selected items";
				strInput += "\nhas 3 optional levels of hardness: soft, med, hard";
				strInput += "\nsoft: just audits connections to see if there were any mistakes";
				strInput += "\nmed: audits connections then forcibly closes any external connections";
				strInput += "\nhard: audits connections, forcibly closes any external connections, then resets associated conds on the target";
				strInput += "\n\nregular players can just use the keyword 'wipe' and it will automatically use the hardest setting";
				strInput += "\ne.g. 'wipe soft' will do the softest type of wipe";
				return true;
			}
			strInput += "\ncannot give help, command name not recognised";
			strInput += "\n";
			return true;
		}
		return false;
	}

	private static bool KeywordUnlockDebug(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance != null)
		{
			strInput += "\nDebug mode unlocked!";
			CrewSim.objInstance.UnlockDebug();
			return true;
		}
		strInput += "\nDebug mode can't unlock unless in CrewSim!";
		return false;
	}

	private static bool KeywordAttach(ref string strInput, string[] strings)
	{
		ConsoleToGUI.Attach();
		strInput += "\nRE-ATTACHED LOGGING!";
		strInput += "\nWarning experimental!";
		return true;
	}

	private static bool KeywordDetach(ref string strInput, string[] strings)
	{
		strInput += "\nDETACHED LOGGING!";
		strInput += "\nWarning experimental!";
		strInput += "\nWithout logs you are condemning this save file to be unfixable should errors occur.";
		ConsoleToGUI.Detach();
		return true;
	}

	private static bool KeywordBugForm(ref string strInput)
	{
		string text = "https://forms.gle/" + DataHandler.GetString("BUG_FORM_LINK_SCRAMBLE", false);
		strInput += "\nopening bug form link!";
		strInput = strInput + "\n" + text;
		Application.OpenURL(text);
		return true;
	}

	private static bool KeywordSpawn(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		CondOwner condOwner = null;
		Loot loot = null;
		bool flag = false;
		if (strings.Length == 2)
		{
			condOwner = CrewSim.GetSelectedCrew();
			loot = DataHandler.GetLoot(strings[1]);
		}
		if (condOwner == null)
		{
			strInput += "\nUnable to find current player";
			return false;
		}
		List<CondOwner> coloot = loot.GetCOLoot(null, false, null);
		if (coloot.Count <= 0)
		{
			strInput += "\nUnable to get valid loot.";
			if (strings.Length == 2)
			{
				strInput += " Trying spawn CO directly.";
				CondOwner condOwner2 = DataHandler.GetCondOwner(strings[1]);
				if (condOwner2 == null)
				{
					strInput += "\nCO not found. Aborting.";
					return false;
				}
				coloot.Add(condOwner2);
			}
		}
		if (coloot.Count <= 0)
		{
			strInput += "\nValid loot found, but it contains no CondOwners!";
			return false;
		}
		foreach (CondOwner condOwner3 in coloot)
		{
			CondOwner condOwner4 = condOwner.AddCO(condOwner3, true, true, true);
			if (condOwner4 != null)
			{
				condOwner4 = condOwner.DropCO(condOwner4, false, null, 0f, 0f, true, null);
				if (condOwner4 != null)
				{
					if (!flag)
					{
						strInput += "\nLoot found but unable to spawn in fully, some leftovers!";
						flag = true;
					}
					strInput = strInput + "\nCouldn't spawn: " + condOwner4.strNameFriendly;
					condOwner4.Destroy();
				}
				else
				{
					strInput = strInput + "\nDropped: " + condOwner3.strNameFriendly;
				}
			}
			else
			{
				strInput = strInput + "\nSpawned: " + condOwner3.strNameFriendly;
			}
		}
		return !flag;
	}

	private static bool KeywordClear(ref string strInput, string[] strings)
	{
		if (strings.Length <= 1)
		{
			ConsoleToGUI.StaticClear(0);
		}
		else
		{
			int nLines;
			if (!int.TryParse(strings[1], out nLines))
			{
				strInput += "\nCould not parse amount to clear.";
				return false;
			}
			ConsoleToGUI.StaticClear(nLines);
		}
		return true;
	}

	private static bool KeywordVerify(ref string strInput)
	{
		strInput += "\nVerfying JSON Files...";
		DataHandler.ScanDictionaries();
		return true;
	}

	private static bool KeywordAddCrew(ref string strInput, string[] strings, bool makeCrew = false)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		int num = 1;
		if (strings.Length >= 2)
		{
			int.TryParse(strings[1], out num);
			if (num < 1)
			{
				strInput += "\nNon positive amount of crew requested.";
				return false;
			}
		}
		strInput = strInput + "\nSpawning " + num.ToString() + " crew.";
		for (int i = 0; i < num; i++)
		{
			CondOwner randomCrew = CrewSim.objInstance.GetRandomCrew(null);
			if (makeCrew)
			{
				CrewSim.objInstance.AddNpcToRoster(randomCrew);
			}
		}
		return true;
	}

	private static bool KeywordKill(ref string strInput, string[] strings)
	{
		CondOwner condOwner = null;
		if (strings.Length < 2)
		{
			strInput += "\nunable to find target to kill!";
			return false;
		}
		if (strings[1] == "[us]" || strings[1] == "player")
		{
			condOwner = CrewSim.GetSelectedCrew();
		}
		else
		{
			string b = strings[1].Replace('_', ' ');
			List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(null, true, true, true);
			foreach (CondOwner condOwner2 in cos)
			{
				if (condOwner2.strNameFriendly == strings[1] || condOwner2.strNameFriendly == b || condOwner2.strName == strings[1] || condOwner2.strName == b || condOwner2.strID == strings[1])
				{
					condOwner = condOwner2;
					break;
				}
			}
		}
		if (condOwner != null)
		{
			condOwner.AddCondAmount("Death", 1.0, 0.0, 0f);
			strInput = strInput + "\nGave " + condOwner.strNameFriendly + " 1.0 Death";
			return true;
		}
		strInput += "\nunable to find target to kill!";
		return false;
	}

	private static bool KeywordDamageShip(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (CrewSim.shipCurrentLoaded == null)
		{
			strInput += "\nNo ship currently loaded.";
			return false;
		}
		double num = 0.1;
		if (strings.Length >= 2)
		{
			double.TryParse(strings[1], out num);
			if (num <= 0.0)
			{
				strInput += "\nNon positive amount of damage requested";
				return false;
			}
		}
		CrewSim.shipCurrentLoaded.DamageAllCOs((float)num, true, null);
		strInput = strInput + "\nDamaged ship by " + (num * 100.0).ToString("N2") + "%";
		return true;
	}

	private static bool KeywordBreakInShip(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (CrewSim.shipCurrentLoaded == null)
		{
			strInput += "\nNo ship currently loaded.";
			return false;
		}
		double num = 0.1;
		if (strings.Length >= 2)
		{
			double.TryParse(strings[1], out num);
			if (num <= 0.0)
			{
				strInput += "\nNon positive amount of damage requested";
				return false;
			}
		}
		CrewSim.shipCurrentLoaded.fBreakInMultiplier = (float)num;
		CrewSim.shipCurrentLoaded.DebugBreakIn();
		strInput = strInput + "\nBreakIn ship by " + (num * 100.0).ToString("N2") + "%";
		return true;
	}

	private static bool KeywordMeteor(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (CrewSim.shipCurrentLoaded == null)
		{
			strInput += "\nNo ship currently loaded.";
			return false;
		}
		int num = 1;
		if (strings.Length >= 2)
		{
			int.TryParse(strings[1], out num);
			if (num < 1)
			{
				strInput += "\nNon positive amount of meteor requested.";
				return false;
			}
		}
		JsonAttackMode attackMode = DataHandler.GetAttackMode("AModeMicrometeoroid");
		if (attackMode == null)
		{
			strInput += "\nAttack mode for meteor not found.";
			return false;
		}
		strInput = strInput + "\nSpawning " + num.ToString() + " meteor.";
		for (int i = 0; i < num; i++)
		{
			CrewSim.shipCurrentLoaded.DamageRayRandom(attackMode, 1f, null, false);
		}
		return true;
	}

	private static bool KeywordOxygen(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (CrewSim.shipCurrentLoaded == null)
		{
			strInput += "\nNo ship currently loaded.";
			return false;
		}
		double num = 0.1;
		if (strings.Length >= 2)
		{
			double.TryParse(strings[1], out num);
			if (num <= 0.0)
			{
				strInput += "\nNon positive amount of oxygen requested";
				return false;
			}
		}
		foreach (CondOwner condOwner in CrewSim.shipCurrentLoaded.GetPeople(false))
		{
			if (condOwner.bAlive)
			{
				condOwner.AddCondAmount("StatOxygen", num, 0.0, 0f);
				string text = strInput;
				strInput = string.Concat(new string[]
				{
					text,
					"\nAdded ",
					num.ToString(),
					" StatOxygen to: ",
					condOwner.strNameFriendly
				});
				break;
			}
		}
		return true;
	}

	private static bool KeywordToggle(ref string strInput, string[] strings)
	{
		if (strings.Length == 1)
		{
			strInput += "\nNothing given to toggle";
			return false;
		}
		string text = strings[1];
		switch (text)
		{
		case "fog":
			strInput += "\nToggling fog!";
			PhotoMode.ToggleFog();
			return true;
		case "ui":
			strInput += "\nToggling UI!";
			PhotoMode.ToggleUI();
			return true;
		case "bugs":
			strInput += "\nToggling bugs!";
			ConsoleToGUI.ToggleBugs();
			return true;
		case "debug":
			strInput += "\nToggling debug mode!";
			CrewSim.ToggleDebug();
			return true;
		case "aoshow":
			strInput += "\nToggling ambient occlusion!";
			PhotoMode.ToggleAO();
			return true;
		case "aozoom":
			strInput += "\nToggling ambient occlusion zoom!";
			PhotoMode.ToggleAOZoom();
			return true;
		case "aospread":
		{
			strInput += "\nSetting Ambient Occlusion Spread!";
			float spread = 11.25f;
			if (strings.Length >= 3)
			{
				spread = float.Parse(strings[2]);
			}
			PhotoMode.AOSpread(spread);
			return true;
		}
		case "aointensity":
		{
			strInput += "\nSetting Ambient Occlusion Intensity!";
			float intensity = 0.66f;
			if (strings.Length >= 3)
			{
				intensity = float.Parse(strings[2]);
			}
			PhotoMode.AOIntensity(intensity);
			return true;
		}
		case "all":
			strInput += "\nToggling all!";
			PhotoMode.ToggleFog();
			PhotoMode.ToggleUI();
			CrewSim.ToggleDebug();
			ConsoleToGUI.ToggleBugs();
			return true;
		}
		strInput += "\nToggle item not found";
		return false;
	}

	private static bool KeywordSkywalk(ref string strInput, string[] strings)
	{
		if (!CrewSim.objInstance || CrewSim.system == null)
		{
			return false;
		}
		string text = strings[1];
		string strRegID = CrewSim.coPlayer.ship.strRegID;
		if (text == strRegID)
		{
			strInput += "\nThat would telefrag you. Aborting";
			return false;
		}
		if (!CrewSim.system.dictShips.ContainsKey(text))
		{
			strInput = strInput + "\nCan't find a ship by ID " + text;
			return false;
		}
		CrewSim.objInstance.TeleportCO(CrewSim.coPlayer, text);
		string text2 = strInput;
		strInput = string.Concat(new string[]
		{
			text2,
			"\nTeleporting player from + ",
			strRegID,
			" to ",
			text
		});
		return true;
	}

	private static bool KeywordRelationship(ref string strInput, string[] strings)
	{
		if (!CrewSim.objInstance || CrewSim.system == null)
		{
			return false;
		}
		string text = strings[1];
		if (!DataHandler.dictConds.ContainsKey(text))
		{
			strInput = strInput + "\nCan't find " + text;
			return false;
		}
		string text2 = string.Empty;
		string text3 = string.Empty;
		if (strings.Length == 3)
		{
			text2 = ((CrewSim.aSelected.Count <= 0) ? CrewSim.coPlayer.strName : CrewSim.aSelected[0].strName);
			text3 = ConsoleResolver.FormatHumanNameFromUnderscores(strings[2]);
		}
		else if (strings.Length == 4)
		{
			text2 = ConsoleResolver.FormatHumanNameFromUnderscores(strings[2]);
			text3 = ConsoleResolver.FormatHumanNameFromUnderscores(strings[3]);
		}
		CondOwner condOwner = null;
		if (!DataHandler.mapCOs.TryGetValue(text2, out condOwner) || condOwner.pspec == null)
		{
			strInput = strInput + "\nCan't find Pspec for " + text2;
			return false;
		}
		PersonSpec pspec = condOwner.pspec;
		condOwner = null;
		if (!DataHandler.mapCOs.TryGetValue(text3, out condOwner) || condOwner.pspec == null)
		{
			strInput = strInput + "\nCan't find Pspec for " + text3;
			return false;
		}
		PersonSpec pspec2 = condOwner.pspec;
		if (!pspec.GetCO().socUs.HasPerson(pspec2))
		{
			pspec.GetCO().socUs.AddPerson(new Relationship(pspec2, new List<string>
			{
				text
			}, new List<string>()));
		}
		else
		{
			pspec.GetCO().socUs.GetRelationship(text3).AddRelationship(pspec2.GetCO(), text);
		}
		bool flag = "aeiouAEIOU".Contains(DataHandler.GetCond(text).strNameFriendly[0]);
		string text4 = strInput;
		strInput = string.Concat(new string[]
		{
			text4,
			"\n",
			pspec.FullName,
			" considers ",
			pspec2.FullName,
			" a",
			(!flag) ? " " : "n ",
			DataHandler.GetCond(text).strNameFriendly
		});
		return true;
	}

	private static bool KeywordSummon(ref string strInput, string[] strings)
	{
		if (!CrewSim.objInstance || CrewSim.system == null)
		{
			return false;
		}
		string text = ConsoleResolver.FormatHumanNameFromUnderscores(strings[1]);
		CondOwner coRider;
		if (DataHandler.mapCOs.TryGetValue(text, out coRider))
		{
			CrewSim.objInstance.TeleportCO(coRider, CrewSim.coPlayer.ship.strRegID);
			return true;
		}
		strInput = strInput + "\nCan't find someone named " + text;
		return false;
	}

	private static bool KeywordPlot(ref string strInput, string[] strings)
	{
		if (strings.Length == 1)
		{
			strInput += "\nNothing given to plot";
			return false;
		}
		JsonPlot plot = DataHandler.GetPlot(strings[1]);
		if (plot == null)
		{
			strInput += "\nPlot name not recognised";
			return false;
		}
		PlotManager.bDebugLogging = true;
		int num = 0;
		JsonPlotSave jsonPlotSave;
		if (PlotManager.dictPlotsActive.TryGetValue(plot.strName, out jsonPlotSave))
		{
			if (CrewSim.coPlayer.strID != jsonPlotSave.dictCOTokens["[protag]"])
			{
				return false;
			}
			num = jsonPlotSave.nPhase;
		}
		string[] array = plot.aPhases[num];
		PlotManager.CheckPlot(plot.strName, CrewSim.coPlayer, (PlotManager.PlotTensionType)3, null, true);
		return true;
	}

	private static bool KeywordLookup(ref string strInput, string[] strings)
	{
		if (strings.Length == 1)
		{
			strInput += "\nNothing given to lookup";
			return false;
		}
		string text = strings[1];
		if (text != null)
		{
			if (text == "tutorials")
			{
				foreach (TutorialBeat tutorialBeat in CrewSimTut.TutorialBeats)
				{
					strInput = strInput + "\n" + tutorialBeat.GetType().ToString();
				}
				return true;
			}
			if (!(text == "ships") && !(text == "ship"))
			{
				if (text == "plots" || text == "plot")
				{
					strInput += "\nLooking for plots!";
					bool flag = false;
					string text2 = string.Empty;
					if (strings.Length > 2)
					{
						for (int i = 2; i < strings.Length; i++)
						{
							if (flag)
							{
								text2 += " ";
							}
							text2 += strings[i];
						}
					}
					strInput += PlotManager.GetAllPlots(text2.ToLower());
					return true;
				}
			}
			else
			{
				strInput += "\nGetting ships!";
				if (CrewSim.objInstance == null)
				{
					return false;
				}
				bool flag2 = false;
				string text3 = string.Empty;
				if (strings.Length > 2)
				{
					for (int j = 2; j < strings.Length; j++)
					{
						if (flag2)
						{
							text3 += " ";
						}
						text3 += strings[j];
						flag2 = true;
					}
				}
				text3 = text3.ToLower();
				foreach (Ship ship in CrewSim.system.dictShips.Values)
				{
					if (text3 == string.Empty || ship.publicName.ToLower().Contains(text3))
					{
						string text4 = strInput;
						strInput = string.Concat(new string[]
						{
							text4,
							"\n",
							ship.strRegID,
							" : ",
							ship.publicName
						});
					}
				}
				return true;
			}
		}
		strInput += "\nLookup item not found";
		return false;
	}

	private static bool KeywordShip(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (strings.Length == 1)
		{
			strInput += "\nNo ship given";
			return false;
		}
		string text = strInput.Remove(0, strings[0].Length + 1);
		strInput = strInput + "\nGetting Ship:" + text;
		Ship ship = CrewSim.system.SpawnShip(text, null, Ship.Loaded.Shallow, Ship.Damage.New, CrewSim.coPlayer.strID, 100, false);
		if (ship == null)
		{
			strInput += "\nShip not found!";
			return false;
		}
		Ship ship2 = CrewSim.coPlayer.ship;
		ship = CrewSim.system.SpawnShip(ship.strRegID, Ship.Loaded.Full);
		ship.ToggleVis(true, true);
		ship2.ToggleVis(false, true);
		ship.MoveShip(-ship.vShipPos);
		CrewSim.MoveCO(CrewSim.coPlayer, ship, true);
		CrewSim.coPlayer.ClaimShip(ship.strRegID);
		Ship ship3 = ship2;
		ship.objSS.vPosx = ship3.objSS.vPosx;
		ship.objSS.vPosy = ship3.objSS.vPosy;
		Vector2 a = MathUtils.GetPushbackVector(ship, ship3);
		a *= 4.679211E-08f;
		ship.objSS.vPosx = ship3.objSS.vPosx + (double)a.x;
		ship.objSS.vPosy = ship3.objSS.vPosy + (double)a.y;
		ship.objSS.vVelX = ship3.objSS.vVelX;
		ship.objSS.vVelY = ship3.objSS.vVelY;
		MonoSingleton<ObjectiveTracker>.Instance.RemoveShipSubscription(ship2.strRegID);
		MonoSingleton<ObjectiveTracker>.Instance.AddShipSubscription(ship.strRegID);
		BeatManager.ResetTensionTimer();
		BeatManager.ResetReleaseTimer();
		return true;
	}

	private static bool KeywordShipVis(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (strings.Length > 3)
		{
			strInput += "\nNot enough params given";
			strInput += "\nshipvis \\<state\\> \\<ship name\\>";
			return false;
		}
		string text = strInput.Remove(0, strings[0].Length + 1 + strings[1].Length + 1);
		bool bShow = strings[1].Equals("True");
		strInput = strInput + "\nFinding Ship:" + text;
		Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
		if (shipByRegID == null)
		{
			strInput += "\nShip not found!";
			return false;
		}
		shipByRegID.ToggleVis(bShow, true);
		return true;
	}

	private static bool KeywordPriceFlips(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		CondOwner condOwner = null;
		string empty = string.Empty;
		if (strings.Length == 1)
		{
			condOwner = CrewSim.GetSelectedCrew();
			if (condOwner == null)
			{
				strInput += "\nCondOwner not found.";
			}
		}
		else if (strings.Length == 2)
		{
			if (strings[1] == "[us]" || strings[1] == "player")
			{
				condOwner = CrewSim.GetSelectedCrew();
				if (condOwner == null)
				{
					strInput += "\nCondOwner not found.";
				}
			}
			else if (strings[1] == "[them]")
			{
				if (!(GUIMegaToolTip.Selected != null))
				{
					strInput += "\nNo target selected for [them].";
					return false;
				}
				condOwner = GUIMegaToolTip.Selected;
			}
			else
			{
				string text = strings[1].Replace('_', ' ');
				if (!DataHandler.mapCOs.TryGetValue(text, out condOwner))
				{
					List<CondOwner> cos = CrewSim.shipCurrentLoaded.GetCOs(null, true, true, true);
					foreach (CondOwner condOwner2 in cos)
					{
						if (condOwner2.strNameFriendly == strings[1] || condOwner2.strNameFriendly == text || condOwner2.strName == strings[1] || condOwner2.strName == text || condOwner2.strID == strings[1])
						{
							condOwner = condOwner2;
							break;
						}
					}
				}
			}
			if (condOwner == null)
			{
				strInput += "\nCondOwner not found.";
			}
		}
		else if (strings.Length > 2)
		{
			strInput += "\nToo many parameters.";
			return false;
		}
		if (!(condOwner != null))
		{
			return false;
		}
		CondOwner condOwner3 = DataHandler.GetCondOwner("ItmCargoLift01");
		if (condOwner3 == null)
		{
			strInput += "\nCannot load ItmCargoLift01 to store items in.";
			return false;
		}
		condOwner.ship.AddCO(condOwner3, false);
		CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsBarter");
		CondTrigger condTrigger2 = condTrigger;
		Trader component = condOwner.GetComponent<Trader>();
		if (component != null)
		{
			component.UpdateGPMs();
			if (component.CTBuy != null)
			{
				condTrigger2 = component.CTBuy;
			}
		}
		List<string> list = new List<string>(DataHandler.dictCOs.Keys);
		list.AddRange(DataHandler.dictCOOverlays.Keys);
		double num = condOwner.GetCondAmount("DiscountBuy");
		if (num == 0.0)
		{
			num = 1.0;
		}
		double num2 = condOwner.GetCondAmount("DiscountSell");
		if (num2 == 0.0)
		{
			num2 = 1.0;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string text2 in list)
		{
			if (text2.IndexOf("Dmg") < 0)
			{
				CondOwner condOwner4 = DataHandler.GetCondOwner(text2);
				if (!(condOwner4 == null))
				{
					if (!condTrigger2.Triggered(condOwner4, null, true))
					{
						condOwner4.Destroy();
					}
					else
					{
						double num3 = condOwner4.GetBasePrice(true) * num;
						Destructable component2 = condOwner4.GetComponent<Destructable>();
						if (component2 == null)
						{
							condOwner4.Destroy();
						}
						else
						{
							string dmgLoot = component2.GetDmgLoot("StatDamage");
							if (string.IsNullOrEmpty(dmgLoot))
							{
								condOwner4.Destroy();
							}
							else
							{
								Loot loot = DataHandler.GetLoot(dmgLoot);
								Interaction interaction = DataHandler.GetInteraction(loot.GetLootNameSingle(null), null, false);
								if (interaction == null)
								{
									condOwner4.Destroy();
								}
								else
								{
									Interaction interaction2 = interaction;
									CondOwner condOwner5 = condOwner4;
									interaction.objThem = condOwner5;
									interaction2.objUs = condOwner5;
									string strID = condOwner4.strID;
									string strName = condOwner4.strName;
									condOwner3.AddCO(condOwner4, false, true, true);
									interaction.ApplyEffects(null, false);
									CondOwner condOwner6 = null;
									if (DataHandler.mapCOs.TryGetValue(strID, out condOwner6) && condOwner6.strName.IndexOf("ItmScrap") != 0 && condOwner6.strName.IndexOf("ItmParts") != 0)
									{
										double num4 = condOwner6.GetBasePrice(true) * num2;
										double num5 = num3 / num4;
										if (num5 < 1.0)
										{
										}
										string text3 = string.Concat(new string[]
										{
											num5.ToString("#.00"),
											" ",
											strName,
											" vs ",
											condOwner6.strName,
											": ",
											num3.ToString("#.00"),
											" vs ",
											num4.ToString("#.00")
										});
										stringBuilder.AppendLine(text3);
										Debug.Log(text3);
									}
								}
							}
						}
					}
				}
			}
		}
		condOwner3.RemoveFromCurrentHome(true);
		condOwner3.Destroy();
		strInput += stringBuilder.ToString();
		return true;
	}

	private static bool KeywordMeatState(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (strings.Length == 1)
		{
			strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
			return true;
		}
		if (strings.Length == 2)
		{
			string text = strings[1];
			switch (text)
			{
			case "Inert":
			case "inert":
			case "0":
				CrewSim.eMeatState = MeatState.Inert;
				strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
				return true;
			case "Dormant":
			case "dormant":
			case "1":
				CrewSim.eMeatState = MeatState.Dormant;
				strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
				return true;
			case "Spread":
			case "spread":
			case "2":
				CrewSim.eMeatState = MeatState.Spread;
				strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
				return true;
			case "Decay":
			case "decay":
			case "3":
				CrewSim.eMeatState = MeatState.Decay;
				strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
				return true;
			case "Eradicate":
			case "eradicate":
			case "4":
				CrewSim.eMeatState = MeatState.Eradicate;
				strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
				return true;
			case "Hell":
			case "hell":
			case "5":
				CrewSim.eMeatState = MeatState.Hell;
				strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
				return true;
			}
			strInput = strInput + "\nMeatState not recognised: " + strings[1];
			strInput = strInput + "\nMeatState: " + CrewSim.eMeatState.ToString();
			return false;
		}
		return true;
	}

	private static bool KeywordPDA(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (CrewSim.guiPDA == null)
		{
			strInput += "\nPDA instance not found.";
			return false;
		}
		if (strings.Length >= 2)
		{
			string text = strings[1];
			if (text != null)
			{
				if (text == "open")
				{
					if (strings.Length > 2)
					{
						GUIPDA.OpenApp(strings[2]);
					}
					return true;
				}
				if (text == "unlock")
				{
					if (CrewSim.guiPDA.pdaVisualisers != null)
					{
						CrewSim.guiPDA.pdaVisualisers.EnableEverything();
					}
					return true;
				}
				if (text == "show")
				{
					if (CrewSim.guiPDA.homepage != null && strings.Length > 2)
					{
						CrewSim.guiPDA.homepage.UnHideApp(strings[2]);
					}
					return true;
				}
				if (text == "hide")
				{
					if (CrewSim.guiPDA.homepage != null && strings.Length > 2)
					{
						CrewSim.guiPDA.homepage.HideApp(strings[2]);
					}
					return true;
				}
				if (text == "reset")
				{
					if (CrewSim.guiPDA.pdaVisualisers != null)
					{
						CrewSim.guiPDA.pdaVisualisers.ResetPresets();
					}
					return true;
				}
			}
			strInput += "\nCommand parameter not recognised!";
			return false;
		}
		return true;
	}

	private static bool KeywordRename(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (GUIMegaToolTip.Selected == null)
		{
			strInput += "\nSelected item not found.";
			return false;
		}
		if (GUIMegaToolTip.Selected.IsHumanOrRobot)
		{
			strInput += "\nSelected item cannot be crew or robot.";
			return false;
		}
		if (strings.Length >= 2)
		{
			string text = strInput.Substring(strings[0].Length);
			string text2 = strInput;
			strInput = string.Concat(new string[]
			{
				text2,
				"\n",
				GUIMegaToolTip.Selected.strNameFriendly,
				" renamed to :",
				text
			});
			GUIMegaToolTip.Selected.Rename(text);
			GUIMegaToolTip.Selected = GUIMegaToolTip.Selected;
			return true;
		}
		return false;
	}

	private static bool KeywordWipe(ref string strInput, string[] strings)
	{
		if (CrewSim.objInstance == null)
		{
			strInput += "\nCrewSim instance not found.";
			return false;
		}
		if (GUIMegaToolTip.Selected == null)
		{
			strInput += "\nSelected item not found.";
			return false;
		}
		if (GUIMegaToolTip.Selected.IsHumanOrRobot)
		{
			strInput += "\nSelected item cannot be crew or robot.";
			return false;
		}
		if (GUIMegaToolTip.Selected.Electrical == null)
		{
			strInput += "\nNo Electrical on selected object.";
			return false;
		}
		if (strings.Length >= 2)
		{
			if (strings[1] == "soft")
			{
				GUIMegaToolTip.Selected.Electrical.AuditConnections();
				return true;
			}
			if (strings[1] == "med" || strings[1] == "medium")
			{
				GUIMegaToolTip.Selected.Electrical.AuditConnections();
				GUIMegaToolTip.Selected.Electrical.CleanUp(true);
				GUIMegaToolTip.Selected.Electrical.inputConnections.Clear();
				GUIMegaToolTip.Selected.Electrical.RecoverBlank();
				GUIMegaToolTip.Selected.Electrical.SetGPM();
				return true;
			}
		}
		GUIMegaToolTip.Selected.Electrical.AuditConnections();
		GUIMegaToolTip.Selected.Electrical.CleanUp(true);
		GUIMegaToolTip.Selected.Electrical.inputConnections.Clear();
		GUIMegaToolTip.Selected.Electrical.RecoverBlank();
		GUIMegaToolTip.Selected.Electrical.SetGPM();
		GUIMegaToolTip.Selected.ZeroCondAmount("IsSignalOff");
		GUIMegaToolTip.Selected.ZeroCondAmount("IsOverrideOff");
		GUIMegaToolTip.Selected.ZeroCondAmount("IsConnected");
		GUIMegaToolTip.Selected.ZeroCondAmount("IsSignalledOn");
		return true;
	}

	public static string FormatHumanNameFromUnderscores(string CONameWithUnderscores)
	{
		string str = CONameWithUnderscores.Replace('_', ' ');
		return ConsoleResolver.TextInfo.ToTitleCase(str);
	}

	private static TextInfo _textInfo;
}
