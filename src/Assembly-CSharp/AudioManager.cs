using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

// Global audio and music controller.
// This owns the shared mixer state, ship audio sources, UI button sounds, and
// the ambient music queue used by both the menu and gameplay scenes.
public class AudioManager : MonoBehaviour
{
	// Unity startup hook; delegates to the explicit initializer.
	private void Awake()
	{
		this.Init();
	}

	// Resets runtime audio caches and stores the singleton instance.
	private void Init()
	{
		AudioManager.am = this;
		this.srcMusic = base.transform.Find("srcMusic").GetComponent<AudioSource>();
		this.fTimeUntilNextFadeIn = this.fMusicGap;
		this.fTimeUntilFadeOut = this.fMusicGap;
		this.bFadingMusic = false;
		this.bLoadingMusic = false;
		AudioManager.bShutdown = false;
		this.dictShipAudio = new Dictionary<string, AudioSource>();
		this.dictVolumes = new Dictionary<string, float>();
		this.dictFadeOuts = new Dictionary<string, bool>();
	}

	// Switches the main audio mixer snapshot for atmosphere changes like vacuum or menu focus.
	public void MixerSet(AudioManager.MixerSnap ms, float fTransTime)
	{
		if (ms != AudioManager.MixerSnap.EVERYTHING)
		{
			if (ms != AudioManager.MixerSnap.EVERYTHING_VACUUM)
			{
				if (ms == AudioManager.MixerSnap.MUSIC_ONLY)
				{
					this.mixerMain.FindSnapshot("MusicOnly").TransitionTo(fTransTime);
				}
			}
			else
			{
				this.mixerMain.FindSnapshot("EverythingVacuum").TransitionTo(fTransTime);
			}
		}
		else
		{
			this.mixerMain.FindSnapshot("Everything").TransitionTo(fTransTime);
		}
	}

	// Writes a mixer parameter and notifies any UI listening for volume changes.
	public void SetMixerFloat(string strName, float fAmount)
	{
		this.mixerMain.SetFloat(strName, fAmount);
		AudioManager.AudioVolumeUpdated.Invoke();
	}

	// Queues a music tag for the next selection window.
	// Tags likely map to entries prepared in DataHandler's music registry.
	public void SuggestMusic(string strTag, bool bForce = false)
	{
		if (strTag == null)
		{
			return;
		}
		if (bForce || this.fTimeUntilNextFadeIn <= this.fGracePeriod)
		{
			this.strSuggestedTag = strTag;
			if (this.fTimeUntilNextFadeIn > 0.5f)
			{
				this.fTimeUntilNextFadeIn = 0.5f;
			}
		}
	}

	// Maps hardcoded station reg ids to regional music tags.
	public string GetStationTag(string strRegID)
	{
		if (strRegID != null)
		{
			if (strRegID == "OKLG")
			{
				return "StationAfrica";
			}
			if (strRegID == "VENC")
			{
				return "StationBrazil";
			}
			if (strRegID == "VNCA")
			{
				return "StationUSA";
			}
			if (strRegID == "VORB")
			{
				return "StationVORB";
			}
		}
		return null;
	}

	// Public wrapper that starts the asynchronous music load path.
	public void CueMusic(string strFile, float fDelay = 0f, float fFadeInTime = 0f)
	{
		base.StartCoroutine(this._CueMusic(strFile, fDelay, fFadeInTime));
	}

	// Loads a music clip from StreamingAssets, swaps the current clip, and schedules the next fade window.
	private IEnumerator _CueMusic(string strFile, float fDelay = 0f, float fFadeInTime = 0f)
	{
		if (this.bLoadingMusic)
		{
			Debug.Log(string.Concat(new object[]
			{
				"Already loading music (",
				StarSystem.fEpoch,
				"). Skipping: ",
				strFile
			}));
			yield break;
		}
		this.bLoadingMusic = true;
		string strFullPath = Application.streamingAssetsPath + "/audio/music/" + strFile;
		WWW www = new WWW("file:///" + strFullPath);
		Debug.Log(string.Concat(new object[]
		{
			"#Info# Attempting to load (",
			StarSystem.fEpoch,
			"): ",
			www.url
		}));
		this.fTimeUntilFadeOut = this.fGracePeriod * 2f;
		this.fTimeUntilNextFadeIn = this.fTimeUntilFadeOut + this.fMusicGap;
		yield return www;
		if (www.error != null)
		{
			Debug.Log(www.error);
		}
		else
		{
			if (this.srcMusic.clip != null)
			{
				this.srcMusic.Stop();
				AudioClip clip = this.srcMusic.clip;
				this.srcMusic.clip = null;
				clip.UnloadAudioData();
				UnityEngine.Object.DestroyImmediate(clip, false);
			}
			this.srcMusic.clip = www.GetAudioClip(false, false);
			if (this.srcMusic.loop)
			{
				this.fTimeUntilFadeOut = MathUtils.Rand(0.5f, 1.5f, MathUtils.RandType.Flat, null) * this.fMusicLoopLength;
				this.fTimeUntilNextFadeIn = this.fTimeUntilFadeOut + this.fMusicGap + MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null) * this.fMusicGap;
				this.srcMusic.time = this.srcMusic.clip.length * MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
				Debug.Log(string.Concat(new object[]
				{
					"Fading loop in. FadeNext: ",
					this.fTimeUntilFadeOut,
					"; MusicNext: ",
					this.fTimeUntilNextFadeIn,
					"; Position: ",
					this.srcMusic.time
				}));
			}
			else
			{
				this.fTimeUntilFadeOut = this.srcMusic.clip.length;
				float num = MathUtils.Rand(1f, 2f, MathUtils.RandType.Flat, null);
				this.fTimeUntilNextFadeIn = this.srcMusic.clip.length + num * this.fMusicGap;
				this.srcMusic.time = 0f;
				Debug.Log(string.Concat(new object[]
				{
					"#Info# m_fMusicNext = ",
					this.fTimeUntilNextFadeIn,
					" = ",
					this.srcMusic.clip.length,
					" + ",
					this.fMusicGap,
					" + ",
					num,
					" * ",
					this.fMusicGap
				}));
			}
			this.FadeInMusic(fFadeInTime, fDelay);
		}
		www.Dispose();
		this.bLoadingMusic = false;
		yield break;
	}

	// Starts a fade-out coroutine for the current music track.
	public void FadeOutMusic(float fDelay)
	{
		this.bFadingMusic = true;
		base.StartCoroutine(this._FadeOutMusic(fDelay));
	}

	// Reduces music volume over time, then stops and resets the music source.
	private IEnumerator _FadeOutMusic(float FadeTime)
	{
		float startVolume = this.srcMusic.volume;
		float fVol3DMusic = Mathf.Lerp(0f, AudioManager.VOLUME_MUTE_DB, this.srcMusic.volume / startVolume);
		this.SetMixerFloat("Vol3DMusic", fVol3DMusic);
		while (this.srcMusic.volume > 0f)
		{
			this.srcMusic.volume -= startVolume * Time.deltaTime / FadeTime;
			fVol3DMusic = Mathf.Lerp(0f, AudioManager.VOLUME_MUTE_DB, this.srcMusic.volume / startVolume);
			this.SetMixerFloat("Vol3DMusic", fVol3DMusic);
			yield return null;
		}
		this.srcMusic.Stop();
		this.srcMusic.volume = startVolume;
		fVol3DMusic = 0f;
		this.SetMixerFloat("Vol3DMusic", fVol3DMusic);
		this.bFadingMusic = false;
		yield break;
	}

	public void FadeInMusic(float fFadeInTime, float fDelay = 0f)
	{
		base.StartCoroutine(this._FadeInMusic(fFadeInTime, fDelay));
	}

	private IEnumerator _FadeInMusic(float FadeTime, float fDelay)
	{
		while (fDelay > 0f)
		{
			if (this.bFadingMusic)
			{
				yield break;
			}
			fDelay -= CrewSim.TimeElapsedUnscaled();
			yield return null;
		}
		this.srcMusic.Play();
		this.srcMusic.volume = 0f;
		float fVol3DMusic = Mathf.Lerp(0f, AudioManager.VOLUME_MUTE_DB, this.srcMusic.volume);
		this.SetMixerFloat("Vol3DMusic", fVol3DMusic);
		while (this.srcMusic.volume < 1f)
		{
			if (this.bFadingMusic)
			{
				yield break;
			}
			this.srcMusic.volume += Time.deltaTime / FadeTime;
			fVol3DMusic = Mathf.Lerp(0f, AudioManager.VOLUME_MUTE_DB, this.srcMusic.volume);
			this.SetMixerFloat("Vol3DMusic", fVol3DMusic);
			yield return null;
		}
		this.srcMusic.volume = 1f;
		fVol3DMusic = AudioManager.VOLUME_MUTE_DB;
		this.SetMixerFloat("Vol3DMusic", fVol3DMusic);
		yield break;
	}

	public void UpdateMusic()
	{
		this.fTimeUntilNextFadeIn -= CrewSim.TimeElapsedUnscaled();
		this.fTimeUntilFadeOut -= CrewSim.TimeElapsedUnscaled();
		if (this.srcMusic != null && this.srcMusic.isPlaying && !this.bFadingMusic && (((double)this.fTimeUntilFadeOut <= 0.0 && this.fTimeUntilFadeOut < this.fTimeUntilNextFadeIn) || this.fTimeUntilNextFadeIn <= this.fFadeDuration))
		{
			float num = Mathf.Min(this.fFadeDuration, this.fTimeUntilNextFadeIn);
			this.FadeOutMusic(num);
			Debug.Log(string.Concat(new object[]
			{
				"Fading music out Volume: ",
				this.srcMusic.volume,
				"; FadeNext: ",
				this.fTimeUntilFadeOut,
				"; MusicNext: ",
				this.fTimeUntilNextFadeIn,
				"; fMin: ",
				num
			}));
		}
		else if ((double)this.fTimeUntilNextFadeIn <= 0.0)
		{
			string text = null;
			if (!string.IsNullOrEmpty(this.strSuggestedTag))
			{
				text = DataHandler.GetTrackForTag(this.strSuggestedTag);
				this.strSuggestedTag = null;
			}
			if (text == null && CrewSim.coPlayer != null && CrewSim.coPlayer.ship != null)
			{
				string strRegID = CrewSim.coPlayer.ship.strRegID;
				string stationTag = this.GetStationTag(strRegID);
				if (!string.IsNullOrEmpty(stationTag))
				{
					text = DataHandler.GetTrackForTag(stationTag);
				}
			}
			if (text == null)
			{
				text = DataHandler.GetTrackForTag("Explore");
			}
			if (text == null || !DataHandler.dictMusic.ContainsKey(text))
			{
				return;
			}
			JsonMusic jsonMusic = DataHandler.dictMusic[text];
			bool bLoop = jsonMusic.bLoop;
			this.srcMusic.loop = jsonMusic.bLoop;
			if (CrewSim.coPlayer != null && CrewSim.coPlayer.HasCond("TutorialZonesNoDorm"))
			{
				this.fTimeUntilNextFadeIn = this.fMusicGap;
				return;
			}
			if (bLoop)
			{
				this.CueMusic(text, 0f, this.fFadeDuration);
			}
			else
			{
				this.CueMusic(text, 0f, 0f);
			}
		}
	}

	private IEnumerator crFadeOutSteady(AudioSource srcSteady, string strJAE)
	{
		if (srcSteady.clip == null)
		{
			yield break;
		}
		float fDuration = 0.1f;
		float fTimePassed = 0f;
		while (fTimePassed <= fDuration)
		{
			if (!this.dictFadeOuts.ContainsKey(strJAE))
			{
				yield break;
			}
			if (!this.dictFadeOuts[strJAE])
			{
				yield break;
			}
			fTimePassed += Time.deltaTime;
			float fBlend = Mathf.Clamp01(fTimePassed / fDuration);
			srcSteady.volume = Mathf.Lerp(srcSteady.volume, 0f, fBlend);
			yield return null;
		}
		srcSteady.Stop();
		this.dictFadeOuts[strJAE] = false;
		yield break;
	}

	public void StopAudioEmitter(string strJAE)
	{
		if (strJAE == null)
		{
			return;
		}
		AudioSource srcSteady = null;
		if (this.dictShipAudio.TryGetValue(strJAE, out srcSteady) && !this.dictFadeOuts[strJAE])
		{
			this.dictFadeOuts[strJAE] = true;
			base.StartCoroutine(this.crFadeOutSteady(srcSteady, strJAE));
			return;
		}
	}

	public void StopAllAudioEmitters()
	{
		foreach (KeyValuePair<string, AudioSource> keyValuePair in this.dictShipAudio)
		{
			if (!this.dictFadeOuts[keyValuePair.Key])
			{
				this.dictFadeOuts[keyValuePair.Key] = true;
				base.StartCoroutine(this.crFadeOutSteady(keyValuePair.Value, keyValuePair.Key));
			}
		}
	}

	public void PlayAudioEmitterAtVol(string strJAE, bool bLoop, bool bNoRestart = false, float volume = 1f)
	{
		if (AudioManager.bShutdown || strJAE == null)
		{
			return;
		}
		AudioSource audioSource = null;
		if (!this.dictShipAudio.TryGetValue(strJAE, out audioSource))
		{
			audioSource = this.CreateAudioEmitter(strJAE, bLoop);
			audioSource.volume = volume;
			audioSource.Play();
			return;
		}
		if (audioSource.isPlaying && (audioSource.loop || bNoRestart))
		{
			return;
		}
		audioSource.volume = volume;
		audioSource.Play();
		this.dictFadeOuts[strJAE] = false;
	}

	public void PlayAudioEmitter(string strJAE, bool bLoop, bool bNoRestart = false)
	{
		if (AudioManager.bShutdown || strJAE == null)
		{
			return;
		}
		AudioSource audioSource = null;
		if (!this.dictShipAudio.TryGetValue(strJAE, out audioSource))
		{
			audioSource = this.CreateAudioEmitter(strJAE, bLoop);
			audioSource.Play();
			return;
		}
		if (audioSource.isPlaying && (audioSource.loop || bNoRestart))
		{
			return;
		}
		audioSource.volume = this.dictVolumes[strJAE];
		audioSource.Play();
		this.dictFadeOuts[strJAE] = false;
	}

	public AudioSource CreateAudioEmitter(string strJAE, bool bLoop)
	{
		JsonAudioEmitter audioEmitter = DataHandler.GetAudioEmitter(strJAE);
		bool flag = audioEmitter != null && (audioEmitter.fLoPassFreq >= 0f || audioEmitter.fLoPassFreqOccluded >= 0f);
		GameObject gameObject;
		if (flag)
		{
			gameObject = new GameObject(strJAE);
			gameObject.transform.SetParent(base.transform);
		}
		else
		{
			gameObject = base.gameObject;
		}
		AudioSource audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.loop = bLoop;
		audioSource.playOnAwake = false;
		audioSource.minDistance = 20f;
		audioSource.maxDistance = 500f;
		audioSource.spatialBlend = 0f;
		audioSource.dopplerLevel = 0f;
		audioSource.clip = Resources.Load<AudioClip>("Audio/blank");
		if (audioEmitter != null)
		{
			if (audioEmitter.strClipSteady != null)
			{
				audioSource.clip = Resources.Load<AudioClip>("Audio/" + audioEmitter.strClipSteady);
			}
			AudioSource audioSource2 = audioSource;
			float fVolumeSteady = audioEmitter.fVolumeSteady;
			this.dictVolumes[strJAE] = fVolumeSteady;
			audioSource2.volume = fVolumeSteady;
			if (audioEmitter.fSpatialBlend >= 0f && audioEmitter.fSpatialBlend <= 1f)
			{
				audioSource.spatialBlend = audioEmitter.fSpatialBlend;
			}
			if (flag)
			{
				AudioLowPassFilter audioLowPassFilter = gameObject.GetComponent<AudioLowPassFilter>();
				if (audioLowPassFilter == null)
				{
					audioLowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
				}
				if (audioEmitter.fLoPassFreq >= 0f)
				{
					audioLowPassFilter.cutoffFrequency = audioEmitter.fLoPassFreq;
				}
			}
			AudioMixerGroup outputAudioMixerGroup = null;
			if (AudioManager.MixerGroups.TryGetValue(audioEmitter.strMixerName, out outputAudioMixerGroup))
			{
				audioSource.outputAudioMixerGroup = outputAudioMixerGroup;
			}
		}
		this.dictShipAudio[strJAE] = audioSource;
		this.dictVolumes[strJAE] = audioSource.volume;
		this.dictFadeOuts[strJAE] = false;
		return audioSource;
	}

	public void TweakAudioEmitter(string strJAE, float fPitch, float fVolumeMod)
	{
		if (AudioManager.bShutdown || strJAE == null)
		{
			return;
		}
		AudioSource audioSource = null;
		if (this.dictShipAudio.TryGetValue(strJAE, out audioSource))
		{
			audioSource.pitch = fPitch;
			audioSource.volume = this.dictVolumes[strJAE] * fVolumeMod;
			return;
		}
	}

	public void LerpAudioTo(AudioSource src, float fVolume, double fTime)
	{
		if (!src.isPlaying)
		{
			return;
		}
		if (fVolume == 0f && (double)src.volume < 0.05)
		{
			src.Stop();
			return;
		}
		src.volume = Mathf.Lerp(src.volume, fVolume, (float)fTime);
	}

	private void UpdateAtmoEffects()
	{
		if (AudioManager.bShutdown)
		{
			return;
		}
		float fAmount = Mathf.Lerp(-7f, 0f, this.fEnvPressure);
		float num = Mathf.Lerp(676f, 22000f, this.fEnvPressure);
		float fAmount2 = AudioManager.VOLUME_MUTE_DB;
		float fAmount3 = num;
		float fAmount4 = Mathf.Lerp(-7f, 0f, this.fWeatherFilter);
		float fAmount5 = Mathf.Lerp(676f, 22000f, this.fWeatherFilter);
		if (this.hsHelmet == GUIHelmet.HelmetStyle.Damaged)
		{
			fAmount2 = Mathf.Lerp(-7f, 0f, this.fEnvPressure);
		}
		else if (this.hsHelmet != GUIHelmet.HelmetStyle.None)
		{
			fAmount2 = Mathf.Lerp(-7f, 0f, this.fEnvPressure);
			fAmount = -7f;
			num = 676f;
		}
		else
		{
			fAmount2 = AudioManager.VOLUME_MUTE_DB;
		}
		if (!this.bGrounded && (double)this.fWeatherFilter < 0.01)
		{
			fAmount = AudioManager.VOLUME_MUTE_DB;
		}
		this.SetMixerFloat("VolAmbient", fAmount);
		this.SetMixerFloat("LoPassFreqAmbient", num);
		this.SetMixerFloat("VolCharacter", fAmount2);
		this.SetMixerFloat("LoPassFreqCharacter", fAmount3);
		this.SetMixerFloat("VolWeather", fAmount4);
		this.SetMixerFloat("LoPassFreqWeather", fAmount5);
	}

	public void StopWindAudio()
	{
		this.StopAudioEmitter("ShipWindStill");
		this.StopAudioEmitter("ShipWindLow");
		this.StopAudioEmitter("ShipWindMed");
		this.StopAudioEmitter("ShipWindHigh");
		this.StopAudioEmitter("ShipWindReentry");
	}

	public void PlayWindAudio(float fSpeed, float fVolume, double fTime)
	{
		if (!this.bWindInit)
		{
			this.bWindInit = true;
			JsonAudioEmitter audioEmitter = DataHandler.GetAudioEmitter("ShipWindStill");
			JsonAudioEmitter audioEmitter2 = DataHandler.GetAudioEmitter("ShipWindLow");
			JsonAudioEmitter audioEmitter3 = DataHandler.GetAudioEmitter("ShipWindMed");
			JsonAudioEmitter audioEmitter4 = DataHandler.GetAudioEmitter("ShipWindHigh");
			if (audioEmitter != null && audioEmitter2 != null && audioEmitter3 != null && audioEmitter4 != null)
			{
				this.srcWindStill = this.CreateAudioEmitter("ShipWindStill", true);
				this.srcWindLow = this.CreateAudioEmitter("ShipWindLow", true);
				this.srcWindMid = this.CreateAudioEmitter("ShipWindMed", true);
				this.srcWindHigh = this.CreateAudioEmitter("ShipWindHigh", true);
				this.srcWindReentry = this.CreateAudioEmitter("ShipWindReentry", false);
				this.fVolMaxWindStill = audioEmitter.fVolumeSteady;
				this.fVolMaxWindLow = audioEmitter2.fVolumeSteady;
				this.fVolMaxWindMid = audioEmitter3.fVolumeSteady;
				this.fVolMaxWindHigh = audioEmitter4.fVolumeSteady;
			}
		}
		if (this.srcWindMid == null)
		{
			return;
		}
		AudioSource audioSource;
		AudioSource audioSource2;
		float num;
		float num2;
		float num3;
		if (fSpeed < 0.02f)
		{
			audioSource = this.srcWindStill;
			audioSource2 = this.srcWindLow;
			num = this.fVolMaxWindStill;
			num2 = this.fVolMaxWindLow;
			this.LerpAudioTo(this.srcWindMid, 0f, fTime);
			this.LerpAudioTo(this.srcWindHigh, 0f, fTime);
			num3 = 4f * fSpeed - 1f;
		}
		else if (fSpeed < 0.5f)
		{
			audioSource = this.srcWindLow;
			audioSource2 = this.srcWindMid;
			num = this.fVolMaxWindLow;
			num2 = this.fVolMaxWindMid;
			this.LerpAudioTo(this.srcWindStill, 0f, fTime);
			this.LerpAudioTo(this.srcWindHigh, 0f, fTime);
			num3 = 4f * (fSpeed - 0.1f) - 1f;
		}
		else
		{
			audioSource = this.srcWindMid;
			audioSource2 = this.srcWindHigh;
			num = this.fVolMaxWindMid;
			num2 = this.fVolMaxWindHigh;
			this.LerpAudioTo(this.srcWindStill, 0f, fTime);
			this.LerpAudioTo(this.srcWindLow, 0f, fTime);
			num3 = 4f * (fSpeed - 0.5f) - 1f;
		}
		if ((double)fSpeed >= 1.0)
		{
			if ((double)Time.unscaledTime - this.fTimeLastReentry > 60.0)
			{
				this.fTimeLastReentry = (double)Time.unscaledTime;
				this.srcWindReentry.time = 0f;
				this.srcWindReentry.Play();
				this.LerpAudioTo(this.srcWindReentry, 1f, fTime / 4.0);
			}
		}
		else
		{
			this.LerpAudioTo(this.srcWindReentry, 0f, fTime);
		}
		if (num3 >= 1f)
		{
			num3 = 0.999999f;
		}
		else if (num3 <= -1f)
		{
			num3 = -0.999999f;
		}
		if (!audioSource.isPlaying)
		{
			audioSource.Play();
			audioSource.volume = 0f;
			audioSource.time = audioSource.clip.length * MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
		}
		if (!audioSource2.isPlaying)
		{
			audioSource2.Play();
			audioSource2.volume = 0f;
			audioSource2.time = audioSource2.clip.length * MathUtils.Rand(0f, 1f, MathUtils.RandType.Flat, null);
		}
		float fVolume2 = Mathf.Sqrt(0.5f * (1f - num3) * fVolume) * num;
		float fVolume3 = Mathf.Sqrt(0.5f * (1f + num3) * fVolume) * num2;
		this.LerpAudioTo(audioSource, fVolume2, fTime);
		this.LerpAudioTo(audioSource2, fVolume3, fTime);
	}

	public void ShutDown()
	{
		this.FadeOutMusic(1f);
		this.StopAllAudioEmitters();
		this.Helmet = GUIHelmet.HelmetStyle.None;
		this.EnvPressure = 1f;
		this.WeatherFilter = 1f;
		this.Grounded = true;
		AudioManager.bShutdown = true;
	}

	public static void AddBtnAudio(GameObject go, string strDown, string strUp)
	{
		if (go == null)
		{
			return;
		}
		GUIAudioBtn guiaudioBtn = go.GetComponent<GUIAudioBtn>();
		if (guiaudioBtn == null)
		{
			guiaudioBtn = go.AddComponent<GUIAudioBtn>();
		}
		guiaudioBtn.strAudioEmitterDown = strDown;
		guiaudioBtn.strAudioEmitterUp = strUp;
	}

	public void PlayCreakAudio(string strLoot)
	{
		Loot loot = DataHandler.GetLoot(strLoot);
		if (loot == null)
		{
			return;
		}
		string lootNameSingle = loot.GetLootNameSingle("SHIP_CREAK");
		this.PlayAudioEmitter(lootNameSingle, false, true);
		this.TweakAudioEmitter(lootNameSingle, 0.85f - MathUtils.Rand(0f, 0.15f, MathUtils.RandType.Flat, null), MathUtils.Rand(0.5f, 1f, MathUtils.RandType.Flat, null));
	}

	public GUIHelmet.HelmetStyle Helmet
	{
		get
		{
			return this.hsHelmet;
		}
		set
		{
			if (AudioManager.bShutdown || this.hsHelmet == value)
			{
				return;
			}
			this.hsHelmet = value;
			this.UpdateAtmoEffects();
		}
	}

	public bool Grounded
	{
		get
		{
			return this.bGrounded;
		}
		set
		{
			if (this.bGrounded == value)
			{
				return;
			}
			this.bGrounded = value;
			this.UpdateAtmoEffects();
		}
	}

	public float EnvPressure
	{
		get
		{
			return this.fEnvPressure;
		}
		set
		{
			if (this.fEnvPressure == value)
			{
				return;
			}
			this.fEnvPressure = value;
			this.UpdateAtmoEffects();
		}
	}

	public float WeatherFilter
	{
		get
		{
			return this.fWeatherFilter;
		}
		set
		{
			if (this.fWeatherFilter == value)
			{
				return;
			}
			this.fWeatherFilter = value;
			this.UpdateAtmoEffects();
		}
	}

	public static Dictionary<string, AudioMixerGroup> MixerGroups
	{
		get
		{
			if (AudioManager.dictMixerGroups == null)
			{
				AudioManager.dictMixerGroups = new Dictionary<string, AudioMixerGroup>();
				List<AudioMixerGroup> list = new List<AudioMixerGroup>();
				list.AddRange(AudioManager.am.mixerMain.FindMatchingGroups(string.Empty));
				foreach (AudioMixerGroup audioMixerGroup in list)
				{
					AudioManager.dictMixerGroups[audioMixerGroup.name] = audioMixerGroup;
				}
			}
			return AudioManager.dictMixerGroups;
		}
	}

	public static readonly UnityEvent AudioVolumeUpdated = new UnityEvent();

	public static AudioManager am;

	private static Dictionary<string, AudioMixerGroup> dictMixerGroups;

	public static bool bShutdown = false;

	public static bool bIgnoreCOTrans = false;

	public AudioMixer mixerMain;

	private GUIHelmet.HelmetStyle hsHelmet = GUIHelmet.HelmetStyle.Unpowered;

	private bool bGrounded = true;

	private float fEnvPressure = 1f;

	private float fWeatherFilter = 1f;

	private const float LOPASS_MAX_HZ = 22000f;

	private const float LOPASS_MIN_HZ = 676f;

	private const float VOLUME_VACUUM_DB = -7f;

	private const float VOLUME_NORMAL_DB = 0f;

	private static float VOLUME_MUTE_DB = Mathf.Log(0.001f) * 20f;

	private AudioSource srcWindStill;

	private AudioSource srcWindLow;

	private AudioSource srcWindMid;

	private AudioSource srcWindHigh;

	private AudioSource srcWindReentry;

	private float fVolMaxWindStill = 1f;

	private float fVolMaxWindLow = 1f;

	private float fVolMaxWindMid = 1f;

	private float fVolMaxWindHigh = 1f;

	private double fTimeLastReentry;

	private bool bWindInit;

	private AudioSource srcMusic;

	private float fFadeDuration = 10f;

	private float fTimeUntilNextFadeIn;

	private float fTimeUntilFadeOut;

	private bool bFadingMusic;

	private bool bLoadingMusic;

	private float fMusicGap = 200f;

	private float fMusicLoopLength = 120f;

	private float fGracePeriod = 60f;

	private string strSuggestedTag;

	private Dictionary<string, AudioSource> dictShipAudio;

	private Dictionary<string, float> dictVolumes;

	private Dictionary<string, bool> dictFadeOuts;

	public enum MixerSnap
	{
		EVERYTHING,
		EVERYTHING_VACUUM,
		MUSIC_ONLY
	}
}
