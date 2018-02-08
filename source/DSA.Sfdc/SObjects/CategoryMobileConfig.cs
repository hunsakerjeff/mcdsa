using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using DSA.Sfdc.QueryUtil;
using Salesforce.SDK.SmartStore.Store;


namespace DSA.Sfdc.SObjects
{
    internal class CategoryMobileConfig : SObject
    {
        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString),
            new IndexSpec(Model.Models.CategoryMobileConfig.MobileAppConfigurationIdIndexKey, SmartStoreType.SmartString),
            new IndexSpec(Model.Models.CategoryMobileConfig.CategoryIdIndexKey, SmartStoreType.SmartString),
        };

        private readonly User _currentUserPredicate;

        internal override string SoqlQuery
        {
            get
            {
                var query = "SELECT " +
                            "Id," + 
                            "LastModifiedDate," +
                            "{0}__Button_Text_Align__c," +
                            "{0}__CategoryBundleId__c," +
                            "{0}__CategoryId__c," +
                            "{0}__ContentAttachmentId__c," +
                            "{0}__ContentOverAttachmentId__c," +
                            "{0}__GalleryHeadingTextColor__c," +
                            "{0}__GalleryHeadingText__c," +
                            "{0}__IsDefault__c," +
                            "{0}__IsDraft__c," +
                            "{0}__LandscapeAttachmentId__c," +
                            "{0}__LandscapeX__c," +
                            "{0}__LandscapeY__c," +
                            "{0}__MAC_in_Edit__c," +
                            "{0}__MobileAppConfigurationId__c," +
                            "{0}__OverlayBgAlpha__c," +
                            "{0}__OverlayBgColor__c," +
                            "{0}__OverlayTextColor__c," +
                            "{0}__PortraitAttachmentId__c," +
                            "{0}__PortraitX__c," +
                            "{0}__PortraitY__c," +
                            "{0}__Sub_Category_Background_Color__c," +
                            "{0}__Top_Level_Category__c " +
                            "FROM " + 
                            "{0}__CategoryMobileConfig__c " +
                            "WHERE " +
                            "{0}__MobileAppConfigurationId__c " + 
                            "in " +
                            "(SELECT Id FROM {0}__MobileAppConfig__c WHERE {0}__Profiles__c INCLUDES (\'{1}\')) AND {0}__IsDraft__c = false";
                          //"(SELECT Id FROM {0}__MobileAppConfig__c WHERE {0}__Profiles__c INCLUDES (\'{1}\') AND {0}__Active__c = true AND {0}__IsDraft__c = false";

                return string.Format(query, Prefix, _currentUserPredicate.ProfileId);
            }
        }

        internal CategoryMobileConfig(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser)
            {
                throw new ArgumentNullException(nameof(User), "Parameter currentUserPredicate must be concrete not null object");
            }

            if (IndexedFieldsForSObjects != null)
            {
                AddIndexSpecItems(IndexedFieldsForSObjects);
            }

            _currentUserPredicate = currentUserPredicate;
        }

        // Methods
        internal IList<Model.Models.CategoryMobileConfig> GetAll()
        {
            RegisterSoup();
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Id", QuerySpec.SqlOrder.ASC, PageSize).RemoveLimit(Store);
            var results = Store.Query(querySpec, 0);

            return results.Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.CategoryMobileConfig>(item.ToString())).ToList();
        }
    }
}
