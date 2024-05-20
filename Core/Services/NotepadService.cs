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

        public NotepadService(IGenericRepository<Entity.Notepad, int> notepadRepository)
        {
            _notepadRepository = notepadRepository;
        }

        public async Task<List<Notepad>> GetAllNotepadsAsync()
        {
            var notepads = await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex)
            );

            return notepads.Adapt<List<Notepad>>();
        }

        public async Task<Notepad> CreateNotepadAsync()
        {
            var notepadEntity = new Entity.Notepad();

            var maxRowIndexNotepad = await _notepadRepository.GetFirstOrDefaultAsync(
                //ne želi pustit OrderByDescending ako nemam trivijalni "true" filter
                x => true,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            //maxRowIndexNotepad može bit null ako kreiram prvi Notepad
            int startIndex = maxRowIndexNotepad != null ? maxRowIndexNotepad.RowIndex + 1 : 1;

            notepadEntity.RowIndex = startIndex;

            _notepadRepository.Add(notepadEntity);

            await _notepadRepository.SaveAsync();

            return notepadEntity.Adapt<Notepad>();
        }

        public async Task<Notepad> UpdateNotepadAsync(int notepadId, Notepad updatedNotepad)
        {
            var notepads = (await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex)
            )).ToList();

            var notepadEntity = notepads.FirstOrDefault(n => n.Id == notepadId);

            if (notepadEntity == null)
            {
                throw new NotFoundException($"Notepad with ID {notepadId} not found.");
            }

            int oldIndex = notepadEntity.RowIndex;
            int newIndex = updatedNotepad.RowIndex;

            // novi index koji postavljam ne smije biti manji od najmanjeg i veći od ukupnog broja Notepada
            if (newIndex < 1 || newIndex > notepads.Count())
            {
                throw new ArgumentOutOfRangeException(nameof(updatedNotepad.RowIndex), "Index out of range.");
            }

            // ako je novi index različiti od trenutačnog indexa
            if (newIndex != oldIndex)
            {
                // ako smanjujemo index Notepadu
                if (newIndex < oldIndex)
                {
                    foreach (var notepad in notepads.Where(n => n.RowIndex >= newIndex && n.RowIndex < oldIndex))
                    {
                        notepad.RowIndex++;
                    }
                }
                // ako povećavamo index Notepadu
                else
                {
                    foreach (var notepad in notepads.Where(n => n.RowIndex <= newIndex && n.RowIndex > oldIndex))
                    {
                        notepad.RowIndex--;
                    }
                }
                // nakon sortiranja ostalih, postavljamo novi index Notepadu
                notepadEntity.RowIndex = newIndex;
            }

            updatedNotepad.Adapt(notepadEntity);

            //u svakom slučaju želimo spremit ako su rađene neke druge promjene npr. update contenta
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

            int deletedIndex = notepadEntity.RowIndex;
            var affectedNotepads = await _notepadRepository.GetAllAsync(
                filter: n => n.RowIndex > deletedIndex,
                orderBy: x => x.OrderBy(n => n.RowIndex)
            );

            foreach (var notepad in affectedNotepads)
            {
                notepad.RowIndex--;
            }

            _notepadRepository.Delete(notepadId);
            await _notepadRepository.SaveAsync();
        }

    }
}
