using System;

public class JsonTransitConnection
{
	public string strName { get; set; }

	public string strLabelNameOptional { get; set; }

	public string ctUserOptional { get; set; }

	public string strTargetRegID { get; set; }

	public string ctKioskOrigin { get; set; }

	public string ctKioskDestination { get; set; }

	public bool bHide { get; set; }

	public bool TargetsWildCard
	{
		get
		{
			return this.strTargetRegID.Contains("|");
		}
	}

	public bool IsValidUser(CondOwner coPlayer)
	{
		if (coPlayer == null || string.IsNullOrEmpty(this.ctUserOptional))
		{
			return true;
		}
		CondTrigger condTrigger = DataHandler.GetCondTrigger(this.ctUserOptional);
		return condTrigger.Triggered(coPlayer, null, true);
	}
}
