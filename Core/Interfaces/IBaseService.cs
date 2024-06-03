namespace Core.Interfaces
{
    public interface IBaseService
    {
        void CheckIfNull<T>(T entity, string errorMessage) where T : class;
    }
}
