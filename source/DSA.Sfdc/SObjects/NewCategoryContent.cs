using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Model;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;

namespace DSA.Sfdc.SObjects
{
    public class NewCategoryContent
    {

        private static readonly Lazy<NewCategoryContent> Lazy = new Lazy<NewCategoryContent>(() => new NewCategoryContent());

        public static NewCategoryContent Instance => Lazy.Value;

        private readonly SmartStore _store;

        private NewCategoryContent()
        {
            _store = SmartStore.GetGlobalSmartStore();
            SetupNewCategoryContentSoupIfNotExists();
        }

        public string SoupName => GetType().Name;

        public void SaveToSoup(List<Model.Models.CategoryContent> newCategoryContents)
        {
            if (newCategoryContents == null)
            {
                return;
            }
            foreach (var newCategoryContent in newCategoryContents)
            {
                _store.Upsert(SoupName, JObject.Parse(CustomPrefixJsonConvert.SerializeObject(newCategoryContent)));
            }
        }

        public void RecreateClearSoup()
        {
            if (_store.HasSoup(SoupName))
            {
                _store.DropSoup(SoupName);
            }

            SetupNewCategoryContentSoupIfNotExists();
        }

        private void SetupNewCategoryContentSoupIfNotExists()
        {
            IndexSpec[] indexSpecs =
            {
                new IndexSpec("Id", SmartStoreType.SmartString),
                new IndexSpec(Model.Models.CategoryContent.CategoryIdIndexKey, SmartStoreType.SmartString),
            };

            if (!_store.HasSoup(SoupName))
            {
                _store.RegisterSoup(SoupName, indexSpecs);
            }
        }

        public IList<Model.Models.CategoryContent> GetAllFromSoup()
        {
            SetupNewCategoryContentSoupIfNotExists();
            var querySpec = QuerySpec.BuildAllQuerySpec(SoupName, "Id", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(_store);
            var results = _store.Query(querySpec, 0);

            var categoryContentList = results.Select(item => CustomPrefixJsonConvert.DeserializeObject<Model.Models.CategoryContent>(item.ToString())).ToList();

            return categoryContentList;
        }
    }
}

