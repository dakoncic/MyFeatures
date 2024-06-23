using Core.DomainModels;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using MyFeatures.DTO;

namespace MyFeatures.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotepadController : ControllerBase
    {
        private readonly INotepadService _notepadService;

        public NotepadController(INotepadService notepadService)
        {
            _notepadService = notepadService;
        }

        [HttpGet("GetAllNotepads")]
        public async Task<ActionResult<IEnumerable<NotepadDto>>> GetAllNotepads()
        {
            var notepads = await _notepadService.GetAll();
            var notepadDtos = notepads.Adapt<List<NotepadDto>>();

            return Ok(notepadDtos);
        }

        [HttpPost("CreateNotepad")]
        public async Task<ActionResult<NotepadDto>> CreateNotepad()
        {
            await _notepadService.Create();

            return Ok();
        }

        [HttpPut("UpdateNotepad/{id}")]
        public async Task<ActionResult<NotepadDto>> UpdateNotepad(int id, NotepadDto notepadDto)
        {
            var notepadDomain = notepadDto.Adapt<Notepad>();
            await _notepadService.Update(id, notepadDomain);

            return Ok();
        }

        [HttpDelete("DeleteNotepad/{id}")]
        public async Task<IActionResult> DeleteNotepad(int id)
        {
            await _notepadService.Delete(id);
            return Ok();
        }
    }
}
