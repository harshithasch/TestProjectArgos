namespace SimulatorAutomation.Tests.Framework
{
    public class CaptureMeasurementData
    {
        public int ExcelRowNumber { get; set; }

        public string PatientMrn { get; set; } =
            string.Empty;

        public override string ToString()
        {
            return
                "Row " +
                ExcelRowNumber +
                " - " +
                PatientMrn;
        }
    }
}