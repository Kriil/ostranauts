using System;

public class JsonShipConstructionTemplate
{
	public JsonShipConstructionTemplate()
	{
	}

	public JsonShipConstructionTemplate(JsonShip jsonShip, int progress)
	{
		this.nProgress = progress;
		if (jsonShip.aItems != null)
		{
			this.aItems = (JsonItem[])jsonShip.aItems.Clone();
		}
		if (jsonShip.aShallowPSpecs != null)
		{
			this.aShallowPSpecs = (JsonItem[])jsonShip.aShallowPSpecs.Clone();
		}
	}

	public int nProgress { get; set; }

	public JsonItem[] aItems { get; set; }

	public JsonItem[] aShallowPSpecs { get; set; }

	public JsonShipConstructionTemplate Clone()
	{
		JsonShipConstructionTemplate jsonShipConstructionTemplate = new JsonShipConstructionTemplate();
		jsonShipConstructionTemplate.nProgress = this.nProgress;
		if (this.aItems != null)
		{
			jsonShipConstructionTemplate.aItems = (JsonItem[])this.aItems.Clone();
		}
		if (this.aShallowPSpecs != null)
		{
			jsonShipConstructionTemplate.aShallowPSpecs = (JsonItem[])this.aShallowPSpecs.Clone();
		}
		return jsonShipConstructionTemplate;
	}
}
