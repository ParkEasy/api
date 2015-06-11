using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ParkEasyAPI.Models;
using ParkEasyAPI.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ParkEasyAPI.Controllers
{   
    public class ScrapingController : Controller
    {   
        // GET /scrape
        [HttpGet]
        [Route("scrape")]
        public dynamic Scrape()
		{
            // read all parking options form webservices
			List<ParkingModel> models = new ParkingLoader().Load();
            
            // open connection to mongodb
            var client = new MongoClient(Environment.GetEnvironmentVariable("mongodb"));
            var database = client.GetDatabase("parkeasy");
            var collection = client.GetCollection<ParkingModel>("parking");
            
            var list = await collection.Find().ToListAsync();
            
            return list;
		}
	}
}