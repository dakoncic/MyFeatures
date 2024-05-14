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

        public async Task UpdateWeekDayTaskItems()
        {
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(7);

            // dohvaćam ne complete-ane taskove, koji su commitani prije današnjeg dana (istekli) 
            //ili je due date unutar tjedan dana a nisu commitani -> automatski ih postavlja
            var relevantItemTasks = await _context.ItemTasks
                .Where(itemTask => itemTask.CompletionDate == null &&
                                   (itemTask.CommittedDate < today ||
                                    (itemTask.DueDate >= today && itemTask.DueDate < endOfWeek && itemTask.CommittedDate == null)))
                .ToListAsync();

            foreach (var task in relevantItemTasks)
            {
                if (task.CommittedDate < today)
                {
                    task.CommittedDate = today;
                }
                //keeping redundant check to safeguard future change
                else if (task.DueDate >= today && task.DueDate < endOfWeek && task.CommittedDate == null)
                {
                    task.CommittedDate = task.DueDate;
                }
            }

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
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
