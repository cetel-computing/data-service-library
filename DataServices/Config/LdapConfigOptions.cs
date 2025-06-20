namespace DataServices.Config
{
    public class LdapConfigOptions
    {
        public IEnumerable<LdapHost> Hosts { get; set; }

        public string DefaultUsername { get; set; }

        public string DefaultPassword { get; set; }

        public string RootBranchDn { get; set; }

        public string UsersOu { get; set; }

        public string MyNetworksOu { get; set; }

        public string OrganisationsOu { get; set; }

        public int OrgDnElements { get; set; }

        public int DomainDnElements { get; set; }

        public string BlacklistedOu { get; set; }

        public string WhitelistedOu { get; set; }

        public string OrgAdminCn { get; set; }

        public string DomainAdminCn { get; set; }

        public string FinanceAdminCn { get; set; }

        public string DashboardUserCn { get; set; }

        public string O365AuthorisedSendersOu { get; set; }

        public string GwsAuthorisedSendersOu { get; set; }

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
