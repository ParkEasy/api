using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ParkEasyAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ParkEasyAPI.Controllers
{   
    public class ParkingController : Controller
    {   
        // GET /search
        // https://github.com/ParkEasy/api/wiki/API-Docs#search
        [HttpGet]
        [Route("search")]
        public dynamic Search(float lat = -1, float lon = -1, int hours = 1, double speed = 0.0)
        {
            int TAKE = 5;
            double PARKING_RADIUS = 20.0;
            double SLOW_SPEED = 3.0;
            
            // validity check: are lat and lon specified?
            if(lat < 0 || lon < 0) 
            {
                Response.StatusCode = 400;
                
                Dictionary<string, string> err = new Dictionary<string, string>();
                err.Add("error", "either 'lat' or 'lon' not defined as parameters");
                
                return err;
            }
            
            Console.WriteLine("----------------");
            
            // create coordinate model form user given parameter
            CoordinateModel currentPosition = new CoordinateModel();
            currentPosition.Latitude = lat;
            currentPosition.Longitude = lon;
            
            // use connection to mongodb
            var server = StaticGlobal.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collectionParking = database.GetCollection<ParkingModel>("parking");
            
            // load all parking options from mongodb
            List<ParkingModel> parkingModels = new List<ParkingModel>();
           
            // fetch all parking options sorted by nearest
            var query = Query.Near("Coordinates", currentPosition.Longitude, currentPosition.Latitude);
            foreach (ParkingModel model in collectionParking.Find(query).SetLimit(500)) 
            {    
                // calculate the distance to the user and add to working list
                model.DistanceToUser = model.Coordinate.DistanceTo(currentPosition);
                parkingModels.Add(model);
            }
            
            Console.WriteLine("{0} parking options", parkingModels.Count);
            
            // filter places that have maximum parking hours lower than needed or 
            // opening hours that exceed the amount of time the user wants to park
            parkingModels = parkingModels.Where(delegate(ParkingModel a)
            {   
                // are there limitations on the amount of parking hours?
                if(a.MaximumParkingHours.HasValue) 
                {
                    // does this exceed the amount of hours the user wants to park?
                    if(a.MaximumParkingHours.Value > 0.0 && a.MaximumParkingHours.Value > hours)
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
                    
                    // check if current time + parking duration in limits of opening hours
                    int parkingDurationAbsolute = Int32.Parse(string.Format("hmm", DateTime.Now.AddHours(hours)));
                    if(openingModel.Open > parkingDurationAbsolute && parkingDurationAbsolute > openingModel.Close)
                    {
                        return false;
                    }
                }
                
                return true;
                
            }).ToList<ParkingModel>();
            
            Console.WriteLine("{0} parking options that are open/available", parkingModels.Count);
            
            // sort by closeness to current position
            parkingModels.Sort(delegate(ParkingModel a, ParkingModel b)
            {
                double dstA = a.DistanceToUser;
                double dstB = b.DistanceToUser;
                
                if(dstA > dstB) return 1;
                else if(dstA < dstB) return -1;
                else return 0;
            });
            
            // dictionary to store return values that translate to JSON
            Dictionary<string, object> returnValues = new Dictionary<string, object>();
            
            // check if a user is right on the closest parkingspot,
            // that would mean, that the STATE 'parked' would be induced
            if(parkingModels.First().DistanceToUser <= PARKING_RADIUS / 1000.0 && speed <= SLOW_SPEED)
            {
                // the first parking spot in the - by distance - sorted list will
                // be the closest
                ParkingModel model = parkingModels.First();
                
                // set appropriate state
                returnValues.Add("state", "parking");
                returnValues.Add("distance", Math.Round(model.DistanceToUser, 2));
                
                // append information about the parking spot we are on right now
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("id", model.Id);
                data.Add("name", model.Name);
                data.Add("price", model.PricePerHour);
                data.Add("type", model.Type);
                data.Add("redpoint", model.RedPointText);
                data.Add("description", model.Description);
                
                returnValues.Add("parking", data);
            }
            
            // user is still driving, we will offer him $(TAKE) of the best 
            // parking spots sorrounding him
            else 
            {
                // prepare the parking models for displaying on 
                // the enduser device
                List<Dictionary<string, object>> parking = new List<Dictionary<string, object>>();
                    
                // append good parking options around user
                foreach(ParkingModel model in parkingModels.Take(TAKE))
                {
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data.Add("id", model.Id);
                    data.Add("name", model.Name);
                    data.Add("price", model.PricePerHour);
                    data.Add("type", model.Type);
                    data.Add("coord", model.Coordinates);
                    
                    parking.Add(data);
                }
                
                // set appropriate state
                returnValues.Add("state", "driving");
                returnValues.Add("parking", parking);
            }
            
            return returnValues;
        }
          // GET /search
        // https://github.com/ParkEasy/api/wiki/API-Docs#search
        [HttpGet]
        [Route("status")]
        public dynamic Status(string id, int? amount)
        {
            if(String.IsNullOrEmpty(id) || !amount.HasValue){
                Response.StatusCode = 400;
                
                Dictionary<string, string> err = new Dictionary<string, string>();
                err.Add("error", "either 'amount' or 'id' not defined as parameters");
                
                return err;
            }
              // use connection to mongodb
            var server = StaticGlobal.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collectionStatus = database.GetCollection<ParkingModel>("status");
            
            StatusModel status = new StatusModel();
            status.ParkingId = id;
            status.Amount = amount;
            status.Time= DateTime.UtcNow;
            
            collectionSatus.Insert(status);
            
            return true;
        }
    }
}