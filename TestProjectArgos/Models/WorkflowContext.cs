using System;

namespace TestProjectArgos.Models
{
    public class WorkflowContext
    {
        public PatientData Patient { get; set; } = new();

        public PatientCreationSource CreationSource { get; set; }

        public DateTime WorkflowStartedAt { get; set; }

        public string WorkflowName { get; set; } = string.Empty;

        public bool PatientCreated { get; set; }

        public bool MeasurementOrdered { get; set; }

        public bool MovuLaunched { get; set; }

        public bool VisionPlannerLaunched { get; set; }

        public bool SurgeonLoggedIn { get; set; }

        public bool PatientFoundInVisionPlanner { get; set; }

        public bool MeasurementCaptured { get; set; }

        public bool MeasurementExported { get; set; }

        public bool ExportVerifiedInAdiPlanner { get; set; }
    }
}