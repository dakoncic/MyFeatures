using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infrastructure.Entities
{
    public class ItemTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Item")]
        public int ItemId { get; set; }

        //zadnji dan (datum) kad se nešto može izvršit,
        ///npr. zubar na točan datum
        //ili registracija auta (može i prije krajnjeg roka)
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CommittedDate { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } //modified description for recurring

        [DataType(DataType.Date)]
        public DateTime? CompletionDate { get; set; }

        // Navigation property na parenta
        public virtual Item Item { get; set; }
    }
}
