using System;
using System.Collections.Generic;

public class InfoLookup
{
	public int Count
	{
		get
		{
			return this.infoNodes.Count;
		}
	}

	public static InfoLookup GetSeries(string s)
	{
		InfoLookup result = null;
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}
		if (Info.articleLookups.TryGetValue(s, out result))
		{
			return result;
		}
		return result;
	}

	public static void Add(InfoNode infoNode)
	{
		if (string.IsNullOrEmpty(infoNode.strLookup))
		{
			return;
		}
		InfoLookup series = InfoLookup.GetSeries(infoNode.strLookup);
		if (series != null)
		{
			series.infoNodes.Add(infoNode);
		}
		else
		{
			InfoLookup infoLookup = new InfoLookup
			{
				strLookup = infoNode.strLookup
			};
			infoLookup.infoNodes.Add(infoNode);
			Info.articleLookups.Add(infoNode.strLookup, infoLookup);
		}
	}

	public string strLookup;

	public List<InfoNode> infoNodes = new List<InfoNode>();
}
