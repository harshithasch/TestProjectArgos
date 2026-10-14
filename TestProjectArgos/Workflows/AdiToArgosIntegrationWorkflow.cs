using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProjectArgos.Actions.AdiPlanner;
using TestProjectArgos.Models;

namespace TestProjectArgos.Workflows
{
    public class AdiToArgosIntegrationWorkflow
    {
        private readonly PatientCreationWorkflowFactory _patientFactory;
        private readonly MeasurementCaptureWorkflow _measurementWorkflow;
        private readonly AdiExportVerificationActions _adiVerificationActions;

        public AdiToArgosIntegrationWorkflow(
            PatientCreationWorkflowFactory patientFactory,
            MeasurementCaptureWorkflow measurementWorkflow,
            AdiExportVerificationActions adiVerificationActions)
        {
            _patientFactory = patientFactory;
            _measurementWorkflow = measurementWorkflow;
            _adiVerificationActions = adiVerificationActions;
        }

        public void Execute(WorkflowContext context)
        {
            Assert.IsNotNull(context);
            Assert.IsNotNull(context.Patient);

            context.WorkflowStartedAt = System.DateTime.Now;

            // Step 1 and 2:
            // Create patient and order measurement based on source.
            IPatientCreationWorkflow patientWorkflow =
                _patientFactory.GetWorkflow(context.CreationSource);

            patientWorkflow.CreatePatient(context);

            // Step 3 to 8:
            // Launch MOVU, launch VP, login, search patient,
            // capture measurement and export.
            _measurementWorkflow.Execute(context);

            // Step 9:
            // Verify exported measurement in ADI Planner.
            if (context.CreationSource == PatientCreationSource.AdiPlanner)
            {
                VerifyMeasurementInAdiPlanner(context);
            }
        }

        private void VerifyMeasurementInAdiPlanner(
            WorkflowContext context)
        {
            bool measurementAvailable =
                _adiVerificationActions.VerifyMeasurementAvailable(
                    context.Patient.Mrn,
                    context.Patient.Eye);

            Assert.IsTrue(
                measurementAvailable,
                $"Exported measurement was not found in ADI Planner " +
                $"for MRN '{context.Patient.Mrn}'.");

            context.ExportVerifiedInAdiPlanner = true;
        }
    }
}