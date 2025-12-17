using AuroraJudge.Application.DTOs;
using AuroraJudge.Application.DTOs.Auth;
using AuroraJudge.Domain.Common;
using AuroraJudge.Domain.Entities;
using AuroraJudge.Domain.Enums;
using AuroraJudge.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuroraJudge.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IPermissionService _permissionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    
    public AuthService(
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        IPermissionService permissionService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _permissionService = permissionService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }
    
    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Check if username exists
        if (await _userRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
        {
            throw new ValidationException("用户名已存在");
        }
        
        // Check if email exists
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new ValidationException("邮箱已被注册");
        }
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DisplayName = request.Username,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Log audit
        await _auditLogService.LogAsync(
            userId: user.Id,
            username: user.Username,
            action: AuditAction.Create,
            description: "用户注册",
            entityType: "User",
            entityId: user.Id.ToString(),
            newValue: new { user.Username, user.Email },
            cancellationToken: cancellationToken);
    }
    
    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail, cancellationToken)
            ?? throw new ValidationException("用户名或密码错误");
        
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationException("用户名或密码错误");
        }
        
        if (user.Status != UserStatus.Active)
        {
            throw new ValidationException("账户已被禁用");
        }
        
        // Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiresInMinutes = GetConfigInt("Jwt:ExpiresInMinutes", 60);
        var refreshExpiresIn = GetConfigInt("Jwt:RefreshExpiresInDays", 7);
        
        // Save refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshExpiresIn);
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = ipAddress;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Log audit
        await _auditLogService.LogAsync(
            userId: user.Id,
            username: user.Username,
            action: AuditAction.Login,
            description: "用户登录",
            entityType: "User",
            entityId: user.Id.ToString(),
            newValue: new { IpAddress = ipAddress },
            cancellationToken: cancellationToken);
        
        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            User: await MapToUserDtoAsync(user, cancellationToken)
        );
    }
    
    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(refreshToken, cancellationToken)
            ?? throw new ValidationException("无效的刷新令牌");
        
        if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            throw new ValidationException("刷新令牌已过期");
        }
        
        // Generate new tokens
        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = GenerateRefreshToken();
        var expiresInMinutes = GetConfigInt("Jwt:ExpiresInMinutes", 60);
        var refreshExpiresIn = GetConfigInt("Jwt:RefreshExpiresInDays", 7);
        
        // Update refresh token
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshExpiresIn);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return new LoginResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            User: await MapToUserDtoAsync(user, cancellationToken)
        );
    }
    
    public async Task LogoutAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        // Parse the token to get user ID
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);
            var userIdClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user != null)
                {
                    // Invalidate refresh token
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    // Log audit
                    await _auditLogService.LogAsync(
                        userId: user.Id,
                        username: user.Username,
                        action: AuditAction.Logout,
                        description: "用户登出",
                        entityType: "User",
                        entityId: user.Id.ToString(),
                        cancellationToken: cancellationToken);
                }
            }
        }
        catch
        {
            // Token parsing failed, just ignore since user is logging out anyway
        }
    }
    
    public async Task<UserDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("用户不存在");
        
        return await MapToUserDtoAsync(user, cancellationToken);
    }
    
    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("用户不存在");
        
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException("当前密码错误");
        }
        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Log audit
        await _auditLogService.LogAsync(
            userId: user.Id,
            username: user.Username,
            action: AuditAction.Update,
            description: "修改密码",
            entityType: "User",
            entityId: user.Id.ToString(),
            newValue: new { Action = "PasswordChanged" },
            cancellationToken: cancellationToken);
    }
    
    #region Private Helpers
    
    private string GenerateAccessToken(User user)
    {
        var key = _configuration["Jwt:Key"] 
            ?? throw new InvalidOperationException("JWT key is not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "AuroraJudge";
        var audience = _configuration["Jwt:Audience"] ?? "AuroraJudgeApi";
        var expiresInMinutes = GetConfigInt("Jwt:ExpiresInMinutes", 60);
        
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        // Add roles
        if (user.UserRoles != null)
        {
            foreach (var userRole in user.UserRoles)
            {
                if (userRole.Role != null)
                {
                    // Use role Code (stable identifier) for auth checks.
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Code));
                }
            }
        }
        
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
    
    private int GetConfigInt(string key, int defaultValue)
    {
        var value = _configuration[key];
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
    
    private async Task<UserDto> MapToUserDtoAsync(User user, CancellationToken cancellationToken)
    {
        var roles = user.UserRoles?
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Code)
            .Distinct()
            .ToList() ?? [];

        var permissions = await _permissionService.GetUserPermissionsAsync(user.Id, cancellationToken);

        return new UserDto(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            Avatar: user.Avatar,
            Bio: user.Bio,
            RealName: user.RealName,
            Organization: user.Organization,
            Status: user.Status,
            SolvedCount: user.SolvedCount,
            SubmissionCount: user.SubmissionCount,
            Rating: user.Rating,
            MaxRating: user.MaxRating,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            Roles: roles,
            Permissions: permissions
        );
    }
    
    #endregion
}
