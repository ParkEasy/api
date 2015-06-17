namespace ParkEasyAPI.Models
{	
	
	public class PerHourModel
	{
		public double Timing;
		public double Price;
		
		public PerHourModel(double Timing, double Price)
		{
			this.Timing = Timing;
			this.Price = Price;
		}
	
		// APPLY
		public double Apply(double hours)
		{
			return hours * this.Timing * this.Price;
		}
	}
}

