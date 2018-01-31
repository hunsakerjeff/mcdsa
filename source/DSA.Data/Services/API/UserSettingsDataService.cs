using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Sfdc.QueryUtil;
using DSA.Sfdc.SerializationUtil;
using DSA.Sfdc.SObjects;
using Salesforce.SDK.SmartStore.Store;
using DTO = DSA.Model.Dto;

namespace DSA.Data.Services.API
{
    public class UserSettingsDataService : IUserSettingsDataService
    {
        public async Task<DTO.UserSettingsDto> GetUserSettings(string userID)
        {
            return await Task.Factory.StartNew(() =>
            {
                var globalStore = SmartStore.GetGlobalSmartStore();
                var querySpec = QuerySpec.BuildAllQuerySpec("UserSettings", "UserId", QuerySpec.SqlOrder.ASC, SfdcConfig.PageSize).RemoveLimit(globalStore);

                return globalStore
                        .Query(querySpec, 0)
                        .Select(item => CustomPrefixJsonConvert.DeserializeObject<DTO.UserSettingsDto>(item.ToString()))
                        .FirstOrDefault(u => u.UserId == userID);
            });
        }

        public async Task SaveSettings(DTO.UserSettingsDto userSettings)
        {
            await Task.Factory.StartNew(() =>
            {
                UserSettings.Instance.SaveToSoup(userSettings);
            });
        }
    }
}
