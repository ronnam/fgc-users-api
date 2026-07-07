using Fgc.Users.Application.Helpers;
using Fgc.Users.Application.Interfaces;
using Fgc.Users.Domain.Entities;
using Fgc.Users.Domain.Exceptions;
using Fgc.Users.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using MassTransit;
using Fgc.MessageContracts.Events;

namespace Fgc.Users.Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;


        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IPublishEndpoint publishEndpoint)
        {
            _userRepository = userRepository;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<User> RegisterAsync(string name,string email,string password)
        {
            // Verificar se o email já está registrado. Procurando remover o Entity Framework que estava instalado por ocasião do monolito.
            var emailExists = await _userRepository.ExistsByEmailAsync(email);

            if (emailExists)
            {
                throw new ConflictException("Email already registered.");
            }

            PasswordValidator.Validate(password);

            var passwordHash = PasswordHasher.Hash(password);
            var emailVo = Email.Create(email);

            var user = User.Create(
                name,
                emailVo,
                passwordHash
            );
            await _userRepository.AddAsync(user);

            await _publishEndpoint.Publish(new UserCreatedEvent(user.Id, user.Name, user.Email.Value));

            _logger.LogInformation(
                "User registered successfully | UserId={UserId} | Email={Email}",
                user.Id,
                user.Email.Value
            );

            return user;
        }

        public async Task<User> UpdateUserAsync(
            Guid userId,
            string email,
            string password)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
            {
               _logger.LogWarning("Attempt to update non-existing user | UserId={UserId}", userId);

                throw new NotFoundException("User not found.");
            }

            PasswordValidator.Validate(password);

            user.UpdateEmail(Email.Create(email));
            user.UpdatePassword(PasswordHasher.Hash(password));

            await _userRepository.UpdateAsync(user);

            return user;
        }
    }
}
