using Infrastructure.Entities;

namespace Infrastructure.Interfaces.IRepository
{
    public interface IItemTaskRepository
    {
        Task<Dictionary<DateTime, List<ItemTask>>> GetItemTasksGroupedByDueDateForNextWeek();
    }
}
