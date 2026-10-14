using System;
using TestProjectArgos.Models;

namespace TestProjectArgos.Workflows
{
    public class PatientCreationWorkflowFactory
    {
        private readonly AdiPatientCreationWorkflow _adiWorkflow;
        private readonly ArgosPatientCreationWorkflow _argosWorkflow;

        public PatientCreationWorkflowFactory(
            AdiPatientCreationWorkflow adiWorkflow,
            ArgosPatientCreationWorkflow argosWorkflow)
        {
            _adiWorkflow = adiWorkflow;
            _argosWorkflow = argosWorkflow;
        }

        public IPatientCreationWorkflow GetWorkflow(
            PatientCreationSource source)
        {
            return source switch
            {
                PatientCreationSource.AdiPlanner => _adiWorkflow,
                PatientCreationSource.Argos => _argosWorkflow,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(source),
                    source,
                    "Unsupported patient creation source.")
            };
        }
    }
}