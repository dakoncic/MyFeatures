using Core.DomainModels;
using Core.Helpers;
using Core.Interfaces;
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
        private readonly IGenericRepository<Entity.Item, int> _itemRepository;
        private readonly IGenericRepository<Entity.ItemTask, int> _itemTaskRepository;

        private readonly IItemTaskRepository _itemTaskExtendedRepository;

        public ItemService(
            IGenericRepository<Entity.Item, int> itemRepository,
            IGenericRepository<Entity.ItemTask, int> itemTaskRepository,
            IItemTaskRepository itemTaskExtendedRepository
            )
        {
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
                    i.CommittedDate == null || i.CommittedDate.Value.Date >= DateTime.UtcNow.Date.AddDays(GlobalConstants.DaysRange));
            }

            var itemTasks = await _itemTaskRepository.GetAllAsync(filter: filter, orderBy: x => x.OrderBy(n => n.Item.RowIndex), includeProperties: "Item");

            return itemTasks.Adapt<List<ItemTask>>();
        }

        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeekAsync()
        {
            //refaktor da se zove samo jednom prije prvog fetcha u danu, ili nekakav task scheduler koji 1 na dan to radi
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

            //parentu u svakom slučaju dajemo row index, null može problem stvorit
            var maxRowIndexItem = await _itemRepository.GetFirstOrDefaultAsync(
                x => x.Recurring.Equals(itemTaskDomain.Item.Recurring) && x.RowIndex != null,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            itemTaskDomain.Item.RowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex.Value + 1 : 0;

            var itemEntity = itemTaskDomain.Item.Adapt<Entity.Item>();
            var itemTaskEntity = itemTaskDomain.Adapt<Entity.ItemTask>();

            itemEntity.ItemTasks.Add(itemTaskEntity);

            _itemRepository.Add(itemEntity);

            await _itemRepository.SaveAsync();
        }

        public async Task UpdateItemAsync(int itemTaskId, ItemTask updatedItemTask)
        {
            //radi se prvo get a ne odma update, zbog concurrency npr.
            //da ne može commitat na bazu ako je netko drugi u
            //međuvremenu save-ao a mi onda immao krivi row version
            //ili npr. ako je u međuvremenu obrisan pa i ne postoji više

            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var originalDueDate = itemTaskEntity.DueDate;

            var domainItemTask = itemTaskEntity.Adapt<ItemTask>();
            //eksplicitno update-amo domainItemTask sa novim poslanim vrijednostima .Adapt()
            updatedItemTask.Adapt(domainItemTask);

            //moramo kalkulirat dane opet za slučaj da je mijenjao
            IntervalCalculator.CalculateAndAssignDaysBetween(domainItemTask.Item);

            //ako je due date različit od originalnog datuma
            if (originalDueDate != domainItemTask.DueDate)
            {
                //ako je postavio due date na null, želim update i committed date
                domainItemTask.CommittedDate = domainItemTask.DueDate;

                //ako due date nije null, commita na ipak neki drugi datum na Edit-u
                if (domainItemTask.DueDate is not null)
                {
                    domainItemTask.RowIndex = await GetNewRowIndex(itemTaskEntity.CommittedDate);
                }
            }

            domainItemTask.Adapt(itemTaskEntity);

            //ako dto child ima referencu na parent, parent dto nebi trebao imat na child
            //zato što se dogodi da kad s frontenda šaljem child s ref. na parenta
            //onda parent nema natrag na child, što mi kod .Adapt(itemTaskEntity) mapiranja ta prazna child lista
            //pregazi itemTaskEntity povučen s frontenda, i na update se obriše child.

            await _itemTaskRepository.SaveAsync();
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId, "ItemTasks");

            CheckIfNull(itemEntity, $"Item with ID {itemId} not found.");

            _itemRepository.Delete(itemId);

            var items = await _itemRepository.GetAllAsync(
                    x => x.Recurring.Equals(itemEntity.Recurring) && x.RowIndex > itemEntity.RowIndex,
                    q => q.OrderBy(x => x.RowIndex)
                );

            foreach (var item in items)
            {
                item.RowIndex--;
            }

            await _itemRepository.SaveAsync();

            var itemTaskEntity = itemEntity.ItemTasks.FirstOrDefault(x => x.CommittedDate != null && x.CompletionDate == null);

            if (itemTaskEntity != null)
            {
                var committedItemTasksForDate = await _itemTaskRepository.GetAllAsync(
                    x => x.CommittedDate.Value.Date == itemTaskEntity.CommittedDate.Value.Date && x.RowIndex > itemEntity.RowIndex,
                    q => q.OrderBy(x => x.RowIndex)
                );

                foreach (var commitedItemTask in committedItemTasksForDate)
                {
                    commitedItemTask.RowIndex--;
                }

                await _itemTaskRepository.SaveAsync();
            }
        }

        public async Task CommitItemTaskAsync(DateTime? commitDay, int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId);
            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

            if (domainItemTask.CommittedDate.HasValue)
            {
                await UpdateItemTaskCommittedDateAsync(commitDay, domainItemTask);
            }
            else
            {
                await CommitItemTaskFirstTimeAsync(commitDay.Value, domainItemTask);
            }


            domainItemTask.Adapt(itemTaskEntity);
            await _itemTaskRepository.SaveAsync();
        }

        //manualno pomicanje već commitanog itema između dana, ili vraćanje u svoju grupu
        private async Task UpdateItemTaskCommittedDateAsync(DateTime? commitDay, ItemTask domainItemTask)
        {
            // Reorder tasks remaining in the original group
            var itemsInOriginalGroup = await _itemTaskRepository.GetAllAsync(
                x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == domainItemTask.CommittedDate.Value.Date && x.RowIndex > domainItemTask.RowIndex,
                q => q.OrderBy(x => x.RowIndex)
            );

            foreach (var item in itemsInOriginalGroup)
            {
                item.RowIndex--;
            }

            if (commitDay.HasValue)
            {
                domainItemTask.CommittedDate = commitDay;
                domainItemTask.RowIndex = await GetNewRowIndex(commitDay);
            }
            else
            {
                domainItemTask.CommittedDate = null;
                domainItemTask.DueDate = null;
                domainItemTask.RowIndex = null;
            }

            await _itemTaskRepository.SaveAsync();
        }

        //manualno commitanje samo iz originalne grupe u dan određen
        private async Task CommitItemTaskFirstTimeAsync(DateTime commitDay, ItemTask domainItemTask)
        {
            domainItemTask.RowIndex = await GetNewRowIndex(commitDay.Date);
            domainItemTask.CommittedDate = commitDay.Date;
        }

        //samo reorderanje pozicije unutar svoje grupe
        public async Task UpdateItemIndex(int itemId, int newIndex, bool recurring)
        {
            var itemToUpdate = await _itemRepository.GetByIdAsync(itemId);

            CheckIfNull(itemToUpdate, $"Item with ID {itemId} not found.");

            int currentIndex = itemToUpdate.RowIndex.Value;

            Expression<Func<Entity.Item, bool>> filter = x => x.Recurring.Equals(recurring) && x.Id != itemId;
            Func<IQueryable<Entity.Item>, IOrderedQueryable<Entity.Item>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var items = await _itemRepository.GetAllAsync(filter, orderBy);

            // Reorder the items based on the new index
            if (newIndex < currentIndex)
            {
                // The item is moving up in the order
                foreach (var item in items)
                {
                    if (item.RowIndex >= newIndex && item.RowIndex < currentIndex)
                    {
                        item.RowIndex += 1;
                    }
                }
            }
            else if (newIndex > currentIndex)
            {
                // The item is moving down in the order
                foreach (var item in items)
                {
                    if (item.RowIndex > currentIndex && item.RowIndex <= newIndex)
                    {
                        item.RowIndex -= 1;
                    }
                }
            }

            itemToUpdate.RowIndex = newIndex;

            await _itemRepository.SaveAsync();
        }

        //samo reorderanje pozicije unutar svoje grupe
        public async Task UpdateItemTaskIndex(int itemId, DateTime commitDate, int newIndex)
        {
            var itemToUpdate = await _itemTaskRepository.GetByIdAsync(itemId);

            CheckIfNull(itemToUpdate, $"ItemTask with ID {itemId} not found.");

            int currentIndex = itemToUpdate.RowIndex.Value;

            Expression<Func<Entity.ItemTask, bool>> filter = x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == commitDate.Date && x.Id != itemId;
            Func<IQueryable<Entity.ItemTask>, IOrderedQueryable<Entity.ItemTask>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var itemsForDate = await _itemTaskRepository.GetAllAsync(filter, orderBy);

            // ne mogu maknut u domain ako nisam mapirao u domain, a mapirat u domain je malo overkill
            if (newIndex < currentIndex)
            {
                // The item is moving up in the order
                foreach (var item in itemsForDate)
                {
                    if (item.RowIndex >= newIndex && item.RowIndex < currentIndex)
                    {
                        item.RowIndex += 1;
                    }
                }
            }
            else if (newIndex > currentIndex)
            {
                // The item is moving down in the order
                foreach (var item in itemsForDate)
                {
                    if (item.RowIndex > currentIndex && item.RowIndex <= newIndex)
                    {
                        item.RowIndex -= 1;
                    }
                }
            }

            itemToUpdate.RowIndex = newIndex;

            await _itemTaskRepository.SaveAsync();
        }

        public async Task CompleteItemTaskAsync(int itemTaskId)
        {
            //one time item se može complete-at koji ima commited date već i koji nema
            //recurring se može complete-at koji ima datum
            //recurring koji nema datum će se samo vratit u svoju listu (novi ItemTask sa committedDate = null)

            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

            domainItemTask.Complete();

            //ako je recurring, kreiram novi itemTask
            if (itemTaskEntity.Item.Recurring)
            {
                //dobar primjer enkapsulacije biznis logike u domain klasu
                var newItemTask = domainItemTask.CreateNewRecurringTask();

                var newItemTaskEntity = newItemTask.Adapt<Entity.ItemTask>();

                _itemTaskRepository.Add(newItemTaskEntity);
            }
            //ako je item One Time, onda moram trimmat ostavljeno prazno mjesto
            else
            {
                var nonRecurringItems = await _itemRepository.GetAllAsync(
                    x => !x.Recurring && x.RowIndex > itemTaskEntity.Item.RowIndex,
                    q => q.OrderBy(x => x.RowIndex)
                );

                foreach (var item in nonRecurringItems)
                {
                    item.RowIndex--;
                }

                //index je dalje nerelevantan za parent one time item
                //par linija gore je još trebao
                itemTaskEntity.Item.RowIndex = null;

                await _itemRepository.SaveAsync();
            }

            await _itemTaskRepository.SaveAsync();
        }

        private async Task<int> GetNewRowIndex(DateTime? compareDate)
        {
            var maxRowIndexItem = await _itemTaskRepository.GetFirstOrDefaultAsync(
                x => x.CommittedDate.HasValue &&
                     compareDate.HasValue &&
                     x.CommittedDate.Value.Date == compareDate.Value.Date &&
                     x.RowIndex != null,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            int newRowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex.Value + 1 : 0;
            return newRowIndex;
        }
    }
}
