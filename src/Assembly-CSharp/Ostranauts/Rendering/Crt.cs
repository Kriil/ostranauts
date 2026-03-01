using System;
using UnityEngine;

namespace Ostranauts.Rendering
{
	public class Crt : MonoBehaviour
	{
		private void Awake()
		{
			this._material = new Material(this._crtShader);
			this._material.hideFlags = HideFlags.HideAndDontSave;
		}

		private void OnEnable()
		{
			Crt._isRunning = true;
			Crt._bend = this._bendyness;
		}

		private void OnDisable()
		{
			Crt._isRunning = false;
			base.enabled = false;
		}

		public static Vector3 ConvertToCrtCoords(float x, float y, float z)
		{
			if (!Crt._isRunning)
			{
				return new Vector3(x, y, z);
			}
			x = Mathf.InverseLerp(0f, (float)CrewSim.objInstance.ActiveCam.pixelWidth, x);
			y = Mathf.InverseLerp(0f, (float)CrewSim.objInstance.ActiveCam.pixelHeight, y);
			x -= 0.5f;
			y -= 0.5f;
			x *= 2f;
			y *= 2f;
			x *= 1f + Mathf.Pow(Mathf.Abs(y) / Crt._bend, 2f);
			y *= 1f + Mathf.Pow(Mathf.Abs(x) / Crt._bend, 2f);
			x /= 2.5f;
			y /= 2.5f;
			x += 0.5f;
			y += 0.5f;
			x = Mathf.Lerp(0f, (float)CrewSim.objInstance.ActiveCam.pixelWidth, x);
			y = Mathf.Lerp(0f, (float)CrewSim.objInstance.ActiveCam.pixelHeight, y);
			return new Vector3(x, y, z);
		}

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			this._material.SetFloat("u_time", Time.fixedUnscaledTime);
			this._material.SetFloat("u_bend", this._bendyness);
			this._material.SetFloat("u_scanline_size_1", this._scanLine1Size);
			this._material.SetFloat("u_scanline_speed_1", this._scanLine1Speed);
			this._material.SetFloat("u_scanline_size_2", this._scanLine2Size);
			this._material.SetFloat("u_scanline_speed_2", this._scanLine2Speed);
			this._material.SetFloat("u_scanline_amount", this._scanLineCount);
			this._material.SetFloat("u_vignette_size", this._vignetteSize);
			this._material.SetFloat("u_vignette_smoothness", this._vignetteSmoothness);
			this._material.SetFloat("u_vignette_edge_round", this._vignetteEdgeRound);
			this._material.SetFloat("u_noise_size", this._noiseSize);
			this._material.SetFloat("u_noise_amount", this._noiseAmount);
			Graphics.Blit(source, destination, this._material);
		}

		[SerializeField]
		private Shader _crtShader;

		[Header("Bendyness")]
		[SerializeField]
		private float _bendyness = 4f;

		[Header("Scanlines")]
		[SerializeField]
		private float _scanLine1Size = 400f;

		[SerializeField]
		private float _scanLine1Speed = -10f;

		[SerializeField]
		private float _scanLine2Size = 20f;

		[SerializeField]
		private float _scanLine2Speed = -3f;

		[SerializeField]
		public float _scanLineCount = 0.03f;

		[Header("Vignette")]
		[SerializeField]
		private float _vignetteSize = 1.9f;

		[SerializeField]
		private float _vignetteSmoothness = 0.6f;

		[SerializeField]
		private float _vignetteEdgeRound = 8f;

		[Header("Noise")]
		[SerializeField]
		private float _noiseSize = 75f;

		[SerializeField]
		private float _noiseAmount = 0.05f;

		private Material _material;

		private static float _bend;

		private static bool _isRunning;
	}
}
