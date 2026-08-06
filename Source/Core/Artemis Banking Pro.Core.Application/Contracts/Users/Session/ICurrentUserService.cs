using System.Collections.Generic;

namespace ArtemisBankingPro.Core.Application.Contracts.Users.Session
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserName { get; }
        IList<string> Roles { get; }
        bool IsInRole(string role);
    }
}
