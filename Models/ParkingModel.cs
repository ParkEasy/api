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
		public int? Trend; // upwards or downwards trend of free spaces
		public double? PricePerHour;
		public double? MaximumParkingHours;
		public string RedPointText; // some payment stuff
		public string SectionFrom;
		public string SectionTo;
		public double DistanceToUser;
	}
}