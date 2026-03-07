using System;
using UnityEngine;
using UnityEngine.Video;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/GlitchEffect")]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(VideoPlayer))]
public class VHSPostProcessEffect : MonoBehaviour
{
	private void OnEnable()
	{
		this._material = new Material(this.shader);
		this._player = base.GetComponent<VideoPlayer>();
		this._player.isLooping = true;
		this._player.renderMode = VideoRenderMode.APIOnly;
		this._player.audioOutputMode = VideoAudioOutputMode.None;
		this._player.clip = this.VHSClip;
		this._player.Play();
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		this._material.SetTexture("_VHSTex", this._player.texture);
		this._yScanline += Time.deltaTime * 0.01f;
		this._xScanline -= Time.deltaTime * 0.1f;
		if (this._yScanline >= 1f)
		{
			this._yScanline = UnityEngine.Random.value;
		}
		if (this._xScanline <= 0f || (double)UnityEngine.Random.value < 0.05)
		{
			this._xScanline = UnityEngine.Random.value;
		}
		this._material.SetFloat("_yScanline", this._yScanline);
		this._material.SetFloat("_xScanline", this._xScanline);
		Graphics.Blit(source, destination, this._material);
	}

	protected void OnDisable()
	{
		if (this._material)
		{
			UnityEngine.Object.DestroyImmediate(this._material);
		}
	}

	public void SetTime(double fTime)
	{
		this._player.time = fTime;
	}

	public Shader shader;

	public VideoClip VHSClip;

	private float _yScanline;

	private float _xScanline;

	private Material _material;

	private VideoPlayer _player;
}
