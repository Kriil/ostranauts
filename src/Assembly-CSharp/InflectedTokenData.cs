using System;

public struct InflectedTokenData
{
	public GrammarUtils.ReplacementType replacementType;

	public GrammarUtils.GrammarLUTIndex LUTIndex;

	public GrammarUtils.VerbForm verbForm;

	public GrammarUtils.UsThem3rd usThem3Rd;

	public GrammarUtils.ReplacementOther replacementOther;

	public int start;

	public int end;

	public string[] verbForms;
}
