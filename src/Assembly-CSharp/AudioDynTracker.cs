using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AudioDynTracker : MonoBehaviour
{
	private void Start()
	{
		this.fIntroDur = this.fBeat * 16f;
		this.fMaxSilenceDur = this.fBeat * 160f;
		this.strAudioPath = Application.streamingAssetsPath + "/audio/";
		this.mapClips = new Dictionary<string, AudioClip>();
		this.aDRUMs = new List<string>();
		this.aBASSs = new List<string>();
		this.aFXs = new List<string>();
		this.aMELODYs = new List<string>();
		this.aPADs = new List<string>();
		this.aPERCs = new List<string>();
		this.aLastClips = new List<string>();
		this.aFlips = new List<int>
		{
			0,
			0,
			0,
			0,
			0,
			0
		};
		this.aSrcDRUM = new List<AudioSource>
		{
			base.gameObject.AddComponent<AudioSource>(),
			base.gameObject.AddComponent<AudioSource>()
		};
		this.aSrcBASS = new List<AudioSource>
		{
			base.gameObject.AddComponent<AudioSource>(),
			base.gameObject.AddComponent<AudioSource>()
		};
		this.aSrcFX = new List<AudioSource>
		{
			base.gameObject.AddComponent<AudioSource>(),
			base.gameObject.AddComponent<AudioSource>()
		};
		this.aSrcMELODY = new List<AudioSource>
		{
			base.gameObject.AddComponent<AudioSource>(),
			base.gameObject.AddComponent<AudioSource>()
		};
		this.aSrcPAD = new List<AudioSource>
		{
			base.gameObject.AddComponent<AudioSource>(),
			base.gameObject.AddComponent<AudioSource>()
		};
		this.aSrcPERC = new List<AudioSource>
		{
			base.gameObject.AddComponent<AudioSource>(),
			base.gameObject.AddComponent<AudioSource>()
		};
		this.mapPlayChanceParams = new Dictionary<string, float[]>();
		this.mapSwitchChanceParams = new Dictionary<string, float[]>();
		this.mapPlayChanceParams["drum"] = new float[]
		{
			0.125f,
			0.9f
		};
		this.mapPlayChanceParams["bass"] = new float[]
		{
			0.9f,
			0.9f
		};
		this.mapPlayChanceParams["FX"] = new float[]
		{
			0f,
			0.125f
		};
		this.mapPlayChanceParams["melody"] = new float[]
		{
			0f,
			0.125f
		};
		this.mapPlayChanceParams["pad"] = new float[]
		{
			0.125f,
			0.5f
		};
		this.mapPlayChanceParams["perc"] = new float[]
		{
			0.75f,
			0.5f
		};
		this.mapSwitchChanceParams["drum"] = new float[]
		{
			0.25f,
			0.25f
		};
		this.mapSwitchChanceParams["bass"] = new float[]
		{
			0.25f,
			0.25f
		};
		this.mapSwitchChanceParams["FX"] = new float[]
		{
			0.5f,
			0.5f
		};
		this.mapSwitchChanceParams["melody"] = new float[]
		{
			0.5f,
			0.5f
		};
		this.mapSwitchChanceParams["pad"] = new float[]
		{
			0.5f,
			0.5f
		};
		this.mapSwitchChanceParams["perc"] = new float[]
		{
			0.5f,
			0.5f
		};
		double item = AudioSettings.dspTime + 1.0;
		this.aNextEventTimes = new List<double>();
		int num = 0;
		foreach (string key in this.mapPlayChanceParams.Keys)
		{
			this.aLastClips.Add(null);
			this.aNextEventTimes.Add(item);
			float num2 = this.mapPlayChanceParams[key][0];
			for (float num3 = 0f; num3 < this.fMaxSilenceDur; num3 += this.fBeat)
			{
				if (UnityEngine.Random.value < num2)
				{
					break;
				}
				List<double> list;
				int index;
				(list = this.aNextEventTimes)[index = num] = list[index] + (double)this.fBeat;
			}
			num++;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(this.strAudioPath);
		FileInfo[] files = directoryInfo.GetFiles("*.ogg");
		foreach (FileInfo fileInfo in files)
		{
			base.StartCoroutine(this.LoadAudio("file://" + this.strAudioPath, fileInfo.Name));
		}
	}

	private void Update()
	{
		if (this.bAllTracksReady)
		{
			this.CheckAudio2("drum", this.aDRUMs, this.aSrcDRUM, 0);
			this.CheckAudio2("bass", this.aBASSs, this.aSrcBASS, 1);
			this.CheckAudio2("FX", this.aFXs, this.aSrcFX, 2);
			this.CheckAudio2("melody", this.aMELODYs, this.aSrcMELODY, 3);
			this.CheckAudio2("pad", this.aPADs, this.aSrcPAD, 4);
			this.CheckAudio2("perc", this.aPERCs, this.aSrcPERC, 5);
		}
	}

	private void CheckAudio2(string strKey, List<string> aList, List<AudioSource> aSrc, int nIndex)
	{
		double dspTime = AudioSettings.dspTime;
		if (dspTime + 1.0 > this.aNextEventTimes[nIndex])
		{
			float num = this.mapPlayChanceParams[strKey][0];
			float num2 = this.mapSwitchChanceParams[strKey][0];
			if (this.aNextEventTimes[nIndex] >= (double)this.fIntroDur)
			{
				num = this.mapPlayChanceParams[strKey][1];
				num2 = this.mapSwitchChanceParams[strKey][1];
			}
			string text = this.aLastClips[nIndex];
			if (text == null || UnityEngine.Random.value <= num2)
			{
				int index = Mathf.RoundToInt(UnityEngine.Random.value * (float)(aList.Count - 1));
				text = aList[index];
			}
			aSrc[this.aFlips[nIndex]].clip = this.mapClips[text];
			this.aLastClips[nIndex] = text;
			aSrc[this.aFlips[nIndex]].PlayScheduled(this.aNextEventTimes[nIndex]);
			Debug.Log(string.Concat(new object[]
			{
				"Scheduled source ",
				text,
				" to start at time ",
				this.aNextEventTimes[nIndex]
			}));
			List<double> list;
			(list = this.aNextEventTimes)[nIndex] = list[nIndex] + (double)aSrc[this.aFlips[nIndex]].clip.length;
			for (float num3 = 0f; num3 < this.fMaxSilenceDur; num3 += this.fBeat)
			{
				if (UnityEngine.Random.value < num)
				{
					break;
				}
				(list = this.aNextEventTimes)[nIndex] = list[nIndex] + (double)this.fBeat;
			}
			this.aFlips[nIndex] = 1 - this.aFlips[nIndex];
		}
	}

	private IEnumerator FadeOut(AudioSource audioSource, float FadeTime)
	{
		float startVolume = audioSource.volume;
		while (audioSource.volume > 0f)
		{
			audioSource.volume -= startVolume * Time.deltaTime / FadeTime;
			yield return null;
		}
		audioSource.Stop();
		audioSource.volume = startVolume;
		yield break;
	}

	private IEnumerator LoadAudio(string strPath, string strName)
	{
		WWW www = new WWW(strPath + strName);
		Debug.Log("clip loading: " + strPath + strName);
		yield return www;
		if (www.error != null)
		{
			Debug.Log(www.error);
		}
		else if (www.progress == 1f)
		{
			this.mapClips[strName] = www.GetAudioClip();
			if (strName.Contains("drum"))
			{
				this.aDRUMs.Add(strName);
			}
			else if (strName.Contains("bass"))
			{
				this.aBASSs.Add(strName);
			}
			else if (strName.Contains("FX"))
			{
				this.aFXs.Add(strName);
			}
			else if (strName.Contains("melody"))
			{
				this.aMELODYs.Add(strName);
			}
			else if (strName.Contains("pad"))
			{
				this.aPADs.Add(strName);
			}
			else if (strName.Contains("perc"))
			{
				this.aPERCs.Add(strName);
			}
		}
		this.bAllTracksReady = (this.aDRUMs.Count > 0 && this.aBASSs.Count > 0 && this.aFXs.Count > 0 && this.aMELODYs.Count > 0 && this.aPADs.Count > 0 && this.aPERCs.Count > 0);
		yield break;
	}

	private List<string> aDRUMs;

	private List<string> aBASSs;

	private List<string> aFXs;

	private List<string> aMELODYs;

	private List<string> aPADs;

	private List<string> aPERCs;

	private List<AudioSource> aSrcDRUM;

	private List<AudioSource> aSrcBASS;

	private List<AudioSource> aSrcFX;

	private List<AudioSource> aSrcMELODY;

	private List<AudioSource> aSrcPAD;

	private List<AudioSource> aSrcPERC;

	private List<double> aNextEventTimes;

	private List<int> aFlips;

	private List<string> aLastClips;

	private Dictionary<string, AudioClip> mapClips;

	private float fBeat = 0.5555556f;

	private string strAudioPath;

	private bool bAllTracksReady;

	private Dictionary<string, float[]> mapPlayChanceParams;

	private Dictionary<string, float[]> mapSwitchChanceParams;

	private float fIntroDur;

	private float fMaxSilenceDur;
}
