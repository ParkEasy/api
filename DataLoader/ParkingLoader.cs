using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ParkEasyAPI.Models;

namespace ParkEasyAPI.Controllers
{   
    public class ParkingLoader
	{
		        // DYNAMIC EXISTS
        // checks if a property exists in a dynamic type
        public static bool DynamicExist(dynamic settings, string name)
        {
            return settings.GetType().GetProperty(name) != null;
        }
        
        // LOAD DATA
        // loads data from various data sources
        public List<ParkingModel> Load(CoordinateModel currentPosition)
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
	}
}