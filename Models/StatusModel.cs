using System;
using MongoDB.Bson;

namespace ParkEasyAPI.Models
{	
    public class StatusModel
	{
		public ObjectId Id;
		public int Amount;	
		public string ParkingId;
		public DateTime Time;	
		public bool HighQualitySample;
	}
}
