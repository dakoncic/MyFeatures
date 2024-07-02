using Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class Notepad : BaseEntity<int>, IHasRowIndex
    {
        [Key]
        public int Id { get; set; }

        public string? Content { get; set; }

        [Required]
        public int? RowIndex { get; set; }
    }
}
