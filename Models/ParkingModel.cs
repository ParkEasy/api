using System;

namespace ParkEasyAPI.Models
{
	
    public class ParkingModel 
	{
		public string ID;
		public string Name;	
		public string Description;
		public ParkingType Type;
		public CoordinateModel Coordinate;
		public int? Capacity; // free spaces in parking space
		public int? CapacityWomen; // free spaces especially for women
		public int? CapacityDisabled; // free spaces for disabled humans
		public int? CapacityService; // free spaces for Dienstfahrzeuge 
		public int? Trend; // upwards or downwards trend of free spaces
		public double? PricePerHour;
		public double? MaximumParkingHours;
		public string RedPointText; // some payment stuff
		public string SectionFrom;
		public string SectionTo;
		public double DistanceToUser;
		public bool Gates;
		public OpeningHoursModel[] OpeningHours;
		
		public  ParkingModel() {
			this.OpeningHours = new OpeningHoursModel[7];
			this.Gates = false;
		}
	}
	
}