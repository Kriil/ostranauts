using System;

public class CondRuleThresh
{
	public string strLootNew { get; set; }

	public float fMinAdd { get; set; }

	public float fMaxAdd { get; set; }

	public float fMin { get; set; }

	public float fMax { get; set; }

	public CondRuleThresh Clone()
	{
		return (CondRuleThresh)base.MemberwiseClone();
	}
}
