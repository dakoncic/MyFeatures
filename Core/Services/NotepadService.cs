using Core.DomainModels;
using Core.Helpers;
using Core.Interfaces;
using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using Entity = Infrastructure.Entities;


namespace Core.Services
{
    public class NotepadService : BaseService, INotepadService
    {
        private readonly MyFeaturesDbContext _context;
        private readonly IGenericRepository<Entity.Notepad, int> _notepadRepository;

        public NotepadService(
            MyFeaturesDbContext context,
            IGenericRepository<Entity.Notepad, int> notepadRepository
            )
        {
            _context = context;
            _notepadRepository = notepadRepository;
        }

        public async Task<List<Notepad>> GetAll()
        {
            var notepads = await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex)
            );

            return notepads.Adapt<List<Notepad>>();
        }

        public async Task Create()
        {
            var notepadEntity = new Entity.Notepad();

            var maxRowIndexNotepad = await _notepadRepository.GetFirstOrDefaultAsync(
                //ne želi pustit OrderByDescending ako nemam trivijalni "true" filter
                x => true,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            int startIndex = maxRowIndexNotepad != null ? maxRowIndexNotepad.RowIndex!.Value + 1 : 1;

            notepadEntity.RowIndex = startIndex;

            _notepadRepository.Add(notepadEntity);

            await _context.SaveChangesAsync();
        }

        //ovo refaktorat da radim single item fetch ipak kao i svagdje drugdje
        public async Task Update(int notepadId, Notepad updatedNotepad)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);

            CheckIfNull(notepadEntity, $"Notepad with ID {notepadId} not found.");

            int currentIndex = notepadEntity.RowIndex!.Value;
            int newIndex = updatedNotepad.RowIndex!.Value;

            updatedNotepad.Adapt(notepadEntity);

            var notepads = await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex));

            if (newIndex < 1 || newIndex > notepads.Count())
            {
                throw new ArgumentOutOfRangeException(nameof(updatedNotepad.RowIndex), "Index out of range.");
            }

            if (newIndex != currentIndex)
            {
                var itemsToUpdate = notepads.Where(n => n.Id != notepadId).ToList();

                RowIndexHelper.ManaulReorderRowIndexes<Entity.Notepad>(itemsToUpdate, newIndex, currentIndex);

                notepadEntity.RowIndex = newIndex;
            }

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int notepadId)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);

            CheckIfNull(notepadEntity, $"Notepad with ID {notepadId} not found.");

            int deletedIndex = notepadEntity.RowIndex!.Value;
            var affectedNotepads = await _notepadRepository.GetAllAsync(
                filter: n => n.RowIndex > deletedIndex,
                orderBy: x => x.OrderBy(n => n.RowIndex)
            );

            //*batch update ovdje najvjv.
            foreach (var notepad in affectedNotepads)
            {
                notepad.RowIndex--;
            }

            _notepadRepository.Delete(notepadId);
            await _context.SaveChangesAsync();
        }
    }
}
