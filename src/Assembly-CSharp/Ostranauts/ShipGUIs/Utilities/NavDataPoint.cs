using System;
using Ostranauts.Utils.Models;

namespace Ostranauts.ShipGUIs.Utilities
{
	public class NavDataPoint
	{
		public NavDataPoint()
		{
		}

		public NavDataPoint(double arrivalTime, ShipSitu objSs)
		{
			this.ArrivalTime = arrivalTime;
			this.ObjSS = objSs;
		}

		public NavDataPoint(double arrivalTime, ShipSitu objSs, double fuelLevel, double torchCycle, double torchFuelLevel)
		{
			this.ArrivalTime = arrivalTime;
			this.ObjSS = objSs;
			this.FuelLevel = fuelLevel;
			this.TorchCycle = torchCycle;
			this.TorchFuelLevel = torchFuelLevel;
		}

		public double VelX
		{
			get
			{
				return this.ObjSS.vVelX;
			}
		}

		public double VelY
		{
			get
			{
				return this.ObjSS.vVelY;
			}
		}

		public Point Vel
		{
			get
			{
				return this.ObjSS.vVel;
			}
		}

		public NavDataPoint Clone()
		{
			ShipSitu shipSitu = new ShipSitu();
			shipSitu.CopyFrom(this.ObjSS, false);
			return new NavDataPoint(this.ArrivalTime, shipSitu)
			{
				FuelLevel = this.FuelLevel,
				TorchCycle = this.TorchCycle,
				TorchFuelLevel = this.TorchFuelLevel
			};
		}

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"ArrivalT: ",
				this.ArrivalTime,
				"; Fuel: ",
				this.FuelLevel,
				"; Torch: ",
				this.TorchCycle,
				"; Torch Fuel: ",
				this.TorchFuelLevel
			});
		}

		public JsonNavDataPoint GetJSON()
		{
			return new JsonNavDataPoint
			{
				ArrivalTime = this.ArrivalTime,
				ObjSS = this.ObjSS.GetJSON(),
				FuelLevel = this.FuelLevel,
				TorchCycle = this.TorchCycle,
				TorchFuelLevel = this.TorchFuelLevel,
				TorchDecelReq = this.TorchDecelReq
			};
		}

		public double ArrivalTime;

		public ShipSitu ObjSS;

		public double FuelLevel;

		public double TorchCycle;

		public double TorchFuelLevel;

		public bool TorchDecelReq;
	}
}
