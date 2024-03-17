using Core.DomainModels;
using Core.Interfaces;
using Infrastructure.Interfaces.IRepository;
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class UserService : IUserService
    {
        private readonly IGenericCrudService<Entity.User, int> _crudService;

        public UserService(IGenericCrudService<Entity.User, int> crudService)
        {
            _crudService = crudService;
        }

        public async Task<List<User>> GetAllPostsAsync()
        {
            var entities = await _crudService.GetAllAsync();
            return entities.Select(ToDomainModel).ToList();
        }

        public async Task CreatePostAsync(User post)
        {
            var entity = ToEntity(post);
            _crudService.Add(entity);
            await _crudService.SaveAsync();
        }

        private User ToDomainModel(Entity.User entity)
        {
            return new User
            {
                Id = entity.Id,
                Content = entity.Content
            };
        }

        private Entity.User ToEntity(User model)
        {
            return new Entity.User
            {
                Id = model.Id,
                Content = model.Content
            };
        }
    }
}
