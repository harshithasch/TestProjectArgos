using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProjectArgos.Actions.AdiPlanner;
using TestProjectArgos.Models;

namespace TestProjectArgos.Workflows
{
    public class AdiPatientCreationWorkflow : IPatientCreationWorkflow
    {
        private readonly AdiLoginActions _loginActions;
        private readonly AdiPatientActions _patientActions;
        private readonly AdiMeasurementOrderActions _measurementOrderActions;

        public AdiPatientCreationWorkflow(
            AdiLoginActions loginActions,
            AdiPatientActions patientActions,
            AdiMeasurementOrderActions measurementOrderActions)
        {
            _loginActions = loginActions;
            _patientActions = patientActions;
            _measurementOrderActions = measurementOrderActions;
        }

        public void CreatePatient(WorkflowContext context)
        {
            Assert.IsNotNull(context);
            Assert.IsNotNull(context.Patient);

            _loginActions.Login();

            _patientActions.CreatePatient(context.Patient);

            _patientActions.VerifyPatientCreated(
                context.Patient.Mrn);

            context.PatientCreated = true;

            _measurementOrderActions.OrderMeasurements(
                context.Patient);

            _measurementOrderActions.VerifyMeasurementOrderCreated(
                context.Patient.Mrn);

            context.MeasurementOrdered = true;
        }
    }
}