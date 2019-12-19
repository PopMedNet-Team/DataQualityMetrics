using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASPE.DQM.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASPE.DQM.Controllers
{
    [Route("api/[controller]")]
    //[ApiController]
    public class VisualizationController : Controller
    {
        readonly ModelDataContext _modelDB;
        readonly ILogger<VisualizationController> _logger;
        readonly IConfiguration _config;
        readonly IAuthorizationService _authorizationService;

        public VisualizationController(ModelDataContext modelDB, ILogger<VisualizationController> logger, IConfiguration config, IAuthorizationService authorizationService)
        {
            _modelDB = modelDB;
            _logger = logger;
            _config = config;
            _authorizationService = authorizationService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var query = _modelDB.Visualizations.AsQueryable();

            if (!User.Identity.IsAuthenticated)
            {
                query = query.Where(v => v.RequiresAuth == false);
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(User, Identity.Claims.SystemAdministrator_Key);
            if (!authorizationResult.Succeeded)
            {
                query = query.Where(v => v.Published);
            }

            return new JsonResult(
                (await query.Select(v =>
                    new EditVisualizationRequest {
                        ID = v.ID,
                        title = v.Title,
                        appID = v.AppID,
                        sheetID = v.SheetID,
                        description = v.Description,
                        requireAuth = v.RequiresAuth,
                        publish = v.Published
                    }
                    ).ToArrayAsync())
                );
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var visualization = await _modelDB.Visualizations.FindAsync(id);

            if(visualization != null)
            {
                return new JsonResult(new EditVisualizationRequest
                {
                    ID = visualization.ID,
                    title = visualization.Title,
                    appID = visualization.AppID,
                    sheetID = visualization.SheetID,
                    description = visualization.Description,
                    requireAuth = visualization.RequiresAuth,
                    publish = visualization.Published
                });
            }
            else
            {
                return new JsonResult(new EditVisualizationRequest { });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterVisualizationRequest visualization)
        {
            if (User.Identity.IsAuthenticated == false || (await _authorizationService.AuthorizeAsync(User, Identity.Claims.SystemAdministrator_Key)).Succeeded == false)
            {
                return Unauthorized();
            }

            await _modelDB.Visualizations.AddAsync(new Visualization
            {
                Title = visualization.title,
                AppID = visualization.appID,
                SheetID = visualization.sheetID,
                Description = visualization.description,
                RequiresAuth = visualization.requireAuth,
                Published = visualization.publish
            });

            await _modelDB.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("edit")]
        public async Task<IActionResult> Edit([FromBody] EditVisualizationRequest visualization)
        {
            if(User.Identity.IsAuthenticated == false || (await _authorizationService.AuthorizeAsync(User, Identity.Claims.SystemAdministrator_Key)).Succeeded == false)
            {
                return Unauthorized();
            }

            var existingVisualization = await _modelDB.Visualizations.FindAsync(visualization.ID);

            if (existingVisualization == null)
                return NotFound();

            existingVisualization.Title = visualization.title;
            existingVisualization.AppID = visualization.appID;
            existingVisualization.SheetID = visualization.sheetID;
            existingVisualization.Description = visualization.description;
            existingVisualization.Published = visualization.publish;
            existingVisualization.RequiresAuth = visualization.requireAuth;

            await _modelDB.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            if (User.Identity.IsAuthenticated == false || (await _authorizationService.AuthorizeAsync(User, Identity.Claims.SystemAdministrator_Key)).Succeeded == false)
            {
                return Unauthorized();
            }

            var existingVisualization = await _modelDB.Visualizations.FindAsync(id);

            if (existingVisualization == null)
                return NotFound();

            _modelDB.Visualizations.Remove(existingVisualization);
            await _modelDB.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("bookmark/{id}")]
        public async Task<IActionResult> Bookmark([FromRoute] Guid id)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return Unauthorized();
            }

            if(!await _modelDB.Visualizations.AnyAsync(v => v.ID == id))
            {
                return NotFound();
            }

            Guid userID = Guid.Parse(User.Claims.Where(cl => cl.Type == Identity.Claims.UserID_Key).Select(cl => cl.Value).FirstOrDefault());
            var existingBookmark = await _modelDB.UserFavoriteVisualizations.FindAsync(userID, id);
            if(existingBookmark != null)
            {
                //delete to remove the bookmark
                _modelDB.UserFavoriteVisualizations.Remove(existingBookmark);
            }
            else
            {
                //add to bookmark the visualization for the user
                _modelDB.UserFavoriteVisualizations.Add(new UserVisualizationFavorite
                {
                    UserID = userID,
                    VisualizationID = id
                });
            }

            await _modelDB.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("bookmarks")]
        public async Task<IActionResult> Bookmarks()
        {
            Guid userID = Guid.Empty;
            if (User.Identity.IsAuthenticated)
            {
                userID = Guid.Parse(User.Claims.Where(cl => cl.Type == Identity.Claims.UserID_Key).Select(cl => cl.Value).FirstOrDefault());
            }

            var query = _modelDB.UserFavoriteVisualizations.Where(f => f.UserID == userID && f.Visualization.Published).OrderByDescending(f => f.CreatedOn).Select(f => new { f.Visualization.Title, f.VisualizationID });

            return new JsonResult(await query.ToArrayAsync());
        }

        public class RegisterVisualizationRequest
        {
            public string title { get; set; }
            public string appID { get; set; }
            public string sheetID { get; set; }
            public string description { get; set; }
            public bool requireAuth { get; set; }
            public bool publish { get; set; }
        }

        public class EditVisualizationRequest
        {
            public Guid ID { get; set; }
            public string title { get; set; }
            public string appID { get; set; }
            public string sheetID { get; set; }
            public string description { get; set; }
            public bool requireAuth { get; set; }
            public bool publish { get; set; }
        }
    }
}