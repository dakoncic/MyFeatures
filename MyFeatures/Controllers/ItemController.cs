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


        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAll()
        {
            var items = await _itemService.GetAllItemsAsync();
            var ItemDtos = items.Adapt<List<ItemDto>>();

            return Ok(ItemDtos);
        }


        //bez {id} bi morao zvat metodu preko query parametra Get?id=123, 
        //a sa {id} mogu Get/1
        [HttpGet("Get/{id}")]
        public async Task<ActionResult<ItemDto>> Get(int id)
        {
            var item = await _itemService.GetItemByIdAsync(id);

            var ItemDto = item.Adapt<ItemDto>();
            return Ok(ItemDto);
        }

        [HttpPost("Create")]
        //mogao sam i [Route("[action]")] pa iznad [HttpPost], isto je, ali je čišće u jednoj liniji
        public async Task<ActionResult<ItemDto>> Create(ItemDto ItemDto)
        {
            var itemDomain = ItemDto.Adapt<Item>();  // Map DTO to Domain Model

            var createdItem = await _itemService.CreateItemAsync(itemDomain);
            var createdItemDto = createdItem.Adapt<ItemDto>();  // Map Domain Model back to DTO

            return Ok(createdItemDto);
        }


        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, ItemDto ItemDto)
        {
            var itemDomain = ItemDto.Adapt<Item>();
            var updatedItem = await _itemService.UpdateItemAsync(id, itemDomain);

            var updatedItemDto = updatedItem.Adapt<ItemDto>(); // update neće vraćati null, bacit će exception

            return Ok(updatedItemDto);
        }



        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _itemService.DeleteItemAsync(id);
            return NoContent();  // Indicate successful deletion with HTTP 204 No Content
        }


        [HttpPost("Complete/{id}")]
        public void Complete(int id, ItemDto item)
        {
        }

        [HttpGet("GetItemsForWeek")]
        public IEnumerable<WeekDayDto> GetItemsForWeek()
        {
            var weekDays = new List<WeekDayDto>();

            //znači sljedećih 7 dana uključujući danas
            //for (int i = 0; i < 7; i++)
            //{
            //    WeekDayDto day = new WeekDayDto
            //    {
            //        WeekDayDate = DateTime.UtcNow.AddDays(i),
            //        Items = new List<ItemDto>
            //        {
            //            new ItemDto { Id = i * 10 + 1, Description = $"Task {i * 10 + 1}", Recurring = false, DueDate = DateTime.UtcNow.AddDays(i), Completed = false },
            //            new ItemDto { Id = i * 10 + 2, Description = $"Task {i * 10 + 2}", Recurring = true, DueDate = DateTime.UtcNow.AddDays(i), Completed = true }
            //        }
            //    };
            //    weekDays.Add(day);
            //}


            return weekDays;
        }
    }
}
