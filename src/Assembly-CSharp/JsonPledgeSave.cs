using System;

[Serializable]
// Live pledge save payload.
// Stores which two CondOwners are linked by a `JsonPledge` template.
public class JsonPledgeSave
{
	// `strName` is the pledge template id; `strUsID` / `strThemID` are live CondOwner ids.
	public string strName { get; set; }

	public string strUsID { get; set; }

	public string strThemID { get; set; }
}
