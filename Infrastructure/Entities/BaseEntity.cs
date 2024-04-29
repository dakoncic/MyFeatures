namespace Infrastructure.Entities
{
    public abstract class BaseEntity<TKeyType>
    {
        public TKeyType Id { get; set; }
    }
}
