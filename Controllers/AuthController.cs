using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Restaurant_Review_Api.Dtos;
using Restaurant_Review_Api.Models;
using Restaurant_Review_Api.Repositories;

namespace Restaurant_Review_Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    private readonly IUserRepository _userRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IMapper _mapper;

    public AuthController(IConfiguration config, IUserRepository userRepository, IAuthRepository authRepository)
    {
        _config = config;

        _userRepository = userRepository;
        _authRepository = authRepository;

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
            return BadRequest("User with this email already exists.");
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

        // Add user to Users table
        _userRepository.AddEntity<User>(newUser);

        if (_userRepository.SaveChanges())
        {
            return Ok(new { message = "User created successfully!" });
        }

        return StatusCode(500, "Failed to create user.");
    }

    [HttpPost("/login")]
    public IActionResult Login(LoginDto userData)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(userData.Email) || string.IsNullOrWhiteSpace(userData.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        // Find user in Auth table
        var userAuth = _authRepository.GetAllUsers().FirstOrDefault(u => u.Email == userData.Email);

        if (userAuth == null)
        {
            return BadRequest(new { message = "Invalid email or password." });
        }

        // Verify password
        var passwordHash = GetPasswordHash(userData.Password, userAuth.PasswordSalt);
        if (!userAuth.PasswordHash.SequenceEqual(passwordHash))
        {
            return BadRequest(new { message = "Invalid email or password." });
        }


        // Get user Details
        var user = _userRepository.GetAllUsers().FirstOrDefault(u => u.Email == userData.Email);
        if (user == null)
        {
            return BadRequest(new { message = "User not found" });
        }

        return Ok(new { message = "Login successful", token = CreateToken(user.UserId), userDetails = user });
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
