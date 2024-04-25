using Core.DomainModels;
using Entity = Infrastructure.Entities;

namespace Core.Interfaces
{
    public interface IItemService
    {
        Task<List<Item>> GetAllItemsAsync();
        Task<List<Item>> GetOneTimeItemsAsync();
        Task<List<Item>> GetRecurringItemsAsync();
        Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetItemsForNextWeekAsync();
        Task<Item> GetItemByIdAsync(int itemId);
        Task<Item> CreateItemAsync(NewItem item);
        Task<Item> UpdateItemAsync(int itemId, Item item);
        Task DeleteItemAsync(int itemId);
    }
}
