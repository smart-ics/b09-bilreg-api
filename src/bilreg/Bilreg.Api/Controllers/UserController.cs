using Bilreg.Application.Shared.User;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Nuna.Lib.ActionResultHelper;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bilreg.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : Controller
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public UserController(IMediator mediator, 
        IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }


    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody]UserGetUserUsmanQuery cmd)
    {
        try
        {
            var dataUser = await _mediator.Send(cmd);
            var tokenAuth = GetToken(cmd.Email, dataUser);
            var result = dataUser with { TokenAuth = tokenAuth };
            return Ok(new JSendOk(result));
        }
        catch (Exception ex) { return Ok(new JSendFailed(ex)); }

    }

    private string GetToken(string email, UserGetUserUsmanResponse response)
    {
        //      - claim identity
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"] ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, response.UserName)
        };

        foreach (var role in response.ListRole ?? [])
        {
            if (!string.IsNullOrWhiteSpace(role.Role))
                claims.Add(new Claim(ClaimTypes.Role, role.Role.Trim()));
        }

        //  EXECUTE
        //      - generate token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty));
        var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: signIn);
        // RETURN
        var result = new JwtSecurityTokenHandler().WriteToken(token);
        return result;
    }
}
