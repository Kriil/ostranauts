using System;
using System.Collections.Generic;
using Ostranauts.Core.Tutorials;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Helmet visor and EVA HUD overlay. Likely swaps between unpowered, powered,
// and damaged suit displays based on the player's room, suit, and gas state.
public class GUIHelmet : MonoBehaviour
{
	// Lazy UI binding: finds the visor widgets, resolves the key suit-related
	// CondTriggers, and initializes gauge danger ranges.
	private void Init()
	{
		this.cg = base.transform.Find("bmpHelmet").GetComponent<CanvasGroup>();
		this.cgTunnel = base.transform.Find("bmpTunnel").GetComponent<CanvasGroup>();
		this.cgHUD = base.transform.Find("bmpHelmet/pnlHUD").GetComponent<CanvasGroup>();
		this.cgGauge = base.transform.Find("bmpHelmet/bmpGauge").GetComponent<CanvasGroup>();
		this.ghO2Ext = base.transform.Find("bmpHelmet/pnlHUD/pnlO2/bmpExt").GetComponent<GUIHelmetBar>();
		this.ghO2Int = base.transform.Find("bmpHelmet/pnlHUD/pnlO2/bmpInt").GetComponent<GUIHelmetBar>();
		this.ghPressExt = base.transform.Find("bmpHelmet/pnlHUD/pnlPress/bmpExt").GetComponent<GUIHelmetBar>();
		this.ghPressInt = base.transform.Find("bmpHelmet/pnlHUD/pnlPress/bmpInt").GetComponent<GUIHelmetBar>();
		this.ghTempExt = base.transform.Find("bmpHelmet/pnlHUD/pnlTemp/bmpExt").GetComponent<GUIHelmetBar>();
		this.ghTempInt = base.transform.Find("bmpHelmet/pnlHUD/pnlTemp/bmpInt").GetComponent<GUIHelmetBar>();
		this.lampO2 = base.transform.Find("bmpHelmet/bmpGauge/bmpO2").GetComponent<GUILamp>();
		this.lampCO2 = base.transform.Find("bmpHelmet/bmpGauge/bmpTemp").GetComponent<GUILamp>();
		this.txtO2 = base.transform.Find("bmpHelmet/pnlHUD/pnlO2Stock/lblValue").GetComponent<TMP_Text>();
		this.txtBatt = base.transform.Find("bmpHelmet/pnlHUD/pnlBattTime/lblValue").GetComponent<TMP_Text>();
		this.tfHelmet = base.transform.Find("bmpHelmet").GetComponent<RectTransform>();
		this.tfTunnel = base.transform.Find("bmpTunnel").GetComponent<RectTransform>();
		this.ctEVA = DataHandler.GetCondTrigger("TIsEVAOn");
		this.ctEVABatt = DataHandler.GetCondTrigger("TIsFitContainerEVABattery");
		this.ctEVABottle = DataHandler.GetCondTrigger("TIsFitContainerEVABottle");
		this.ctPS = DataHandler.GetCondTrigger("TIsPressureSuit");
		this.asO2Beep = base.transform.Find("bmpHelmet/pnlHUD").GetComponent<AudioSource>();
		this.hsCurrent = GUIHelmet.HelmetStyle.Unpowered;
		this.tfHelmet.gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG("GUIHelmetDark.png", false, false);
		this.ghO2Int.Min = 0.0;
		this.ghO2Int.Max = 160.0;
		this.ghO2Int.DangerLow = this.fO2PPMin;
		this.ghO2Int.DangerHigh = this.fO2PPMax;
		this.ghO2Ext.Min = 0.0;
		this.ghO2Ext.Max = 160.0;
		this.ghO2Ext.DangerLow = this.fO2PPMin;
		this.ghO2Ext.DangerHigh = this.fO2PPMax;
		this.ghTempInt.Min = 0.0;
		this.ghTempInt.Max = 450.0;
		this.ghTempInt.DangerLow = 301.0;
		this.ghTempInt.DangerHigh = 316.0;
		this.ghTempExt.Min = 0.0;
		this.ghTempExt.Max = 450.0;
		this.ghTempExt.DangerLow = this.fTempMin;
		this.ghTempExt.DangerHigh = this.fTempMax;
		this.ghPressInt.Min = 0.0;
		this.ghPressInt.Max = 250.0;
		this.ghPressInt.DangerLow = this.fO2PPMin;
		this.ghPressInt.DangerHigh = this.fO2PPMax;
		this.ghPressExt.Min = 0.0;
		this.ghPressExt.Max = 250.0;
		this.ghPressExt.DangerLow = this.ghPressInt.Value - this.fPressureDiffMax;
		this.ghPressExt.DangerHigh = this.ghPressInt.Value + this.fPressureDiffMax;
		this.lampO2.State = 0;
		this.lampCO2.State = 0;
		this.Visible = false;
		this.TunnelOn = false;
		this.cgHUD.alpha = 0f;
		this.cgGauge.alpha = 0f;
		this.bInit = true;
	}

	// Unity update: lazy-initializes once, then animates tunnel fade and visor
	// motion each frame.
	private void Update()
	{
		if (!this.bInit)
		{
			this.Init();
		}
		this.ShiftHelmet();
		if (this.cgTunnel.alpha > this.fTunnelOpacityTarget)
		{
			this.cgTunnel.alpha -= this.ftunnelRate * CrewSim.TimeElapsedScaled();
		}
		else if (this.cgTunnel.alpha < this.fTunnelOpacityTarget)
		{
			this.cgTunnel.alpha += this.ftunnelRate * CrewSim.TimeElapsedScaled();
		}
	}

	// Adds a subtle mouse-driven parallax shift to the helmet and tunnel sprites.
	private void ShiftHelmet()
	{
		float num = this.tfHelmet.rect.width / (float)Screen.width;
		Vector3 vector = new Vector3(Input.mousePosition.x / (float)Screen.width, Input.mousePosition.y / (float)Screen.height, 0f);
		vector.x -= 0.5f;
		vector.y -= 0.5f;
		vector *= 90f;
		vector.x = Mathf.Clamp(vector.x, -45f, 45f);
		vector.y = Mathf.Clamp(vector.y, -45f, 45f);
		this.tfHelmet.anchoredPosition = vector;
		this.tfTunnel.anchoredPosition = vector;
	}

	// Damaged/pressure-suit fallback when the full powered HUD is unavailable.
	// Uses room gas values directly and can trigger the low-O2 tutorial.
	public void UpdateUIDmg(bool bPS, CondOwner coRoom)
	{
		this.Style = GUIHelmet.HelmetStyle.Damaged;
		this.Visible = true;
		this.GaugeOn = bPS;
		this.HUDOn = false;
		if (bPS && coRoom != null)
		{
			double condAmount = coRoom.GetCondAmount("StatGasPpO2");
			double condAmount2 = coRoom.GetCondAmount("StatGasPpCO2");
			this.UpdatePSGauge(condAmount, condAmount2);
			if (condAmount <= this.fO2PPMin || condAmount2 >= this.fCO2Max)
			{
				this.TriggerTutorial();
			}
		}
	}

	// Main helmet HUD refresh. Reads the player's room, suit inventory, internal
	// tanks/battery state, and environmental gas to choose the active visor mode.
	public void UpdateUI(CondOwner coRoomIn, CondOwner coRoomOut)
	{
		if (coRoomIn == null || !coRoomIn.HasCond("IsHuman"))
		{
			this.Style = GUIHelmet.HelmetStyle.None;
			this.Visible = false;
			return;
		}
		if (coRoomOut == null)
		{
			return;
		}
		if (!this.bInit)
		{
			this.Init();
		}
		this.Visible = true;
		List<CondOwner> list = new List<CondOwner>();
		bool flag = true;
		bool flag2 = false;
		bool flag3 = false;
		if (coRoomIn.HasCond("IsEVAHUD"))
		{
			list = coRoomIn.compSlots.GetCOs("shirt_out", false, this.ctEVA);
			if (list.Count > 0)
			{
				CondOwner condOwner = list[0];
				flag2 = true;
				this.Style = GUIHelmet.HelmetStyle.Powered;
				if (flag2)
				{
					list = condOwner.GetCOs(false, null);
					bool flag4 = false;
					if (list != null)
					{
						foreach (CondOwner condOwner2 in list)
						{
							if (!flag4 && this.ctEVABottle.Triggered(condOwner2, null, false))
							{
								double num = condOwner2.GetCondAmount("StatGasMolO2") / condOwner2.GetCondAmount("StatRef") * 100.0;
								this.txtO2.text = num.ToString("n2") + "%";
								flag = false;
								if (num != this.fO2Last)
								{
									this.asO2Beep.Play();
									this.fO2Last = num;
								}
								flag4 = true;
							}
							else if (this.ctEVABatt.Triggered(condOwner2, null, false))
							{
								Powered component = condOwner2.GetComponent<Powered>();
								double num2 = condOwner2.GetCondAmount("StatPowerMax");
								if (component != null)
								{
									num2 = component.PowerStoredMax;
								}
								if (num2 == 0.0)
								{
									num2 = 1.0;
								}
								double num3 = condOwner2.GetCondAmount("StatPower") / num2 * 100.0;
								this.txtBatt.text = num3.ToString("n2") + "%";
							}
						}
					}
				}
			}
			else
			{
				this.Style = GUIHelmet.HelmetStyle.Unpowered;
			}
		}
		else if (coRoomIn.HasCond("IsPSHUD"))
		{
			flag3 = true;
			this.Style = GUIHelmet.HelmetStyle.Powered;
		}
		this.HUDOn = flag2;
		this.GaugeOn = flag3;
		double condAmount = coRoomIn.GetCondAmount("StatGasPpO2");
		double condAmount2 = coRoomIn.GetCondAmount("StatGasPpCO2");
		if (condAmount <= this.fO2PPMin || condAmount2 >= this.fCO2Max)
		{
			this.TriggerTutorial();
		}
		if (flag3)
		{
			this.UpdatePSGauge(condAmount, condAmount2);
		}
		else if (flag2)
		{
			if (flag)
			{
				this.txtO2.text = "ERROR";
			}
			this.ghO2Int.Value = coRoomIn.GetCondAmount("StatGasPpO2");
			this.ghO2Ext.Value = coRoomOut.GetCondAmount("StatGasPpO2");
			this.ghPressInt.Value = coRoomIn.GetCondAmount("StatGasPressure");
			this.ghPressExt.Value = coRoomOut.GetCondAmount("StatGasPressure");
			this.ghTempInt.Value = coRoomIn.GetCondAmount("StatGasTemp");
			this.ghTempExt.Value = coRoomOut.GetCondAmount("StatGasTemp");
			this.ghPressExt.DangerLow = this.ghPressInt.Value - this.fPressureDiffMax;
			this.ghPressExt.DangerHigh = this.ghPressInt.Value + this.fPressureDiffMax;
		}
	}

	private void UpdatePSGauge(double fPPO2, double fPPCO2)
	{
		if (fPPO2 > this.fO2PPMin)
		{
			this.lampO2.State = 0;
		}
		else
		{
			if (this.lampO2.State == 0 && (double)Time.timeScale > 1.0)
			{
				CrewSim.ResetTimeScale();
			}
			this.lampO2.State = 2;
		}
		if (fPPCO2 < this.fCO2Max)
		{
			this.lampCO2.State = 0;
		}
		else
		{
			if (this.lampCO2.State == 0 && (double)Time.timeScale > 1.0)
			{
				CrewSim.ResetTimeScale();
			}
			this.lampCO2.State = 2;
		}
		if (this.nIndexOld < this.lampO2.ImageIndex)
		{
			this.nBeepIndex--;
			if (this.nBeepIndex <= 0)
			{
				AudioManager.am.PlayAudioEmitter("HelmetO2Alarm", false, false);
				this.nBeepIndex = 4;
			}
			this.nIndexOld = this.lampO2.ImageIndex;
		}
		else if (this.nIndexOld > this.lampO2.ImageIndex)
		{
			this.nIndexOld = this.lampO2.ImageIndex;
		}
		if (this.nCO2IndexOld < this.lampCO2.ImageIndex)
		{
			this.nCO2BeepIndex--;
			if (this.nCO2BeepIndex <= 0)
			{
				AudioManager.am.PlayAudioEmitter("HelmetO2Alarm", false, false);
				this.nCO2BeepIndex = 4;
			}
			this.nCO2IndexOld = this.lampCO2.ImageIndex;
		}
		else if (this.nCO2IndexOld > this.lampCO2.ImageIndex)
		{
			this.nCO2IndexOld = this.lampCO2.ImageIndex;
		}
	}

	private void TriggerTutorial()
	{
		if (this._hasSeenHelmetAtmoTutorial || !CrewSim.coPlayer.HasCond("IsAIManual"))
		{
			return;
		}
		if (CrewSim.coPlayer.HasCond("IsGasContCarving"))
		{
			return;
		}
		this._hasSeenHelmetAtmoTutorial = (CrewSim.coPlayer.HasCond("TutorialHelmetAtmoShow") || CrewSim.coPlayer.HasCond("TutorialHelmetAtmoComplete"));
		if (!this._hasSeenHelmetAtmoTutorial)
		{
			CrewSimTut.BeginTutorialBeat<HelmetAtmosphereUnsafe>();
		}
	}

	public float GetTunnelAmount(CondOwner co)
	{
		if (co == null)
		{
			return 0f;
		}
		if (co.HasCond("DcGrav04") || co.HasCond("DcGrav05"))
		{
			return 1f;
		}
		if (co.HasCond("DcGrav03"))
		{
			return 0.8f;
		}
		return 0f;
	}

	public void TunnelOpacity(float fAmount, bool bInstant)
	{
		fAmount = Mathf.Clamp(fAmount, 0f, 1f);
		this.fTunnelOpacityTarget = fAmount;
		if (bInstant)
		{
			this.cgTunnel.alpha = fAmount;
		}
	}

	public bool Visible
	{
		get
		{
			return this.cg.alpha > 0f;
		}
		set
		{
			if (value)
			{
				this.cg.alpha = 1f;
			}
			else
			{
				this.cg.alpha = 0f;
			}
		}
	}

	public bool TunnelOn
	{
		get
		{
			return this.cgTunnel.alpha > 0f;
		}
		set
		{
			if (value)
			{
				this.cgTunnel.alpha = 1f;
			}
			else
			{
				this.cgTunnel.alpha = 0f;
			}
		}
	}

	private bool HUDOn
	{
		get
		{
			return this.cgHUD.alpha > 0f;
		}
		set
		{
			if (this.cgHUD.alpha > 0f == value)
			{
				return;
			}
			if (value)
			{
				this.cgHUD.alpha = 1f;
			}
			else
			{
				this.cgHUD.alpha = 0f;
			}
		}
	}

	public GUIHelmet.HelmetStyle Style
	{
		get
		{
			return this.hsCurrent;
		}
		set
		{
			if (this.hsCurrent == value)
			{
				return;
			}
			if (value != GUIHelmet.HelmetStyle.Powered)
			{
				if (value != GUIHelmet.HelmetStyle.Damaged)
				{
					this.tfHelmet.gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG("GUIHelmetDark.png", false, false);
				}
				else
				{
					this.tfHelmet.gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG("GUIHelmetDmg.png", false, false);
				}
			}
			else
			{
				this.tfHelmet.gameObject.GetComponent<RawImage>().texture = DataHandler.LoadPNG("GUIHelmet.png", false, false);
			}
			this.hsCurrent = value;
		}
	}

	private bool GaugeOn
	{
		get
		{
			return this.cgGauge.alpha > 0f;
		}
		set
		{
			if (this.cgGauge.alpha > 0f == value)
			{
				return;
			}
			if (value)
			{
				this.cgGauge.alpha = 1f;
			}
			else
			{
				this.cgGauge.alpha = 0f;
			}
		}
	}

	private GUIHelmetBar ghO2Ext;

	private GUIHelmetBar ghO2Int;

	private GUIHelmetBar ghPressExt;

	private GUIHelmetBar ghPressInt;

	private GUIHelmetBar ghTempExt;

	private GUIHelmetBar ghTempInt;

	private GUILamp lampO2;

	private GUILamp lampCO2;

	private TMP_Text txtO2;

	private TMP_Text txtBatt;

	private CanvasGroup cg;

	private CanvasGroup cgTunnel;

	private CanvasGroup cgHUD;

	private CanvasGroup cgGauge;

	private RectTransform tfHelmet;

	private RectTransform tfTunnel;

	private CondTrigger ctEVA;

	private CondTrigger ctEVABatt;

	private CondTrigger ctEVABottle;

	private CondTrigger ctPS;

	private AudioSource asO2Beep;

	private double fTempMin;

	private double fTempMax = 394.0;

	private double fPressureDiffMax = 202.6;

	private double fPressureMax = 1378.0;

	private double fO2PPMin = 17.0;

	private double fO2PPMax = 100.0;

	private double fO2Last;

	private int nIndexOld;

	private int nBeepIndex;

	private double fCO2Max = 1.5;

	private float fTunnelOpacityTarget;

	private float ftunnelRate = 0.15f;

	private int nCO2IndexOld;

	private int nCO2BeepIndex;

	private bool bInit;

	private GUIHelmet.HelmetStyle hsCurrent = GUIHelmet.HelmetStyle.Unpowered;

	private bool _hasSeenHelmetAtmoTutorial;

	public enum HelmetStyle
	{
		None,
		Unpowered,
		Powered,
		Damaged
	}
}
