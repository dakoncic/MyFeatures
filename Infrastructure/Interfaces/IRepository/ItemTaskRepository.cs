using Infrastructure.Entities;

namespace Infrastructure.Interfaces.IRepository
{
    public interface IItemTaskRepository
    {
        Task<IEnumerable<IGrouping<DateTime, ItemTask>>> GetItemTasksGroupedByDueDateForNextWeek();
    }
}
