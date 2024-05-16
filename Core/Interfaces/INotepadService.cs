using Core.DomainModels;

namespace Core.Interfaces
{
    public interface INotepadService
    {
        Task<List<Notepad>> GetAllNotepadsAsync();
    }
}
