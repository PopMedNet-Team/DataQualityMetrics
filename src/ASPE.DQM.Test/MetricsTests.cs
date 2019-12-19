using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using ASPE.DQM.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ASPE.DQM.Test
{
    [TestClass]
    public class MetricsTests
    {
        [TestMethod]
        public void CreateMetric()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<Model.ModelDataContext>())
            {
                var author = db.Users.FirstOrDefault(u => u.UserName == "dqm1");
                var metric = new Metric {
                    Author = author,
                    AuthorID = author.ID,
                    Justification = "I need metrics to test with.",
                    Title = "The Best ever metric in the world first",
                    Description = "This is a wickedly awesome description of a silly little metric that will cause everyone to have headaches.",
                    ExpectedResults = "This should have some type of results."
                };

                metric.ResultsTypeID = db.MetricResultTypes.Select(rt => rt.ID).First();
                metric.AddDomains(db.Domains.Where(d => d.Title == "Demographics" || d.Title == "Enrollment").ToArray());
                metric.AddFrameworkCategories(db.DataQualityFrameworkCategories.Where(f => f.Title == "Conformance").ToArray());

                metric.Statuses.Add(new MetricStatusItem { Metric = metric, MetricStatus = db.MetricStatuses.Where(s => s.Title == "Draft").First(), User = author });

                db.Metrics.Add(metric);

                db.SaveChanges();
            }
        }


        [TestMethod]
        public async Task GetMetrics()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<Model.ModelDataContext>())
            using (var identityDb = scoped.ServiceProvider.GetRequiredService<Identity.IdentityContext>())
            {
                //TODO: userID will the ID of the authenticated user, else Guid.Empty for non-authenticated
                var userID = Guid.Empty;
                //var authorizationResult = await _authorizationService.AuthorizeAsync(User, Identity.Claims.SystemAdministrator_Key);
                bool isSystemAdministrator = await identityDb.UserClaims.Where(cl => cl.UserId == userID && cl.ClaimType == Identity.Claims.SystemAdministrator_Key).AnyAsync();

                var query = from m in db.Metrics
                            join statusItem in db.MetricStatusItems on m.ID equals statusItem.MetricID
                            join status in db.MetricStatuses on statusItem.MetricStatusID equals status.ID
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
                            select new { 
                                ID = m.ID,
                                Author = new { m.Author.ID, m.Author.UserName, m.Author.FirstName, m.Author.LastName },
                                ServiceDeskUrl = m.ServiceDeskUrl,
                                Title = m.Title,
                                Status = new { statusItem.ID, User = new { statusItem.User.ID, statusItem.User.UserName, statusItem.User.FirstName, statusItem.User.LastName }, statusItem.Note, statusItem.MetricStatusID, status.Title, statusItem.CreateOn },
                                Domains = m.Domains.OrderBy(d => d.Domain.Title).Select(d => new { d.Domain.ID, d.Domain.Title }),
                                FrameworkCategories = m.FrameworkCategories.OrderBy(f => f.DataQualityFrameworkCategory.Title).ThenBy(f => f.DataQualityFrameworkCategory.SubCategory).Select(f => new { f.DataQualityFrameworkCategory.ID, f.DataQualityFrameworkCategory.Title, f.DataQualityFrameworkCategory.SubCategory }),
                                ResultsType = new { m.ResultsType.ID, m.ResultsType.Value }
                            };

                foreach(var m in query)
                {
                    Console.WriteLine($"{m.Title}, {m.Status.Title} - {m.Status.CreateOn}");
                }
            }
        }

        [TestMethod]
        public void CheckFlag()
        {
            var current = Model.MetricStatusAccess.SystemAdministrator | MetricStatusAccess.Author;
            var metric = MetricStatusAccess.Author | MetricStatusAccess.Public;
            bool isSystemAdmin = (metric & current) == MetricStatusAccess.SystemAdministrator;

            Console.WriteLine(isSystemAdmin);

            Console.WriteLine(metric & current);
        }

        [TestMethod]
        public void ExportMetricDetails()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<Model.ModelDataContext>())
            {
                var q = from m in db.Metrics
                        let currentStatus = m.Statuses.OrderByDescending(s => s.CreateOn).FirstOrDefault()
                        orderby m.Title
                        select new
                        {
                            ID = m.ID,
                            Title = m.Title,
                            AuthorID = m.AuthorID,
                            Author = m.Author.FirstName + " " + m.Author.LastName,
                            CreatedOn = m.CreatedOn,
                            Description = m.Description,
                            Justification = m.Justification,
                            ExpectedResults = m.ExpectedResults,
                            ModifiedOn = m.ModifiedOn,
                            ResultsTypeID = m.ResultsTypeID,
                            ResultsType = m.ResultsType.Value,
                            ServiceDeskUrl = m.ServiceDeskUrl,
                            MeasuresCount = db.MeasurementMeta.Where(mm => mm.MetricID == m.ID).Count(),
                            Status = new
                            {
                                ID = currentStatus.ID,
                                CreatedOn = currentStatus.CreateOn,
                                StatusID = currentStatus.MetricStatusID,
                                Status = currentStatus.MetricStatus.Title,
                                UserID = currentStatus.UserID,
                                User = currentStatus.User.FirstName + " " + currentStatus.User.LastName
                            },
                            DataQualityHarmonizationCategories = db.DataQualityFrameworkCategories.Where(fc => m.FrameworkCategories.Any(mfc => mfc.DataQualityFrameworkCategoryID == fc.ID)).OrderBy(fc => fc.Title).ThenBy(fc => fc.SubCategory).Select(fc => new { ID = fc.ID, Title = fc.Title, SubCategory = fc.SubCategory }),
                            Domains = m.Domains.Select(d => new { ID = d.DomainID, Title = d.Domain.Title }).OrderBy(d => d.Title),
                            Documents = db.Documents.Where(doc => doc.ItemID == m.ID).OrderBy(doc => doc.Name).Select(doc => new
                            {
                                ID = doc.ID,
                                CreatedOn = doc.CreatedOn,
                                FileName = doc.FileName,
                                Length = doc.Length,
                                MimeType = doc.MimeType,
                                UploadedByID = doc.UploadedByID,
                                UploadedByUserName = doc.UploadedBy.UserName,
                                UploadedBy = doc.UploadedBy.FirstName + " " + doc.UploadedBy.LastName
                            })
                        };



                var result = q.ToArray();
            }
        }

        [TestMethod]
        public void ExportMeasuresByMetric()
        {
            var appFactory = new ApplicationFactory();

            using (var scoped = appFactory.CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<Model.ModelDataContext>())
            {
                //var q = from metric in db.Metrics
                //        select new
                //        {
                //            MetricID = metric.ID,
                //            MetricTitle = metric.Title,
                //            Measurements = metric.Measurements.Join(db.MetricResultTypes, mm => mm.ResultsTypeID, rt => rt.ID, (mm, rt) => new { Measurement = mm, ResultsType = rt })
                //            .Select(j => new {
                //                ID = j.Measurement.ID,
                //                OrganizationID = j.Measurement.OrganizationID,
                //                Organization = j.Measurement.Organization,
                //                DataSourceID = j.Measurement.DataSourceID,
                //                DataSource = j.Measurement.DataSource,
                //                RunDate = j.Measurement.RunDate,
                //                Network = j.Measurement.Network,
                //                CommonDataModel = j.Measurement.CommonDataModel,
                //                CommonDataModelVersion = j.Measurement.CommonDataModelVersion,
                //                DatabaseSystem = j.Measurement.DatabaseSystem,
                //                DateRangeStart = j.Measurement.DateRangeStart,
                //                DateRangeEnd = j.Measurement.DateRangeEnd,
                //                ResultsTypeID = j.Measurement.ResultsTypeID,
                //                ResultsType = j.ResultsType.Value,
                //                SuspendedByID = j.Measurement.SuspendedByID,
                //                SuspendedBy = j.Measurement.SuspendedBy.FirstName + " " + j.Measurement.SuspendedBy.LastName,
                //                SuspendedOn = j.Measurement.SuspendedOn,
                //                SubmittedByID = j.Measurement.SubmittedByID,
                //                SubmittedBy = j.Measurement.SubmittedBy.FirstName + " " + j.Measurement.SubmittedBy.LastName,
                //                SubmittedOn = j.Measurement.SubmittedOn,
                //                EvaluatedVariableDataType = j.Measurement.EvaluatedVariableDataType,
                //                ResultsDelimiter = j.Measurement.ResultsDelimiter,
                //                Measures = j.Measurement.Measurements.Select(m => new { m.RawValue, m.Definition, m.Measure, m.Total })
                //            })
                //        };

                var q = (from metric in db.Metrics
                         join mm in db.MeasurementMeta on metric.ID equals mm.MetricID into measurements
                         from meta in measurements.DefaultIfEmpty()
                         select new
                         {
                             MetricID = metric.ID,
                             MetricTitle = metric.Title,
                             ResultsType = metric.ResultsType.Value,
                             Measurement = meta
                         }).GroupBy(k => new { k.MetricID, k.MetricTitle })
                        .Select(g => new
                        {
                            g.Key.MetricID,
                            g.Key.MetricTitle,
                            Measurements = g.Select(j => new
                            {
                                ID = j.Measurement.ID,
                                OrganizationID = j.Measurement.OrganizationID,
                                Organization = j.Measurement.Organization,
                                DataSourceID = j.Measurement.DataSourceID,
                                DataSource = j.Measurement.DataSource,
                                RunDate = j.Measurement.RunDate,
                                Network = j.Measurement.Network,
                                CommonDataModel = j.Measurement.CommonDataModel,
                                CommonDataModelVersion = j.Measurement.CommonDataModelVersion,
                                DatabaseSystem = j.Measurement.DatabaseSystem,
                                DateRangeStart = j.Measurement.DateRangeStart,
                                DateRangeEnd = j.Measurement.DateRangeEnd,
                                ResultsTypeID = j.Measurement.ResultsTypeID,
                                ResultsType = j.ResultsType,
                                SuspendedByID = j.Measurement.SuspendedByID,
                                SuspendedBy = j.Measurement.SuspendedBy.FirstName + " " + j.Measurement.SuspendedBy.LastName,
                                SuspendedOn = j.Measurement.SuspendedOn,
                                SubmittedByID = j.Measurement.SubmittedByID,
                                SubmittedBy = j.Measurement.SubmittedBy.FirstName + " " + j.Measurement.SubmittedBy.LastName,
                                SubmittedOn = j.Measurement.SubmittedOn,
                                ResultsDelimiter = j.Measurement.ResultsDelimiter,
                                j.Measurement.SupportingResources,
                                Measures = j.Measurement.Measurements.Select(m => new { m.RawValue, m.Definition, m.Measure, m.Total }).DefaultIfEmpty()
                            })

                        });

                            var result = q.ToArray();
            }
        }



    }
}
