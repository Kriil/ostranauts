using System;

[Serializable]
public class JsonAIShipSave
{
	public string strATCLast { get; set; }

	public string strRegId { get; set; }

	public string strHomeStation { get; set; }

	public AIType enumAIType { get; set; }

	public string strActiveCommand { get; set; }

	public string[] strActiveCommandPayload { get; set; }
}
