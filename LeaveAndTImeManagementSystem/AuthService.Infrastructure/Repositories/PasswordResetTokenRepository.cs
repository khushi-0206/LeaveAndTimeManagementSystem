using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories
{
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AuthDbContext _context;

        public PasswordResetTokenRepository(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<PasswordResetToken?> GetByTokenAsync(string token)
            => await _context.PasswordResetTokens
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Token == token && !p.IsUsed);

        public async Task MarkUsedAsync(string token)
        {
            var t = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(p => p.Token == token);
            if (t != null)
            {
                t.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}