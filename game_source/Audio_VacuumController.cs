using System;
using UnityEngine;
using UnityEngine.Audio;

public class Audio_VacuumController : MonoBehaviour
{
	private void Awake()
	{
		this.VacuumSource = base.gameObject.AddComponent<AudioSource>();
		this.VacuumSource.playOnAwake = false;
		this.HelmetSource = base.gameObject.AddComponent<AudioSource>();
		this.HelmetSource.playOnAwake = false;
	}

	public void CheckCurrent()
	{
		if ((double)AudioManager.am.EnvPressure < 0.5)
		{
			this.VacuumOn();
		}
		else
		{
			this.VacuumOff();
		}
		if (AudioManager.am.Helmet != GUIHelmet.HelmetStyle.None)
		{
			this.HelmetOn();
		}
		else
		{
			this.HelmetOff();
		}
	}

	private void VacuumOn()
	{
		if (this.bVacuum)
		{
			return;
		}
		if (this.VacuumSource.isPlaying)
		{
			this.VacuumSource.Stop();
		}
		for (int i = 0; i < this.VacuumSnapshot.Length; i++)
		{
		}
		if (this.VacuumSound != null)
		{
			this.VacuumSource.clip = this.VacuumSound;
			this.VacuumSource.Play();
		}
		this.bVacuum = true;
	}

	private void VacuumOff()
	{
		if (!this.bVacuum)
		{
			return;
		}
		if (this.VacuumSource.isPlaying)
		{
			this.VacuumSource.Stop();
		}
		for (int i = 0; i < this.VacuumSnapshot.Length; i++)
		{
		}
		if (this.NonVacuumSound != null)
		{
			this.VacuumSource.clip = this.NonVacuumSound;
			this.VacuumSource.Play();
		}
		this.bVacuum = false;
	}

	private void HelmetOn()
	{
		if (this.bHelmet)
		{
			return;
		}
		AudioManager.am.PlayAudioEmitter("HelmetOn", false, false);
		this.bHelmet = true;
	}

	private void HelmetOff()
	{
		if (!this.bHelmet)
		{
			return;
		}
		AudioManager.am.PlayAudioEmitter("HelmetOff", false, false);
		this.bHelmet = false;
	}

	private bool bVacuum;

	private bool bHelmet;

	private AudioSource VacuumSource;

	private AudioSource HelmetSource;

	public AudioClip VacuumSound;

	public AudioClip NonVacuumSound;

	public AudioMixerSnapshot[] VacuumSnapshot;

	public AudioMixerSnapshot[] VacuumSnapshot_off;

	public float transitionTime = 2f;
}
