using System;

// Contract/job definition used by gig boards or mission generation.
// This appears to tie a client person-spec to a chain of interaction ids for
// setup, abandonment, completion, and the actual work action.
public class JsonJob
{
	// `strName` is the internal job id.
	public string strName { get; set; }

	// Likely references a `JsonPersonSpec` id used to generate or match the client.
	public string strPSpecClient { get; set; }

	public string strIASetupClient { get; set; }

	public string strIASetupPlayer { get; set; }

	public string strIAAbandonClient { get; set; }

	public string strIAAbandonPlayer { get; set; }

	public string strIAFinishClient { get; set; }

	public string strIAFinishPlayer { get; set; }

	public string strIADo { get; set; }

	// Loot ids for briefing text, cargo/items, and registration ids tied to route endpoints.
	public string strLootTxt1 { get; set; }

	public string strLootJobItems { get; set; }

	public string strLootRegIDsOrigin { get; set; }

	public string strLootRegIDsDest { get; set; }

	public double fDuration { get; set; }

	public double fContractMin { get; set; }

	public double fContractMax { get; set; }

	public double fPayoutMin { get; set; }

	public double fPayoutMax { get; set; }
}
