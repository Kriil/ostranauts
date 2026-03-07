using System;

// Audio emitter definition payload. Likely loaded from JSON and applied to an
// AudioEmitter component to configure clips, falloff, mixer routing, and filters.
public class JsonAudioEmitter
{
	// Sets conservative defaults so missing fields still produce a usable emitter.
	public JsonAudioEmitter()
	{
		this.fSpatialBlend = -1f;
		this.fMaxDistance = 35f;
		this.fMinDistance = 20f;
		this.fLoPassFreq = 20000f;
		this.fLoPassFreqOccluded = 5000f;
	}

	// Likely the emitter id used by DataHandler/AudioManager lookups.
	public string strName { get; set; }

	public string strMixerName { get; set; }

	public string strClipSteady { get; set; }

	public string strClipTrans { get; set; }

	public string strClipPickup { get; set; }

	public string strFalloffCurve { get; set; }

	public string strLowPassCurve { get; set; }

	public float fVolumeSteady { get; set; }

	public float fVolumeTrans { get; set; }

	public float fVolumePickup { get; set; }

	public float fSteadyDelay { get; set; }

	public float fTransDuration { get; set; }

	public float fLoPassFreq { get; set; }

	public float fLoPassFreqOccluded { get; set; }

	public float fMaxDistance { get; set; }

	public float fMinDistance { get; set; }

	public float fSpatialBlend { get; set; }

	public override string ToString()
	{
		return this.strName;
	}
}
