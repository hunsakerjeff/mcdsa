using System;
using DSA.Model.Models;
using DSA.Sfdc.SObjects.Abstract;
using Salesforce.SDK.SmartStore.Store;

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
                var query = " SELECT Id,LastModifiedDate,{0}__Active__c,{0}__ButtonDefaultAttachmentId__c," +
                            "{0}__ButtonHighlightAttachmentId__c,{0}__ButtonHighlightTextColor__c,{0}__ButtonTextAlpha__c," +
                            "{0}__ButtonTextColor__c,{0}__Check_In_Enabled__c,{0}__inEdit__c,{0}__IntroTextAlpha__c," +
                            "{0}__IntroTextColor__c,{0}__IntroText__c,{0}__LandscapeAttachmentId__c,{0}__Language__c," +
                            "{0}__LinkToEditor__c,{0}__LogoAttachmentId__c,{0}__PortraitAttachmentId__c,{0}__Profiles__c," +
                            "{0}__ProfileText__c,{0}__Profile_Names__c,{0}__Report_an_Issue__c,{0}__TitleBgAlpha__c," +
                            "{0}__TitleBgColor__c,{0}__TitleTextAlpha__c,{0}__TitleTextColor__c,{0}__TitleText__c," +
                            "Name,OwnerId FROM {0}__MobileAppConfig__c "
                             + "WHERE {0}__Profiles__c INCLUDES (\'{1}\') AND {0}__Active__c = true";

                return string.Format(query, Prefix, _currentUserPredicate.ProfileId);
            } 
        }

        internal MobileAppConfig(SmartStore store, User currentUserPredicate) : base(store)
        {
            if (currentUserPredicate == null || currentUserPredicate is NullUser) { throw new ArgumentNullException("Parameter currentUserPredicate must be concrete not null object"); }

            _currentUserPredicate = currentUserPredicate;

            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }
    }
}