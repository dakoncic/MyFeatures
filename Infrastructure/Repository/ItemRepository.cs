using Infrastructure.DAL;
using Infrastructure.Entities;
using Infrastructure.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class ItemRepository : IItemRepository
    {
        private readonly MyFeaturesDbContext _context;

        public ItemRepository(MyFeaturesDbContext context)
        {
            _context = context;
        }

        //ako želim naglasit da je ovo potpuna materijalizirana lista koje se neće query-at još
        //ond ostavit List
        public async Task<List<Item>> GetNonRecurringUnitemTasksAsync()
        {
            return await _context.Items
                .Where(i => !i.Recurring && !i.Deleted && !i.ItemTasks.Any(ci => ci.CommittedDate != null))
                .ToListAsync();
        }
    }
}
