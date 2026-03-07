using System;
using System.Collections.Generic;
using Ostranauts.Pledges;

public static class PledgeFactory
{
	public static Pledge2 Factory(CondOwner coUs, JsonPledge jp, CondOwner coThem = null)
	{
		if (coUs == null || jp == null || !jp.Valid())
		{
			return null;
		}
		jp.strType = jp.strType.ToLower();
		if (PledgeFactory.dictTypes.ContainsKey(jp.strType))
		{
			Pledge2 pledge = (Pledge2)Activator.CreateInstance(PledgeFactory.dictTypes[jp.strType]);
			if (pledge.Init(coUs, jp, coThem))
			{
				return pledge;
			}
		}
		return null;
	}

	public static Pledge2 Factory(JsonPledgeSave jps)
	{
		if (jps == null)
		{
			return null;
		}
		JsonPledge pledge = DataHandler.GetPledge(jps.strName);
		if (pledge == null)
		{
			return null;
		}
		pledge.strType = pledge.strType.ToLower();
		if (PledgeFactory.dictTypes.ContainsKey(pledge.strType))
		{
			Pledge2 pledge2 = (Pledge2)Activator.CreateInstance(PledgeFactory.dictTypes[pledge.strType]);
			if (pledge2.Init(jps.strUsID, pledge, jps.strThemID))
			{
				return pledge2;
			}
		}
		return null;
	}

	private static Dictionary<string, Type> dictTypes = new Dictionary<string, Type>
	{
		{
			"disembark",
			typeof(PledgeDisembark)
		},
		{
			"embark",
			typeof(PledgeEmbark)
		},
		{
			"follow",
			typeof(PledgeFollow)
		},
		{
			"repeat",
			typeof(PledgeRepeat)
		},
		{
			"surviveo2",
			typeof(PledgeSurviveO2)
		},
		{
			"surviveco2",
			typeof(PledgeSurviveCO2)
		},
		{
			"wearsuit",
			typeof(PledgeWearSuit)
		},
		{
			"eat",
			typeof(PledgeEat)
		},
		{
			"drink",
			typeof(PledgeDrink)
		},
		{
			"reply",
			typeof(PledgeReply)
		},
		{
			"combat",
			typeof(PledgeCombat)
		},
		{
			"faction",
			typeof(PledgeFaction)
		},
		{
			"crime",
			typeof(PledgeCrime)
		},
		{
			"patrol",
			typeof(PledgePatrol)
		}
	};
}
