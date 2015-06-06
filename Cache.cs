using System;

namespace ParkEasyAPI
{
	// Static class for caching remote WWW data
	public static class Cache
	{
		public static dynamic GarageData;
		public static DateTime? GarageDataExpiration;
		
		public static dynamic MachineData;
		public static DateTime? MachineDataExpiration;
		public static dynamic UniData;
		public static DateTime? UniDataExpiration;
	}
}
