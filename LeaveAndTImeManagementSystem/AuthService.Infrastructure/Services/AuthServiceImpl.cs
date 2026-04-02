using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Repositories;
using AutoMapper;

namespace AuthService.Infrastructure.Services
{
    public class AuthServiceImpl : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly IPasswordResetTokenRepository _resetTokenRepo;
        private readonly IJwtService _jwtService;
        private readonly IMapper _mapper;

        public AuthServiceImpl(
            IUserRepository userRepo,
            IRefreshTokenRepository refreshTokenRepo,
            IPasswordResetTokenRepository resetTokenRepo,
            IJwtService jwtService,
            IMapper mapper)
        {
            _userRepo = userRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _resetTokenRepo = resetTokenRepo;
            _jwtService = jwtService;
            _mapper = mapper;
        }

        public async Task<ApiResponseDto<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email);

            if (user == null)
                return ApiResponseDto<LoginResponseDto>.Fail("Invalid email or password.");

            if (!user.IsActive)
                return ApiResponseDto<LoginResponseDto>.Fail("Account is deactivated.");

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                return ApiResponseDto<LoginResponseDto>.Fail(
                    $"Account locked. Try again after {user.LockoutEnd.Value.ToLocalTime():HH:mm:ss}.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    user.FailedLoginAttempts = 0;
                }
                await _userRepo.UpdateAsync(user);
                return ApiResponseDto<LoginResponseDto>.Fail("Invalid email or password.");
            }

            // Reset failed attempts on success
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _userRepo.UpdateAsync(user);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            await _refreshTokenRepo.CreateAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            return ApiResponseDto<LoginResponseDto>.Ok(new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = _jwtService.GetAccessTokenExpiry(),
                User = _mapper.Map<UserDto>(user)
            });
        }

        public async Task<ApiResponseDto<LoginResponseDto>> RefreshTokenAsync(string refreshToken)
        {
            var token = await _refreshTokenRepo.GetByTokenAsync(refreshToken);

            if (token == null || token.ExpiresAt < DateTime.UtcNow)
                return ApiResponseDto<LoginResponseDto>.Fail("Invalid or expired refresh token.");

            await _refreshTokenRepo.RevokeAsync(refreshToken);

            var newAccessToken = _jwtService.GenerateAccessToken(token.User);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            await _refreshTokenRepo.CreateAsync(new RefreshToken
            {
                UserId = token.UserId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            return ApiResponseDto<LoginResponseDto>.Ok(new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = _jwtService.GetAccessTokenExpiry(),
                User = _mapper.Map<UserDto>(token.User)
            });
        }

        public async Task<ApiResponseDto<string>> LogoutAsync(string refreshToken)
        {
            await _refreshTokenRepo.RevokeAsync(refreshToken);
            return ApiResponseDto<string>.Ok("Logged out successfully.");
        }

        public async Task<ApiResponseDto<UserDto>> RegisterUserAsync(RegisterUserDto dto)
        {
            if (await _userRepo.ExistsAsync(dto.Email))
                return ApiResponseDto<UserDto>.Fail("Email already registered.");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 10),
                Role = dto.Role,
                Department = dto.Department,
                Designation = dto.Designation,
                ManagerId = dto.ManagerId,
                DateOfJoining = dto.DateOfJoining,
                EmploymentType = dto.EmploymentType
            };

            var created = await _userRepo.CreateAsync(user);
            return ApiResponseDto<UserDto>.Ok(_mapper.Map<UserDto>(created), "User registered successfully.");
        }

        public async Task<ApiResponseDto<string>> ForgotPasswordAsync(string email)
        {
            var user = await _userRepo.GetByEmailAsync(email);

            // Always return success (don't reveal if email exists)
            if (user == null)
                return ApiResponseDto<string>.Ok("If the email exists, a reset link has been sent.");

            var token = new PasswordResetToken
            {
                UserId = user.Id,
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };

            await _resetTokenRepo.CreateAsync(token);

            // TODO Day 5: Send email via NotificationService
            Console.WriteLine($"[DEV] Password reset token for {email}: {token.Token}");

            return ApiResponseDto<string>.Ok("If the email exists, a reset link has been sent.");
        }

        public async Task<ApiResponseDto<string>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var token = await _resetTokenRepo.GetByTokenAsync(dto.Token);

            if (token == null || token.ExpiresAt < DateTime.UtcNow)
                return ApiResponseDto<string>.Fail("Invalid or expired reset token.");

            token.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, 10);
            await _userRepo.UpdateAsync(token.User);

            await _resetTokenRepo.MarkUsedAsync(dto.Token);
            await _refreshTokenRepo.RevokeAllForUserAsync(token.UserId);

            return ApiResponseDto<string>.Ok("Password reset successfully.");
        }

        public async Task<bool> AdminExistsAsync()
        {
            var user = await _userRepo.GetByEmailAsync("admin@company.com");
            return user != null && user.Role == "HRAdmin";
        }
    }
}