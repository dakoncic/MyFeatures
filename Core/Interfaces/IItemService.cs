using Core.DomainModels;
using Entity = Infrastructure.Entities;

namespace Core.Interfaces
{
    public interface IItemService
    {
        Task<ItemTask> CommitItemTaskAsync(int itemTaskId);
        Task CompleteItemTaskAsync(int itemTaskId);
        Task<Item> CreateItemAsync(NewItem newItemDomain);
        Task DeleteItemAsync(int itemId);
        Task<List<Item>> GetAllItemsAsync();
        Task<ItemTask> GetItemTaskByIdAsync(int itemTaskId);
        Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetItemsForNextWeekAsync();
        Task<List<ItemTask>> GetOneTimeItemTasksAsync();
        Task<List<ItemTask>> GetRecurringItemTasksAsync();
        Task ReturnItemTaskToGroupAsync(int itemTaskId);
        Task<ItemTask> UpdateItemAsync(int itemTaskId, ItemTask updatedItemTask);
    }
}
