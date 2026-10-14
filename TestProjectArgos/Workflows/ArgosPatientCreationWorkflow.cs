using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestProjectArgos.Actions.Argos;
using TestProjectArgos.Models;

namespace TestProjectArgos.Workflows
{
    public class ArgosPatientCreationWorkflow : IPatientCreationWorkflow
    {
        private readonly ArgosPatientActions _patientActions;

        public ArgosPatientCreationWorkflow(
            ArgosPatientActions patientActions)
        {
            _patientActions = patientActions;
        }

        public void CreatePatient(WorkflowContext context)
        {
            Assert.IsNotNull(context);
            Assert.IsNotNull(context.Patient);

            _patientActions.CreatePatient(context.Patient);

            _patientActions.VerifyPatientCreated(
                context.Patient.Mrn);

            context.PatientCreated = true;
        }
    }
}