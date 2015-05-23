namespace ParkEasyAPI.Models
{
    public class CoordinateModel 
	{
		public float Latitude;
		public float Longitude; 
		
		// Constructor
		public CoordinateModel() 
		{		
		}
		
		// Constructor
		public CoordinateModel(float latitude, float longitude) 
		{
			this.Latitude = latitude;
			this.Longitude = longitude;
		}
	}
}