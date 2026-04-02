using System;
using System.Collections.Generic;
using System.Text;

using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        DateTime GetAccessTokenExpiry();
    }
}