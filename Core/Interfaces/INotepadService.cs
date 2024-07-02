using Core.DomainModels;

namespace Core.Interfaces
{
    public interface INotepadService
    {
        Task Create();
        Task Update(int notepadId, Notepad updatedNotepad);
        Task Delete(int notepadId);
        Task<List<Notepad>> GetAll();
    }
}
