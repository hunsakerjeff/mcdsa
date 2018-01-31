using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    internal class PlaylistContent : SObject
    {
        private static readonly Lazy<PlaylistContent> Lazy = new Lazy<PlaylistContent>(() => new PlaylistContent(SmartStore.GetGlobalSmartStore()));

        public static PlaylistContent SyncUpInstance => Lazy.Value;

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec($"{Prefix}__Playlist__c", SmartStoreType.SmartString)//we need to index this field to maintain object relation when local id is changed to salesforce id
        };

        internal override List<string> FieldsToSyncUp { get; set; } = new List<string>()
        {
            "Name",
            "{0}__ContentId__c",
            "{0}__Order__c",
            "{0}__Playlist__c"
        };

        internal override List<string> FieldsToExcludeOnUpdate { get; set; } = new List<string>()
        {
            "{0}__Playlist__c"
        };

        internal override string SoqlQuery
        {
            get
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec("Playlist", "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                var ids = globalStore.Query(querySpec, 0).Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.Playlist>(item.ToString()).Id);
                if (!ids.Any())
                {
                    return null;
                }

                var query = "SELECT Id,LastModifiedDate,{0}__ExternalId__c,{0}__ContentId__c, {0}__Order__c, {0}__Playlist__c " +
                    "FROM {0}__Playlist_Content_Junction__c " +
                    "WHERE {0}__Playlist__c IN ('" + ids.Aggregate((i, j) => i + "','" + j) + "')";
                return string.Format(query, Prefix, _currentUserPredicate.Id);
            }
        }

        private readonly User _currentUserPredicate;

        internal PlaylistContent(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser) { throw new ArgumentNullException("Parameter currentUserPredicate must be concrete not null object"); }

            _currentUserPredicate = currentUserPredicate;

            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        public PlaylistContent(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }
    }
}