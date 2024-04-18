using Core.DomainModels;

namespace Core.Interfaces
{
    public interface IItemService
    {
        Task<List<Item>> GetAllItemsAsync();
        Task<Item> GetItemByIdAsync(int itemId);
        Task<Item> CreateItemAsync(Item item);
        Task<Item> UpdateItemAsync(int itemId, Item item);
        Task DeleteItemAsync(int itemId);
    }
}
