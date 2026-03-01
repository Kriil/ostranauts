using System;
using System.Collections.Generic;

public class SocialStats
{
	public SocialStats(string strName)
	{
		this.strName = strName;
	}

	public override string ToString()
	{
		string text = string.Empty;
		text += this.strName;
		text += "\t";
		text += this.nUsed;
		text += "\t";
		text += this.nLowScored;
		text += "\t";
		text += this.nMissingItem;
		text += "\t";
		text += this.nChance;
		text += "\t";
		text += this.nChecked;
		text += "\t";
		text += this.fForbids;
		text += "\t";
		text += this.fReqs;
		text += "\t\"";
		foreach (string text2 in this.dictForbids.Keys)
		{
			string text3 = text;
			text = string.Concat(new object[]
			{
				text3,
				text2,
				"   ",
				this.dictForbids[text2],
				"\n"
			});
		}
		text += "\"\t\"";
		foreach (string text4 in this.dictReqs.Keys)
		{
			string text3 = text;
			text = string.Concat(new object[]
			{
				text3,
				text4,
				"   ",
				this.dictReqs[text4],
				"\n"
			});
		}
		text += "\"";
		return text;
	}

	public string strName;

	public int nUsed;

	public int nLowScored;

	public int nMissingItem;

	public int nChance;

	public int nChecked;

	public float fForbids;

	public float fReqs;

	public Dictionary<string, float> dictForbids = new Dictionary<string, float>();

	public Dictionary<string, float> dictReqs = new Dictionary<string, float>();
}
