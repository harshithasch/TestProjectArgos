using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace SimulatorAutomation.Tests.Framework
{
    /// <summary>
    /// Reads patient data and configuration from Excel files.
    /// Converted from Python utils/excel_reader.py
    /// </summary>
    public static class ExcelDataReader
    {
        /// <summary>
        /// Reads configuration from Excel Config sheet.
        /// Expected columns: key, value
        /// </summary>
        public static Dictionary<string, string> ReadConfig(string excelPath)
        {
            var config = new Dictionary<string, string>();

            try
            {
                using (var workbook = new XLWorkbook(excelPath))
                {
                    var sheet = workbook.Worksheet("Config");
                    var rows = sheet.RangeUsed().RowsUsed();

                    foreach (var row in rows)
                    {
                        if (row.RowNumber() == 1) continue; // Skip header

                        string key = row.Cell(1).Value.ToString().Trim();
                        string value = row.Cell(2).Value.ToString().Trim();

                        config[key] = value;
                    }
                }

                TestContext.Progress.WriteLine($"Loaded {config.Count} config values from Excel");
                return config;
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine($"Error reading config from Excel: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Reads patient data from Excel Patients sheet.
        /// </summary>
        public static List<PatientDataFromExcel> ReadPatients(
    string excelPath)
        {
            var patients =
                new List<PatientDataFromExcel>();

            try
            {
                using var workbook =
                    new XLWorkbook(excelPath);

                IXLWorksheet sheet =
                    workbook.Worksheet("Patients");

                Dictionary<string, int> headerMap =
                    GetHeaderMap(sheet);

                LogHeaders(
                    "Patients",
                    headerMap);

                IXLRange usedRange =
                    sheet.RangeUsed();

                if (usedRange == null)
                {
                    TestContext.Progress.WriteLine(
                        "Patients sheet is empty.");

                    return patients;
                }

                foreach (IXLRangeRow row
                         in usedRange.RowsUsed().Skip(1))
                {
                    if (row.CellsUsed().All(
                        cell => cell.IsEmpty()))
                    {
                        continue;
                    }

                    var patient =
                        new PatientDataFromExcel
                        {
                            Run = GetCellValue(
                                row,
                                headerMap,
                                "run"),

                            FirstName = GetCellValue(
                                row,
                                headerMap,
                                "first_name"),

                            LastName = GetCellValue(
                                row,
                                headerMap,
                                "last_name"),

                            Gender = GetCellValue(
                                row,
                                headerMap,
                                "gender"),

                            DateOfBirth = GetCellValue(
                                row,
                                headerMap,
                                "dob"),

                            Mrn = GetCellValue(
                                row,
                                headerMap,
                                "mrn"),

                            DiagnosticCenter = GetCellValue(
                                row,
                                headerMap,
                                "diagnostic_center"),

                            Device = GetCellValue(
                                row,
                                headerMap,
                                "device"),
                        };

                    TestContext.Progress.WriteLine(
                        "Patient Excel row " +
                        row.RowNumber() +
                        ": Run='" +
                        patient.Run +
                        "', Name='" +
                        patient.FirstName +
                        " " +
                        patient.LastName +
                        "', MRN='" +
                        patient.Mrn +
                        "'");

                    patients.Add(patient);
                }

                TestContext.Progress.WriteLine(
                    "Loaded " +
                    patients.Count +
                    " patients from Excel.");

                return patients;
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Error reading patients from Excel: " +
                    exception.Message);

                throw;
            }
        }
        private static void LogHeaders(
    string sheetName,
    Dictionary<string, int> headerMap)
        {
            TestContext.Progress.WriteLine(
                sheetName +
                " headers detected: " +
                string.Join(
                    ", ",
                    headerMap.Keys));
        }

        /// <summary>
        /// Reads order measurement data from Excel.
        /// </summary>
        public static List<OrderMeasurementData>
    ReadOrderMeasurements(string excelPath)
        {
            var orders =
                new List<OrderMeasurementData>();

            try
            {
                using var workbook =
                    new XLWorkbook(excelPath);

                if (!workbook.TryGetWorksheet(
                        "Ordermeasurement",
                        out IXLWorksheet sheet))
                {
                    TestContext.Progress.WriteLine(
                        "Ordermeasurement sheet was not found.");

                    return orders;
                }

                Dictionary<string, int> headerMap =
                    GetHeaderMap(sheet);

                LogHeaders(
                    "Ordermeasurement",
                    headerMap);

                IXLRange usedRange =
                    sheet.RangeUsed();

                if (usedRange == null)
                {
                    TestContext.Progress.WriteLine(
                        "Ordermeasurement sheet is empty.");

                    return orders;
                }

                foreach (IXLRangeRow row
                         in usedRange.RowsUsed().Skip(1))
                {
                    if (row.CellsUsed().All(
                            cell => cell.IsEmpty()))
                    {
                        continue;
                    }

                    var order =
                        new OrderMeasurementData
                        {
                            Run = GetCellValue(
                                row,
                                headerMap,
                                "run"),

                            Mrn = GetCellValue(
                                row,
                                headerMap,
                                "mrn"),

                            Device = GetCellValue(
                                row,
                                headerMap,
                                "device"),

                            ScheduledDate = GetCellValue(
                                row,
                                headerMap,
                                "scheduled_date"),

                            ScheduledTime = GetCellValue(
                                row,
                                headerMap,
                                "scheduled_time")
                        };

                    TestContext.Progress.WriteLine(
                        "Order Excel row " +
                        row.RowNumber() +
                        ": Run='" +
                        order.Run +
                        "', MRN='" +
                        order.Mrn +
                        "', Device='" +
                        order.Device +
                        "', Date='" +
                        order.ScheduledDate +
                        "', Time='" +
                        order.ScheduledTime +
                        "'");

                    orders.Add(order);
                }

                TestContext.Progress.WriteLine(
                    "Loaded " +
                    orders.Count +
                    " order measurements from Excel.");

                return orders;
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Error reading order measurements: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Reads cataract plan data from Excel.
        /// </summary>
        public static List<CataractPlanData>
    ReadCataractPlans(
        string excelPath)
        {
            var plans =
                new List<CataractPlanData>();

            try
            {
                using var workbook =
                    new XLWorkbook(excelPath);

                if (!workbook.TryGetWorksheet(
                    "CataractPlan",
                    out IXLWorksheet sheet))
                {
                    TestContext.Progress.WriteLine(
                        "CataractPlan sheet was not found.");

                    return plans;
                }

                Dictionary<string, int> headerMap =
                    GetHeaderMap(sheet);

                LogHeaders(
                    "CataractPlan",
                    headerMap);

                IXLRange usedRange =
                    sheet.RangeUsed();

                if (usedRange == null)
                {
                    return plans;
                }

                foreach (IXLRangeRow row
                         in usedRange.RowsUsed().Skip(1))
                {
                    if (row.CellsUsed().All(
                        cell => cell.IsEmpty()))
                    {
                        continue;
                    }

                    var plan =
                        new CataractPlanData
                        {
                            Run = GetCellValue(
                                row,
                                headerMap,
                                "Run"),

                            Mrn = GetCellValue(
                                row,
                                headerMap,
                                "MRN"),

                            Eye = GetCellValue(
                                row,
                                headerMap,
                                "Eye_type"),

                            Surgeon = GetCellValue(
                                row,
                                headerMap,
                                "Operating_surgeon"),

                            Lens = GetCellValue(
                                row,
                                headerMap,
                                "Lens"),

                            SurgeryDate = GetCellValue(
                                row,
                                headerMap,
                                "Surgery_date"),

                            SurgeryCenter = GetCellValue(
                                row,
                                headerMap,
                                "Surgery_center")
                        };

                    TestContext.Progress.WriteLine(
                        "Cataract plan Excel row " +
                        row.RowNumber() +
                        ": Run='" +
                        plan.Run +
                        "', MRN='" +
                        plan.Mrn +
                        "', Eye='" +
                        plan.Eye +
                        "'");

                    plans.Add(plan);
                }

                TestContext.Progress.WriteLine(
                    "Loaded " +
                    plans.Count +
                    " cataract plans from Excel.");

                return plans;
            }
            catch (Exception exception)
            {
                TestContext.Progress.WriteLine(
                    "Error reading cataract plans: " +
                    exception.Message);

                throw;
            }
        }

        /// <summary>
        /// Helper to get cell value by column name (case-insensitive).
        /// </summary>
        private static Dictionary<string, int> GetHeaderMap(
     IXLWorksheet sheet)
        {
            var headerMap =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            IXLRow headerRow =
                sheet.Row(1);

            int lastColumnNumber =
                sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (int columnNumber = 1;
                 columnNumber <= lastColumnNumber;
                 columnNumber++)
            {
                string header =
                    headerRow
                        .Cell(columnNumber)
                        .GetString()
                        .Trim();

                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                headerMap[header] =
                    columnNumber;
            }

            return headerMap;
        }

        private static string GetCellValue(
            IXLRangeRow row,
            Dictionary<string, int> headerMap,
            string columnName)
        {
            if (row == null)
            {
                return string.Empty;
            }

            if (headerMap == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return string.Empty;
            }

            if (!headerMap.TryGetValue(
                columnName.Trim(),
                out int columnNumber))
            {
                TestContext.Progress.WriteLine(
                    "[EXCEL WARNING] Column was not found: " +
                    columnName);

                return string.Empty;
            }

            IXLCell cell =
                row.Cell(columnNumber);

            if (cell == null || cell.IsEmpty())
            {
                return string.Empty;
            }

            if (cell.DataType == XLDataType.DateTime)
            {
                return cell
                    .GetDateTime()
                    .ToString(
                        "MM/dd/yyyy",
                        CultureInfo.InvariantCulture);
            }

            return cell
                .GetFormattedString()
                .Trim();
        }
    }

    /// <summary>
    /// Patient data model from Excel.
    /// </summary>
    public class PatientDataFromExcel
    {
        public string Run { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public string DateOfBirth { get; set; }
        public string Mrn { get; set; }
        public string DiagnosticCenter { get; set; }

        // Measurement fields
        public string Device { get; set; }
        public string KIndex { get; set; }
        public string Units { get; set; }
        public string MeasurementDate { get; set; }
        public string MeasurementTime { get; set; }

        // OD fields
        public string OdK1R1 { get; set; }
        public string OdFlatAxis { get; set; }
        public string OdK2R2 { get; set; }
        public string OdSteepAxis { get; set; }
        public string OdLensState { get; set; }
        public string OdAl { get; set; }
        public string OdAcd { get; set; }
        public string OdLt { get; set; }
        public string OdAd { get; set; }
        public string OdCct { get; set; }
        public string OdWtw { get; set; }

        // OS fields
        public string OsK1R1 { get; set; }
        public string OsFlatAxis { get; set; }
        public string OsK2R2 { get; set; }
        public string OsSteepAxis { get; set; }
        public string OsLensState { get; set; }
        public string OsAl { get; set; }
        public string OsAcd { get; set; }
        public string OsLt { get; set; }
        public string OsAd { get; set; }
        public string OsCct { get; set; }
        public string OsWtw { get; set; }
    }

    /// <summary>
    /// Order measurement data model.
    /// </summary>
    public class OrderMeasurementData
    {
        public string Run { get; set; }
        public string Mrn { get; set; }
        public string Device { get; set; }
        public string ScheduledDate { get; set; }
        public string ScheduledTime { get; set; }
    }

    /// <summary>
    /// Cataract plan data model.
    /// </summary>
    public class CataractPlanData
    {
        public string Run { get; set; }
        public string Mrn { get; set; }
        public string Eye { get; set; }
        public string Surgeon { get; set; }
        public string Lens { get; set; }
        public string SurgeryDate { get; set; }
        public string SurgeryCenter { get; set; }
    }
}
