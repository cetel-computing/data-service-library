namespace DataServices.Config
{
    public class LdapConfigOptions
    {
        public IEnumerable<LdapHost> Hosts { get; set; }

        public string DefaultUsername { get; set; }

        public string DefaultPassword { get; set; }

        public string RootBranchDn { get; set; }

        public string UsersOu { get; set; }

        public string OrganisationsOu { get; set; }

        public string MyNetworksCn { get; set; }
    }

    public class LdapHost
    {
        public string Name { get; set; }

        public int Port { get; set; } = 636; //standard LDAP port

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
