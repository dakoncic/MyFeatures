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

        /// <summary>
        /// Retrieves all notepads.
        /// </summary>
        /// <returns>An ActionResult containing a list of NotepadDto objects.</returns>
        [HttpGet("GetAllNotepads")]
        public async Task<ActionResult<IEnumerable<NotepadDto>>> GetAllNotepads()
        {
            var notepads = await _notepadService.GetAll();
            var notepadDtos = notepads.Adapt<List<NotepadDto>>();

            return Ok(notepadDtos);
        }

        /// <summary>
        /// Creates a new notepad.
        /// </summary>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CreateNotepad")]
        public async Task<ActionResult<NotepadDto>> CreateNotepad()
        {
            await _notepadService.Create();

            return Ok();
        }

        /// <summary>
        /// Updates an existing notepad.
        /// </summary>
        /// <param name="id">The ID of the notepad to update.</param>
        /// <param name="notepadDto">The data transfer object containing the updated notepad details.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPut("UpdateNotepad/{id}")]
        public async Task<ActionResult<NotepadDto>> UpdateNotepad(int id, NotepadDto notepadDto)
        {
            var notepadDomain = notepadDto.Adapt<Notepad>();
            await _notepadService.Update(id, notepadDomain);

            return Ok();
        }

        /// <summary>
        /// Deletes a notepad.
        /// </summary>
        /// <param name="id">The ID of the notepad to delete.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpDelete("DeleteNotepad/{id}")]
        public async Task<IActionResult> DeleteNotepad(int id)
        {
            await _notepadService.Delete(id);
            return Ok();
        }
    }
}
