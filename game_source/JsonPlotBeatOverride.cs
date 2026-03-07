using System;

public class JsonPlotBeatOverride
{
	public string strName { get; set; }

	public string strTemplateBeat { get; set; }

	public string strFind { get; set; }

	public string strReplace { get; set; }

	public string[] aOverrideBeatValues { get; set; }

	public string[] aOverrideTriggerIAValues { get; set; }

	public JsonPlotBeat Generate()
	{
		if (this.strTemplateBeat == null)
		{
			return null;
		}
		string key = JsonPlotBeat.CloneDeep(this.strTemplateBeat, this.strReplace, this.strFind);
		JsonPlotBeat jsonPlotBeat = null;
		DataHandler.dictPlotBeats.TryGetValue(key, out jsonPlotBeat);
		if (jsonPlotBeat == null)
		{
			return null;
		}
		DataHandler.ApplyOverride(jsonPlotBeat, this.aOverrideBeatValues);
		JsonInteraction jsonInteraction = null;
		if (jsonPlotBeat.strIATrigger != null)
		{
			string ianameFromString = PlotManager.GetIANameFromString(jsonPlotBeat.strIATrigger);
			DataHandler.dictInteractions.TryGetValue(ianameFromString, out jsonInteraction);
		}
		if (jsonInteraction == null)
		{
			return null;
		}
		DataHandler.ApplyOverride(jsonInteraction, this.aOverrideTriggerIAValues);
		return jsonPlotBeat;
	}
}
