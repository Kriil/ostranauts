using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.Objectives;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.NavStation
{
	public class NavModTransponder : NavModBase
	{
		protected override void Awake()
		{
			base.Awake();
			this.chkXPDR.onValueChanged.AddListener(delegate(bool isOn)
			{
				this.ToggleXPDR(isOn, false);
			});
			CanvasManager.HideCanvasGroup(this.cgPnlXPDRTxt);
			CanvasManager.HideCanvasGroup(this.cgXPDRTxtLicensed);
			CanvasManager.HideCanvasGroup(this.cgXPDRTxtWanted);
		}

		protected override void Init()
		{
			bool flag = !string.IsNullOrEmpty(this.COSelf.ship.strXPDR);
			if (this.chkXPDR.isOn != flag)
			{
				this.chkXPDR.isOn = flag;
				this._guiOrbitDraw.SetPropMapData("bXPDROn", flag.ToString().ToLower());
			}
			else
			{
				this.ToggleXPDR(this.chkXPDR.isOn, true);
			}
		}

		protected override void UpdateUI()
		{
			if (StarSystem.fEpoch - this.fTimeLastUpdate > 1.0)
			{
				this.fTimeLastUpdate = StarSystem.fEpoch;
				this.bXPDRRetry = true;
			}
			if (this.bXPDRRetry && this.fEpochXPDRRetry <= StarSystem.fEpoch)
			{
				if (!this.chkXPDR.isOn)
				{
					this.bXPDRRetry = false;
				}
				else
				{
					this.SetTextXPDR(true, this.strXPDRRetryCOID);
				}
			}
		}

		private void ToggleXPDR(bool bValue, bool suppressCrime = false)
		{
			string text = null;
			if (this.COSelf != null || this.COSelf.ship != null)
			{
				List<CondOwner> cos = this.COSelf.ship.GetCOs(Ship.ctXPDR, false, false, false);
				foreach (CondOwner condOwner in cos)
				{
					condOwner.ZeroCondAmount("IsOverrideOff");
					condOwner.ZeroCondAmount("IsOverrideOn");
					if (bValue)
					{
						condOwner.AddCondAmount("IsOverrideOn", 1.0, 0.0, 0f);
					}
					else
					{
						condOwner.AddCondAmount("IsOverrideOff", 1.0, 0.0, 0f);
					}
					if (text == null)
					{
						text = condOwner.strID;
					}
				}
			}
			if (!suppressCrime && !bValue)
			{
				MonoSingleton<ObjectiveTracker>.Instance.CreateCrimeWarning(CrewSim.GetSelectedCrew(), DataHandler.GetString("OBJV_CRIME_FLYING_DARK", false), false);
			}
			this._guiOrbitDraw.SetPropMapData("bXPDROn", bValue.ToString().ToLower());
			this.SetTextXPDR(bValue, text);
		}

		private void SetTextXPDR(bool bOn, string strXPDRCOID)
		{
			if (!bOn)
			{
				CanvasManager.HideCanvasGroup(this.cgPnlXPDRTxt);
				this.COSelf.ship.strXPDR = null;
				return;
			}
			CanvasManager.ShowCanvasGroup(this.cgPnlXPDRTxt);
			string text = null;
			this.bXPDRRetry = GUIXPDR.GetXPDRStatusFromCO(strXPDRCOID, out text);
			this.strXPDRRetryCOID = strXPDRCOID;
			if (this.fEpochXPDRRetry < StarSystem.fEpoch)
			{
				this.fEpochXPDRRetry = StarSystem.fEpoch + 1.0;
			}
			Ship shipByRegID = CrewSim.system.GetShipByRegID(text);
			if (shipByRegID == null || text == "?" || this.bXPDRRetry)
			{
				this.txtRegID.text = "------";
				this.txtShipName.text = "------";
				this.txtMake.text = "------";
				this.txtModel.text = "------";
				this.txtYear.text = "------";
				this.txtDesignation.text = "------";
				CanvasManager.HideCanvasGroup(this.cgXPDRTxtLicensed);
				CanvasManager.HideCanvasGroup(this.cgXPDRTxtWanted);
				if (shipByRegID != null)
				{
					shipByRegID.strXPDR = text;
				}
			}
			else
			{
				if (shipByRegID.strRegID == text)
				{
					this.txtRegID.text = shipByRegID.strRegID;
				}
				else
				{
					this.txtRegID.text = shipByRegID.strRegID + "*";
				}
				this.txtShipName.text = shipByRegID.publicName;
				this.txtMake.text = shipByRegID.make;
				this.txtModel.text = shipByRegID.model;
				this.txtYear.text = shipByRegID.year;
				this.txtDesignation.text = shipByRegID.designation;
				shipByRegID.strXPDR = text;
				if (shipByRegID.HasOKLGSalvageLicense())
				{
					CanvasManager.ShowCanvasGroup(this.cgXPDRTxtLicensed);
				}
				else
				{
					CanvasManager.HideCanvasGroup(this.cgXPDRTxtLicensed);
				}
				if (CrewSim.system.IsOwnerWanted(text, shipByRegID.objSS.vPos))
				{
					CanvasManager.ShowCanvasGroup(this.cgXPDRTxtWanted);
				}
				else
				{
					CanvasManager.HideCanvasGroup(this.cgXPDRTxtWanted);
				}
			}
		}

		[SerializeField]
		private Toggle chkXPDR;

		[SerializeField]
		private CanvasGroup cgPnlXPDRTxt;

		[SerializeField]
		private CanvasGroup cgXPDRTxtLicensed;

		[SerializeField]
		private CanvasGroup cgXPDRTxtWanted;

		[SerializeField]
		private TMP_Text txtRegID;

		[SerializeField]
		private TMP_Text txtShipName;

		[SerializeField]
		private TMP_Text txtMake;

		[SerializeField]
		private TMP_Text txtModel;

		[SerializeField]
		private TMP_Text txtYear;

		[SerializeField]
		private TMP_Text txtDesignation;

		private double fTimeLastUpdate;

		private bool bXPDRRetry;

		private double fEpochXPDRRetry;

		private string strXPDRRetryCOID;
	}
}
