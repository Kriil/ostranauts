using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class ComeHere : BaseCommand
	{
		public ComeHere(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
			this._commands = new List<ICommand>
			{
				new Undock(pilot),
				new TargetPlayer(pilot),
				new FlyTo(pilot),
				new Hold(pilot),
				new TargetATC(pilot)
			};
			this._activeCommand = this._commands.First<ICommand>();
		}

		public override string DescriptionFriendly
		{
			get
			{
				return (this._activeCommand == null) ? string.Empty : this._activeCommand.DescriptionFriendly;
			}
		}

		public override CommandCode RunCommand()
		{
			CommandCode commandCode = this._activeCommand.RunCommand();
			if ((CommandCode.ResultDone & commandCode) != commandCode)
			{
				return CommandCode.Ongoing;
			}
			if (this._commands.IndexOf(this._activeCommand) == this._commands.Count - 1)
			{
				base.ShipUs.shipScanTarget = null;
				return CommandCode.Finished;
			}
			this.NextCommand();
			return CommandCode.Ongoing;
		}

		private void NextCommand()
		{
			int num = this._commands.IndexOf(this._activeCommand);
			if (num == -1 || num == this._commands.Count - 1)
			{
				this._activeCommand = this._commands.FirstOrDefault<ICommand>();
			}
			if (num + 1 < this._commands.Count)
			{
				this._activeCommand = this._commands[num + 1];
			}
		}

		private List<ICommand> _commands;

		private ICommand _activeCommand;
	}
}
