namespace ParkEasyAPI.Models
{	
    public class ParkingModel 
	{
		public string Id;
		public string Name;	
		public string Description;
		public ParkingType Type;
		public CoordinateModel Coordinate;
		public double[] Coordinates;
		public int? Capacity; // free spaces in parking space
		public int? CapacityWomen; // free spaces especially for women
		public int? CapacityDisabled; // free spaces for disabled humans
		public int? CapacityService; // free spaces for Dienstfahrzeuge 
		public double FreeLikelihood;
		public PriceModel Price;
		public double? MaximumParkingHours;
		public string RedPointText; // some payment stuff
		public string SectionFrom;
		public string SectionTo;
		public double DistanceToUser;
		public bool Gates;
		public OpeningHoursModel[] OpeningHours;
		
		public  ParkingModel() {
			this.Gates = false;
			this.Coordinates = new double[2];
			this.FreeLikelihood = 0.0;
		}
	}
	
}