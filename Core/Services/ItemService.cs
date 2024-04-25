using Core.DomainModels;
using Core.Exceptions;
using Core.Interfaces;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using System.Linq.Expressions;
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class ItemService : IItemService
    {
        private readonly IGenericCrudService<Entity.Item, int> _crudService;
        private readonly IItemTaskRepository _itemTaskRepository;

        public ItemService(
            IGenericCrudService<Entity.Item, int> crudService,
            IItemTaskRepository itemTaskRepository
            )
        {
            _crudService = crudService;
            _itemTaskRepository = itemTaskRepository;
        }

        //vjerojatno se neće koristiti nigdje
        public async Task<List<Item>> GetAllItemsAsync()
        {
            var items = await _crudService.GetAllAsync();

            return items.Adapt<List<Item>>();
        }

        public async Task<List<Item>> GetOneTimeItemsAsync()
        {
            //one time itemi, nisu izbrisani (znači nisu completed) i isto nisu već commitani za specifičan dan
            Expression<Func<Entity.Item, bool>> filter = i => !i.Recurring && !i.Deleted && !i.ItemTasks.Any(ci => ci.CommittedDate != null);

            var items = await _crudService.GetAllAsync(filter);

            return items.Adapt<List<Item>>();
        }

        public async Task<List<Item>> GetRecurringItemsAsync()
        {
            //ponavljajući itemi, nisu izbrisani. *U budućnosti možda ne dohvaćat ako su commited, al za sad da
            Expression<Func<Entity.Item, bool>> filter = i => i.Recurring && !i.Deleted;

            var items = await _crudService.GetAllAsync(filter);

            return items.Adapt<List<Item>>();
        }

        //refaktorat ovo, napravit ItemTask service
        //i mapirat ovo u kontroleru, odma entitet jer nije bitno stvarno
        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetItemsForNextWeekAsync()
        {
            var groupedItems = await _itemTaskRepository.GetItemTasksGroupedByDueDateForNextWeek();

            return groupedItems;
        }


        public async Task<Item> GetItemByIdAsync(int itemId)
        {
            var itemEntity = await _crudService.GetByIdAsync(itemId);
            if (itemEntity == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

            return itemEntity.Adapt<Item>();
        }



        public async Task<Item> CreateItemAsync(NewItem newItemDomain)
        {
            //mapiram ono što mogu s NewItem u Item entity.
            var itemEntity = newItemDomain.Adapt<Entity.Item>();

            //ako je odabran DueDate, odma kreiraj ItemTask za taj parent
            if (newItemDomain.DueDate is not null)
            {
                var itemTaskEntity = new Entity.ItemTask
                {
                    DueDate = newItemDomain.DueDate
                };

                itemEntity.ItemTasks.Add(itemTaskEntity);
            }

            _crudService.Add(itemEntity);

            await _crudService.SaveAsync();

            //itemEntity sadrži samo nove promjene koje su se dogodile usred .SaveAsync()
            //znači ne mora novi poseban get request nakon Save.
            //ovdje vraćam samo parenta bez osvježene djece
            //ovo je ok ako mi ne treba za ništa specijalno, mogao sam skroz drugo vraćat
            return itemEntity.Adapt<Item>();
        }

        public async Task<Item> UpdateItemAsync(int itemId, Item updatedItem)
        {
            //radi se prvo get a ne odma update, zbog concurrency npr.
            //da ne može commitat na bazu ako je netko drugi u
            //međuvremenu save-ao a mi onda immao krivi row version
            //ili "you might have a rule that says a transaction can only be updated if it is in a pending state"
            //znači ima neki uvjet, neki "if" za neki property

            var itemEntity = await _crudService.GetByIdAsync(itemId);
            if (itemEntity == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

            //ovo će pregazit entity property-e sa updatedItem objektom
            //tako da pripremimo itemEntity za update na bazu
            updatedItem.Adapt(itemEntity);

            //ne treba ako je iz istog DbContext scope-a (može samo get), ali neće štetit
            _crudService.Update(itemEntity);
            await _crudService.SaveAsync();
            return itemEntity.Adapt<Item>();
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var itemEntity = await _crudService.GetByIdAsync(itemId);

            if (itemEntity == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

            _crudService.Delete(itemId);
            await _crudService.SaveAsync();
        }

        public async Task CommitItemAsync(int itemId, DateTime committedDay)
        {
            //item u one time ili repeating listi se može assignat na određen dan u tjednu
            //odabere se dan na UI i na click item u listi se pošalje ID taska
            //i napravi se create u tablici commited item-a
            //item može odma imati due date ili ne mora

            //var itemEntity = itemDomain.Adapt<Entity.Item>();

            //_crudService.Add(itemEntity);
            //await _crudService.SaveAsync();

            ////itemEntity sadrži nove potencijalne promjene koje su se dogodile usred .SaveAsync()
            ////znači ne mora novi poseban get request nakon Save.
            //return itemEntity.Adapt<Item>();

            var itemEntity = await _crudService.GetByIdAsync(itemId);

            if (itemEntity == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

            _crudService.Delete(itemId);
            await _crudService.SaveAsync();
        }


    }
}
