using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class Item : BaseEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } //original description for recurring

        public bool Recurring { get; set; }

        //ako je false, onda će se days between dodat na completion date
        //za novi DueDate
        //ako je true onda na DueDate
        public bool? RenewOnDueDate { get; set; }
        public int? DaysBetween { get; set; }

        public int? RowIndex { get; set; }

        // navigacijski property na djecu, nova lista se inicijalizira tako da nikad ne bude null
        public ICollection<ItemTask> ItemTasks { get; set; } = new List<ItemTask>();
    }
}
