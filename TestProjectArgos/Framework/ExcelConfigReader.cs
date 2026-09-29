using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using ClosedXML.Excel;
using NUnit.Framework;

namespace SimulatorAutomation.Tests.Framework
{
    public static class ExcelConfigReader
    {
        private const string ConfigurationSheetName = "Configuration";
        private const string PatientCreationSheetName = "Patientcreation";
        private const string CaptureMeasurementSheetName = "Capturemeasurement";
        private const string WorkbookFileName = "AutomationConfig.xlsx";

        public static string GetWorkbookPath()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    string path = Path.Combine(directory.FullName, "Test data", WorkbookFileName);
                    if (!File.Exists(path))
                    {
                        path = Path.Combine(directory.FullName, "TestData", WorkbookFileName);
                    }

                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException(
                            "AutomationConfig.xlsx was not found under Test data or TestData.",
                            path);
                    }

                    TestContext.Progress.WriteLine("SOURCE Excel workbook path: " + path);
                    return path;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Unable to locate the project directory containing the .csproj file.");
        }

        public static AutomationConfig Load()
        {
            string path = GetWorkbookPath();
            Dictionary<string, string> values = ReadKeyValueSheet(path, ConfigurationSheetName);

            AutomationConfig config = new AutomationConfig
            {
                MovuShortcutPath = GetRequiredValue(values, "MovuShortcutPath", ConfigurationSheetName),
                VisionPlannerShortcutPath = GetRequiredValue(values, "VisionPlannerShortcutPath", ConfigurationSheetName),
                VisionPlannerLoginTileName = GetRequiredValue(values, "VisionPlannerLoginTileName", ConfigurationSheetName),
                VisionPlannerUserName = GetRequiredValue(values, "VisionPlannerUserName", ConfigurationSheetName),
                PasswordEnvironmentVariable = GetRequiredValue(values, "PasswordEnvironmentVariable", ConfigurationSheetName)
            };

            ValidateConfiguration(config);
            return config;
        }

        public static List<PatientCreationData> LoadAllPatientCreationRows()
        {
            string path = GetWorkbookPath();
            List<PatientCreationData> rows = new List<PatientCreationData>();

            using (XLWorkbook workbook = new XLWorkbook(path))
            {
                IXLWorksheet sheet = GetRequiredWorksheet(workbook, PatientCreationSheetName);
                Dictionary<string, int> columns = GetHeaderColumns(sheet);
                int lastRow = sheet.LastRowUsed() == null ? 1 : sheet.LastRowUsed().RowNumber();

                int lastNameColumn = GetRequiredColumn(columns, "PatientLastName");
                int firstNameColumn = GetRequiredColumn(columns, "PatientFirstName");
                int dobColumn = GetRequiredColumn(columns, "PatientDateOfBirth");
                int idColumn = GetRequiredColumn(columns, "PatientID");
                int genderColumn = GetRequiredColumn(columns, "PatientGender");

                for (int rowNumber = 2; rowNumber <= lastRow; rowNumber++)
                {
                    string lastName = ReadCellAsText(sheet, rowNumber, lastNameColumn);
                    string firstName = ReadCellAsText(sheet, rowNumber, firstNameColumn);
                    string dob = ReadDateCellAsText(sheet, rowNumber, dobColumn);
                    string id = ReadCellAsText(sheet, rowNumber, idColumn);
                    string gender = ReadCellAsText(sheet, rowNumber, genderColumn);

                    if (string.IsNullOrWhiteSpace(lastName) &&
                        string.IsNullOrWhiteSpace(firstName) &&
                        string.IsNullOrWhiteSpace(dob) &&
                        string.IsNullOrWhiteSpace(id) &&
                        string.IsNullOrWhiteSpace(gender))
                    {
                        continue;
                    }

                    rows.Add(new PatientCreationData
                    {
                        ExcelRowNumber = rowNumber,
                        PatientLastName = lastName,
                        PatientFirstName = firstName,
                        PatientDateOfBirth = dob,
                        PatientID = id,
                        PatientGender = gender
                    });
                }
            }

            if (rows.Count == 0)
            {
                throw new InvalidOperationException("No patient-creation rows were found.");
            }

            return rows;
        }

        public static List<CaptureMeasurementData> LoadAllCaptureMeasurementRows()
        {
            string path = GetWorkbookPath();
            List<CaptureMeasurementData> rows = new List<CaptureMeasurementData>();

            using (XLWorkbook workbook = new XLWorkbook(path))
            {
                IXLWorksheet sheet = GetRequiredWorksheet(workbook, CaptureMeasurementSheetName);
                Dictionary<string, int> columns = GetHeaderColumns(sheet);
                int mrnColumn = GetRequiredColumn(columns, "Patientmrn");
                int lastRow = sheet.LastRowUsed() == null ? 1 : sheet.LastRowUsed().RowNumber();

                for (int rowNumber = 2; rowNumber <= lastRow; rowNumber++)
                {
                    string mrn = ReadCellAsText(sheet, rowNumber, mrnColumn);
                    if (string.IsNullOrWhiteSpace(mrn))
                    {
                        continue;
                    }

                    rows.Add(new CaptureMeasurementData
                    {
                        ExcelRowNumber = rowNumber,
                        PatientMrn = mrn
                    });
                }
            }

            if (rows.Count == 0)
            {
                throw new InvalidOperationException("No capture-measurement rows were found.");
            }

            return rows;
        }

        public static string LoadCaptureMeasurementPatientMrn()
        {
            return LoadAllCaptureMeasurementRows()[0].PatientMrn;
        }

        public static void UpdateTestResult(string sheetName, int excelRowNumber, string result)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                throw new ArgumentException("Worksheet name cannot be empty.", nameof(sheetName));
            }

            if (excelRowNumber < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(excelRowNumber));
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "FAILED: Result was empty";
            }

            string path = GetWorkbookPath();
            Exception lastException = null;

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using (XLWorkbook workbook = new XLWorkbook(path))
                    {
                        IXLWorksheet sheet = GetRequiredWorksheet(workbook, sheetName);
                        int resultColumn = FindColumnByHeader(sheet, "Result");

                        if (resultColumn <= 0)
                        {
                            throw new InvalidOperationException(
                                "The Result column was not found in worksheet '" + sheetName + "'.");
                        }

                        IXLCell cell = sheet.Cell(excelRowNumber, resultColumn);
                        cell.Value = result;
                        cell.Style.Font.Bold = true;
                        cell.Style.Alignment.WrapText = true;

                        if (result.StartsWith("PASSED", StringComparison.OrdinalIgnoreCase))
                        {
                            cell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                            cell.Style.Font.FontColor = XLColor.DarkGreen;
                        }
                        else
                        {
                            cell.Style.Fill.BackgroundColor = XLColor.LightPink;
                            cell.Style.Font.FontColor = XLColor.DarkRed;
                        }

                        sheet.Column(resultColumn).Width = 45;
                        workbook.Save();
                    }

                    string saved = ReadSavedResult(path, sheetName, excelRowNumber);
                    if (!string.Equals(saved, result, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Saved result verification failed. Expected '" + result +
                            "', actual '" + saved + "'.");
                    }

                    TestContext.Progress.WriteLine(
                        "Excel result saved and verified. Sheet: " + sheetName +
                        ", Row: " + excelRowNumber + ", Result: " + saved);
                    return;
                }
                catch (Exception exception)
                {
                    lastException = exception;
                    TestContext.Progress.WriteLine(
                        "Excel update attempt " + attempt + " failed: " + exception.Message);
                    Thread.Sleep(1000);
                }
            }

            throw new InvalidOperationException(
                "Unable to update Excel. Close AutomationConfig.xlsx and verify: " + path,
                lastException);
        }

        private static IXLWorksheet GetRequiredWorksheet(XLWorkbook workbook, string sheetName)
        {
            if (!workbook.Worksheets.Contains(sheetName))
            {
                throw new InvalidOperationException("Worksheet '" + sheetName + "' was not found.");
            }

            return workbook.Worksheet(sheetName);
        }

        private static int FindColumnByHeader(IXLWorksheet sheet, string expectedHeader)
        {
            int lastColumn = sheet.LastColumnUsed() == null
                ? 0
                : sheet.LastColumnUsed().ColumnNumber();

            for (int column = 1; column <= lastColumn; column++)
            {
                string header = sheet.Cell(1, column).GetString().Trim();
                if (string.Equals(header, expectedHeader, StringComparison.OrdinalIgnoreCase))
                {
                    return column;
                }
            }

            return -1;
        }

        private static string ReadSavedResult(
            string path,
            string sheetName,
            int rowNumber)
        {
            using (XLWorkbook workbook = new XLWorkbook(path))
            {
                IXLWorksheet sheet = GetRequiredWorksheet(workbook, sheetName);
                int resultColumn = FindColumnByHeader(sheet, "Result");

                if (resultColumn <= 0)
                {
                    throw new InvalidOperationException("Result column was not found.");
                }

                return sheet.Cell(rowNumber, resultColumn).GetString().Trim();
            }
        }

        private static Dictionary<string, int> GetHeaderColumns(IXLWorksheet sheet)
        {
            Dictionary<string, int> columns =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            int lastColumn = sheet.LastColumnUsed() == null
                ? 0
                : sheet.LastColumnUsed().ColumnNumber();

            for (int column = 1; column <= lastColumn; column++)
            {
                string header = sheet.Cell(1, column).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(header))
                {
                    columns[header] = column;
                }
            }

            return columns;
        }

        private static int GetRequiredColumn(Dictionary<string, int> columns, string header)
        {
            int column;
            if (!columns.TryGetValue(header, out column))
            {
                throw new InvalidOperationException(
                    "Required Excel column '" + header + "' was not found.");
            }

            return column;
        }

        private static string ReadCellAsText(
            IXLWorksheet sheet,
            int row,
            int column)
        {
            return sheet.Cell(row, column).GetFormattedString().Trim();
        }

        private static string ReadDateCellAsText(
            IXLWorksheet sheet,
            int row,
            int column)
        {
            IXLCell cell = sheet.Cell(row, column);
            DateTime date;

            if (cell.TryGetValue<DateTime>(out date))
            {
                return date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            }

            return cell.GetFormattedString().Trim();
        }

        private static Dictionary<string, string> ReadKeyValueSheet(
            string path,
            string sheetName)
        {
            Dictionary<string, string> values =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (XLWorkbook workbook = new XLWorkbook(path))
            {
                IXLWorksheet sheet = GetRequiredWorksheet(workbook, sheetName);
                IXLRange range = sheet.RangeUsed();

                if (range == null)
                {
                    throw new InvalidOperationException("Worksheet '" + sheetName + "' is empty.");
                }

                foreach (IXLRangeRow row in range.RowsUsed())
                {
                    if (row.RowNumber() == 1)
                    {
                        continue;
                    }

                    string key = row.Cell(1).GetString().Trim();
                    string value = row.Cell(2).GetFormattedString().Trim();

                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        values[key] = value;
                    }
                }
            }

            return values;
        }

        private static string GetRequiredValue(
            Dictionary<string, string> values,
            string key,
            string sheetName)
        {
            string value;
            if (!values.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Required value '" + key + "' is missing in worksheet '" + sheetName + "'.");
            }

            return Environment.ExpandEnvironmentVariables(value.Trim());
        }

        private static void ValidateConfiguration(AutomationConfig config)
        {
            if (config == null)
            {
                throw new InvalidOperationException("Automation configuration is null.");
            }

            if (!File.Exists(config.MovuShortcutPath))
            {
                throw new FileNotFoundException(
                    "MOVU shortcut was not found at: " + config.MovuShortcutPath);
            }

            if (!File.Exists(config.VisionPlannerShortcutPath))
            {
                throw new FileNotFoundException(
                    "Vision Planner shortcut was not found at: " +
                    config.VisionPlannerShortcutPath);
            }
        }
    }
}
