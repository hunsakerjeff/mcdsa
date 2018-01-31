namespace DSA.Model.Models
{
    public class SuccessfulSync
    {
        public static readonly string SoupName = "SuccessfulSyncState";

        public static readonly string SyncIdIndexKey = "SyncId";

        public static readonly string TransactionItemTypeIndexKey = "TransactionItemType";

        public string SyncId { get; set; }
        public string TransactionItemType { get; set; }

        public string DocVersionId { get; set; }
        public System.DateTime LastModifiedDate { get; set; }
    }
}
