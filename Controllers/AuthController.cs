using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Restaurant_Review_Api.Data;
using Restaurant_Review_Api.Dtos;
using Restaurant_Review_Api.Models;
using Restaurant_Review_Api.Repositories;

namespace Restaurant_Review_Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly DataContext _dbContext;
    private readonly IConfiguration _config;

    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public AuthController(DataContext dbContext, IConfiguration config, IUserRepository userRepository)
    {
        _dbContext = dbContext;
        _config = config;

        _userRepository = userRepository;

        _mapper = new Mapper(new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SignupDto, User>();
            cfg.CreateMap<LoginDto, User>();
        }));
    }

    [HttpPost("signup")]
    public IActionResult Signup(SignupDto userData)
    {
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

}
