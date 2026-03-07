using System;
using System.Collections.Generic;

namespace Ostranauts.Pledges
{
	public class PledgeSurviveCO2 : Pledge2
	{
		public PledgeSurviveCO2()
		{
			if (PledgeSurviveCO2.ctSuffocating == null)
			{
				PledgeSurviveCO2.ctSuffocating = DataHandler.GetCondTrigger("TIsSuffocatingCO2");
			}
		}

		public override bool Init(CondOwner coUs, JsonPledge jpIn, CondOwner coThem = null)
		{
			return base.Init(coUs, jpIn, coUs);
		}

		public override bool Init(string strUs, JsonPledge jpIn, string strThem = null)
		{
			return base.Init(strUs, jpIn, strUs);
		}

		public override bool IsEmergency()
		{
			return !(base.Us == null) && !base.Us.HasCond("IsAIManual") && base.IsEmergency();
		}

		public override bool Do()
		{
			if (base.Us == null || base.Us.HasCond("IsAIManual"))
			{
				return false;
			}
			if (this.Finished())
			{
				return true;
			}
			if (!base.Triggered())
			{
				return false;
			}
			if (base.Us.ship == null)
			{
				return false;
			}
			Room roomAtWorldCoords = base.Us.ship.GetRoomAtWorldCoords1(base.Us.tf.position, false);
			bool flag = base.Us.HasCond("IsAirtight");
			bool flag2 = false;
			if (roomAtWorldCoords != null && !roomAtWorldCoords.Void)
			{
				flag2 = (PledgeSurviveCO2.CoHasSafeCO2Lvl(roomAtWorldCoords.CO) && PledgeSurviveO2.CoHasO2(roomAtWorldCoords.CO));
			}
			bool flag3 = flag && PledgeSurviveCO2.CoHasSafeCO2Lvl(base.Us) && PledgeSurviveO2.CoHasO2(base.Us);
			bool flag4 = PledgeSurviveCO2.ctSuffocating.Triggered(base.Us, null, true) || (!flag && !flag2) || (flag && !flag3);
			if (!flag4)
			{
				base.Us.ZeroCondAmount("AttemptingPledgeSurviveCO2");
				return false;
			}
			base.Us.AddCondAmount("AttemptingPledgeSurviveCO2", 1.0, 0.0, 0f);
			Slots component = base.Us.GetComponent<Slots>();
			Slot slot = component.GetSlot("head_out");
			if (!flag)
			{
				if (flag2)
				{
					return false;
				}
			}
			else
			{
				if (flag3)
				{
					return false;
				}
				if (flag2)
				{
					CondTrigger condTrigger = DataHandler.GetCondTrigger("TIsHelmet");
					foreach (CondOwner condOwner in slot.aCOs)
					{
						if (!(condOwner == null))
						{
							if (condTrigger.Triggered(condOwner, null, true))
							{
								JsonPledge pledge = DataHandler.GetPledge("AIEquipSpaceSuitAndHelmet");
								Pledge2 pledge2 = PledgeFactory.Factory(base.Us, pledge, null);
								if (base.Us.HasPledge(pledge2))
								{
									base.Us.RemovePledge(pledge2);
								}
								Interaction interaction = DataHandler.GetInteraction("EquipHelmetEvenIfSlotted", null, false);
								base.Us.QueueInteraction(condOwner, interaction, true);
								CrewSim.objInstance.workManager.IdleRemove(base.Us);
								return true;
							}
						}
					}
				}
			}
			Pathfinder pathfinder = base.Us.Pathfinder;
			if (pathfinder == null)
			{
				return false;
			}
			base.Us.AddCondAmount("IsEmergencyOverride", 1.0, 0.0, 0f);
			List<Ship> allDockedShips = base.Us.ship.GetAllDockedShips();
			allDockedShips.Insert(0, base.Us.ship);
			foreach (Ship ship in allDockedShips)
			{
				Tile randomAtmoTile = ship.GetRandomAtmoTile(true);
				if (randomAtmoTile != null && base.Us.AIIssueOrder(null, null, false, randomAtmoTile, randomAtmoTile.tf.position.x, randomAtmoTile.tf.position.y))
				{
					CrewSim.objInstance.workManager.IdleRemove(base.Us);
					return true;
				}
			}
			JsonPledge pledge3 = DataHandler.GetPledge("AIEquipSpaceSuitAndHelmet");
			Pledge2 pledge4 = PledgeFactory.Factory(base.Us, pledge3, null);
			base.Us.AddPledge(pledge4);
			CrewSim.objInstance.workManager.IdleRemove(base.Us);
			return true;
		}

		public static bool CoHasSafeCO2Lvl(CondOwner co)
		{
			return co != null && co.GetCondAmount("StatGasPpCO2") < 1.5;
		}

		private static CondTrigger ctSuffocating;

		private static CondTrigger ctNotCarried;

		private static CondTrigger _ctHelmet;
	}
}
