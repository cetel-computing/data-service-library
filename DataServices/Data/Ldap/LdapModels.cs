using System.Linq.Expressions;

namespace DataServices.Data
{
    public abstract class Ldap
    {
        public string Dn { get; set; }

        public string EntryUuid { get; set; }

        public DateTime CreateTimestamp { get; set; }

        public DateTime ModifyTimestamp { get; set; }

        public virtual string ToDnString(string parentDn) => $"{ToString()},{parentDn}";

        public virtual string ToIdString() => $"entryUUID={EntryUuid}";

        public virtual string ToParentDnString() => string.Join(",", Dn.Split(',').Skip(1));

        public virtual string ToParentDnString(int elementsRequired)
        {
            var dns = Dn.Split(',');
            var skip = dns.Length > elementsRequired ? dns.Length - elementsRequired : 0;

            return string.Join(",", dns.Skip(skip));
        }

        public abstract override string ToString();
    }

    public abstract class LdapDescription : Ldap
    {
        public string Description { get; set; }
    }

    public static class LdapExtensions
    {
        public static List<T> OrderByDynamic<T>(this List<T> list, string sortColumn, SortOrder? sortOrder = SortOrder.Asc)
        {
            var queryList = list.AsQueryable();

            // Dynamically creates a call like this: query.OrderBy(p => p.SortColumn)
            var parameter = Expression.Parameter(typeof(T), "p");

            var command = sortOrder == SortOrder.Asc ? "OrderBy" : "OrderByDescending";

            //find the sort column or use the dn
            var property = typeof(T).GetProperty(sortColumn) ?? typeof(T).GetProperty("Dn");

            // this is the part p.SortColumn
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);

            // this is the part p => p.SortColumn
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);

            // finally, call the "OrderBy" / "OrderByDescending" method with the order by lambda expression
            var resultExpression = Expression.Call(typeof(Queryable), command, new[] { typeof(T), property.PropertyType },
                queryList.Expression, Expression.Quote(orderByExpression));

            return queryList.Provider.CreateQuery<T>(resultExpression).ToList();
        }
    }

    public class Group : Ldap
    {
        public string Cn { get; set; }

        public string[] UniqueMember { get; set; }

        public override string ToString() => $"cn={Cn}";
    }

    public class GroupIn : Group
    {
        public string[] ObjectClass => new[] { "groupOfUniqueNames", "top" };
    }

    public enum LdapSearchScope
    {
        Base = 0,
        CurrentBranch = 1,
        AllLevels = 2
    }
    
    public enum SortOrder
    {
        Desc,
        Asc
    }
}
