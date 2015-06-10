using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ParkEasyAPI.Models;
using ParkEasyAPI.Data;

namespace ParkEasyAPI.Controllers
{   
    public class ParkingController : Controller
    {   
        // GET /closest
        // https://github.com/ParkEasy/api/wiki/API-Docs#closest
        [HttpGet]
        [Route("closest")]
        public dynamic Closest(float lat = -1, float lon = -1, int hours = 1, double speed = 0.0)
        {
            double START_RADIUS = 300.0;
            double INCREMENT_RADIUS = 100.0; 
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
            
            // load data from various datasources
            ParkingLoader parkingLoader = new ParkingLoader();
            List<ParkingModel> parkingModels = parkingLoader.Load(currentPosition);
            
            Console.WriteLine("{0} parking options overall", parkingModels.Count);
            
            // slowly increase the query radius by 500m until we found at least 
            // 5 parking spaces in radius
            double radius = START_RADIUS;
            List<ParkingModel> radiusModels = new List<ParkingModel>();
            while(radiusModels.Count <= TAKE) 
            {
                // filter all the places that are not within radius distance and 
                // that have no space available anyway
                radiusModels = parkingModels.Where(delegate(ParkingModel a)
                {
                    return a.DistanceToUser < radius / 1000.0 && a.Capacity > 0;
                    
                }).ToList<ParkingModel>();
                
                radius += INCREMENT_RADIUS;
            }

            parkingModels = radiusModels;
            
            Console.WriteLine("{0} parking options in radius", parkingModels.Count);
            
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
                data.Add("id", model.ID);
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
                    data.Add("id", model.ID);
                    data.Add("name", model.Name);
                    data.Add("price", model.PricePerHour);
                    data.Add("type", model.Type);
                    data.Add("coord", new List<double>(){ model.Coordinate.Latitude, model.Coordinate.Longitude });
                    
                    parking.Add(data);
                }
                
                // set appropriate state
                returnValues.Add("state", "driving");
                returnValues.Add("radius", radius);
                returnValues.Add("parking", parking);
            }
            
            return returnValues;
        }
    }
}