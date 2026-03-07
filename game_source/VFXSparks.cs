using System;
using System.Collections.Generic;
using UnityEngine;

public class VFXSparks : MonoBehaviour
{
	private void Start()
	{
		this.aSparks = new List<VFXSparkInstance>();
		GameObject original = Resources.Load("vfxSparks02") as GameObject;
		string arg = "ShipSpark0";
		for (int i = 0; i < 10; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, base.transform);
			string strAudioEmitter = arg + (i + 1);
			if (i > 4)
			{
				strAudioEmitter = arg + (i - 4);
			}
			this.aSparks.Add(new VFXSparkInstance(gameObject.GetComponent<ParticleSystem>(), strAudioEmitter));
		}
	}

	private void Update()
	{
		for (int i = 0; i < this.aSparks.Count; i++)
		{
			VFXSparkInstance vfxsparkInstance = this.aSparks[i];
			if (vfxsparkInstance.vfx.main.duration <= vfxsparkInstance.vfx.time)
			{
				this.CleanupSpark(vfxsparkInstance);
			}
			else
			{
				ParticleSystem.MainModule main = vfxsparkInstance.vfx.main;
				float num = Time.timeScale;
				if (CrewSim.Paused)
				{
					num = 0f;
				}
				main.simulationSpeed = num;
				Color lightColor = vfxsparkInstance.vis.LightColor;
				lightColor.a = vfxsparkInstance.vis.LightColor.a / (1f + 1f * num);
				vfxsparkInstance.vis.LightColor = lightColor;
			}
		}
	}

	private void CleanupSpark(VFXSparkInstance spark)
	{
		if (spark == null)
		{
			return;
		}
		spark.vfx.Stop();
		spark.vis.GO.SetActive(false);
	}

	public void AddSparkAt(Vector3 vWorldPos)
	{
		foreach (VFXSparkInstance vfxsparkInstance in this.aSparks)
		{
			if (!vfxsparkInstance.vfx.isPlaying)
			{
				vfxsparkInstance.vfx.transform.position = vWorldPos;
				vfxsparkInstance.vfx.Play();
				Color lightColor = vfxsparkInstance.vis.LightColor;
				lightColor.a = 1f;
				vfxsparkInstance.vis.LightColor = lightColor;
				vfxsparkInstance.vis.Position = vWorldPos;
				vfxsparkInstance.vis.bRedraw = true;
				vfxsparkInstance.vis.GO.SetActive(true);
				if (vfxsparkInstance.ae != null)
				{
					vfxsparkInstance.ae.StartTrans(false);
				}
				break;
			}
		}
	}

	private List<VFXSparkInstance> aSparks;

	private const int MAX_SPARKS = 10;
}
