namespace SimulatorAutomation.Tests.Framework
{
    public class AutomationConfig
    {
        public string MovuShortcutPath { get; set; }

        public string VisionPlannerShortcutPath { get; set; }

        public string VisionPlannerLoginTileName { get; set; }

        public string VisionPlannerUserName { get; set; }

        public string PasswordEnvironmentVariable { get; set; }

        /* Patient Creation Fields */

        public string PatientLastName { get; set; }
        public string PatientFirstName { get; set; }
        public string PatientDateOfBirthDay { get; set; }
        public string PatientDateOfBirthMonth { get; set; }
        public string PatientDateOfBirthYear { get; set; }
        public string PatientID { get; set; }
        public string PatientGender { get; set; }
        public string Patientmrn { get; set; }
    }
}