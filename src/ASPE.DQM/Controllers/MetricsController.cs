using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASPE.DQM.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ASPE.DQM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetricsController : Controller
    {
        readonly Model.ModelDataContext _db;
        readonly IAuthorizationService _authorizationService;
        readonly IServiceProvider _serviceProvider;

        public MetricsController(Model.ModelDataContext modelDB, IAuthorizationService authorizationService, IServiceProvider serviceProvider)
        {
            _db = modelDB;
            _authorizationService = authorizationService;
            _serviceProvider = serviceProvider;
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(bool returnMyOnly = false)
        {
            var userID = User.Identity.IsAuthenticated ? Guid.Parse(User.Claims.Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Select(x => x.Value).FirstOrDefault()) : Guid.Empty;            
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, Identity.Claims.SystemAdministrator_Key);
            bool isSystemAdministrator = authorizationResult.Succeeded;

            var query = from m in _db.Metrics
                        join statusItem in _db.MetricStatusItems on m.ID equals statusItem.MetricID
                        join status in _db.MetricStatuses on statusItem.MetricStatusID equals status.ID
                        let sysAdminAccess = MetricStatusAccess.SystemAdministrator
                        let authorAccess = MetricStatusAccess.Author
                        let authUserAccess = MetricStatusAccess.AuthenticatedUser
                        let publicAccess = MetricStatusAccess.Public
                        where statusItem == m.Statuses.OrderByDescending(s => s.CreateOn).FirstOrDefault()
                        && statusItem.ID != MetricStatus.DeletedID &&
                       (
                           //system administrator
                           (isSystemAdministrator && (status.Access & sysAdminAccess) == sysAdminAccess)
                           //author
                           || (m.AuthorID == userID && (status.Access & authorAccess) == authorAccess)
                           //authenticated user
                           || (userID != Guid.Empty && (status.Access & authUserAccess) == authUserAccess)
                           //anyone
                           || ((status.Access & publicAccess) == publicAccess)
                       )
                        select new
                        {
                            ID = m.ID,
                            Author = new { m.Author.ID, m.Author.UserName, m.Author.FirstName, m.Author.LastName },
                            ServiceDeskUrl = m.ServiceDeskUrl,
                            Title = m.Title,
                            Status = new { statusItem.ID, User = new { statusItem.User.ID, statusItem.User.UserName, statusItem.User.FirstName, statusItem.User.LastName }, statusItem.Note, statusItem.MetricStatusID, status.Title, statusItem.CreateOn },
                            Domains = m.Domains.OrderBy(d => d.Domain.Title).Select(d => new { d.Domain.ID, d.Domain.Title }),
                            FrameworkCategories = m.FrameworkCategories.OrderBy(f => f.DataQualityFrameworkCategory.Title).ThenBy(f => f.DataQualityFrameworkCategory.SubCategory).Select(f => new { f.DataQualityFrameworkCategory.ID, f.DataQualityFrameworkCategory.Title, f.DataQualityFrameworkCategory.SubCategory }),
                            ResultsType = new { m.ResultsType.ID, m.ResultsType.Value }
                        };

            if (returnMyOnly)
            {
                query = query.Where(x => x.Author.ID == userID);
            }

            var metrics = await query.ToArrayAsync();

            return Json(metrics);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var userID = User.Identity.IsAuthenticated ? Guid.Parse(User.Claims.Where(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier).Select(x => x.Value).FirstOrDefault()) : Guid.Empty;
            var authorizationResult = await _authorizationService.AuthorizeAsync(User, Identity.Claims.SystemAdministrator_Key);
            bool isSystemAdministrator = authorizationResult.Succeeded;

            var query = from m in _db.Metrics
                        join statusItem in _db.MetricStatusItems on m.ID equals statusItem.MetricID
                        join status in _db.MetricStatuses on statusItem.MetricStatusID equals status.ID
                        let sysAdminAccess = MetricStatusAccess.SystemAdministrator
                        let authorAccess = MetricStatusAccess.Author
                        let authUserAccess = MetricStatusAccess.AuthenticatedUser
                        let publicAccess = MetricStatusAccess.Public
                        where statusItem == m.Statuses.OrderByDescending(s => s.CreateOn).FirstOrDefault()
                        && statusItem.ID != MetricStatus.DeletedID &&
                       (
                           //system administrator
                           (isSystemAdministrator && (status.Access & sysAdminAccess) == sysAdminAccess)
                           //author
                           || (m.AuthorID == userID && (status.Access & authorAccess) == authorAccess)
                           //authenticated user
                           || (userID != Guid.Empty && (status.Access & authUserAccess) == authUserAccess)
                           //anyone
                           || ((status.Access & publicAccess) == publicAccess)
                       )
                        select new
                        {
                            m.ID,
                            m.Timestamp,
                            m.AuthorID,
                            m.Title,
                            m.Description,
                            m.Justification,
                            m.ExpectedResults,
                            m.CreatedOn,
                            m.ModifiedOn,
                            m.ServiceDeskUrl,
                            m.ResultsTypeID,
                            Author = new { m.Author.ID, m.Author.FirstName, m.Author.LastName, m.Author.UserName },
                            Status = new { statusItem.ID, User = new { statusItem.User.ID, statusItem.User.UserName, statusItem.User.FirstName, statusItem.User.LastName }, statusItem.Note, statusItem.MetricStatusID, status.Title, statusItem.CreateOn },
                            Domains = m.Domains.OrderBy(d => d.Domain.Title).Select(d => d.DomainID),
                            FrameworkCategories = m.FrameworkCategories.OrderBy(f => f.DataQualityFrameworkCategory.Title).ThenBy(f => f.DataQualityFrameworkCategory.SubCategory).Select(f => f.DataQualityFrameworkCategoryID),
                            Bookmarked = _db.UserFavoriteMetrics.Where(f => f.MetricID == id && f.UserID == userID).Any()
                        };

            var metric = await query.Where(m => m.ID == id).FirstOrDefaultAsync();

            if (metric == null)
            {
                return NotFound();
            }

            return Json(new {
                metric.ID,
                metric.Timestamp,
                metric.AuthorID,
                metric.Title,
                metric.Description,
                metric.Justification,
                metric.ExpectedResults,
                metric.CreatedOn,
                metric.ModifiedOn,
                metric.ServiceDeskUrl,
                status = metric.Status,
                author = metric.Author,
                domains = metric.Domains,
                frameworkCategories = metric.FrameworkCategories,
                metric.ResultsTypeID,
                bookmarked = metric.Bookmarked
            });
        }

        [HttpGet("dependencies")]
        public async Task<IActionResult> GetDependencyCollections()
        {
            var dependencies = new
            {
                Domains = await _db.Domains.OrderBy(d => d.Title).Select(d => new { d.ID, d.Title }).ToArrayAsync(),
                ResultsTypes = await _db.MetricResultTypes.OrderBy(r => r.Value).Select(r => new { r.ID, r.Value }).ToArrayAsync(),
                FrameworkCategories = await _db.DataQualityFrameworkCategories.OrderBy(f => f.Title).ThenBy(f => f.SubCategory).Select(f => new { f.ID, f.Title, f.SubCategory }).ToArrayAsync()
            };

            return Json(dependencies);
        }

        [Authorize, HttpPost("{id}")]
        public async Task<IActionResult> Update([FromBody]MetricUpdateRequest data)
        {
            if (data == null)
                return BadRequest("No data received.");

            var metric = await _db.Metrics
                .Include(m => m.Domains)
                .Include(m => m.FrameworkCategories)
                .Where(m => m.ID == data.ID).FirstOrDefaultAsync();

            if (metric == null)
                return NotFound();

            //make sure only the author or a system administrator is saving metric, and they have the author metric claim
            if (!User.HasClaim(cl => cl.Type == Identity.Claims.AuthorMetric_Key))
            {
                return BadRequest("The current user does not have permission to author a metric.");
            }

            var currentUserID = Guid.Parse(User.Claims.Where(x => x.Type == Identity.Claims.UserID_Key).Select(x => x.Value).FirstOrDefault());
            
            if(metric.AuthorID != currentUserID && !User.HasClaim(cl => cl.Type == Identity.Claims.SystemAdministrator_Key))
            {
                return BadRequest("The current user is not the author of the metric and does not have permission to edit the metric.");
            }

            var currentStatus = await _db.MetricStatusItems.Where(si => si.MetricID == metric.ID).OrderByDescending(si => si.CreateOn).Select(si => si.MetricStatus).FirstOrDefaultAsync();
            if(currentStatus.AllowEdit == false)
            {
                return BadRequest("The current status of the metric does not allow for edit.");
            }

            //TODO: validate the minimum required metadata is included

            //update the metric
            metric.Description = data.Description;
            metric.Justification = data.Justification;
            metric.ExpectedResults = data.ExpectedResults;
            metric.ModifiedOn = DateTime.UtcNow;
            metric.ResultsTypeID = data.ResultsTypeID;
            metric.ServiceDeskUrl = data.ServiceDeskUrl;
            metric.Title = data.Title;

            foreach(var domainID in data.Domains.Except(metric.Domains.Select(d => d.DomainID)))
            {
                metric.AddDomains(domainID);
            }

            foreach (var domain in metric.Domains.Where(d => !data.Domains.Contains(d.DomainID)).ToArray())
            {
                metric.Domains.Remove(domain);
            }

            foreach (var frameworkCategoryID in data.FrameworkCategories.Except(metric.FrameworkCategories.Select(d => d.DataQualityFrameworkCategoryID)))
            {
                metric.AddFrameworkCategories(frameworkCategoryID);
            }

            foreach(var category in metric.FrameworkCategories.Where(c => !data.FrameworkCategories.Contains(c.DataQualityFrameworkCategoryID)).ToArray())
            {
                metric.FrameworkCategories.Remove(category);
            }

            await _db.SaveChangesAsync();

            return await Get(data.ID);
        }

        [Authorize, HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody]MetricStatusUpdateRequest data)
        {
            if (data == null)
                return BadRequest("No data received.");

            var metric = await _db.Metrics
                .Where(m => m.ID == data.MetricID)
                .Select(m => new { m.AuthorID, PreviousMetricStatusItemID = m.Statuses.OrderByDescending(s => s.CreateOn).Select(s => s.ID).FirstOrDefault() })
                .FirstOrDefaultAsync();

            if (metric == null)
                return NotFound("Metric not found.");

            var newStatus = await _db.MetricStatuses.Where(s => s.ID == data.NewStatusID).FirstOrDefaultAsync();

            if (newStatus == null)
                return NotFound("New status not found.");

            var claims = User.Claims.ToArray();
            Guid userID;
            if(!Guid.TryParse(claims.Where(cl => cl.Type == Identity.Claims.UserID_Key).Select(cl => cl.Value).FirstOrDefault(), out userID))
            {
                return BadRequest("Unable to determine the current user, user is not authenticated.");
            }

            if(metric.AuthorID != userID && !claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key))
            {
                return BadRequest("The current user does not have permission to change the status of the request.");
            }

            var metricStatus = new Model.MetricStatusItem { MetricID = data.MetricID, MetricStatusID = data.NewStatusID, PreviousMetricStatusID = metric.PreviousMetricStatusItemID, UserID = userID };
            if (!string.IsNullOrWhiteSpace(data.Comment))
                metricStatus.Note = data.Comment;

            _db.MetricStatusItems.Add(metricStatus);

            await _db.SaveChangesAsync();

            return Ok(new { metricStatusID = newStatus.ID, allowEdit = newStatus.AllowEdit });            
        }

        public class MetricStatusUpdateRequest
        {
            public Guid MetricID { get; set; }

            public Guid NewStatusID { get; set; }

            public string Comment { get; set; }
        }

        [Authorize, HttpPost("new")]
        public async Task<IActionResult> New([FromBody]MetricUpdateRequest data)
        {
            if (data == null)
                return BadRequest("No data received.");

            //make sure only the author or a system administrator is saving metric, and they have the author metric claim
            if (!User.HasClaim(cl => cl.Type == Identity.Claims.AuthorMetric_Key))
            {
                return BadRequest("The current user does not have permission to author a metric.");
            }

            //TODO: validate the minimum required metadata is included

            var currentUserID = Guid.Parse(User.Claims.Where(x => x.Type == Identity.Claims.UserID_Key).Select(x => x.Value).FirstOrDefault());

            var metric = new Model.Metric {
                AuthorID = currentUserID,
                Description = data.Description,
                Justification = data.Justification,
                ExpectedResults = data.ExpectedResults,
                ModifiedOn = DateTime.UtcNow,
                ResultsTypeID = data.ResultsTypeID,
                ServiceDeskUrl = data.ServiceDeskUrl ?? string.Empty,
                Title = data.Title
            };

            _db.Metrics.Add(metric);

            metric.Statuses.Add(new Model.MetricStatusItem { MetricID = metric.ID, MetricStatusID = Model.MetricStatus.DraftID, UserID = currentUserID });            

            foreach (var domainID in data.Domains)
            {
                metric.AddDomains(domainID);
            }

            foreach (var frameworkCategoryID in data.FrameworkCategories)
            {
                metric.AddFrameworkCategories(frameworkCategoryID);
            }

            await _db.SaveChangesAsync();

            return await Get(metric.ID);
        }

        public class MetricUpdateRequest
        {
            public Guid ID { get; set; }
            public string Title { get; set; }

            public string Description { get; set; }

            public string Justification { get; set; }

            public string ExpectedResults { get; set; }

            public string ServiceDeskUrl { get; set; }

            public Guid ResultsTypeID { get; set; }

            public IEnumerable<Guid> Domains { get; set; }

            public IEnumerable<Guid> FrameworkCategories { get; set; }
        }

        public class SimilarMetricsRequest
        {
            public Guid? ResultsTypeID { get; set; }

            public IEnumerable<Guid> Domains { get; set; }

            public IEnumerable<Guid> FrameworkCategories { get; set; }
        }

        [HttpPost("find")]
        public async Task<IActionResult> FindSimilar([FromBody] SimilarMetricsRequest data)
        {
            if(data.ResultsTypeID.HasValue == false && (data.Domains == null || data.Domains.Any() == false) && (data.FrameworkCategories == null || data.FrameworkCategories.Any() == false))
            {
                return Json(Enumerable.Empty<FindMetricResultDTO>());
            }

            var query = _db.Metrics.AsQueryable();

            if (data.ResultsTypeID.HasValue)
            {
                query = query.Where(q => q.ResultsTypeID == data.ResultsTypeID.Value);
            }

            if(data.Domains != null && data.Domains.Any())
            {
                query = query.Where(m => m.Domains.Any(d => data.Domains.Contains(d.DomainID)));
            }

            if(data.FrameworkCategories != null && data.FrameworkCategories.Any())
            {
                query = query.Where(m => m.FrameworkCategories.Any(f => data.FrameworkCategories.Contains(f.DataQualityFrameworkCategoryID)));
            }

            if (User.Identity.IsAuthenticated)
            {
                var statusIDs = new[] { MetricStatus.PublishedID, MetricStatus.PublishedRequiresAuthenticationID };
                query = query.Where(m => statusIDs.Contains(m.Statuses.OrderByDescending(s => s.CreateOn).FirstOrDefault().MetricStatusID));
            }
            else
            {
                query = query.Where(m => m.Statuses.OrderByDescending(s => s.CreateOn).FirstOrDefault().MetricStatusID == MetricStatus.PublishedID);
            }

            var result = await (query.OrderBy(m => m.Title)
                .Select(m => new FindMetricResultDTO {
                    ID = m.ID,
                    Title = m.Title,
                    Description = m.Description,
                    ResultsType = m.ResultsType,
                    Domains = m.Domains.Select(d => d.Domain),
                    FrameworkCategories = m.FrameworkCategories.Select(f =>  f.DataQualityFrameworkCategory)
                })).ToArrayAsync();

            return Json(result);
        }

        public class FindMetricResultDTO
        {
            public Guid ID { get; set; }

            public string Title { get; set; }

            public string Description { get; set; }

            public Model.MetricResultsType ResultsType { get; set; }

            public IEnumerable<Model.Domain> Domains { get; set; }

            public IEnumerable<Model.DataQualityFrameworkCategory> FrameworkCategories { get; set; }
        }

        

        [Authorize, HttpPost("copy/{sourceMetricID}")]
        public async Task<IActionResult> Copy([FromRoute] Guid sourceMetricID)
        {
            var sourceMetric = await _db.Metrics
                .Include(m => m.Domains)
                .Include(m => m.FrameworkCategories)
                .Where(m => m.ID == sourceMetricID).FirstOrDefaultAsync();

            if (sourceMetric == null)
                return NotFound();

            //make sure only the author or a system administrator is saving metric, and they have the author metric claim
            if (!User.HasClaim(cl => cl.Type == Identity.Claims.AuthorMetric_Key))
            {
                return BadRequest("The current user does not have permission to author a metric.");
            }

            var currentUserID = Guid.Parse(User.Claims.Where(x => x.Type == Identity.Claims.UserID_Key).Select(x => x.Value).FirstOrDefault());

            var metric = new Model.Metric
            {
                AuthorID = currentUserID,
                Description = sourceMetric.Description,
                Justification = sourceMetric.Justification,
                ResultsTypeID = sourceMetric.ResultsTypeID,
                Title = sourceMetric.Title + " (Copy)"
            };

            if (sourceMetric.Domains.Any())
            {
                metric.AddDomains(sourceMetric.Domains.Select(d => d.DomainID).ToArray());
            }

            if (sourceMetric.FrameworkCategories.Any())
            {
                metric.AddFrameworkCategories(sourceMetric.FrameworkCategories.Select(f => f.DataQualityFrameworkCategoryID).ToArray());
            }

            metric.Statuses.Add(new Model.MetricStatusItem { MetricID = metric.ID, MetricStatusID = Model.MetricStatus.DraftID, UserID = currentUserID, Note = $"Copied from metric: \"{ sourceMetric.Title }\" [id: { sourceMetric.ID.ToString("D") }]" });

            _db.Metrics.Add(metric);

            await _db.SaveChangesAsync();

            return Ok(metric.ID);
        }

        [Authorize, HttpPost("delete/{metricID}")]
        public async Task<IActionResult> Delete([FromRoute] Guid metricID)
        {
            var metric = await _db.Metrics.Where(m => m.ID == metricID).FirstOrDefaultAsync();

            if(metric == null)
            {
                return NotFound();
            }

            var currentUserID = Guid.Parse(User.Claims.Where(x => x.Type == Identity.Claims.UserID_Key).Select(x => x.Value).FirstOrDefault());

            if(currentUserID != metric.AuthorID)
            {
                return BadRequest("Only the author of a metric can delete the metric.");
            }

            var documents = await _db.Documents.Where(d => d.ItemID == metricID).ToArrayAsync();
            if (documents.Any())
            {
                _db.RemoveRange(documents);
            }

            var userFavorites = await _db.UserFavoriteMetrics.Where(d => d.MetricID == metricID).ToArrayAsync();
            if (userFavorites.Any())
            {
                _db.RemoveRange(userFavorites);
            }

            _db.Remove(metric);

            await _db.SaveChangesAsync();

            if (documents.Any())
            {
                using(var scope = _serviceProvider.CreateScope())
                {
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    var fileService = (Files.IFileService)_serviceProvider.GetRequiredService(Type.GetType(config["Files:Type"]));

                    foreach (var document in documents)
                    {
                        await fileService.DeleteFileChunksAsync(document.ID);
                    }
                }
                
            }

            return Ok();
        }

        [HttpGet("download-measure-template")]
        public async Task<IActionResult> DownloadMeasureTemplate(Guid metricID, string format)
        {
            var metric = await _db.Metrics.Where(m => m.ID == metricID).Select(m => new { m.Title, ResultsType = m.ResultsType.Value }).FirstOrDefaultAsync();

            if(metric == null)
            {
                return NotFound();
            }

            if(string.Equals("json", format, StringComparison.OrdinalIgnoreCase))
            {
                var measure = new Models.MeasureSubmissionViewModel {
                    MetricID = metricID,
                    ResultsType = metric.ResultsType,
                    Measures = new HashSet<Models.MeasurementSubmissionViewModel> { new Models.MeasurementSubmissionViewModel { Definition = string.Empty, RawValue = string.Empty, Measure = 0, Total = 0 } }
                };

                var serializerSettings = new Newtonsoft.Json.JsonSerializerSettings { Formatting = Formatting.Indented, DateFormatString="'yyyy-MM-dd'" };
                var result = new FileContentResult(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(measure, serializerSettings)), Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json"))
                {
                    FileDownloadName = CleanFilename("MeasureSubmission_" + metric.Title) + ".json"
                };

                return result;
            }
            else if(string.Equals("xlsx", format, StringComparison.OrdinalIgnoreCase))
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var hostingEnvironment = scope.ServiceProvider.GetService<IWebHostEnvironment>();
                    
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();

                    using (var fs = new System.IO.FileStream(System.IO.Path.Combine(hostingEnvironment.WebRootPath, "assets", "MeasureSubmission_template.xlsx"), System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        await fs.CopyToAsync(ms);
                        await fs.FlushAsync();
                    }

                    ms.Position = 0;

                    using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(ms, true))
                    {
                        var helper = new Utils.MeasuresExcelReader(document);
                        helper.UpdateMetricInformation(metricID, metric.ResultsType);
                        helper.Document.Save();
                        document.Close();
                    }

                    ms.Position = 0;

                    var result = new FileStreamResult(ms, Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                    {
                        FileDownloadName = CleanFilename("MeasureSubmission_" + metric.Title + ".xlsx")
                    };

                    return result;
                    
                }
            }

            return BadRequest("Invalid document format, must be either 'json' or 'xlsx'.");
        }

        [HttpPost("bookmark/{id}")]
        public async Task<IActionResult> Bookmark([FromRoute] Guid id)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return Unauthorized();
            }

            if (!await _db.Metrics.AnyAsync(m => m.ID == id))
            {
                return NotFound();
            }

            Guid userID = Guid.Parse(User.Claims.Where(cl => cl.Type == Identity.Claims.UserID_Key).Select(cl => cl.Value).FirstOrDefault());
            var existingBookmark = await _db.UserFavoriteMetrics.FindAsync(userID, id);
            if (existingBookmark != null)
            {
                //delete to remove the bookmark
                _db.UserFavoriteMetrics.Remove(existingBookmark);
            }
            else
            {
                //add to bookmark the visualization for the user
                _db.UserFavoriteMetrics.Add(new UserMetricFavorite
                {
                    UserID = userID,
                    MetricID = id
                });
            }

            await _db.SaveChangesAsync();

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

            var query = from f in _db.UserFavoriteMetrics
                        join m in _db.Metrics on f.MetricID equals m.ID
                        join statusItem in _db.MetricStatusItems on m.ID equals statusItem.MetricID
                        join status in _db.MetricStatuses on statusItem.MetricStatusID equals status.ID
                        where f.UserID == userID
                        && statusItem == m.Statuses.OrderByDescending(s => s.CreateOn).FirstOrDefault()
                        && status.ID != MetricStatus.DeletedID
                        && (
                            //the metric is available to anyone
                            ((status.Access & MetricStatusAccess.Public) == MetricStatusAccess.Public)
                            //the metric is available to the author
                            || (f.Metric.AuthorID == userID && (status.Access & MetricStatusAccess.Author) == MetricStatusAccess.Author)
                            //the metric is available to authenticated users
                            || (userID != Guid.Empty && (status.Access & MetricStatusAccess.AuthenticatedUser) == MetricStatusAccess.AuthenticatedUser)
                        )
                        orderby f.CreatedOn descending
                        select new { f.Metric.Title, f.MetricID };

            return new JsonResult(await query.ToArrayAsync());
        }


        static readonly System.Text.RegularExpressions.Regex filenameRegex = new System.Text.RegularExpressions.Regex($"[{ System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()) + new string(System.IO.Path.GetInvalidPathChars())) }]");
       
        static string CleanFilename(string filename)
        {
            filename = filename.Replace(' ', '_');
            return filenameRegex.Replace(filename, "");
        }

    }
}