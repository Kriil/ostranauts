using System;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

public class CameraImageEffects : MonoBehaviour
{
	private void Awake()
	{
		this.bloom = base.GetComponent<BloomOptimized>();
		this.blur = base.GetComponent<BlurOptimized>();
		this.motionBlur = base.GetComponent<MotionBlur>();
		this.vignette = base.GetComponent<VignetteAndChromaticAberration>();
	}

	private void Start()
	{
		this.bloom.intensity = 2f;
		this.bloom.blurSize = 4f;
		this.bloom.blurIterations = 3;
		this.motionBlur.blurAmount = 0.69f;
	}

	private void Update()
	{
		if (this.showPain)
		{
			this.bloom.enabled = true;
			this.motionBlur.enabled = true;
			this.vignette.enabled = true;
			float num = Mathf.Sin(Time.timeSinceLevelLoad * 1.5f) * Mathf.Sin(Time.timeSinceLevelLoad) / 4.5f + 0.55f;
			this.vignette.intensity = num;
			num = -(Mathf.Sin(Time.timeSinceLevelLoad * 1.5f) * Mathf.Sin(Time.timeSinceLevelLoad)) / 4f + 0.25f;
			this.bloom.threshold = num;
			num = Mathf.Sin(Time.timeSinceLevelLoad * 1.5f) * Mathf.Sin(Time.timeSinceLevelLoad) * 2f + 2f;
			this.bloom.blurSize = num;
			num = Mathf.Sin(Time.timeSinceLevelLoad * 1.5f) * Mathf.Sin(Time.timeSinceLevelLoad) * 40f + 0.55f;
			this.vignette.chromaticAberration = num;
			if (Mathf.Sin(Time.timeSinceLevelLoad * 1.5f) * Mathf.Sin(Time.timeSinceLevelLoad) > 0.7f)
			{
			}
		}
		else if (this.showInsaneMaybe)
		{
			this.vignette.enabled = true;
			float chromaticAberration = Mathf.Sin(Time.timeSinceLevelLoad * 1.5f) * Mathf.Sin(Time.timeSinceLevelLoad) * 20000f + (float)UnityEngine.Random.Range(0, 100);
			this.vignette.chromaticAberration = chromaticAberration;
		}
		else
		{
			this.bloom.enabled = false;
			this.blur.enabled = false;
			this.motionBlur.enabled = false;
			this.vignette.enabled = false;
		}
	}

	public bool Pain
	{
		get
		{
			return this.showPain;
		}
		set
		{
			this.showPain = value;
		}
	}

	public bool Insane
	{
		get
		{
			return this.showInsaneMaybe;
		}
		set
		{
			this.showInsaneMaybe = value;
		}
	}

	private BloomOptimized bloom;

	private BlurOptimized blur;

	private MotionBlur motionBlur;

	private VignetteAndChromaticAberration vignette;

	private bool showPain;

	private bool showInsaneMaybe;
}
