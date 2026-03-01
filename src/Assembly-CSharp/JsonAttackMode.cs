using System;

public class JsonAttackMode
{
	public string strName { get; set; }

	public string strNameFriendly { get; set; }

	public string strType { get; set; }

	public string strAudioAttack { get; set; }

	public float fRange { get; set; }

	public float fDmgCut { get; set; }

	public float fDmgBlunt { get; set; }

	public float fDmgEnv { get; set; }

	public float fPenetration { get; set; }

	public float fTargetDefenseMod { get; set; }

	public float fSpread { get; set; }

	public int nExtraRays { get; set; }

	public bool bPlayAudioEarly { get; set; }

	public bool bAllowOnInanimate { get; set; }

	public JsonAttackMode.Type GetAttackType()
	{
		if (this.strType == "ranged")
		{
			return JsonAttackMode.Type.ranged;
		}
		return JsonAttackMode.Type.melee;
	}

	public double GetDmgAmount(CondOwner co)
	{
		double max = 1.0;
		if (co != null && this.GetAttackType() == JsonAttackMode.Type.melee)
		{
			max = co.GetCondAmount("StatAttDmgMult");
		}
		return MathUtils.Rand(0.0, max, MathUtils.RandType.Mid, null);
	}

	public enum Type
	{
		melee,
		ranged
	}
}
