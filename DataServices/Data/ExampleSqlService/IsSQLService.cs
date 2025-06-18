using Microsoft.AspNetCore.Identity;

namespace DataServices.Data
{
    /// <summary>
    /// primary class that is responsible for interacting with idenity services sql database data
    /// </summary>
    public class IsSQLService : SQLService, IIsSQLService
    {
        public IsSQLService(string connectionString) : base(connectionString)
        {
        }

        public async Task<bool> ExistsUserAsync(string userId)
        {
            var sql = "SELECT count(Id) FROM aspnetusers WHERE Id = @userId";
            var users = await Count(sql, new { userId });
            return users > 0;
        }

        public async Task<AmUser?> GetUserAsync(string userId)
        {
            var sql = "select us.Id, us.Username, lower(us.NormalizedEmail) as Email, current_date() as DateAdded, '' as AddedBy, " +
                "json_objectagg(userorg,userroles) as UserOrgRolesJoined, json_objectagg(userorg,usersubscriptions) as UserOrgSubscriptionsJoined, " +
                "group_concat(distinct userorg) as UserOrgsJoined, group_concat(distinct userrolesstring) as UserRolesJoined " +
                "from aspnetusers us " +
                "left join " +
                "(select ur.UserId, gn.name as userorg, group_concat(distinct rl.Name) as userrolesstring, json_arrayagg(distinct rl.Name) as userroles, " +
                "json_arrayagg(distinct gp.key) as usersubscriptions " +
                "from aspnetuserroles ur " +
                "join aspnetroles rl on ur.roleid = rl.id " +
                "left join groupuserroleassignments gura on ur.userid = gura.userid and ur.RoleId = gura.RoleId " +
                "left join groupnodes gn on gura.groupnodeid = gn.id " +
                "left join groupproperties gp on gura.groupnodeid = gp.nodeid " +
                "group by ur.UserId, gn.name " +
                "order by ur.UserId, gn.name) usgroup on us.Id = usgroup.UserId " +
                "where us.Id = @userId " +
                "group by us.Id, us.Username, lower(us.NormalizedEmail)";

            var users = await GetByQuery<AmUser>(sql, new { userId });

            return users.FirstOrDefault();
        }

        public async Task<IEnumerable<AmUser>> GetAllUsersAsync()
        {
            var sql = "select us.Id, us.Username, lower(us.NormalizedEmail) as Email, current_date() as DateAdded, '' as AddedBy, " +
                "json_objectagg(userorg,userroles) as UserOrgRolesJoined, json_objectagg(userorg,usersubscriptions) as UserOrgSubscriptionsJoined, " +
                "group_concat(distinct userorg) as UserOrgsJoined, group_concat(distinct userrolesstring) as UserRolesJoined " +
                "from aspnetusers us " +
                "left join " +
                 "(select ur.UserId, gn.name as userorg, group_concat(distinct rl.Name) as userrolesstring, json_arrayagg(distinct rl.Name) as userroles, " +
                "json_arrayagg(distinct gp.key) as usersubscriptions " +
                "from aspnetuserroles ur " +
                "join aspnetroles rl on ur.roleid = rl.id " +
                "left join groupuserroleassignments gura on ur.userid = gura.userid and ur.RoleId = gura.RoleId " +
                "left join groupnodes gn on gura.groupnodeid = gn.id " +
                "left join groupproperties gp on gura.groupnodeid = gp.nodeid " +
                "group by ur.UserId, gn.name " +
                "order by ur.UserId, gn.name) usgroup on us.Id = usgroup.UserId " +
                "group by us.Id, us.Username, lower(us.NormalizedEmail)";

            var users = await GetByQuery<AmUser>(sql);

            return users;
        }

        public async Task<string?> AddUserAsync(string email)
        {
            var sql = "select Id " +
                "from aspnetusers " +
                "where NormalizedEmail = upper(@email)";

            var users = await GetByQuery<string>(sql, new { email });

            var user = users.FirstOrDefault();
            if (user != null)
            {
                return user;
            }
            else
            {
                var newUser = new IdentityUser
                {
                    Email = email,
                    NormalizedEmail = email.ToUpper(),
                    UserName = email,
                    NormalizedUserName = email.ToUpper(),
                    EmailConfirmed = true,
                    LockoutEnabled = true
                };

                sql = "insert into aspnetusers(Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, " +
                    "PasswordHash, SecurityStamp, ConcurrencyStamp, " +
                    "PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount)" +
                    "values(@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed, " +
                    "@PasswordHash, @SecurityStamp, @ConcurrencyStamp, " +
                    "@PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled, @LockoutEnd, @LockoutEnabled, @AccessFailedCount)";

                var result = await Execute(sql, newUser);
                if (result != null)
                {
                    return newUser.Id;
                }
            }
            return null;
        }

        public async Task UpdateUserByIdAsync(string id, IdentityUser updatedUserData)
        {
            updatedUserData.Id = id;

            await Update(updatedUserData);
        }

        public async Task DeleteUserAsync(string userId)
        {
            var sql = "delete from cis.aspnetusers " +
            "where Id = @userId";

            await Execute(sql, new { userId });
        }

        //roles
        public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
        {
            var result = new List<string>();

            var sql = "select RoleId " +
                "from aspnetuserroles " +
                "where UserId = @userId";

            var roleIds = await GetByQuery<string>(sql, new { userId });

            foreach (var roleId in roleIds)
            {
                sql = "select Name " +
                "from aspnetroles " +
                "where Id = @roleId";

                var roles = await GetByQuery<string>(sql, new { roleId });

                result.AddRange(roles);
            }

            return result;
        }

        public async Task<IdentityResult> AddToRoleByNameAsync(string userId, IList<string> roles)
        {
            var result = 0;

            foreach (var role in roles)
            {
                var sql = "select Id " +
                "from aspnetroles " +
                "where NormalizedName = upper(@role)";

                var roleIds = await GetByQuery<string>(sql, new { role });
                var roleId = roleIds.FirstOrDefault();

                sql = "insert into aspnetuserroles(UserId, RoleId) " +
                "values(@userId, @roleId)";

                var rows = await Execute(sql, new { userId, roleId });

                result += rows ?? 0;
            }

            return result > 0 ? IdentityResult.Success : IdentityResult.Failed();
        }

        public async Task<IdentityResult> AddToRoleByNameAsync(IList<string> usersIds, IList<string> roles)
        {
            var result = 0;

            foreach (var userId in usersIds)
            {
                foreach (var role in roles)
                {
                    var sql = "select Id " +
                    "from aspnetroles " +
                    "where NormalizedName = upper(@role)";

                    var roleIds = await GetByQuery<string>(sql, new { role });
                    var roleId = roleIds.FirstOrDefault();

                    sql = "insert into aspnetuserroles(UserId, RoleId) " +
                    "values(@userId, @roleId)";

                    var rows = await Execute(sql, new { userId, roleId });

                    result += rows ?? 0;
                }
            }

            return result > 0 ? IdentityResult.Success : IdentityResult.Failed();
        }

        public async Task<IdentityResult> RemoveFromRoleByNameAsync(string userId, string roleName)
        {
            var sql = "select Id " +
                    "from aspnetroles " +
                    "where NormalizedName = upper(@roleName)";

            var roleIds = await GetByQuery<string>(sql, new { roleName });
            var roleId = roleIds.FirstOrDefault();

            sql = "delete from aspnetuserroles " +
            "where UserId = @userId and RoleId = @roleId";

            var result = await Execute(sql, new { userId, roleId });

            return result > 0 ? IdentityResult.Success : IdentityResult.Failed();
        }

        //user claims
        public async Task<IEnumerable<IdentityUserClaim<string>>> GetUserClaimsAsync(string userId)
        {
            var sql = "select * " +
                    "from aspnetuserclaims " +
                    "where UserId = @userId";

            return await GetByQuery<IdentityUserClaim<string>>(sql, new { userId });
        }

        public async Task<IdentityResult> AddNewUserClaimAsync(IdentityUserClaim<string> claim)
        {
            var sql = "insert into aspnetuserclaims(Id, UserId, ClaimType, ClaimValue) " +
                "values(@id, @userId, @claimType, @claimValue)";

            var result = await Execute(sql, claim);

            return result > 0 ? IdentityResult.Success : IdentityResult.Failed();
        }

        public async Task DeleteUserClaimAsync(string userId, string claimId)
        {
            var sql = "delete from aspnetuserclaims " +
                "where UserId = @userId and Id = @claimId";

            await Execute(sql, new { userId, claimId });
        }

        //role claims
        public async Task<IEnumerable<IdentityRoleClaim<string>>> GetRoleClaimsAsync(string roleId)
        {
            var sql = "select * " +
                    "from aspnetroleclaims " +
                    "where RoleId = @roleId";

            return await GetByQuery<IdentityRoleClaim<string>>(sql, new { roleId });
        }

        public async Task<IdentityResult> AddNewRoleClaimAsync(IdentityRoleClaim<string> claim)
        {
            var sql = "insert into aspnetroleclaims(Id, RoleId, ClaimType, ClaimValue) " +
                "values(@id, @roleId, @claimType, @claimValue)";

            var result = await Execute(sql, claim);

            return result > 0 ? IdentityResult.Success : IdentityResult.Failed();
        }

        public async Task DeleteRoleClaimAsync(string roleId, string claimId)
        {
            var sql = "delete from aspnetroleclaims " +
            "where RoleId = @roleId and Id = @claimId";

            await Execute(sql, new { roleId, claimId });
        }
    }

    public class AmUser
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string Username { get; set; }

        public DateTime DateAdded { get; set; }

        public string AddedBy { get; set; }

        public string UserRolesJoined { get; set; }

        public string UserOrgsJoined { get; set; }

        public string UserOrgSubscriptionsJoined { get; set; }

        public string UserOrgRolesJoined { get; set; }
    }
}
