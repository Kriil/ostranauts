using System;
using Ostranauts.ShipGUIs.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModMapControls : NavModBase
	{
		protected override void Awake()
		{
			base.Awake();
			this.btnTimeRateReset.onClick.AddListener(new UnityAction(this._guiOrbitDraw.ResetTime));
			this.btnZoomStn.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.fZoomTimer = 3f;
				this._guiOrbitDraw.dMagTarget = 1.72E+17;
			});
			this.btnZoomATC.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.fZoomTimer = 3f;
				this._guiOrbitDraw.dMagTarget = 1300000000000000.0;
			});
			this.btnZoomPlanet.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.fZoomTimer = 3f;
				this._guiOrbitDraw.dMagTarget = 850000000.0;
			});
			this.btnZoomInner.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.fZoomTimer = 3f;
				this._guiOrbitDraw.dMagTarget = 3192.0;
			});
			this.btnZoomOuter.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.fZoomTimer = 3f;
				this._guiOrbitDraw.dMagTarget = 12.0;
			});
			this.txtTimeRate.text = MathUtils.GetTimeUnits(this._guiOrbitDraw.fTimeFuture, "INF s");
			this.knobFollow.bWrap = true;
			this.knobFollow.Callback = new Action<int>(this.SetFollow);
			this.knobRef.bWrap = true;
			this.knobRef.Callback = delegate(int i)
			{
				this._guiOrbitDraw.SetPropMapData("nRef", i.ToString());
			};
			this.knobLabels.bWrap = true;
			this.knobLabels.Callback = delegate(int i)
			{
				this._guiOrbitDraw.SetPropMapData("nLabels", i.ToString());
			};
			this.chkNWZ.onValueChanged.AddListener(delegate(bool isOn)
			{
				this._guiOrbitDraw.SetPropMapData("bShowNWZ", isOn.ToString().ToLower());
				this._guiOrbitDraw.bShowNWZ = isOn;
			});
		}

		protected override void Init()
		{
			string text;
			if (this.dictPropMap.TryGetValue("nFollow", out text))
			{
				this.knobFollow.SetStateSilent(int.Parse(text));
			}
			else
			{
				this.knobFollow.SetStateSilent(1);
			}
			if (this.dictPropMap.TryGetValue("nLabels", out text))
			{
				this.knobLabels.SetStateSilent(int.Parse(text));
			}
			else
			{
				this.knobLabels.SetStateSilent(3);
			}
			if (this.dictPropMap.TryGetValue("nRef", out text))
			{
				this.knobRef.SetStateSilent(int.Parse(text));
			}
			else
			{
				this.knobRef.SetStateSilent(0);
			}
			if (this.dictPropMap.TryGetValue("bShowNWZ", out text))
			{
				this.chkNWZ.isOn = bool.Parse(text);
			}
		}

		private void Update()
		{
			if (this.COSelf == null || this.COSelf.ship == null)
			{
				return;
			}
			if (this.gplMapFFWD != null && this.gplMapFFWD.bPressed)
			{
				if (this.slidMapFFWD != null)
				{
					float num = Mathf.Clamp(this.slidMapFFWD.value, -0.99999f, 0.99999f);
					if (this.slidMapFFWD.value > 0f)
					{
						this._guiOrbitDraw.fTimeFutureTarget += 50f * (1f / (1f - num) - 1f);
					}
					else
					{
						this._guiOrbitDraw.fTimeFutureTarget -= 50f * (1f / (1f + num) - 1f);
					}
				}
				if (this._guiOrbitDraw.fTimeFutureTarget < 1f)
				{
					this._guiOrbitDraw.fTimeFutureTarget = 1f;
				}
			}
			else
			{
				if (this._guiOrbitDraw.fTimeFutureTarget != 1f)
				{
					this._guiOrbitDraw.fTimeFutureTarget = this._guiOrbitDraw.fTimeFuture;
				}
				if (this.slidMapFFWD != null)
				{
					this.slidMapFFWD.value = 0f;
				}
			}
			double num2 = StarSystem.fEpoch - this.dfEpochLastCheck;
			if (num2 < 2.0)
			{
				return;
			}
			bool propMapData = this._guiOrbitDraw.GetPropMapData("bShowNWZ", this.chkNWZ.isOn);
			if (propMapData != this.chkNWZ.isOn)
			{
				this.chkNWZ.isOn = propMapData;
			}
		}

		protected override void UpdateUI()
		{
			this.txtTimeRate.text = MathUtils.GetTimeUnits(this._guiOrbitDraw.fTimeFuture, "INF s");
		}

		private void SetFollow(int nState)
		{
			this._guiOrbitDraw.follow = new NavPOI(0.0, 0.0);
			if (nState != 1)
			{
				if (nState == 2)
				{
					this._guiOrbitDraw.follow = GUIOrbitDraw.CrossHairTarget;
				}
			}
			else
			{
				this._guiOrbitDraw.follow = new NavPOI(this._guiOrbitDraw.sdNS, this.COSelf.ship, this._guiOrbitDraw.ShipPropMap);
			}
			this._guiOrbitDraw.dFollowOffsetSX = 0.0;
			this._guiOrbitDraw.dFollowOffsetSY = 0.0;
			this._guiOrbitDraw.SetOldFollow();
			this._guiOrbitDraw.SetPropMapData("nFollow", this.knobFollow.State.ToString());
		}

		public const float FFWD_COEFF = 50f;

		[SerializeField]
		private Button btnTimeRateReset;

		[SerializeField]
		private Button btnZoomStn;

		[SerializeField]
		private Button btnZoomATC;

		[SerializeField]
		private Button btnZoomPlanet;

		[SerializeField]
		private Button btnZoomInner;

		[SerializeField]
		private Button btnZoomOuter;

		[SerializeField]
		private TMP_Text txtTimeRate;

		[SerializeField]
		private GUIKnob knobFollow;

		[SerializeField]
		private GUIKnob knobRef;

		[SerializeField]
		private GUIKnob knobLabels;

		[SerializeField]
		public Toggle chkNWZ;

		[SerializeField]
		private GUIPointerListener gplMapFFWD;

		[SerializeField]
		private Slider slidMapFFWD;

		private double dfEpochLastCheck = -1.0;
	}
}
