using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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
            var server = Cache.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collection = database.GetCollection<ParkingModel>("parking");
            
            // upsert each of the available parking options
            foreach(ParkingModel model in models)
            {   
                collection.Save(model);
            }
            
            return models.Count;
		}
	}
}