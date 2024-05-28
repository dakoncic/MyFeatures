using Infrastructure.Entities;

namespace Infrastructure.Interfaces.IRepository
{
    public interface IItemTaskRepository
    {
        Task UpdateExpiredItemTasks();
        Task<Dictionary<DateTime, List<ItemTask>>> GetItemTasksGroupedByCommitDateForNextWeek();
    }
}
