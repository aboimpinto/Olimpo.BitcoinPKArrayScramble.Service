using System.Threading.Tasks;

namespace WorkerUtilitiesService
{
    public interface IWorkerLifeCycleService
    {
        Task StartWorker(string workerName);
    }
}
