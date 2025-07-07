using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Interfaces;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Api.Services;
using ECommerce.Core.Entities;
using BCrypt.Net;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("User registration attempt for username: {Username}", request.Username);

            // Check if user already exists
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Username {Username} already exists", request.Username);
                return Error<object>("Username already exists", statusCode: 409);
            }

            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                return Error<object>("Email already exists", statusCode: 409);
            }

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _userRepository.AddAsync(user);
            _logger.LogInformation("User registered successfully: {Username}", request.Username);

            return Success<object>(new { message = "User registered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return Error<object>("An error occurred during registration", statusCode: 500);
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for username: {Username}", request.Username);

            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for username: {Username}", request.Username);
                return Error<LoginResponse>("Invalid username or password", statusCode: 401);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: User {Username} is not active", request.Username);
                return Error<LoginResponse>("User account is not active", statusCode: 401);
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var response = new LoginResponse
            {
                Token = token,
                Username = user.Username,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            _logger.LogInformation("User logged in successfully: {Username}", request.Username);
            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return Error<LoginResponse>("An error occurred during login", statusCode: 500);
        }
    }
}