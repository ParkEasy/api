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
        [Route("closest")]
        [HttpGet]
        public IEnumerable<float> Closest(float lat, float lon)
        {
            return new float[] { lat, lon };
        }
    }
}
