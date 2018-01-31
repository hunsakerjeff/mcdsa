using DSA.Model.Dto;
using DSA.Shell.ViewModels.Abstract;

namespace DSA.Shell.ViewModels.History
{
    public class HistoryItemViewModel : DSAMediaItemViewModelBase
    {
        private readonly HistoryItemDto _historyItem;

        public HistoryItemViewModel(HistoryItemDto historyItem)
        {
            _historyItem = historyItem;
            _media = _historyItem.Media;
        }

        public string DisplayName
        {
            get { return $"{ConfigurationName} - {Name}"; }
        }

        public string Name
        {
            get { return _historyItem.Media.Name; }
        }

        public string Description
        {
            get { return _historyItem.Media.Description; }
        }

        public string ConfigurationName
        {
            get
            {
                return _historyItem.ConfigurationName;
            }
        }
    }
}