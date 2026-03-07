using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Runtime audio source wrapper. Owns steady/transient/pickup clips plus low-pass
// filtering and mixer routing for one world sound emitter.
public class AudioEmitter : MonoBehaviour
{
	// Stops active sources and clears cached clip/source references.
	public void Destroy()
	{
		if (this.srcSteady != null)
		{
			this.srcSteady.Stop();
		}
		if (this.srcTrans != null)
		{
			this.srcTrans.Stop();
		}
		this.clipPickup = null;
		this.srcSteady = null;
		this.srcTrans = null;
		this._LoPassFilter = null;
	}

	// Lazily creates the backing AudioSource components with the current range and
	// spatial settings.
	public void Init()
	{
		if (this.bInit)
		{
			return;
		}
		GameObject gameObject = base.gameObject;
		bool activeInHierarchy = gameObject.activeInHierarchy;
		gameObject.SetActive(false);
		this.srcSteady = base.gameObject.AddComponent<AudioSource>();
		this.srcSteady.loop = true;
		this.srcSteady.playOnAwake = false;
		this.srcSteady.minDistance = this.fMinDistance;
		this.srcSteady.maxDistance = this.fMaxDistance;
		this.srcSteady.spatialBlend = this.fSpatialBlend;
		this.srcSteady.dopplerLevel = this.fDopplerLevel;
		this.srcSteady.enabled = false;
		this.srcTrans = base.gameObject.AddComponent<AudioSource>();
		this.srcTrans.loop = false;
		this.srcTrans.playOnAwake = false;
		this.srcTrans.minDistance = this.fMinDistance;
		this.srcTrans.maxDistance = this.fMaxDistance;
		this.srcTrans.spatialBlend = this.fSpatialBlend;
		this.srcTrans.dopplerLevel = this.fDopplerLevel;
		this.srcTrans.enabled = false;
		gameObject.SetActive(activeInHierarchy);
		this.bInit = true;
	}

	// Applies a JsonAudioEmitter definition to this live emitter.
	public void SetData(JsonAudioEmitter jae)
	{
		if (jae == null)
		{
			return;
		}
		this.SetClip(jae.strClipSteady, AudioEmitter.ClipType.STEADY);
		this.SetClip(jae.strClipTrans, AudioEmitter.ClipType.TRANS);
		this.SetClip(jae.strClipPickup, AudioEmitter.ClipType.PICKUP);
		float num = jae.fVolumeSteady;
		this.srcSteady.volume = num;
		this.fVolumeSteady = num;
		num = jae.fVolumeTrans;
		this.srcTrans.volume = num;
		this.fVolumeTrans = num;
		this.fVolumePickup = jae.fVolumePickup;
		this.fSteadyDelay = jae.fSteadyDelay;
		this.fTransDuration = jae.fTransDuration;
		if (jae.fSpatialBlend >= 0f && jae.fSpatialBlend <= 1f)
		{
			num = jae.fSpatialBlend;
			this.srcSteady.spatialBlend = num;
			num = num;
			this.srcTrans.spatialBlend = num;
			this.fSpatialBlend = num;
		}
		if (jae.fLoPassFreq >= 0f || jae.fLoPassFreqOccluded >= 0f)
		{
			if (jae.fLoPassFreq >= 0f)
			{
				num = jae.fLoPassFreq;
				this.LoPassFilter.cutoffFrequency = num;
				this.fLoPassFreq = num;
			}
			if (jae.fLoPassFreqOccluded >= 0f)
			{
				this.fLoPassFreqOccluded = jae.fLoPassFreqOccluded;
			}
		}
		if (jae.fMaxDistance > 0f)
		{
			num = jae.fMaxDistance;
			this.srcTrans.maxDistance = num;
			num = num;
			this.srcSteady.maxDistance = num;
			this.fMaxDistance = num;
		}
		if (jae.fMinDistance > 0f)
		{
			num = jae.fMinDistance;
			this.srcTrans.minDistance = num;
			num = num;
			this.srcSteady.minDistance = num;
			this.fMinDistance = num;
		}
		if (jae.strFalloffCurve != null && jae.strFalloffCurve != string.Empty)
		{
			AnimationCurveAsset animationCurveAsset = Resources.Load<AnimationCurveAsset>("Curves/" + jae.strFalloffCurve);
			if (animationCurveAsset != null)
			{
				AudioSource audioSource = this.srcSteady;
				AudioRolloffMode rolloffMode = AudioRolloffMode.Custom;
				this.srcTrans.rolloffMode = rolloffMode;
				audioSource.rolloffMode = rolloffMode;
				this.srcSteady.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animationCurveAsset.curve);
				this.srcTrans.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animationCurveAsset.curve);
			}
		}
		if (jae.strLowPassCurve != null && jae.strLowPassCurve != string.Empty)
		{
			AnimationCurveAsset animationCurveAsset2 = Resources.Load<AnimationCurveAsset>("Curves/" + jae.strLowPassCurve);
			if (animationCurveAsset2 != null)
			{
				this.LoPassFilter.customCutoffCurve = animationCurveAsset2;
			}
		}
		AudioMixerGroup audioMixerGroup = null;
		if (AudioManager.MixerGroups.TryGetValue(jae.strMixerName, out audioMixerGroup))
		{
			AudioSource audioSource2 = this.srcSteady;
			AudioMixerGroup outputAudioMixerGroup = audioMixerGroup;
			this.srcTrans.outputAudioMixerGroup = outputAudioMixerGroup;
			audioSource2.outputAudioMixerGroup = outputAudioMixerGroup;
		}
	}

	// Loads one named audio clip into the requested clip slot.
	public void SetClip(string strClip, AudioEmitter.ClipType nType)
	{
		if (!this.bInit)
		{
			this.Init();
		}
		AudioClip audioClip = null;
		if (strClip != null)
		{
			audioClip = Resources.Load<AudioClip>("Audio/" + strClip);
		}
		if (audioClip == null)
		{
			return;
		}
		switch (nType)
		{
		case AudioEmitter.ClipType.STEADY:
			this.srcSteady.clip = audioClip;
			this.bSteady = true;
			break;
		case AudioEmitter.ClipType.TRANS:
			this.srcTrans.clip = audioClip;
			this.bTrans = true;
			break;
		case AudioEmitter.ClipType.PICKUP:
			this.clipPickup = audioClip;
			this.bPickup = true;
			break;
		case AudioEmitter.ClipType.OTHER:
			this.clipOther = audioClip;
			this.bPickup = true;
			break;
		}
	}

	public void PlaySteady()
	{
		if (!this.bSteadyPlaying)
		{
			if (this.bSteady)
			{
				this.srcSteady.enabled = true;
				this.srcSteady.Play();
			}
			this.bSteadyPlaying = true;
		}
	}

	public void FadeInSteady(float fDuration = -1f, float fDelay = -1f)
	{
		if (fDuration < 0f)
		{
			fDuration = this.fTransDuration;
		}
		if (fDelay < 0f)
		{
			fDelay = this.fSteadyDelay;
		}
		base.StartCoroutine("crFadeInSteady", new float[]
		{
			fDuration,
			fDelay
		});
	}

	private IEnumerator crFadeInSteady(float[] aTimeInfo)
	{
		if (this.srcSteady == null || !this.bSteady || aTimeInfo == null || aTimeInfo.Length < 2 || this.srcSteady.clip == null)
		{
			yield break;
		}
		float fDuration = aTimeInfo[0];
		float fDelay = aTimeInfo[1];
		float fTimePassed = 0f;
		this.fFadeInVolumeStart = this.srcSteady.volume;
		while (fTimePassed < fDelay)
		{
			fTimePassed += Time.deltaTime;
			yield return null;
		}
		if (fTimePassed >= fDelay && !this.bSteadyPlaying)
		{
			if (this.bSteady)
			{
				this.srcSteady.enabled = true;
				this.srcSteady.Play();
			}
			this.bSteadyPlaying = true;
		}
		while (fTimePassed < fDuration + fDelay)
		{
			fTimePassed += Time.deltaTime;
			float fBlend = Mathf.Clamp01(fTimePassed / fDuration);
			this.srcSteady.volume = Mathf.Lerp(this.fFadeInVolumeStart, this.fVolumeSteady, fBlend);
			yield return null;
		}
		this.srcSteady.volume = this.fVolumeSteady;
		yield break;
	}

	public void StopSteady()
	{
		if (this.srcSteady != null)
		{
			base.StartCoroutine(this.crFadeOutSteady());
		}
		this.bSteadyPlaying = false;
	}

	public void TweakSteady(float fVolume, float fPitch)
	{
		if (this.srcSteady != null)
		{
			this.srcSteady.volume = fVolume * this.fVolumeSteady;
			this.srcSteady.pitch = fPitch;
			return;
		}
	}

	public void TweakSteadyReset()
	{
		if (this.srcSteady != null)
		{
			this.srcSteady.volume = this.fVolumeSteady;
			this.srcSteady.pitch = 1f;
			return;
		}
	}

	public void FastForwardSteady(float fAmount)
	{
		if (this.srcSteady != null && this.srcSteady.clip != null)
		{
			fAmount = Mathf.Clamp(fAmount, 0f, 1f);
			this.srcSteady.time = this.srcSteady.clip.length * fAmount;
		}
	}

	public void RandomizePitchAll()
	{
		float pitch = MathUtils.Rand(0.95f, 1.05f, MathUtils.RandType.Flat, null);
		if (this.srcTrans != null)
		{
			this.srcTrans.pitch = pitch;
		}
		if (this.srcSteady != null)
		{
			this.srcSteady.pitch = pitch;
		}
	}

	public void ResetPitchAll()
	{
		if (this.srcTrans != null)
		{
			this.srcTrans.pitch = 1f;
		}
		if (this.srcSteady != null)
		{
			this.srcSteady.pitch = 1f;
		}
	}

	private IEnumerator crFadeOutSteady()
	{
		if (this.srcSteady.clip == null || !this.bSteady)
		{
			yield break;
		}
		float fDuration = 0.1f;
		float fTimePassed = 0f;
		this.fFadeOutVolumeStart = this.srcSteady.volume;
		while (fTimePassed <= fDuration)
		{
			fTimePassed += Time.deltaTime;
			float fBlend = Mathf.Clamp01(fTimePassed / fDuration);
			this.srcSteady.volume = Mathf.Lerp(this.fFadeOutVolumeStart, 0f, fBlend);
			yield return null;
		}
		this.srcSteady.enabled = false;
		yield break;
	}

	public void StartTrans(bool dontStack = false)
	{
		if (!CrewSim.objInstance.FinishedLoading)
		{
			return;
		}
		if (this.srcTrans.isPlaying && dontStack)
		{
			return;
		}
		if (this.bTrans)
		{
			this.srcTrans.enabled = true;
			this.srcTrans.PlayOneShot(this.srcTrans.clip, this.fVolumeTrans);
		}
	}

	public void StartPickup()
	{
		if (this.bPickup)
		{
			this.srcTrans.enabled = true;
			this.srcTrans.PlayOneShot(this.clipPickup, this.fVolumePickup);
		}
	}

	public void StartOther(string strAudioEmitterName)
	{
		if (string.IsNullOrEmpty(strAudioEmitterName))
		{
			return;
		}
		JsonAudioEmitter audioEmitter = DataHandler.GetAudioEmitter(strAudioEmitterName);
		if (audioEmitter == null)
		{
			return;
		}
		this.SetClip(audioEmitter.strClipTrans, AudioEmitter.ClipType.OTHER);
		if (this.bPickup)
		{
			this.srcTrans.enabled = true;
			this.srcTrans.PlayOneShot(this.clipOther, audioEmitter.fVolumeTrans);
		}
	}

	public bool PlayingSteady
	{
		get
		{
			return this.bSteadyPlaying;
		}
	}

	public AudioLowPassFilter LoPassFilter
	{
		get
		{
			this._LoPassFilter = base.gameObject.GetComponent<AudioLowPassFilter>();
			if (this._LoPassFilter == null)
			{
				this._LoPassFilter = base.gameObject.AddComponent<AudioLowPassFilter>();
			}
			return this._LoPassFilter;
		}
	}

	private AudioSource srcSteady;

	private AudioSource srcTrans;

	private AudioClip clipPickup;

	private AudioClip clipOther;

	private AudioLowPassFilter _LoPassFilter;

	private bool bInit;

	private bool bSteadyPlaying;

	private bool bSteady;

	private bool bTrans;

	private bool bPickup;

	private float fVolumeSteady = 1f;

	private float fVolumeTrans = 1f;

	private float fVolumePickup = 1f;

	private float fSteadyDelay;

	private float fTransDuration;

	private float fLoPassFreq = 5000f;

	private float fLoPassFreqOccluded = 5000f;

	private float fMaxDistance = 500f;

	private float fMinDistance = 20f;

	private float fSpatialBlend = 1f;

	private float fDopplerLevel;

	private float fFadeInVolumeStart = 1f;

	private float fFadeOutVolumeStart = 1f;

	public enum ClipType
	{
		STEADY,
		TRANS,
		PICKUP,
		OTHER
	}
}
