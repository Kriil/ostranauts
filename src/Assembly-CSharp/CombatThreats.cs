using System;
using System.Collections.Generic;

public class CombatThreats
{
	public float fLastAssess;

	public CondOwner coThreatBest;

	public List<CondOwner> aEnemies;

	public List<CondOwner> aFriends;

	public double m_fMoraleSituHidden;

	public double fBestThreatAdj;

	public CondOwner coThreatDefault;

	public CondOwner coFirstFriend;

	public bool bLeaderPassive;

	public bool bLeaderActive;
}
