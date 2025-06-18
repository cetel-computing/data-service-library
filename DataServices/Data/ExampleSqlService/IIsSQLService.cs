using Microsoft.AspNetCore.Identity;

namespace DataServices.Data
{
    /// <summary>
    /// primary interface that is responsible for interacting with cis sql database data
    /// </summary>
    public interface IIsSQLService : IDisposable
    {
        Task<bool> ExistsUserAsync(string userId);

        Task<AmUser?> GetUserAsync(string userId);

        Task<IEnumerable<AmUser>> GetAllUsersAsync();

        Task<string?> AddUserAsync(string email);

        Task UpdateUserByIdAsync(string id, IdentityUser updatedUserData);

        Task DeleteUserAsync(string userId);

        //roles
        Task<IEnumerable<string>> GetUserRolesAsync(string userId);

        Task<IdentityResult> AddToRoleByNameAsync(string userId, IList<string> roles);

        Task<IdentityResult> AddToRoleByNameAsync(IList<string> usersIds, IList<string> roles);

        Task<IdentityResult> RemoveFromRoleByNameAsync(string userId, string roleName);

        //user claims
        Task<IEnumerable<IdentityUserClaim<string>>> GetUserClaimsAsync(string userId);

        Task<IdentityResult> AddNewUserClaimAsync(IdentityUserClaim<string> claim);

        Task DeleteUserClaimAsync(string userId, string claimId);

        //role claims
        Task<IEnumerable<IdentityRoleClaim<string>>> GetRoleClaimsAsync(string roleId);

        Task<IdentityResult> AddNewRoleClaimAsync(IdentityRoleClaim<string> claim);

        Task DeleteRoleClaimAsync(string roleId, string claimId);

        Task<int> Count(string sql, object? parameters = null);

        Task<IEnumerable<T>> Get<T>() where T : class;

        Task<IEnumerable<T>> Get<T>(string sql, object? parameters = null) where T : class;

        Task<T> Get<T>(string id) where T : class;

        Task<IEnumerable<T>> GetByQuery<T>(string sql, object? parameters = null) where T : class;

        Task<int?> Execute(string sql, object? parameters = null);

        Task<int?> Create<T>(T document) where T : class;

        Task<int> Update<T>(T document) where T : class;

        Task<int> Remove<T>(string id) where T : class;
    }
}
