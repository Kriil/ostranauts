using System;
using System.Collections.Generic;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships.Rooms;
using Ostranauts.Tools.ExtensionMethods;
using UnityEngine;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class ShipListEntryBase : MonoBehaviour
	{
		public virtual void SetData(Ship ship, float price)
		{
			this.SetData(ship, price, null);
		}

		public virtual Dictionary<string, Texture2D> SetData(Ship ship, float price, Dictionary<string, Texture2D> loadedImages)
		{
			return null;
		}

		protected string GetRoomDescription(Ship ship)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			string text = string.Empty;
			foreach (RoomSpec roomSpec in ship.GetRoomSpecs())
			{
				dictionary.Increment(roomSpec.strNameFriendly);
			}
			foreach (KeyValuePair<string, int> keyValuePair in dictionary)
			{
				string text2 = text;
				text = string.Concat(new object[]
				{
					text2,
					keyValuePair.Value,
					"x ",
					keyValuePair.Key,
					" "
				});
			}
			return (!string.IsNullOrEmpty(text)) ? text : "No room specializations";
		}

		protected Texture GetSilhouetteImage(Ship ship)
		{
			return SilhouetteUtility.GenerateTexture(ship.FloorPlan, this.ClrWhite02, new Vector2(130f, 100f));
		}

		protected Texture GetRoomIcon(string imgName)
		{
			string strName = imgName;
			int num = imgName.IndexOf('_');
			if (num != -1)
			{
				strName = imgName.Substring(0, num);
			}
			RoomSpec roomDef = DataHandler.GetRoomDef(strName);
			if (roomDef == null)
			{
				return null;
			}
			return DataHandler.LoadPNG("rooms/" + roomDef.iconPath + ".png", false, false);
		}

		protected string GetFormatedPrice(double price)
		{
			return price.ToString("n");
		}

		private Color ClrWhite02 = new Color(0.46875f, 0.46875f, 0.46875f, 0.9f);
	}
}
