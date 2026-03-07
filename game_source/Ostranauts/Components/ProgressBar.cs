using System;
using TMPro;
using UnityEngine;

namespace Ostranauts.Components
{
	public class ProgressBar : MonoBehaviour
	{
		private bool ShortBarState
		{
			get
			{
				return this._shortBarState;
			}
			set
			{
				this.progressBarShort.gameObject.SetActive(value);
				this._shortBarState = value;
				this._shortBarDisabledTimeStamp = (this._shortBarState ? 0f : Time.time);
			}
		}

		private bool LongBarState
		{
			get
			{
				return this._longBarState;
			}
			set
			{
				this.progressBarLong.gameObject.SetActive(value);
				this._longBarState = value;
			}
		}

		private void Awake()
		{
		}

		private void OnDisable()
		{
			if (this.text != null)
			{
				UnityEngine.Object.Destroy(this.text.gameObject);
			}
		}

		private void LateUpdate()
		{
			if (this.ShortBarState)
			{
				this._shortBarCurrentLength += this._durationMultiplier * CrewSim.TimeElapsedScaled();
				this.progressBarShort.localScale = new Vector3(this._shortBarCurrentLength, 0.1f);
				this.progressBarShort.position = new Vector3(base.transform.parent.position.x + this._shortBarCurrentLength / 2f - 0.5f, this.progressBarShort.parent.position.y - 1.25f, -5f);
				this.progressBarShort.LookAt(this.progressBarShort.position + CrewSim.objInstance.camMain.transform.rotation * Vector3.forward, CrewSim.objInstance.camMain.transform.rotation * Vector3.up);
				if (this.text != null)
				{
					this.text.transform.position = new Vector3(base.transform.parent.position.x, this.progressBarShort.parent.position.y - 1f, -5f);
				}
				if (this._shortBarCurrentLength > 1f)
				{
					this.DeactivateShort();
				}
			}
			if (this.LongBarState)
			{
				this.progressBarLong.localScale = new Vector3((this._longBarLengthMax - this._longBarCurrentLength) / this._longBarLengthMax, 0.1f);
				this.progressBarLong.position = new Vector3(this.progressBarLong.parent.position.x + this.progressBarLong.localScale.x / 2f - 0.5f, base.transform.parent.position.y - 1.4f, -5f);
				this.progressBarLong.LookAt(this.progressBarLong.position + CrewSim.objInstance.camMain.transform.rotation * Vector3.forward, CrewSim.objInstance.camMain.transform.rotation * Vector3.up);
				if (this._longBarCurrentLength >= this._longBarLengthMax)
				{
					this.DeactivateImmediate();
				}
				if (!this.ShortBarState && Time.time - this._shortBarDisabledTimeStamp > ProgressBar.AUTOHIDETIMESPAN)
				{
					this.LongBarState = false;
				}
			}
		}

		public void Activate(float duration, bool showLongbar, Interaction interaction)
		{
			if (duration > 60f || interaction == null)
			{
				return;
			}
			if (this.text == null)
			{
				this.text = UnityEngine.Object.Instantiate<GameObject>(this.textFloatUIPrefab, CrewSim.CanvasManager.goCanvasWorld.transform).GetComponent<TextMeshPro>();
				this.text.fontSize -= 24f;
				this.text.color = new Color32(197, 197, 197, byte.MaxValue);
				this.text.text = string.Empty;
				string text = "ProgressBar.";
				if (this._targetCO != null)
				{
					text += this._targetCO.strName;
				}
				this.text.gameObject.name = text;
			}
			string text2 = null;
			this.text.text = interaction.strTitle;
			if (interaction.strName != null && DataHandler.dictInstallables2.ContainsKey(interaction.strName))
			{
				JsonInstallable jsonInstallable = DataHandler.dictInstallables2[interaction.strName];
				this.text.text = jsonInstallable.strJobType + interaction.GetTextRate();
				text2 = jsonInstallable.strProgressStat;
			}
			this._targetCO = interaction.objThem;
			this.ShortBarState = true;
			this._shortBarCurrentLength = 0f;
			if (duration > 0f)
			{
				this._durationMultiplier = 1f / duration;
			}
			if (showLongbar)
			{
				this.LongBarState = true;
				this.progressBarLong.localScale = new Vector3(1f, 0.1f);
				if (text2 != null)
				{
					this.SetLongBarProgressStat(text2);
				}
			}
		}

		private void SetLongBarProgressStat(string strProgressStat)
		{
			this._longBarCurrentLength = 0f;
			this._longBarLengthMax = 0f;
			if (this._targetCO != null)
			{
				this._longBarCurrentLength = (float)this._targetCO.GetCondAmount(strProgressStat);
				Destructable component = this._targetCO.GetComponent<Destructable>();
				if (component != null)
				{
					this._longBarLengthMax = (float)component.DmgMax(strProgressStat);
				}
			}
			if (this._longBarLengthMax == 0f)
			{
				this._longBarLengthMax = 100f;
			}
			if (this._longBarCurrentLength >= this._longBarLengthMax)
			{
				this.DeactivateImmediate();
			}
			this.progressBarLong.GetComponent<Renderer>().material.color = Color.Lerp(ProgressBar.LBSSTARTCOLOR, ProgressBar.LBSENDCOLOR, (this._longBarLengthMax - this._longBarCurrentLength) / 100f);
		}

		public void Destroy()
		{
			if (this.text != null)
			{
				UnityEngine.Object.Destroy(this.text.gameObject);
			}
			this.progressBarShort.gameObject.SetActive(false);
			this.progressBarLong.gameObject.SetActive(false);
			UnityEngine.Object.Destroy(this);
		}

		public void DeactivateImmediate()
		{
			if (this.text != null)
			{
				this.text.text = string.Empty;
			}
			this.LongBarState = false;
			this.ShortBarState = false;
		}

		private void DeactivateShort()
		{
			if (this.text != null)
			{
				this.text.text = string.Empty;
			}
			this.ShortBarState = false;
		}

		private static readonly Color LBSSTARTCOLOR = new Color32(byte.MaxValue, 163, 57, byte.MaxValue);

		private static readonly Color LBSENDCOLOR = new Color32(178, byte.MaxValue, byte.MaxValue, byte.MaxValue);

		private static readonly float AUTOHIDETIMESPAN = 0.5f;

		[SerializeField]
		private Transform progressBarLong;

		[SerializeField]
		private Transform progressBarShort;

		[SerializeField]
		private GameObject textFloatUIPrefab;

		private TextMeshPro text;

		private bool _shortBarState;

		private bool _longBarState;

		private float _shortBarDisabledTimeStamp;

		private float _durationMultiplier;

		private float _longBarCurrentLength;

		private float _longBarLengthMax;

		private float _shortBarCurrentLength;

		private CondOwner _targetCO;
	}
}
