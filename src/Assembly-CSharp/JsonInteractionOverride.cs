using System;

public class JsonInteractionOverride
{
	public string strName { get; set; }

	public string strTemplateIA { get; set; }

	public string strFind { get; set; }

	public string strReplace { get; set; }

	public string[] aOverrideValues { get; set; }

	public JsonInteraction Generate()
	{
		if (this.strTemplateIA == null)
		{
			return null;
		}
		string key = JsonInteraction.CloneDeep(this.strTemplateIA, this.strReplace, this.strFind);
		JsonInteraction jsonInteraction = null;
		DataHandler.dictInteractions.TryGetValue(key, out jsonInteraction);
		if (jsonInteraction == null)
		{
			return null;
		}
		DataHandler.ApplyOverride(jsonInteraction, this.aOverrideValues);
		return jsonInteraction;
	}
}
