namespace MyFeatures.DTO
{
    public class UpdateItemIndexDto
    {
        public int ItemId { get; set; }

        public int NewIndex { get; set; }

        public bool Recurring { get; set; }
    }
}
