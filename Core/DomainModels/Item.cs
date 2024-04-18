namespace Core.DomainModels
{
    public class Item
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool Recurring { get; set; }
        public bool RenewOnDueDate { get; set; }
        public int DaysBetween { get; set; }
        public ICollection<CommittedItem> CommittedItems { get; set; }

        public Item()
        {
            CommittedItems = new List<CommittedItem>();
        }
    }

}
