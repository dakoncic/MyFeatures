using Core.DomainModels;

namespace Core.Interfaces
{
    public interface INotepadService
    {
        Task<List<Notepad>> GetAll();
        Task Create();
        Task Update(int notepadId, Notepad updatedNotepad);
        Task Delete(int notepadId);
    }
}
