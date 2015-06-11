using System;
using System.Collections.Generic;
using MongoDB.Driver;
using ParkEasyAPI.Models;

namespace ParkEasyAPI.Data
{
	// Static class for caching remote WWW data
	public static class Cache
	{
		public static List<ParkingModel> ParkingModels;
		
		// MongoDB
		public static MongoClient MongoDBClient;
	}
}
