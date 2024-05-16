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

        public async Task UpdateWeekDayTaskItems()
        {
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(7);

            // dohvaćam ne complete-ane taskove, koji su commitani prije današnjeg dana (istekli) 
            //ili je due date unutar tjedan dana a nisu commitani -> automatski ih postavlja
            var relevantItemTasks = await _context.ItemTasks
                .Where(itemTask => itemTask.CompletionDate == null &&
                                   (itemTask.CommittedDate <= today ||
                                    (itemTask.DueDate >= today && itemTask.DueDate < endOfWeek && itemTask.CommittedDate == null)))
                .ToListAsync();

            // Categorize tasks
            var (expiredTasks, dueTasks) = CategorizeTasks(relevantItemTasks, today, endOfWeek);

            // Update RowIndex for expired tasks
            await UpdateExpiredTasks(expiredTasks, today, _itemTaskRepository);

            // Update RowIndex for due tasks
            await UpdateDueTasks(dueTasks, _itemTaskRepository);

            // Save changes if any modifications have been made
            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }
        }

        private (List<ItemTask> expiredTasks, List<ItemTask> dueTasks) CategorizeTasks(List<ItemTask> tasks, DateTime today, DateTime endOfWeek)
        {
            var expiredTasks = new List<ItemTask>();
            var dueTasks = new List<ItemTask>();

            foreach (var task in tasks)
            {
                if (task.CommittedDate < today)
                {
                    task.CommittedDate = today;
                    expiredTasks.Add(task);
                }
                else if (task.DueDate >= today && task.DueDate < endOfWeek && task.CommittedDate == null)
                {
                    task.CommittedDate = task.DueDate;
                    dueTasks.Add(task);
                }
            }

            return (expiredTasks, dueTasks);
        }

        private async Task UpdateExpiredTasks(List<ItemTask> expiredTasks, DateTime today, IGenericRepository<Entity.ItemTask, int> itemTaskRepository)
        {
            // Determine the starting RowIndex for expired tasks by fetching the maximum row index of today's tasks
            var maxRowIndexItem = await itemTaskRepository.GetFirstOrDefaultAsync(
                x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == today,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            int startIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;

            foreach (var task in expiredTasks)
            {
                task.RowIndex = startIndex++;
            }
        }

        private async Task UpdateDueTasks(List<ItemTask> dueTasks, IGenericRepository<Entity.ItemTask, int> itemTaskRepository)
        {
            var dueTasksByDay = dueTasks.GroupBy(t => t.CommittedDate.Value.Date);

            foreach (var dueTasksGroup in dueTasksByDay)
            {
                var commitDay = dueTasksGroup.Key;

                var maxRowIndexItemForDay = await itemTaskRepository.GetFirstOrDefaultAsync(
                    x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == commitDay,
                    q => q.OrderByDescending(x => x.RowIndex)
                );

                int newRowIndex = maxRowIndexItemForDay != null ? maxRowIndexItemForDay.RowIndex + 1 : 0;

                foreach (var task in dueTasksGroup)
                {
                    task.RowIndex = newRowIndex++;
                }
            }
        }

        public async Task<Dictionary<DateTime, List<ItemTask>>> GetItemTasksGroupedByCommitDateForNextWeek()
        {
            const int DaysInWeek = 7;
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(DaysInWeek);

            // dohvati sve committane taskove koji nisu complete-ani, al da su unutar 7 dana
            var tasks = await _context.ItemTasks
                .Where(itemTask =>
                    itemTask.CompletionDate == null &&
                    itemTask.CommittedDate >= today &&
                    itemTask.CommittedDate < endOfWeek
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
