using System;
using System.Collections.Generic;
using Ostranauts.Ships.Commands;

namespace Ostranauts.Ships.AIPilots.Interfaces
{
	public interface IAICharacter
	{
		Ship ShipUs { get; }

		List<ICommand> Commands { get; set; }

		TargetData GetTarget();

		AIType AIType { get; }

		double MaxSpeed(bool? inAtmo = null);

		ICommand FFWD(ICommand lastActiveCommand);
	}
}
