using System.Security.Claims;
using Novell.Directory.Ldap;

namespace DataServices.Data
{
    /// <summary>
    /// responsible for interacting with ldap database data
    /// </summary>
    public interface ILdapService : IDisposable
    {
        Task<IEnumerable<T>> Search<T>(string dn, string filter = null, LdapSearchScope scope = LdapSearchScope.CurrentBranch) where T : Ldap;

        Task<int> Count(string dn, string filter = null, int scope = LdapConnection.ScopeOne);

        Task<T> Upsert<T>(string dn, T obj) where T : Ldap;

        Task<T> Create<T>(string dn, T obj) where T : Ldap;

        Task<T> Update<T>(string dn, T obj) where T : Ldap;

        Task<bool> AddMember<T>(string dn, string modCn, string member) where T : Ldap;

        Task RemoveMember<T>(string dn, string modCn, string member) where T : Ldap;

        Task<T> Delete<T>(string dn, string filter) where T : Ldap;

    }
}
