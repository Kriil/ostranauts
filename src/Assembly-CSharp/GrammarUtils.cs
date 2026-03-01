using System;
using System.Collections.Generic;
using System.Text;

// Text inflection and pronoun helper. This appears to expand tokenized strings
// for interactions, conditions, logs, and job text based on involved entities.
public static class GrammarUtils
{
	// Maps a CondOwner to the pronoun/part-of-speech bucket used by the lookup
	// tables below (player, male, female, non-binary, non-human, etc.).
	public static int COToPOSIndex(CondOwner CO)
	{
		if (!CO)
		{
			return 0;
		}
		int result = 5;
		if (!CO.HasCond("IsHuman"))
		{
			return result;
		}
		if (CO.HasCond("IsPlayer"))
		{
			return 1;
		}
		if (CO.HasCond("IsFemale"))
		{
			return 3;
		}
		if (CO.HasCond("IsMale"))
		{
			return 2;
		}
		if (CO.HasCond("IsNB"))
		{
			return 4;
		}
		return result;
	}

	// Expands an inflected string for a condition/context pair.
	public static string GetInflectedString(string target, Condition condition, CondOwner condOwner)
	{
		if (string.IsNullOrEmpty(target))
		{
			return string.Empty;
		}
		if (!GrammarUtils.inflectedStrings.TryGetValue(target, out GrammarUtils.InflectedString))
		{
			return target;
		}
		GrammarUtils.targetString = target;
		GrammarUtils.outputVisibleToPlayer = false;
		GrammarUtils.replacements = GrammarUtils.InflectedString.tokens;
		GrammarUtils.PrepareString(condOwner, condition);
		GrammarUtils.GenerateString();
		return GrammarUtils.interactionOutput.ToString();
	}

	// Fallback expansion path when only the template string is needed.
	public static string GetInflectedString(string target, object o)
	{
		if (string.IsNullOrEmpty(target))
		{
			return string.Empty;
		}
		if (!GrammarUtils.inflectedStrings.TryGetValue(target, out GrammarUtils.InflectedString))
		{
			return target;
		}
		GrammarUtils.targetString = target;
		GrammarUtils.outputVisibleToPlayer = false;
		GrammarUtils.replacements = GrammarUtils.InflectedString.tokens;
		GrammarUtils.GenerateString();
		return GrammarUtils.interactionOutput.ToString();
	}

	// Expands an interaction description while binding `us`, `them`, and `3rd`
	// entities to the live interaction participants.
	public static string GetInflectedString(string target, Interaction interaction)
	{
		if (string.IsNullOrEmpty(target))
		{
			return string.Empty;
		}
		if (!GrammarUtils.inflectedStrings.TryGetValue(target, out GrammarUtils.InflectedString))
		{
			return target;
		}
		GrammarUtils.targetString = target;
		GrammarUtils.tempInteraction = interaction;
		if (GrammarUtils.outputVisibleToPlayer)
		{
			if (!(interaction.objUs == CrewSim.coPlayer) && !(interaction.objThem == CrewSim.coPlayer))
			{
				GrammarUtils.outputVisibleToPlayer = false;
			}
		}
		GrammarUtils.replacements = GrammarUtils.InflectedString.tokens;
		GrammarUtils.PrepareString(interaction);
		GrammarUtils.GenerateString();
		return GrammarUtils.interactionOutput.ToString();
	}

	// Clears the cached sentence entities before preparing a new expansion.
	public static void ClearLast()
	{
		foreach (KeyValuePair<string, GrammarUtils.SentenceEntity> keyValuePair in GrammarUtils.entityMap)
		{
			keyValuePair.Value.Reset();
		}
	}

	// Prepares a single-entity context, usually for condition text.
	public static void PrepareString(CondOwner condOwner, Condition condition)
	{
		GrammarUtils.ClearLast();
		GrammarUtils.entityMap["us"].Set(condOwner);
	}

	// Prepares a multi-entity context from an interaction's participants.
	public static void PrepareString(Interaction interaction)
	{
		GrammarUtils.ClearLast();
		if (interaction.objUs)
		{
			GrammarUtils.entityMap["us"].Set(interaction.objUs);
		}
		if (interaction.objThem)
		{
			GrammarUtils.entityMap["them"].Set(interaction.objThem);
		}
		if (interaction.obj3rd)
		{
			GrammarUtils.entityMap["3rd"].Set(interaction.obj3rd);
		}
	}

	// Main token replacement pass. Walks the parsed token list and substitutes
	// names, pronouns, verb forms, and other grammar fragments.
	private static void GenerateString()
	{
		GrammarUtils.interactionOutput.Length = 0;
		int num = 0;
		for (int i = 0; i < GrammarUtils.replacements.Count; i++)
		{
			InflectedTokenData inflectedTokenData = GrammarUtils.replacements[i];
			GrammarUtils.interactionOutput.Append(GrammarUtils.targetString, num, inflectedTokenData.start - num);
			GrammarUtils.SentenceEntity sentenceEntity = null;
			GrammarUtils.UsThem3rd usThem3Rd = inflectedTokenData.usThem3Rd;
			if (usThem3Rd != GrammarUtils.UsThem3rd.Us)
			{
				if (usThem3Rd != GrammarUtils.UsThem3rd.Them)
				{
					if (usThem3Rd == GrammarUtils.UsThem3rd.Third)
					{
						sentenceEntity = GrammarUtils.entityMap["3rd"];
					}
				}
				else
				{
					sentenceEntity = GrammarUtils.entityMap["them"];
				}
			}
			else
			{
				sentenceEntity = GrammarUtils.entityMap["us"];
			}
			bool flag = false;
			bool flag2 = false;
			if (inflectedTokenData.replacementType == GrammarUtils.ReplacementType.Other)
			{
				if (inflectedTokenData.replacementOther == GrammarUtils.ReplacementOther.Data || inflectedTokenData.replacementOther == GrammarUtils.ReplacementOther.None)
				{
					flag = true;
				}
				if (!flag && (sentenceEntity == null || sentenceEntity.CondOwner == null))
				{
					flag2 = true;
				}
			}
			if ((sentenceEntity == null || sentenceEntity.CondOwner == null) && (inflectedTokenData.replacementType == GrammarUtils.ReplacementType.Name || inflectedTokenData.replacementType == GrammarUtils.ReplacementType.FromLUT || inflectedTokenData.replacementType == GrammarUtils.ReplacementType.FromVerbList))
			{
				flag2 = true;
			}
			if (flag2)
			{
				GrammarUtils.interactionOutput.Append(GrammarUtils.targetString, inflectedTokenData.start, inflectedTokenData.end - inflectedTokenData.start + 1);
				num = inflectedTokenData.end + 1;
				if (i == GrammarUtils.replacements.Count - 1)
				{
					GrammarUtils.interactionOutput.Append(GrammarUtils.targetString, num, GrammarUtils.targetString.Length - num);
				}
				if (GrammarUtils.outputVisibleToPlayer)
				{
					foreach (KeyValuePair<string, GrammarUtils.SentenceEntity> keyValuePair in GrammarUtils.entityMap)
					{
						if (keyValuePair.Value.CondOwner)
						{
							GrammarUtils.LogSentenceEntity(keyValuePair.Value);
						}
					}
				}
				GrammarUtils.outputVisibleToPlayer = false;
			}
			else
			{
				bool flag3 = GrammarUtils.Capitalise(GrammarUtils.targetString, GrammarUtils.replacements[i].start);
				Dictionary<GrammarUtils.GrammarLUTIndex, string[]> dictionary = (!flag3) ? GrammarUtils.partsOfSpeech : GrammarUtils.partsOfSpeechSentenceCase;
				switch (GrammarUtils.replacements[i].replacementType)
				{
				case GrammarUtils.ReplacementType.Name:
					if (sentenceEntity.InflectionIndex == GrammarUtils.PronounInflection.Second)
					{
						GrammarUtils.interactionOutput.Append(dictionary[GrammarUtils.GrammarLUTIndex.Subjective][1]);
					}
					else
					{
						if (sentenceEntity.InflectionIndex == GrammarUtils.PronounInflection.ThirdNeuterNonHuman)
						{
							if (flag3)
							{
								GrammarUtils.interactionOutput.Append("The ");
								flag3 = false;
							}
							else
							{
								GrammarUtils.interactionOutput.Append("the ");
							}
						}
						if (sentenceEntity.CondOwner.HasCond("IsPlaceholder") && !string.IsNullOrEmpty(sentenceEntity.CondOwner.strPlaceholderInstallFinish))
						{
							GrammarUtils.interactionOutput.Append(DataHandler.GetCOShortName(sentenceEntity.CondOwner.strPlaceholderInstallFinish));
						}
						else if (GrammarUtils.highlight != null)
						{
							string value = sentenceEntity.CondOwner.ShortName;
							if (sentenceEntity.CondOwner.HasCond("IsPlaceholder"))
							{
								value = DataHandler.GetCOShortName(sentenceEntity.CondOwner.strPlaceholderInstallFinish);
							}
							else if (GrammarUtils.highlight == sentenceEntity.CondOwner)
							{
								value = "<color=#FFCC00>" + LinkOpener.GetCOLink(sentenceEntity.CondOwner) + "</color>";
							}
							else
							{
								value = LinkOpener.GetCOLink(sentenceEntity.CondOwner);
							}
							GrammarUtils.interactionOutput.Append(value);
						}
						else
						{
							if (!flag3 && sentenceEntity.InflectionIndex == GrammarUtils.PronounInflection.ThirdNeuterNonHuman && !string.IsNullOrEmpty(sentenceEntity.CondOwner.strNameShortLCase))
							{
								GrammarUtils.interactionOutput.Append(sentenceEntity.CondOwner.strNameShortLCase);
							}
							else if (sentenceEntity.logHistory != null && StarSystem.fEpoch <= sentenceEntity.logHistory.lastTimeUsed + 20.0)
							{
								GrammarUtils.interactionOutput.Append(sentenceEntity.logHistory.Alias);
							}
							else
							{
								GrammarUtils.interactionOutput.Append(sentenceEntity.CondOwner.ShortName);
							}
							if (GrammarUtils.highlight != null && sentenceEntity.CondOwner == GrammarUtils.highlight)
							{
								GrammarUtils.interactionOutput.Append("</color>");
							}
							sentenceEntity.named = true;
						}
					}
					break;
				case GrammarUtils.ReplacementType.FromLUT:
					if (inflectedTokenData.LUTIndex == GrammarUtils.GrammarLUTIndex.FullName && sentenceEntity != null && sentenceEntity.CondOwner)
					{
						GrammarUtils.interactionOutput.Append(sentenceEntity.CondOwner.ShortName);
						sentenceEntity.named = true;
					}
					else if (!sentenceEntity.named && sentenceEntity.InflectionIndex != GrammarUtils.PronounInflection.Second)
					{
						GrammarUtils.interactionOutput.Append(sentenceEntity.CondOwner.ShortName);
						if (inflectedTokenData.LUTIndex == GrammarUtils.GrammarLUTIndex.Possessive)
						{
							GrammarUtils.interactionOutput.Append("'s");
						}
						sentenceEntity.named = true;
					}
					else
					{
						GrammarUtils.interactionOutput.Append(dictionary[inflectedTokenData.LUTIndex][(int)sentenceEntity.InflectionIndex]);
					}
					break;
				case GrammarUtils.ReplacementType.FromVerbList:
				{
					int num2 = 0;
					if (sentenceEntity.lastSubjectiveWasPronoun && sentenceEntity.InflectionIndex == GrammarUtils.PronounInflection.ThirdNeuter)
					{
						num2 = 1;
					}
					if (sentenceEntity.InflectionIndex == GrammarUtils.PronounInflection.Second)
					{
						num2 = 1;
					}
					GrammarUtils.interactionOutput.Append(inflectedTokenData.verbForms[num2]);
					break;
				}
				case GrammarUtils.ReplacementType.Other:
					switch (GrammarUtils.replacements[i].replacementOther)
					{
					case GrammarUtils.ReplacementOther.RegID:
						if (GrammarUtils.tempInteraction != null)
						{
							if (GrammarUtils.tempInteraction.bOpener && sentenceEntity.usThem3Rd == GrammarUtils.UsThem3rd.Us)
							{
								GrammarUtils.interactionOutput.Append(GrammarUtils.GetIcaoRegName(sentenceEntity.CondOwner.ship.strRegID));
							}
							else
							{
								GrammarUtils.interactionOutput.Append(sentenceEntity.CondOwner.ship.strRegID);
							}
						}
						break;
					case GrammarUtils.ReplacementOther.ShipFriendlyName:
						if (GrammarUtils.gigFormat && inflectedTokenData.usThem3Rd == GrammarUtils.UsThem3rd.Them)
						{
							GrammarUtils.interactionOutput.Append(GUIJobs.GetShipName(sentenceEntity.CondOwner.ship, CrewSim.coPlayer.ship));
						}
						else if (GrammarUtils.tempInteraction != null)
						{
							GrammarUtils.interactionOutput.Append(sentenceEntity.CondOwner.ship.publicName);
						}
						break;
					case GrammarUtils.ReplacementOther.Captain:
						if (GrammarUtils.tempInteraction != null)
						{
							GrammarUtils.interactionOutput.Append(sentenceEntity.CondOwner.strName);
						}
						break;
					case GrammarUtils.ReplacementOther.Data:
						if (GrammarUtils.tempInteraction != null)
						{
							GrammarUtils.interactionOutput.Append(GrammarUtils.tempInteraction.GetDataPayload());
						}
						break;
					default:
						GrammarUtils.interactionOutput.Append(GrammarUtils.targetString, inflectedTokenData.start, inflectedTokenData.end - inflectedTokenData.start + 1);
						break;
					}
					break;
				}
				num = inflectedTokenData.end + 1;
				if (i == GrammarUtils.replacements.Count - 1)
				{
					GrammarUtils.interactionOutput.Append(GrammarUtils.targetString, num, GrammarUtils.targetString.Length - num);
				}
				if (GrammarUtils.outputVisibleToPlayer)
				{
					foreach (KeyValuePair<string, GrammarUtils.SentenceEntity> keyValuePair2 in GrammarUtils.entityMap)
					{
						if (keyValuePair2.Value.CondOwner)
						{
							GrammarUtils.LogSentenceEntity(keyValuePair2.Value);
						}
					}
				}
				GrammarUtils.outputVisibleToPlayer = false;
			}
		}
		if (GrammarUtils.tempInteraction != null)
		{
			GrammarUtils.tempInteraction = null;
		}
		if (GrammarUtils.highlight != null)
		{
			GrammarUtils.highlight = null;
		}
		if (GrammarUtils.gigFormat)
		{
			GrammarUtils.gigFormat = false;
		}
	}

	public static string GenerateDescription(Interaction interaction, bool log = false)
	{
		GrammarUtils.outputVisibleToPlayer = log;
		return GrammarUtils.GetInflectedString(interaction.strDesc, interaction);
	}

	public static string GenerateDescription(Interaction interaction)
	{
		GrammarUtils.outputVisibleToPlayer = false;
		return GrammarUtils.GetInflectedString(interaction.strDesc, interaction);
	}

	// Spells out a ship registration id using the ICAO-style letter mapping used
	// by radio/comms text.
	public static string GetIcaoRegName(string regId)
	{
		GrammarUtils.shipRegIDOutput.Length = 0;
		regId = regId.ToLower();
		for (int i = 0; i < regId.Length; i++)
		{
			string value;
			if (GrammarUtils._icaoDict.TryGetValue(regId[i], out value))
			{
				GrammarUtils.shipRegIDOutput.Append(value);
			}
			else
			{
				GrammarUtils.shipRegIDOutput.Append(regId[i]);
			}
			if (i < regId.Length - 1 && regId[i] != ' ')
			{
				GrammarUtils.shipRegIDOutput.Append(' ');
			}
		}
		return GrammarUtils.shipRegIDOutput.ToString();
	}

	// Enables the alternate formatting mode used by job/gig descriptions.
	public static void PrepareGigFormat()
	{
		GrammarUtils.gigFormat = true;
	}

	// Checks whether a replacement token starts a new sentence and should use the
	// sentence-case lookup tables.
	public static bool Capitalise(string desc, int index)
	{
		return index == 0 || (index >= 2 && desc.Length > 2 && desc[index - 2] == '.');
	}

	// Records visible named entities so later log text can keep a stable alias for
	// the player after the first reveal.
	public static void LogSentenceEntity(GrammarUtils.SentenceEntity entity)
	{
		if (!GrammarUtils.outputVisibleToPlayer)
		{
			return;
		}
		if (!entity.named)
		{
			return;
		}
		if (entity.CondOwner == CrewSim.coPlayer)
		{
			return;
		}
		if (entity.CondOwner.pspec == null)
		{
			return;
		}
		LogHistory logHistory = null;
		if (GrammarUtils.logHistories.TryGetValue(entity.CondOwner.strID, out logHistory))
		{
			logHistory.lastTimeUsed = StarSystem.fEpoch;
		}
		else
		{
			logHistory = new LogHistory
			{
				COID = entity.CondOwner.strID,
				lastTimeUsed = StarSystem.fEpoch,
				Alias = entity.CondOwner.pspec.strFirstName
			};
			GrammarUtils.logHistories[entity.CondOwner.strID] = logHistory;
		}
	}

	public static bool outputVisibleToPlayer = false;

	public static readonly Dictionary<string, InflectedString> inflectedStrings = new Dictionary<string, InflectedString>();

	public static readonly Dictionary<string, LogHistory> logHistories = new Dictionary<string, LogHistory>();

	public static readonly Dictionary<GrammarUtils.GrammarLUTIndex, string[]> partsOfSpeech = new Dictionary<GrammarUtils.GrammarLUTIndex, string[]>
	{
		{
			GrammarUtils.GrammarLUTIndex.Subjective,
			new string[]
			{
				"I",
				"you",
				"he",
				"she",
				"they",
				"it"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.Possessive,
			new string[]
			{
				"my",
				"your",
				"his",
				"her",
				"their",
				"its"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.Objective,
			new string[]
			{
				"me",
				"you",
				"him",
				"her",
				"them",
				"it"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.Reflexive,
			new string[]
			{
				"myself",
				"yourself",
				"himself",
				"herself",
				"themself",
				"itself"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.ContractIs,
			new string[]
			{
				"I'm",
				"you're",
				"he's",
				"she's",
				"they're",
				"it's"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.ContractHas,
			new string[]
			{
				"I've",
				"you've",
				"he's",
				"she's",
				"they've",
				"it's"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.ContractWill,
			new string[]
			{
				"I'll",
				"you'll",
				"he'll",
				"she'll",
				"they'll",
				"it'll"
			}
		}
	};

	public static readonly Dictionary<GrammarUtils.GrammarLUTIndex, string[]> partsOfSpeechSentenceCase = new Dictionary<GrammarUtils.GrammarLUTIndex, string[]>
	{
		{
			GrammarUtils.GrammarLUTIndex.Subjective,
			new string[]
			{
				"I",
				"You",
				"He",
				"She",
				"They",
				"It"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.Possessive,
			new string[]
			{
				"My",
				"Your",
				"His",
				"Her",
				"Their",
				"Its"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.Objective,
			new string[]
			{
				"Me",
				"You",
				"Him",
				"Her",
				"Them",
				"It"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.Reflexive,
			new string[]
			{
				"Myself",
				"Yourself",
				"Himself",
				"Herself",
				"Themself",
				"Itself"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.ContractIs,
			new string[]
			{
				"I'm",
				"You're",
				"He's",
				"She's",
				"They're",
				"It's"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.ContractHas,
			new string[]
			{
				"I've",
				"You've",
				"He's",
				"She's",
				"They've",
				"It's"
			}
		},
		{
			GrammarUtils.GrammarLUTIndex.ContractWill,
			new string[]
			{
				"I'll",
				"You'll",
				"He'll",
				"She'll",
				"They'll",
				"It'll"
			}
		}
	};

	private static readonly Dictionary<char, string> _icaoDict = new Dictionary<char, string>
	{
		{
			'a',
			"Alfa"
		},
		{
			'b',
			"Bravo"
		},
		{
			'c',
			"Charlie"
		},
		{
			'd',
			"Delta"
		},
		{
			'e',
			"Echo"
		},
		{
			'f',
			"Foxtrot"
		},
		{
			'g',
			"Golf"
		},
		{
			'h',
			"Hotel"
		},
		{
			'i',
			"India"
		},
		{
			'j',
			"Juliett"
		},
		{
			'k',
			"Kilo"
		},
		{
			'l',
			"Lima"
		},
		{
			'm',
			"Mike"
		},
		{
			'n',
			"November"
		},
		{
			'o',
			"Oscar"
		},
		{
			'p',
			"Papa"
		},
		{
			'q',
			"Quebec"
		},
		{
			'r',
			"Romeo"
		},
		{
			's',
			"Sierra"
		},
		{
			't',
			"Tango"
		},
		{
			'u',
			"Uniform"
		},
		{
			'v',
			"Victor"
		},
		{
			'w',
			"Whiskey"
		},
		{
			'x',
			"X-ray"
		},
		{
			'y',
			"Yankee"
		},
		{
			'z',
			"Zulu"
		},
		{
			'0',
			"Zero"
		},
		{
			'1',
			"One"
		},
		{
			'2',
			"Two"
		},
		{
			'3',
			"Three"
		},
		{
			'4',
			"Four"
		},
		{
			'5',
			"Five"
		},
		{
			'6',
			"Six"
		},
		{
			'7',
			"Seven"
		},
		{
			'8',
			"Eight"
		},
		{
			'9',
			"Nine"
		}
	};

	public static List<GrammarUtils.SentenceEntity> grammarEntities = new List<GrammarUtils.SentenceEntity>();

	public static StringBuilder interactionOutput = new StringBuilder(250);

	public static string targetString;

	public static List<InflectedTokenData> replacements;

	public static InflectedString InflectedString;

	public static Dictionary<string, GrammarUtils.SentenceEntity> entityMap = new Dictionary<string, GrammarUtils.SentenceEntity>
	{
		{
			"us",
			new GrammarUtils.SentenceEntity()
		},
		{
			"them",
			new GrammarUtils.SentenceEntity()
		},
		{
			"3rd",
			new GrammarUtils.SentenceEntity()
		}
	};

	private static bool gigFormat = false;

	public static Interaction tempInteraction;

	public static CondOwner highlight;

	public static StringBuilder shipRegIDOutput = new StringBuilder(100);

	public enum UsThem3rd
	{
		None,
		Us,
		Them,
		Third
	}

	public enum GrammarLUTIndex
	{
		None,
		Subjective,
		Possessive,
		Objective,
		Reflexive,
		ContractIs,
		ContractHas,
		ContractWill,
		FullName
	}

	public enum VerbForm
	{
		None,
		Singular,
		Plural,
		ContractIs,
		ContractHas,
		ContractWill
	}

	public enum ReplacementType
	{
		None,
		Name,
		FromLUT,
		FromVerbList,
		Other
	}

	public enum PronounInflection
	{
		First,
		Second,
		ThirdMasculine,
		ThirdFeminine,
		ThirdNeuter,
		ThirdNeuterNonHuman
	}

	public enum ReplacementOther
	{
		None,
		RegID,
		ShipFriendlyName,
		Captain,
		Data
	}

	public class SentenceEntity
	{
		public void Reset()
		{
			this.CondOwner = null;
			this.named = false;
			this.lastSubjectiveWasPronoun = false;
			this.usThem3Rd = GrammarUtils.UsThem3rd.None;
			this.InflectionIndex = GrammarUtils.PronounInflection.First;
			this.logHistory = null;
		}

		public void Set(CondOwner condOwner)
		{
			this.CondOwner = condOwner;
			this.named = false;
			this.lastSubjectiveWasPronoun = false;
			this.InflectionIndex = (GrammarUtils.PronounInflection)GrammarUtils.COToPOSIndex(condOwner);
			if (GrammarUtils.outputVisibleToPlayer)
			{
				GrammarUtils.logHistories.TryGetValue(condOwner.strID, out this.logHistory);
			}
		}

		public CondOwner CondOwner;

		public bool named;

		public bool lastSubjectiveWasPronoun;

		public GrammarUtils.UsThem3rd usThem3Rd;

		public GrammarUtils.PronounInflection InflectionIndex;

		public LogHistory logHistory;
	}
}
