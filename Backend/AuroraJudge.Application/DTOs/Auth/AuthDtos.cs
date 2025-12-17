namespace AuroraJudge.Application.DTOs.Auth;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string ConfirmPassword
);

public record LoginRequest(
    string UsernameOrEmail,
    string Password,
    bool RememberMe = false
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);

public record ResetPasswordRequest(
    string Email
);

public record ConfirmResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmPassword
);
