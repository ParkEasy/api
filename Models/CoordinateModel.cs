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
			// euclidean distance
			/*double dst = Math.Sqrt(
				Math.Pow(this.Latitude - coordinate.Latitude, 2) + 
				Math.Pow(this.Longitude - coordinate.Longitude, 2)
			);*/
			
			// manhattan distance
			double dist = Math.Abs(this.Latitude - coordinate.Latitude) + Math.Abs(this.Longitude - coordinate.Longitude);
			return Math.Abs(dst) * 6371 / 1000;
		}
	}
}