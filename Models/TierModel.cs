using System;

namespace ParkEasyAPI.Models
{	
    public class TierModel
	{
		public double From;
		public double To;
		public double Price;
		public bool PerHour;
		
		public TierModel()
		{
			this.PerHour = false;
		}
	}
}
