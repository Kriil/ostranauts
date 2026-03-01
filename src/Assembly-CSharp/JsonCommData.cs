using System;

[Serializable]
public class JsonCommData
{
	public JsonShipMessage[] aMessages { get; set; }

	public double dClearanceRequestTime { get; set; }

	public string strClearanceTargetRegId { get; set; }

	public double dClearanceIssueTimestamp { get; set; }

	public string strClearanceDockID { get; set; }

	public string strClearanceSquak { get; set; }

	public string strClearanceType { get; set; }

	public bool bClearanceSquawkID { get; set; }

	public JsonCommData Clone()
	{
		JsonCommData jsonCommData = new JsonCommData();
		jsonCommData.dClearanceRequestTime = this.dClearanceRequestTime;
		jsonCommData.strClearanceTargetRegId = this.strClearanceTargetRegId;
		jsonCommData.dClearanceIssueTimestamp = this.dClearanceIssueTimestamp;
		jsonCommData.strClearanceDockID = this.strClearanceDockID;
		jsonCommData.strClearanceSquak = this.strClearanceSquak;
		jsonCommData.strClearanceType = this.strClearanceType;
		jsonCommData.bClearanceSquawkID = this.bClearanceSquawkID;
		if (this.aMessages != null)
		{
			jsonCommData.aMessages = (JsonShipMessage[])this.aMessages.Clone();
		}
		return jsonCommData;
	}
}
