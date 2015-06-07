using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;
using ParkEasyAPI.Models;

namespace ParkEasyAPI.Controllers
{   
    public class ParkingController : Controller
    {
        // DYNAMIC EXISTS
        // checks if a property exists in a dynamic type
        public static bool DynamicExist(dynamic settings, string name)
        {
            return settings.GetType().GetProperty(name) != null;
        }
        
        // LOAD DATA
        // loads data from various data sources
        private List<ParkingModel> LoadData(CoordinateModel currentPosition)
        {
            List<ParkingModel> parkingModels = new List<ParkingModel>();
            
            // GARAGE DATA //
            
            // use cache or fetch remote?
            dynamic jsonGarage;
            if(Cache.GarageData != null && Cache.GarageDataExpiration.HasValue && Cache.GarageDataExpiration.Value.CompareTo(DateTime.Now) >= 0) 
            {
                // load data from cache
                Console.WriteLine("Garages from Cache");
                jsonGarage = Cache.GarageData;
            }
            else 
            {
                // load data from remote source
                Console.WriteLine("Garages from WWW");
                using(var client = new System.Net.WebClient())
                {
                    var body = client.DownloadString("http://www.stadt-koeln.de/externe-dienste/open-data/parking.php");
                    jsonGarage = JsonConvert.DeserializeObject(body);
                    
                    // put data into cache 
                    Cache.GarageData = jsonGarage;
                    Cache.GarageDataExpiration = DateTime.Now.AddMinutes(5);
                }
            }
            
            // parse parking garages into our parking model
            foreach(dynamic garage in jsonGarage.features) 
            {    
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
            }
            
            // MACHINE DATA //
            
            // use cache or fetch remote?
            dynamic jsonMachine;
            if(Cache.MachineData != null && Cache.MachineDataExpiration.HasValue && Cache.MachineDataExpiration.Value.CompareTo(DateTime.Now) >= 0) 
            {
                // load data from cache
                Console.WriteLine("Machine from Cache");
                jsonMachine = Cache.MachineData;
            }
            else 
            {
                // load data from remote source
                Console.WriteLine("Machine from WWW");
                using(var client = new System.Net.WebClient())
                {
                    var body = client.DownloadString("http://geoportal1.stadt-koeln.de/ArcGIS/rest/services/66/Parkscheinautomaten/MapServer/0/query?text=&geometry=&geometryType=esriGeometryPoint&inSR=&spatialRel=esriSpatialRelIntersects&relationParam=&objectIds=&where=id%20is%20not%20null&time=&returnCountOnly=false&returnIdsOnly=false&returnGeometry=true&maxAllowableOffset=&outSR=4326&outFields=%2A&f=json");
                    jsonMachine = JsonConvert.DeserializeObject(body); 
                    
                    // put data in cache for 5 minutes
                    Cache.MachineData = jsonMachine;
                    Cache.MachineDataExpiration = DateTime.Now.AddMinutes(5);
                }
            }
            
            // parse parking machines into our parking model
            foreach(dynamic machine in jsonMachine.features) 
            {
                
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
            
            // UNI DATA //
            
            // use cache or fetch from remote?
            dynamic jsonUni;
            if(Cache.UniData != null && Cache.UniDataExpiration.HasValue && Cache.UniDataExpiration.Value.CompareTo(DateTime.Now) >= 0) 
            {
                // load data from cache
                Console.WriteLine("Uni from Cache");
                jsonUni = Cache.UniData;
            }
            else 
            {
                // load data from remote source
                Console.WriteLine("Uni from WWW");
                using(var client = new System.Net.WebClient())
                {
                    var body = client.DownloadString("https://raw.githubusercontent.com/ParkEasy/tools/master/uni.json");
                    jsonUni = JsonConvert.DeserializeObject(body); 
                    
                    // put data in cache for 5 minutes
                    Cache.UniData = jsonUni;
                    Cache.UniDataExpiration = DateTime.Now.AddMinutes(5);
                }
            }
            
            // loop university parking spaces
            foreach(dynamic uniparking in jsonUni) 
            {
                ParkingModel model = new ParkingModel();
                
                model.Type = ParkingType.University;
                model.Name = uniparking.name;
                model.Description = string.Join(", ", uniparking.descriptions);
                model.Capacity = uniparking.num_spaces;
                model.CapacityDisabled = uniparking.num_disabled;
                model.CapacityService = uniparking.num_service;
                model.Gates = uniparking.gates;
                
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
                
                model.DistanceToUser = model.Coordinate.DistanceTo(currentPosition);
                
                parkingModels.Add(model);
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
            
            // filter out places that have maximum parking hours lower than needed or 
            // opening hours that exceed the amount of time the user wants to park
            parkingModels = parkingModels.Where(delegate(ParkingModel a)
            {
                // are there limitations on the amount of parking hours?
                if(a.MaximumParkingHours.HasValue) 
                {
                    // does this exceed the amount of hours the user wants to park?
                    if(a.MaximumParkingHours.Value > hours)
                    {
                        return false;
                    }
                }
                
                // are there opening hours?
                if(a.OpeningHours != null)
                {
                    // find opening hours of current day
                    int day = (int) DateTime.Now.DayOfWeek;
                    OpeningHoursModel openingModel = a.OpeningHours[day];
                    
                    // is the parking closed today?
                    if(openingModel.Closed == true) 
                    {
                        return false;
                    }
                    
                    // TODO: check if current time + parking duration in limits of opening hours
                }
                
                return true;
                
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
            
            return parkingModels.Take(10);
        }
    }
}
