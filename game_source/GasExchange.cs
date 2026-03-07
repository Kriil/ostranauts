using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class GasExchange : MonoBehaviour, IManUpdater
{
	private void Awake()
	{
		this.co = base.GetComponent<CondOwner>();
		if (!GasExchange.bLoadedAEs)
		{
			GasExchange.jaeLow = DataHandler.GetAudioEmitter("ShipAirLeakLow");
			GasExchange.jaeMid = DataHandler.GetAudioEmitter("ShipAirLeakMid");
			GasExchange.jaeHigh = DataHandler.GetAudioEmitter("ShipAirLeakHigh");
			GasExchange._ctRoom = DataHandler.GetCondTrigger("TIsRoom");
			GasExchange._ctRoom.logReason = false;
			GasExchange._ctGasBlocker = DataHandler.GetCondTrigger("TIsGasExchangeBlocker");
			GasExchange._ctGasBlocker.logReason = false;
			GasExchange.bLoadedAEs = true;
		}
		if (GasExchange.jaeLow != null && GasExchange.jaeMid != null && GasExchange.jaeHigh != null)
		{
			this.srcLow = base.gameObject.AddComponent<AudioSource>();
			this.srcMid = base.gameObject.AddComponent<AudioSource>();
			this.srcHigh = base.gameObject.AddComponent<AudioSource>();
			this.fVolumeMaxLow = GasExchange.jaeLow.fVolumeSteady;
			this.fVolumeMaxMid = GasExchange.jaeMid.fVolumeSteady;
			this.fVolumeMaxHigh = GasExchange.jaeHigh.fVolumeSteady;
			this.SetAudioData(this.srcLow, GasExchange.jaeLow);
			this.SetAudioData(this.srcMid, GasExchange.jaeMid);
			this.SetAudioData(this.srcHigh, GasExchange.jaeHigh);
		}
	}

	private void Update()
	{
		double num = StarSystem.fEpoch - this.fTimeOfLastCheck;
		if (num < this.fCheckPeriod)
		{
			return;
		}
		if (this.co == null || this.co.ship == null || CrewSim.system == null)
		{
			return;
		}
		if (num >= this.fCheckPeriod * 10.0)
		{
			num = this.fCheckPeriod;
		}
		this.DoGas(num);
		this.CatchUp();
	}

	public void UpdateManual()
	{
		this.Update();
	}

	public void CatchUp()
	{
		this.fTimeOfLastCheck = StarSystem.fEpoch;
	}

	public static double GetStatVolume(CondOwner co)
	{
		if (co == null)
		{
			return double.PositiveInfinity;
		}
		double num = co.GetCondAmount("StatVolume");
		if (num <= 0.0)
		{
			num = double.PositiveInfinity;
		}
		return num;
	}

	public static double GetStatGasTemp(CondOwner co, bool trueValue = false)
	{
		if (co == null)
		{
			return 0.0;
		}
		double num = co.GetCondAmount("StatGasTemp");
		GasContainer gasContainer = co.GasContainer;
		if (gasContainer != null)
		{
			num += gasContainer.fDGasTemp;
		}
		if (num < 1.0)
		{
			return (!trueValue) ? 1.0 : num;
		}
		int num2 = 100000000;
		if (double.IsNaN(num) || num > (double)num2)
		{
			return 293.0;
		}
		return num;
	}

	public static double GetStatGasPressure(CondOwner co)
	{
		if (co == null)
		{
			return 0.0;
		}
		return co.GetCondAmount("StatGasPressure");
	}

	private void MergeKeys(ref Dictionary<string, double> mapGasMolsA, ref Dictionary<string, double> mapGasMolsB)
	{
		foreach (KeyValuePair<string, double> keyValuePair in mapGasMolsB)
		{
			if (!mapGasMolsA.ContainsKey(keyValuePair.Key))
			{
				mapGasMolsA[keyValuePair.Key] = 0.0;
			}
		}
	}

	private bool IsBlocked(Vector2 tilePos)
	{
		if (float.IsInfinity(tilePos.x) || float.IsInfinity(tilePos.y))
		{
			return false;
		}
		Tile tileAtWorldCoords = this.co.ship.GetTileAtWorldCoords1(tilePos.x, tilePos.y, true, false);
		if (tileAtWorldCoords == null || tileAtWorldCoords.coProps == null)
		{
			return true;
		}
		if (tileAtWorldCoords.IsWall || tileAtWorldCoords.IsPortal)
		{
			this._blockingCOs.Clear();
			this.co.ship.GetCOsAtWorldCoords1(this.ptA, GasExchange._ctGasBlocker, true, false, this._blockingCOs);
			if (this._blockingCOs.Count > 0)
			{
				this._blockingCOs.Clear();
				return true;
			}
		}
		return false;
	}

	private void FixForVentSensors()
	{
		this.checkedGasPS = true;
		this.gasPS = base.GetComponent<GasPressureSense>();
		if (!this.gasPS)
		{
			return;
		}
		this.gasPS.FixForVentSensors();
		this.doNotExchange = this.gasPS.doNotExchange;
	}

	private void DoGas(double fTime)
	{
		if (this.co.ship == null)
		{
			return;
		}
		if (!this.checkedGasPS)
		{
			this.FixForVentSensors();
		}
		if (this.doNotExchange)
		{
			return;
		}
		CondOwner condOwner = null;
		CondOwner condOwner2 = null;
		Room room = null;
		Room room2 = null;
		this.ptA = this.co.GetPos(this.strPointA, false);
		this.ptB = this.co.GetPos(this.strPointB, false);
		if (this.strPointA == "self")
		{
			condOwner = this.co;
		}
		else
		{
			room = this.co.ship.GetRoomAtWorldCoords1(this.ptA, true);
			if (room != null)
			{
				condOwner = room.CO;
			}
			if (this.IsBlocked(this.ptA))
			{
				return;
			}
		}
		if (this.strPointB == "self")
		{
			condOwner2 = this.co;
		}
		else
		{
			room2 = this.co.ship.GetRoomAtWorldCoords1(this.ptB, true);
			if (room2 != null)
			{
				condOwner2 = room2.CO;
			}
			if (this.IsBlocked(this.ptB))
			{
				return;
			}
		}
		if (condOwner == condOwner2)
		{
			return;
		}
		if (condOwner == null || condOwner2 == null)
		{
			if (!(condOwner == null) || (!float.IsInfinity(this.ptA.x) && !float.IsInfinity(this.ptA.y)))
			{
				if (!(condOwner2 == null) || (!float.IsInfinity(this.ptB.x) && !float.IsInfinity(this.ptB.y)))
				{
					return;
				}
			}
		}
		double statVolume = GasExchange.GetStatVolume(condOwner);
		double statGasTemp = GasExchange.GetStatGasTemp(condOwner, false);
		double statVolume2 = GasExchange.GetStatVolume(condOwner2);
		double statGasTemp2 = GasExchange.GetStatGasTemp(condOwner2, false);
		if (double.IsPositiveInfinity(statVolume) && double.IsPositiveInfinity(statVolume2))
		{
			return;
		}
		if (double.IsPositiveInfinity(statVolume) && room2 != null && room2.Void)
		{
			return;
		}
		if (double.IsPositiveInfinity(statVolume2) && room != null && room.Void)
		{
			return;
		}
		bool flag = room2 != null && room2.Void;
		bool flag2 = room != null && room.Void;
		if (double.IsPositiveInfinity(statVolume) || flag2)
		{
			Vector2 vector = this.ptA;
			this.ptA = this.ptB;
			this.ptB = vector;
			this.CalcMoleDiff(statGasTemp2, statVolume2, statVolume, condOwner2, flag, condOwner, flag2, this.fWall, fTime);
		}
		else
		{
			this.CalcMoleDiff(statGasTemp, statVolume, statVolume2, condOwner, flag2, condOwner2, flag, this.fWall, fTime);
		}
	}

	private void CalcMoleDiff(double fTempA, double fVolA, double fVolB, CondOwner coA, bool roomAIsVoid, CondOwner coB, bool roomBIsVoid, float fWall, double fTime)
	{
		if (coA == null || double.IsPositiveInfinity(fVolA) || double.IsNaN(fVolA))
		{
			return;
		}
		Dictionary<string, double> dictionary = GasExchange.GasMolsVoid;
		Dictionary<string, double> dictionary2 = GasExchange.GasMolsVoid;
		GasContainer gasContainer = coA.GasContainer;
		GasContainer gasContainer2 = null;
		if (coB != null)
		{
			gasContainer2 = coB.GasContainer;
		}
		double statGasTemp = GasExchange.GetStatGasTemp(coB, false);
		if (gasContainer != null)
		{
			dictionary = gasContainer.mapGasMols1;
		}
		if (gasContainer2 != null)
		{
			dictionary2 = gasContainer2.mapGasMols1;
		}
		this.MergeKeys(ref dictionary, ref dictionary2);
		this.MergeKeys(ref dictionary2, ref dictionary);
		double num = fTime * (double)fWall;
		if (num > 0.5)
		{
			num = 0.5;
		}
		double num2 = 0.0;
		double num3 = 0.0;
		foreach (KeyValuePair<string, double> keyValuePair in dictionary)
		{
			string key = keyValuePair.Key;
			if (!(key == "StatGasMolTotal"))
			{
				double num4 = dictionary[key];
				double num5 = dictionary2[key];
				double num6 = num4 * 0.008314000442624092 * fTempA / fVolA;
				double num7 = (num4 + num5) * 0.008314000442624092 * fTempA / (fVolA + fVolB);
				double val = 9.999999960041972E-12;
				num6 = Math.Max(num6, val);
				num7 = Math.Max(num7, val);
				double num8 = num7 - num6;
				if (num8 >= 0.001 || num8 <= -0.001)
				{
					num8 *= num;
					double num9 = (num6 + num8) * fVolA / (0.008314000442624092 * fTempA);
					if (double.IsNaN(num9))
					{
						num9 = 0.0;
					}
					if (num9 < 0.0)
					{
						Debug.Log("fMolANew < 0");
						num9 = 0.0;
					}
					double num10 = num9 - num4;
					if (coA != null && !roomAIsVoid)
					{
						if (!gasContainer.mapDGasMols.ContainsKey(key))
						{
							gasContainer.mapDGasMols[key] = 0.0;
						}
						Dictionary<string, double> mapDGasMols;
						string key2;
						(mapDGasMols = gasContainer.mapDGasMols)[key2 = key] = mapDGasMols[key2] + num10;
						if (gasContainer.mapDGasMols[key] < -dictionary[key])
						{
							gasContainer.mapDGasMols[key] = -dictionary[key];
						}
						if (gasContainer.mapDGasMols[key] != 0.0)
						{
							coA.GasChanged = true;
						}
					}
					if (coB != null && !roomBIsVoid)
					{
						if (!gasContainer2.mapDGasMols.ContainsKey(key))
						{
							gasContainer2.mapDGasMols[key] = 0.0;
						}
						Dictionary<string, double> mapDGasMols;
						string key3;
						(mapDGasMols = gasContainer2.mapDGasMols)[key3 = key] = mapDGasMols[key3] + -(num9 - num4);
						if (gasContainer2.mapDGasMols[key] < -dictionary2[key])
						{
							gasContainer2.mapDGasMols[key] = -dictionary2[key];
						}
						if (gasContainer2.mapDGasMols[key] != 0.0)
						{
							coB.GasChanged = true;
						}
					}
					if (num10 > 0.0)
					{
						num3 += num10;
					}
					else
					{
						num2 -= num10;
					}
				}
			}
		}
		bool flag = num2 > 0.1 || num3 > 0.1;
		float num11 = 1f;
		if (num2 > 0.1)
		{
			Vector4 ptIn = new Vector4(this.ptA.x, this.ptA.y, this.ptB.x, this.ptB.y);
			this.AddGasPuff(ptIn);
			num11 = (float)num2 / 200f / (fWall * fWall);
		}
		if (num3 > 0.1)
		{
			Vector4 ptIn2 = new Vector4(this.ptB.x, this.ptB.y, this.ptA.x, this.ptA.y);
			this.AddGasPuff(ptIn2);
			num11 = (float)num3 / 200f / (fWall * fWall);
		}
		if (this.srcLow != null)
		{
			if (flag)
			{
				float num12 = 1.16f - fWall;
				num12 = Mathf.Clamp(num12, 0f, 1f);
				num11 = Mathf.Clamp(num11, 0f, 1f);
				this.PlayAudio(num12, num11, fTime / 2.0);
			}
			else
			{
				this.StopAudio(fTime / 2.0);
			}
		}
		double num13 = fTempA;
		if (dictionary2["StatGasMolTotal"] + dictionary["StatGasMolTotal"] > 0.0)
		{
			num13 = (fTempA * dictionary["StatGasMolTotal"] + statGasTemp * dictionary2["StatGasMolTotal"]) / (dictionary2["StatGasMolTotal"] + dictionary["StatGasMolTotal"]);
		}
		if (double.IsNaN(num13))
		{
			Debug.Log("fTempNew NaN");
		}
		if (gasContainer != null && !roomAIsVoid)
		{
			gasContainer.fDGasTemp += (num13 - fTempA) * num;
			gasContainer.Run(false);
		}
		if (gasContainer2 != null && !roomBIsVoid)
		{
			gasContainer2.fDGasTemp += (num13 - statGasTemp) * num;
			gasContainer2.Run(false);
		}
	}

	private void AddGasPuff(Vector4 ptIn)
	{
		if (float.IsInfinity(ptIn.x))
		{
			ptIn.x = this.co.tf.position.x;
		}
		if (float.IsInfinity(ptIn.y))
		{
			ptIn.y = this.co.tf.position.y;
		}
		if (float.IsInfinity(ptIn.z))
		{
			ptIn.z = this.co.tf.position.x;
		}
		if (float.IsInfinity(ptIn.w))
		{
			ptIn.w = this.co.tf.position.y;
		}
		ptIn.x += UnityEngine.Random.Range(-0.5f, 0.5f);
		ptIn.y += UnityEngine.Random.Range(-0.5f, 0.5f);
		ptIn.z += UnityEngine.Random.Range(-0.5f, 0.5f);
		ptIn.w += UnityEngine.Random.Range(-0.5f, 0.5f);
		CrewSim.vfxPuffs.AddGasPuff(new VFXGasPuffData(ptIn));
	}

	private void PlayAudio(float fPitch, float fVolume, double fTime)
	{
		AudioSource audioSource;
		AudioSource audioSource2;
		float num;
		float num2;
		float num3;
		if (fPitch < 0.5f)
		{
			audioSource = this.srcLow;
			audioSource2 = this.srcMid;
			num = this.fVolumeMaxLow;
			num2 = this.fVolumeMaxMid;
			AudioManager.am.LerpAudioTo(this.srcHigh, 0f, fTime);
			num3 = 4f * fPitch - 1f;
		}
		else
		{
			audioSource = this.srcMid;
			audioSource2 = this.srcHigh;
			num = this.fVolumeMaxMid;
			num2 = this.fVolumeMaxHigh;
			AudioManager.am.LerpAudioTo(this.srcLow, 0f, fTime);
			num3 = 4f * (fPitch - 0.5f) - 1f;
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
		AudioManager.am.LerpAudioTo(audioSource, fVolume2, fTime);
		AudioManager.am.LerpAudioTo(audioSource2, fVolume3, fTime);
	}

	private void StopAudio(double fTime)
	{
		AudioManager.am.LerpAudioTo(this.srcLow, 0f, fTime);
		AudioManager.am.LerpAudioTo(this.srcMid, 0f, fTime);
		AudioManager.am.LerpAudioTo(this.srcHigh, 0f, fTime);
	}

	public void SetAudioData(AudioSource srcSteady, JsonAudioEmitter jae)
	{
		if (jae == null || jae.strClipSteady == null)
		{
			return;
		}
		srcSteady.clip = Resources.Load<AudioClip>("Audio/" + jae.strClipSteady);
		srcSteady.loop = true;
		srcSteady.playOnAwake = false;
		srcSteady.volume = jae.fVolumeSteady;
		if (jae.fSpatialBlend >= 0f && jae.fSpatialBlend <= 1f)
		{
			srcSteady.spatialBlend = jae.fSpatialBlend;
		}
		if (jae.fMaxDistance > 0f)
		{
			srcSteady.maxDistance = jae.fMaxDistance;
		}
		if (jae.fMinDistance > 0f)
		{
			srcSteady.minDistance = jae.fMinDistance;
		}
		if (jae.strFalloffCurve != null && jae.strFalloffCurve != string.Empty)
		{
			AnimationCurveAsset animationCurveAsset = Resources.Load<AnimationCurveAsset>("Curves/" + jae.strFalloffCurve);
			if (animationCurveAsset != null)
			{
				srcSteady.rolloffMode = AudioRolloffMode.Custom;
				srcSteady.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animationCurveAsset.curve);
			}
		}
		AudioMixerGroup outputAudioMixerGroup = null;
		if (AudioManager.MixerGroups.TryGetValue(jae.strMixerName, out outputAudioMixerGroup))
		{
			srcSteady.outputAudioMixerGroup = outputAudioMixerGroup;
		}
	}

	public void SetData(string strPointA, string strPointB, float fWall)
	{
		this.strPointA = strPointA;
		this.strPointB = strPointB;
		this.fWall = fWall;
		this.CatchUp();
	}

	private CondOwner co;

	private string strPointA;

	private string strPointB;

	private float fWall;

	private double fTimeOfLastCheck;

	private double fCheckPeriod = 0.5;

	private Vector2 ptA;

	private Vector2 ptB;

	private AudioSource srcLow;

	private AudioSource srcMid;

	private AudioSource srcHigh;

	private float fVolumeMaxLow = 1f;

	private float fVolumeMaxMid = 1f;

	private float fVolumeMaxHigh = 1f;

	private static CondTrigger _ctRoom;

	private static CondTrigger _ctGasBlocker;

	private static JsonAudioEmitter jaeLow;

	private static JsonAudioEmitter jaeMid;

	private static JsonAudioEmitter jaeHigh;

	private static bool bLoadedAEs = false;

	private readonly List<CondOwner> _blockingCOs = new List<CondOwner>();

	private GasPressureSense gasPS;

	private bool checkedGasPS;

	private bool doNotExchange;

	public static readonly Dictionary<string, double> GasMolsVoid = new Dictionary<string, double>
	{
		{
			"StatGasMolTotal",
			0.009999999776482582
		}
	};
}
