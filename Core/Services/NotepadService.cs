using Core.DomainModels;
using Core.Interfaces;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class NotepadService : INotepadService
    {
        private readonly IGenericRepository<Entity.Notepad, int> _notepadRepository;

        public NotepadService(
            IGenericRepository<Entity.Notepad, int> notepadRepository
            )
        {
            _notepadRepository = notepadRepository;
        }

        public async Task<List<Notepad>> GetAllNotepadsAsync()
        {
            var items = await _notepadRepository.GetAllAsync();

            return items.Adapt<List<Notepad>>();
        }

        public async Task<Notepad> CreateNotepadAsync()
        {
            var notepadEntity = new Entity.Notepad();

            _notepadRepository.Add(notepadEntity);

            await _notepadRepository.SaveAsync();

            return notepadEntity.Adapt<Notepad>();
        }
    }
}
