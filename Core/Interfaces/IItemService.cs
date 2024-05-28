using Core.DomainModels;
using Entity = Infrastructure.Entities;

namespace Core.Interfaces
{
    public interface IItemService
    {
        Task CommitItemTaskAsync(DateTime? commitDay, int itemTaskId);
        Task UpdateItemIndex(int itemId, int newIndex, bool recurring);
        Task UpdateItemTaskIndex(int itemId, DateTime commitDate, int newIndex);
        Task CompleteItemTaskAsync(int itemTaskId);
        Task<ItemTask> CreateItemAsync(ItemTask itemTaskDomain);
        Task DeleteItemAsync(int itemId);
        Task<ItemTask> GetItemTaskByIdAsync(int itemTaskId);
        Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeekAsync();
        Task<List<ItemTask>> GetActiveItemTasksAsync(bool recurring, bool includeWeekdaysCommitted);
        Task<ItemTask> UpdateItemAsync(int itemTaskId, ItemTask updatedItemTask);
    }
}
