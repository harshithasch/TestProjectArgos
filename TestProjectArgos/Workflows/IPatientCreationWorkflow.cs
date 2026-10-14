using TestProjectArgos.Models;

namespace TestProjectArgos.Workflows
{
    public interface IPatientCreationWorkflow
    {
        void CreatePatient(WorkflowContext context);
    }
}