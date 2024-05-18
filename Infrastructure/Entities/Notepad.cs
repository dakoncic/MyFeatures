using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class Notepad : BaseEntity<int>
    {
        [Key]
        public int Id { get; set; }

        public string? Content { get; set; }
    }
}
