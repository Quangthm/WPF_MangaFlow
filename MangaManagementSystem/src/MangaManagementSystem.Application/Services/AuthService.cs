using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Mappers;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Services
{
    public class AuthService : IAuthService
    {
        /// <summary>
        /// Default role name assigned to new registrations (Google signup and standard OTP signup).
        /// Resolved by name from auth.Roles at runtime.
        /// </summary>
        private const string DefaultRegistrationRoleName = "Mangaka";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IOtpCacheService otpCacheService,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _otpCacheService = otpCacheService;
            _logger = logger;
        }

        public async Task<bool> SendRegistrationOtpAsync(RegisterDto request)
        {
            var normalizedEmail = NormalizeEmail(request.Email);
            var trimmedUsername = request.Username.Trim();

            // Only check username availability against DB
            if (await _unitOfWork.Users.GetByUsernameAsync(trimmedUsername) is not null)
                throw new InvalidOperationException("This username is already taken.");

            var otp = GenerateOtp();
            var cachedRequest = request with
            {
                Email = normalizedEmail,
                Username = trimmedUsername
            };

            _otpCacheService.StoreRegistrationOtp(normalizedEmail, otp, cachedRequest);
            await _emailService.SendOtpEmailAsync(normalizedEmail, otp);

            return true;
        }

        public async Task<UserDto> CompleteRegistrationWithOtpAsync(
            string email, string otp,
            byte[]? portfolioFileBytes = null,
            string? portfolioFileName = null,
            string? portfolioContentType = null)
        {
            var normalizedEmail = NormalizeEmail(email);
            var pendingRegistration = _otpCacheService.TryValidateAndRemoveRegistrationOtp(normalizedEmail, otp);

            if (pendingRegistration is null)
                throw new InvalidOperationException("The verification code is invalid or has expired.");

            await EnsureEmailAndUsernameAvailableAsync(normalizedEmail, pendingRegistration.Username);

            var roleName = pendingRegistration.RoleName;
            var passwordHash = _passwordHasher.HashPassword(pendingRegistration.Password);

            var roles = await _unitOfWork.Roles.GetAllAsync();
            var role = roles.FirstOrDefault(r => r.RoleName == roleName)
                ?? throw new InvalidOperationException($"Role '{roleName}' not found.");

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                RoleId = role.RoleId,
                Username = pendingRegistration.Username,
                PasswordHash = passwordHash
            };

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            var created = await _unitOfWork.Users.GetByIdAsync(newUser.UserId);
            if (created is null)
                throw new InvalidOperationException("Failed to load created user.");

            return created.ToDto();
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto request)
        {
            var loginIdentifier = ResolveLoginIdentifier(request.UsernameOrEmail);
            var user = await _unitOfWork.Users.GetByUsernameOrEmailAsync(loginIdentifier);

            if (user is null)
            {
                _logger.LogWarning("Login failed: User not found for identifier {LoginIdentifier}", loginIdentifier);
                return new AuthResultDto(false, null, null, "Invalid credentials");
            }

            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user {UserId} ({Username})", user.UserId, user.Username);
                return new AuthResultDto(false, null, null, "Invalid credentials");
            }

            var authResult = BuildSuccessfulAuthResult(user, "Login failed");

            if (authResult.Succeeded)
            {
                _logger.LogInformation("Login succeeded for user {UserId} ({Username}) with role {RoleName}",
                    user.UserId, user.Username, authResult.RoleName);
            }

            return authResult;
        }

        public async Task<GoogleSignupCallbackResult> ProcessGoogleSignupCallbackAsync(
            string email, string? googleDisplayName)
        {
            var normalizedEmail = NormalizeEmail(email);
            var username = await GenerateUniqueUsernameAsync(googleDisplayName, normalizedEmail);

            // Check if user already exists by username
            var existingUser = await _unitOfWork.Users.GetByUsernameAsync(username);
            if (existingUser is not null)
            {
                var userDto = existingUser.ToDto();
                return new GoogleSignupCallbackResult(
                    GoogleSignupFlow.ActiveUserLogin,
                    normalizedEmail,
                    userDto,
                    userDto.RoleName);
            }

            var passwordHash = _passwordHasher.HashPassword(Guid.NewGuid().ToString("N") + "!Aa1");

            var roles = await _unitOfWork.Roles.GetAllAsync();
            var role = roles.FirstOrDefault(r => r.RoleName == DefaultRegistrationRoleName)
                ?? throw new InvalidOperationException($"Role '{DefaultRegistrationRoleName}' not found.");

            var newUser = new User
            {
                UserId = Guid.NewGuid(),
                RoleId = role.RoleId,
                Username = username,
                PasswordHash = passwordHash
            };

            await _unitOfWork.Users.AddAsync(newUser);
            await _unitOfWork.SaveChangesAsync();

            await SendEmailVerificationOtpAsync(normalizedEmail);

            _logger.LogInformation("Google sign-up created pending user {UserId} ({Email}) with username {Username}",
                newUser.UserId, normalizedEmail, username);

            return new GoogleSignupCallbackResult(
                GoogleSignupFlow.NewUserVerifyOtp,
                normalizedEmail);
        }

        public async Task<bool> SendEmailVerificationOtpAsync(string email)
        {
            var normalizedEmail = NormalizeEmail(email);
            var otp = GenerateOtp();
            _otpCacheService.StoreEmailVerificationOtp(normalizedEmail, otp);
            await _emailService.SendOtpEmailAsync(normalizedEmail, otp);
            return true;
        }

        public async Task<bool> CompleteEmailVerificationOtpAsync(string email, string otp)
        {
            var normalizedEmail = NormalizeEmail(email);

            if (!_otpCacheService.TryValidateAndRemoveEmailVerificationOtp(normalizedEmail, otp))
                throw new InvalidOperationException("The verification code is invalid or has expired.");

            _logger.LogInformation("Email verified via OTP for {Email}.", normalizedEmail);
            return true;
        }

        private async Task EnsureEmailAndUsernameAvailableAsync(string normalizedEmail, string username)
        {
            if (await _unitOfWork.Users.GetByUsernameAsync(username.Trim()) is not null)
                throw new InvalidOperationException("This username is already taken.");
        }

        private AuthResultDto BuildSuccessfulAuthResult(User user, string failureContext)
        {
            try
            {
                var userDto = user.ToDto();
                return new AuthResultDto(true, userDto, userDto.RoleName, null);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "{FailureContext}: Role was not loaded for user {UserId} ({Username})",
                    failureContext,
                    user.UserId,
                    user.Username);

                return new AuthResultDto(
                    false,
                    null,
                    null,
                    "Account configuration is invalid. Contact support.");
            }
        }

        private GoogleSignupCallbackResult BuildActiveGoogleSignupLoginResult(
            User user,
            string normalizedEmail)
        {
            var userDto = user.ToDto();

            return new GoogleSignupCallbackResult(
                GoogleSignupFlow.ActiveUserLogin,
                normalizedEmail,
                userDto,
                userDto.RoleName);
        }

        private async Task<string> GenerateUniqueUsernameAsync(string? googleDisplayName, string email)
        {
            var baseUsername = BuildBaseUsername(googleDisplayName, email);
            var candidate = baseUsername;
            var suffix = 0;

            while (await _unitOfWork.Users.GetByUsernameAsync(candidate) is not null)
            {
                suffix++;
                candidate = $"{baseUsername}{suffix}";
            }

            return candidate;
        }

        private static string BuildBaseUsername(string? googleDisplayName, string email)
        {
            var fromName = string.IsNullOrWhiteSpace(googleDisplayName)
                ? string.Empty
                : new string(googleDisplayName.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

            if (fromName.Length >= 3)
            {
                return fromName.Length > 50 ? fromName[..50] : fromName;
            }

            var localPart = email.Split('@')[0];
            var fromEmail = new string(localPart.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

            if (fromEmail.Length >= 3)
            {
                return fromEmail.Length > 50 ? fromEmail[..50] : fromEmail;
            }

            return $"user{Random.Shared.Next(1000, 9999)}";
        }

        private static string GenerateOtp()
            => Random.Shared.Next(100000, 999999).ToString();

        private static string NormalizeEmail(string email)
            => email.Trim().ToLowerInvariant();

        private static string ResolveLoginIdentifier(string usernameOrEmail)
        {
            var trimmed = usernameOrEmail.Trim();
            return trimmed.Contains('@') ? NormalizeEmail(trimmed) : trimmed;
        }
    }
}