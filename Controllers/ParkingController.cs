using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Newtonsoft.Json;
using ParkEasyAPI.Models;

namespace ParkEasyAPI.Controllers
{
    public class ParkingController : Controller
    {
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
            
            List<ParkingModel> parkingModels = new List<ParkingModel>();
            
            // load parking garages
            using(var client = new System.Net.WebClient()) {
                var body = client.DownloadString("http://www.stadt-koeln.de/externe-dienste/open-data/parking.php");
                dynamic json = JsonConvert.DeserializeObject(body);
                
                // parse parking garages into our parking model
                foreach(dynamic garage in json.features) {
                    
                    ParkingModel model = new ParkingModel();
                    
                    // parse garage data
                    model.Name = garage.attributes.PARKHAUS;
                    model.ID = garage.attributes.IDENTIFIER;
                    model.Capacity = garage.attributes.KAPAZITAET;
                    model.Trend = garage.attributes.TENDENZ;
                    
                    // parse coordinates
                    CoordinateModel coordinateModel = new CoordinateModel();
                    coordinateModel.Latitude = garage.geometry.y;
                    coordinateModel.Longitude = garage.geometry.x;
                    model.Coordinate = coordinateModel;
                    
                    parkingModels.Add(model);
                }
            }
            
            return parkingModels;
        }
    }
}
