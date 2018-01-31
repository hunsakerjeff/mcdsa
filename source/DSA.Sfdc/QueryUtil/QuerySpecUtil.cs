using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.QueryUtil
{
    public static class QuerySpecUtil
    {
        public static QuerySpec RemoveLimit(this QuerySpec querySpec, SmartStore store)
        {
            if (string.IsNullOrWhiteSpace(querySpec.SoupName) == false && store.HasSoup(querySpec.SoupName) == false)
            {
                return querySpec;
            }

            var count = (int)store.CountQuery(querySpec);
            
            switch(querySpec.QueryType)
            {
                case QuerySpec.SmartQueryType.Exact:
                    return QuerySpec.BuildExactQuerySpec(querySpec.SoupName, querySpec.Path, querySpec.MatchKey, count);
                case QuerySpec.SmartQueryType.Like:
                    return QuerySpec.BuildLikeQuerySpec(querySpec.SoupName, querySpec.Path, querySpec.LikeKey, querySpec.Order, count);
                case QuerySpec.SmartQueryType.Range:
                    return QuerySpec.BuildRangeQuerySpec(querySpec.SoupName, querySpec.Path, querySpec.BeginKey, querySpec.EndKey, querySpec.Order, count);
                default:
                case QuerySpec.SmartQueryType.Smart:
                    return QuerySpec.BuildSmartQuerySpec(querySpec.SmartSql, count);
            }
        }
    }
}
