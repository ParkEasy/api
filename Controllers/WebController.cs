using System;
using Microsoft.AspNet.Mvc;

namespace ParkEasyAPI.Controllers
{   
    public class WebController : Controller
    {   
		[Route("")]
		public ActionResult Index()
		{	
			return View();
		}
	}
}