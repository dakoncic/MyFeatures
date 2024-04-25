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

        public async Task<List<Item>> GetOneTimeItemsAsync()
        {
            //one time itemi, nisu izbrisani (znači nisu completed) i isto nisu već commitani za specifičan dan
            Expression<Func<Entity.Item, bool>> filter = i => !i.Recurring && !i.Deleted && !i.ItemTasks.Any(ci => ci.CommittedDate != null);

            var items = await _itemRepository.GetAllAsync(filter);

            return items.Adapt<List<Item>>();
        }

        public async Task<List<Item>> GetRecurringItemsAsync()
        {
            //ponavljajući itemi, nisu izbrisani. *U budućnosti možda ne dohvaćat ako su commited, al za sad da
            Expression<Func<Entity.Item, bool>> filter = i => i.Recurring && !i.Deleted;

            var items = await _itemRepository.GetAllAsync(filter);

            return items.Adapt<List<Item>>();
        }

        //refaktorat ovo, napravit ItemTask service
        //i mapirat ovo u kontroleru, odma entitet jer nije bitno stvarno
        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetItemsForNextWeekAsync()
        {
            var groupedItems = await _itemTaskExtendedRepository.GetItemTasksGroupedByDueDateForNextWeek();

            return groupedItems;
        }


        public async Task<Item> GetItemByIdAsync(int itemId)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId);
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
                    Description = newItemDomain.Description,
                    DueDate = newItemDomain.DueDate
                };

                itemEntity.ItemTasks.Add(itemTaskEntity);
            }

            _itemRepository.Add(itemEntity);

            await _itemRepository.SaveAsync();

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

            var itemEntity = await _itemRepository.GetByIdAsync(itemId);
            if (itemEntity == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

            //ovo će pregazit entity property-e sa updatedItem objektom
            //tako da pripremimo itemEntity za update na bazu
            updatedItem.Adapt(itemEntity);

            //ne treba ako je iz istog DbContext scope-a (može samo get), ali neće štetit
            _itemRepository.Update(itemEntity);
            await _itemRepository.SaveAsync();
            return itemEntity.Adapt<Item>();
        }

        public async Task DeleteItemAsync(int itemId)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId);

            if (itemEntity == null)
            {
                throw new NotFoundException($"Item with ID {itemId} not found.");
            }

            _itemRepository.Delete(itemId);
            await _itemRepository.SaveAsync();
        }

        //refaktorat ovo, nije dobro
        public async Task<ItemTask> CommitItemAsync(Item item)
        {
            //prvo dohvaćamo ItemTask po: ItemID-u i gdje item još nije committan, to je source of truth
            //ako postoji DueDate, onda item već postoji
            Expression<Func<Entity.ItemTask, bool>> filter = itemTask =>
                itemTask.ItemId.Equals(item.Id) && itemTask.CommittedDate == null;

            var itemTaskEntity = await _itemTaskRepository.GetFirstOrDefaultAsync(filter);

            //ako ne postoji radimo insert
            if (itemTaskEntity is null)
            {
                itemTaskEntity = new Entity.ItemTask
                {
                    ItemId = item.Id,
                    CommittedDate = DateTime.UtcNow
                    //due date se ne postavlja ako originalno ni nije bio
                };

                _itemTaskRepository.Add(itemTaskEntity);
            }

            //postavi committed date neovisno jel novi ili postojeći
            itemTaskEntity.CommittedDate = DateTime.UtcNow;


            //ne moramo eksplicitno Update zvat, sam će skužit zbog Get metode
            await _itemRepository.SaveAsync();

            return itemTaskEntity.Adapt<ItemTask>();
        }

        public async Task<ItemTask> CompleteItemAsync(Item item)
        {
            //one time item se može complete-at koji ima commited date već i koji nema
            //recurring se može complete-at koji ima datum
            //recurring koji nema datum će se samo vratit u svoju listu (novi ItemTask sa committedDate = null)

            //DEFINIRAT:
            //što je Item a što ItemTask, gdje je koji prikazan, problem je sad što su ispremješani na UI
            //u listi itema i recurring itema koji nisu prešli gore u Weekdays

            //ako je na UI sve item task, onda bi kod kreiranja itema odma morao postojat, makar i prazan
            //pozitivno je što child ima direktnu referencu na parenta


            Expression<Func<Entity.ItemTask, bool>> filter = itemTask =>
                itemTask.ItemId.Equals(item.Id) && itemTask.CommittedDate == null;

            var itemTaskEntity = await _itemTaskRepository.GetFirstOrDefaultAsync(filter);






            return itemTaskEntity.Adapt<ItemTask>();
        }
    }
}
