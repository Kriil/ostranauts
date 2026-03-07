using System;

public class JsonNavDataPoint
{
	public double ArrivalTime { get; set; }

	public JsonShipSitu ObjSS { get; set; }

	public double FuelLevel { get; set; }

	public double TorchCycle { get; set; }

	public double TorchFuelLevel { get; set; }

	public bool TorchDecelReq { get; set; }
}
