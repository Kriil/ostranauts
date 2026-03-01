using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUICrewNoise : MonoBehaviour
{
	private void Start()
	{
		this.noiseTextures.Add(DataHandler.LoadPNG("GUINoise1Large.png", false, false));
		this.noiseTextures.Add(DataHandler.LoadPNG("GUINoise2Large.png", false, false));
		this.noiseTextures.Add(DataHandler.LoadPNG("GUINoise3Large.png", false, false));
		this.noiseTextures.Add(DataHandler.LoadPNG("GUINoise4Large.png", false, false));
		this.noiseTextures.Add(DataHandler.LoadPNG("GUINoise5Large.png", false, false));
		this.noiseTextures.Add(DataHandler.LoadPNG("GUINoise6Large.png", false, false));
		this.image = base.GetComponent<RawImage>();
		this.lastTexture = this.noiseTextures[5];
	}

	private void Update()
	{
		if ((double)this.durationsincelastupdate > 0.05)
		{
			this.image.texture = this.noiseTextures[UnityEngine.Random.Range(0, 6)];
			this.noiseTextures.Add(this.lastTexture);
			this.noiseTextures.Remove((Texture2D)this.image.texture);
			this.lastTexture = (Texture2D)this.image.texture;
			this.durationsincelastupdate = 0f;
			return;
		}
		this.durationsincelastupdate += Time.deltaTime;
	}

	private List<Texture2D> noiseTextures = new List<Texture2D>();

	private Texture2D lastTexture;

	private RawImage image;

	private float timePassed;

	private float durationsincelastupdate;
}
