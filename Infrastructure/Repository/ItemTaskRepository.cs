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

        public async Task<Dictionary<DateTime, List<ItemTask>>> GetItemTasksGroupedByDueDateForNextWeek()
        {
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(7);

            // dohvati sve taskove za tjedan
            var tasks = await _context.ItemTasks
                .Where(itemTask => itemTask.DueDate >= today && itemTask.DueDate < endOfWeek)
                .ToListAsync();

            //dictionary koji će držat grupu taskova za tjedan
            var groupedTasks = new Dictionary<DateTime, List<ItemTask>>();

            // za svaki dan postoji lista, neovisno koliko ih ima taj dan
            for (DateTime day = today; day < endOfWeek; day = day.AddDays(1))
            {
                // filter taskova za specifičan dan
                var tasksForDay = tasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == day).ToList();

                // dodaj dan i taskove za taj dan u dictionary
                groupedTasks.Add(day, tasksForDay);
            }

            return groupedTasks;
        }


    }
}
