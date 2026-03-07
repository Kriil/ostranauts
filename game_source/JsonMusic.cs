using System;

public class JsonMusic
{
	public string strName { get; set; }

	public string[] strTags { get; set; }

	public bool bLoop { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
