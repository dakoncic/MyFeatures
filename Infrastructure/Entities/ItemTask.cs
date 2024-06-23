using Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class ItemTask : BaseEntity<int>, IHasRowIndex
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ItemId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CommittedDate { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } //modified description for recurring

        [DataType(DataType.Date)]
        public DateTime? CompletionDate { get; set; }

        public int RowIndex { get; set; }

        public Item Item { get; set; }
    }
}
