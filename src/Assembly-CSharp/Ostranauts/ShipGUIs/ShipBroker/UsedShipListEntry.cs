using System;
using System.Collections.Generic;
using Ostranauts.Events.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class UsedShipListEntry : ShipListEntryBase
	{
		private void Awake()
		{
			this.btnVisit.onClick.AddListener(new UnityAction(this.OnVisitShipClicked));
		}

		public override Dictionary<string, Texture2D> SetData(Ship ship, float price, Dictionary<string, Texture2D> loadedImages)
		{
			this._shipIdentifier = ship.strRegID;
			this.txtDesignation.text = ship.designation;
			this.txtPublicName.text = ship.publicName;
			this.txtRegNr.text = ship.strRegID;
			this.txtMake.text = ship.make;
			this.txtModel.text = ship.model;
			this.txtDescription.text = ship.description;
			this.txtPrice.text = "$" + base.GetFormatedPrice((double)price);
			this.txtRooms.text = base.GetRoomDescription(ship);
			if (loadedImages != null)
			{
				this._imgDict = loadedImages;
			}
			else
			{
				this._imgDict = DataHandler.LoadPNGFolder("ships/" + ship.json.strName + "/", false);
			}
			bool isSilhouette = false;
			if (this._imgDict == null || this._imgDict.Count == 0)
			{
				this.imgMain.SetData(base.GetSilhouetteImage(ship), null, true);
				isSilhouette = true;
			}
			else
			{
				int num = 1;
				foreach (KeyValuePair<string, Texture2D> keyValuePair in this._imgDict)
				{
					if (keyValuePair.Key.Contains(ship.json.strName))
					{
						this.imgMain.SetData(keyValuePair.Value, null, false);
						this.previewImages[0].gameObject.SetActive(true);
						this.previewImages[0].SetData(keyValuePair.Value, null, false);
						this.previewImages[0].GetComponent<Button>().onClick.AddListener(delegate()
						{
							this.OnButtonClicked(this.previewImages[0]);
						});
					}
					else if (num < this.previewImages.Length)
					{
						PreviewImage previewImage = this.previewImages[num];
						previewImage.gameObject.SetActive(true);
						previewImage.SetData(keyValuePair.Value, base.GetRoomIcon(keyValuePair.Key), false);
						previewImage.GetComponent<Button>().onClick.AddListener(delegate()
						{
							this.OnButtonClicked(previewImage);
						});
						num++;
					}
				}
			}
			this._shipDto = new UsedShipDTO(ship)
			{
				ShipValue = (double)price,
				Image = this.imgMain.imgMain.texture,
				IsSilhouette = isSilhouette
			};
			this.btnBuy.onClick.AddListener(delegate()
			{
				GUIShipBroker.OnTradeShip.Invoke(this._shipDto);
			});
			return this._imgDict;
		}

		public void SetData(JsonShip ship, float price, bool allowedToBuy = true)
		{
			this._shipIdentifier = ship.strName;
			this.txtDesignation.text = ship.designation;
			this.txtPublicName.text = ship.publicName;
			this.txtRegNr.text = ship.strRegID;
			this.txtMake.text = ship.make;
			this.txtModel.text = ship.model;
			this.txtDescription.text = ship.description;
			this.txtPrice.text = "$" + base.GetFormatedPrice((double)price);
			this.txtRooms.text = string.Empty;
			this._imgDict = DataHandler.LoadPNGFolder("ships/" + ship.strName + "/", false);
			int num = 1;
			foreach (KeyValuePair<string, Texture2D> keyValuePair in this._imgDict)
			{
				if (keyValuePair.Key.Contains(ship.strName))
				{
					this.imgMain.SetData(keyValuePair.Value, null, false);
					this.previewImages[0].gameObject.SetActive(true);
					this.previewImages[0].SetData(keyValuePair.Value, null, false);
					this.previewImages[0].GetComponent<Button>().onClick.AddListener(delegate()
					{
						this.OnButtonClicked(this.previewImages[0]);
					});
				}
				else if (num < this.previewImages.Length)
				{
					PreviewImage previewImage = this.previewImages[num];
					previewImage.gameObject.SetActive(true);
					previewImage.SetData(keyValuePair.Value, base.GetRoomIcon(keyValuePair.Key), false);
					previewImage.GetComponent<Button>().onClick.AddListener(delegate()
					{
						this.OnButtonClicked(previewImage);
					});
					num++;
				}
			}
			this.btnBuy.interactable = allowedToBuy;
			if (!allowedToBuy)
			{
				this.btnBuy.GetComponentInChildren<TMP_Text>().text = "No Permit";
			}
			this._shipDto = new ApartmentDTO(ship)
			{
				ShipValue = (double)price,
				Image = this.imgMain.imgMain.texture
			};
			this.btnBuy.onClick.AddListener(delegate()
			{
				GUIShipBroker.OnTradeShip.Invoke(this._shipDto);
			});
		}

		public void SetSpecialOfferData(Ship ship, float price)
		{
			this.SetData(ship, price, null);
			this._shipDto.IsSpecialOffer = true;
			this.txtPrice.text = "$ 0";
		}

		private void OnVisitShipClicked()
		{
			GUIShipBroker.OnVisitShip.Invoke(this._shipIdentifier);
		}

		private void OnButtonClicked(PreviewImage previewImage)
		{
			this.imgMain.SetData(previewImage);
		}

		[Header("Images")]
		[SerializeField]
		private PreviewImage imgMain;

		[SerializeField]
		private PreviewImage[] previewImages;

		[Header("Text")]
		[SerializeField]
		private TMP_Text txtDesignation;

		[SerializeField]
		private TMP_Text txtPublicName;

		[SerializeField]
		private TMP_Text txtModel;

		[SerializeField]
		private TMP_Text txtMake;

		[SerializeField]
		private TMP_Text txtRegNr;

		[SerializeField]
		private TMP_Text txtRooms;

		[SerializeField]
		private TMP_Text txtDescription;

		[SerializeField]
		private TMP_Text txtPrice;

		[Header("Buttons")]
		[SerializeField]
		private Button btnVisit;

		[SerializeField]
		private Button btnBuy;

		private string[] _placeholderDescriptions;

		private string _shipIdentifier;

		private UsedShipDTO _shipDto;

		private Dictionary<string, Texture2D> _imgDict = new Dictionary<string, Texture2D>();
	}
}
