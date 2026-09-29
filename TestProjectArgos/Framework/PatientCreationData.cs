namespace SimulatorAutomation.Tests.Framework
{
    public class PatientCreationData
    {
        public int ExcelRowNumber { get; set; }

        public string PatientLastName { get; set; } =
            string.Empty;

        public string PatientFirstName { get; set; } =
            string.Empty;

        public string PatientDateOfBirth { get; set; } =
            string.Empty;

        public string PatientID { get; set; } =
            string.Empty;

        public string PatientGender { get; set; } =
            string.Empty;

        public override string ToString()
        {
            return
                "Row " +
                ExcelRowNumber +
                " - " +
                PatientID;
        }
    }
}