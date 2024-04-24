using Infrastructure.DAL;
using Infrastructure.Entities;
using Infrastructure.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class CommitedItemRepository : ICommitedItemRepository
    {
        private readonly MyFeaturesDbContext _context;

        public CommitedItemRepository(MyFeaturesDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<IGrouping<DateTime, CommittedItem>>> GetCommitedItemsGroupedByDueDateForNextWeek()
        {
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(7);

            return await _context.CommittedItems
                .Where(item => item.DueDate >= today && item.DueDate < endOfWeek)
                .GroupBy(item => item.DueDate)
                .ToListAsync();

        }

    }
}
