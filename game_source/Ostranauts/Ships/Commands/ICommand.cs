using System;

namespace Ostranauts.Ships.Commands
{
	public interface ICommand
	{
		Ship ShipUs { get; }

		string[] SaveData { get; set; }

		string DescriptionFriendly { get; }

		CommandCode RunCommand();

		CommandCode ResolveInstantly();
	}
}
