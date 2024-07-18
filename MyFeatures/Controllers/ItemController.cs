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

        /// <summary>
        /// Creates a new item and associated task.
        /// </summary>
        /// <param name="itemTaskDto">The data transfer object containing the item and task details.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CreateItemAndTask")]
        //mogao sam i [Route("[action]")] pa iznad [HttpPost], isto je, ali je čišće u jednoj liniji
        public async Task<ActionResult> CreateItemAndTask(ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();

            await _itemService.CreateItemAndTask(itemTaskDomain);

            return Ok();
        }

        /// <summary>
        /// Retrieves an item task by its ID.
        /// </summary>
        /// <param name="id">The ID of the item task to retrieve.</param>
        /// <returns>An ActionResult containing the item task data transfer object.</returns>
        //bez {id} bi morao zvat metodu preko query parametra Get?id=123, 
        //a sa {id} mogu Get/1
        [HttpGet("GetItemTaskById/{id}")]
        public async Task<ActionResult<ItemTaskDto>> GetItemTaskById(int id)
        {
            var itemTask = await _itemService.GetItemTaskById(id);

            var itemTaskDto = itemTask.Adapt<ItemTaskDto>();
            return Ok(itemTaskDto);
        }

        /// <summary>
        /// Updates an existing item and associated task.
        /// </summary>
        /// <param name="id">The ID of the item and task to update.</param>
        /// <param name="itemTaskDto">The data transfer object containing the updated item and task details.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPut("UpdateItemAndTask/{id}")]
        public async Task<ActionResult> UpdateItemAndTask(int id, ItemTaskDto itemTaskDto)
        {
            var itemTaskDomain = itemTaskDto.Adapt<ItemTask>();

            await _itemService.UpdateItemAndTask(id, itemTaskDomain);

            return Ok();
        }

        /// <summary>
        /// Deletes an item and its associated tasks.
        /// </summary>
        /// <param name="id">The ID of the item to delete.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpDelete("DeleteItemAndTasks/{id}")]
        public async Task<IActionResult> DeleteItemAndTasks(int id)
        {
            await _itemService.DeleteItemAndTasks(id);
            return NoContent();
        }

        /// <summary>
        /// Marks an item task as complete.
        /// </summary>
        /// <param name="itemTaskId">The ID of the item task to mark as complete.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CompleteItemTask/{itemTaskId}")]
        public async Task<IActionResult> CompleteItemTask(int itemTaskId)
        {
            await _itemService.CompleteItemTask(itemTaskId);

            return Ok();
        }

        /// <summary>
        /// Commits an item task to a specific day or returns it to the group.
        /// </summary>
        /// <param name="itemTaskDto">The data transfer object containing the commit day and item task ID.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CommitItemTask")]
        public async Task<ActionResult> CommitItemTask(CommitItemTaskDto itemTaskDto)
        {
            await _itemService.CommitItemTaskOrReturnToGroup(itemTaskDto.CommitDay, itemTaskDto.ItemTaskId);

            return Ok();
        }

        /// <summary>
        /// Reorders an item within a group.
        /// </summary>
        /// <param name="updateItemIndexDto">The data transfer object containing the item ID, new index, and recurrence flag.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
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

        /// <summary>
        /// Reorders an item task within a group.
        /// </summary>
        /// <param name="updateItemTaskIndexDto">The data transfer object containing the item task ID, commit day, and new index.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
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

        /// <summary>
        /// Retrieves a list of one-time item tasks
        /// </summary>
        /// <returns>An ActionResult containing a list of one-time item task data transfer objects.</returns>
        [HttpGet("GetOneTimeItemTasks")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetOneTimeItemTasks()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(false, false);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        /// <summary>
        /// Retrieves a list of recurring item tasks.
        /// </summary>
        /// <returns>An ActionResult containing a list of recurring item task data transfer objects.</returns>
        [HttpGet("GetRecurringItemTasks")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetRecurringItemTasks()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(true, false);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        /// <summary>
        /// Retrieves a list of one-time item tasks including those committed for the next week.
        /// </summary>
        /// <returns>An ActionResult containing a list of one-time item task data transfer objects with weekdays.</returns>
        [HttpGet("GetOneTimeItemTasksWithWeekdays")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetOneTimeItemTasksWithWeekdays()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(false, true);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        /// <summary>
        /// Retrieves a list of recurring item tasks including those committed for the next week.
        /// </summary>
        /// <returns>An ActionResult containing a list of recurring item task data transfer objects with weekdays.</returns>
        [HttpGet("GetRecurringItemTasksWithWeekdays")]
        public async Task<ActionResult<IEnumerable<ItemTaskDto>>> GetRecurringItemTasksWithWeekdays()
        {
            var itemTasks = await _itemService.GetActiveItemTasks(true, true);
            var itemTasksDto = itemTasks.Adapt<List<ItemTaskDto>>();

            return Ok(itemTasksDto);
        }

        /// <summary>
        /// Retrieves committed items for the next week, grouped by day.
        /// </summary>
        /// <returns>A list of WeekDayDto objects containing the committed items grouped by day.</returns>
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
