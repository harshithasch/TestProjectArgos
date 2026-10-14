using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProjectArgos.Actions.MOVU;
using TestProjectArgos.Actions.VisionPlanner;
using TestProjectArgos.Models;

namespace TestProjectArgos.Workflows
{
    public class MeasurementCaptureWorkflow
    {
        private readonly MovuLaunchActions _movuLaunchActions;
        private readonly VisionPlannerLaunchActions _vpLaunchActions;
        private readonly SurgeonLoginActions _surgeonLoginActions;
        private readonly PatientSearchActions _patientSearchActions;
        private readonly MovuMeasurementActions _measurementActions;
        private readonly MeasurementExportActions _exportActions;

        public MeasurementCaptureWorkflow(
            MovuLaunchActions movuLaunchActions,
            VisionPlannerLaunchActions vpLaunchActions,
            SurgeonLoginActions surgeonLoginActions,
            PatientSearchActions patientSearchActions,
            MovuMeasurementActions measurementActions,
            MeasurementExportActions exportActions)
        {
            _movuLaunchActions = movuLaunchActions;
            _vpLaunchActions = vpLaunchActions;
            _surgeonLoginActions = surgeonLoginActions;
            _patientSearchActions = patientSearchActions;
            _measurementActions = measurementActions;
            _exportActions = exportActions;
        }

        public void Execute(WorkflowContext context)
        {
            Assert.IsTrue(
                context.PatientCreated,
                "Patient must be created before measurement capture.");

            if (context.CreationSource == PatientCreationSource.AdiPlanner)
            {
                Assert.IsTrue(
                    context.MeasurementOrdered,
                    "Measurements must be ordered in ADI Planner.");
            }

            LaunchApplications(context);

            LoginToVisionPlanner(context);

            SearchAndOpenPatient(context);

            CaptureMeasurements(context);

            ExportMeasurements(context);
        }

        private void LaunchApplications(WorkflowContext context)
        {
            _movuLaunchActions.Launch();
            _movuLaunchActions.WaitUntilReady();

            context.MovuLaunched = true;

            _vpLaunchActions.Launch();
            _vpLaunchActions.WaitUntilReady();

            context.VisionPlannerLaunched = true;
        }

        private void LoginToVisionPlanner(WorkflowContext context)
        {
            _surgeonLoginActions.Login();

            context.SurgeonLoggedIn = true;
        }

        private void SearchAndOpenPatient(WorkflowContext context)
        {
            bool patientFound = _patientSearchActions.SearchByMrn(
                context.Patient.Mrn);

            Assert.IsTrue(
                patientFound,
                $"Patient with MRN '{context.Patient.Mrn}' was not found.");

            _patientSearchActions.OpenPatient(
                context.Patient.Mrn);

            _patientSearchActions.StartNewImageGuidedMeasurement();

            context.PatientFoundInVisionPlanner = true;
        }

        private void CaptureMeasurements(WorkflowContext context)
        {
            switch (context.Patient.Eye.ToUpperInvariant())
            {
                case "OD":
                    _measurementActions.CaptureOd();
                    break;

                case "OS":
                    _measurementActions.CaptureOs();
                    break;

                case "OU":
                    _measurementActions.CaptureOd();
                    _measurementActions.CaptureOs();
                    break;

                default:
                    Assert.Fail(
                        $"Unsupported eye value: {context.Patient.Eye}");
                    break;
            }

            _measurementActions.ClickFinish();
            _measurementActions.ConfirmFinish();

            context.MeasurementCaptured = true;
        }

        private void ExportMeasurements(WorkflowContext context)
        {
            _exportActions.Export();

            bool exportSucceeded =
                _exportActions.VerifyExportSuccessful();

            Assert.IsTrue(
                exportSucceeded,
                "Measurement export was not successful.");

            context.MeasurementExported = true;
        }
    }
}