using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using Novell.Directory.Ldap;
using DataServices.Config;

namespace DataServices.Data
{
    public class LdapService : ILdapService
    {
        private readonly LdapConfigOptions _config;
        private readonly LdapConnection _connection;

        public LdapService(LdapConfigOptions settings)
        {
            _config = settings;

            var options = new LdapConnectionOptions()
                .ConfigureRemoteCertificateValidationCallback(new((sender, certificate, chain, errors) => true))
                .UseSsl();

            _connection = new LdapConnection(options)
            {
                SecureSocketLayer = true
            };

            //assume hosts are in order of preference in the config
            foreach (var host in _config.Hosts)
            {
                //try to connect to the host, otherwise go to the next one
                try
                {
                    _connection.Connect(host.Name, host.Port);
                }
                catch (Exception e)
                {
                    //Log.Warning(e, "Failed to connect to {Name}:{Port}", host.Name, host.Port);
                    continue;
                }

                //use default username/password if non supplied per host
                var username = string.IsNullOrEmpty(host.Username) ? _config.DefaultUsername : host.Username;
                var password = string.IsNullOrEmpty(host.Password) ? _config.DefaultPassword : host.Password;

                //attempt to bind
                _connection.Bind(username, password);
                if (_connection.Bound)
                {
                    //stop on first host that authenticates
                    break;
                }
                else
                {
                    //Log.Warning("Failed to bind to {Name}:{Port} with user {Username}", host.Name, host.Port, username);
                }
            }

            if (!_connection.Bound)
            {
                //if we still have no bound connection after trying all hosts then error
                throw new Exception("Connection or authentication failed for all hosts.");
            }
        }

        public void Dispose()
        {
            _connection.Disconnect();
        }

        public async Task<IEnumerable<T>> Search<T>(string dn, string filter = null, LdapSearchScope scope = LdapSearchScope.CurrentBranch) where T : Ldap
        {
            var result = new List<T>();

            //work out what properties are in this object - need the properties and an array of the names to search the ldap with
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanWrite).ToArray();
            var ldapAtts = props.Select(a => a.Name).ToArray();

            //search ldap for the attributes we want at the scope level specified (default is current branch)
            var queue = _connection.Search(dn, (int)scope, filter, ldapAtts, false, null, null);
            LdapMessage message;
            while ((message = queue.GetResponse()) != null)
            {
                switch (message)
                {
                    // OPTION 1: the message is a search result reference
                    case LdapSearchResultReference searchResultRef:
                        {
                            // Not following referrals to keep things simple
                            var urls = searchResultRef.Referrals;
                            //Log.Debug("Search result references: {V}", string.Join(",", urls));

                            break;
                        }
                    // OPTION 2:the message is a search result
                    case LdapSearchResult searchResult:
                        {
                            var ent = searchResult.Entry;

                            var atts = ent.GetAttributeSet();
                            if (atts == null)
                            {
                                continue;
                            }

                            var obj = Activator.CreateInstance<T>();
                            obj.Dn = ent.Dn;

                            //map each of the ldap results back to the required object
                            foreach (LdapAttribute att in atts)
                            {
                                if (props.FirstOrDefault(p => p.Name.Equals(att.Name, StringComparison.OrdinalIgnoreCase)) is PropertyInfo prop)
                                {
                                    if (prop.Name == nameof(Ldap.CreateTimestamp) || prop.Name == nameof(Ldap.ModifyTimestamp))
                                    {
                                        prop.SetValue(obj, DateTime.ParseExact(att.StringValue.Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture));
                                    }
                                    else
                                    {
                                        prop.SetValue(obj, prop.PropertyType.IsArray ? (object)att.StringValueArray : att.StringValue);
                                    }
                                }
                            }

                            result.Add(obj);

                            break;
                        }
                    // OPTION 3: The message is a search response
                    case LdapResponse searchResponse:
                        {
                            var status = searchResponse.ResultCode;

                            switch (status)
                            {
                                // the return code is Ldap success                            
                                case LdapException.Success:
                                    break;
                                // the return code is referral exception
                                case LdapException.Referral:
                                    {
                                        var urls = searchResponse.Referrals;
                                        //Log.Debug("Referrals: {V}", string.Join(",", urls));

                                        break;
                                    }
                                default:
                                    {
                                        //Log.Error("Asynchronous search failed {E}", searchResponse.ErrorMessage);
                                        break;
                                    }
                            }

                            break;
                        }
                }
            }

            return result;
        }

        public async Task<int> Count(string dn, string filter = null, int scope = LdapConnection.ScopeOne)
        {
            var searchResults = _connection.Search(dn, scope, filter, null, true);
            searchResults.HasMore();

            return searchResults.Count;
        }

        public async Task<T> Upsert<T>(string dn, T obj) where T : Ldap
        {
            var result = await Update(dn, obj);
            return result ?? await Create(dn, obj);
        }

        public async Task<T> Create<T>(string dn, T obj) where T : Ldap
        {
            var dnString = obj.ToDnString(dn);

            var objString = obj.ToString();

            //check if already exists
            var searchResult = await Search<T>(dn, objString);
            if (searchResult.Any())
            {
                return null;
            }

            //work out what properties are in this object
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .Where(p => p.Name != nameof(Ldap.Dn))
                .Where(p => p.Name != nameof(Ldap.EntryUuid))
                .Where(p => p.Name != nameof(Ldap.CreateTimestamp))
                .Where(p => p.Name != nameof(Ldap.ModifyTimestamp))
                .ToArray();

            var atts = new LdapAttributeSet();

            //map to the ldap
            foreach (var prop in props)
            {
                if (prop.GetValue(obj) is string[] newPropArray && newPropArray.Any())
                {
                    atts.Add(new LdapAttribute(prop.Name, newPropArray));
                }
                else if (prop.GetValue(obj) is string newProp && !string.IsNullOrWhiteSpace(newProp))
                {
                    atts.Add(new LdapAttribute(prop.Name, newProp));
                }
            }

            var ent = new LdapEntry(dnString, atts);

            try
            {
                _connection.Add(ent);
            }
            catch (Exception e)
            {
                //Log.Error(e, "Failed to modify ldap.");
                return null;
            }

            var createdResult = await Search<T>(dn, objString);
            return (createdResult.Any()) ? createdResult.First() : null;
        }

        public async Task<T> Update<T>(string dn, T obj) where T : Ldap
        {
            var dnString = obj.ToDnString(dn);

            var objString = obj.ToString();

            //check exists
            var searchResult = await Search<T>(dn, objString);
            if (!searchResult.Any())
            {
                return null;
            }

            if (searchResult.Count() != 1)
            {
                throw new Exception($"Unexpected record count during LDAP update. Expected 1 record, got {searchResult.Count()} record(s).");
            }

            //work out what properties are in this object
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead)
                .Where(p => p.Name != nameof(Ldap.Dn))
                .Where(p => p.Name != nameof(Ldap.EntryUuid))
                .Where(p => p.Name != nameof(Ldap.CreateTimestamp))
                .Where(p => p.Name != nameof(Ldap.ModifyTimestamp))
                .ToArray();

            //map to the ldap
            var mods = new List<LdapModification>();

            foreach (var prop in props)
            {
                if (prop.GetValue(obj) is string[] newPropArray && newPropArray.Any())
                {
                    mods.Add(new LdapModification(LdapModification.Replace, new LdapAttribute(prop.Name, newPropArray)));
                }
                else if (prop.GetValue(obj) is string newProp && !string.IsNullOrEmpty(newProp))
                {
                    mods.Add(new LdapModification(LdapModification.Replace, new LdapAttribute(prop.Name, newProp)));
                }
            }

            try
            {
                _connection.Modify(dnString, mods.ToArray());
            }
            catch (Exception e)
            {
                //Log.Error(e, "Failed to modify ldap.");
                return null;
            }

            var updatedSearchResult = await Search<T>(dn, objString);
            return (updatedSearchResult.Any()) ? updatedSearchResult.First() : null;
        }

        public async Task<bool> AddMember<T>(string dn, string modCn, string member) where T : Ldap
        {
            var filter = $"(&(uniqueMember={member})({modCn}))";
            var searchResult = await Search<T>(dn, filter);

            if (searchResult.Any())
            {
                return true; //already added
            }

            var modDn = $"{modCn},{dn}";
            var success = await AddRemoveMember(modDn, member);

            if (success)
            {
                return true;
            }

            //otherwise - group doesn't exist - create it
            var groupIn = new GroupIn()
            {
                Cn = modCn.Replace("cn=", ""),
                UniqueMember = new string[] { member }
            };

            var group = await Create(dn, groupIn);
            if (group != null)
            {
                success = true;
            }

            return success;
        }

        public async Task RemoveMember<T>(string dn, string modCn, string member) where T : Ldap
        {
            var filter = $"(&(uniqueMember={member})({modCn}))";
            var searchResult = await Search<T>(dn, filter);

            if (searchResult.Any())
            {
                var modDn = $"{modCn},{dn}";
                var success = await AddRemoveMember(modDn, member, LdapModification.Delete);

                //last one in group - delete whole group
                if (!success)
                {
                    await Delete<Group>(dn, modCn);
                }
            }
        }

        public async Task<bool> AddRemoveMember(string modDn, string member, int modType = LdapModification.Add)
        {
            var success = true;

            //attempt to add/remove a member to/from a group
            var mods = new LdapModification(modType, new LdapAttribute("uniqueMember", member));
            try
            {
                _connection.Modify(modDn, mods);
            }
            catch (LdapException)
            {
                success = false;
            }
            return success;
        }

        public async Task<T> Delete<T>(string dn, string filter) where T : Ldap
        {
            //check exists
            var searchResult = await Search<T>(dn, filter);
            if (!searchResult.Any())
            {
                return null;
            }

            if (searchResult.Count() != 1)
            {
                throw new Exception($"Unexpected record count during LDAP delete. Expected 1 record, got {searchResult.Count()} record(s).");
            }

            var dnString = searchResult.First().Dn;

            await Delete(dnString);

            return (searchResult.First());
        }

        public async Task Delete(string dn)
        {
            var subNodes = _connection.Search(dn, LdapConnection.ScopeOne, null, null, true);
            foreach (var ent in subNodes)
            {
                //keep deleting until we run out of branches
                await Delete(ent.Dn);
            }

            _connection.Delete(dn);
        }
    }    
}
