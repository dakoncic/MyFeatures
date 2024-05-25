using Core.DomainModels;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using MyFeatures.DTO;

namespace MyFeatures.Controllers
{
    [ApiController]
    //route attribut da ima kontrollera
    [Route("api/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpPost("CommitItemTask")]
        public async Task<ActionResult> CommitItemTask(CommitItemTaskDto itemTaskDto)
        {
            await _itemService.CommitItemTaskAsync(itemTaskDto.CommitDay, itemTaskDto.ItemTaskId);

            return Ok();
        }

        [HttpPost("UpdateItemTaskIndex")]
        public async Task<ActionResult> UpdateItemTaskIndex(UpdateItemTaskIndexDto updateItemTaskIndexDto)
        {
            await _itemService.UpdateItemTaskIndex(
                updateItemTaskIndexDto.ItemTaskId,
                updateItemTaskIndexDto.CommitDay,
                updateItemTaskIndexDto.NewIndex
                );

            return Ok();
        }

        [HttpPost("UpdateItemIndex")]
        public async Task<ActionResult> UpdateItemIndex(UpdateItemIndexDto updateItemIndexDto)
        {
            await _itemService.UpdateItemIndex(
                updateItemIndexDto.ItemId,
                updateItemIndexDto.NewIndex,
                updateItemIndexDto.Recurring
                );

            return Ok();
        }

        //vjv ne treba, obrisat kasnije
        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAll()
        {
            var items = await _itemService.GetAllItemsAsync();
            var ItemDtos = items.Adapt<List<ItemDto>>();

            return Ok(ItemDtos);
        }

        [HttpGet("GetOneTimeItemTasks")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetOneTimeItemTasks()
        {
            var itemTasks = await _itemService.GetActiveItemTasksAsync(false, false);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        [HttpGet("GetRecurringItemTasks")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetRecurringItemTasks()
        {
            var itemTasks = await _itemService.GetActiveItemTasksAsync(true, false);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        [HttpGet("GetOneTimeItemTasksWithWeekdays")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetOneTimeItemTasksWithWeekdays()
        {
            var itemTasks = await _itemService.GetActiveItemTasksAsync(false, true);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        [HttpGet("GetRecurringItemTasksWithWeekdays")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetRecurringItemTasksWithWeekdays()
        {
            var itemTasks = await _itemService.GetActiveItemTasksAsync(true, true);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        //bez {id} bi morao zvat metodu preko query parametra Get?id=123, 
        //a sa {id} mogu Get/1
        [HttpGet("GetItemTask/{id}")]
        public async Task<ActionResult<ItemTaskDto>> GetItemTask(int id)
        {
            var itemTask = await _itemService.GetItemTaskByIdAsync(id);

            var itemTaskDto = itemTask.Adapt<ItemTaskDto>();
            return Ok(itemTaskDto);
        }

        [HttpPost("Create")]
        //mogao sam i [Route("[action]")] pa iznad [HttpPost], isto je, ali je čišće u jednoj liniji
        public async Task<ActionResult<ItemTaskDto>> Create(ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();  // Map DTO to Domain Model

            var createdItem = await _itemService.CreateItemAsync(itemTaskDomain);
            var createdItemTaskDto = createdItem.Adapt<ItemTaskDto>();  // Map Domain Model back to DTO

            return Ok(createdItemTaskDto);
        }

        [HttpPut("Update/{id}")]
        public async Task<ActionResult<ItemTaskDto>> Update(int id, ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();
            var updatedItemTask = await _itemService.UpdateItemAsync(id, itemTaskDomain);

            var updatedItemTaskDto = updatedItemTask.Adapt<ItemTaskDto>();

            return Ok(updatedItemTaskDto);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _itemService.DeleteItemAsync(id);
            return NoContent();  // Indicate successful deletion with HTTP 204 No Content
        }

        [HttpPost("CompleteItemTask/{itemTaskId}")]
        public async Task<IActionResult> CompleteItemTask(int itemTaskId)
        {
            await _itemService.CompleteItemTaskAsync(itemTaskId);

            //provjerit dal vratit nešto drugo osim samo ok?
            return Ok();
        }

        [HttpGet("GetItemsForWeek")]
        public async Task<IEnumerable<WeekDayDto>> GetCommitedItemsForNextWeek()
        {
            var weekDays = new List<WeekDayDto>();

            var groupedItems = await _itemService.GetCommitedItemsForNextWeekAsync();
            var weekDayDtos = groupedItems
                .Select(group => new WeekDayDto
                {
                    WeekDayDate = group.Key,
                    //!fali mapster mapiranje za committed item
                    ItemTasks = group.Value.Select(itemTask => itemTask.Adapt<ItemTaskDto>()).ToList()
                })
                .ToList();

            return weekDayDtos;
        }
    }
}
