namespace Core.DomainModels
{
    public class NewItem
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public bool Recurring { get; set; }
        public bool RenewOnDueDate { get; set; }
        public int DaysBetween { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
