using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Events.DTOs;
using Ostranauts.ShipGUIs.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class SellShipEntry : ShipListEntryBase
	{
		public override void SetData(Ship ship, float price)
		{
			this._ship = ship;
			this._shipDto = new SellShipDTO(ship);
			this.txtName.text = ship.publicName;
			this.txtMake.text = ship.make;
			this.txtModel.text = ship.model;
			this.txtRegNr.text = ship.strRegID;
			this.txtLocation.text = this.GetLocation(ship);
			this._shipDto.ShipValue = (double)price;
			this.txtPrice.text = "$" + base.GetFormatedPrice((double)price);
			this.txtRooms.text = base.GetRoomDescription(ship);
			Dictionary<string, Texture2D> dictionary = null;
			if (DataHandler.dictShipImages.TryGetValue(ship.strRegID, out dictionary))
			{
				Texture2D texture2D = null;
				if (dictionary.TryGetValue(ship.strRegID, out texture2D) && texture2D != null)
				{
					this.imgMain.SetData(texture2D, null, false);
				}
			}
			if (this.imgMain.imgMain.texture == null)
			{
				if (ship.FloorPlan == null || ship.FloorPlan.Count == 0)
				{
					ship.FloorPlan = SilhouetteUtility.GetFloorVectors(ship.json.aItems);
				}
				this.imgMain.SetData(base.GetSilhouetteImage(ship), null, true);
				this._shipDto.IsSilhouette = true;
			}
			this._shipDto.Image = this.imgMain.imgMain.texture;
			int num = 0;
			this.tfRoomContainer.gameObject.SetActive(num != 0);
			RectTransform component = base.GetComponent<RectTransform>();
			component.sizeDelta = new Vector2(component.sizeDelta.x, (float)(155 + num * 75));
			this.btnSell.onClick.AddListener(delegate()
			{
				GUIShipBroker.OnTradeShip.Invoke(this._shipDto);
			});
		}

		private string GetLocation(Ship ship)
		{
			List<Ship> allDockedShips = ship.GetAllDockedShips();
			if (allDockedShips.Count != 0 || (ship.json.aDocked != null && ship.json.aDocked.Length != 0))
			{
				string str = (allDockedShips.Count == 0) ? ship.json.aDocked.First<string>() : allDockedShips.First<Ship>().strRegID;
				return "docked with " + str;
			}
			int num = (int)(ship.GetRangeTo(CrewSim.shipCurrentLoaded) * 149597872.0);
			return num + " km away from here";
		}

		[Header("Images")]
		[SerializeField]
		private PreviewImage imgMain;

		[Header("Overview")]
		[SerializeField]
		private TMP_Text txtName;

		[SerializeField]
		private TMP_Text txtMake;

		[SerializeField]
		private TMP_Text txtModel;

		[SerializeField]
		private TMP_Text txtLocation;

		[SerializeField]
		private TMP_Text txtRegNr;

		[SerializeField]
		private TMP_Text txtRooms;

		[SerializeField]
		private TMP_Text txtPrice;

		[SerializeField]
		private Button btnSell;

		[Header("Rooms")]
		[SerializeField]
		private RoomEntry roomPrefab;

		[SerializeField]
		private Transform tfRoomContainer;

		private Ship _ship;

		private SellShipDTO _shipDto;
	}
}
