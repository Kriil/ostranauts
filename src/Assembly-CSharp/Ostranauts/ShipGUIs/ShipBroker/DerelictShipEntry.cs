using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Ostranauts.Events.DTOs;
using Ostranauts.Ships.Rooms;
using Ostranauts.Tools.ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class DerelictShipEntry : ShipListEntryBase
	{
		private void Awake()
		{
			this.btnBuy.onClick.AddListener(delegate()
			{
				GUIShipBroker.OnTradeShip.Invoke(this._shipDto);
			});
		}

		public override void SetData(Ship ship, float price)
		{
			this._ship = ship;
			this._shipDto = new DerelictDTO(ship);
			bool flag = ship.json.fLastVisit != 0.0;
			int num = DerelictShipEntry.HashIdIntoNumber(ship.strRegID);
			this.txtRegNr.text = ship.strRegID;
			string text = (!flag && num >= 3 && num <= 8) ? "?" : ship.json.designation;
			this.txtName.text = text;
			this._shipDto.ShipName = text;
			this.txtPublicName.text = "Name: ";
			TMP_Text tmp_Text = this.txtPublicName;
			tmp_Text.text += ((!flag && (num <= 4 || num >= 7)) ? "?" : ship.publicName);
			if (ship.fLastVisit == 0.0)
			{
				this.txtLastVisited.text = "Never";
			}
			else if (ship.fLastVisit < 0.0)
			{
				this.txtLastVisited.text = "Some time ago";
			}
			else
			{
				this.txtLastVisited.text = MathUtils.GetUTCFromS(ship.json.fLastVisit);
			}
			string text2 = "Model: ";
			this._shipDto.Model = ((!flag && num > 4) ? "?" : (ship.model + string.Empty));
			text2 += this._shipDto.Model;
			text2 += " Make: ";
			this._shipDto.Make = ((!flag && num <= 4) ? "?" : ship.make);
			text2 += this._shipDto.Make;
			this.txtModelMake.text = text2;
			int num2 = (int)(price / DerelictShipEntry.GetRandomPriceModifier(num));
			this.txtEstimatedValue.text = string.Concat(new object[]
			{
				"Estimated value: $",
				(float)num2 * 0.5f,
				" - ",
				num2
			});
			this.txtPrice.text = "$" + base.GetFormatedPrice((double)price);
			this._shipDto.ShipValue = (double)price;
			if (flag || num >= 4)
			{
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				foreach (RoomSpec roomSpec in ship.GetRoomSpecs())
				{
					dictionary.Increment(roomSpec.strNameFriendly);
				}
				if (dictionary.Count == 0)
				{
					this.txtRooms.text = "No room specializations";
				}
				else
				{
					foreach (KeyValuePair<string, int> keyValuePair in dictionary)
					{
						TMP_Text tmp_Text2 = this.txtRooms;
						string text3 = tmp_Text2.text;
						tmp_Text2.text = string.Concat(new object[]
						{
							text3,
							keyValuePair.Value,
							"x ",
							keyValuePair.Key,
							" "
						});
					}
				}
			}
			else
			{
				this.txtRooms.text = "?";
			}
			this.imgMain.texture = null;
			Dictionary<string, Texture2D> dictionary2 = null;
			if (DataHandler.dictShipImages.TryGetValue(ship.strRegID, out dictionary2))
			{
				Texture2D texture2D = null;
				if (dictionary2.TryGetValue(ship.strRegID, out texture2D) && texture2D != null)
				{
					this.imgMain.texture = texture2D;
					this.imgBackground.gameObject.SetActive(false);
				}
			}
			if (this.imgMain.texture == null)
			{
				this.imgMain.texture = base.GetSilhouetteImage(ship);
				this._shipDto.IsSilhouette = true;
			}
			this._shipDto.Image = this.imgMain.texture;
			int num3 = (int)(ship.GetRangeTo(CrewSim.shipCurrentLoaded) * 149597872.0);
			this.txtDistance.text = num3 + " km";
		}

		public static int HashIdIntoNumber(string regId)
		{
			int result;
			using (SHA256 sha = SHA256.Create())
			{
				byte[] value = sha.ComputeHash(Encoding.UTF8.GetBytes(regId));
				int num = BitConverter.ToInt32(value, 0);
				result = Mathf.Abs(num % 10);
			}
			return result;
		}

		public static float GetRandomPriceModifier(int randomNr)
		{
			return 0.5f + 0.055f * (float)randomNr;
		}

		[Header("Images")]
		[SerializeField]
		private RawImage imgMain;

		[SerializeField]
		private RawImage imgBackground;

		[Header("Text")]
		[SerializeField]
		private TMP_Text txtName;

		[SerializeField]
		private TMP_Text txtPublicName;

		[SerializeField]
		private TMP_Text txtLastVisited;

		[SerializeField]
		private TMP_Text txtDistance;

		[SerializeField]
		private TMP_Text txtModelMake;

		[SerializeField]
		private TMP_Text txtRegNr;

		[SerializeField]
		private TMP_Text txtRooms;

		[SerializeField]
		private TMP_Text txtPrice;

		[SerializeField]
		private TMP_Text txtEstimatedValue;

		[Header("Buttons")]
		[SerializeField]
		private Button btnBuy;

		private Ship _ship;

		private DerelictDTO _shipDto;
	}
}
