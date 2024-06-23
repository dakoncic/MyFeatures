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

        public ItemService(
            MyFeaturesDbContext context,
            IGenericRepository<Entity.Item, int> itemRepository,
            IGenericRepository<Entity.ItemTask, int> itemTaskRepository
            )
        {
            _context = context;
            _itemRepository = itemRepository;
            _itemTaskRepository = itemTaskRepository;
        }

        public async Task<List<ItemTask>> GetActiveItemTasks(bool recurring, bool includeWeekdaysCommitted)
        {
            //ako je sort mode, onda prikaži sve ne complete-ane
            Expression<Func<Entity.ItemTask, bool>> filter = i =>
                i.Item.Recurring.Equals(recurring) && i.CompletionDate == null;

            if (!includeWeekdaysCommitted)
            {
                filter = filter.AndAlso(i =>
                    //ako nije sort mode, onda dodatno filtriraj za samo ne committane, i committane
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

        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeek()
        {
            await UpdateExpiredItemTasks();

            var groupedItems = await GetItemTasksGroupedByCommitDateForNextWeek();

            return groupedItems;
        }

        private async Task UpdateExpiredItemTasks()
        {
            var today = DateTime.Now.Date;

            Expression<Func<Entity.ItemTask, bool>> filter = x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date < today;

            var expiredItemTasks = await _itemTaskRepository.GetAllAsync(filter);

            if (expiredItemTasks.Any())
            {
                int newRowIndex = await GetNewItemTaskRowIndex(today);

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
        }
        private async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetItemTasksGroupedByCommitDateForNextWeek()
        {
            var today = DateTime.Now.Date;
            var endOfDayRange = today.AddDays(GlobalConstants.DaysRange);

            Expression<Func<Entity.ItemTask, bool>> filter = x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date >= today &&
                x.CommittedDate.Value.Date < endOfDayRange;

            var itemTasks = await _itemTaskRepository.GetAllAsync(filter, includeProperties: "Item");

            var groupedTasks = new Dictionary<DateTime, List<Entity.ItemTask>>();

            for (DateTime day = today; day < endOfDayRange; day = day.AddDays(1))
            {
                // commitani taskovi za specifičan dan
                var tasksForDay = itemTasks
                    .Where(t =>
                        t.CommittedDate.HasValue &&
                        t.CommittedDate.Value.Date == day
                        )
                    .OrderBy(t => t.RowIndex)
                    .ToList();

                groupedTasks.Add(day, tasksForDay);
            }

            return groupedTasks;
        }

        public async Task<ItemTask> GetItemTaskById(int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            //ostavljamo check ovdje u servisu, ako je entity null, onda on ne može zvat
            //nikakvu metodu da provjeri sam sebe jeli null
            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            return itemTaskEntity.Adapt<ItemTask>();
        }

        public async Task CreateItem(ItemTask itemTaskDomain)
        {
            IntervalCalculator.CalculateAndAssignDaysBetween(itemTaskDomain.Item);

            if (itemTaskDomain.DueDate != null)
            {
                itemTaskDomain.CommittedDate = itemTaskDomain.DueDate;
                itemTaskDomain.RowIndex = await GetNewItemTaskRowIndex(itemTaskDomain.CommittedDate);
            }

            await GetNewItemRowIndex(itemTaskDomain);

            var itemEntity = itemTaskDomain.Item.Adapt<Entity.Item>();
            var itemTaskEntity = itemTaskDomain.Adapt<Entity.ItemTask>();

            itemEntity.ItemTasks.Add(itemTaskEntity);

            _itemRepository.Add(itemEntity);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateItem(int itemTaskId, ItemTask updatedItemTask)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

            updatedItemTask.Adapt(domainItemTask);

            var newDueDate = domainItemTask.DueDate?.Date;
            var oldCommittedDate = itemTaskEntity.CommittedDate;

            IntervalCalculator.CalculateAndAssignDaysBetween(domainItemTask.Item);

            if (oldCommittedDate?.Date != newDueDate?.Date)
            {
                await HandleDueDateChange(itemTaskEntity, domainItemTask, oldCommittedDate, newDueDate);
            }

            domainItemTask.Adapt(itemTaskEntity);
            await _context.SaveChangesAsync();
        }

        private async Task HandleDueDateChange(Entity.ItemTask itemTaskEntity, ItemTask domainItemTask, DateTime? oldCommittedDate, DateTime? newDueDate)
        {
            domainItemTask.CommittedDate = newDueDate?.Date;

            await UpdateItemTaskRowIndexesIfDateProvided(oldCommittedDate, itemTaskEntity.RowIndex);

            if (newDueDate is not null)
            {
                domainItemTask.RowIndex = await GetNewItemTaskRowIndex(domainItemTask.CommittedDate);
            }
        }

        public async Task DeleteItem(int itemId)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId, "ItemTasks");

            CheckIfNull(itemEntity, $"Item with ID {itemId} not found.");

            _itemRepository.Delete(itemId);

            await UpdateRowIndexesForRemainingItems(itemEntity);

            //dohvaćam committan ItemTask za Item ako postoji
            var itemTaskEntity = itemEntity.ItemTasks.FirstOrDefault(x => x.CommittedDate != null && x.CompletionDate == null);

            //i ako je bio, za sve itemTaskove na taj dan im pomičem index
            if (itemTaskEntity != null)
            {
                await UpdateItemTaskRowIndexesIfDateProvided(itemTaskEntity.CommittedDate, itemTaskEntity.RowIndex);
            }

            await _context.SaveChangesAsync();
        }

        public async Task CommitItemTaskOrReturnToGroup(DateTime? newCommitDay, int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId);
            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

            var oldCommittedDate = domainItemTask.CommittedDate;

            if (oldCommittedDate?.Date != newCommitDay?.Date)
            {
                domainItemTask.CommittedDate = newCommitDay?.Date;

                await UpdateItemTaskRowIndexesIfDateProvided(oldCommittedDate, itemTaskEntity.RowIndex);

                if (newCommitDay is not null)
                {
                    domainItemTask.RowIndex = await GetNewItemTaskRowIndex(newCommitDay);
                }
                else
                {
                    domainItemTask.DueDate = null;
                }
            }

            domainItemTask.Adapt(itemTaskEntity);

            await _context.SaveChangesAsync();
        }

        public async Task ReorderItemInsideGroup(int itemId, int newIndex, bool recurring)
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

            RowIndexHelper.ManaulReorderRowIndexes<Entity.Item>(items, newIndex, currentIndex);

            itemToUpdate.RowIndex = newIndex;

            await _context.SaveChangesAsync();
        }

        public async Task ReorderItemTaskInsideGroup(int itemId, DateTime commitDate, int newIndex)
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

            RowIndexHelper.ManaulReorderRowIndexes<Entity.ItemTask>(itemsForDate, newIndex, currentIndex);

            itemToUpdate.RowIndex = newIndex;

            await _context.SaveChangesAsync();
        }

        public async Task CompleteItemTask(int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            itemTaskEntity.CompletionDate = DateTime.Now;

            await UpdateItemTaskRowIndexesIfDateProvided(itemTaskEntity.CommittedDate, itemTaskEntity.RowIndex);

            if (itemTaskEntity.Item.Recurring)
            {
                var domainItemTask = itemTaskEntity.Adapt<ItemTask>();

                //dobar primjer enkapsulacije biznis logike u domain klasu
                var newItemTask = domainItemTask.CreateNewRecurringTask();
                newItemTask.RowIndex = await GetNewItemTaskRowIndex(newItemTask.CommittedDate);

                var newItemTaskEntity = newItemTask.Adapt<Entity.ItemTask>();
                _itemTaskRepository.Add(newItemTaskEntity);
            }
            else
            {
                await UpdateRowIndexesForRemainingItems(itemTaskEntity.Item);

                itemTaskEntity.Item.Completed = true;
            }

            await _context.SaveChangesAsync();
        }

        private async Task GetNewItemRowIndex(ItemTask itemTaskDomain)
        {
            var maxRowIndexItem = await _itemRepository.GetFirstOrDefaultAsync(
                            x =>
                            !x.Completed &&
                            x.Recurring.Equals(itemTaskDomain.Item.Recurring),
                            q => q.OrderByDescending(x => x.RowIndex)
                        );

            itemTaskDomain.Item.RowIndex = maxRowIndexItem != null ? maxRowIndexItem.RowIndex + 1 : 0;
        }

        private async Task<int> GetNewItemTaskRowIndex(DateTime? compareDate)
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

        private async Task UpdateRowIndexesForRemainingItems(Entity.Item itemEntity)
        {
            await _itemRepository.UpdateBatchAsync(
                x => !x.Completed &&
                      x.Recurring.Equals(itemEntity.Recurring) &&
                      x.RowIndex > itemEntity.RowIndex,
                x => new Entity.Item { RowIndex = x.RowIndex - 1 }
            );
        }

        private async Task UpdateItemTaskRowIndexesIfDateProvided(DateTime? oldCommittedDate, int oldItemTaskRowIndex)
        {
            await _itemTaskRepository.UpdateBatchAsync(
                x => x.CompletionDate == null &&
                     x.CommittedDate.HasValue &&
                     oldCommittedDate.HasValue &&
                     x.CommittedDate.Value.Date == oldCommittedDate.Value.Date &&
                     x.RowIndex > oldItemTaskRowIndex,
                x => new Entity.ItemTask { RowIndex = x.RowIndex - 1 }
            );
        }
    }
}
