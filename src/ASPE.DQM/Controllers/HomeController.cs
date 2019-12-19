using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ASPE.DQM.Models;
using Microsoft.AspNetCore.Identity;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ASPE.DQM.Controllers
{
    public class HomeController : Controller
    {
        readonly Model.ModelDataContext _modelDB;
        readonly UserManager<Identity.IdentityUser> _userManager;
        readonly SignInManager<Identity.IdentityUser> _signInManager;
        readonly IConfiguration _config;

        public HomeController(Model.ModelDataContext modelDB, UserManager<Identity.IdentityUser> userManager, SignInManager<Identity.IdentityUser> signInManager, IConfiguration config)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
            this._config = config;
            this._modelDB = modelDB;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Route("my-dashboard")]
        public IActionResult Dashboard()
        {
            if(User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [Route("resources")]
        public IActionResult Resources()
        {
            return View();
        }

        [Route("metrics")]
        public IActionResult Metrics()
        {
            return View();
        }

        [Route("metric/{id}")]
        public async Task<IActionResult> MetricDetails(Guid id)
        {
            //confirm the user has permission to edit the metric based on it's status
            var currentStatus = await _modelDB.MetricStatusItems.Where(si => si.MetricID == id).OrderByDescending(si => si.CreateOn).Select(si => si.MetricStatus).FirstOrDefaultAsync();

            var metric = await _modelDB.Metrics.Where(m => m.ID == id).Select(m => new { m.Title, m.AuthorID }).FirstOrDefaultAsync();

            var claims = User.Claims.Where(cl => cl.Type == Identity.Claims.UserID_Key || cl.Type == Identity.Claims.SystemAdministrator_Key || cl.Type == Identity.Claims.AuthorMetric_Key).ToArray();

            Guid userID;
            if(!Guid.TryParse(claims.Where(cl => cl.Type == Identity.Claims.UserID_Key).Select(cl => cl.Value).FirstOrDefault(), out userID))
            {
                userID = Guid.Empty;
            }

            var vm = new Models.MetricDetailsViewModel
            {
                ID = id,
                Title = metric.Title,
                CurrentStatusID = currentStatus.ID,
                AllowEdit = currentStatus.AllowEdit,
                AllowCopy = claims.Any(cl => cl.Type == Identity.Claims.AuthorMetric_Key),
                IsSystemAdministrator = claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key),
                IsAuthor = metric.AuthorID == userID
            };

            if ((metric.AuthorID == userID && (currentStatus.Access & Model.MetricStatusAccess.Author) == Model.MetricStatusAccess.Author) ||
               (vm.IsSystemAdministrator && (currentStatus.Access & Model.MetricStatusAccess.SystemAdministrator) == Model.MetricStatusAccess.SystemAdministrator) ||
               (User.Identity.IsAuthenticated && (currentStatus.Access & Model.MetricStatusAccess.AuthenticatedUser) == Model.MetricStatusAccess.AuthenticatedUser) ||
               ((currentStatus.Access & Model.MetricStatusAccess.Public) == Model.MetricStatusAccess.Public))
            {

                vm.AllowView = true;

                return View(vm);
            }

            return RedirectToAction("NotAuthorized");
        }

        [Authorize, Route("metric/{id}/edit")]
        public async Task<IActionResult> MetricEdit(Guid id)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return RedirectToAction("Index");
            }

            //confirm the user has permission to edit the metric based on it's status
            var currentStatus = await _modelDB.MetricStatusItems.Where(si => si.MetricID == id).OrderByDescending(si => si.CreateOn).Select(si => si.MetricStatus).FirstOrDefaultAsync();
            if(currentStatus.AllowEdit == false)
            {
                return BadRequest("The current status of the Metric does not allow for edit.");
            }

            var metric = await _modelDB.Metrics.Where(m => m.ID == id).Select(m => new { m.Title, m.AuthorID }).FirstOrDefaultAsync();

            var claims = User.Claims.Where(cl => cl.Type == Identity.Claims.UserID_Key || cl.Type == Identity.Claims.SystemAdministrator_Key).ToArray();
            var userID = Guid.Parse(claims.First(cl => cl.Type == Identity.Claims.UserID_Key).Value);

            if((metric.AuthorID == userID && (currentStatus.Access & Model.MetricStatusAccess.Author) == Model.MetricStatusAccess.Author) ||
               (claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key) && (currentStatus.Access & Model.MetricStatusAccess.SystemAdministrator) == Model.MetricStatusAccess.SystemAdministrator))
            {
                //only the author or a system admin can edit a metric - depends also on if access level of the status
                ViewBag.ID = id;
                ViewBag.MetricTitle = Utils.StringEx.Ellipse(metric.Title, 50, "...");

                return View();
            }

            return RedirectToAction("NotAuthorized");
        }

        [Authorize, Route("submit-metric")]
        public IActionResult SubmitMetric()
        {
            //view will handle not allowing submit if the user is not authenticated or does not have permission to submit metric
            return View();
        }

        [Route("measures")]
        public IActionResult Measures()
        {
            return View();
        }

        [Authorize, Route("submit-measure")]
        public IActionResult SubmitMeasure()
        {
            return View();
        }

        [Route("visualizations")]
        public IActionResult Visualizations()
        {
            return View();
        }

        [Authorize, Route("register-visualization"), Route("edit-visualization/{id}")]
        public IActionResult RegisterVisualization(Guid? id)
        {
            if (id.HasValue)
                ViewBag.ID = id.Value;

            return View();
        }

        [Authorize, Route("manage-measures")]
        public IActionResult ManageSubmittedMeasures()
        {
            return View();
        }

        [Route("visual/{id}")]
        public async Task<IActionResult> Visual(Guid id)
        {
            var visualization = await _modelDB.Visualizations.FindAsync(id);

            if(visualization == null || visualization.Published == false || (visualization.RequiresAuth && User.Identity.IsAuthenticated == false))
            {
                return NotFound();
            }

            ViewBag.VisualDescription = visualization.Description;

            var urlBuilder = new UriBuilder("https://", _config["QlikServer"], Convert.ToInt32(_config["QlikServerQPSPort"]));
            urlBuilder.Path = "qps/" + _config["QlikQPSPrefix"];

            var req = new QlikAuth.Ticket
            {
                UserDirectory = _config["QlikUserDirectory"],
                UserId = _config["QlikUserID"],
                ProxyRestUri = urlBuilder.ToString(),
                TargetId = string.Empty,
                CertificateThumbprint = _config["QlikQPSCertThumbprint"]
            };

            if (string.Equals(_config["QlikCertLocation"], "LocalMachine", StringComparison.OrdinalIgnoreCase))
            {
                req.CertificateLocation = System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine;
            }
            else if (string.Equals(_config["QlikCertLocation"], "CurrentUser", StringComparison.OrdinalIgnoreCase))
            {
                req.CertificateLocation = System.Security.Cryptography.X509Certificates.StoreLocation.CurrentUser;
            }
            else
            {
                throw new Exception("The Configuration didnt specify a correct location of the Certifictae.");
            }

            string ticket = req.TicketRequest();


            //https://help.qlik.com/en-US/sense-developer/September2019/Subsystems/Mashups/Content/Sense_Mashups/AppIntegrationAPI/app-integration.htm

            string embedUrl;
            if (!string.IsNullOrWhiteSpace(visualization.SheetID))
            {
                embedUrl = $"https://{_config["QlikServer"]}/{_config["QlikQPSPrefix"]}/single/?appid={visualization.AppID}&sheet={visualization.SheetID}&opt=ctxmenu,currsel&identity={_config["QlikUserID"]}&{ticket}";
                //embedUrl = $"https://{_config["QlikServer"]}/{_config["QlikQPSPrefix"]}/single/?appid={visualization.AppID}&sheet={visualization.SheetID}&opt=ctxmenu,currsel";
            }
            else
            {
                embedUrl = $"https://{_config["QlikServer"]}/{_config["QlikQPSPrefix"]}/sense/app/{visualization.AppID}?{ticket}";
                //embedUrl = $"https://{_config["QlikServer"]}/{_config["QlikQPSPrefix"]}/single/?appid={visualization.AppID}&apt=ctxmenu,currsel";
            }

            bool bookmarked = false;
            if (User.Identity.IsAuthenticated)
            {
                Guid userID = Guid.Parse(User.Claims.Where(cl => cl.Type == Identity.Claims.UserID_Key).Select(cl => cl.Value).FirstOrDefault());
                bookmarked = await _modelDB.UserFavoriteVisualizations.AnyAsync(f => f.VisualizationID == id && f.UserID == userID);
            }

            return View(new VisualizationViewModel {
                ID = visualization.ID,
                Title = visualization.Title,
                Description = visualization.Description,
                AppID = visualization.AppID,
                SheetID = visualization.SheetID,
                RequiresAuthentication = visualization.RequiresAuth,
                EmbedUrl = embedUrl,
                Bookmarked = bookmarked
            });
            
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Registration")]
        public IActionResult Registration()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [Authorize, Route("redirect-to-pmn")]
        public async Task<IActionResult> RedirectToPMN()
        {
            var user = await _userManager.GetUserAsync(User);

            var cookie = Crypto.DecryptStringAES(Request.Cookies.Where(x => x.Key == "DQM-User").Select(x => x.Value).FirstOrDefault(), _config["PMNoAuthHash"], _config["PMNoAuthKey"]).Split(':');

            var url = string.Format("{0}?Data={1}&returnUrl={2}",
                _config["PMNPortal"],
                HttpUtility.UrlEncode(Crypto.EncryptStringAES(string.Format("{0}:{1}", user.UserName, cookie[1]), _config["PMNoAuthHash"], _config["PMNoAuthKey"])),
                HttpUtility.UrlEncode("/users/details?ID=" + user.Id));


            return new RedirectResult(url);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("Test")]
        public IActionResult Test()
        {
            return View();
        }

        [Route("NotAuthorized")]
        public IActionResult NotAuthorized()
        {
            return View();
        }

        [Route("{*url}", Order = 999)]
        public IActionResult PageNotFound()
        {
            Response.StatusCode = 404;
            return View();
        }
    }
}
