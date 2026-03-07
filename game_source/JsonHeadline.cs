using System;

public class JsonHeadline
{
	public string strName { get; set; }

	public string strDesc { get; set; }

	public string strRegion { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
