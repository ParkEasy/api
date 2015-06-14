using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using ParkEasyAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace ParkEasyAPI.Controllers
{   
    public class ScrapingController : Controller
    {   
        // DYNAMIC EXISTS
        // checks if a property exists in a dynamic type
        public static bool DynamicExist(dynamic settings, string name)
        {
            return settings.GetType().GetProperty(name) != null;
        }
        
        // GET /scrape
        [HttpGet]
        [Route("scrape")]
        public dynamic Scrape()
		{
            List<ParkingModel> parkingModels = new List<ParkingModel>();
            
            // open connection to mongodb
            var server = StaticGlobal.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collection = database.GetCollection<ParkingModel>("parking");
            
            // GARAGE DATA //
            Dictionary<string, dynamic> jsonGarage;
            
            // load data from remote source
            Console.WriteLine("Garages from WWW");
            using(var client = new System.Net.WebClient())
            {
                var body = client.DownloadString("http://www.koeln.de/apps/parken/json/current");
                jsonGarage = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(body);
            }
            
            List<string> prices = new List<string>();
                
            // parse parking garages into our parking model
            foreach(string key in jsonGarage.Keys) 
            {    
                dynamic obj = jsonGarage[key];
                
                ParkingModel model = new ParkingModel();
                
                // parse garage data
                model.Type = ParkingType.Garage;
                model.Name = obj.title;
                model.Id = obj.shortname;
                model.Capacity = obj.capacity;
                model.Free = obj.free;
                
                try
                {
                    model.CapacityWomen = obj.womens_parking;
                }
                catch(Exception e) {}
                
                model.Price = new PriceModel(1.0);
                model.Description = obj.fulltext;
                
                var collectionStatus = database.GetCollection<ParkingModel>("status");
            
                // store the "free" parking spaces information
                StatusModel status = new StatusModel();
                status.ParkingId = model.Id;
                status.Amount = obj.free;
                status.Time = DateTime.UtcNow;
                status.Id = ObjectId.GenerateNewId();
                
                collectionStatus.Insert(status);
                
                // parse coordinates
                CoordinateModel coordinateModel = new CoordinateModel();
                coordinateModel.Latitude = Convert.ToDouble(obj.lat);
                coordinateModel.Longitude = Convert.ToDouble(obj.lon);
                
                model.Coordinate = coordinateModel;
                model.Coordinates = new double[2] {coordinateModel.Longitude, coordinateModel.Latitude};
                
                try 
                {
                    model.OpeningHours = new OpeningHoursModel[7];
                    
                    // loop a whole week
                    for(int i = 0; i <= 6; i++)
                    {
                        // extract opening times for every day of the week
                        OpeningHoursModel hoursModel = new OpeningHoursModel();
                        hoursModel.Open = Convert.ToInt32(Convert.ToString(obj.open_time).Replace(":", ""));
                        hoursModel.Close = Convert.ToInt32(Convert.ToString(obj.close_time).Replace(":", ""));
                        
                        model.OpeningHours[i] = hoursModel;
                    }
                }
                catch(Exception e) {
                    Console.WriteLine(e.ToString());
                }
                
                parkingModels.Add(model);
            }
            
            // MACHINE DATA //
            dynamic jsonMachine;
            
            // load data from remote source
            Console.WriteLine("Machine from WWW");
            using(var client = new System.Net.WebClient())
            {
                var body = client.DownloadString("http://geoportal1.stadt-koeln.de/ArcGIS/rest/services/66/Parkscheinautomaten/MapServer/0/query?text=&geometry=&geometryType=esriGeometryPoint&inSR=&spatialRel=esriSpatialRelIntersects&relationParam=&objectIds=&where=id%20is%20not%20null&time=&returnCountOnly=false&returnIdsOnly=false&returnGeometry=true&maxAllowableOffset=&outSR=4326&outFields=%2A&f=json");
                jsonMachine = JsonConvert.DeserializeObject(body); 
            }
            
            // parse parking machines into our parking model
            foreach(dynamic machine in jsonMachine.features) 
            {
                
                ParkingModel model = new ParkingModel();
                
                // parse garage data
                model.Type = ParkingType.TicketMachine;
                model.Name = machine.attributes.Aufstellort;
                model.Id = Convert.ToString(machine.attributes.ID);
                model.Capacity = machine.attributes.Stellplaetze;
                model.Price = new PriceModel(Convert.ToDouble(machine.attributes.Gebuehr));
                model.MaximumParkingHours = Convert.ToDouble(machine.attributes.Hoechstparkdauer);
                model.RedPointText = machine.attributes.Roter_Punkt_Text;
                model.SectionFrom = machine.attributes.Abschnitt_Von;
                model.SectionTo = machine.attributes.Abschnitt_Bis;
                
                // parse coordinates
                CoordinateModel coordinateModel = new CoordinateModel();
                coordinateModel.Latitude = machine.geometry.y;
                coordinateModel.Longitude = machine.geometry.x;
                model.Coordinate = coordinateModel;
                
                model.Coordinates = new double[2] {coordinateModel.Longitude, coordinateModel.Latitude};
                
                parkingModels.Add(model);
            }
            
            // UNI DATA //
            dynamic jsonUni;
            
            // load data from remote source
            Console.WriteLine("Uni from WWW");
            using(var client = new System.Net.WebClient())
            {
                var body = client.DownloadString("https://github.com/ParkEasy/tools/releases/download/v1/uni.json");
                jsonUni = JsonConvert.DeserializeObject(body); 
            }
            
            // loop university parking spaces
            foreach(dynamic uniparking in jsonUni) 
            {
                ParkingModel model = new ParkingModel();
                
                model.Id = uniparking.name;
                model.Type = ParkingType.University;
                model.Name = uniparking.name;
                model.Description = string.Join(", ", uniparking.descriptions);
                model.Capacity = uniparking.num_spaces;
                model.CapacityDisabled = uniparking.num_disabled;
                model.CapacityService = uniparking.num_service;
                model.Gates = uniparking.gates;
                model.Price = new PriceModel(1.3);
                
                // parse opening hours
                if(DynamicExist(uniparking, "hours")) 
                {
                    model.OpeningHours = new OpeningHoursModel[7];
                    
                    int i = 0;
                    foreach(dynamic hour in uniparking.hours)
                    {
                        OpeningHoursModel hoursModel = new OpeningHoursModel();
                        if(hour != null) 
                        {
                            hoursModel.Open = hour.open;
                            hoursModel.Close = hour.close;
                        }
                        else 
                        {
                            hoursModel.Closed = true;
                        }
                        
                        model.OpeningHours[i] = hoursModel;
                        i++;
                    }    
                }
                
                // parse coordinates
                CoordinateModel coordinateModel = new CoordinateModel();
                coordinateModel.Latitude = uniparking.coordinates.latitude;
                coordinateModel.Longitude = uniparking.coordinates.longitude;
                model.Coordinate = coordinateModel;
                
                model.Coordinates = new double[2] {coordinateModel.Longitude, coordinateModel.Latitude};
                
                parkingModels.Add(model);
            }
            
            // upsert each of the available parking options
            foreach(ParkingModel model in parkingModels)
            {   
                collection.Save(model);
            }
            
            return parkingModels.Count;
		}
	}
}