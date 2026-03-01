using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.CrewBar
{
	public class GUICrewCard : MonoBehaviour
	{
		private CondOwner CO
		{
			get
			{
				if (this._co == null && this._coId != null)
				{
					DataHandler.mapCOs.TryGetValue(this._coId, out this._co);
				}
				return this._co;
			}
			set
			{
				this._co = value;
				this._coId = ((!(value != null)) ? null : value.strID);
			}
		}

		public void SetData(CondOwner co)
		{
			if (this.ttRank == null)
			{
				this.ttRank = this._bmpRank.GetComponent<Tooltippable2>();
				this.ttShift = this._bmpShift.GetComponent<Tooltippable2>();
			}
			this.CO = co;
			Texture texture = MonoSingleton<GUIRenderTargets>.Instance.CreatePortrait(co);
			this._portraitImg.texture = texture;
			bool flag = co.strID == CrewSim.coPlayer.strID;
			this._txtName.text = co.FriendlyName;
			this._txtName.fontStyle = FontStyles.Normal;
			if (flag)
			{
				this._bmpRank.texture = GUICrewCard.GetShiftRankIcon("Captain");
				this.ttRank.SetData("Rank", "Captain", false);
			}
			else
			{
				this._bmpRank.texture = GUICrewCard.GetShiftRankIcon("Crew");
				this.ttRank.SetData("Rank", "Crew", false);
			}
			this._isSelected = (CrewSim.GetSelectedCrew() == co);
			this._imgSelectionBracket.SetActive(this._isSelected);
			this.SetButton();
		}

		private void SetButton()
		{
			this._portraitBtn.onClick.RemoveAllListeners();
			if (this._isSelected)
			{
				this._portraitBtn.onClick.AddListener(delegate()
				{
					if (!CrewSim.bRaiseUI)
					{
						CommandInventory.ToggleInventory(CrewSim.GetSelectedCrew(), false);
					}
				});
			}
			else
			{
				this._portraitBtn.onClick.AddListener(delegate()
				{
					CrewSim.objInstance.CycleCrew(this._co);
				});
			}
		}

		private void Update()
		{
			if (Time.unscaledTime - this._lastUpdateTimestamp < 1f || this.CO == null || this.CO.ship == null)
			{
				return;
			}
			this._lastUpdateTimestamp = Time.unscaledTime;
			if (CrewSim.GetSelectedCrew().strID == this.CO.strID != this._isSelected)
			{
				this._isSelected = (CrewSim.GetSelectedCrew().strID == this.CO.strID);
				this._imgSelectionBracket.SetActive(this._isSelected);
				this.SetButton();
			}
			bool flag = this.CO.ship != null && this.CO.ship.LoadState > Ship.Loaded.Shallow;
			if (flag)
			{
				if (this._foreGround.activeSelf)
				{
					this._foreGround.SetActive(false);
				}
				Interaction interaction = (this.CO.aQueue == null || this.CO.aQueue.Count <= 0) ? null : this.CO.aQueue[0];
				if (interaction != null)
				{
					if (interaction.strTitle != this._txtStatus.text)
					{
						this._txtStatus.text = this.CO.aQueue[0].strTitle;
					}
				}
				else if ("none" != this._txtStatus.text)
				{
					this._txtStatus.text = "none";
				}
			}
			else
			{
				if (!this._foreGround.activeSelf)
				{
					this._foreGround.SetActive(true);
				}
				if (this.CO.ship.strRegID != this._txtStatus.text)
				{
					this._txtStatus.text = this.CO.ship.strRegID;
				}
			}
			if (this.CO.jsShiftLast != null && this.CO.jsShiftLast.nID >= 0)
			{
				this._txtShift.text = this.CO.jsShiftLast.strName;
				this._bmpShift.texture = GUICrewCard.GetShiftRankIcon(this.CO.jsShiftLast.strName);
				this.ttShift.SetData("Shift", this.CO.jsShiftLast.strName, false);
			}
			else
			{
				this._txtShift.text = string.Empty;
				this._bmpShift.texture = GUICrewCard.GetShiftRankIcon("blank");
				this.ttShift.SetData("Shift", "None", false);
			}
			this.SetIndicators();
		}

		private void SetIndicators()
		{
			if (this.CO.ship.LoadState < Ship.Loaded.Full && this._physioIndicator1.gameObject.activeSelf)
			{
				this._physioIndicator1.gameObject.SetActive(false);
				this._physioIndicator2.gameObject.SetActive(false);
				this._physioIndicator3.gameObject.SetActive(false);
			}
			else if (this.CO.ship.LoadState >= Ship.Loaded.Full && !this._physioIndicator1.gameObject.activeSelf)
			{
				this._physioIndicator1.gameObject.SetActive(true);
				this._physioIndicator2.gameObject.SetActive(true);
				this._physioIndicator3.gameObject.SetActive(true);
			}
			if (!this._physioIndicator1.gameObject.activeSelf)
			{
				return;
			}
			if (this.CO.currentRoom == null)
			{
				this.CO.currentRoom = this.CO.ship.GetRoomAtWorldCoords1(this.CO.tf.position, true);
			}
			this._physioValueDict.Clear();
			foreach (KeyValuePair<string, PhysioDef> keyValuePair in MonoSingleton<GUICrewStatus>.Instance._physioDefs)
			{
				this._physioValueDict.Add(new Tuple<string, double>(keyValuePair.Key, keyValuePair.Value.CalculateFillAmount(this.CO)));
			}
			this._physioValueDict.Sort((Tuple<string, double> x, Tuple<string, double> y) => y.Item2.CompareTo(x.Item2));
			this._physioIndicator1.Init(MonoSingleton<GUICrewStatus>.Instance.GetPhysioDef(this._physioValueDict[0].Item1));
			this._physioIndicator1.UpdateCO(this.CO, true);
			this._physioIndicator2.Init(MonoSingleton<GUICrewStatus>.Instance.GetPhysioDef(this._physioValueDict[1].Item1));
			this._physioIndicator2.UpdateCO(this.CO, true);
			this._physioIndicator3.Init(MonoSingleton<GUICrewStatus>.Instance.GetPhysioDef(this._physioValueDict[2].Item1));
			this._physioIndicator3.UpdateCO(this.CO, true);
		}

		public static Texture GetShiftRankIcon(string strShiftRank)
		{
			if (GUICrewCard.dictShiftIcons == null)
			{
				GUICrewCard.dictShiftIcons = new Dictionary<string, Texture>();
				GUICrewCard.dictShiftIcons["missing"] = DataHandler.LoadPNG("missing.png", false, false);
				GUICrewCard.dictShiftIcons["Captain"] = DataHandler.LoadPNG("IcoCaptain.png", false, false);
				GUICrewCard.dictShiftIcons["Crew"] = DataHandler.LoadPNG("IcoCrew.png", false, false);
				GUICrewCard.dictShiftIcons["Work"] = DataHandler.LoadPNG("IcoWork.png", false, false);
				GUICrewCard.dictShiftIcons["Sleep"] = DataHandler.LoadPNG("IcoSleep.png", false, false);
				GUICrewCard.dictShiftIcons["Free"] = DataHandler.LoadPNG("IcoFree.png", false, false);
				GUICrewCard.dictShiftIcons["blank"] = DataHandler.LoadPNG("blank.png", false, false);
				foreach (Texture texture in GUICrewCard.dictShiftIcons.Values)
				{
					texture.filterMode = FilterMode.Bilinear;
				}
			}
			if (string.IsNullOrEmpty(strShiftRank) || !GUICrewCard.dictShiftIcons.ContainsKey(strShiftRank))
			{
				return GUICrewCard.dictShiftIcons["missing"];
			}
			return GUICrewCard.dictShiftIcons[strShiftRank];
		}

		public void Refresh()
		{
			this._lastUpdateTimestamp = 0f;
		}

		[SerializeField]
		private RawImage _portraitImg;

		[SerializeField]
		private TMP_Text _txtName;

		[SerializeField]
		private TMP_Text _txtStatus;

		[SerializeField]
		private GameObject _foreGround;

		[SerializeField]
		private TMP_Text _txtShift;

		[SerializeField]
		private RawImage _bmpRank;

		[SerializeField]
		private RawImage _bmpShift;

		[SerializeField]
		private PhysioIndicator _physioIndicator1;

		[SerializeField]
		private PhysioIndicator _physioIndicator2;

		[SerializeField]
		private PhysioIndicator _physioIndicator3;

		[SerializeField]
		private GameObject _imgSelectionBracket;

		[SerializeField]
		private Button _portraitBtn;

		private Tooltippable2 ttRank;

		private Tooltippable2 ttShift;

		private float _lastUpdateTimestamp;

		private List<Tuple<string, double>> _physioValueDict = new List<Tuple<string, double>>();

		private string _coId;

		private CondOwner _co;

		private bool _isSelected;

		private static Dictionary<string, Texture> dictShiftIcons;
	}
}
