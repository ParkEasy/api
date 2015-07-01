using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ParkEasyAPI.Models;
using ParkEasyAPI.Parser;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using WilsonScore;

namespace ParkEasyAPI.Controllers
{   
    public class ParkingController : Controller
    {   
        // GET /all
        [HttpGet]
        [Route("all")]
        public dynamic All()
        {
            Response.Headers.Add("Access-Control-Allow-Origin", new string[]{"*"});
            
            // use connection to mongodb
            var server = StaticGlobal.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collectionParking = database.GetCollection<ParkingModel>("parking");
            
            dynamic geojson = new Dictionary<string, Object>();
            
            try
            {
                geojson["type"] = "FeatureCollection";
                geojson["features"] = new List<dynamic>();
       
                foreach(ParkingModel parking in collectionParking.FindAll().SetFields(new string[]{"Name", "Free", "Coordinates", "Type", "Capacity"}))
                {
                    parking.CalcFreeLikelihood();
                    
                    dynamic feature = new Dictionary<string, Object>();
                    feature["type"] = "Feature";
                    
                    feature["geometry"] = new Dictionary<string, Object>();
                    feature["geometry"]["type"] = "Point";
                    feature["geometry"]["coordinates"] = parking.Coordinates;
                    
                    feature["properties"] = new Dictionary<string, Object>();
                    feature["properties"]["title"] = parking.Name;
                    feature["properties"]["description"] = parking.Capacity + " Plätze<br>Wahrsch. freier Parkpl.: " + Math.Round(parking.FreeLikelihood*100);
                    feature["properties"]["marker-size"] = "large";
                    
                    switch(parking.Type)
                    {
                        case ParkingType.Garage:
                            
                            feature["properties"]["marker-symbol"] = "parking";
                            feature["properties"]["marker-color"] = "#e74c3c";
                            break;
                            
                       case ParkingType.TicketMachine:
                       
                            feature["properties"]["marker-color"] = "#3498db";
                            feature["properties"]["marker-symbol"] = "parking";
                            break;
                            
                       case ParkingType.University:
                            feature["properties"]["marker-symbol"] = "parking";
                            feature["properties"]["marker-color"] = "#9b59b6";
                            break;
                    }
                    
                    geojson["features"].Add(feature);
                }
            }
            catch(Exception e)
            {
                return e;
            }
            
            return geojson;
        }
        
        // GET /detail
        [HttpGet]
        [Route("detail")]
        public dynamic Detail(string id)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", new string[]{"*"});
            
            // use connection to mongodb
            var server = StaticGlobal.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collectionParking = database.GetCollection<ParkingModel>("parking");
            
            return collectionParking.FindOneById(id);
        }
        
        // GET /search
        // https://github.com/ParkEasy/api/wiki/API-Docs#search
        [HttpGet]
        [Route("search")]
        public dynamic Search(float lat = -1, float lon = -1, int hours = 1, double speed = 0.0)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", new string[]{"*"});
            
            // constant parameters
            int TAKE = 5;
            double PARKING_RADIUS = 5.0;
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
            var collectionStatus = database.GetCollection<StatusModel>("status");
            
            // load all parking options from mongodb
            List<ParkingModel> parkingModels = new List<ParkingModel>();
           
            // fetch all parking options sorted by nearest
            var query = Query.Near("Coordinates", currentPosition.Longitude, currentPosition.Latitude, 0.65/111.12);
            foreach (ParkingModel model in collectionParking.Find(query).SetLimit(500)) 
            {   
                model.CalcFreeLikelihood();
                
                // calculate the distance to the user and add to working list
                model.DistanceToUser = model.Coordinate.DistanceTo(currentPosition);
                
                parkingModels.Add(model);
            }
            
            Console.WriteLine("{0} parking options", parkingModels.Count);
            
            // filter places that have maximum parking hours lower than needed or 
            // opening hours that exceed the amount of time the user wants to park
            parkingModels = parkingModels.Where(delegate(ParkingModel a)
            {   
                if(a.FreeLikelihood == 0.0 || a.Price == null)
                {
                    return false;
                }
                
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
                    int parkingDurationAbsolute = Convert.ToInt32(string.Format("{0:HHmm}", DateTime.Now.AddHours(hours)));
                    if(openingModel.Open > parkingDurationAbsolute && parkingDurationAbsolute > openingModel.Close)
                    {
                        return false;
                    }
                }
                
                return true;
                
            }).ToList<ParkingModel>();
            
            Console.WriteLine("{0} parking options that are open/available", parkingModels.Count);
            
            // dictionary to store return values that translate to JSON
            Dictionary<string, object> returnValues = new Dictionary<string, object>();
            
            // there are no available parking options in the area
            if(parkingModels.Count == 0)
            {   
                // set appropriate state
                returnValues.Add("state", "driving");
                returnValues.Add("parking", parkingModels);
                return returnValues;
            }
            
            // store the current first one in the list since it will be the closest
            // due to mongodb's geosearch
            ParkingModel closestModel = parkingModels.First();
            
            // sort by closeness to current position
            parkingModels.Sort(delegate(ParkingModel a, ParkingModel b)
            {   
                double scoreA = a.DistanceToUser * PriceParser.Interpret(a.Price, hours) * a.FreeLikelihood;
                double scoreB = b.DistanceToUser * PriceParser.Interpret(b.Price, hours) * b.FreeLikelihood;
                
                if(scoreA > scoreB) return 1;
                else if(scoreA < scoreB) return -1;
                else return 0;
            });
            
            // check if a user is right on the closest parkingspot,
            // that would mean, that the STATE 'parked' would be induced
            if(closestModel.DistanceToUser <= PARKING_RADIUS / 1000.0 && speed <= SLOW_SPEED)
            {   
                // set appropriate state
                returnValues.Add("state", "parking");
                returnValues.Add("distance", Math.Round(closestModel.DistanceToUser, 2));
                
                // append information about the parking spot we are on right now
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("id", closestModel.Id);
                data.Add("name", closestModel.Name);
                data.Add("price", closestModel.Price.PerHour.Price);
                data.Add("type", closestModel.Type);
                data.Add("redpoint", closestModel.RedPointText);
                data.Add("description", closestModel.Description);
                
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
                    Console.WriteLine(PriceParser.Interpret(model.Price, hours));
                    
                    Dictionary<string, object> data = new Dictionary<string, object>();
                    data.Add("id", model.Id);
                    data.Add("name", model.Name);
                    data.Add("price", PriceParser.Interpret(model.Price, hours));
                    data.Add("type", model.Type);
                    data.Add("coord", model.Coordinates);
                    data.Add("free", model.FreeLikelihood);
                    
                    parking.Add(data);
                }
                
                // set appropriate state
                returnValues.Add("state", "driving");
                returnValues.Add("parking", parking);
            }
            
            return returnValues;
        }
          // GET /status
        // https://github.com/ParkEasy/api/wiki/API-Docs#status
        [HttpGet]
        [Route("status")]
        public dynamic Status(string id, int? amount, bool hq = false)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", new string[]{"*"});
            
            if(String.IsNullOrEmpty(id) || !amount.HasValue)
            {
                Response.StatusCode = 400;
                
                Dictionary<string, string> err = new Dictionary<string, string>();
                err.Add("error", "either 'amount' or 'id' not defined as parameters");
                
                return err;
            }
              // use connection to mongodb
            var server = StaticGlobal.MongoDBClient.GetServer();
            var database = server.GetDatabase("parkeasy");
            var collectionStatus = database.GetCollection<StatusModel>("status");
            var collectionParking = database.GetCollection<ParkingModel>("parking");
            
            StatusModel status = new StatusModel();
            status.ParkingId = id;
            status.Amount = amount.Value;
            status.Time = DateTime.UtcNow;
            status.Id = ObjectId.GenerateNewId();
            status.HighQualitySample = hq;
            
            collectionStatus.Insert(status);
            
            // count up and downvotes
            var queryStati = new QueryDocument {
                { "ParkingId", id }
            };
            
            int upvotes = 0;
            int total = 0;
            foreach (StatusModel stati in collectionStatus.Find(queryStati)) 
            {
                if(stati.HighQualitySample == false)
                {
                    if(stati.Amount > 0) {
                        upvotes++;
                    }
                    
                    total++;
                }
            }
            
            double ws = Wilson.Score(upvotes, total);
            
            // update the parking document to for denormalization purposes
            var query = new QueryDocument {
                { "_id", id }
            };
            
            var update = new UpdateDocument {
                { "$set", new BsonDocument("FreeLikelihood", ws) }
            };
            
            collectionParking.Update(query, update);
            
            return true;
        }
    }
}