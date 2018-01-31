using System;
using System.Linq;
using System.Threading.Tasks;
using DSA.Model.Models;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using DSA.Sfdc.Sync;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.Auth;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Manager;
using Salesforce.SDK.SmartSync.Model;

namespace DSA.Sfdc.SObjects
{
    /// <summary>
    /// User
    /// Represents a user in your organization
    /// https://developer.salesforce.com/docs/atlas.en-us.api.meta/api/sforce_api_objects_user.htm
    /// </summary>
    internal class CurrentUser : SObject
    {

        public static string[] Fields = { "Id", "ProfileId", "LastModifiedDate" };

        public static string[] AlternativeFields = { "Id", "{0}__Custom_Profile_ID__c", "LastModifiedDate" };

        public static bool UseAlternativeFields;

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec("Id", SmartStoreType.SmartString)
        };

        internal override string SoqlQuery
        {
            get
            {
                string query = SOQLBuilder.GetInstanceWithFields(UseAlternativeFields ? AlternativeFields : Fields)
                    .From("User")
                    .Where($"Id=\'{_account.UserId}\'")
                    .Limit(1)
                    .Build();
                return string.Format(query, Prefix);
            }

        }

        private readonly Account _account;
        internal CurrentUser(SmartStore store) : base(store)
        {
            _account = AccountManager.GetAccount();

            if (_account == null)
            {
                throw  new InvalidOperationException("OAuth not valid or user logged out.");
            }

            if (IndexedFieldsForSObjects != null) AddIndexSpecItems(IndexedFieldsForSObjects);
        }

        internal User GetCurrentUserFromSoup()
        {
            User currentUser = new NullUser();

            var querySpec = QuerySpec.BuildExactQuerySpec(SoupName, "Id", _account.UserId, 10);
            var resultsArr = Store?.Query(querySpec, 0);
            var resultToken = resultsArr?.FirstOrDefault();

            if (resultToken != null)
            {
                currentUser = CustomPrefixJsonConvert.DeserializeObject<User>(resultToken.ToString()); 
            }

            return currentUser;
        }

        internal async Task<User> GetCurrentUserFromSoql(SyncManager syncManager)
        {
            User currentUser = new NullUser();
            JArray results;
            try
            {
                var target = new SoqlSyncDownTarget(SoqlQuery);
                if (target.hasTokenExpired())
                {
                    //logout
                    await ObjectSyncDispatcher.Instance.RefreshToken();
                    return currentUser;
                }
                results = await target.StartFetch(syncManager, -1);
            }
            catch
            {
                UseAlternativeFields = true;
                var target = new SoqlSyncDownTarget(SoqlQuery);
                results = await target.StartFetch(syncManager, -1);
            }

            var curUser = results?.Select(x => CustomPrefixJsonConvert.DeserializeObject<User>(x.ToString())).SingleOrDefault();
            return curUser ?? currentUser;
        }
    }
}