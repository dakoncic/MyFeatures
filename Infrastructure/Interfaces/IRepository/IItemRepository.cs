using Infrastructure.Entities;

namespace Infrastructure.Interfaces.IRepository
{
    public interface IItemRepository
    {
        Task<List<Item>> GetNonRecurringUnitemTasksAsync();
    }
}
