using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class Playlist : SObject
    {
        private static readonly Lazy<Playlist> Lazy = new Lazy<Playlist>(() => new Playlist(SmartStore.GetGlobalSmartStore()));

        public static Playlist SyncUpInstance => Lazy.Value;

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
        };

        internal override List<string> FieldsToSyncUp { get; set; } = new List<string>()
        {
            "Name"
        };

        internal string SoqlWhereIOwn = "WHERE {0}__IsFeatured__c = true " +
                            "OR ownerid = '{1}'";

        internal string SoqlWhereIFollow
        {
            get
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec("EntitySubscription", "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                var ids = globalStore.Query(querySpec, 0).Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.EntitySubscription>(item.ToString()).ParentId);
                if (!ids.Any())
                {
                    return null;
                }

                return "WHERE Id in ('" + ids.Aggregate((i, j) => i + "','" + j) + "')";
            }
        }

        public bool GetPlaylistWhichIFollow { get; set; }

        public void DontClearSoup()
        {
            ClearSoup = false;
        }

        internal override string SoqlQuery
        {
            get
            {
                var where = SoqlWhereIOwn;
                if (GetPlaylistWhichIFollow)
                {
                    where = SoqlWhereIFollow;
                    if (where == null)
                    {
                        return null;
                    }
                }
                var query = "SELECT Id," +
                            "LastModifiedDate," +
                            "{0}__Description__c," +
                            "{0}__External_Approved__c," +
                            "{0}__IsFeatured__c," +
                            "{0}__Language_Code__c," +
                            "{0}__Language__c," +
                            "{0}__Large_Image_URL__c," +
                            "{0}__Order__c," +
                            "{0}__Query__c," +
                            "{0}__Shared_Internally__c," +
                            "{0}__Small_Image_URL__c," +
                            "Name, " +
                            "OwnerId " +
                            "FROM {0}__DSA_Playlist__c " +
                            where;

                return string.Format(query, Prefix, _currentUserPredicate.Id); ;
            }
        }

        internal override void LocalIdChangeHandler(string previousId, string newId)
        {
            string childSoupName = "PlaylistContent";
            string childFieldName = $"{Prefix}__Playlist__c";

            string smartSql = "SELECT {" + childSoupName + ":_soup} FROM {" + childSoupName + "} WHERE {" + childSoupName + ":" + childFieldName + "} = '" + previousId + "'";

            QuerySpec querySpec = QuerySpec.BuildSmartQuerySpec(smartSql, 10);
            var count = (int)Store.CountQuery(querySpec);
            querySpec = QuerySpec.BuildSmartQuerySpec(smartSql, count);

            JArray recordsToUpdate = Store.Query(querySpec, 0);
            if (recordsToUpdate != null && recordsToUpdate.Count > 0)
            {
                foreach (var recordToUpdate in recordsToUpdate)
                {
                    JObject item = recordToUpdate[0].ToObject<JObject>();
                    item[childFieldName] = newId;
                    JObject res = Store.Upsert(childSoupName, item);
                }
            }
        }

        private readonly User _currentUserPredicate;

        internal Playlist(SmartStore store, User currentUserPredicate) : base(store)
        {
            GetPlaylistWhichIFollow = false;
            if (currentUserPredicate == null || currentUserPredicate is NullUser) { throw new ArgumentNullException(nameof(User), "Parameter currentUserPredicate must be concrete not null object"); }

            _currentUserPredicate = currentUserPredicate;

            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        private Playlist(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }
    }
}