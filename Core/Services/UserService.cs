using Core.DomainModels;
using Core.Interfaces;
using Infrastructure.DAL;
using Microsoft.EntityFrameworkCore;
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class UserService : IUserService
    {
        private readonly AzureCICDDbContext _context;

        public UserService(AzureCICDDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllPostsAsync()
        {
            var entities = await _context.Users.ToListAsync();

            return MapEntitiesToDomainModels(entities);
        }

        public async Task CreatePostAsync(User post)
        {
            var entity = ToEntity(post);

            _context.Users.Add(entity);

            await _context.SaveChangesAsync();
        }

        public List<User> MapEntitiesToDomainModels(List<Entity.User> entities)
        {
            return entities.Select(entity => new User
            {
                Id = entity.Id,
                Content = entity.Content
            }).ToList();
        }

        public User ToDomainModel(Entity.User entity)
        {
            return new User
            {
                Id = entity.Id,
                Content = entity.Content
            };
        }

        public Entity.User ToEntity(User entity)
        {
            return new Entity.User
            {
                Id = entity.Id,
                Content = entity.Content
            };
        }

    }

}
