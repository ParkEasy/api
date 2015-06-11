using System;
using System.Collections.Generic;
using MongoDB.Driver;

namespace ParkEasyAPI
{
	// Static class for caching remote WWW data
	public static class StaticGlobal
	{	
		// MongoDB
		public static MongoClient MongoDBClient;
	}
}
