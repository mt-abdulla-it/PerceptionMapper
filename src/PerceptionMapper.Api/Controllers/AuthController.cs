using Microsoft.AspNetCore.Mvc;
using PerceptionMapper.Api.Models;
using PerceptionMapper.Application.Interfaces;

namespace PerceptionMapper.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) =>
        Ok(await _auth.LoginAsync(request.Username, request.Password));

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request) =>
        Ok(await _auth.RegisterAsync(request.Username, request.Password));
}
