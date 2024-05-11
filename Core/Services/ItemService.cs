using Core.DomainModels;
using Core.Exceptions;
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

        public async Task<List<ItemTask>> GetOneTimeItemTasksAsync()
        {
            //one time taskovi, nisu izbrisani (znači nisu completed) i isto nisu već commitani za specifičan dan
            Expression<Func<Entity.ItemTask, bool>> filter = i => !i.Item.Recurring && !i.Item.Deleted && i.CommittedDate == null;

            var items = await _itemTaskRepository.GetAllAsync(filter);

            return items.Adapt<List<ItemTask>>();
        }

        public async Task<List<ItemTask>> GetRecurringItemTasksAsync()
        {
            //ponavljajući taskovi, mogu bit committed
            Expression<Func<Entity.ItemTask, bool>> filter = i => i.Item.Recurring && !i.Item.Deleted;

            var items = await _itemTaskRepository.GetAllAsync(filter);

            return items.Adapt<List<ItemTask>>();
        }

        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeekAsync()
        {
            //refaktor da se zove prije prvog fetcha u danu
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


        //pitat chat gpt jel ima smisla flow od frontend forme to backend ove metode što radim
        public async Task<ItemTask> CreateItemAsync(ItemTask itemTaskDomain)
        {
            var itemEntity = itemTaskDomain.Item.Adapt<Entity.Item>();

            //po biznis logici, mora po defaulta odma biti kreiran ItemTask
            var itemTaskEntity = itemTaskDomain.Adapt<Entity.ItemTask>();

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

            //include properties fali kao parametar u ovoj metodi
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");
            if (itemTaskEntity == null)
            {
                throw new NotFoundException($"ItemTask with ID {itemTaskId} not found.");
            }

            //eksplicitno update-amo itemTaskEntity sa novim vrijednostima .Adapt()
            //ovo neće transformirat entity objekt u domain, samo update se radi
            updatedItemTask.Adapt(itemTaskEntity);

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
            await _itemRepository.SaveAsync();
        }

        public async Task CommitItemTaskAsync(DateTime commitDay, int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId);

            //ako nije obrisanm radimo update
            if (itemTaskEntity is null)
            {
                throw new NotFoundException($"ItemTask with ID {itemTaskId} not found.");
            }

            //postavi committed date
            itemTaskEntity.CommittedDate = commitDay;

            //ne moramo eksplicitno Update zvat, sam će skužit zbog Get metode
            await _itemRepository.SaveAsync();
        }

        //jel ok ako ništa ne vraćamo kao i za delete?
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
                    //ako due date
                    if (itemTaskEntity.Item.RenewOnDueDate!.Value)
                    {
                        newItemTaskEntity.DueDate = itemTaskEntity.DueDate.Value.AddDays(itemTaskEntity.Item.DaysBetween!.Value);
                    }
                    //inače se obnavlja na completion date a ti he sad (npr. registracija auta)
                    else
                    {
                        newItemTaskEntity.DueDate = itemTaskEntity.CompletionDate.Value.AddDays(itemTaskEntity.Item.DaysBetween!.Value);
                    }
                }

                //ako DueDate nije null, onda ostaje null, ne postavljen


                _itemTaskRepository.Add(newItemTaskEntity);

                await _itemTaskRepository.SaveAsync();
            }
        }

        public async Task ReturnItemTaskToGroupAsync(int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId);
            if (itemTaskEntity == null)
            {
                throw new NotFoundException($"ItemTask with ID {itemTaskId} not found.");
            }

            //za sada samo task više nije comittan na određen datum i to je to
            //TO DO: u budućnosti ako ima daysBetween, preskočit će ovaj put
            //(npr.preskočit ću pranje balkona ovaj put)

            itemTaskEntity.CommittedDate = null;

            await _itemTaskRepository.SaveAsync();
        }

    }
}
