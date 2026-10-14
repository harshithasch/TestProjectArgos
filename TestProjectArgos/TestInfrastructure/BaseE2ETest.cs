using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProjectArgos.Actions.AdiPlanner;
using TestProjectArgos.Actions.Argos;
using TestProjectArgos.Actions.MOVU;
using TestProjectArgos.Actions.VisionPlanner;
using TestProjectArgos.Workflows;

namespace TestProjectArgos.TestInfrastructure
{
    public abstract class BaseE2ETest
    {
        protected AdiToArgosIntegrationWorkflow E2EWorkflow { get; private set; }
            = null!;

        [TestInitialize]
        public virtual void TestInitialize()
        {
            /*
             * Replace the constructor arguments below with the existing
             * driver, browser, FlaUI application and window objects used
             * by your current classes.
             */

            var adiLoginActions = new AdiLoginActions();
            var adiPatientActions = new AdiPatientActions();
            var adiOrderActions = new AdiMeasurementOrderActions();
            var adiVerificationActions =
                new AdiExportVerificationActions();

            var argosPatientActions = new ArgosPatientActions();

            var movuLaunchActions = new MovuLaunchActions();
            var movuMeasurementActions =
                new MovuMeasurementActions();

            var vpLaunchActions = new VisionPlannerLaunchActions();
            var surgeonLoginActions = new SurgeonLoginActions();
            var patientSearchActions = new PatientSearchActions();
            var exportActions = new MeasurementExportActions();

            var adiPatientWorkflow =
                new AdiPatientCreationWorkflow(
                    adiLoginActions,
                    adiPatientActions,
                    adiOrderActions);

            var argosPatientWorkflow =
                new ArgosPatientCreationWorkflow(
                    argosPatientActions);

            var patientFactory =
                new PatientCreationWorkflowFactory(
                    adiPatientWorkflow,
                    argosPatientWorkflow);

            var measurementWorkflow =
                new MeasurementCaptureWorkflow(
                    movuLaunchActions,
                    vpLaunchActions,
                    surgeonLoginActions,
                    patientSearchActions,
                    movuMeasurementActions,
                    exportActions);

            E2EWorkflow =
                new AdiToArgosIntegrationWorkflow(
                    patientFactory,
                    measurementWorkflow,
                    adiVerificationActions);
        }

        [TestCleanup]
        public virtual void TestCleanup()
        {
            /*
             * Add screenshot capture on failure here.
             * Do not close MOVU or VP between workflow steps.
             * Close them only after the complete E2E scenario finishes.
             */
        }
    }
}