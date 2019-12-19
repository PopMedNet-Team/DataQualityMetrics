using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ASPE.DQM.Utils
{
    public class MeasuresExcelReader
    {
        public readonly SpreadsheetDocument Document;
        public readonly Worksheet MetadataWorksheet;
        public readonly Worksheet ValuesWorksheet;
        readonly SharedStringTablePart sharedStringTablePart;

        public MeasuresExcelReader(SpreadsheetDocument document)
        {
            Document = document;

            var sheetIDs = document.WorkbookPart.Workbook.Descendants<Sheet>().Select(s => s.Id).ToArray();

            MetadataWorksheet = ((WorksheetPart)document.WorkbookPart.GetPartById(sheetIDs[0].Value)).Worksheet;
            ValuesWorksheet = ((WorksheetPart)document.WorkbookPart.GetPartById(sheetIDs[1].Value)).Worksheet;

            sharedStringTablePart = document.WorkbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault();
        }


        public IEnumerable<Row> GetMetadataRows()
        {
            return MetadataWorksheet.Descendants<Row>();
        }

        public IEnumerable<Row> GetValuesRows()
        {
            return ValuesWorksheet.Descendants<Row>();
        }

        public void UpdateMetricInformation(Guid metricID, string resultsType)
        {
            foreach (var row in GetMetadataRows())
            {
                var cells = row.Descendants<Cell>().Take(2).ToArray();

                string metadataHeader = GetCellValue(cells[0]);
                if (metadataHeader.Contains("MetricID", StringComparison.OrdinalIgnoreCase))
                {
                    Cell cell;
                    if (cells.Length >= 2)
                    {
                        cell = cells[1];
                        cell.DataType = CellValues.String;
                    }
                    else
                    {
                        cell = new Cell() { DataType = CellValues.String };
                        row.AppendChild<Cell>(cell);
                    }

                    cell.CellValue = new CellValue(metricID.ToString("D"));
                }

                if(metadataHeader.Contains("Results Type", StringComparison.OrdinalIgnoreCase) || metadataHeader.Contains("ResultsType", StringComparison.OrdinalIgnoreCase))
                {
                    Cell cell;
                    if (cells.Length >= 2)
                    {
                        cell = cells[1];
                        cell.DataType = CellValues.String;
                    }
                    else
                    {
                        cell = new Cell() { DataType = CellValues.String };
                        row.AppendChild<Cell>(cell);
                    }

                    cell.CellValue = new CellValue(resultsType);
                }
            }

        }

        public Models.MeasureSubmissionViewModel Convert(IList<string> errors)
        {
            var measure = new Models.MeasureSubmissionViewModel();
            
            foreach (var row in GetMetadataRows())
            {
                var cells = row.Descendants<Cell>().ToArray();

                if (cells.Length == 0)
                    continue;

                //check if the first cell is in column A, if not skip
                string cellReference = cells[0].CellReference.Value;
                if (!cellReference.StartsWith("A", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string metadataHeader = GetCellValue(cells[0]);

                string value = string.Empty;

                if (cells.Length > 1)
                {
                    cellReference = cells[1].CellReference.Value;
                    if (cellReference.StartsWith("B", StringComparison.OrdinalIgnoreCase))
                    {
                        value = GetCellValue(cells[1]);
                    }
                }

                SetMeasureValue(measure, metadataHeader, value);                
            }
            
            var measurementRows = GetValuesRows().ToArray();
            if(measurementRows.Length > 1)
            {
                var measurements = new List<Models.MeasurementSubmissionViewModel>();

                //going to assume for a valid document that the first four cells of the first row will always have a value
                //assume that the cell reference for each header will be A1-D1.
                var headers = measurementRows.First().Descendants<Cell>().Take(4).Select(c => GetCellValue(c)).ToArray();

                foreach(var row in measurementRows.Skip(1))
                {
                    var cells = row.Descendants<Cell>().ToArray();
                    var measurement = new Models.MeasurementSubmissionViewModel();

                    foreach(var cell in cells)
                    {
                        switch (cell.CellReference.Value[0].ToStringEx().ToUpper())
                        {
                            case "A":
                                SetMeasurementValue(measurement, headers[0], GetCellValue(cell));
                                break;
                            case "B":
                                SetMeasurementValue(measurement, headers[1], GetCellValue(cell));
                                break;
                            case "C":
                                SetMeasurementValue(measurement, headers[2], GetCellValue(cell));
                                break;
                            case "D":
                                SetMeasurementValue(measurement, headers[3], GetCellValue(cell));
                                break;
                        }
                    }

                    if (measurement.IsNotNull())
                        measurements.Add(measurement);
                }

                measure.Measures = measurements;
            }            

            return measure;
        }

        void SetMeasureValue(Models.MeasureSubmissionViewModel measure, string property, object value)
        {
            if (value == null)
                return;

            Guid id;

            switch (property.TrimEnd(' ', '*').ToLower())
            {
                case "metricid":                    
                    if(Guid.TryParse(value.ToStringEx(), out id))
                    {
                        measure.MetricID = id;
                    }
                    break;
                case "organizationid":
                    if (Guid.TryParse(value.ToStringEx(), out id))
                    {
                        measure.OrganizationID = id;
                    }
                    break;
                case "organization":
                    measure.Organization = value.ToStringEx();
                    break;
                case "datasourceid":
                    if (Guid.TryParse(value.ToStringEx(), out id))
                    {
                        measure.DataSourceID = id;
                    }
                    break;
                case "datasource":
                    measure.DataSource = value.ToStringEx();
                    break;
                case "rundate":
                    measure.RunDate = ConvertToDate(value);
                    break;
                case "network":
                    measure.Network = value.ToStringEx();
                    break;
                case "resultstype":
                case "datatype":
                case "results type":
                case "data type":
                    measure.ResultsType = value.ToStringEx();
                    break;

            }

            if(property.StartsWith("common", StringComparison.OrdinalIgnoreCase))
            {
                if(property.EndsWith("version", StringComparison.OrdinalIgnoreCase))
                {
                    measure.CommonDataModelVersion = value.ToStringEx();
                }
                else
                {
                    measure.CommonDataModel = value.ToStringEx();
                }
            }
            if(property.Contains("Results Delimiter", StringComparison.OrdinalIgnoreCase))
            {
                measure.ResultsDelimiter = value.ToStringEx();
            }
            if(property.StartsWith("database", StringComparison.OrdinalIgnoreCase))
            {
                measure.DatabaseSystem = value.ToStringEx();
            }
            if(property.Contains("range start", StringComparison.OrdinalIgnoreCase))
            {
                measure.DateRangeStart = ConvertToDate(value);
            }
            if(property.Contains("range end", StringComparison.OrdinalIgnoreCase))
            {
                measure.DateRangeEnd = ConvertToDate(value);
            }
            if(property.Contains("supporting resources", StringComparison.OrdinalIgnoreCase))
            {
                measure.SupportingResources = value.ToStringEx();
            }
        }

        void SetMeasurementValue(Models.MeasurementSubmissionViewModel measurement, string property, object value)
        {
            if (value == null)
                return;

            float num;

            switch (property.TrimEnd(' ', '*').ToLower())
            {
                case "rawvalue":
                case "raw value":
                    measurement.RawValue = value.ToStringEx();
                    break;
                case "definition":
                    measurement.Definition = value.ToStringEx();
                    break;
                case "measure":
                    if(float.TryParse(value.ToStringEx(), out num))
                    {
                        measurement.Measure = num;
                    }
                    break;
                case "total":
                    if(float.TryParse(value.ToStringEx(), out num))
                    {
                        measurement.Total = num;
                    }
                    break;                
            }
        }


        public string GetCellValue(Cell cell)
        {
            if (cell == null)
                return string.Empty;

            if (cell.DataType != null)
            {
                switch (cell.DataType.Value)
                {
                    case CellValues.SharedString:
                        if (sharedStringTablePart != null)
                        { 
                            return sharedStringTablePart.SharedStringTable.ElementAt(int.Parse(cell.InnerText)).InnerText;
                        }

                        break;
                    case CellValues.Boolean:
                        return cell.InnerText == "0" ? bool.FalseString : bool.TrueString;
                }
            }

            return cell.InnerText;
        }

        static DateTime? ConvertToDate(object value)
        {
            DateTime dt;
            double v;
            if (double.TryParse(value.ToStringEx(), out v))
            {
                return DateTime.FromOADate(v);
            }
            else if (DateTime.TryParse(value.ToStringEx(), out dt))
            {
                return dt;
            }

            return null;
        }

        
    }
}
