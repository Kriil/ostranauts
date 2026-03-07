using System;

public class JsonPlot
{
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public bool bNoCheck { get; set; }

	public string strLootNextPlot { get; set; }

	public string strCancelBeat { get; set; }

	public string[][] aPhases { get; set; }

	public string[] aPhaseTitles { get; set; }

	public string[][] aIADos { get; set; }

	public string FriendlyName
	{
		get
		{
			return (this.strNameFriendly == null) ? this.strName : this.strNameFriendly;
		}
	}

	public override string ToString()
	{
		return this.strName;
	}
}
