using Core.DomainModels;

namespace Core.Interfaces
{
    public interface IUserService
    {
        Task<List<User>> GetAllPostsAsync();
        Task CreatePostAsync(User post);

    }
}
