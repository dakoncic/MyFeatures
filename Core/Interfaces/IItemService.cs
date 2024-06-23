using Core.DomainModels;
using Entity = Infrastructure.Entities;

namespace Core.Interfaces
{
    public interface IItemService
    {
        Task CommitItemTaskOrReturnToGroup(DateTime? commitDay, int itemTaskId);
        Task ReorderItemInsideGroup(int itemId, int newIndex, bool recurring);
        Task ReorderItemTaskInsideGroup(int itemId, DateTime commitDate, int newIndex);
        Task CompleteItemTask(int itemTaskId);
        Task CreateItem(ItemTask itemTaskDomain);
        Task DeleteItem(int itemId);
        Task<ItemTask> GetItemTaskById(int itemTaskId);
        Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeek();
        Task<List<ItemTask>> GetActiveItemTasks(bool recurring, bool includeWeekdaysCommitted);
        Task UpdateItem(int itemTaskId, ItemTask updatedItemTask);
    }
}
