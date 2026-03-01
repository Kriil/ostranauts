using System;

public class JsonDCOCollection
{
	public string strName { get; set; }

	public string strFriendlyName { get; set; }

	public string[] aReqPodTypes { get; set; }

	public string[] aIncludeCOs { get; set; }

	public string[] aCondReqs { get; set; }

	public string[] aCondForbids { get; set; }

	public bool bAND { get; set; }
}
