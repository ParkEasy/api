using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;
using ParkEasyAPI.Models;

namespace ParkEasyAPI.Controllers
{   
    public class ParkingController : Controller
    {
        // LOAD DATA
        // loads data from various data sources
        private List<ParkingModel> LoadData(CoordinateModel currentPosition)
        {
            List<ParkingModel> parkingModels = new List<ParkingModel>();
            
            // load parking garages
            using(var client = new System.Net.WebClient())
            {
                var body = client.DownloadString("http://www.stadt-koeln.de/externe-dienste/open-data/parking.php");
                dynamic json = JsonConvert.DeserializeObject(body);
                
                // parse parking garages into our parking model
                foreach(dynamic garage in json.features) {
                    
                    ParkingModel model = new ParkingModel();
                    
                    // parse garage data
                    model.Type = ParkingType.Garage;
                    model.Name = garage.attributes.PARKHAUS;
                    model.ID = garage.attributes.IDENTIFIER;
                    model.Capacity = garage.attributes.KAPAZITAET;
                    model.Trend = garage.attributes.TENDENZ;
                    
                    // parse coordinates
                    CoordinateModel coordinateModel = new CoordinateModel();
                    coordinateModel.Latitude = garage.geometry.y;
                    coordinateModel.Longitude = garage.geometry.x;
                    model.Coordinate = coordinateModel;
                    
                    model.DistanceToUser = model.Coordinate.DistanceTo(currentPosition);
                    
                    parkingModels.Add(model);
                }
            }
            
            // load parking ticket machines
            using(var client = new System.Net.WebClient()) 
            {
                var body = client.DownloadString("http://geoportal1.stadt-koeln.de/ArcGIS/rest/services/66/Parkscheinautomaten/MapServer/0/query?text=&geometry=&geometryType=esriGeometryPoint&inSR=&spatialRel=esriSpatialRelIntersects&relationParam=&objectIds=&where=id%20is%20not%20null&time=&returnCountOnly=false&returnIdsOnly=false&returnGeometry=true&maxAllowableOffset=&outSR=4326&outFields=%2A&f=json");
                dynamic json = JsonConvert.DeserializeObject(body);
                
                // parse parking machines into our parking model
                foreach(dynamic machine in json.features) {
                    
                    ParkingModel model = new ParkingModel();
                    
                    // parse garage data
                    model.Type = ParkingType.TicketMachine;
                    model.Name = machine.attributes.Aufstellort;
                    model.ID = Convert.ToString(machine.attributes.ID);
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
                    
                    model.DistanceToUser = model.Coordinate.DistanceTo(currentPosition);
                    
                    parkingModels.Add(model);
                }
            }
            
            return parkingModels;
        }
        
        // GET /closest
        // https://github.com/ParkEasy/api/wiki/API-Docs#closest
        [HttpGet]
        [Route("closest")]
        public dynamic Closest(float lat = -1, float lon = -1, int radius = 5000, int hours = 1)
        {
            // validity check: are lat and long specified?
            if(lat < 0 || lon < 0) 
            {
                return "Error";
            }
            
            // create coordinate model form user given parameter
            CoordinateModel currentPosition = new CoordinateModel();
            currentPosition.Latitude = lat;
            currentPosition.Longitude = lon;
            
            // load data from various datasources
            List<ParkingModel> parkingModels = this.LoadData(currentPosition);
            
            // filter out all the places that are not within radius distance
            parkingModels = parkingModels.Where(delegate(ParkingModel a)
            {
                return a.DistanceToUser < radius / 1000;
            }).ToList<ParkingModel>();
            
            // sort by closeness to current position
            parkingModels.Sort(delegate(ParkingModel a, ParkingModel b)
            {
                double dstA = a.DistanceToUser;
                double dstB = b.DistanceToUser;
                
                if(dstA > dstB) return 1;
                else if(dstA < dstB) return -1;
                else return 0;
            });
            
            return parkingModels;
        }
    }
}
