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
            
            // GARAGE DATA //
            dynamic jsonGarage;
            
            // load data from remote source
            Console.WriteLine("Garages from WWW");
            using(var client = new System.Net.WebClient())
            {
                var body = client.DownloadString("http://www.stadt-koeln.de/externe-dienste/open-data/parking.php");
                jsonGarage = JsonConvert.DeserializeObject(body);
            }
                
            // parse parking garages into our parking model
            foreach(dynamic garage in jsonGarage.features) 
            {    
                ParkingModel model = new ParkingModel();
                
                // parse garage data
                model.Type = ParkingType.Garage;
                model.Name = garage.attributes.PARKHAUS;
                model.Id = garage.attributes.IDENTIFIER;
                model.Capacity = garage.attributes.KAPAZITAET;
                model.Trend = garage.attributes.TENDENZ;
                model.PricePerHour = 1.0;
                
                // parse coordinates
                CoordinateModel coordinateModel = new CoordinateModel();
                coordinateModel.Latitude = garage.geometry.y;
                coordinateModel.Longitude = garage.geometry.x;
                model.Coordinate = coordinateModel;
                
                model.Coordinates = new double[2] {coordinateModel.Longitude, coordinateModel.Latitude};
                
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
                model.PricePerHour = Convert.ToDouble(machine.attributes.Gebuehr);
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
                model.PricePerHour = 1.3;
                
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
            
            // open connection to mongodb
            var server = StaticGlobal.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collection = database.GetCollection<ParkingModel>("parking");
            
            // upsert each of the available parking options
            foreach(ParkingModel model in parkingModels)
            {   
                collection.Save(model);
            }
            
            return parkingModels.Count;
		}
	}
}