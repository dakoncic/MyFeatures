using Infrastructure.DAL;
using Infrastructure.Entities;
using Infrastructure.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class ItemTaskRepository : IItemTaskRepository
    {
        private readonly MyFeaturesDbContext _context;

        public ItemTaskRepository(MyFeaturesDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<IGrouping<DateTime?, ItemTask>>> GetItemTasksGroupedByDueDateForNextWeek()
        {
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(7);

            return await _context.ItemTasks
                .Where(itemTask => itemTask.DueDate >= today && itemTask.DueDate < endOfWeek)
                .GroupBy(itemTask => itemTask.DueDate)
                .ToListAsync();

        }

    }
}
