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

        [HttpPost("CreateItemAndTask")]
        //mogao sam i [Route("[action]")] pa iznad [HttpPost], isto je, ali je čišće u jednoj liniji
        public async Task<ActionResult> CreateItemAndTask(ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();

            await _itemService.CreateItemAndTask(itemTaskDomain);

            return Ok();
        }

        //bez {id} bi morao zvat metodu preko query parametra Get?id=123, 
        //a sa {id} mogu Get/1
        [HttpGet("GetItemTaskById/{id}")]
        public async Task<ActionResult<ItemTaskDto>> GetItemTaskById(int id)
        {
            var itemTask = await _itemService.GetItemTaskById(id);

            var itemTaskDto = itemTask.Adapt<ItemTaskDto>();
            return Ok(itemTaskDto);
        }

        [HttpPut("UpdateItemAndTask/{id}")]
        public async Task<ActionResult> UpdateItemAndTask(int id, ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();

            await _itemService.UpdateItemAndTask(id, itemTaskDomain);

            return Ok();
        }

        [HttpDelete("DeleteItemAndTasks/{id}")]
        public async Task<IActionResult> DeleteItemAndTasks(int id)
        {
            await _itemService.DeleteItemAndTasks(id);
            return NoContent();
        }

        [HttpPost("CompleteItemTask/{itemTaskId}")]
        public async Task<IActionResult> CompleteItemTask(int itemTaskId)
        {
            await _itemService.CompleteItemTask(itemTaskId);

            return Ok();
        }

        [HttpPost("CommitItemTask")]
        public async Task<ActionResult> CommitItemTask(CommitItemTaskDto itemTaskDto)
        {
            await _itemService.CommitItemTaskOrReturnToGroup(itemTaskDto.CommitDay, itemTaskDto.ItemTaskId);

            return Ok();
        }

        [HttpPost("ReorderItemInsideGroup")]
        public async Task<ActionResult> ReorderItemInsideGroup(UpdateItemIndexDto updateItemIndexDto)
        {
            await _itemService.ReorderItemInsideGroup(
                updateItemIndexDto.ItemId,
                updateItemIndexDto.NewIndex,
                updateItemIndexDto.Recurring
                );

            return Ok();
        }

        [HttpPost("ReorderItemTaskInsideGroup")]
        public async Task<ActionResult> ReorderItemTaskInsideGroup(UpdateItemTaskIndexDto updateItemTaskIndexDto)
        {
            await _itemService.ReorderItemTaskInsideGroup(
                updateItemTaskIndexDto.ItemTaskId,
                updateItemTaskIndexDto.CommitDay,
                updateItemTaskIndexDto.NewIndex
                );

            return Ok();
        }

        [HttpGet("GetOneTimeItemTasks")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetOneTimeItemTasks()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(false, false);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        [HttpGet("GetRecurringItemTasks")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetRecurringItemTasks()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(true, false);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        [HttpGet("GetOneTimeItemTasksWithWeekdays")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetOneTimeItemTasksWithWeekdays()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(false, true);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        [HttpGet("GetRecurringItemTasksWithWeekdays")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetRecurringItemTasksWithWeekdays()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(true, true);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        [HttpGet("GetCommitedItemsForNextWeek")]
        public async Task<IEnumerable<WeekDayDto>> GetCommitedItemsForNextWeek()
        {
            var weekDays = new List<WeekDayDto>();

            var groupedItems = await _itemService.GetCommitedItemsForNextWeek();
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
