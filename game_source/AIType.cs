using System;

[Flags]
public enum AIType
{
	NA = 0,
	Police = 1,
	Scav = 2,
	HaulerDeployer = 4,
	HaulerRetriever = 8,
	Station = 16,
	Pirate = 32,
	HaulerCargo = 64,
	Auto = 128,
	NonPriorityShips = 110,
	PriorityShips = 129,
	AllShips = 239,
	All = 255
}
