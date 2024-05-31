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
            var notepads = await _notepadService.GetAllAsync();
            var notepadDtos = notepads.Adapt<List<NotepadDto>>();

            return Ok(notepadDtos);
        }

        [HttpPost("CreateNotepad")]
        public async Task<ActionResult<NotepadDto>> CreateNotepad()
        {
            var createdNotepad = await _notepadService.CreateAsync();
            var createdNotepadDto = createdNotepad.Adapt<NotepadDto>();

            return Ok(createdNotepadDto);
        }

        [HttpPut("UpdateNotepad/{id}")]
        public async Task<ActionResult<NotepadDto>> UpdateNotepad(int id, NotepadDto notepadDto)
        {
            var notepadDomain = notepadDto.Adapt<Notepad>();
            var updatedNotepad = await _notepadService.UpdateAsync(id, notepadDomain);

            var updatedNotepadDto = updatedNotepad.Adapt<NotepadDto>();

            return Ok(updatedNotepadDto);
        }

        [HttpDelete("DeleteNotepad/{id}")]
        public async Task<IActionResult> DeleteNotepad(int id)
        {
            await _notepadService.DeleteAsync(id);
            return Ok();
        }
    }
}
