using System;
using System.Collections.Generic;
using Ostranauts.Condowner;

[Serializable]
public class JsonAIPersonality
{
	public string strName { get; set; }

	public Dictionary<string, CondHistory> mapIAHist2 { get; set; }
}
