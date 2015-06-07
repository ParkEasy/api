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
            ParkingLoader parkingLoader = new ParkingLoader();
            List<ParkingModel> parkingModels = parkingLoader.Load(currentPosition);
            
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
