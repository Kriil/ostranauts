using System;
using Ostranauts.Ships.Rooms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.ShipGUIs.ShipBroker
{
	public class RoomEntry : MonoBehaviour
	{
		public void SetData(int position, JsonRoom jsonRoom, RoomSpec roomSpec)
		{
			this.imgMain.gameObject.SetActive(false);
			this.transform.anchoredPosition = new Vector2(this.transform.anchoredPosition.x, (float)(position * -75));
			this.txtRooms.text = roomSpec.strNameFriendly;
			this.txtContents.text = "Room size: " + jsonRoom.aTiles.Length;
			this.txtPrice.text = jsonRoom.roomValue.ToString("n");
		}

		[SerializeField]
		private new RectTransform transform;

		[SerializeField]
		private RawImage imgMain;

		[SerializeField]
		private TMP_Text txtRooms;

		[SerializeField]
		private TMP_Text txtContents;

		[SerializeField]
		private TMP_Text txtPrice;
	}
}
