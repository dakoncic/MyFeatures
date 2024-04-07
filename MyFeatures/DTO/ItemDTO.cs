namespace MyFeatures.DTO
{
    public class ItemDTO
    {
        public int Id { get; set; }
        public string Description { get; set; }

        public bool Recurring { get; set; }

        public DateTime DueDate { get; set; }

        public bool Completed { get; set; }
    }
}
