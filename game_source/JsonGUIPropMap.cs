using System;

[Serializable]
public class JsonGUIPropMap
{
	public string strName { get; set; }

	public string[] dictGUIPropMap { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
