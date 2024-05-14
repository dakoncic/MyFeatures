using Core.DomainModels;
using Entity = Infrastructure.Entities;

namespace Core.Interfaces
{
    public interface IItemService
    {
        Task CommitItemTaskAsync(DateTime commitDay, int itemTaskId);
        Task UpdateItemTaskIndex(int itemId, DateTime commitDate, int newIndex);
        Task CompleteItemTaskAsync(int itemTaskId);
        Task<ItemTask> CreateItemAsync(ItemTask itemTaskDomain);
        Task DeleteItemAsync(int itemId);
        Task<List<Item>> GetAllItemsAsync();
        Task<ItemTask> GetItemTaskByIdAsync(int itemTaskId);
        Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeekAsync();
        Task<List<ItemTask>> GetOneTimeItemTasksAsync();
        Task<List<ItemTask>> GetRecurringItemTasksAsync();
        Task ReturnItemTaskToGroupAsync(int itemTaskId);
        Task<ItemTask> UpdateItemAsync(int itemTaskId, ItemTask updatedItemTask);
    }
}
