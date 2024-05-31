using Core.DomainModels;

namespace Core.Interfaces
{
    public interface INotepadService
    {
        Task<List<Notepad>> GetAllAsync();
        Task<Notepad> CreateAsync();
        Task<Notepad> UpdateAsync(int notepadId, Notepad updatedNotepad);
        Task DeleteAsync(int notepadId);
    }
}
