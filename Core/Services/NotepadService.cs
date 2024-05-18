using Core.DomainModels;
using Core.Exceptions;
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
            var notepads = await _notepadRepository.GetAllAsync();

            return notepads.Adapt<List<Notepad>>();
        }

        public async Task<Notepad> CreateNotepadAsync()
        {
            var notepadEntity = new Entity.Notepad();

            _notepadRepository.Add(notepadEntity);

            await _notepadRepository.SaveAsync();

            return notepadEntity.Adapt<Notepad>();
        }

        public async Task<Notepad> UpdateNotepadAsync(int notepadId, Notepad updatedNotepad)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);
            if (notepadEntity == null)
            {
                throw new NotFoundException($"Notepad with ID {notepadId} not found.");
            }

            updatedNotepad.Adapt(notepadEntity);

            await _notepadRepository.SaveAsync();

            return notepadEntity.Adapt<Notepad>();
        }

        public async Task DeleteNotepadAsync(int notepadId)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);

            if (notepadEntity == null)
            {
                throw new NotFoundException($"Notepad with ID {notepadId} not found.");
            }

            _notepadRepository.Delete(notepadId);
            await _notepadRepository.SaveAsync();
        }
    }
}
