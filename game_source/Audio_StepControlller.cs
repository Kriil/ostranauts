using System;
using UnityEngine;
using UnityEngine.Audio;

public class Audio_StepControlller : MonoBehaviour
{
	private void Awake()
	{
		this.TheSource = base.gameObject.AddComponent<AudioSource>();
		this.TheSource.playOnAwake = false;
		this.TheSource.minDistance = 20f;
		this.TheSource.maxDistance = (float)this.AudibleDistance;
		this.TheSource.volume = this.AudibleVolume;
		this.TheSource.spatialBlend = 1f;
		this.TheSource.outputAudioMixerGroup = this.SendGroup;
	}

	private void Start()
	{
		AnimationCurveAsset animationCurveAsset = Resources.Load<AnimationCurveAsset>("Curves/FalloffLogShallowMin20Max500");
		if (animationCurveAsset != null)
		{
			this.TheSource.rolloffMode = AudioRolloffMode.Custom;
			this.TheSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, animationCurveAsset.curve);
		}
	}

	private void LeftFoot()
	{
		this.NameOfClip = "Footstep";
		this.randomizeclip(this.StepClip);
	}

	private void RightFoot()
	{
		this.NameOfClip = "Footstep";
		this.randomizeclip(this.StepClip);
	}

	private void randomizeclip(AudioClip[] TypeOfClip)
	{
		this.NumberOfClips = TypeOfClip.Length;
		if (this.NumberOfClips == 0)
		{
			return;
		}
		this.ClipNumber = UnityEngine.Random.Range(0, this.NumberOfClips);
		if (this.ClipNumber == this.CurrentClip && this.ClipNumber != this.NumberOfClips)
		{
			this.ClipNumber++;
		}
		if (this.ClipNumber >= this.NumberOfClips)
		{
			this.ClipNumber = 0;
		}
		this.CurrentClip = this.ClipNumber;
		this.TheSource.clip = TypeOfClip[this.ClipNumber];
		this.TheSource.Play();
		if (this.Use_Debug)
		{
			MonoBehaviour.print(string.Concat(new object[]
			{
				"Can play animation sound and ",
				this.NameOfClip,
				" is playing ",
				this.ClipNumber
			}));
		}
	}

	private void BurbeeJump()
	{
		this.NameOfClip = "BurpeeJump";
		this.randomizeclip(this.BurpeeJumpSounds);
	}

	private void BashWindupSound()
	{
	}

	private void BashSound()
	{
		this.NameOfClip = "BashSound";
		this.randomizeclip(this.BashSounds);
	}

	private void BurpeeJumpSound()
	{
		this.NameOfClip = "BurpeeJumpSound";
		this.randomizeclip(this.BurpeeJumpSounds);
	}

	private void ClapSound()
	{
		this.NameOfClip = "ClapSound";
		this.randomizeclip(this.ClapSounds);
	}

	private void DefecatingSound()
	{
		this.NameOfClip = "Defecating";
		this.randomizeclip(this.DefecatingSounds);
	}

	private void Sitting1Sound()
	{
		this.NameOfClip = "Sitting1";
		this.randomizeclip(this.Sitting1Sounds);
	}

	private void Sitting2Sound()
	{
		this.NameOfClip = "Sitting2";
		this.randomizeclip(this.Sitting2Sounds);
	}

	private void TabletSound()
	{
		this.NameOfClip = "TabletSound";
		this.randomizeclip(this.TabletSounds);
	}

	private void ToolSound()
	{
		this.NameOfClip = "ToolSound";
		this.randomizeclip(this.ToolingSounds);
	}

	private void TalkSound()
	{
	}

	private void AngrySound()
	{
	}

	private void YesSound()
	{
	}

	private void NoSound()
	{
	}

	public AudioSource TheSource;

	public int AudibleDistance = 50;

	public float AudibleVolume = 0.5f;

	public AudioMixerGroup SendGroup;

	public AudioClip[] StepClip;

	public AudioClip[] BurbeeJumpClip;

	public AudioClip[] MovementSounds;

	public AudioClip[] TinkerSounds;

	public AudioClip[] ClapSounds;

	public AudioClip[] DefecatingSounds;

	public AudioClip[] Sitting1Sounds;

	public AudioClip[] Sitting2Sounds;

	public AudioClip[] TabletSounds;

	public AudioClip[] ToolingSounds;

	public AudioClip[] BashSounds;

	public AudioClip[] BurpeeJumpSounds;

	private int NumberOfClips;

	private int CurrentClip;

	private int ClipNumber;

	private string NameOfClip = "NoName";

	public bool Use_Debug;
}
