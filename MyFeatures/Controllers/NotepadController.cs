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

        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<NotepadDto>>> GetAll()
        {
            var notepads = await _notepadService.GetAllNotepadsAsync();
            var NotepadDtos = notepads.Adapt<List<NotepadDto>>();

            return Ok(NotepadDtos);
        }

        [HttpPost("Create")]
        public async Task<ActionResult<NotepadDto>> Create()
        {
            var createdNotepad = await _notepadService.CreateNotepadAsync();
            var createdNotepadDto = createdNotepad.Adapt<NotepadDto>();

            return Ok(createdNotepadDto);
        }

        [HttpPut("Update/{id}")]
        public async Task<ActionResult<NotepadDto>> Update(int id, NotepadDto notepadDto)
        {
            var notepadDomain = notepadDto.Adapt<Notepad>();
            var updatedNotepad = await _notepadService.UpdateNotepadAsync(id, notepadDomain);

            var updatedNotepadDto = updatedNotepad.Adapt<NotepadDto>();

            return Ok(updatedNotepadDto);
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _notepadService.DeleteNotepadAsync(id);
            return NoContent();
        }
    }
}
