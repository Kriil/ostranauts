using System;
using System.Collections.Generic;

public class JsonPlotManagerSettings
{
	public string strName { get; set; }

	public Dictionary<string, float> dictEventChances { get; set; }

	public double fTensionPeriod { get; set; }

	public double fReleasePeriod { get; set; }

	public double fSocialPeriod { get; set; }

	public double fAutosavePeriod { get; set; }
}
