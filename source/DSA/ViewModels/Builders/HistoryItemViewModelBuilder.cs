using System.Collections.Generic;
using System.Linq;
using DSA.Model.Dto;
using DSA.Shell.ViewModels.History;

namespace DSA.Shell.ViewModels.Builders
{
    public static class HistoryItemViewModelBuilder
    {
        public static IEnumerable<HistoryItemViewModel> Create(List<HistoryItemDto> modelData)
        {
           return  modelData.Select(hi => new HistoryItemViewModel(hi));
        }
    }
}
