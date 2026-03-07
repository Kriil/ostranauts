using System;
using System.Collections.Generic;
using UnityEngine;

public class VFXGasPuffs : MonoBehaviour
{
	private void Start()
	{
		this.bOverride = false;
		this.vfx = base.gameObject.GetComponent<ParticleSystem>();
		this.aPuffs = new ParticleSystem.Particle[this.vfx.main.maxParticles];
	}

	private void Update()
	{
		this.DoParticles();
	}

	private void DoParticles()
	{
		this.nPuffsActive = this.vfx.GetParticles(this.aPuffs);
		this.vfx.GetCustomParticleData(this.customData, ParticleSystemCustomData.Custom1);
		for (int i = 0; i < this.nPuffsActive; i++)
		{
			if (this.customData[i] == Vector4.zero)
			{
				if (VFXGasPuffs.aPoints.Count > 0)
				{
					this.customData[i] = VFXGasPuffs.aPoints[0].ptOriginToDest;
					VFXGasPuffs.aPoints.RemoveAt(0);
				}
				else
				{
					this.customData[i] = new Vector4(9999f, 9999f, 9999f, 9999f);
				}
			}
			ParticleSystem.Particle particle = this.aPuffs[i];
			float num = (particle.startLifetime - particle.remainingLifetime) / particle.startLifetime;
			num *= num;
			if (float.IsNaN(num))
			{
				num = 0f;
			}
			particle.position = new Vector3
			{
				x = Mathf.Lerp(this.customData[i].x, this.customData[i].z, num),
				y = Mathf.Lerp(this.customData[i].y, this.customData[i].w, num)
			};
			this.aPuffs[i] = particle;
		}
		this.vfx.SetParticles(this.aPuffs, this.nPuffsActive);
		this.vfx.SetCustomParticleData(this.customData, ParticleSystemCustomData.Custom1);
	}

	public void AddGasPuff(VFXGasPuffData gpd)
	{
		if (gpd == null)
		{
			return;
		}
		VFXGasPuffs.aPoints.Add(gpd);
		this.vfx.Emit(1);
	}

	public static List<VFXGasPuffData> aPoints = new List<VFXGasPuffData>();

	public Vector3 point = default(Vector3);

	private List<Vector4> customData = new List<Vector4>();

	private bool bOverride;

	private ParticleSystem vfx;

	private ParticleSystem.Particle[] aPuffs;

	private int nPuffsActive;
}
