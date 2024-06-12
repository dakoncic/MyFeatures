using Core.DomainModels;
using Core.Helpers;
using Core.Interfaces;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using Entity = Infrastructure.Entities;


namespace Core.Services
{
    public class NotepadService : BaseService, INotepadService
    {
        private readonly IGenericRepository<Entity.Notepad, int> _notepadRepository;

        public NotepadService(IGenericRepository<Entity.Notepad, int> notepadRepository)
        {
            _notepadRepository = notepadRepository;
        }

        public async Task<List<Notepad>> GetAllAsync()
        {
            var notepads = await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex)
            );

            return notepads.Adapt<List<Notepad>>();
        }

        public async Task CreateAsync()
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
        }

        //ovo refaktorat da radim single item fetch ipak kao i svagdje drugdje
        public async Task UpdateAsync(int notepadId, Notepad updatedNotepad)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);

            CheckIfNull(notepadEntity, $"Notepad with ID {notepadId} not found.");

            int currentIndex = notepadEntity.RowIndex;
            int newIndex = updatedNotepad.RowIndex;

            updatedNotepad.Adapt(notepadEntity);

            var notepads = await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex));

            // novi index koji postavljam ne smije biti manji od najmanjeg i veći od ukupnog broja Notepada
            if (newIndex < 1 || newIndex > notepads.Count())
            {
                throw new ArgumentOutOfRangeException(nameof(updatedNotepad.RowIndex), "Index out of range.");
            }

            // ako je novi index različiti od trenutačnog indexa
            if (newIndex != currentIndex)
            {
                var itemsToUpdate = notepads.Where(n => n.Id != notepadId).ToList();

                RowIndexHelper.UpdateRowIndexes<Entity.Notepad>(itemsToUpdate, newIndex, currentIndex);

                // nakon sortiranja ostalih, postavljamo novi index Notepadu
                notepadEntity.RowIndex = newIndex;
            }

            //u svakom slučaju želimo spremit ako su rađene neke druge promjene npr. update contenta
            await _notepadRepository.SaveAsync();
        }

        public async Task DeleteAsync(int notepadId)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);

            CheckIfNull(notepadEntity, $"Notepad with ID {notepadId} not found.");

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
