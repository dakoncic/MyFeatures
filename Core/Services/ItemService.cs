using Core.DomainModels;
using Core.Helpers;
using Core.Interfaces;
using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using Shared;
using System.Linq.Expressions;
//using Infrastructure.Entities; ako imam error ambiguous reference, onda maknut ovu liniju
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class ItemService : BaseService, IItemService
    {
        private readonly MyFeaturesDbContext _context;
        private readonly IGenericRepository<Entity.Item, int> _itemRepository;
        private readonly IGenericRepository<Entity.ItemTask, int> _itemTaskRepository;

        private readonly IItemTaskRepository _itemTaskExtendedRepository;

        public ItemService(
            MyFeaturesDbContext context,
            IGenericRepository<Entity.Item, int> itemRepository,
            IGenericRepository<Entity.ItemTask, int> itemTaskRepository,
            IItemTaskRepository itemTaskExtendedRepository
            )
        {
            _context = context;
            _itemRepository = itemRepository;
            _itemTaskRepository = itemTaskRepository;
            _itemTaskExtendedRepository = itemTaskExtendedRepository;
        }

        public async Task<List<ItemTask>> GetActiveItemTasksAsync(bool recurring, bool includeWeekdaysCommitted)
        {
            Expression<Func<Entity.ItemTask, bool>> filter = i =>
                i.Item.Recurring.Equals(recurring) && i.CompletionDate == null;

            if (!includeWeekdaysCommitted)
            {
                filter = filter.AndAlso(i =>
                    //ako je uvjet "i.CommittedDate == null ||" onda će izostat
                    i.CommittedDate == null ||
                    i.CommittedDate.Value.Date >= DateTime.Now.Date.AddDays(GlobalConstants.DaysRange));
            }

            var itemTasks = await _itemTaskRepository.GetAllAsync(
                filter: filter,
                orderBy: x => x.OrderBy(n => n.Item.RowIndex),
                includeProperties: "Item"
                );

            return itemTasks.Adapt<List<ItemTask>>();
        }

        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeekAsync()
        {
            await _itemTaskExtendedRepository.UpdateExpiredItemTasks();

            var groupedItems = await _itemTaskExtendedRepository.GetItemTasksGroupedByCommitDateForNextWeek();

            return groupedItems;
        }

        public async Task<ItemTask> GetItemTaskByIdAsync(int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            //ostavljamo check ovdje u servisu, ako je entity null, onda on ne može zvat
            //nikakvu metodu da provjeri sam sebe jeli null
            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            return itemTaskEntity.Adapt<ItemTask>();
        }

        public async Task CreateItemAsync(ItemTask itemTaskDomain)
        {
            IntervalCalculator.CalculateAndAssignDaysBetween(itemTaskDomain.Item);

            itemTaskDomain.InitializeDates();

            if (itemTaskDomain.DueDate != null)
            {
                itemTaskDomain.RowIndex = await GetNewRowIndex(itemTaskDomain.CommittedDate);
            }

            //parentu moram dat row index
            var maxRowIndexItem = await _itemRepository.GetFirstOrDefaultAsync(
                x =>
                !x.Completed &&
                x.Recurring.Equals(itemTaskDomain.Item.Recurring),
                q => q.OrderByDescending(x => x.RowIndex)
            );

            itemTaskDomain.Item.RowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;

            var itemEntity = itemTaskDomain.Item.Adapt<Entity.Item>();
            var itemTaskEntity = itemTaskDomain.Adapt<Entity.ItemTask>();

            itemEntity.ItemTasks.Add(itemTaskEntity);

            _itemRepository.Add(itemEntity);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateItemAsync(int itemTaskId, ItemTask updatedItemTask)
        {
            //radi se prvo get a ne odma update, zbog concurrency npr.
            //da ne može commitat na bazu ako je netko drugi u
            //međuvremenu save-ao a mi onda immao krivi row version
            //ili npr. ako je u međuvremenu obrisan pa i ne postoji više

            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            //prvo povlačimo sve postojeće
            var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

            //a onda update-amo domainItemTask sa novim vrijednostima
            //domain možda ne sadrži sve property-e kao i entity, zato smo prethodno mapirali prvo postojeće
            //a tek onda gazimo sa novim objektom, i concurrency sa objekta će pregazit entity concurrency
            updatedItemTask.Adapt(domainItemTask);

            var newDueDate = domainItemTask.DueDate?.Date;
            var oldCommittedDate = itemTaskEntity.CommittedDate;

            IntervalCalculator.CalculateAndAssignDaysBetween(domainItemTask.Item);

            if (oldCommittedDate?.Date != newDueDate?.Date)
            {
                await HandleDueDateChange(itemTaskEntity, domainItemTask, oldCommittedDate, newDueDate);
            }

            domainItemTask.Adapt(itemTaskEntity);

            //ako dto child ima referencu na parent, parent dto nebi trebao imat na child
            //zato što se dogodi da kad s frontenda šaljem child s ref. na parenta
            //onda parent nema natrag na child, što mi kod .Adapt(itemTaskEntity) mapiranja ta prazna child lista
            //pregazi itemTaskEntity povučen s frontenda, i na update se obriše child.

            await _context.SaveChangesAsync();
        }

        private async Task HandleDueDateChange(Entity.ItemTask itemTaskEntity, ItemTask domainItemTask, DateTime? oldCommittedDate, DateTime? newDueDate)
        {
            domainItemTask.CommittedDate = newDueDate?.Date;

            //ako stari committed nije bio null, onda će dolazit do promjene indexa itema koji ostaju
            if (oldCommittedDate is not null)
            {
                await _itemTaskRepository.UpdateBatchAsync(
                x => x.CompletionDate == null &&
                     x.CommittedDate.HasValue &&
                     oldCommittedDate.HasValue &&
                     x.CommittedDate.Value.Date == oldCommittedDate.Value.Date &&
                     x.RowIndex > itemTaskEntity.RowIndex,
                x => new Entity.ItemTask { RowIndex = x.RowIndex - 1 }
                );
            }

            //a ako novi datum nije null, onda trebamo novi index za ItemTask
            //napomena: moguće je da obadva ne budu null
            if (newDueDate is not null)
            {
                domainItemTask.RowIndex = await GetNewRowIndex(domainItemTask.CommittedDate);
            }
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId, "ItemTasks");

            CheckIfNull(itemEntity, $"Item with ID {itemId} not found.");

            _itemRepository.Delete(itemId);

            await _itemRepository.UpdateBatchAsync(
                x => !x.Completed &&
                     x.Recurring.Equals(itemEntity.Recurring) &&
                     x.RowIndex > itemEntity.RowIndex,
                x => new Entity.Item { RowIndex = x.RowIndex - 1 }
            );

            //fetcham itemTask ako je bio committan na weekday
            var itemTaskEntity = itemEntity.ItemTasks.FirstOrDefault(x => x.CommittedDate != null && x.CompletionDate == null);

            //i ako je bio, za sve itemTaskove na taj dan im pomičem index
            if (itemTaskEntity != null)
            {
                await _itemTaskRepository.UpdateBatchAsync(
                    x => x.CompletionDate == null &&
                         x.CommittedDate.HasValue &&
                         itemTaskEntity.CommittedDate.HasValue &&
                         x.CommittedDate.Value.Date == itemTaskEntity.CommittedDate.Value.Date &&
                         x.RowIndex > itemTaskEntity.RowIndex,
                    x => new Entity.ItemTask { RowIndex = x.RowIndex - 1 }
                );

            }

            await _context.SaveChangesAsync();
        }

        public async Task CommitItemTaskOrReturnToGroupAsync(DateTime? newCommitDay, int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId);
            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

            var oldCommittedDate = domainItemTask.CommittedDate;

            //ako dolazi do bilo kakve NOVE promjene committed date-a
            if (oldCommittedDate?.Date != newCommitDay?.Date)
            {
                domainItemTask.CommittedDate = newCommitDay?.Date;

                //ako je stari imao vrijednost i dolazi do promjene onda ide decrement
                if (oldCommittedDate is not null)
                {
                    await _itemTaskRepository.UpdateBatchAsync(
                        x => x.CompletionDate == null &&
                             x.CommittedDate.HasValue &&
                             x.CommittedDate.Value.Date == oldCommittedDate.Value.Date &&
                             x.RowIndex > domainItemTask.RowIndex,
                        x => new Entity.ItemTask { RowIndex = x.RowIndex - 1 }
                    );
                }

                if (newCommitDay is not null)
                {
                    domainItemTask.RowIndex = await GetNewRowIndex(newCommitDay);
                }
                else
                {
                    domainItemTask.DueDate = null;
                }
            }

            domainItemTask.Adapt(itemTaskEntity);

            await _context.SaveChangesAsync();
        }

        //samo reorderanje pozicije unutar svoje originalne grupe
        public async Task UpdateItemIndex(int itemId, int newIndex, bool recurring)
        {
            var itemToUpdate = await _itemRepository.GetByIdAsync(itemId);

            CheckIfNull(itemToUpdate, $"Item with ID {itemId} not found.");

            int currentIndex = itemToUpdate.RowIndex;

            Expression<Func<Entity.Item, bool>> filter = x =>
                !x.Completed &&
                x.Recurring.Equals(recurring) &&
                x.Id != itemId;
            Func<IQueryable<Entity.Item>, IOrderedQueryable<Entity.Item>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var items = await _itemRepository.GetAllAsync(filter, orderBy);

            RowIndexHelper.UpdateRowIndexes<Entity.Item>(items, newIndex, currentIndex);

            itemToUpdate.RowIndex = newIndex;

            await _context.SaveChangesAsync();
        }

        //samo reorderanje pozicije unutar svoje grupe
        public async Task UpdateItemTaskIndex(int itemId, DateTime commitDate, int newIndex)
        {
            var itemToUpdate = await _itemTaskRepository.GetByIdAsync(itemId);

            CheckIfNull(itemToUpdate, $"ItemTask with ID {itemId} not found.");

            int currentIndex = itemToUpdate.RowIndex;

            Expression<Func<Entity.ItemTask, bool>> filter =
                x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date == commitDate.Date &&
                x.Id != itemId;

            Func<IQueryable<Entity.ItemTask>, IOrderedQueryable<Entity.ItemTask>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var itemsForDate = await _itemTaskRepository.GetAllAsync(filter, orderBy);

            RowIndexHelper.UpdateRowIndexes<Entity.ItemTask>(itemsForDate, newIndex, currentIndex);

            itemToUpdate.RowIndex = newIndex;

            await _context.SaveChangesAsync();
        }

        public async Task CompleteItemTaskAsync(int itemTaskId)
        {
            //one time item se može complete-at koji ima commited date već i koji nema
            //recurring se može complete-at koji ima datum
            //recurring koji nema datum će se samo vratit u svoju listu (novi ItemTask sa committedDate = null)

            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            //task se complete-a
            itemTaskEntity.CompletionDate = DateTime.Now;

            //ako je itemTaskEntity bio commitan, onda za taj dan moram pomaknut sve indexe na ostalima za taj dan
            if (itemTaskEntity.CommittedDate != null)
            {
                await _itemTaskRepository.UpdateBatchAsync(
                    x => x.CompletionDate == null &&
                         x.CommittedDate.HasValue &&
                         itemTaskEntity.CommittedDate.HasValue &&
                         x.CommittedDate.Value.Date == itemTaskEntity.CommittedDate.Value.Date &&
                         x.RowIndex > itemTaskEntity.RowIndex,
                    x => new Entity.ItemTask { RowIndex = x.RowIndex - 1 }
                );
            }

            //u ovom batchu se update-aju samo itemi čiji je row index veći od našeg, znači nema clasha
            //i onda kod postavljanja novog itema, on dobiva novi datum koji je sigurno veći od današnjeg dana
            //znači neće bit clash za novi RowIndex

            //ako je recurring, kreiram novi itemTask
            if (itemTaskEntity.Item.Recurring)
            {
                var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

                //dobar primjer enkapsulacije biznis logike u domain klasu
                var newItemTask = domainItemTask.CreateNewRecurringTask();

                newItemTask.RowIndex = await GetNewRowIndex(newItemTask.CommittedDate);

                var newItemTaskEntity = newItemTask.Adapt<Entity.ItemTask>();

                _itemTaskRepository.Add(newItemTaskEntity);
            }
            //ako je item One Time, onda moram trimmat ostavljeno prazno mjesto
            else
            {
                await _itemRepository.UpdateBatchAsync(
                    x => !x.Completed &&
                         !x.Recurring &&
                         x.RowIndex > itemTaskEntity.Item.RowIndex,
                    x => new Entity.Item { RowIndex = x.RowIndex - 1 }
                );

                itemTaskEntity.Item.Completed = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<int> GetNewRowIndex(DateTime? compareDate)
        {
            var maxRowIndexItem = await _itemTaskRepository.GetFirstOrDefaultAsync(
            x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                compareDate.HasValue &&
                x.CommittedDate.Value.Date == compareDate.Value.Date,
            q => q.OrderByDescending(x => x.RowIndex)
            );

            int newRowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;
            return newRowIndex;
        }
    }
}
