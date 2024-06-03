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

        public void InitializeDates()
        {
            if (DueDate != null)
            {
                CommittedDate = DueDate;
            }
        }

        public void Complete()
        {
            CompletionDate = DateTime.UtcNow;
            RowIndex = null;
        }

        public ItemTask CreateNewRecurringTask()
        {
            var newItemTask = new ItemTask
            {
                ItemId = ItemId,
                Description = Description
            };

            //ako npr. se uzima riblje ulje ned., neovisno zakasnio dan-2
            //onda uvečavam na DueDate
            //ali ako je ponavljajući bez datuma, npr. posjet zubarici (ja određujem kad, nema DueDate)
            //onda se ne uvečava ništa

            //izvršavamo samo ako je DueDate i DaysBetween, ako je samo DueDate neće bit ponovno
            if (DueDate.HasValue && Item.DaysBetween.HasValue)
            {
                var daysBetween = Item.DaysBetween.Value;

                //ako je renewOnDueDate true, neće bit null jer postoji dajs between
                if (Item.RenewOnDueDate.Value)
                {
                    //na complete uvijek dodajem dane barem 1 put
                    newItemTask.DueDate = DueDate.Value.AddDays(daysBetween);

                    // i onda još dodaj dok ne bude dovoljno da taj datum bude veći od današnjeg dana (ako već nije)
                    while (newItemTask.DueDate.Value.Date <= DateTime.UtcNow.Date)
                    {
                        newItemTask.DueDate = newItemTask.DueDate.Value.AddDays(daysBetween);
                    }
                }
                //inače se obnavlja na completion date npr. registracija auta
                else
                {
                    newItemTask.DueDate = CompletionDate.Value.AddDays(daysBetween);
                }

                //odma committamo, ne čekamo ništa
                newItemTask.CommittedDate = newItemTask.DueDate;
            }

            return newItemTask;
        }
    }

}
