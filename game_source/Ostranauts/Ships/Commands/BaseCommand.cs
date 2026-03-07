using System;

namespace Ostranauts.Ships.Commands
{
	public class BaseCommand : ICommand
	{
		public Ship ShipUs
		{
			get
			{
				if (this._shipUs != null && this._shipUs.bDestroyed)
				{
					this._shipUs = CrewSim.system.GetShipByRegID(this._shipUs.strRegID);
				}
				return this._shipUs;
			}
			protected set
			{
				this._shipUs = value;
			}
		}

		public virtual string[] SaveData { get; set; }

		public virtual string DescriptionFriendly { get; protected set; }

		public virtual CommandCode RunCommand()
		{
			return CommandCode.Cancelled;
		}

		public virtual CommandCode ResolveInstantly()
		{
			return CommandCode.Cancelled;
		}

		protected float GetHandOffDistance(float fCollisionDistance)
		{
			return fCollisionDistance * 3f;
		}

		protected void PlaceWithinDockingRange(ShipSitu target)
		{
			if (this.ShipUs == null || target == null)
			{
				return;
			}
			target.UpdateTime(StarSystem.fEpoch, true);
			this.ShipUs.objSS.UpdateTime(StarSystem.fEpoch, true);
			this.ShipUs.Maneuver(0f, 0f, 0f, 0, 1E-10f, Ship.EngineMode.RCS);
			this.ShipUs.objSS.vVelX = target.vVelX;
			this.ShipUs.objSS.vVelY = target.vVelY;
			this.ShipUs.objSS.PlaceOrbitPosition(target);
		}

		private Ship _shipUs;
	}
}
