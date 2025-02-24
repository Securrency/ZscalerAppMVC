using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ZscalerAppMVC.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ZscalerAppMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;

        public HomeController(ILogger<HomeController> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetIp()
        {
            var response = await _httpClient.GetStringAsync("/get-ip");
            var ipData = System.Text.Json.JsonDocument.Parse(response).RootElement;

            ViewData["IPAddress"] = ipData.GetProperty("ipAddress").GetString(); // Fixed key
            ViewData["ZscalerHostname"] = ipData.GetProperty("zscalerHostname").GetString(); // Fixed key
            ViewData["Location"] = ipData.GetProperty("location").GetString(); // Fixed key

            return View("GetIP");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}