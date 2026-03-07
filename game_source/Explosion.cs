using System;
using UnityEngine;

public class Explosion : MonoBehaviour, IManUpdater
{
	private void Update()
	{
		if (!this.bFinished)
		{
			if (!this.bStarted)
			{
				this.fEpochStart = StarSystem.fEpoch;
				this.bSilent = (Math.Abs(this.CO.ship.fFirstVisit - StarSystem.fEpoch) < 2.0);
			}
			string text = this.strType;
			if (text != null)
			{
				if (text == "Fusion")
				{
					if (!this.bStarted)
					{
						this.fDmg = MathUtils.Rand(1500.0, 3250.0, MathUtils.RandType.Mid, null);
						if (!this.bSilent)
						{
							CrewSim.objInstance.CamShake(1f);
							AudioManager.am.PlayAudioEmitter("ExplosionFusion01", false, false);
							foreach (CondOwner condOwner in this.CO.ship.GetPeople(true))
							{
								double num = MathUtils.Rand(1000.0, 7000.0, MathUtils.RandType.Flat, null);
								condOwner.AddCondAmount("StatRad", num, 0.0, 0f);
								Debug.Log(string.Concat(new object[]
								{
									"Radiation exposure to ",
									condOwner.strName,
									": ",
									num
								}));
							}
						}
					}
					this.ExplodeFusion();
				}
			}
			this.bStarted = true;
		}
		double num2 = StarSystem.fEpoch - this.fEpochStart;
		if (num2 > 5.0)
		{
			this.CO.RemoveFromCurrentHome(true);
			this.CO.Destroy();
		}
		else
		{
			double num3 = (double)(1f - (float)num2 / 5f);
			Item item = this.CO.Item;
			item.fFlickerAmount = (float)num3;
			foreach (Visibility visibility in item.aLights)
			{
				visibility.Radius = 16f;
				visibility.fFlickerAmount = (float)num3;
			}
		}
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
	}

	private void ExplodeFusion()
	{
		if (this.fDmg > 0.0 && this.CO.ship != null)
		{
			JsonAttackMode attackMode = DataHandler.GetAttackMode("AModeExplosionFusion");
			if (attackMode == null)
			{
				this.fDmg = 0.0;
				return;
			}
			bool bPoolVisUpdates = CrewSim.bPoolVisUpdates;
			bool bPoolShipUpdates = CrewSim.bPoolShipUpdates;
			bool bAudio = Wound.bAudio;
			Wound.bAudio = true;
			CrewSim.bPoolVisUpdates = true;
			CrewSim.bPoolShipUpdates = true;
			Vector3 position = this.CO.tf.position;
			float num = 5f;
			double num2 = 150.0;
			if (this.fDmg < num2)
			{
				num2 = this.fDmg;
			}
			this.fDmg -= num2;
			if (!this.bStarted)
			{
				this.CO.ship.DamageRadius(position, num, (float)num2, attackMode, null, true);
			}
			float angle = MathUtils.Rand(0f, 360f, MathUtils.RandType.Flat, null);
			Vector3 vector = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up * num;
			num = 15f;
			num2 = 200.0;
			if (this.fDmg < num2)
			{
				num2 = this.fDmg;
			}
			this.CO.ship.DamageRay(position, vector.normalized, num, (float)num2, attackMode, null, true);
			this.fDmg -= num2;
			Wound.bAudio = bAudio;
			CrewSim.bPoolVisUpdates = bPoolVisUpdates;
			CrewSim.bPoolShipUpdates = bPoolShipUpdates;
		}
	}

	public CondOwner CO
	{
		get
		{
			if (this.co == null)
			{
				this.co = base.GetComponent<CondOwner>();
			}
			return this.co;
		}
	}

	private CondOwner co;

	public string strType;

	private double fEpochStart;

	private double fDmg;

	private bool bStarted;

	private bool bFinished;

	private bool bSilent;
}
