using System;

public class JsonTip
{
	public string strName { get; set; }

	public string strCategory { get; set; }

	public string strBody { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
