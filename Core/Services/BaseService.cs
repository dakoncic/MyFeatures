using Core.Exceptions;
using Core.Interfaces;

namespace Core.Services
{
    public abstract class BaseService : IBaseService
    {
        public void CheckIfNull<T>(T entity, string errorMessage) where T : class
        {
            if (entity == null)
            {
                throw new NotFoundException(errorMessage);
            }
        }
    }

}
