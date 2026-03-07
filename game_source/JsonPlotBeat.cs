using System;

public class JsonPlotBeat
{
	public string strName { get; set; }

	public string strIATrigger { get; set; }

	public string strTokenSetUs { get; set; }

	public string strTokenSetThem { get; set; }

	public string strTokenSet3rd { get; set; }

	public bool bTension { get; set; }

	public bool bRelease { get; set; }

	public bool bNoticeable { get; set; }

	public int nPhaseChange { get; set; }

	public bool bNoObjective { get; set; }

	public JsonPlotBeat Clone()
	{
		return new JsonPlotBeat
		{
			strName = this.strName,
			strIATrigger = this.strIATrigger,
			strTokenSetUs = this.strTokenSetUs,
			strTokenSetThem = this.strTokenSetThem,
			strTokenSet3rd = this.strTokenSet3rd,
			bTension = this.bTension,
			bRelease = this.bRelease,
			bNoticeable = this.bNoticeable,
			nPhaseChange = this.nPhaseChange,
			bNoObjective = this.bNoObjective
		};
	}

	public JsonPlotBeat CloneDeep(string strFind, string strReplace)
	{
		if (string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strFind == strReplace)
		{
			return this.Clone();
		}
		JsonPlotBeat jsonPlotBeat = this.Clone();
		jsonPlotBeat.strName = this.strName.Replace(strFind, strReplace);
		string ianameFromString = PlotManager.GetIANameFromString(this.strIATrigger);
		string newValue = JsonInteraction.CloneDeep(ianameFromString, strReplace, strFind);
		jsonPlotBeat.strIATrigger = this.strIATrigger.Replace(ianameFromString, newValue);
		DataHandler.dictPlotBeats[jsonPlotBeat.strName] = jsonPlotBeat;
		return jsonPlotBeat;
	}

	public static string CloneDeep(string strOrigName, string strReplace, string strFind)
	{
		if (string.IsNullOrEmpty(strOrigName) || string.IsNullOrEmpty(strReplace) || string.IsNullOrEmpty(strFind) || strReplace == strFind || strOrigName.IndexOf(strFind) < 0)
		{
			return strOrigName;
		}
		JsonPlotBeat jsonPlotBeat = null;
		if (!DataHandler.dictPlotBeats.TryGetValue(strOrigName, out jsonPlotBeat))
		{
			return strOrigName;
		}
		string text = strOrigName.Replace(strFind, strReplace);
		JsonPlotBeat jsonPlotBeat2 = null;
		if (!DataHandler.dictPlotBeats.TryGetValue(text, out jsonPlotBeat2))
		{
			jsonPlotBeat2 = jsonPlotBeat.CloneDeep(strFind, strReplace);
		}
		return text;
	}

	public override string ToString()
	{
		return this.strName;
	}
}
