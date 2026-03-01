using System;

namespace Ostranauts.Ships.Comms
{
	public class ShipMessage
	{
		public string ID
		{
			get
			{
				string text = (this.Interaction == null) ? string.Empty : this.Interaction.strName;
				return string.Concat(new object[]
				{
					this.SenderRegId,
					this.ReceiverRegId,
					this.AvailableTime,
					text
				});
			}
		}

		public JsonShipMessage GetJson()
		{
			if (this.Interaction == null)
			{
				return null;
			}
			return new JsonShipMessage
			{
				strSenderRegId = this.SenderRegId,
				strRecieverRegId = this.ReceiverRegId,
				dAvailableTime = this.AvailableTime,
				bRead = this.Read,
				iaMessageInteraction = (this.Interaction.GetJSONSave() ?? new JsonInteractionSave(this.Interaction)),
				strMessageText = this.MessageText
			};
		}

		public string SenderRegId;

		public string ReceiverRegId;

		public double AvailableTime;

		public bool Read;

		public Interaction Interaction;

		public string MessageText;
	}
}
