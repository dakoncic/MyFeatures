using Infrastructure.Entities;

namespace Infrastructure.Interfaces.IRepository
{
    public interface IItemTaskRepository
    {
        Task UpdateWeekDayTaskItems();
        Task<Dictionary<DateTime, List<ItemTask>>> GetItemTasksGroupedByCommitDateForNextWeek();
    }
}
