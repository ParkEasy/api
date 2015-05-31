using System;

namespace ParkEasyAPI.Models
{
    public class CoordinateModel 
	{
		public double Latitude;
		public double Longitude; 
		
		// DISTANCE TO
		// Calculates the euclidian distance between two coordinates in meters
		public double DistanceTo(CoordinateModel coordinate)
		{
			double dst = Math.Sqrt(
				Math.Pow(this.Latitude - coordinate.Latitude, 2) + 
				Math.Pow(this.Longitude - coordinate.Longitude, 2)
			);
			return Math.Abs(dst) * 6371 / 1000;
		}
	}
}