using System;
using System.Collections.Generic;

public static class FluidStrings
{
	static FluidStrings()
	{
		foreach (string str in FluidStrings.moleculeNames)
		{
			FluidStrings.mol.Add(FluidStrings.molConcat + str);
			FluidStrings.pps.Add(FluidStrings.partialPressureConcat + str);
		}
	}

	public static readonly List<string> moleculeNames = new List<string>
	{
		"CH4",
		"CO2",
		"H2",
		"H2O",
		"H2SO4",
		"He2",
		"N2",
		"NH3",
		"O2"
	};

	public static readonly string molConcat = "StatGasMol";

	public static readonly string partialPressureConcat = "StatGasPp";

	public static readonly string kpaConcat = "kPa";

	public static readonly List<string> pps = new List<string>();

	public static readonly List<string> mol = new List<string>();
}
