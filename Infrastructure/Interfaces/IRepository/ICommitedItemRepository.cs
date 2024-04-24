using Infrastructure.Entities;

namespace Infrastructure.Interfaces.IRepository
{
    public interface ICommitedItemRepository
    {
        Task<IEnumerable<IGrouping<DateTime, CommittedItem>>> GetCommitedItemsGroupedByDueDateForNextWeek();
    }
}
