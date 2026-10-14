using System;
using System.Collections.Generic;
using System.Text;

namespace TestProjectArgos.Models
{
    public class PatientData
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Mrn { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public string Gender { get; set; } = string.Empty;

        public string Eye { get; set; } = "OU";

        public string DiagnosticFacility { get; set; } = string.Empty;

        public string MeasurementDevice { get; set; } = "ARGOS";

        public string FullName =>
            $"{FirstName} {LastName}".Trim();
    }
}
