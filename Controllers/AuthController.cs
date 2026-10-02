using DiplomBackend.Models;
using DiplomBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace DiplomBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            var result = _authService.Register(dto);
            if (result == null)
                return BadRequest(new { message = "Пользователь с таким Email уже существует" });

            return Ok(result);
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var result = _authService.Login(dto);
            if (result == null)
                return Unauthorized(new { message = "Неверный Email или пароль" });

            return Ok(result);
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto dto)
        {
            var result = await _authService.GoogleLoginAsync(dto.Credential);
            if (result == null)
                return Unauthorized(new { message = "Не удалось авторизоваться через Google" });

            return Ok(result);
        }
    }
}