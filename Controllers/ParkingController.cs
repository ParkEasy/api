using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ParkEasyAPI.Controllers
{
    [Route("[controller]")]
    public class ParkingController : Controller
    {
        // GET /parking/5
        [HttpGet("closest/{lat}/{lon}")]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
    }
}
