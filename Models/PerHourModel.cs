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
	}
}
