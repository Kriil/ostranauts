using System;
using System.Collections;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.AIPilots;
using Ostranauts.Ships.Commands;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModCoursePlot : NavModBase
	{
		public NavData NavPlan
		{
			get
			{
				return this._navPlan;
			}
			set
			{
				this._navPlan = value;
				if (this._guiOrbitDraw != null)
				{
					this._guiOrbitDraw.navPlan = value;
				}
			}
		}

		private new void Awake()
		{
			base.Awake();
			this.btnErrorDn.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.MoveCrosshair(0.0, -2.0);
			});
			this.btnErrorLt.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.MoveCrosshair(-2.0, 0.0);
			});
			this.btnErrorRt.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.MoveCrosshair(2.0, 0.0);
			});
			this.btnErrorUp.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.MoveCrosshair(0.0, 2.0);
			});
			this.btnError0.onClick.AddListener(delegate()
			{
				this._guiOrbitDraw.ResetCrosshair();
			});
			this.gphErrorLt = this.btnErrorLt.gameObject.AddComponent<GUIBtnPressHold>();
			this.gphErrorRt = this.btnErrorRt.gameObject.AddComponent<GUIBtnPressHold>();
			this.gphErrorDn = this.btnErrorDn.gameObject.AddComponent<GUIBtnPressHold>();
			this.gphErrorUp = this.btnErrorUp.gameObject.AddComponent<GUIBtnPressHold>();
			this.btnCrsPlot.onClick.AddListener(new UnityAction(this.OnPlotCoursePressed));
			this.slidCrsLim.onValueChanged.AddListener(delegate(float _)
			{
				this._guiOrbitDraw.SetPropMapData("fCrsLim", this.slidCrsLim.value.ToString());
				this.OnDriftSliderChanged((!Input.GetKey(KeyCode.LeftShift)) ? 0 : 2);
			});
			this.slidCrsCoast.onValueChanged.AddListener(delegate(float _)
			{
				this._guiOrbitDraw.SetPropMapData("fCrsCoast", this.slidCrsCoast.value.ToString());
				this.OnDriftSliderChanged((!Input.GetKey(KeyCode.LeftShift)) ? 0 : 2);
			});
			this.clrCoursePlotText = this.txtCrsFuelReactant.color;
			this.eEngage = delegate(bool val)
			{
				this._guiOrbitDraw.SetPropMapData("chkEngage", val.ToString());
				this.Engage();
			};
			this.chkEngage.onValueChanged.AddListener(this.eEngage);
		}

		protected override void Init()
		{
			this.NavPlan = this.COSelf.ship.objSS.NavData;
			string s;
			if (this.dictPropMap.TryGetValue("fCrsLim", out s))
			{
				float num = float.Parse(s);
				if (num > 0f)
				{
					this.slidCrsLim.value = num;
				}
			}
			if (this.dictPropMap.TryGetValue("fCrsCoast", out s))
			{
				float num2 = float.Parse(s);
				if (num2 > 0f)
				{
					this.slidCrsCoast.value = num2;
				}
			}
			this.UpdateUI();
		}

		protected override void OnNavModMessage(NavModMessageType messageType, object arg)
		{
			if (messageType == NavModMessageType.UpdateUI)
			{
				this.UpdatePropMapsValues();
				this.UpdateUI();
			}
		}

		private void UpdatePropMapsValues()
		{
			string text;
			if (this.dictPropMap.TryGetValue("fCrsLimMax", out text))
			{
				float num = float.Parse(text);
				if (num > 0f)
				{
					this.slidCrsLim.maxValue = num;
				}
			}
			if (this.dictPropMap.TryGetValue("chkEngage", out text))
			{
				bool flag = false;
				if (bool.TryParse(text, out flag) && flag != this.chkEngage.isOn)
				{
					this.chkEngage.isOn = flag;
				}
			}
		}

		private void OnPlotCoursePressed()
		{
			double num = 0.0;
			double num2 = 0.0;
			if (GUIOrbitDraw.CrossHairTarget != null)
			{
				GUIOrbitDraw.CrossHairTarget.GetSXY(out num, out num2);
			}
			if (this.EvaluateMinimumAPDistance(num, num2))
			{
				this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_MINDISTANCE", false);
				return;
			}
			if (this.NavPlan == null || (GUIOrbitDraw.CrossHairTarget != null && Math.Abs(num - this.NavPlan.Destination.ObjSS.vPosx) > 1E-14 && Math.Abs(num2 - this.NavPlan.Destination.ObjSS.vPosy) > 1E-14))
			{
				this.NavPlan = null;
				this.NavPlan = this.PlanTrip((!Input.GetKey(KeyCode.LeftShift)) ? 0 : 2, null);
				if (this.NavPlan == null)
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_NOPLOT", false);
				}
				if (!this._guiOrbitDraw.GetPropMapData("bShowNWZ", false))
				{
					this._guiOrbitDraw.bShowNWZ = true;
				}
			}
			else
			{
				this.NavPlan = null;
				this._guiOrbitDraw.SetPropMapData("bShowNWZ", false.ToString().ToLower());
			}
		}

		private void OnDriftSliderChanged(int iterations)
		{
			if (this._tripPlanerCoRoutine != null || this.NavPlan == null)
			{
				return;
			}
			this._tripPlanerCoRoutine = base.StartCoroutine(this._PlanTrip(iterations));
		}

		private IEnumerator _PlanTrip(int iterations)
		{
			yield return new WaitForSeconds(0.5f);
			this.NavPlan = null;
			this.NavPlan = this.PlanTrip(iterations, null);
			if (this.NavPlan == null)
			{
				this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_NOPLOT", false);
			}
			this._tripPlanerCoRoutine = null;
			yield break;
		}

		private void Engage()
		{
			if (this.COSelf.ship.bDocked && this.chkEngage.isOn)
			{
				GUIOrbitDraw.NavModMessageEvent.Invoke(NavModMessageType.WarnClampEngaged, null);
				this.chkEngage.isOn = false;
				return;
			}
			this._guiOrbitDraw.ResetTime();
			if (this.chkEngage.isOn)
			{
				if (this.COSelf.ship.Reactor != null && this.COSelf.ship.Reactor.HasCond("IsAutopilotAborted"))
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_INVALIDATED", false);
				}
				else if (this.NavPlan == null)
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_NOPLAN", false);
				}
				else if (!this.EvaluateIsOutsideNoWakeRange(StarSystem.fEpoch))
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_NWZ", false);
					this._guiOrbitDraw.FlashNWZCircle();
				}
				else if (this.EvaluateMinimumAPDistance(this.NavPlan.Destination))
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_MINDISTANCE", false);
				}
				else if (!this.EvaluateIsReactorFueledAndOn())
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_REACTOR", false);
				}
				else if (!this.EvaluateIsReactorCycleActive())
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_CYCLE_SAFETY", false);
					this._guiOrbitDraw.FlashCycleSafety();
				}
				else if (!this.EvaluateRCS())
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_RCS", false);
				}
				else if (!this.EvaluateIsCloseEnough())
				{
					this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_DISTANCE", false);
				}
				else
				{
					this.NavPlan = this.PlanTrip(0, null);
					if (this.NavPlan == null)
					{
						this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_NOPLOT", false);
					}
					else
					{
						this.EngageTrip();
					}
				}
			}
			else
			{
				this.COSelf.ship.shipSituTarget = null;
				this.COSelf.ship.shipScanTarget = null;
				this.COSelf.ship.objSS.ResetNavData();
				AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.COSelf.ship.strRegID);
				if (aishipByRegID != null && aishipByRegID.ActiveCommandName == "FlyToAutoPilot")
				{
					AIShipManager.UnregisterShip(this.COSelf.ship);
					this.COSelf.ship.LogAdd(DataHandler.GetString("NAV_LOG_AP_DISABLED", false), StarSystem.fEpoch, true);
				}
			}
		}

		private NavData PlanTrip(int nIterations, NavData navData)
		{
			FlyToPath flyToPath = new FlyToPath(this.COSelf.ship, false);
			this._guiOrbitDraw.ClearDebugDraws(null);
			this.COSelf.ship.fTimeEngaged = 0f;
			this.COSelf.ship.nCurrentWaypoint = -1;
			this.COSelf.ship.shipScanTarget = GUIOrbitDraw.CrossHairTarget.Ship;
			if (GUIOrbitDraw.CrossHairTarget.Ship != null)
			{
				this.COSelf.ship.shipSituTarget = new ShipSitu();
				this.COSelf.ship.shipSituTarget.CopyFrom(GUIOrbitDraw.CrossHairTarget.Ship.objSS, false);
				this.COSelf.ship.shipSituTarget.vPosx += GUIOrbitDraw.CrossHairTarget.fOffsetSX;
				this.COSelf.ship.shipSituTarget.vPosy += GUIOrbitDraw.CrossHairTarget.fOffsetSY;
			}
			else
			{
				this.COSelf.ship.shipSituTarget = new ShipSitu();
				double vPosx;
				double vPosy;
				GUIOrbitDraw.CrossHairTarget.GetSXY(out vPosx, out vPosy);
				this.COSelf.ship.shipSituTarget.vPosx = vPosx;
				this.COSelf.ship.shipSituTarget.vPosy = vPosy;
			}
			ShipSitu objSS = this.COSelf.ship.objSS;
			ShipSitu shipSituTarget = this.COSelf.ship.shipSituTarget;
			if (shipSituTarget == null || objSS == null)
			{
				this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_TOO_CLOSE", false);
				return null;
			}
			NavDataPoint navOrigin = flyToPath.CreateNavPointStatic(objSS, StarSystem.fEpoch, false, false);
			NavDataPoint navDestination = flyToPath.CreateNavPointStatic(shipSituTarget, StarSystem.fEpoch, false, true);
			flyToPath.nRecursion = 0;
			NavData navData2 = flyToPath.PlanTrip4(navOrigin, navDestination, this.slidCrsLim.value, this.slidCrsCoast.value);
			if (navData2 != null)
			{
				this._guiOrbitDraw.fTimeFuture = (this._guiOrbitDraw.fTimeFutureTarget = (float)(navData2.Destination.ArrivalTime - StarSystem.fEpoch));
				bool flag = false;
				bool flag2 = false;
				if (this.COSelf.ship.Reactor != null)
				{
					if (!this.COSelf.ship.Reactor.HasCond("StatICCryoMult"))
					{
						flag2 = true;
					}
					double flowforCYCLE = NavData.GetFLOWforCYCLE(this.COSelf.ship.Reactor, (double)this.slidCrsLim.value);
					if (flowforCYCLE > (double)this.slidCrsLim.maxValue)
					{
						flag = true;
					}
					navData2.SetFlowMultPlot(flowforCYCLE);
				}
				string text = null;
				if (flag2)
				{
					text = DataHandler.GetString("GUI_ORBIT_WARN_CRYO_OFF", false);
				}
				if (flag)
				{
					if (flag2)
					{
						text += "\n\n";
					}
					text += DataHandler.GetString("GUI_ORBIT_WARN_FLOW_MAX", false);
				}
				if (!string.IsNullOrEmpty(text))
				{
					this._guiOrbitDraw.CGClampWarning(text, true);
				}
			}
			return navData2;
		}

		private new void UpdateUI()
		{
			float fAmount = 0f;
			if (this._guiOrbitDraw.livePlan != null)
			{
				fAmount = (float)this._guiOrbitDraw.livePlan.GetDuration();
			}
			this.txtCrsDuration.text = MathUtils.GetTimeUnits(fAmount, "INF");
			AIShip aishipByRegID = AIShipManager.GetAIShipByRegID(this.COSelf.ship.strRegID);
			bool flag = aishipByRegID != null && aishipByRegID.ActiveCommandName == "FlyToAutoPilot";
			if (this.chkEngage.isOn != flag)
			{
				this.chkEngage.onValueChanged.RemoveListener(this.eEngage);
				this.chkEngage.isOn = flag;
				this._guiOrbitDraw.SetPropMapData("chkEngage", flag.ToString());
				this.chkEngage.onValueChanged.AddListener(this.eEngage);
			}
			if (this.gphErrorDn.bPressed)
			{
				this._guiOrbitDraw.MoveCrosshair(0.0, -2.0);
			}
			if (this.gphErrorUp.bPressed)
			{
				this._guiOrbitDraw.MoveCrosshair(0.0, 2.0);
			}
			if (this.gphErrorLt.bPressed)
			{
				this._guiOrbitDraw.MoveCrosshair(-2.0, 0.0);
			}
			if (this.gphErrorRt.bPressed)
			{
				this._guiOrbitDraw.MoveCrosshair(2.0, 0.0);
			}
			NavData navData = this.NavPlan;
			if (this.COSelf.ship.objSS.HasNavData())
			{
				navData = this.COSelf.ship.objSS.NavData;
			}
			if (navData != null)
			{
				double num = (navData.GetArrivalEpoch() - StarSystem.fEpoch) / 3600.0;
				this.txtCrsDuration.text = num.ToString("#.00") + "h";
				double arrivalTorchFuel = navData.GetArrivalTorchFuel();
				if (arrivalTorchFuel < 0.0)
				{
					this.txtCrsFuelReactant.text = ((this.COSelf.ship.fShallowFusionRemain + -1.0 * arrivalTorchFuel) / 3600.0).ToString("#.00") + "h";
					this.txtCrsFuelReactant.color = this._guiOrbitDraw.clrRed01;
				}
				else
				{
					this.txtCrsFuelReactant.text = ((this.COSelf.ship.fShallowFusionRemain - navData.GetArrivalTorchFuel()) / 3600.0).ToString("#.00") + "h";
					this.txtCrsFuelReactant.color = this.clrCoursePlotText;
				}
				this.txtCrsFuelRCS.text = (this.COSelf.ship.GetRCSRemain() - navData.GetArrivalRCSFuel()).ToString("#.00") + "kg";
				this.txtCrsAcc.text = (this.COSelf.ship.GetMaxTorchThrust(this.slidCrsLim.value) / 6.684587E-12f / 9.81f).ToString("#.00") + "G";
			}
			else
			{
				this.txtCrsDuration.text = "--";
				this.txtCrsFuelReactant.text = "--";
				this.txtCrsFuelRCS.text = "--";
				this.txtCrsAcc.text = "--";
			}
			if (this.COSelf.ship.Reactor != null && this.COSelf.ship.Reactor.HasCond("IsAutopilotAborted"))
			{
				this.COSelf.ship.Reactor.ZeroCondAmount("IsAutopilotAborted");
				this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_INVALIDATED", false);
				this.chkEngage.isOn = false;
				this._guiOrbitDraw.SetPropMapData("chkEngage", false.ToString());
			}
		}

		private void EngageTrip()
		{
			if (this.NavPlan == null)
			{
				this._guiOrbitDraw.CGClampWarning("GUI_ORBIT_WARN_AUTOPILOT_NOPLAN", false);
			}
			this.COSelf.ship.objSS.NavData = this.NavPlan;
			AIShipManager.UnregisterShip(this.COSelf.ship);
			AIShip aiship = AIShipManager.AddAIToShip(this.COSelf.ship, AIType.Auto, "INTERREGIONAL", new JsonAIShipSave
			{
				strATCLast = AIShipManager.strATCLast,
				strRegId = this.COSelf.ship.strRegID,
				strHomeStation = "OKLG",
				enumAIType = AIType.Auto,
				strActiveCommand = "FlyToAutoPilot",
				strActiveCommandPayload = new string[]
				{
					this._lastTorchSpeedLimit.ToString()
				}
			});
			this.COSelf.ship.LogAdd(DataHandler.GetString("NAV_LOG_AP_ENABLED", false), StarSystem.fEpoch, true);
			this.COSelf.ship.StopManeuver(true, true);
		}

		private void EndTrip()
		{
			this.chkEngage.isOn = false;
		}

		private bool EvaluateIsOutsideNoWakeRange(double fEpoch)
		{
			return !CrewSim.system.IsWithinNoWakeRangeOfAnyStation(this.COSelf.ship.objSS, fEpoch);
		}

		private bool EvaluateMinimumAPDistance(double targetX, double targetY)
		{
			double distance = MathUtils.GetDistance(this.COSelf.ship.objSS.vPosx, this.COSelf.ship.objSS.vPosy, targetX, targetY);
			return distance < 3.342293712194078E-05;
		}

		private bool EvaluateMinimumAPDistance(NavDataPoint navDest)
		{
			if (navDest == null)
			{
				return true;
			}
			navDest.ObjSS.UpdateTime(StarSystem.fEpoch, false);
			double distance = MathUtils.GetDistance(this.COSelf.ship.objSS.vPosx, this.COSelf.ship.objSS.vPosy, navDest.ObjSS.vPosx, navDest.ObjSS.vPosy);
			return distance < 3.342293712194078E-05;
		}

		private bool EvaluateIsReactorCycleActive()
		{
			if (this.NavPlan == null)
			{
				return false;
			}
			bool flag = this.NavPlan.GetArrivalTorchFuel() < this.COSelf.ship.fShallowFusionRemain;
			string reactorGPMValue = this.COSelf.ship.GetReactorGPMValue("knobRatio");
			int num = 0;
			if (!string.IsNullOrEmpty(reactorGPMValue))
			{
				num = int.Parse(reactorGPMValue);
			}
			return !flag || num == 1;
		}

		private bool EvaluateIsReactorFueledAndOn()
		{
			if (this.NavPlan == null)
			{
				return false;
			}
			bool flag = this.NavPlan.GetArrivalTorchFuel() < this.COSelf.ship.fShallowFusionRemain;
			return !flag || this.COSelf.ship.bFusionReactorRunning;
		}

		private bool EvaluateRCS()
		{
			if (this.NavPlan == null)
			{
				return false;
			}
			double arrivalRCSFuel = this.NavPlan.GetArrivalRCSFuel();
			double rcsremain = this.COSelf.ship.GetRCSRemain();
			Debug.Log(string.Concat(new object[]
			{
				"arrival: ",
				arrivalRCSFuel,
				" current: ",
				rcsremain
			}));
			return arrivalRCSFuel > 0.0 && rcsremain > 0.0;
		}

		private bool EvaluateIsCloseEnough()
		{
			if (this.NavPlan == null)
			{
				return false;
			}
			NavDataPoint origin = this.NavPlan.Origin;
			return origin != null && this.COSelf.ship.objSS.GetDistance(origin.ObjSS) <= (double)(this.COSelf.ship.objSS.GetRadiusAU() * 10f) && StarSystem.fEpoch - origin.ArrivalTime <= 60.0;
		}

		[SerializeField]
		private Button btnErrorDn;

		[SerializeField]
		private Button btnErrorLt;

		[SerializeField]
		private Button btnErrorRt;

		[SerializeField]
		private Button btnErrorUp;

		[SerializeField]
		private Button btnError0;

		[SerializeField]
		private Button btnCrsPlot;

		[SerializeField]
		public Slider slidCrsLim;

		[SerializeField]
		private Slider slidCrsCoast;

		[SerializeField]
		private TMP_Text txtCrsDuration;

		[SerializeField]
		private TMP_Text txtCrsAcc;

		[SerializeField]
		private TMP_Text txtCrsFuelRCS;

		[SerializeField]
		private TMP_Text txtCrsFuelReactant;

		[SerializeField]
		public Toggle chkEngage;

		private Color clrCoursePlotText;

		private GUIBtnPressHold gphErrorDn;

		private GUIBtnPressHold gphErrorUp;

		private GUIBtnPressHold gphErrorLt;

		private GUIBtnPressHold gphErrorRt;

		private double _lastTorchSpeedLimit;

		private Coroutine _tripPlanerCoRoutine;

		private NavData _navPlan;

		private UnityAction<bool> eEngage;
	}
}
