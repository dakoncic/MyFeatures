using Core.Enum;

namespace Core.DomainModels
{
    public class ItemTask
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public DateTime? CommittedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Description { get; set; }
        public DateTime? CompletionDate { get; set; }
        public int? RowIndex { get; set; }
        public Item Item { get; set; }

        public ItemTask CreateNewRecurringTask()
        {
            var newItemTask = new ItemTask
            {
                ItemId = ItemId,
                Description = Item.Description
            };

            if (DueDate is not null && Item.IntervalValue is not null)
            {
                var daysBetween = CalculateDaysBetween(Item);

                //ako je renewOnDueDate true, neće bit null jer postoji days between
                //npr. vit D svake ned.
                if (Item.RenewOnDueDate!.Value)
                {
                    //na complete uvijek dodajem dane barem 1 put
                    newItemTask.DueDate = DueDate.Value.AddDays(daysBetween);

                    // i onda još dodaj dok ne bude dovoljno da taj datum bude veći od današnjeg dana (ako već nije)
                    while (newItemTask.DueDate.Value.Date <= DateTime.Now.Date)
                    {
                        newItemTask.DueDate = newItemTask.DueDate.Value.AddDays(daysBetween);
                    }
                }
                //inače se obnavlja na completion date npr. registracija auta
                else
                {
                    newItemTask.DueDate = DateTime.Now.AddDays(daysBetween);
                }

                //odma committamo
                newItemTask.CommittedDate = newItemTask.DueDate;
            }

            return newItemTask;
        }

        private int CalculateDaysBetween(Item item)
        {
            if (item.IntervalType!.Value == IntervalType.Months)
            {
                return CalculateDaysBetweenForMonths(item.IntervalValue!.Value);
            }
            else
            {
                return item.IntervalValue!.Value;
            }
        }

        private int CalculateDaysBetweenForMonths(int months)
        {
            var startDate = DateTime.Now;
            var endDate = startDate.AddMonths(months);
            return (endDate - startDate).Days;
        }
    }

}
