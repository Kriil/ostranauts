using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.aChecks = new List<DestCheck>();
		this.co = base.GetComponent<CondOwner>();
	}

	public void UpdateManual()
	{
		if (!this.bActive && this.aChecks.Count > 0)
		{
			this.bActive = true;
		}
		if (!this.bNeedsCheck || StarSystem.fEpoch < this.fTimeOfNextSignalCheck)
		{
			return;
		}
		this.DamageCheck();
	}

	public void CatchUp()
	{
	}

	public void ScheduleDamageCheck()
	{
		if (base.gameObject == null || !base.gameObject.activeInHierarchy)
		{
			return;
		}
		base.StartCoroutine(this.DelayedDamageCheck());
	}

	private IEnumerator DelayedDamageCheck()
	{
		yield return null;
		this.DamageCheck();
		yield break;
	}

	public void DamageCheck()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		this.bActive = false;
		this.bNeedsCheck = false;
		if (this.co.ship == null)
		{
			return;
		}
		bool flag = false;
		foreach (DestCheck destCheck in this.aChecks)
		{
			if (destCheck.DamageCheck(this.co) && !flag)
			{
				flag = true;
			}
		}
		if (flag && this.co.Item != null)
		{
			this.co.Item.VisualizeOverlays(false);
		}
		this.bActive = (this.aChecks.Count > 0);
		float realtimeSinceStartup2 = Time.realtimeSinceStartup;
		if (this.bDebug)
		{
			Destructable.bOutput = true;
			Destructable.debugTimesRunThisFrame++;
			Destructable.debugRunTimems += realtimeSinceStartup2 - realtimeSinceStartup;
		}
	}

	public static void LateUpdateDebug()
	{
		if (Destructable.bOutput)
		{
			Destructable.bOutput = false;
			Debug.Log(string.Concat(new object[]
			{
				"Run Destructable.DamageCheck() ",
				Destructable.debugTimesRunThisFrame,
				" times this frame for a total of ",
				Destructable.debugRunTimems * 1000f,
				" ms"
			}));
			Destructable.debugTimesRunThisFrame = 0;
			Destructable.debugRunTimems = 0f;
		}
	}

	public void SetData(string[] aStrings)
	{
		bool flag = true;
		foreach (DestCheck destCheck in this.aChecks)
		{
			if (destCheck.strDamageCond == aStrings[1])
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			DestCheck destCheck2 = new DestCheck();
			if (destCheck2.SetData(aStrings, this.co))
			{
				this.aChecks.Add(destCheck2);
			}
		}
		this.bActive = (this.aChecks.Count > 0);
	}

	public void SwapDmgLoot(string strCond, string strLoot)
	{
		if (string.IsNullOrEmpty(strCond) || string.IsNullOrEmpty(strLoot))
		{
			return;
		}
		foreach (DestCheck destCheck in this.aChecks)
		{
			if (destCheck.strDamageCond == strCond)
			{
				destCheck.strLootModeSwitch = strLoot;
				break;
			}
		}
	}

	public string GetDmgLoot(string strCond)
	{
		if (string.IsNullOrEmpty(strCond))
		{
			return null;
		}
		foreach (DestCheck destCheck in this.aChecks)
		{
			if (destCheck.strDamageCond == strCond)
			{
				return destCheck.strLootModeSwitch;
			}
		}
		return null;
	}

	public void ClearChecks()
	{
		foreach (DestCheck destCheck in this.aChecks)
		{
			destCheck.Destroy();
		}
		this.aChecks.Clear();
		this.bActive = (this.aChecks.Count > 0);
	}

	public void CopyFrom(Destructable dest)
	{
		foreach (DestCheck destCheck in dest.aChecks)
		{
			DestCheck destCheck2 = new DestCheck();
			destCheck2.fSignalCheckPeriod = destCheck.fSignalCheckPeriod;
			destCheck2.strDamageCond = destCheck.strDamageCond;
			destCheck2.strDamageCondMax = destCheck.strDamageCondMax;
			destCheck2.strLootModeSwitch = destCheck.strLootModeSwitch;
			this.aChecks.Add(destCheck2);
		}
		this.bActive = (this.aChecks.Count > 0);
	}

	private bool bActive
	{
		get
		{
			return this._bActive;
		}
		set
		{
			this._bActive = value;
			if (value)
			{
				this.fTimeOfNextSignalCheck = StarSystem.fEpoch + (double)(UnityEngine.Random.value * 0.5f);
			}
			else
			{
				this.fTimeOfNextSignalCheck = double.PositiveInfinity;
			}
		}
	}

	public double DmgLeft(string strStat)
	{
		if (strStat == null || strStat == string.Empty)
		{
			return 0.0;
		}
		foreach (DestCheck destCheck in this.aChecks)
		{
			if (!(destCheck.strDamageCond != strStat))
			{
				return this.co.GetCondAmount(destCheck.strDamageCondMax) - this.co.GetCondAmount(strStat);
			}
		}
		return 0.0;
	}

	public double DmgMax(string strStat)
	{
		if (strStat == null || strStat == string.Empty)
		{
			return 0.0;
		}
		foreach (DestCheck destCheck in this.aChecks)
		{
			if (!(destCheck.strDamageCond != strStat))
			{
				return this.co.GetCondAmount(destCheck.strDamageCondMax);
			}
		}
		return 0.0;
	}

	public CondOwner CO
	{
		get
		{
			return this.co;
		}
	}

	private CondOwner co;

	private double fTimeOfNextSignalCheck;

	public List<DestCheck> aChecks;

	private static float debugRunTimems;

	private static int debugTimesRunThisFrame;

	private static bool bOutput;

	private bool bDebug;

	public bool bNeedsCheck = true;

	private bool _bActive;
}
