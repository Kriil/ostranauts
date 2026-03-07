using System;

[Serializable]
// Serialized objective/job tracker entry.
// Likely used for tutorial beats, plot tasks, and contract-style goals that
// point at CondOwners plus condition trigger ids.
public class JsonObjective
{
	// Objective source CondOwner definition id and instance id.
	public string objectiveCOStrName { get; set; }

	public string objectiveCOStrID { get; set; }

	// Ship or focused CondOwner ids used when the objective targets a specific actor.
	public string shipCOID { get; set; }

	public string strCOFocusID { get; set; }

	// `objectiveCTStrName` and focus variants likely point to `data/condtrigs` ids.
	public string objectiveCTStrName { get; set; }

	public string objectiveCTFocusStrName { get; set; }

	// Display strings are likely tooltip/log text already resolved for UI.
	public string strDisplayName { get; set; }

	public string strDisplayDesc { get; set; }

	public string strDisplayDescComplete { get; set; }

	public string strPlotName { get; set; }

	public string strTutorialBeat { get; set; }

	// Start time and state flags for objective tracking and UI presentation.
	public float fTimeStart { get; set; }

	public bool bNew { get; set; }

	public bool bTutorial { get; set; }

	public bool stackable { get; set; }

	public bool bFinished { get; set; }
}
