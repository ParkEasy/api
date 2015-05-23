using System;

namespace ParkEasyAPI.Models
{
    public class ParkingModel 
	{
		public String ID;
		public String Name;	
		public CoordinateModel Coordinate;
		public int Capacity;	// free spaces in parking space
		public int Trend;		// upwards or downwards trend of free spaces
	}
}