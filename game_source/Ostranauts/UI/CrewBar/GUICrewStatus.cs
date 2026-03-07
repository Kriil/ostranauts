using System;
using System.Collections;
using System.Collections.Generic;
using Ostranauts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.CrewBar
{
	public class GUICrewStatus : MonoSingleton<GUICrewStatus>
	{
		private new void Awake()
		{
			this.InitPhysioDef();
			this._btnToggleCards.onClick.AddListener(delegate()
			{
				this.ToggleCards(false);
			});
			this.ttRank = this._bmpRank.GetComponent<Tooltippable2>();
			this.ttShift = this._bmpShift.GetComponent<Tooltippable2>();
		}

		private IEnumerator Start()
		{
			this.SetupCrewBar();
			this.ToggleCards(true);
			yield return new WaitUntil(() => CrewSim.coPlayer != null);
			this.SyncMainPortrait();
			yield break;
		}

		public void ToggleCards(bool showBars = false)
		{
			if (showBars || !this._pnlFillBars.activeInHierarchy)
			{
				this._pnlFillBars.SetActive(true);
				this._crewCardHost.Show(false);
				this._lblName.gameObject.SetActive(true);
				this.SetToggleButton(true);
				this._bmpShift.gameObject.SetActive(true);
			}
			else
			{
				this._pnlFillBars.SetActive(false);
				this._crewCardHost.Show(true);
				this._lblName.gameObject.SetActive(false);
				this.SetToggleButton(false);
				this._bmpShift.gameObject.SetActive(false);
			}
		}

		private void SetToggleButton(bool showCards)
		{
			if (showCards)
			{
				this._imgToggleCards.sprite = this._spriteCrew;
				this._txtToggleCards.text = "CREW";
			}
			else
			{
				this._imgToggleCards.sprite = this._spriteBars;
				this._txtToggleCards.text = "STATS";
			}
		}

		private void SetupCrewBar()
		{
			for (int i = 0; i < this.aStatBars.Length; i++)
			{
				this.aStatBars[i] = base.transform.Find("pnlFillBars/physio" + (i + 1).ToString("00")).GetComponent<PhysioIndicator>();
				this.aStatBars[i].Init(null);
			}
		}

		public void UpdateCrewBar(CondOwner co)
		{
			if (CrewSim.bShipEdit)
			{
				return;
			}
			if (this._lblName.text != co.strName)
			{
				this._lblName.text = co.strName;
				if (this._pnlFillBars.activeInHierarchy)
				{
					this.Refresh();
				}
			}
			if (co.currentRoom == null)
			{
				co.currentRoom = co.ship.GetRoomAtWorldCoords1(co.tf.position, true);
			}
			CondOwner coRoomIn = null;
			CondOwner condOwner = null;
			CondOwner condOwner2 = null;
			double num = 1.0;
			if (co.currentRoom != null && co.currentRoom.CO != null)
			{
				condOwner = co.currentRoom.CO;
				if (!co.currentRoom.Void)
				{
					num = 0.0;
				}
			}
			bool flag = false;
			if (co.HasCond("IsAirtight"))
			{
				coRoomIn = co;
				condOwner2 = co;
			}
			else if (co.HasCond("IsDmgHUD"))
			{
				flag = true;
			}
			if (condOwner2 == null)
			{
				condOwner2 = condOwner;
			}
			double num2 = 0.0;
			if (condOwner2 != null)
			{
				this.aStatBars[0].UpdateCO(condOwner2, false);
				this.aStatBars[1].UpdateCO(condOwner2, false);
				num2 = condOwner2.GetCondAmount("StatGasPressure") / 101.30000305175781;
			}
			AudioManager.am.EnvPressure = Mathf.Clamp((float)num2, 0.001f, 1f);
			AudioManager.am.WeatherFilter = Mathf.Clamp((float)num, 0.001f, 1f);
			if (flag)
			{
				CanvasManager.instance.helmet.UpdateUIDmg(co.HasCond("IsPSHUD"), condOwner);
			}
			else
			{
				CanvasManager.instance.helmet.UpdateUI(coRoomIn, condOwner);
			}
			AudioManager.am.Helmet = CanvasManager.instance.helmet.Style;
			for (int i = 2; i < this.aStatBars.Length; i++)
			{
				this.aStatBars[i].UpdateCO(co, false);
			}
			if (co == CrewSim.coPlayer)
			{
				this._bmpRank.texture = GUICrewCard.GetShiftRankIcon("Captain");
				this.ttRank.SetData("Rank", "Captain", false);
			}
			else
			{
				this._bmpRank.texture = GUICrewCard.GetShiftRankIcon("Crew");
				this.ttRank.SetData("Rank", "Crew", false);
			}
			string text = "blank";
			if (co.jsShiftLast != null)
			{
				text = co.jsShiftLast.strName;
			}
			if (this.strShiftLast != text)
			{
				this._bmpShift.texture = GUICrewCard.GetShiftRankIcon(text);
				this.ttShift.SetData("Shift", text, false);
				this.strShiftLast = text;
			}
		}

		public void Hide()
		{
			Canvas component = base.GetComponent<Canvas>();
			if (component != null)
			{
				component.enabled = false;
			}
			else
			{
				Debug.LogWarning("CrewStatus canvas not found");
			}
		}

		public void Show()
		{
			Canvas component = base.GetComponent<Canvas>();
			if (component != null)
			{
				component.enabled = true;
			}
			else
			{
				Debug.LogWarning("CrewStatus canvas not found");
			}
		}

		public void Refresh()
		{
			this._crewCardHost.Refresh();
			this.SyncMainPortrait();
		}

		private void SyncMainPortrait()
		{
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if (selectedCrew == null)
			{
				this._bmpPortrait.texture = null;
				this._coIdMainPortrait = string.Empty;
			}
			else if (selectedCrew.strID != this._coIdMainPortrait)
			{
				this._bmpPortrait.texture = MonoSingleton<GUIRenderTargets>.Instance.CreatePortrait(selectedCrew);
				this._coIdMainPortrait = selectedCrew.strID;
			}
		}

		private void InitPhysioDef()
		{
			this._physioDefs.Add("StatGasPressure", new PhysioDef
			{
				Title = "GUI_STAT_ATMO_PRESSURE",
				Icon = "icons/Air",
				IconNeg = "icons/Air",
				StatTracked = "StatGasPressure",
				Minimum = 100.0,
				Maximum = 100.0,
				InverseMaximum = -100.0,
				NeedsRoom = true,
				CondTrack = new List<string>
				{
					"DcGasPressure01",
					"DcGasPressure02",
					"DcGasPressure03"
				}
			});
			this._physioDefs.Add("StatGasTemp", new PhysioDef
			{
				Title = "GUI_STAT_ATMO_TEMP",
				Icon = "icons/TempHot1",
				IconNeg = "icons/TempHot1",
				StatTracked = "StatGasTemp",
				Minimum = 297.5,
				Maximum = 20.0,
				InverseMaximum = -20.0,
				NeedsRoom = true,
				CondTrack = new List<string>
				{
					"DcGasTemp01",
					"DcGasTemp02",
					"DcGasTemp03"
				}
			});
			this._physioDefs.Add("StatSolidTemp", new PhysioDef
			{
				Title = "GUI_STAT_BODY_TEMP",
				Icon = "icons/TempHot2",
				IconNeg = "icons/TempHot2",
				StatTracked = "StatSolidTemp",
				Minimum = 309.85,
				Maximum = 5.85,
				InverseMaximum = -16.65,
				CondTrack = new List<string>
				{
					"DcBodyTemp01",
					"DcBodyTemp02",
					"DcBodyTemp02",
					"DcBodyTemp03",
					"DcBodyTemp04",
					"DcBodyTemp05",
					"DcBodyTemp06",
					"DcBodyTemp07",
					"DcBodyTemp08",
					"DcBodyTemp09"
				}
			});
			this._physioDefs.Add("StatPain", new PhysioDef
			{
				Title = "GUI_STAT_PAIN",
				Icon = "icons/Pain",
				IconNeg = "icons/Pain",
				StatTracked = "StatPain",
				Minimum = 0.0,
				Maximum = 100.0,
				InverseMaximum = -100.0,
				CondTrack = new List<string>
				{
					"DcPain00",
					"DcPain01",
					"DcPain02",
					"DcPain03",
					"DcPain04"
				}
			});
			this._physioDefs.Add("StatSatiety", new PhysioDef
			{
				Title = "GUI_STAT_HUNGER",
				Icon = "icons/MealNeg",
				IconNeg = "icons/MealPos",
				StatTracked = "StatSatiety",
				Minimum = 6.0,
				Maximum = 12.0,
				InverseMaximum = -6.0,
				CondTrack = new List<string>
				{
					"DcSatiety01",
					"DcSatiety02",
					"DcSatiety03",
					"DcSatiety04"
				}
			});
			this._physioDefs.Add("StatHydration", new PhysioDef
			{
				Title = "GUI_STAT_HYDRATION",
				Icon = "icons/Glass",
				IconNeg = "icons/Glass",
				StatTracked = "StatHydration",
				Minimum = 0.0,
				Maximum = 50.0,
				InverseMaximum = -100.0,
				CondTrack = new List<string>
				{
					"DcHydration01",
					"DcHydration02",
					"DcHydration03",
					"DcHydration04",
					"DcHydration05"
				}
			});
			this._physioDefs.Add("StatEncumbrance", new PhysioDef
			{
				Title = "GUI_STAT_ENCUMBERANCE",
				Icon = "icons/Weight",
				IconNeg = "icons/Weight",
				StatTracked = "StatEncumbrance",
				Minimum = 0.0,
				Maximum = 200.0,
				InverseMaximum = -100.0,
				CondTrack = new List<string>
				{
					"DcEncumbrance01",
					"DcEncumbrance02",
					"DcEncumbrance03",
					"DcEncumbrance04"
				}
			});
			this._physioDefs.Add("StatDefecate", new PhysioDef
			{
				Title = "GUI_STAT_DEFECATE",
				Icon = "icons/Bowels",
				IconNeg = "icons/Bowels",
				StatTracked = "StatDefecate",
				Minimum = 0.0,
				Maximum = 100.0,
				InverseMaximum = -100.0,
				CondTrack = new List<string>
				{
					"DcDefecate01",
					"DcDefecate02",
					"DcDefecate03",
					"DcDefecate04",
					"DcDefecate05"
				}
			});
			this._physioDefs.Add("StatFatigue", new PhysioDef
			{
				Title = "GUI_STAT_FATIGUE",
				Icon = "icons/Fatigue",
				IconNeg = "icons/Fatigue",
				StatTracked = "StatFatigue",
				Minimum = 0.0,
				Maximum = 5.0,
				InverseMaximum = -100.0,
				CondTrack = new List<string>
				{
					"DcFatigue01",
					"DcFatigue02",
					"DcFatigue03",
					"DcFatigue04",
					"DcFatigue05"
				}
			});
			this._physioDefs.Add("StatSleep", new PhysioDef
			{
				Title = "GUI_STAT_SLEEP",
				Icon = "icons/Sleep",
				IconNeg = "icons/Sleep",
				StatTracked = "StatSleep",
				Minimum = 0.0,
				Maximum = 80.0,
				InverseMaximum = -100.0,
				CondTrack = new List<string>
				{
					"DcSleep00",
					"DcSleep01",
					"DcSleep02",
					"DcSleep03",
					"DcSleep04",
					"DcSleep05"
				}
			});
			this._physioDefs.Add("StatHygiene", new PhysioDef
			{
				Title = "GUI_STAT_HYGIENE",
				Icon = "icons/Hygiene",
				IconNeg = "icons/Hygiene",
				StatTracked = "StatHygiene",
				Minimum = 0.0,
				Maximum = 100.0,
				InverseMaximum = -100.0,
				CondTrack = new List<string>
				{
					"DcHygiene01",
					"DcHygiene02",
					"DcHygiene03",
					"DcHygiene04"
				}
			});
		}

		public PhysioDef GetPhysioDef(string condName)
		{
			PhysioDef result = null;
			this._physioDefs.TryGetValue(condName, out result);
			return result;
		}

		[Header("Toggle Button")]
		[SerializeField]
		private Button _btnToggleCards;

		[SerializeField]
		private Image _imgToggleCards;

		[SerializeField]
		private TMP_Text _txtToggleCards;

		[SerializeField]
		private Sprite _spriteCrew;

		[SerializeField]
		private Sprite _spriteBars;

		[SerializeField]
		private RawImage _bmpRank;

		[SerializeField]
		private RawImage _bmpShift;

		[SerializeField]
		private RawImage _bmpPortrait;

		[SerializeField]
		private GameObject _pnlFillBars;

		[SerializeField]
		private TMP_Text _lblName;

		[SerializeField]
		private GUICrewCardHost _crewCardHost;

		private Tooltippable2 ttRank;

		private Tooltippable2 ttShift;

		private string strShiftLast;

		private string _coIdMainPortrait = string.Empty;

		private PhysioIndicator[] aStatBars = new PhysioIndicator[11];

		public Dictionary<string, PhysioDef> _physioDefs = new Dictionary<string, PhysioDef>();
	}
}
