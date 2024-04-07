using Microsoft.AspNetCore.Mvc;
using MyFeatures.DTO;

namespace MyFeatures.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemController : ControllerBase
    {
        [HttpGet("GetAll")]
        public IEnumerable<ItemDTO> GetAll()
        {
            var items = new List<ItemDTO>
            {
                new ItemDTO { Id = 1, Description = "Item 1 Description" },
                new ItemDTO { Id = 2, Description = "Item 2 Description" },
                new ItemDTO { Id = 3, Description = "Item 3 Description" }
            };


            return items;
        }

        //bez {id} bi morao zvat metodu preko query parametra Get?id=123, 
        //a sa {id} mogu Get/1
        [HttpGet("Get/{id}")]
        public ItemDTO Get(int id)
        {
            return null;
        }

        [HttpPost("Create")]
        public void Create(ItemDTO item)
        {
        }

        [HttpPut("Update/{id}")]
        public void Update(int id, ItemDTO item)
        {
        }

        [HttpDelete("Delete/{id}")]
        public void Delete(int id)
        {
        }

        [HttpPost("Complete/{id}")]
        public void Complete(int id, ItemDTO item)
        {
        }

        [HttpGet("GetItemsForWeek")]
        public IEnumerable<WeekDayDTO> GetItemsForWeek()
        {
            var weekDays = new List<WeekDayDTO>();

            //znači sljedećih 7 dana uključujući danas
            for (int i = 0; i < 7; i++)
            {
                WeekDayDTO day = new WeekDayDTO
                {
                    WeekDayDate = DateTime.UtcNow.AddDays(i),
                    Items = new List<ItemDTO>
                    {
                        new ItemDTO { Id = i * 10 + 1, Description = $"Task {i * 10 + 1}", Recurring = false, DueDate = DateTime.UtcNow.AddDays(i), Completed = false },
                        new ItemDTO { Id = i * 10 + 2, Description = $"Task {i * 10 + 2}", Recurring = true, DueDate = DateTime.UtcNow.AddDays(i), Completed = true }
                    }
                };
                weekDays.Add(day);
            }


            return weekDays;
        }
    }
}
