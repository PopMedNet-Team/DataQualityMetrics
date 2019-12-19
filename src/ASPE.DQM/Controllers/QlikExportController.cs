using ASPE.DQM.Model;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ASPE.DQM.Controllers
{
    /// <summary>
    /// Route containing actions to support exporting metric and measure information to Qlik.
    /// </summary>
    [Route("api/qlik-export")]
    [ApiController]
    public class QlikExportController : Controller
    {
        readonly ModelDataContext _modelDB;
        readonly IConfiguration _config;
        readonly ILogger<QlikExportController> _logger;

        public QlikExportController(ModelDataContext modelDB, IConfiguration config, IServiceProvider serviceProvider, ILogger<QlikExportController> logger)
        {
            _modelDB = modelDB;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Lists all the Metrics in the system regardless of status, includes the current status only. The results will be ordered by Metric Title ascending.
        /// The requestor must have the System Administrator claim granted. 
        /// </summary>
        /// <returns></returns>
        [HttpGet("metrics"),
         Utils.Authorization.RequestHeaderBasicAuthorizationAttribute]
        public async Task<IActionResult> Metrics()
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to export metric details."));

            if (!User.Claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to export metric details."));
            }

            var q = from m in _modelDB.Metrics
                    let currentStatus = m.Statuses.OrderByDescending(s => s.CreateOn).FirstOrDefault()
                    orderby m.Title
                    select new MetricDetail {
                        ID = m.ID,
                        Title = m.Title,
                        AuthorID = m.AuthorID,
                        Author = m.Author.LastName + ", " + m.Author.FirstName,
                        CreatedOn = m.CreatedOn,
                        Description = m.Description ?? string.Empty,
                        Justification = m.Justification ?? string.Empty,
                        ExpectedResults = m.ExpectedResults ?? string.Empty,
                        ModifiedOn = m.ModifiedOn,
                        ResultsTypeID = m.ResultsTypeID,
                        ResultsType = m.ResultsTypeID != null ? m.ResultsType.Value : string.Empty,
                        ServiceDeskUrl = m.ServiceDeskUrl ?? string.Empty,
                        MeasuresCount = _modelDB.MeasurementMeta.Where(mm => mm.MetricID == m.ID).Count(),
                        Status = new CurrentStatus {
                            ID = currentStatus.ID,
                            CreatedOn = currentStatus.CreateOn,
                            StatusID = currentStatus.MetricStatusID,
                            Status = currentStatus.MetricStatus.Title,
                            UserID = currentStatus.UserID,
                            User = currentStatus.User.LastName + ", " + currentStatus.User.FirstName
                        },
                        DataQualityHarmonizationCategories = _modelDB.DataQualityFrameworkCategories.Where(fc => m.FrameworkCategories.Any(mfc => mfc.DataQualityFrameworkCategoryID == fc.ID)).OrderBy(fc => fc.Title).ThenBy(fc => fc.SubCategory).Select(fc => new DataQualityHarmonizationCategoryTitle { ID = fc.ID, Title = fc.Title, SubCategory = fc.SubCategory }),
                        Domains = m.Domains.Select(d => new ItemTitle { ID = d.DomainID, Title = d.Domain.Title }).OrderBy(d => d.Title),
                        Documents = _modelDB.Documents.Where(doc => doc.ItemID == m.ID).OrderBy(doc => doc.Name).Select(doc => new DocumentDTO {
                            ID = doc.ID,
                            CreatedOn = doc.CreatedOn,
                            FileName = doc.FileName,
                            Length = doc.Length,
                            MimeType = doc.MimeType,
                            UploadedByID = doc.UploadedByID,
                            UploadedByUserName = doc.UploadedBy.UserName,
                            UploadedBy = doc.UploadedBy.LastName + ", " + doc.UploadedBy.FirstName
                        })
                    };            

            return Ok(await q.ToArrayAsync());
        }

        /// <summary>
        /// Lists all Measures and their metadata grouped by the Associated Metric. Each root metric must have one or more Measures to be included in the results. The results will be ordered by Metric Title ascending, and then by Measure Run Date ascending.
        /// The requestor must have the System Administrator claim granted.
        /// </summary>
        /// <returns></returns>
        [HttpGet("measures-by-metric"),
         Utils.Authorization.RequestHeaderBasicAuthorizationAttribute]
        public async Task<IActionResult> MeasuresByMetric()
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to export measure details."));

            if (!User.Claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to export measure details."));
            }

            var query = await (from meta in _modelDB.MeasurementMeta
                        join m in _modelDB.Measurements on meta.ID equals m.MetadataID
                        let suspendUser = meta.SuspendedBy
                        let submitter = meta.SubmittedBy
                        orderby meta.Metric.Title, meta.RunDate
                        select new MeasurementFlatDTO
                        {
                            MetricID = meta.MetricID,
                            MetricTitle = meta.Metric.Title,
                            MetaDataID = meta.ID,
                            OrganizationID = meta.OrganizationID,
                            Organization = meta.Organization,
                            DataSourceID = meta.DataSourceID,
                            DataSource = meta.DataSource,
                            RunDate = meta.RunDate,
                            Network = meta.Network,
                            CommonDataModel = meta.CommonDataModel,
                            CommonDataModelVersion = meta.CommonDataModelVersion,
                            DatabaseSystem = meta.DatabaseSystem,
                            DateRangeStart = meta.DateRangeStart,
                            DateRangeEnd = meta.DateRangeEnd,
                            ResultsTypeID = meta.ResultsTypeID,
                            ResultsType = meta.Metric.ResultsType.Value,
                            SuspendedByID = meta.SuspendedByID,
                            SuspendedBy = meta.SuspendedByID.HasValue ? suspendUser.LastName + ", " + suspendUser.FirstName : "",
                            SuspendedOn = meta.SuspendedOn,
                            SubmittedByID = meta.SubmittedByID,
                            SubmittedBy = submitter.LastName + ", " + submitter.FirstName,
                            SubmittedOn = meta.SubmittedOn,
                            ResultsDelimiter = meta.ResultsDelimiter,
                            SupportingResources = meta.SupportingResources,
                            Definition = m.Definition,
                            RawValue = m.RawValue,
                            Measure = m.Measure,
                            Total = m.Total
                        }).ToArrayAsync();

            var output = query.GroupBy(k => new { k.MetricID, k.MetricTitle })
                .Select(k => new
                {
                    k.Key.MetricID,
                    k.Key.MetricTitle,
                    Measurements = k.GroupBy(j => new
                    {
                        j.MetaDataID,
                        j.OrganizationID,
                        j.Organization,
                        j.DataSourceID,
                        j.DataSource,
                        j.RunDate,
                        j.Network,
                        j.CommonDataModel,
                        j.CommonDataModelVersion,
                        j.DatabaseSystem,
                        j.DateRangeStart,
                        j.DateRangeEnd,
                        j.ResultsTypeID,
                        j.ResultsType,
                        j.SuspendedByID,
                        j.SuspendedBy,
                        j.SuspendedOn,
                        j.SubmittedByID,
                        j.SubmittedBy,
                        j.SubmittedOn,
                        j.ResultsDelimiter,
                        j.SupportingResources
                    })
                    .Select(l => new MeasureMetadataDTO
                    {
                        MetaDataID = l.Key.MetaDataID,
                        OrganizationID = l.Key.OrganizationID,
                        Organization = l.Key.Organization,
                        DataSourceID = l.Key.DataSourceID,
                        DataSource = l.Key.DataSource,
                        RunDate = l.Key.RunDate,
                        Network = l.Key.Network ?? string.Empty,
                        CommonDataModel = l.Key.CommonDataModel ?? string.Empty,
                        CommonDataModelVersion = l.Key.CommonDataModelVersion ?? string.Empty,
                        DatabaseSystem = l.Key.DatabaseSystem ?? string.Empty,
                        DateRangeStart = l.Key.DateRangeStart,
                        DateRangeEnd = l.Key.DateRangeEnd,
                        ResultsTypeID = l.Key.ResultsTypeID,
                        ResultsType = l.Key.ResultsType,
                        SuspendedByID = l.Key.SuspendedByID,
                        SuspendedBy = l.Key.SuspendedBy,
                        SuspendedOn = l.Key.SuspendedOn,
                        SubmittedByID = l.Key.SubmittedByID,
                        SubmittedBy = l.Key.SubmittedBy,
                        SubmittedOn = l.Key.SubmittedOn,
                        ResultsDelimiter = l.Key.ResultsDelimiter ?? string.Empty,
                        SupportingResources = l.Key.SupportingResources ?? string.Empty,
                        Measures = l.Select(j => new MeasureDTO { RawValue = j.RawValue, Definition = j.Definition, Measure = j.Measure, Total = j.Total }).ToArray()
                    })
                }).ToArray();

            return Ok(output);
        }        

        /// <summary>
        /// Returns all Data Quality Harmonization Categories titles, ordered by the Title, and then by the SubCategory ascending.
        /// The requestor must have the System Administrator claim granted.
        /// </summary>
        /// <returns></returns>
        [HttpGet("data-quality-harmonization-categories"),
         Utils.Authorization.RequestHeaderBasicAuthorizationAttribute]
        public async Task<IActionResult> DataQualityHarmonizationCategories()
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to list Data Quality Harmonization Categories."));

            var q = from hc in _modelDB.DataQualityFrameworkCategories
                    orderby hc.Title, hc.SubCategory
                    select new DataQualityHarmonizationCategoryTitle
                    {
                        ID = hc.ID,
                        Title = hc.Title,
                        SubCategory = hc.SubCategory
                    };

            return Ok(await q.ToArrayAsync());
        }

        /// <summary>
        /// Returns all Results Types titles, ordered by Title ascending.
        /// The requestor must have the System Administrator claim granted.
        /// </summary>
        /// <returns></returns>
        [HttpGet("results-types"),
         Utils.Authorization.RequestHeaderBasicAuthorizationAttribute]
        public async Task<IActionResult> ResultsTypes()
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to list Results Types."));

            var q = from rt in _modelDB.MetricResultTypes
                    orderby rt.Value
                    select new ItemTitle { ID = rt.ID, Title = rt.Value };

            return Ok(await q.ToArrayAsync());
        }

        /// <summary>
        /// Returns all Domain titles, ordered by Title ascending.
        /// The requestor must have the System Administrator claim granted.
        /// </summary>
        /// <returns></returns>
        [HttpGet("domains"),
         Utils.Authorization.RequestHeaderBasicAuthorizationAttribute]
        public async Task<IActionResult> Domains()
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to list Domains."));

            var q = from d in _modelDB.Domains
                    orderby d.Title
                    select new ItemTitle { ID = d.ID, Title = d.Title };

            return Ok(await q.ToArrayAsync());
        }

        /// <summary>
        /// Returns all Metric Status titles, ordered by Title ascending.
        /// The requestor must have the System Administrator claim granted.
        /// </summary>
        /// <returns></returns>
        [HttpGet("metric-statuses"),
         Utils.Authorization.RequestHeaderBasicAuthorizationAttribute]
        public async Task<IActionResult> MetricStatuses()
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to list Metric Statuses."));

            var q = from s in _modelDB.MetricStatuses
                    orderby s.Order, s.Title
                    select new ItemTitle { ID = s.ID, Title = s.Title };

            return Ok(await q.ToArrayAsync());
        }


        public class MetricDetail
        {
            public Guid ID { get; set; }

            public Guid AuthorID { get; set; }

            public string Author { get; set; }

            public string Title { get; set; }

            public string Description { get; set; }

            public string Description_PlainText { get { return CleanHtml(Description); } }

            public string Justification { get; set; }

            public string Justification_PlainText { get { return CleanHtml(Justification); } }

            public string ExpectedResults { get; set; }

            public string ExpectedResults_PlainText { get { return CleanHtml(ExpectedResults); } }

            public DateTime CreatedOn { get; set; }

            public DateTime ModifiedOn { get; set; }

            public string ServiceDeskUrl { get; set; }

            public Guid ResultsTypeID { get; set; }

            public string ResultsType { get; set; }

            public CurrentStatus Status { get; set; }

            public IEnumerable<DataQualityHarmonizationCategoryTitle> DataQualityHarmonizationCategories { get; set; }

            public IEnumerable<ItemTitle> Domains { get; set; }

            public IEnumerable<DocumentDTO> Documents { get; set; }

            public int MeasuresCount { get; set; }

            static string CleanHtml(string value)
            {
                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                if(value.IndexOf('<') < 0 && value.IndexOf('>') < 0)
                {
                    //most likely not html fragment
                    return value;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(value);

                return GetTextFromNodes(htmlDoc.DocumentNode.ChildNodes);
            }

            static string GetTextFromNodes(HtmlNodeCollection nodes, int indent = 0)
            {
                var texts = new StringBuilder();
                string[] linebreaks = { "p", "br", "table", "th", "tr" };
                string[] indentTag = { "ul", "li" };
                foreach (var node in nodes)
                {
                    if (node.Name.ToLowerInvariant() == "style")
                        continue;
                    if (node.HasChildNodes)
                    {
                        if (indentTag.Contains(node.Name.ToLowerInvariant()))
                            texts.Append(GetTextFromNodes(node.ChildNodes, indent + 1));
                        else
                            texts.Append(GetTextFromNodes(node.ChildNodes, indent));
                    }
                    else
                    {
                        var innerText = node.InnerText;
                        if (!string.IsNullOrWhiteSpace(innerText))
                        {
                            texts.Append(new String(' ', indent) + node.InnerText);
                        }
                    }

                    if (node.Name.ToLowerInvariant() == "a")
                        texts.Append("\r\n" + node.Attributes["href"].Value + "\r\n");
                    if (node.Name.ToLowerInvariant() == "img" && !node.Attributes["src"].Value.EndsWith("invis.gif"))
                        texts.Append("\r\n" + node.Attributes["src"].Value + "\r\n");
                    if (linebreaks.Contains(node.Name.ToLowerInvariant()))
                        texts.Append("\r\n");
                }

                string clean = texts.ToString();
                clean = clean.Replace("&nbsp;", " ");
                if(clean.EndsWith(Environment.NewLine, StringComparison.OrdinalIgnoreCase))
                {
                    //line break is treated as a single character...
                    clean = clean.Substring(0, clean.Length - 2);
                }


                return clean;
            }

        }

        public class ItemTitle
        {
            public Guid ID { get; set; }

            public string Title { get; set; }
        }

        public class DataQualityHarmonizationCategoryTitle
        {
            public Guid ID { get; set; }

            public string Title { get; set; }

            public string SubCategory { get; set; }
        }

        public class CurrentStatus
        {
            public Guid ID { get; set; }

            public Guid UserID { get; set; }

            public string User { get; set; }

            public DateTime CreatedOn { get; set; }

            public Guid StatusID { get; set; }

            public string Status { get; set; }
        }

        public class DocumentDTO
        {
            public Guid ID { get; set; }

            public string FileName { get; set; }

            public string MimeType { get; set; }

            public DateTime CreatedOn { get; set; }

            public long Length { get; set; }

            public Guid? UploadedByID { get; set; }

            public string UploadedByUserName { get; set; }

            public string UploadedBy { get; set; }
        }

        internal class MeasurementFlatDTO
        {
            public Guid MetricID { get; set; }
            public string MetricTitle { get; set; }
            public Guid MetaDataID { get; set; }
            public Guid? OrganizationID { get; set; }
            public string Organization { get; set; }
            public Guid? DataSourceID { get; set; }
            public string DataSource { get; set; }
            public DateTime RunDate { get; set; }
            public string Network { get; set; }
            public string CommonDataModel { get; set; }
            public string CommonDataModelVersion { get; set; }
            public string DatabaseSystem { get; set; }
            public DateTime DateRangeStart { get; set; }
            public DateTime DateRangeEnd { get; set; }
            public Guid ResultsTypeID { get; set; }
            public string ResultsType { get; set; }
            public Guid? SuspendedByID { get; set; }
            public string SuspendedBy { get; set; }
            public DateTime? SuspendedOn { get; set; }
            public Guid SubmittedByID { get; set; }
            public string SubmittedBy { get; set; }
            public DateTime SubmittedOn { get; set; }
            public string ResultsDelimiter { get; set; }
            public string SupportingResources { get; set; }
            public string Definition { get; set; }
            public string RawValue { get; set; }
            public float Measure { get; set; }
            public float? Total { get; set; }
        }


        public class MeasureMetadataDTO
        {
            public Guid MetaDataID { get; set; }

            public Guid? OrganizationID { get; set; }

            public string Organization { get; set; }

            public Guid? DataSourceID { get; set; }

            public string DataSource { get; set; }

            public DateTime RunDate { get; set; }

            public string Network { get; set; }

            public string CommonDataModel { get; set; }

            public string CommonDataModelVersion { get; set; }

            public string DatabaseSystem { get; set; }

            public DateTime DateRangeStart { get; set; }

            public DateTime DateRangeEnd { get; set; }

            public Guid ResultsTypeID { get; set; }

            public string ResultsType { get; set; }

            public Guid? SuspendedByID { get; set; }

            public string SuspendedBy { get; set; }

            public DateTime? SuspendedOn { get; set; }

            public Guid SubmittedByID { get; set; }

            public string SubmittedBy { get; set; }

            public DateTime SubmittedOn { get; set; }

            public string ResultsDelimiter { get; set; }

            public string SupportingResources { get; set; }

            public IEnumerable<MeasureDTO> Measures { get; set; }
        }

        public class MeasureDTO
        {
            public string RawValue { get; set; }

            public string Definition { get; set; }

            public float Measure { get; set; }

            public float? Total { get; set; }
        }

    }
}
