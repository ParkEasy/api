using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using ParkEasyAPI.Models;

namespace ParkEasyAPI.Controllers
{   
    public class ParkingController : Controller
    {   
        // GET /closest
        // https://github.com/ParkEasy/api/wiki/API-Docs#closest
        [HttpGet]
        [Route("closest")]
        public dynamic Closest(float lat = -1, float lon = -1, int hours = 1)
        {
            int START_RADIUS = 500;
            int TAKE = 5;
            
            // validity check: are lat and long specified?
            if(lat < 0 || lon < 0) 
            {
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
            int radius = START_RADIUS;
            List<ParkingModel> radiusModels = new List<ParkingModel>();
            while(radiusModels.Count <= TAKE) 
            {
                // filter all the places that are not within radius distance and 
                // that have no space available anyway
                radiusModels = parkingModels.Where(delegate(ParkingModel a)
                {
                    return a.DistanceToUser < radius / 1000 && a.Capacity > 0;
                    
                }).ToList<ParkingModel>();
                
                radius += 500;
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
            
            return parkingModels.Take(TAKE);
        }
    }
}
