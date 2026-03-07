using System;
using Ostranauts.Ships.AIPilots.Interfaces;

namespace Ostranauts.Ships.Commands
{
	public class Shoot : BaseCommand
	{
		public Shoot(IAICharacter ai)
		{
			this.fTimeLastUpdate = StarSystem.fEpoch;
			base.ShipUs = ai.ShipUs;
		}

		public override string DescriptionFriendly
		{
			get
			{
				return "Shooting at " + ((base.ShipUs.shipScanTarget == null) ? "target" : base.ShipUs.shipScanTarget.strRegID);
			}
		}

		public override CommandCode RunCommand()
		{
			if (StarSystem.fEpoch - this.fTimeLastUpdate < 3.0)
			{
				return CommandCode.Ongoing;
			}
			JsonAttackMode attackMode = DataHandler.GetAttackMode("AModeMicrometeoroid");
			bool bAudio = Wound.bAudio;
			Wound.bAudio = true;
			if (attackMode != null)
			{
				CrewSim.coPlayer.ship.DamageRayRandom(attackMode, 1f, null, false);
			}
			Wound.bAudio = bAudio;
			Loot loot = DataHandler.GetLoot("TXTRandomBangAudio");
			if (loot != null)
			{
				CrewSim.objInstance.CamShake(0.2f);
				AudioManager.am.PlayAudioEmitter(loot.GetLootNameSingle(null), false, false);
			}
			if (this._shotCount <= 0)
			{
				return CommandCode.Finished;
			}
			this.fTimeLastUpdate = StarSystem.fEpoch;
			this._shotCount--;
			return CommandCode.Ongoing;
		}

		private double fTimeLastUpdate;

		private int _shotCount = 2;
	}
}
