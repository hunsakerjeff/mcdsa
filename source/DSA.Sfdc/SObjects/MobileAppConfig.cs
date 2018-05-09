using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model.Models;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;
using System.Threading.Tasks;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;

namespace DSA.Sfdc.SObjects
{
    internal class MobileAppConfig : SObject
    {
        private readonly User _currentUserPredicate;

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec("TitleText", SmartStoreType.SmartString)
        };

        internal override string SoqlQuery
        {
            get
            {
                var query = " SELECT " +
                            " Id,LastModifiedDate,{0}__Active__c,{0}__ButtonDefaultAttachmentId__c, {0}__ButtonHighlightAttachmentId__c,{0}__ButtonHighlightTextColor__c,{0}__ButtonTextAlpha__c," +
                            "{0}__ButtonTextColor__c,{0}__Check_In_Enabled__c,{0}__inEdit__c,{0}__IntroTextAlpha__c,{0}__IntroTextColor__c,{0}__IntroText__c,{0}__LandscapeAttachmentId__c,{0}__Language__c," +
                            "{0}__LinkToEditor__c,{0}__LogoAttachmentId__c,{0}__PortraitAttachmentId__c,{0}__Profiles__c,{0}__ProfileText__c,{0}__Profile_Names__c,{0}__Report_an_Issue__c,{0}__TitleBgAlpha__c," +
                            "{0}__TitleBgColor__c,{0}__TitleTextAlpha__c,{0}__TitleTextColor__c,{0}__TitleText__c,Name,OwnerId " + 
                            "FROM " +
                            "{0}__MobileAppConfig__c " +
                            "WHERE " +
                            "{0}__Profiles__c INCLUDES (\'{1}\') AND {0}__Active__c = true";

                return string.Format(query, Prefix, _currentUserPredicate.ProfileId);
            }
        }

        // CTOR
        internal MobileAppConfig(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser)
            {
                throw new ArgumentNullException("Parameter currentUserPredicate must be concrete not null object");
            }

            if (IndexedFieldsForSObjects != null)
            {
                AddIndexSpecItems(IndexedFieldsForSObjects);
            }

            _currentUserPredicate = currentUserPredicate;
        }

        // Methods
        internal IList<Model.Models.MobileAppConfig> GetFromSoup()
        {
            RegisterSoup();
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize).RemoveLimit(Store);
            var results = Store.Query(querySpec, 0);
            return results.Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.MobileAppConfig>(item.ToString())).ToList();
        }

        internal async Task<IList<Model.Models.MobileAppConfig>> GetFromSoql(SyncManager syncManager)
        {
            List<Model.Models.MobileAppConfig> results = new List<Model.Models.MobileAppConfig>();
            var target = new SoqlSyncDownTarget(SoqlQuery);

            // Sync to JSON
            var _results = await target.StartFetch(syncManager, -1);
            while (_results != null && _results.Count > 0)
            {
                results.AddRange(_results.Select(x => x.ToObject<Model.Models.MobileAppConfig>()).ToList());
                _results = await target.ContinueFetch(syncManager);
            }
            return results;
        }
    }
}