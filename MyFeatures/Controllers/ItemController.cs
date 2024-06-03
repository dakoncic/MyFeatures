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

        [HttpPost("CreateItemTask")]
        //mogao sam i [Route("[action]")] pa iznad [HttpPost], isto je, ali je čišće u jednoj liniji
        public async Task<ActionResult> CreateItemTask(ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();

            await _itemService.CreateItemAsync(itemTaskDomain);

            return Ok();
        }

        [HttpPut("UpdateItemTask/{id}")]
        public async Task<ActionResult> UpdateItemTask(int id, ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();

            await _itemService.UpdateItemAsync(id, itemTaskDomain);

            return Ok();
        }

        [HttpDelete("DeleteItemTask/{id}")]
        public async Task<IActionResult> DeleteItemTask(int id)
        {
            await _itemService.DeleteItemAsync(id);
            return NoContent();
        }

        [HttpPost("CompleteItemTask/{itemTaskId}")]
        public async Task<IActionResult> CompleteItemTask(int itemTaskId)
        {
            await _itemService.CompleteItemTaskAsync(itemTaskId);

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
                    ItemTasks = group.Value.Select(itemTask => itemTask.Adapt<ItemTaskDto>()).ToList()
                })
                .ToList();

            return weekDayDtos;
        }
    }
}
