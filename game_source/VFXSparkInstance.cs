using System;
using UnityEngine;

public class VFXSparkInstance
{
	public VFXSparkInstance(ParticleSystem vfx, string strAudioEmitter)
	{
		this.vfx = vfx;
		Transform transform = vfx.transform;
		JsonLight light = DataHandler.GetLight("VFXSparkBlue");
		this.vis = UnityEngine.Object.Instantiate<Visibility>(Visibility.visTemplate, transform);
		this.vis.LightColor = DataHandler.GetColor(light.strColor);
		this.vis.GO.name = light.strName;
		this.vis.Parent = transform;
		this.vis.tfParent = transform;
		this.vis.SetCookie("ItmLitSphere01");
		Vector2 vector = default(Vector2);
		vector = light.ptPos;
		float x = 1f * vector.x / 16f;
		float y = 1f * vector.y / 16f;
		this.vis.ptOffset = new Vector2(x, y);
		CrewSim.objInstance.AddLight(this.vis);
		this.vis.GO.SetActive(false);
		JsonAudioEmitter audioEmitter = DataHandler.GetAudioEmitter(strAudioEmitter);
		if (audioEmitter != null)
		{
			this.ae = vfx.gameObject.AddComponent<AudioEmitter>();
			this.ae.SetData(audioEmitter);
		}
	}

	public ParticleSystem vfx;

	public Visibility vis;

	public AudioEmitter ae;
}
