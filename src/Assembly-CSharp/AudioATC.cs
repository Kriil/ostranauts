using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioATC : MonoBehaviour
{
	private void Awake()
	{
	}

	private void Update()
	{
		if (this.srcVoiceNow == null)
		{
			return;
		}
		this.fTimeLeft -= Time.deltaTime;
		if (this.fTimeLeft <= 0f)
		{
			this.GetNextLine(0.0);
		}
	}

	public void Stop()
	{
		if (this.srcNoiseNow != null)
		{
			this.srcNoiseNow.Stop();
		}
		if (this.srcVoiceNow != null)
		{
			this.srcVoiceNow.Stop();
		}
	}

	public void Init(double fDelay, bool bPreCook)
	{
		this.Stop();
		this.fTimeLeft = 0f;
		this.aScript1 = new List<string>
		{
			"atcShipDeep01",
			"atcTowerDeep02",
			"atcTowerClose01",
			"atcShipClose02",
			"atcTowerDock01",
			"atcShipDock02"
		};
		if (bPreCook)
		{
			int num = MathUtils.Rand(0, 7, MathUtils.RandType.Flat, null);
			for (int i = 0; i < num; i++)
			{
				this.aScript1.RemoveAt(0);
			}
		}
		this.GetNextLine(fDelay);
		this.RandomizePitchVol();
		this.RandomizeFilters(this.srcVoice1);
		this.RandomizeFilters(this.srcVoice2);
		this.Play(fDelay);
	}

	private void GetNextLine(double fDelay)
	{
		if (this.aScript1.Count == 0)
		{
			this.Init(0.0, false);
			return;
		}
		string text = this.aScript1[0];
		this.aScript1.RemoveAt(0);
		if (text.IndexOf("Ship") >= 0)
		{
			this.srcVoice1.clip = Resources.Load<AudioClip>("Audio/" + text + this.strVoice1Suffix);
			this.srcVoiceNow = this.srcVoice1;
			this.srcNoiseNow = this.srcNoise1;
		}
		else
		{
			this.srcVoice2.clip = Resources.Load<AudioClip>("Audio/" + text + this.strVoice2Suffix);
			this.srcVoiceNow = this.srcVoice2;
			this.srcNoiseNow = this.srcNoise2;
		}
		this.Play(fDelay);
	}

	public void RandomizeFilters(AudioSource src)
	{
		AudioLowPassFilter component = src.GetComponent<AudioLowPassFilter>();
		component.cutoffFrequency = MathUtils.Rand(this.fMinLowPass, this.fMaxLowPass, MathUtils.RandType.Flat, null);
		AudioHighPassFilter component2 = src.GetComponent<AudioHighPassFilter>();
		component2.cutoffFrequency = MathUtils.Rand(this.fMinHighPass, this.fMaxHighPass, MathUtils.RandType.Flat, null);
	}

	public void RandomizePitchVol()
	{
		this.srcVoice1.volume = MathUtils.Rand(this.fMinVolVoice, this.fMaxVolVoice, MathUtils.RandType.Flat, null);
		this.srcVoice1.pitch = MathUtils.Rand(this.fMinPitchVoice, this.fMaxPitchVoice, MathUtils.RandType.Flat, null);
		this.srcVoice2.volume = MathUtils.Rand(this.fMinVolVoice, this.fMaxVolVoice, MathUtils.RandType.Flat, null);
		this.srcVoice2.pitch = MathUtils.Rand(this.fMinPitchVoice, this.fMaxPitchVoice, MathUtils.RandType.Flat, null);
		if (MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null) >= 0.5f)
		{
			this.strVoice1Suffix = "a";
		}
		else
		{
			this.strVoice1Suffix = string.Empty;
		}
		this.srcNoise1.volume = MathUtils.Rand(this.fMinVolNoise, this.fMaxVolNoise, MathUtils.RandType.Flat, null);
		this.srcNoise1.pitch = MathUtils.Rand(this.fMinPitchNoise, this.fMaxPitchNoise, MathUtils.RandType.Flat, null);
		this.srcNoise2.volume = MathUtils.Rand(this.fMinVolNoise, this.fMaxVolNoise, MathUtils.RandType.Flat, null);
		this.srcNoise2.pitch = MathUtils.Rand(this.fMinPitchNoise, this.fMaxPitchNoise, MathUtils.RandType.Flat, null);
		if (MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null) >= 0.5f)
		{
			this.strVoice2Suffix = "a";
		}
		else
		{
			this.strVoice2Suffix = string.Empty;
		}
	}

	public void Play(double fDelay)
	{
		double num = AudioSettings.dspTime + fDelay;
		this.srcNoiseNow.PlayScheduled(num);
		this.srcVoiceNow.PlayScheduled(num);
		num += (double)(this.srcVoiceNow.clip.length / this.srcVoiceNow.pitch);
		num += (double)MathUtils.Rand(0f, this.fMaxDeadAirLength, MathUtils.RandType.Low, null);
		this.srcNoiseNow.SetScheduledEndTime(num);
		num += (double)MathUtils.Rand(this.fMinGapLength, this.fMaxGapLength, MathUtils.RandType.Flat, null);
		this.fTimeLeft = Convert.ToSingle(num - AudioSettings.dspTime);
	}

	public AudioSource srcVoice1;

	public AudioSource srcNoise1;

	public AudioSource srcVoice2;

	public AudioSource srcNoise2;

	private AudioSource srcVoiceNow;

	private AudioSource srcNoiseNow;

	private string strVoice1Suffix = string.Empty;

	private string strVoice2Suffix = string.Empty;

	private List<string> aScript1;

	private float fMinVolVoice = 0.85f;

	private float fMaxVolVoice = 1f;

	private float fMinVolNoise = 0.01f;

	private float fMaxVolNoise = 0.125f;

	private float fMinPitchVoice = 0.9f;

	private float fMaxPitchVoice = 1.1f;

	private float fMinPitchNoise = 0.9f;

	private float fMaxPitchNoise = 1.1f;

	private float fMinGapLength;

	private float fMaxGapLength = 15f;

	private float fMaxDeadAirLength = 1.5f;

	private float fMinLowPass = 1000f;

	private float fMaxLowPass = 2500f;

	private float fMinHighPass = 450f;

	private float fMaxHighPass = 2500f;

	private float fTimeLeft;
}
