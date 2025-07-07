using Microsoft.AspNetCore.Mvc;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Entities;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Api.Services;
using ECommerce.Core.Enums;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository userRepository, IJwtService jwtService, ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid login attempt for username: {Username}", request.Username);
                return CreateErrorResponse<LoginResponse>("Invalid username or password");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Username}", request.Username);
                return CreateErrorResponse<LoginResponse>("Account is inactive");
            }

            var token = _jwtService.GenerateToken(user);
            var response = new LoginResponse
            {
                Token = token,
                Username = user.Username,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("User {Username} logged in successfully", user.Username);
            return CreateResponse(response, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return CreateErrorResponse<LoginResponse>("An error occurred during login");
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Check if username already exists
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return CreateErrorResponse("Username already exists");
            }

            // Check if email already exists
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return CreateErrorResponse("Email already exists");
            }

            // Create new user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            _logger.LogInformation("New user registered: {Username}", user.Username);

            return CreateResponse("User registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return CreateErrorResponse("An error occurred during registration");
        }
    }
}