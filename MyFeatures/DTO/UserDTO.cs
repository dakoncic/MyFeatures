using System.ComponentModel.DataAnnotations;

namespace MyFeatures.DTO
{
    public class UserDTO
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

    }
}
