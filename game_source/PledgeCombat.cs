using System;
using System.Collections.Generic;

public class PledgeCombat : Pledge2
{
	public PledgeCombat()
	{
		if (PledgeCombat.dictThreats == null)
		{
			PledgeCombat.dictThreats = new Dictionary<string, CombatThreats>();
		}
		if (PledgeCombat._ctNeutralized == null)
		{
			PledgeCombat._ctNeutralized = DataHandler.GetCondTrigger("TIsCombatNeutralized");
		}
		if (PledgeCombat._ctDistracted == null)
		{
			PledgeCombat._ctDistracted = DataHandler.GetCondTrigger("TIsCombatDistracted");
		}
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
		bool flag = false;
		if (CrewSim.GetSelectedCrew() == base.Us)
		{
			flag = true;
		}
		else if (PledgeCombat._ctNeutralized.Triggered(base.Us, null, true))
		{
			flag = true;
		}
		else if (this.Them == null || this.Them.ship == null || this.Them.ship.LoadState <= Ship.Loaded.Shallow || PledgeCombat._ctNeutralized.Triggered(this.Them, null, true))
		{
			flag = true;
		}
		if (flag)
		{
			base.Us.RemovePledge(this);
			return false;
		}
		CombatThreats combatThreats = null;
		if (!PledgeCombat.dictThreats.TryGetValue(base.Us.strID, out combatThreats))
		{
			combatThreats = new CombatThreats();
			PledgeCombat.dictThreats[base.Us.strID] = combatThreats;
		}
		if (StarSystem.fEpoch > (double)combatThreats.fLastAssess)
		{
			this.ThreatAssess(combatThreats);
		}
		if (combatThreats.coThreatBest == null || combatThreats.coThreatBest != this.Them)
		{
			return false;
		}
		Interaction move = this.GetMove(combatThreats);
		return move != null && move.objUs.QueueInteraction(move.objThem, move, false);
	}

	private void EquipBestWeapon()
	{
	}

	private void ThreatAssess(CombatThreats threats)
	{
		if (threats == null)
		{
			return;
		}
		this.EquipBestWeapon();
		bool flag = base.Us.HasCond("IsIntelligentBeing");
		threats.aEnemies = base.Us.ship.GetPeople(true);
		threats.aFriends = new List<CondOwner>();
		threats.aEnemies.Remove(base.Us);
		float num = MathUtils.Rand(-10f, 10f, MathUtils.RandType.Mid, null);
		threats.m_fMoraleSituHidden = base.Us.GetCondAmount("StatSecurity") + (double)num;
		threats.fBestThreatAdj = 0.0;
		threats.coThreatBest = null;
		threats.coThreatDefault = null;
		threats.coFirstFriend = null;
		threats.bLeaderPassive = false;
		threats.bLeaderActive = false;
		for (int i = threats.aEnemies.Count - 1; i >= 0; i--)
		{
			CondOwner condOwner = threats.aEnemies[i];
			if (PledgeCombat._ctNeutralized.Triggered(condOwner, null, true) || !Visibility.IsCondOwnerLOSVisibleBlocks(base.Us, condOwner.tfVector2Position, false, true))
			{
				threats.aEnemies.Remove(condOwner);
			}
			else
			{
				bool flag2 = condOwner.SharesFactionsWith(base.Us);
				float factionScore = base.Us.GetFactionScore(condOwner.GetAllFactions());
				if (threats.coThreatDefault == null)
				{
					threats.coThreatDefault = condOwner;
				}
				bool flag3 = base.Us.HasPledge(this.jp, condOwner.strID);
				if (!flag3 && JsonFaction.GetReputation(factionScore) == JsonFaction.Reputation.Likes)
				{
					if (flag2)
					{
						threats.aEnemies.Remove(condOwner);
						if (!condOwner.HasCond("Unconscious"))
						{
							if (!threats.aFriends.Contains(condOwner))
							{
								threats.aFriends.Add(condOwner);
							}
							threats.m_fMoraleSituHidden -= 50.0;
							if (condOwner.GetCondAmount("StatEsteem") < base.Us.GetCondAmount("StatEsteem"))
							{
								if (condOwner.HasCond("IsCombatPassive"))
								{
									threats.bLeaderPassive = true;
									threats.coFirstFriend = condOwner;
								}
								else
								{
									threats.bLeaderActive = true;
								}
							}
						}
					}
					else
					{
						threats.m_fMoraleSituHidden -= 10.0;
						threats.aEnemies.Remove(condOwner);
						if (threats.coFirstFriend == null)
						{
							threats.coFirstFriend = condOwner;
						}
					}
				}
				else
				{
					threats.coThreatDefault = condOwner;
					if (flag3)
					{
						double num2 = condOwner.GetCondAmount("StatThreat") - 105.0;
						if (flag)
						{
							num2 += condOwner.GetCondAmount("StatThreatIntelligent") - 105.0;
						}
						int num3 = TileUtils.TileRange(condOwner.GetPos(null, false), base.Us.GetPos(null, false));
						double num4;
						if (!condOwner.HasCond("IsWieldingRanged") && num3 > 3)
						{
							num4 = num2 / (double)num3;
						}
						else
						{
							num4 = num2;
						}
						Interaction interactionCurrent = condOwner.GetInteractionCurrent();
						if (interactionCurrent == null || interactionCurrent.objThem != base.Us || interactionCurrent.strActionGroup != "Fight")
						{
							num4 -= 50.0;
						}
						if (threats.coThreatBest == null)
						{
							threats.coThreatBest = condOwner;
							threats.fBestThreatAdj = num4;
						}
						else if (num4 > threats.fBestThreatAdj)
						{
							threats.coThreatBest = condOwner;
							threats.fBestThreatAdj = num4;
						}
					}
				}
			}
		}
	}

	private Interaction GetMove(CombatThreats threats)
	{
		if (threats == null)
		{
			return null;
		}
		Interaction interaction = null;
		base.Us.ZeroCondAmount("IsCombatActive");
		base.Us.ZeroCondAmount("IsCombatPassive");
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		bool flag6 = false;
		if (threats.coThreatBest == null)
		{
			threats.coThreatBest = threats.coThreatDefault;
		}
		base.Us.SetCondAmount("StatFightFear", 0.0, 0.0);
		int num = -1;
		int num2 = 0;
		if (threats.bLeaderPassive || (threats.aEnemies.Count == 0 && !threats.bLeaderActive))
		{
			if (threats.aFriends.Count > 0)
			{
				flag3 = true;
				threats.coThreatBest = threats.coFirstFriend;
			}
			else
			{
				flag = true;
				threats.coThreatBest = threats.coThreatDefault;
			}
		}
		else if (threats.aEnemies.Contains(threats.coThreatBest))
		{
			CondRule condRule = base.Us.GetCondRule("StatSecurity");
			if (condRule != null)
			{
				num2 = condRule.aThresholds.Length;
				CondRuleThresh currentThresh = condRule.GetCurrentThresh(base.Us, threats.m_fMoraleSituHidden + threats.fBestThreatAdj);
				num = Array.IndexOf<CondRuleThresh>(condRule.aThresholds, currentThresh);
			}
			if (num == num2 - 1)
			{
				flag = true;
			}
			else
			{
				flag2 = true;
			}
			if (base.Us.IsRobot)
			{
				flag = false;
				flag2 = true;
				num = 0;
				num2 = 5;
			}
			base.Us.SetCondAmount("StatFightFear", threats.m_fMoraleSituHidden + threats.fBestThreatAdj, 0.0);
		}
		else
		{
			flag3 = true;
		}
		if (flag)
		{
			int num3 = TileUtils.TileRange(threats.coThreatBest.GetPos(null, false), base.Us.GetPos(null, false));
			if (num3 <= 3)
			{
				if (MathUtils.Rand(0.0, 1.0, MathUtils.RandType.Flat, null) < 0.5 || PledgeCombat._ctDistracted.Triggered(threats.coThreatBest, null, true))
				{
					flag6 = true;
				}
				else
				{
					flag5 = true;
				}
			}
			else
			{
				flag6 = true;
			}
		}
		else if (flag2)
		{
			if ((float)num <= (float)(num2 - 1) * 0.6f)
			{
				flag4 = true;
			}
			else
			{
				flag5 = true;
			}
		}
		if (flag3)
		{
			interaction = this.GetReply(DataHandler.GetInteraction("PLGCombatPassive", null, false));
			if (interaction != null)
			{
				base.Us.SetCondAmount("IsCombatPassive", 1.0, 0.0);
			}
			else
			{
				flag3 = false;
				flag6 = true;
			}
		}
		if (!flag3)
		{
			base.Us.SetCondAmount("IsCombatActive", 1.0, 0.0);
			if (flag5)
			{
				interaction = this.GetReply(DataHandler.GetInteraction("PLGCombatPosition", null, false));
			}
			else if (flag6)
			{
				interaction = this.GetReply(DataHandler.GetInteraction("PLGCombatFlee", null, false));
			}
			else if (flag4)
			{
				interaction = ((!base.Us.IsRobot) ? DataHandler.GetInteraction("PLGCombatFight", null, false) : DataHandler.GetInteraction("PLGCombatRobotFight", null, false));
				if (interaction != null)
				{
					List<string> list = new List<string>(interaction.aInverse);
					foreach (string str in base.Us.aAttackIAs)
					{
						list.Insert(0, str + ",[us],[them]");
					}
					interaction.aInverse = list.ToArray();
				}
				interaction = this.GetReply(interaction);
			}
		}
		return interaction;
	}

	private Interaction GetReply(Interaction iaBest)
	{
		if (iaBest == null)
		{
			return null;
		}
		iaBest.objUs = base.Us;
		iaBest.objThem = this.Them;
		iaBest = iaBest.GetReply();
		return iaBest;
	}

	private static CondTrigger _ctNeutralized;

	private static CondTrigger _ctDistracted;

	private static Dictionary<string, CombatThreats> dictThreats;

	private const double THREAT_NEUTRAL_AMOUNT = 105.0;

	private const int MELEE_RANGE = 3;
}
