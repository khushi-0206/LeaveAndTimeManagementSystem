using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<LoginRequestDto> _loginValidator;
        private readonly IValidator<RegisterUserDto> _registerValidator;

        public AuthController(
            IAuthService authService,
            IValidator<LoginRequestDto> loginValidator,
            IValidator<RegisterUserDto> registerValidator)
        {
            _authService = authService;
            _loginValidator = loginValidator;
            _registerValidator = registerValidator;
        }

        // ================= LOGIN =================
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var validation = await _loginValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(ApiResponseDto<string>.Fail("Validation failed",
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var result = await _authService.LoginAsync(dto);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        // ================= REFRESH TOKEN =================
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        // ================= LOGOUT =================
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.LogoutAsync(dto.RefreshToken);
            return Ok(result);
        }

        // ================= REGISTER ADMIN =================
        [HttpPost("register-admin")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterUserDto dto)
        {
            // 🔥 Force role as Admin (security)
            dto.Role = "HRAdmin";

            // 🔥 Optional: Prevent multiple admins
            var adminExists = await _authService.AdminExistsAsync();
            if (adminExists)
            {
                return BadRequest(ApiResponseDto<string>.Fail("Admin already exists. Please login."));
            }

            var validation = await _registerValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(ApiResponseDto<string>.Fail("Validation failed",
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var result = await _authService.RegisterUserAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ================= REGISTER USER =================
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            var validation = await _registerValidator.ValidateAsync(dto);
            if (!validation.IsValid)
                return BadRequest(ApiResponseDto<string>.Fail("Validation failed",
                    validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var result = await _authService.RegisterUserAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ================= FORGOT PASSWORD =================
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(result);
        }

        // ================= RESET PASSWORD =================
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}