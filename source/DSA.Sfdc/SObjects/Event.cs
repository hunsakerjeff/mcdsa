using System;
using System.Collections.Generic;
using System.Linq;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects.Abstract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Salesforce.SDK.SmartStore.Store;
using Salesforce.SDK.SmartSync.Util;

namespace DSA.Sfdc.SObjects
{
    internal class Event : SObject
    {
        private static readonly Lazy<Event> Lazy = new Lazy<Event>(() => new Event(SmartStore.GetGlobalSmartStore()));

        public static Event Instance => Lazy.Value;

        internal IndexSpec[] IndexedFieldsForSObjects => new[]
        {
            new IndexSpec(Constants.Id, SmartStoreType.SmartString)
        };

        internal override bool DeleteAfterSyncUp { get; set; } = true;

        internal override List<string> FieldsToSyncUp { get; set; } = 
            JObject.FromObject(new Model.Models.Event(), JsonSerializer.Create(CustomJsonSerializerSettings.Instance.Settings))
                    .Properties().Select(p => p.Name).ToList();
        
        private Event(SmartStore store) : base(store)
        {
            if (IndexedFieldsForSObjects != null)
                AddIndexSpecItems(IndexedFieldsForSObjects);
        }

    }
}
