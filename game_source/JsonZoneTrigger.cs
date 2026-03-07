using System;

public class JsonZoneTrigger
{
	public string strName { get; set; }

	public bool bRemoveOnTrigger { get; set; }

	public string strRunEncounter { get; set; }

	public bool bRunEncounterInterrupt { get; set; }

	public string strQueueInteraction { get; set; }

	public bool bQueueInteractionInsert { get; set; }

	public string strApplyInteractionChain { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
