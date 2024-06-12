using Core.DomainModels;

namespace Core.Interfaces
{
    public interface INotepadService
    {
        Task<List<Notepad>> GetAllAsync();
        Task CreateAsync();
        Task UpdateAsync(int notepadId, Notepad updatedNotepad);
        Task DeleteAsync(int notepadId);
    }
}
