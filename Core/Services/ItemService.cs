using Core.DomainModels;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using System.Linq.Expressions;
//using Infrastructure.Entities; ako imam error ambiguous reference, onda maknut ovu liniju
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class ItemService : IItemService
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

        //vjerojatno se neće koristiti nigdje
        public async Task<List<Item>> GetAllItemsAsync()
        {
            var items = await _itemRepository.GetAllAsync();

            return items.Adapt<List<Item>>();
        }

        public async Task<List<ItemTask>> GetActiveItemTasksAsync(bool recurring, bool includeWeekdaysCommitted)
        {
            Expression<Func<Entity.ItemTask, bool>> filter = i =>
                i.Item.Recurring.Equals(recurring) && i.CompletionDate == null;

            if (!includeWeekdaysCommitted)
            {
                filter = filter.AndAlso(i =>
                    i.CommittedDate == null || i.CommittedDate >= DateTime.UtcNow.AddDays(7));
            }

            var itemTasks = await _itemTaskRepository.GetAllAsync(filter: filter, orderBy: x => x.OrderBy(n => n.Item.RowIndex), includeProperties: "Item");

            return itemTasks.Adapt<List<ItemTask>>();
        }

        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeekAsync()
        {
            //refaktor da se zove samo jednom prije prvog fetcha u danu, ili nekakav task scheduler koji 1 na dan to radi
            await _itemTaskExtendedRepository.UpdateWeekDayTaskItems();

            var groupedItems = await _itemTaskExtendedRepository.GetItemTasksGroupedByCommitDateForNextWeek();

            return groupedItems;
        }

        public async Task<ItemTask> GetItemTaskByIdAsync(int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");
            if (itemTaskEntity == null)
            {
                throw new NotFoundException($"ItemTask with ID {itemTaskId} not found.");
            }

            return itemTaskEntity.Adapt<ItemTask>();
        }

        public async Task<ItemTask> CreateItemAsync(ItemTask itemTaskDomain)
        {
            var itemEntity = itemTaskDomain.Item.Adapt<Entity.Item>();

            //po biznis logici, mora po defaulta odma biti kreiran ItemTask
            var itemTaskEntity = itemTaskDomain.Adapt<Entity.ItemTask>();

            //prvo so mapirali što možemo, a sad eksplicitno postavljamo value za days between property
            if (itemTaskDomain.Item.IntervalValue is not null)
            {
                if (itemTaskDomain.Item.IntervalType == IntervalType.Months)
                {
                    itemTaskEntity.Item.DaysBetween = CalculateDaysBetweenForMonths(itemTaskDomain.Item.IntervalValue.Value);
                }
                else
                {
                    itemTaskEntity.Item.DaysBetween = itemTaskDomain.Item.IntervalValue;
                }
            }

            if (itemTaskEntity.DueDate is not null)
            {
                itemTaskEntity.CommittedDate = itemTaskEntity.DueDate;

                var maxRowIndexItemTask = await _itemTaskRepository.GetFirstOrDefaultAsync(
                    x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == itemTaskEntity.CommittedDate.Value.Date,
                    q => q.OrderByDescending(x => x.RowIndex)
                );

                int newRowIndex = maxRowIndexItemTask != null ? maxRowIndexItemTask.RowIndex + 1 : 0;

                itemTaskEntity.RowIndex = newRowIndex;
            }

            //na create itema, daj row index i parentu, svakako mu treba početna vrijednost
            //ako će se kliknut sort button da već ima svoju poziciju
            //ako pustim da je null, sortiranje nije pouzdano
            var maxRowIndexItem = await _itemRepository.GetFirstOrDefaultAsync(
                x => x.Recurring.Equals(itemEntity.Recurring),
                q => q.OrderByDescending(x => x.RowIndex)
            );

            int startIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;
            itemEntity.RowIndex = startIndex;

            itemEntity.ItemTasks.Add(itemTaskEntity);

            _itemRepository.Add(itemEntity);

            await _itemRepository.SaveAsync();


            //fetchano je i dijete iako je isključen LazyLoading zato što sam ga dodao kad i parenta
            return itemTaskEntity.Adapt<ItemTask>();
        }

        public async Task<ItemTask> UpdateItemAsync(int itemTaskId, ItemTask updatedItemTask)
        {
            //radi se prvo get a ne odma update, zbog concurrency npr.
            //da ne može commitat na bazu ako je netko drugi u
            //međuvremenu save-ao a mi onda immao krivi row version
            //ili npr. ako je u međuvremenu obrisan pa i ne postoji više

            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");
            if (itemTaskEntity == null)
            {
                throw new NotFoundException($"ItemTask with ID {itemTaskId} not found.");
            }

            var originalDueDate = updatedItemTask.DueDate;

            //eksplicitno update-amo itemTaskEntity sa novim vrijednostima .Adapt()
            //ovo neće transformirat entity objekt u domain, samo update se radi
            updatedItemTask.Adapt(itemTaskEntity);

            //moramo kalkulirat dane opet za slučaj da je mijenjao
            if (updatedItemTask.Item.IntervalValue is not null)
            {
                if (updatedItemTask.Item.IntervalType == IntervalType.Months)
                {
                    itemTaskEntity.Item.DaysBetween = CalculateDaysBetweenForMonths(updatedItemTask.Item.IntervalValue.Value);
                }
                else
                {
                    itemTaskEntity.Item.DaysBetween = updatedItemTask.Item.IntervalValue;
                }
            }

            //ako je due date različit od originalnog datuma
            if (originalDueDate != itemTaskEntity.DueDate)
            {
                //ako je postavio due date na null, želim update i committed date
                itemTaskEntity.CommittedDate = itemTaskEntity.DueDate;

                //ako due date nije null, commita na ipak neki drugi datum na Edit-u
                if (itemTaskEntity.DueDate is not null)
                {
                    var maxRowIndexItem = await _itemTaskRepository.GetFirstOrDefaultAsync(
                        x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == itemTaskEntity.CommittedDate.Value.Date,
                        q => q.OrderByDescending(x => x.RowIndex)
                    );

                    int newRowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;

                    itemTaskEntity.RowIndex = newRowIndex;
                }
            }

            //ako dto child ima referencu na parent, parent dto nebi trebao imat na child
            //zato što se dogodi da kad s frontenda šaljem child s ref. na parenta
            //onda parent nema natrag na child, što mi kod .Adapt(itemTaskEntity) mapiranja ta prazna child lista
            //pregazi itemTaskEntity povučen s frontenda, i na update se obriše child.

            await _itemTaskRepository.SaveAsync();

            return itemTaskEntity.Adapt<ItemTask>();
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId);

            if (itemEntity == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

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
        }

        public async Task CommitItemTaskAsync(DateTime? commitDay, int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId);

            if (itemTaskEntity is null)
            {
                throw new NotFoundException($"ItemTask with ID {itemTaskId} not found.");
            }

            if (itemTaskEntity.CommittedDate.HasValue)
            {
                await UpdateItemTaskCommittedDateAsync(commitDay, itemTaskEntity);
            }
            else
            {
                await CommitItemTaskFirstTimeAsync(commitDay.Value, itemTaskEntity);
            }
        }

        //manualno commitanje samo iz originalne grupe u dan određen
        private async Task CommitItemTaskFirstTimeAsync(DateTime commitDay, Entity.ItemTask itemTaskEntity)
        {
            var maxRowIndexItem = await _itemTaskRepository.GetFirstOrDefaultAsync(
                x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == commitDay.Date,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            int newRowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;
            itemTaskEntity.CommittedDate = commitDay;
            itemTaskEntity.RowIndex = newRowIndex;

            await _itemTaskRepository.SaveAsync();
        }

        //manualno pomicanje već commitanog itema između dana, ili vraćanje u svoju grupu
        private async Task UpdateItemTaskCommittedDateAsync(DateTime? commitDay, Entity.ItemTask itemTaskEntity)
        {
            var originalCommitDate = itemTaskEntity.CommittedDate;

            if (commitDay.HasValue)
            {
                itemTaskEntity.CommittedDate = commitDay;

                var maxRowIndexItem = await _itemTaskRepository.GetFirstOrDefaultAsync(
                    x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == commitDay.Value.Date,
                    q => q.OrderByDescending(x => x.RowIndex)
                );

                int newRowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;
                itemTaskEntity.RowIndex = newRowIndex;
            }
            else
            {
                itemTaskEntity.CommittedDate = null;
            }

            await _itemTaskRepository.SaveAsync();

            // reorderanje taskova koji ostaju u originalnoj grupi
            var itemsInOriginalGroup = await _itemTaskRepository.GetAllAsync(
                x => x.CommittedDate.HasValue && x.CommittedDate.Value.Date == originalCommitDate.Value.Date && x.RowIndex > itemTaskEntity.RowIndex,
                q => q.OrderBy(x => x.RowIndex)
            );

            foreach (var item in itemsInOriginalGroup)
            {
                item.RowIndex--;
            }

            await _itemTaskRepository.SaveAsync();
        }

        //samo reorderanje pozicije unutar svoje grupe
        public async Task UpdateItemIndex(int itemId, int newIndex, bool recurring)
        {
            var itemToUpdate = await _itemRepository.GetByIdAsync(itemId);

            if (itemToUpdate == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

            int currentIndex = itemToUpdate.RowIndex;

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

            // Set the new index for the item to be updated
            itemToUpdate.RowIndex = newIndex;

            await _itemRepository.SaveAsync();
        }

        //samo reorderanje pozicije unutar svoje grupe
        public async Task UpdateItemTaskIndex(int itemId, DateTime commitDate, int newIndex)
        {
            var itemToUpdate = await _itemTaskRepository.GetByIdAsync(itemId);

            if (itemToUpdate == null)
            {
                throw new NotFoundException($"ItemTask with ID {itemId} not found.");
            }

            int currentIndex = itemToUpdate.RowIndex;

            Expression<Func<Entity.ItemTask, bool>> filter = x => x.CommittedDate.Value.Date == commitDate.Date && x.Id != itemId;
            Func<IQueryable<Entity.ItemTask>, IOrderedQueryable<Entity.ItemTask>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var itemsForDate = await _itemTaskRepository.GetAllAsync(filter, orderBy);

            // Reorder the items based on the new index
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

            // Set the new index for the item to be updated
            itemToUpdate.RowIndex = newIndex;

            await _itemTaskRepository.SaveAsync();
        }

        public async Task CompleteItemTaskAsync(int itemTaskId)
        {
            //one time item se može complete-at koji ima commited date već i koji nema
            //recurring se može complete-at koji ima datum
            //recurring koji nema datum će se samo vratit u svoju listu (novi ItemTask sa committedDate = null)

            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");
            if (itemTaskEntity == null)
            {
                throw new NotFoundException($"ItemTask with ID {itemTaskId} not found.");
            }

            //vrijeme complete-anja je sad, i tu završavamo s ovim taskom
            itemTaskEntity.CompletionDate = DateTime.UtcNow;

            //ako je recurring, kreiram novi itemTask
            //i kreira se novi ItemTask
            if (itemTaskEntity.Item.Recurring)
            {
                var newItemTaskEntity = new Entity.ItemTask
                {
                    ItemId = itemTaskEntity.Item.Id,
                    Description = itemTaskEntity.Item.Description
                };

                //ako npr. se uzima riblje ulje ned., neovisno zakasnio dan-2
                //onda uvečavam na DueDate
                //ali ako je ponavljajući bez datuma, npr. posjet zubarici (ja određujem kad, nema DueDate)
                //onda se ne uvečava ništa

                //ako je DueDate not null i ako ima daysBetween, a ne samo ako je DueDate not null
                if (itemTaskEntity.DueDate is not null && itemTaskEntity.Item.DaysBetween is not null)
                {
                    var daysBetween = itemTaskEntity.Item.DaysBetween.Value;

                    if (itemTaskEntity.Item.RenewOnDueDate!.Value)
                    {
                        newItemTaskEntity.DueDate = itemTaskEntity.DueDate.Value;

                        // novi DueDate ne smije biti u prošlosti, ako sam zakasnio čak i za novi datum
                        while (newItemTaskEntity.DueDate < DateTime.UtcNow.Date)
                        {
                            newItemTaskEntity.DueDate = newItemTaskEntity.DueDate.Value.AddDays(daysBetween);
                        }
                    }
                    //inače se obnavlja na completion date npr. registracija auta
                    else
                    {
                        newItemTaskEntity.DueDate = itemTaskEntity.CompletionDate.Value.AddDays(daysBetween);
                    }

                    //odma committamo, ne čekamo ništa
                    newItemTaskEntity.CommittedDate = newItemTaskEntity.DueDate;
                }

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

                await _itemRepository.SaveAsync();
            }


            await _itemTaskRepository.SaveAsync();
        }

        private int CalculateDaysBetweenForMonths(int months)
        {
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = startDate.AddMonths(months);

            int daysBetween = (endDate - startDate).Days;

            return daysBetween;
        }

    }
}
