using System;
using Ostranauts.Core;
using Ostranauts.Racing;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class RaceComms : BaseCommand
	{
		public RaceComms(IAICharacter pilot)
		{
			base.ShipUs = pilot.ShipUs;
		}

		public override string[] SaveData
		{
			set
			{
				if (value == null || value.Length == 0)
				{
					return;
				}
				this._payload = value;
			}
		}

		public override CommandCode RunCommand()
		{
			if (this._payload == null)
			{
				return CommandCode.Cancelled;
			}
			MonoSingleton<RacingLeagueManager>.Instance.ReceiveMessage(base.ShipUs, this._payload);
			return CommandCode.Finished;
		}

		private string[] _payload;
	}
}
