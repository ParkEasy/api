using System;
using System.Collections.Generic;

namespace ParkEasyAPI.Models
{	
    public class PriceModel
	{
		public double? FullDay;	
		public PerHourModel PerHour;
		public List<TierModel> Tiered;
		public TierModel SpecialHours;
		
		public PriceModel()
		{
		}
		
		public PriceModel(double PerHourPrice, double FullDayPrice)
		{
			this.FullDay = FullDayPrice;
			this.PerHour = new PerHourModel(1, PerHourPrice);
		}
		
		public PriceModel(double PerHourPrice)
		{
			this.PerHour = new PerHourModel(1, PerHourPrice);
		}
	}
}
