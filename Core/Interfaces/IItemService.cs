using Core.DomainModels;
using Entity = Infrastructure.Entities;

namespace Core.Interfaces
{
    public interface IItemService
    {
        Task CreateItemAndTask(ItemTask itemTaskDomain);
        Task<ItemTask> GetItemTaskById(int itemTaskId);
        Task UpdateItemAndTask(int itemTaskId, ItemTask itemTaskDomain);
        Task DeleteItemAndTasks(int itemId);
        Task CompleteItemTask(int itemTaskId);
        Task CommitItemTaskOrReturnToGroup(DateTime? commitDay, int itemTaskId);
        Task ReorderItemInsideGroup(int itemId, int newIndex, bool recurring);
        Task ReorderItemTaskInsideGroup(int itemId, DateTime commitDate, int newIndex);
        Task<List<ItemTask>> GetActiveItemTasks(bool recurring, bool includeWeekdaysCommitted);
        Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeek();
    }
}
