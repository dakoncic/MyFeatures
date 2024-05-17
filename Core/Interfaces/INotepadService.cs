using Core.DomainModels;

namespace Core.Interfaces
{
    public interface INotepadService
    {
        Task<List<Notepad>> GetAllNotepadsAsync();
        Task<Notepad> CreateNotepadAsync();
        Task<Notepad> UpdateNotepadAsync(int notepadId, Notepad updatedNotepad);
        Task DeleteNotepadAsync(int notepadId);
    }
}
