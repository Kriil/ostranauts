using System;

[Serializable]
// Serialized queued ship-comms message.
// These records are stored in JsonStarSystemSave and rehydrated into runtime
// ShipMessage objects during star-system loading.
public class JsonShipMessage
{
	// Sender/receiver ship registration ids and when the message becomes available.
	public string strSenderRegId { get; set; }

	public string strRecieverRegId { get; set; }

	public double dAvailableTime { get; set; }

	public bool bRead { get; set; }

	// Embedded interaction payload shown or executed when the message is opened.
	public JsonInteractionSave iaMessageInteraction { get; set; }

	public string strMessageText { get; set; }
}
