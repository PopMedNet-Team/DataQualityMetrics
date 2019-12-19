using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASPE.DQM.Test
{
    [TestClass]
    public class MeasureTemplatesTests
    {
        readonly string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Measures\MeasureSubmission_template.xlsx");
        readonly string outputPath = @"Z:\Shared\Adaptable";
        [TestMethod]
        public void ReadExcelTemplate()
        {
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(templatePath, false))
            {
                var reader = new ASPE.DQM.Utils.MeasuresExcelReader(doc);
                foreach(var row in reader.GetMetadataRows())
                {
                    Console.WriteLine("Row Index:" + row.RowIndex);

                    foreach (Cell cell in row.Descendants<Cell>())
                    {
                        Console.WriteLine("\t" + reader.GetCellValue(cell));
                    }

                }

            }
        }

        [TestMethod]
        public void UpdateInitialTemplateValues()
        {
            string outputDocumentPath = Path.Combine(outputPath, $"MeasureSubmission_template_{ DateTime.Now.Ticks }.xlsx");
            File.Copy(templatePath, outputDocumentPath);

            using (SpreadsheetDocument document = SpreadsheetDocument.Open(outputDocumentPath, true))
            {
                var reader = new ASPE.DQM.Utils.MeasuresExcelReader(document);
                foreach (var row in reader.GetMetadataRows())
                {
                    var cells = row.Descendants<Cell>().ToArray();

                    string metadataHeader = reader.GetCellValue(cells[0]);
                    if(metadataHeader.Contains("MetricID", StringComparison.OrdinalIgnoreCase))
                    {
                        Cell cell;
                        if(cells.Length >= 2)
                        {
                            cell = cells[1];
                            cell.DataType = CellValues.String;
                        }
                        else
                        {
                            cell = new Cell() { DataType = CellValues.String };
                            row.AppendChild<Cell>(cell);
                        }

                        cell.CellValue = new CellValue(Guid.NewGuid().ToString("D"));
                        break;
                    }
                }

                reader.MetadataWorksheet.Save();
                reader.Document.Save();
                document.Close();

                
            }
        }

        [TestMethod]
        public void UpdateTemplateUsingMemoryStream()
        {
            string outputDocumentPath = Path.Combine(outputPath, $"MeasureSubmission_template_{ DateTime.Now.Ticks }.xlsx");
            MemoryStream ms = new MemoryStream();

            using(var reader = File.OpenRead(templatePath))
            {
                reader.CopyTo(ms);
            }

            ms.Position = 0;

            using(var document = SpreadsheetDocument.Open(ms, true))
            {
                var reader = new ASPE.DQM.Utils.MeasuresExcelReader(document);
                reader.UpdateMetricInformation(Guid.NewGuid(), "Count");

                reader.Document.Save();
                document.Close();
            }

            ms.Position = 0;

            using(var writer = File.OpenWrite(outputDocumentPath))
            {
                ms.CopyTo(writer);
            }
        }

        [TestMethod]
        public async Task UploadJsonMeasure()
        {
            var app = new ApplicationFactory();

            using (var scope = app.CreateScope())
            using (var db = scope.ServiceProvider.GetRequiredService<Model.ModelDataContext>())
            {
                //get the first count metric
                var metric = db.Metrics.Include(m => m.ResultsType).Where(m => m.ResultsType.Value == "Count" && m.Statuses.OrderByDescending(s => s.CreateOn).First().MetricStatusID == Model.MetricStatus.PublishedID).FirstOrDefault();

                if(metric == null)
                {
                    Assert.Fail("There is no published metric to create measures for.");
                }

                var rnd = new Random();
                

                var measureMeta = new Models.MeasureSubmissionViewModel {
                    MetricID = metric.ID,
                    ResultsType = metric.ResultsType.Value,
                    DataSource = "Unit Test DataSource",
                    Organization = "Unit Test Organization",
                    Network = "Developerland",
                    RunDate = DateTime.Now.Date,
                    DateRangeStart = DateTime.Now.AddYears(Convert.ToInt32(-100 * rnd.NextDouble())).Date,
                    DateRangeEnd = DateTime.Now.AddMonths(Convert.ToInt32(-12 * rnd.NextDouble())).Date,
                    SupportingResources = "https://github.com/some-organization/repositoryurl"
                };

                List<Models.MeasurementSubmissionViewModel> measurements = new List<Models.MeasurementSubmissionViewModel>();
                int totalRows = Convert.ToInt32(20 * rnd.NextDouble()) + 1;
                for(int i = 0; i < totalRows; i++)
                {
                    measurements.Add(new Models.MeasurementSubmissionViewModel { RawValue = i.ToString(), Definition = i.ToString(), Measure = Convert.ToSingle(100 * rnd.NextDouble()) });
                }
                float sum = measurements.Sum(m => m.Measure.Value);
                foreach(var measure in measurements)
                {
                    measure.Total = sum;
                }

                measureMeta.Measures = measurements;

                using (var ms = new System.IO.MemoryStream())
                {
                    using (var sw = new System.IO.StreamWriter(ms, Encoding.Default, 1024, true))
                    using (var jw = new Newtonsoft.Json.JsonTextWriter(sw))
                    {
                        var serializer = new Newtonsoft.Json.JsonSerializer();
                        serializer.DateFormatString = "yyyy'-'MM'-'dd";
                        serializer.Formatting = Newtonsoft.Json.Formatting.None;

                        serializer.Serialize(jw, measureMeta);
                        await jw.FlushAsync();
                    }

                    ms.Seek(0, SeekOrigin.Begin);

                    using(var http = new System.Net.Http.HttpClient())
                    {
                        http.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
                        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes("dqm1:Password1!")));

                        try
                        {
                            var content = new System.Net.Http.StreamContent(ms);
                            content.Headers.Add("Content-Type", "application/json");

                            var result = await http.PostAsync("https://localhost:44317/api/measures/submit", content);

                            if (result.IsSuccessStatusCode)
                            {
                                var responseContent = await result.Content.ReadAsStringAsync();
                            }
                            else
                            {
                                string error = await result.Content.ReadAsStringAsync();
                                Assert.Fail(error);
                            }
                            

                        }
                        catch (System.Net.WebException webex)
                        {
                            using (var reader = new StreamReader(webex.Response.GetResponseStream()))
                            {
                                Assert.Fail(await reader.ReadToEndAsync());
                            }
                        }
                    }
                }                

            }
        }

        [TestMethod]
        public async Task UploadMeasureFile()
        {
            using (var http = new System.Net.Http.HttpClient())
            {
                http.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes("dqm1:Password1!")));

                try
                {
                    using (var stream = System.IO.File.OpenRead(@"C:\work\MeasureSubmission_This_is_the_Second_Best_Metric_ever_-_for_real....json"))
                    {
                        var content = new System.Net.Http.StreamContent(stream);
                        content.Headers.Add("Content-Type", "application/json");

                        var result = await http.PostAsync("https://localhost:44317/api/measures/submit", content);

                        if (!result.IsSuccessStatusCode)
                        {
                            string error = await result.Content.ReadAsStringAsync();
                            Assert.Fail(error);
                            System.Diagnostics.Debug.WriteLine(error);
                        }
                    }

                }
                catch (System.Net.WebException webex)
                {
                    using (var reader = new StreamReader(webex.Response.GetResponseStream()))
                    {
                        Assert.Fail(await reader.ReadToEndAsync());
                        System.Diagnostics.Debug.WriteLine(await reader.ReadToEndAsync());
                    }
                }
            }
        }

        [TestMethod]
        public void ConfirmSampleFiles()
        {
            string sampleFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Measures\HPHCI");
            string[] files = Directory.GetFiles(sampleFolder);

            foreach(var file in files)
            {
                Console.WriteLine("Reading file: " + file);
                using(SpreadsheetDocument document = SpreadsheetDocument.Open(file, false))
                {
                    var reader = new ASPE.DQM.Utils.MeasuresExcelReader(document);
                    foreach (var row in reader.GetMetadataRows())
                    {
                        var cells = row.Descendants<Cell>().ToArray();

                        if (cells.Length == 0)
                            continue;

                        //check if the first cell is in column A, if not skip
                        string cellReference = cells[0].CellReference.Value;
                        if(!cellReference.StartsWith("A", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string metadataHeader = reader.GetCellValue(cells[0]);
                        string value = string.Empty;
                        if (cells.Length > 1)
                        {
                            cellReference = cells[1].CellReference.Value;
                            if (cellReference.StartsWith("B", StringComparison.OrdinalIgnoreCase))
                            {
                                value = reader.GetCellValue(cells[1]);
                            }
                        }

                        Console.WriteLine($"{metadataHeader}: {value}");
                    }

                    List<string> errors = new List<string>();
                    var vm = reader.Convert(errors);

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(vm);
                    Console.WriteLine(json);

                   
                    document.Close();
                }
                Console.WriteLine(string.Empty);
            }
        }

        [TestMethod]
        public async Task UploadAndReadExcelFileFromAzureBlobStorage()
        {
            var app = new ApplicationFactory((IServiceCollection services) =>
            {
                services.Add(new ServiceDescriptor(typeof(Files.IFileService), typeof(Files.AzureBlobStorageFileService), ServiceLifetime.Transient));
            });

            string sampleFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Measures\HPHCI");
            string[] files = Directory.GetFiles(sampleFolder);

            using (var scope = app.CreateScope())
            {
                var service = (Files.IFileService)scope.ServiceProvider.GetService(typeof(Files.IFileService));
                string identifier = Guid.NewGuid().ToString("D");

                try
                {
                    string file = files.First();


                    FileInfo fi = new FileInfo(file);
                    using (var fs = fi.OpenRead())
                    {
                        Console.WriteLine($"Writing excel file: \"{ file }\" to Azure Blob Storage. Total Length:{ fi.Length }");
                        await service.WriteToStreamAsync(identifier, 0, fs);
                    }

                    List<string> errors = new List<string>();
                    DQM.Models.MeasureSubmissionViewModel measure = null;


                    OpenSettings openSettings = new OpenSettings { AutoSave = false };

                    Console.WriteLine($"Reading excel document: \"{ file }\" from Azure Blob Storage.");
                    using (var stream = service.ReturnTempFileStream(identifier))
                    using(var ms = new MemoryStream())
                    {
                        long bytesRead = 0;
                        do
                        {
                            byte[] buffer = new byte[32];
                            bytesRead = stream.Read(buffer, 0, 32);
                            if (bytesRead > 0)
                            {
                                ms.Write(buffer, 0, Convert.ToInt32(bytesRead));
                            }
                        } while (bytesRead > 0);

                        ms.Position = 0;

                        Console.WriteLine($"Total length of document reported by Azure Blob Storage:{ stream.Length }");
                        using (var document = SpreadsheetDocument.Open(ms, false, openSettings))
                        {
                            var reader = new ASPE.DQM.Utils.MeasuresExcelReader(document);
                            measure = reader.Convert(errors);

                            document.Close();
                        }

                        //string filename = Path.Combine(@"z:\shared\dqmtemp", identifier + ".xlsx");
                        //using (var tempFS = File.Open(filename, FileMode.OpenOrCreate))
                        //{
                        //    long bytesRead = 0;
                        //    do
                        //    {
                        //        byte[] buffer = new byte[32];
                        //        bytesRead = stream.Read(buffer, 0, 32);
                        //        if (bytesRead > 0)
                        //        {
                        //            tempFS.Write(buffer, 0, Convert.ToInt32(bytesRead));
                        //        }
                        //    } while (bytesRead > 0);

                        //    tempFS.Flush();

                        //    Console.WriteLine($"File size of downloaded file is:{ new FileInfo(filename).Length }");
                        //}

                    }

                    if(errors.Count > 0)
                    {
                        foreach(var error in errors)
                        {
                            Console.WriteLine(error);
                        }
                        Assert.Fail("There were errors reading the excel document.");
                    }

                }
                finally
                {
                    await service.DeleteTempFileChunkAsync(identifier);
                }
            }
        }

        [TestMethod]
        public async Task UploadAndReadChunkedExcelDocumentFromBlobStorage()
        {
            var app = new ApplicationFactory((IServiceCollection services) =>
            {
                services.Add(new ServiceDescriptor(typeof(Files.IFileService), typeof(Files.AzureBlobStorageFileService), ServiceLifetime.Transient));
            });

            string sampleFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Measures\HPHCI");
            string[] files = Directory.GetFiles(sampleFolder);

            using (var scope = app.CreateScope())
            {
                var service = (Files.IFileService)scope.ServiceProvider.GetService(typeof(Files.IFileService));
                string identifier = Guid.NewGuid().ToString("D");

                try
                {
                    string file = files.First();


                    FileInfo fi = new FileInfo(file);
                    using (var fs = fi.OpenRead())
                    {
                        int bytesToRead = Convert.ToInt32(fs.Length / 2);
                        var buffer = new byte[bytesToRead];
                        fs.Read(buffer, 0, bytesToRead);

                        var ms = new MemoryStream(buffer);
                        ms.Position = 0;

                        Console.WriteLine($"Writing excel file: \"{ file }\" to Azure Blob Storage. Total Length:{ fi.Length }. Chunk: 0, length:{ bytesToRead }");
                        await service.WriteToStreamAsync(identifier, 0, ms);

                        Console.WriteLine($"Writing excel file: \"{ file }\" to Azure Blob Storage. Total Length:{ fi.Length }. Chunk: 1, length:{ fs.Length - bytesToRead }");
                        await service.WriteToStreamAsync(identifier, 1, fs);

                    }


                    //Console.WriteLine($"Reading excel document: \"{ file }\" from Azure Blob Storage.");
                    //using (var stream = service.ReturnTempFileStream(identifier))
                    //{
                    //    string filename = Path.Combine(@"z:\shared\dqmtemp", identifier + ".xlsx");
                    //    using (var tempFS = File.Open(filename, FileMode.OpenOrCreate))
                    //    {
                    //        long bytesRead = 0;
                    //        do
                    //        {
                    //            byte[] buffer = new byte[32];
                    //            bytesRead = stream.Read(buffer, 0, 32);
                    //            if (bytesRead > 0)
                    //            {
                    //                tempFS.Write(buffer, 0, Convert.ToInt32(bytesRead));
                    //            }
                    //        } while (bytesRead > 0);

                    //        tempFS.Flush();

                    //        Console.WriteLine($"File size of downloaded file is:{ new FileInfo(filename).Length }");
                    //    }
                    //}


                    List<string> errors = new List<string>();
                    DQM.Models.MeasureSubmissionViewModel measure = null;


                    OpenSettings openSettings = new OpenSettings { AutoSave = false };

                    Console.WriteLine($"Reading excel document: \"{ file }\" from Azure Blob Storage.");
                    using (var stream = service.ReturnTempFileStream(identifier))
                    using (var ms = new MemoryStream())
                    {
                        //long bytesRead = 0;
                        //do
                        //{
                        //    byte[] buffer = new byte[32];
                        //    bytesRead = stream.Read(buffer, 0, 32);
                        //    if (bytesRead > 0)
                        //    {
                        //        ms.Write(buffer, 0, Convert.ToInt32(bytesRead));
                        //    }
                        //} while (bytesRead > 0);

                        //ms.Position = 0;

                        Console.WriteLine($"Total length of document reported by Azure Blob Storage:{ stream.Length }");
                        using (var document = SpreadsheetDocument.Open(stream, false, openSettings))
                        {
                            var reader = new ASPE.DQM.Utils.MeasuresExcelReader(document);
                            measure = reader.Convert(errors);

                            document.Close();
                        }

                    }
                }
                finally
                {
                    await service.DeleteTempFileChunkAsync(identifier);
                }


            }
        }

        [TestMethod]
        public async Task TestUploadingXLSXAndConvert()
        {

            var app = new ApplicationFactory((IServiceCollection services) =>
            {
                services.Add(new ServiceDescriptor(typeof(Files.IFileService), typeof(Files.AzureBlobStorageFileService), ServiceLifetime.Transient));
            });

            using (var scope = app.CreateScope())
            {
                string sampleFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Measures\HPHCI");
                string[] files = Directory.GetFiles(sampleFolder);

                var service = (Files.IFileService)scope.ServiceProvider.GetService(typeof(Files.IFileService));                

                foreach (var file in files)
                {
                    string identifier = Guid.NewGuid().ToString("D");
                    Console.WriteLine("Reading file: " + file);

                    using (var fs = new FileStream(file, FileMode.Open))
                    {
                        await service.WriteToStreamAsync(identifier, 0, fs);
                    }

                    List<string> errors = new List<string>();
                    try
                    {
                        DQM.Models.MeasureSubmissionViewModel measure = null;

                        using (var stream = service.ReturnTempFileStream(identifier))
                        using (var sr = new System.IO.StreamReader(stream))
                        using (var document = SpreadsheetDocument.Open(stream, false))
                        {
                            var reader = new ASPE.DQM.Utils.MeasuresExcelReader(document);
                            measure = reader.Convert(errors);

                            document.Close();
                        }

                        //Can delete the excel file regardless of validation, will be saved as json if successfull
                        await service.DeleteTempFileChunkAsync(identifier);

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

                            await service.WriteToStreamAsync(identifier, 0, ms);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed on file {file}, with error: {ex.Message}");
                        await service.DeleteTempFileChunkAsync(identifier);
                        //Assert.Fail();
                    }

                    try
                    {
                        Models.MeasureSubmissionViewModel import;

                        using (var stream = service.ReturnTempFileStream(identifier))
                        using (var sr = new System.IO.StreamReader(stream))
                        using (var jr = new Newtonsoft.Json.JsonTextReader(sr))
                        {
                            var serializer = new Newtonsoft.Json.JsonSerializer();
                            serializer.DateFormatString = "yyyy'-'MM'-'dd";
                            import = serializer.Deserialize<Models.MeasureSubmissionViewModel>(jr);
                        }

                        await service.DeleteTempFileChunkAsync(identifier);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed on file {file}, with error: {ex.Message}");
                        await service.DeleteTempFileChunkAsync(identifier);
                        //Assert.Fail();
                    }
                }
            }            
        }
    }
}
