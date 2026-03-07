using System;
using System.Collections.Generic;

// Serialized plot progression state.
// This tracks which beat/phase is active and stores token substitutions
// that map plot placeholders like `[protag]` to live CondOwner ids.
public class JsonPlotSave
{
	// `strPlotName` likely matches a plot definition id in the game's story data.
	public string strPlotName { get; set; }

	// Token table used to resolve plot text placeholders into live entities.
	public Dictionary<string, string> dictCOTokens { get; set; }

	public int nPhase { get; set; }

	public string strCurrentBeat { get; set; }

	public string strCOFocusID { get; set; }

	public string[] aCompletedBeats { get; set; }

	// Manual initializer for save records created before a full JsonPlot is available.
	public void Init(string strName)
	{
		this.strPlotName = strName;
		this.dictCOTokens = new Dictionary<string, string>();
		this.aCompletedBeats = new string[0];
	}

	// Initializes from a plot definition and seeds the protagonist token.
	public void Init(JsonPlot jp, CondOwner coProtag)
	{
		this.strPlotName = jp.strName;
		this.dictCOTokens = new Dictionary<string, string>();
		this.dictCOTokens["[protag]"] = coProtag.strID;
		this.aCompletedBeats = new string[0];
	}

	// Resolves a user-facing plot title from the plot registry when available.
	public string GetPlotFriendlyName()
	{
		string result = this.strPlotName;
		JsonPlot plot = DataHandler.GetPlot(this.strPlotName);
		if (plot != null)
		{
			result = plot.FriendlyName;
		}
		return result;
	}

	// Builds the current phase title for UI, falling back to the current beat name,
	// then expands CondOwner links through the stored token table.
	public string GetCurrentPhaseTitle(CondOwner coHighlight = null, string strOpen = null, string strClose = null)
	{
		string text = null;
		if (this.dictCOTokens != null)
		{
			JsonPlot plot = DataHandler.GetPlot(this.strPlotName);
			if (plot.aPhaseTitles != null && plot.aPhaseTitles.Length > this.nPhase)
			{
				text = plot.aPhaseTitles[this.nPhase];
			}
			if (string.IsNullOrEmpty(text))
			{
				text = this.strCurrentBeat;
			}
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			text = this.GetLinkedText(text, coHighlight, strOpen, strClose);
		}
		return text;
	}

	// Replaces plot tokens with clickable CondOwner links.
	// Likely used by journal/tutorial UI so plot text can open the referenced entity.
	public string GetLinkedText(string strIn, CondOwner coHighlight = null, string strOpen = null, string strClose = null)
	{
		if (string.IsNullOrEmpty(strIn))
		{
			return strIn;
		}
		if (this.dictCOTokens != null)
		{
			foreach (KeyValuePair<string, string> keyValuePair in this.dictCOTokens)
			{
				CondOwner co = null;
				if (DataHandler.mapCOs.TryGetValue(keyValuePair.Value, out co))
				{
					string newValue;
					if (coHighlight != null && coHighlight.strID == keyValuePair.Value)
					{
						newValue = strOpen + LinkOpener.GetCOLink(coHighlight) + strClose;
					}
					else
					{
						newValue = LinkOpener.GetCOLink(co);
					}
					strIn = strIn.Replace(keyValuePair.Key, newValue);
				}
			}
		}
		return strIn;
	}

	// Keeps logs/debug output compact by returning the plot id.
	public override string ToString()
	{
		return this.strPlotName;
	}
}
