using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ParkEasyAPI.Models;
using ParkEasyAPI.Data;
//using MongoDB.Bson;
//using MongoDB.Driver;

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
            
            return "hello world";
            /*
            // open connection to mongodb
            var client = new MongoClient(Environment.GetEnvironmentVariable("CUSTOMCONNSTR_mongodb"));
            var database = client.GetDatabase("parkeasy");
            var collection = database.GetCollection<ParkingModel>("parking");
            
            // upsert each of the available parking options
            foreach(ParkingModel model in models)
            {
                UpdateOptions options = new UpdateOptions();
                options.IsUpsert = true;
                collection.ReplaceOneAsync(x => x.ID == model.ID, model, options);
            }
            
            return true;*/
		}
	}
}