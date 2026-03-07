using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModWarnings : NavModBase
	{
		private bool IsXPDROn
		{
			get
			{
				return this._guiOrbitDraw.GetPropMapData("bXPDROn", false);
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this.chkProxMute.onValueChanged.AddListener(new UnityAction<bool>(this.ToggleProxMute));
			this.btnProxClear.onClick.AddListener(new UnityAction(this.ProxClear));
			this.btnTrackClear.onClick.AddListener(new UnityAction(this.TrackClear));
		}

		protected override void OnNavModMessage(NavModMessageType messageType, object arg)
		{
			if (messageType != NavModMessageType.UpdateUI)
			{
				if (messageType == NavModMessageType.WarnClampEngaged)
				{
					this.goEngageDock.State = 2;
					this.QueueLamps(3f);
				}
			}
			else
			{
				this.UpdateUI();
			}
		}

		protected override void Init()
		{
			this.chkProxMute.isOn = !this.COSelf.HasCond("IsProxMuted");
		}

		private void QueueLamps(float time)
		{
			base.StartCoroutine(this.SetLamps(time));
		}

		private IEnumerator SetLamps(float time)
		{
			yield return new WaitForSeconds(time);
			this.goEngageDock.State = 3;
			yield break;
		}

		private new void UpdateUI()
		{
			Ship ship = this.COSelf.ship;
			if (ship.bDocked)
			{
				if (this.goEngageDock.State != 2)
				{
					this.goEngageDock.State = 3;
				}
			}
			else
			{
				this.goEngageDock.State = 0;
			}
			int state = 0;
			if (ship.proximityWarning && ((int)(4f * Time.realtimeSinceStartup) & 1) == 0)
			{
				state = 3;
			}
			this.goEngageProx.State = state;
			state = 0;
			if (ship.trackWarning && ((int)(4f * Time.realtimeSinceStartup) & 1) == 0)
			{
				state = 3;
			}
			this.goEngageTrack.State = state;
			state = 0;
			if (this.IsXPDROn && string.IsNullOrEmpty(this.COSelf.ship.strXPDR))
			{
				state = 2;
			}
			this.goXPDRFault.State = state;
			state = 0;
			if (!this.COSelf.ship.bXPDRAntenna)
			{
				state = 2;
			}
			this.goXPDRAntFault.State = state;
		}

		private void ProxClear()
		{
			if (this.COSelf.ship == null)
			{
				return;
			}
			this.COSelf.ship.aProxIgnores.Clear();
			foreach (string item in this.COSelf.ship.aProxCurrent)
			{
				this.COSelf.ship.aProxIgnores.Add(item);
			}
			this.COSelf.ship.proximityWarning = false;
		}

		private void TrackClear()
		{
			if (this.COSelf.ship == null)
			{
				return;
			}
			this.COSelf.ship.aTrackIgnores.Clear();
			foreach (string item in this.COSelf.ship.aTrackCurrent)
			{
				this.COSelf.ship.aTrackIgnores.Add(item);
			}
			this.COSelf.ship.trackWarning = false;
		}

		private void ToggleProxMute(bool isOn)
		{
			if (isOn)
			{
				this.COSelf.ZeroCondAmount("IsProxMuted");
				if (this.COSelf.ship != null && this.COSelf.ship.proximityWarning)
				{
					AudioManager.am.PlayAudioEmitter("ShipProxAlarm", true, false);
				}
				return;
			}
			this.COSelf.AddCondAmount("IsProxMuted", 1.0, 0.0, 0f);
			AudioManager.am.StopAudioEmitter("ShipProxAlarm");
		}

		[SerializeField]
		public GUILamp goEngageDock;

		[SerializeField]
		private GUILamp goEngageProx;

		[SerializeField]
		private GUILamp goEngageTrack;

		[SerializeField]
		private GUILamp goXPDRFault;

		[SerializeField]
		private GUILamp goXPDRAntFault;

		[SerializeField]
		private Button btnProxClear;

		[SerializeField]
		private Button btnTrackClear;

		[SerializeField]
		private Toggle chkProxMute;
	}
}
