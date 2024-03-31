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
    }
}
