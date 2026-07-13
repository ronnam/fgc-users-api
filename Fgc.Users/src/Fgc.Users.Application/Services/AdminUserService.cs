using Fgc.MessageContracts.Events;
using Fgc.Users.Application.Helpers;
using Fgc.Users.Application.Interfaces;
using Fgc.Users.Domain.Entities;
using Fgc.Users.Domain.Exceptions;
using Fgc.Users.Domain.ValueObjects;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Fgc.Users.Application.Services
{
    public class AdminUserService
    {
        private readonly IAdminUserRepository _adminUserRepository;
        private readonly ILogger<AdminUserService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public AdminUserService(IAdminUserRepository adminUserRepository, ILogger<AdminUserService> logger, IPublishEndpoint publishEndpoint)
        {
            _adminUserRepository = adminUserRepository;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }
        public async Task<User> RegisterAsync(
            string name,
            string email,
            string password)
        {
            PasswordValidator.Validate(password);

            var passwordHash = PasswordHasher.Hash(password);
            var emailVo = Email.Create(email);

            var user = User.Create(
                name,
                emailVo,
                passwordHash
            );

            await _adminUserRepository.AddAsync(user);

            var userCreatedEvent = new UserCreatedEvent(user.Id, user.Name, user.Email.Value, DateTime.UtcNow);
            await _publishEndpoint.Publish(userCreatedEvent);

            _logger.LogInformation("Admin created user | UserId={UserId} | Email={Email}", user.Id,user.Email.Value);

            return user;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            _logger.LogInformation("Admin requested user list");

            return await _adminUserRepository.GetAllAsync();
        }
        public async Task<User?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Admin requested user details | UserId={UserId}",id);

            return await _adminUserRepository.GetByIdAsync(id);
        }

        public async Task<User> UpdateByAdminAsync(Guid userId, string role)
        {
            var user = await _adminUserRepository.GetByIdAsync(userId);

            if (user is null)
            {
                _logger.LogWarning(
                    "Admin attempted to update role of non-existing user | UserId={UserId}",
                    userId
                );
                throw new NotFoundException("User not found.");
            }

            if (role != "User" && role != "Admin")
                throw new ArgumentException("Invalid role.");

            user.UpdateRole(role);

            await _adminUserRepository.UpdateAsync(user);

            _logger.LogInformation(
                "Admin updated user role | UserId={UserId} | NewRole={Role}",
                user.Id,
                role
            );

            return user;
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _adminUserRepository.GetByIdAsync(id);

            if (user is null)
            {
                _logger.LogWarning("Admin attempted to delete non-existing user | UserId={UserId}", id);

                throw new NotFoundException("User not found.");
            }

            await _adminUserRepository.DeleteAsync(user);

            _logger.LogInformation("Admin deleted user | UserId={UserId} | Email={Email}",user.Id,user.Email.Value);
        }
    }
}

