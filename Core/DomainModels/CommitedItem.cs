namespace Core.DomainModels
{
    public class CommittedItem
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public DateTime? CommittedDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Description { get; set; }
        public DateTime? CompletionDate { get; set; }
        public Item Item { get; set; }
    }

}
