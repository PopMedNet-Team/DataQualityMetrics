using ASPE.DQM.Files;
using ASPE.DQM.Model;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Authorization;
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
using System.Threading.Tasks;

namespace ASPE.DQM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]    
    public class MeasuresController : Controller
    {
        readonly ModelDataContext _modelDB;
        readonly IConfiguration _config;
        readonly IFileService _fileService;
        readonly ILogger<MeasuresController> _logger;

        public MeasuresController(ModelDataContext modelDB, IConfiguration config, IServiceProvider serviceProvider, ILogger<MeasuresController> logger)
        {
            _modelDB = modelDB;
            _config = config;
            _fileService = (IFileService)serviceProvider.GetRequiredService(Type.GetType(config["Files:Type"]));
            _logger = logger;
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(bool forAdmin = false)
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to view measures."));

            if (forAdmin)
            {
                if(!User.Claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key))
                {
                    return BadRequest(new Models.ApiErrorResult("User does not have permission to view all submitted Measures."));
                }
            }

            var query = from m in _modelDB.MeasurementMeta
                        let metric = m.Metric
                        orderby m.SubmittedOn descending
                        select new
                        {
                            m.ID,
                            m.SubmittedByID,
                            SubmittedBy = m.SubmittedBy.UserName,
                            m.SubmittedOn,
                            m.SuspendedByID,
                            m.SuspendedOn,
                            m.Organization,
                            m.OrganizationID,
                            m.DataSource,
                            m.DataSourceID,
                            m.RunDate,
                            m.DateRangeStart,
                            m.DateRangeEnd,
                            m.CommonDataModel,
                            m.DatabaseSystem,
                            m.Network,
                            m.MetricID,
                            MetricTitle = metric.Title,
                            m.ResultsTypeID,
                            ResultsType = metric.ResultsType.Value,
                            m.CommonDataModelVersion,
                            m.ResultsDelimiter,
                            m.SupportingResources,
                            MeasureCount = m.Measurements.Count()
                        };

            if (!forAdmin)
            {
                Guid userID = Guid.Parse(User.Claims.Where(cl => cl.Type == Identity.Claims.UserID_Key).Select(cl => cl.Value).FirstOrDefault());
                query = query.Where(m => m.SubmittedByID == userID && m.SuspendedByID.HasValue == false);
            }

            return Ok(new Models.ApiResult<IEnumerable<object>>(await query.ToArrayAsync()));
        }

        [HttpPost("submit"),
         Utils.Authorization.RequestHeaderBasicAuthorizationAttribute]
        public async Task<IActionResult> Submit([FromBody] Models.MeasureSubmissionViewModel data)
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to view measures."));

            if (data == null)
            {
                return BadRequest(new Models.ApiErrorResult("Invalid measure data."));
            }

            if (!User.Claims.Any(cl => cl.Type == Identity.Claims.SubmitMeasure_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to submit measures."));
            }

            List<string> errors = new List<string>();
            if(!ValidateSubmission(data, errors))
            {
                return BadRequest(new { errors });
            }            

            var user = await _modelDB.Users.FindAsync(Guid.Parse(User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).Select(x => x.Value).FirstOrDefault()));

            MeasurementMeta measure = new MeasurementMeta
            {
                CommonDataModel = data.CommonDataModel,
                DatabaseSystem = data.DatabaseSystem,
                DataSource = data.DataSource,
                DataSourceID = data.DataSourceID,
                DateRangeEnd = data.DateRangeEnd.Value,
                DateRangeStart = data.DateRangeStart.Value,
                MetricID = data.MetricID.Value,
                Network = data.Network,
                Organization = data.Organization,
                OrganizationID = data.OrganizationID,
                RunDate = data.RunDate.Value,
                SubmittedByID = user.ID,
                SubmittedOn = DateTime.UtcNow,
                CommonDataModelVersion = data.CommonDataModelVersion,
                ResultsDelimiter = data.ResultsDelimiter,
                ResultsTypeID = await _modelDB.MetricResultTypes.Where(rt => rt.Metrics.Any(m => m.ID == data.MetricID.Value)).Select(rt => rt.ID).FirstOrDefaultAsync(),
                SupportingResources = data.SupportingResources
            };

            measure.Measurements = new HashSet<Measurement>(data.Measures.Select(i => new Measurement { MetadataID = measure.ID, RawValue = i.RawValue, Definition = string.IsNullOrEmpty(i.Definition) ? i.RawValue : i.Definition, Measure = i.Measure.HasValue ? i.Measure.Value : 0f, Total = i.Total }).ToArray());

            _modelDB.MeasurementMeta.Add(measure);

            await _modelDB.SaveChangesAsync();

            return Ok();
        }


        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IEnumerable<IFormFile> files, [FromForm] string metaData)
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to view measures."));

            if (!Request.ContentType.Contains("multipart/form-data"))
            {
                return BadRequest(new Models.ApiErrorResult("Content must be mime multipart."));
            }

            if(!User.Claims.Any(cl => cl.Type == Identity.Claims.SubmitMeasure_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to submit measures."));
            }

            ChunkMetaData metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<ChunkMetaData>(Request.Form["metadata"]);

            if(!metadata.FileExtension.EndsWith("xlsx", StringComparison.OrdinalIgnoreCase) && !metadata.FileExtension.EndsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new Models.ApiErrorResult("Only Excel and json files are valid."));
            }


            var user = await _modelDB.Users.FindAsync(Guid.Parse(User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).Select(x => x.Value).FirstOrDefault()));

            await _fileService.WriteToStreamAsync(files.FirstOrDefault(), metadata);

            if (!metadata.IsFinalChunck)
            {
                return Ok(new UploadResult(metadata.UploadUid, metadata.IsFinalChunck));
            }


            List<string> errors = new List<string>();
            string metricName = null;
            Guid? metricID = null;
            try
            {
                DQM.Models.MeasureSubmissionViewModel measure = null;

                if(metadata.FileExtension.EndsWith("xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    using(var stream = _fileService.ReturnTempFileStream(metadata.UploadUid))
                    using (var document = SpreadsheetDocument.Open( stream, false))
                    {
                        var reader = new ASPE.DQM.Utils.MeasuresExcelReader(document);
                        measure = reader.Convert(errors);                        

                        document.Close();
                    }

                    //Can delete the excel file regardless of validation, will be saved as json if successfull
                    await _fileService.DeleteTempFileChunkAsync(metadata.UploadUid);

                    if(errors.Count > 0)
                    {
                        return BadRequest(new UploadResult(metadata.UploadUid, true, errors.ToArray()));
                    }

                    //validate the submission.
                    if (ValidateSubmission(measure, errors) == false)
                    {
                        return BadRequest(new UploadResult(metadata.UploadUid, true, errors.ToArray()));
                    }

                    //save as json if valid
                    using (var ms = new System.IO.MemoryStream())
                    {
                        using (var sw = new System.IO.StreamWriter(ms, System.Text.Encoding.UTF8, 1024, true))
                        using (var jw = new Newtonsoft.Json.JsonTextWriter(sw))
                        {
                            var serializerSettings = new Newtonsoft.Json.JsonSerializerSettings { Formatting = Newtonsoft.Json.Formatting.None, DateFormatString = "'yyyy-MM-dd'" };
                            var serializer = new Newtonsoft.Json.JsonSerializer();
                            serializer.DateFormatString = "yyyy'-'MM'-'dd";
                            serializer.Formatting = Newtonsoft.Json.Formatting.None;

                            serializer.Serialize(jw, measure);
                            await jw.FlushAsync();
                        }

                        ms.Seek(0, System.IO.SeekOrigin.Begin);
                        //ms.Position = 0;

                        await _fileService.WriteToStreamAsync(metadata.UploadUid, 0, ms);
                    }

                }
                else
                {
                    //assume json file
                    using(var stream = _fileService.ReturnTempFileStream(metadata.UploadUid))
                    using (var sr = new System.IO.StreamReader(stream))
                    using( var jr = new Newtonsoft.Json.JsonTextReader(sr))
                    {
                        var serializer = new Newtonsoft.Json.JsonSerializer();
                        serializer.DateFormatString = "yyyy'-'MM'-'dd";
                        measure = serializer.Deserialize<Models.MeasureSubmissionViewModel>(jr);
                    }

                    //validate the submission.
                    if (ValidateSubmission(measure, errors) == false)
                    {
                        //upload is invalid, delete the temp file
                        await _fileService.DeleteTempFileChunkAsync(metadata.UploadUid);

                        return BadRequest(new UploadResult(metadata.UploadUid, true, errors.ToArray()));
                    }
                }

                metricName = await _modelDB.Metrics.Where(m => m.ID == measure.MetricID.Value).Select(m => m.Title).FirstOrDefaultAsync();
                metricID = measure.MetricID;

            }catch(Exception ex)
            {
                _logger.LogError(ex, "Error validating pending measure upload.");
                errors.Add(ex.Message);
            }

            if(errors.Count > 0)
            {
                return BadRequest(new UploadResult(metadata.UploadUid, true, errors.ToArray()) { metricID = metricID, metricName = metricName });
            }

            return Ok(new UploadResult(metadata.UploadUid, true, metricID, metricName));
        }

        bool ValidateSubmission(Models.MeasureSubmissionViewModel measure, IList<string> errors)
        {
            if (!measure.MetricID.HasValue)
            {
                errors.Add("Unable to determine MetricID.");
                return false;
            }
            else
            {
                if (!_modelDB.Metrics.Any(m => m.ID == measure.MetricID.Value)){
                    errors.Add("Invalid MetricID, metric not found.");
                    return false;
                }
            }

            if(string.IsNullOrEmpty(measure.Organization))
            {
                errors.Add("Missing the Organization name.");
            }

            if (string.IsNullOrEmpty(measure.DataSource))
            {
                errors.Add("Missing the DataSource name.");
            }

            if (!measure.RunDate.HasValue)
            {
                errors.Add("Missing the Run Date.");
            }

            if (!measure.DateRangeStart.HasValue)
            {
                errors.Add("Missing the Date Range Start value.");
            }

            if (!measure.DateRangeStart.HasValue)
            {
                errors.Add("Missing the Date Range End value.");
            }

            if(measure.DateRangeStart.HasValue && measure.DateRangeEnd.HasValue && measure.DateRangeStart.Value > measure.DateRangeEnd.Value)
            {
                errors.Add("The Date Range Start value should occure before the Date Range End value.");
            }

            if (string.IsNullOrEmpty(measure.ResultsType))
            {
                errors.Add("Missing the Results Type value.");
            }
            else
            {
                var validResultType = _modelDB.MetricResultTypes.Where(rt => rt.Metrics.Any(m => m.ID == measure.MetricID.Value)).Select(rt => rt.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(validResultType))
                {
                    errors.Add("Unable to determine the Results Type for the specified metric.");
                }
                else if (!validResultType.Equals(measure.ResultsType, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Invalid Results Type value. The expected value of '" + validResultType + "' was expected for the specified Metric.");
                }
            }

            if(measure.Measures == null || measure.Measures.Any() == false)
            {
                errors.Add("At least one measurement is required.");
                return false;
            }

            if(measure.Measures.Any(m => string.IsNullOrEmpty(m.RawValue)))
            {
                errors.Add("All measurements require a value for Raw Value.");
            }

            if (measure.Measures.Any(m => m.Measure.HasValue == false))
            {
                errors.Add("All measurements require a value for Measure.");
            }

            return errors.Count == 0;
        }

        [HttpPatch("accept/{fileUid}")]
        public async Task<IActionResult> AcceptMeasure([FromRoute]string fileUid)
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult( "User must be authenticated to view measures."));

            if (!User.Claims.Any(cl => cl.Type == Identity.Claims.SubmitMeasure_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to submit measures."));
            }

            try
            {
                var user = await _modelDB.Users.FindAsync(Guid.Parse(User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).Select(x => x.Value).FirstOrDefault()));

                Models.MeasureSubmissionViewModel import;
                using (var stream = _fileService.ReturnTempFileStream(fileUid))
                using (var sr = new System.IO.StreamReader(stream))
                using (var jr = new Newtonsoft.Json.JsonTextReader(sr))
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer();
                    serializer.DateFormatString = "yyyy'-'MM'-'dd";
                    import = serializer.Deserialize<Models.MeasureSubmissionViewModel>(jr);
                }

                MeasurementMeta measure = new MeasurementMeta {
                    CommonDataModel = import.CommonDataModel,
                    DatabaseSystem = import.DatabaseSystem,
                    DataSource = import.DataSource,
                    DataSourceID = import.DataSourceID,
                    DateRangeEnd = import.DateRangeEnd.Value,
                    DateRangeStart = import.DateRangeStart.Value,
                    MetricID = import.MetricID.Value,
                    Network = import.Network,
                    Organization = import.Organization,
                    OrganizationID = import.OrganizationID,
                    RunDate = import.RunDate.Value,
                    SubmittedByID = user.ID,
                    SubmittedOn = DateTime.UtcNow,
                    CommonDataModelVersion = import.CommonDataModelVersion,
                    ResultsDelimiter = import.ResultsDelimiter,
                    ResultsTypeID = await _modelDB.MetricResultTypes.Where(rt => rt.Metrics.Any(m => m.ID == import.MetricID.Value)).Select(rt => rt.ID).FirstOrDefaultAsync(),
                    SupportingResources = import.SupportingResources
                };

                measure.Measurements = new HashSet<Measurement>(import.Measures.Select(i => new Measurement { MetadataID = measure.ID, RawValue = i.RawValue, Definition = string.IsNullOrEmpty(i.Definition) ? i.RawValue : i.Definition, Measure = i.Measure.HasValue ? i.Measure.Value : 0f, Total = i.Total }).ToArray());

                _modelDB.MeasurementMeta.Add(measure);

                await _modelDB.SaveChangesAsync();                

                
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "There was an error procesing the Measure upload with fileUid of:" + fileUid);
                BadRequest(new Models.ApiErrorResult("There was an error processing the Measure."));
            }
            finally
            {
                try
                {
                    await _fileService.DeleteTempFileChunkAsync(fileUid);
                }
                catch(Exception ex)
                {
                    //do not throw since either the save of the measure failed which will require a reupload anyhow, or the cleanup failed and the service will handle in a couple of hours.
                    _logger.LogError(ex, "There was an error deleting the temp file for the Measure upload with fileUid of:" + fileUid);
                }
            }

            return Ok();
        }

        [HttpDelete("reject/{fileUid}")]
        public async Task<IActionResult> RejectMeasure([FromRoute]string fileUid)
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to view measures."));

            if (!User.Claims.Any(cl => cl.Type == Identity.Claims.SubmitMeasure_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to submit measures."));
            }

            try
            {
                //Can delete the excel file regardless of validation, will be saved as json if successfull
                await _fileService.DeleteTempFileChunkAsync(fileUid);
            }
            catch (Exception ex)
            {
                //do not need to notify the user since the temp file will get cleaned up by the background service anyhow.
                _logger.LogError(ex, "There was an rejecting the Measure upload with fileUid of:" + fileUid);
            }

            return Ok();
        }

        [HttpPut("toggle/{id}")]
        public async Task<IActionResult> Toggle([FromRoute] Guid id)
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to toggle a measure."));

            if (!User.Claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to toggle a measure."));
            }

            try
            {
                var measureMeta = await _modelDB.MeasurementMeta.FindAsync(id);
                if(measureMeta == null)
                {
                    return NotFound();
                }


                if (measureMeta.SuspendedByID.HasValue)
                {
                    measureMeta.SuspendedByID = null;
                    measureMeta.SuspendedOn = null;
                }
                else
                {
                    measureMeta.SuspendedOn = DateTime.UtcNow;
                    measureMeta.SuspendedByID = Guid.Parse(User.Claims.First(cl => cl.Type == Identity.Claims.UserID_Key).Value);                    
                }

                await _modelDB.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                //do not need to notify the user since the temp file will get cleaned up by the background service anyhow.
                _logger.LogError(ex, "There was an toggling the suspended values for measure metadata:" + id.ToString("D"));
            }

            return Ok();
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            if (User.Identity.IsAuthenticated == false)
                return BadRequest(new Models.ApiErrorResult("User must be authenticated to toggle a measure."));

            if (!User.Claims.Any(cl => cl.Type == Identity.Claims.SystemAdministrator_Key))
            {
                return BadRequest(new Models.ApiErrorResult("The user does not have permission to delete a measure."));
            }

            try
            {
                var measureMeta = await _modelDB.MeasurementMeta.FindAsync(id);
                if (measureMeta == null)
                {
                    return NotFound();
                }


                if (measureMeta.SuspendedByID.HasValue)
                {
                    measureMeta.SuspendedByID = null;
                    measureMeta.SuspendedOn = null;
                }
                else
                {
                    _modelDB.Measurements.RemoveRange(_modelDB.Measurements.Where(m => m.MetadataID == measureMeta.ID));
                    _modelDB.MeasurementMeta.Remove(measureMeta);

                    await _modelDB.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {
                //do not need to notify the user since the temp file will get cleaned up by the background service anyhow.
                _logger.LogError(ex, "There was an deleting the Measure upload with ID of:" + id.ToString("D"));
            }

            return Ok();
        }

    }

    public class UploadResult : Models.ApiResult<object>
    {
        public UploadResult(string fileUid, bool uploaded, params string[] errors) : base(errors) {
            this.fileUid = fileUid;
            this.uploaded = uploaded;
        }

        public UploadResult(string fileUid, bool uploaded, Guid? metricID, string metricName)
        {
            this.fileUid = fileUid;
            this.uploaded = uploaded;
            this.metricID = metricID;
            this.metricName = metricName;
        }

        public string fileUid { get; set; }

        public bool uploaded { get; set; }

        public Guid? metricID { get; set; }

        public string metricName { get; set; }
    }
}
