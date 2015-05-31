using System;

namespace ParkEasyAPI.Models
{
    public class CoordinateModel 
	{
		public double Latitude;
		public double Longitude; 
		
		public double DistanceTo(CoordinateModel coordinate)
		{
			double dst = Math.Sqrt(
				Math.Pow(coordinate.Latitude - this.Latitude, 2) + 
				Math.Pow(coordinate.Longitude - this.Longitude, 2)
			);
			return Math.Abs(dst);
		}
	}
}