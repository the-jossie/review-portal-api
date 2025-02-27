using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AutoMapper;
using Ca_Bank_Api.Dtos;
using Ca_Bank_Api.Models;
using Ca_Bank_Api.Repositories;

namespace Ca_Bank_Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{

    private static readonly ConcurrentDictionary<string, (int Attempts, DateTime LastAttempt)> OTPRequests = new();
    private static readonly TimeSpan RateLimitTimeWindow = TimeSpan.FromMinutes(1);
    private static readonly int MaxAttempts = 3;

    private readonly IConfiguration _config;

    private readonly IUserRepository _userRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;
    private readonly EmailService _emailService;

    public AuthController(IConfiguration config, IUserRepository userRepository, IAuthRepository authRepository, ILogger<AuthController> logger, EmailService emailService)
    {
        _config = config;

        _userRepository = userRepository;
        _authRepository = authRepository;
        _logger = logger;
        _emailService = emailService;

        _mapper = new Mapper(new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SignupDto, User>();
            cfg.CreateMap<LoginDto, User>();
        }));
    }

    [HttpPost("/signup")]
    public IActionResult Signup(SignupDto userData)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(userData.Email) ||
            string.IsNullOrWhiteSpace(userData.Password) ||
            string.IsNullOrWhiteSpace(userData.PasswordConfirmation) ||
            string.IsNullOrWhiteSpace(userData.UserName))
        {
            return BadRequest(new { message = "All fields are required." });
        }

        if (userData.Password != userData.PasswordConfirmation)
        {
            return BadRequest(new { message = "Passwords do not match" });
        }

        var newUser = _mapper.Map<User>(userData);

        // Check if the user already exists
        var existingUser = _userRepository.GetAllUsers().FirstOrDefault(u => u.Email == newUser.Email);

        if (existingUser != null)
        {
            return BadRequest(new { message = "User with this email already exists." });
        }

        // Generate password salt and hash
        byte[] passwordSalt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetNonZeroBytes(passwordSalt);
        }

        byte[] passwordHash = GetPasswordHash(userData.Password, passwordSalt);


        // Add user to Auth table
        var newUserAuth = new Auth
        {
            Email = userData.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };
        _userRepository.AddEntity<Auth>(newUserAuth);

        // Set default role to "user"
        newUser.UserRole = "user";

        // Add user to Users table
        _userRepository.AddEntity<User>(newUser);

        if (_userRepository.SaveChanges())
        {
            return Ok(new { message = "User created successfully!" });
        }

        return StatusCode(500, "Failed to create user.");
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(LoginDto userData)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(userData.Email) || string.IsNullOrWhiteSpace(userData.Password))
        {
            return await Task.FromResult<IActionResult>(BadRequest(new { message = "Email and password are required." }));
        }

        // Find user in Auth table
        var userAuth = _authRepository.GetAllUsers().FirstOrDefault(u => u.Email == userData.Email);

        if (userAuth == null)
        {
            _logger.LogWarning($"Failed login attempt for email: {userData.Email}");
            return await Task.FromResult<IActionResult>(BadRequest(new { message = "Invalid email or password." }));
        }

        // Verify password
        var passwordHash = GetPasswordHash(userData.Password, userAuth.PasswordSalt);
        if (!userAuth.PasswordHash.SequenceEqual(passwordHash))
        {
            _logger.LogWarning($"Failed login attempt for email: {userData.Email}");
            return await Task.FromResult<IActionResult>(BadRequest(new { message = "Invalid email or password." }));
        }

        // Get user Details
        var user = _userRepository.GetAllUsers().FirstOrDefault(u => u.Email == userData.Email);
        if (user == null)
        {
            _logger.LogWarning($"Failed login attempt for email: {userData.Email}");
            return await Task.FromResult<IActionResult>(BadRequest(new { message = "User not found" }));
        }


        if (OTPRequests.TryGetValue(userData.Email, out var request) && request.Attempts >= MaxAttempts && request.LastAttempt.Add(RateLimitTimeWindow) > DateTime.UtcNow)
        {
            _logger.LogWarning($"Too many OTP requests for email: {userData.Email}");
            return await Task.FromResult<IActionResult>(BadRequest(new { message = "Too many OTP requests. Please try again later." }));
        }

        // Generate OTP
        var otp = GenerateOTP();
        userAuth.OTP = otp.ToString();
        userAuth.OTPExpiry = DateTime.UtcNow.AddMinutes(5);

        // Update OTP details in the repository
        _authRepository.UpdateEntity<Auth>(userAuth);

        // Save changes to the database
        if (!_authRepository.SaveChanges())
        {
            return await Task.FromResult<IActionResult>(StatusCode(500, "Failed to save OTP details."));
        }

        bool isEmailSent = await SendOTPEmail(userData.Email, otp);

        if (!isEmailSent)
        {
            return await Task.FromResult<IActionResult>(StatusCode(500, "Failed to send OTP email. Please try again."));
        }

        OTPRequests[userData.Email] = (request.Attempts + 1, DateTime.UtcNow);

        _logger.LogInformation($"OTP sent to email: {userData.Email}");
        return await Task.FromResult<IActionResult>(Ok(new { message = "OTP sent to your email. Please verify to proceed." }));
    }

    [HttpPost("/verify-otp")]
    public IActionResult VerifyOTP(OTPDto otpData)
    {
        var userAuth = _authRepository.GetAllUsers().FirstOrDefault(u => u.Email == otpData.Email);
        if (userAuth == null || userAuth.OTP != otpData.OTP || userAuth.OTPExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning($"Invalid or expired OTP attempt for email: {otpData.Email}");
            return BadRequest(new { message = "Invalid or expired OTP." });
        }


        // Get user Details
        var user = _userRepository.GetAllUsers().FirstOrDefault(u => u.Email == otpData.Email);
        if (user == null)
        {
            _logger.LogWarning($"Failed login attempt for email: {otpData.Email}");
            return BadRequest(new { message = "User not found" });
        }

        userAuth.OTP = null;
        userAuth.OTPExpiry = null;

        // Update OTP details in the repository
        _authRepository.UpdateEntity<Auth>(userAuth);

        // Save changes to the database
        if (!_authRepository.SaveChanges())
        {
            return StatusCode(500, "Failed to save OTP details.");
        }

        _logger.LogInformation($"OTP verified successfully for email: {otpData.Email}");
        return Ok(new { message = "OTP verified successfully.", token = CreateToken(user.UserId), userDetails = user });
    }

    private async Task<bool> SendOTPEmail(string email, int otp)
    {
        try
        {
            string subject = "CaBank: Your OTP Code";
            string body = $"Your OTP code is {otp}. It will expire in 5 minutes.";
            await _emailService.SendEmailAsync(email, subject, body);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to send OTP email to {email}: {ex.Message}");
            return false;
        }
    }

    private int GenerateOTP()
    {
        return new Random().Next(100000, 999999);
    }


    private byte[] GetPasswordHash(string password, byte[] passwordSalt)
    {
        string passwordSaltPlusString = _config.GetSection("AppSettings:PasswordKey").Value + Convert.ToBase64String(passwordSalt);

        return KeyDerivation.Pbkdf2(
            password: password,
            salt: Encoding.ASCII.GetBytes(passwordSaltPlusString),
            prf: KeyDerivationPrf.HMACSHA1,
            iterationCount: 100000,
            numBytesRequested: 256 / 8
        );
    }

    private string CreateToken(int userId)
    {
        var claims = new[]
        {
            new Claim("userId", userId.ToString())
        };

        var tokenKeyString = _config.GetSection("AppSettings:TokenKey").Value;
        var tokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKeyString ?? ""));

        var signingCredentials = new SigningCredentials(tokenKey, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(1),
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}
