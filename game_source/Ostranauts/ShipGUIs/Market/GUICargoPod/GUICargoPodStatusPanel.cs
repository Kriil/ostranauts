using System;
using System.Collections;
using Ostranauts.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.Market.GUICargoPod
{
	public class GUICargoPodStatusPanel : MonoBehaviour
	{
		public void SetData(MarketActorConfig podConfig)
		{
			GUIShipMarketDTO guishipMarketDTO = (podConfig == null) ? null : podConfig.GetCargoPodData();
			string otherShipId = this.GetOtherShipId(podConfig);
			this.txtCargoPodId.text = "ID: ";
			TMP_Text tmp_Text = this.txtCargoPodId;
			tmp_Text.text += ((podConfig == null) ? " - " : podConfig.COwnerId.Substring(podConfig.COwnerId.Length - 5, 4));
			TMP_Text tmp_Text2 = this.txtCargoPodId;
			tmp_Text2.text = tmp_Text2.text + " <color=yellow>" + otherShipId + "</color>";
			if (guishipMarketDTO == null || guishipMarketDTO.IsEmpty)
			{
				this.txtCargo.text = ((guishipMarketDTO != null) ? "Empty" : " - ");
				this.txtStock.text = " - / -";
				this.txtMass.text = " 0 / " + MarketManager.CARGOPOD_DEFAULTMASSCAPACITY + "kg";
			}
			else
			{
				this.txtCargo.text = guishipMarketDTO.DataCoCollection.FriendlyName;
				this.txtStock.text = guishipMarketDTO.Stock + " / " + guishipMarketDTO.MaxInventory;
				this.txtMass.text = string.Concat(new object[]
				{
					(int)(guishipMarketDTO.AvgMass * (double)guishipMarketDTO.Stock),
					" / ",
					MarketManager.CARGOPOD_DEFAULTMASSCAPACITY,
					"kg"
				});
			}
			this.SetCargoReqIcons(podConfig);
			this.SetCargoPodImage(podConfig);
		}

		private string GetOtherShipId(MarketActorConfig podConfig)
		{
			if (podConfig == null || podConfig.COwnerId == null)
			{
				return string.Empty;
			}
			CondOwner selectedCrew = CrewSim.GetSelectedCrew();
			if (selectedCrew == null || selectedCrew.ship == null)
			{
				return string.Empty;
			}
			CondOwner cobyID = selectedCrew.ship.GetCOByID(podConfig.COwnerId);
			if (!(cobyID == null))
			{
				return string.Empty;
			}
			foreach (Ship ship in selectedCrew.ship.GetAllDockedShips())
			{
				cobyID = ship.GetCOByID(podConfig.COwnerId);
				if (cobyID != null)
				{
					break;
				}
			}
			if (cobyID != null && cobyID.ship != null)
			{
				return cobyID.ship.strRegID;
			}
			return string.Empty;
		}

		private void SetCargoReqIcons(MarketActorConfig mConfig)
		{
			IEnumerator enumerator = this.tfSymbolHost.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					Transform transform = (Transform)obj;
					UnityEngine.Object.Destroy(transform.gameObject);
				}
			}
			finally
			{
				IDisposable disposable;
				if ((disposable = (enumerator as IDisposable)) != null)
				{
					disposable.Dispose();
				}
			}
			if (mConfig == null || mConfig.CargoSpecs == null)
			{
				return;
			}
			foreach (JsonCargoSpec jsonCargoSpec in mConfig.CargoSpecs)
			{
				if (!string.IsNullOrEmpty(jsonCargoSpec.strImg))
				{
					GUICargoPodReqIcon guicargoPodReqIcon = UnityEngine.Object.Instantiate<GUICargoPodReqIcon>(this.prefabReqIcon, this.tfSymbolHost);
					guicargoPodReqIcon.SetData(jsonCargoSpec.strImg);
				}
			}
			RectTransform component = this.tfSymbolHost.GetComponent<RectTransform>();
			if (component != null)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(component);
			}
		}

		private void SetCargoPodImage(MarketActorConfig podConfig)
		{
			if (podConfig == null)
			{
				return;
			}
			CondOwner cobyID = CrewSim.GetSelectedCrew().ship.GetCOByID(podConfig.COwnerId);
			if (cobyID == null || this._imgCO == null)
			{
				return;
			}
			Texture2D texture2D = DataHandler.LoadPNG(cobyID.strPortraitImg + ".png", false, false);
			if (texture2D != null)
			{
				this._imgAspectRatioFitter.aspectRatio = (float)texture2D.width / (float)texture2D.height;
				this._imgCO.texture = texture2D;
			}
		}

		[SerializeField]
		private TMP_Text txtCargoPodId;

		[SerializeField]
		private TMP_Text txtCargo;

		[SerializeField]
		private TMP_Text txtStock;

		[SerializeField]
		private TMP_Text txtMass;

		[SerializeField]
		private Transform tfSymbolHost;

		[SerializeField]
		private GUICargoPodReqIcon prefabReqIcon;

		[SerializeField]
		private RawImage _imgCO;

		[SerializeField]
		private AspectRatioFitter _imgAspectRatioFitter;
	}
}
