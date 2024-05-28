using Infrastructure.DAL;
using Infrastructure.Entities;
using Infrastructure.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
using Entity = Infrastructure.Entities;

namespace Infrastructure.Repository
{
    public class ItemTaskRepository : IItemTaskRepository
    {
        private readonly MyFeaturesDbContext _context;
        private readonly IGenericRepository<Entity.ItemTask, int> _itemTaskRepository;

        public ItemTaskRepository(
            MyFeaturesDbContext context,
            IGenericRepository<Entity.ItemTask, int> itemTaskRepository
            )
        {
            _context = context;
            _itemTaskRepository = itemTaskRepository;
        }

        //rename u "expired" jer samo se za njih primjenjuje
        public async Task UpdateExpiredItemTasks()
        {
            var today = DateTime.UtcNow.Date;

            // fetchamo samo istekle taskove
            var expiredItemTasks = await _context.ItemTasks
                .Where(itemTask => itemTask.CompletionDate == null &&
                                   itemTask.CommittedDate.Value.Date < today)
                .ToListAsync();

            if (!expiredItemTasks.Any())
            {
                return;
            }

            // Get the max row index for tasks committed today
            var maxRowIndexItemForDay = await _itemTaskRepository.GetFirstOrDefaultAsync(
                x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == today,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            int newRowIndex = maxRowIndexItemForDay != null ? maxRowIndexItemForDay.RowIndex + 1 : 0;

            foreach (var task in expiredItemTasks)
            {
                task.CommittedDate = today;
                task.RowIndex = newRowIndex++;
            }

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Dictionary<DateTime, List<ItemTask>>> GetItemTasksGroupedByCommitDateForNextWeek()
        {
            const int DaysInWeek = 7;
            var today = DateTime.UtcNow.Date;
            var endOfWeek = today.AddDays(DaysInWeek);

            // dohvati sve committane taskove koji nisu complete-ani, al da su unutar 7 dana
            var tasks = await _context.ItemTasks
                .Where(itemTask =>
                    itemTask.CompletionDate == null &&
                    itemTask.CommittedDate.Value.Date >= today &&
                    itemTask.CommittedDate.Value.Date < endOfWeek
                )
                .Include(itemTask => itemTask.Item)
                .ToListAsync();


            //dictionary koji će držat grupu taskova za tjedan
            var groupedTasks = new Dictionary<DateTime, List<ItemTask>>();

            // za svaki dan postoji lista, makar ih bilo 0 za taj dan
            for (DateTime day = today; day < endOfWeek; day = day.AddDays(1))
            {
                // commitani taskovi za specifičan dan
                var tasksForDay = tasks
                    .Where(t => t.CommittedDate.HasValue && t.CommittedDate.Value.Date == day)
                    .OrderBy(t => t.RowIndex)
                    .ToList();

                // dodaj dan i taskove za taj dan u dictionary
                groupedTasks.Add(day, tasksForDay);
            }

            return groupedTasks;
        }
    }
}
