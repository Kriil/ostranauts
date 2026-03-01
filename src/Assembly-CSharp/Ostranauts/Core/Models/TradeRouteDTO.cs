using System;
using Ostranauts.Trading;

namespace Ostranauts.Core.Models
{
	public class TradeRouteDTO
	{
		public TradeRouteDTO()
		{
		}

		public TradeRouteDTO(TradeRouteDTO supplier, TradeRouteDTO consumer)
		{
			this.OriginStation = supplier.OriginStation;
			this.DestinationStation = consumer.DestinationStation;
			this.CoCollection = (supplier.CoCollection ?? consumer.CoCollection);
			this.Amount = ((consumer.Amount >= supplier.Amount) ? supplier.Amount : consumer.Amount);
			this.PriceModOrigin = supplier.PriceModOrigin;
			this.PriceModDestination = consumer.PriceModDestination;
		}

		public double RouteValue
		{
			get
			{
				if (this.CoCollection == null)
				{
					return 0.0;
				}
				double num = this.PriceModDestination - this.PriceModOrigin;
				return this.CoCollection.GetAveragePrice() * (double)this.Amount * num;
			}
		}

		public TradeRouteDTO SetSupply(string origin, DataCoCollection coColl, int amount, float priceMod)
		{
			this.OriginStation = origin;
			this.CoCollection = coColl;
			this.Amount = amount;
			this.PriceModOrigin = (double)priceMod;
			return this;
		}

		public TradeRouteDTO SetDemand(string origin, DataCoCollection coColl, int amount, float priceMod)
		{
			this.DestinationStation = origin;
			this.CoCollection = coColl;
			this.Amount = amount;
			this.PriceModDestination = (double)priceMod;
			return this;
		}

		public string OriginStation;

		public double PriceModOrigin;

		public double PriceModDestination;

		public string DestinationStation;

		public DataCoCollection CoCollection;

		public int Amount;
	}
}
