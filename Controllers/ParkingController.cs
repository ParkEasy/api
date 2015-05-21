using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ParkEasyAPI.Controllers
{
    public class ParkingController : Controller
    {
        // GET /closest
        // https://github.com/ParkEasy/api/wiki/API-Docs#closest
        [HttpGet]
        [Route("closest")]
        public IEnumerable<float> Closest(float lat, float lon, int radius = 5000, int hours = 1)
        {
            return new float[] { lat, lon, radius, hours };
        }
    }
}
