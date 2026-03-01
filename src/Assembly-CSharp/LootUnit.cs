using System;
using UnityEngine;

public class LootUnit
{
	public float GetAmount(float fBaseChance, float fRand)
	{
		if (fRand > fBaseChance + this.fChance)
		{
			return 0f;
		}
		float num = UnityEngine.Random.Range(this.fMin, this.fMax);
		if (!this.bPositive)
		{
			num = -num;
		}
		return num;
	}

	public string strName;

	public float fChance;

	public float fMin;

	public float fMax;

	public bool bPositive = true;
}
