using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSA.Model.Dto;
using DSA.Shell.Commands;
using DSA.Shell.ViewModels.Playlist;
using GalaSoft.MvvmLight.Views;

namespace DSA.Shell.ViewModels.Builders
{
    public static class PlaylistViewModelBuilder
    {
        internal static ObservableCollection<PlaylistViewModel> Create(List<PlaylistData> data, NavigateToMediaCommand navigationCommand, IDialogService dialogService)
        {            
          return new ObservableCollection<PlaylistViewModel>(data.Select(md => Create(md, navigationCommand, dialogService)));
        }

        internal static PlaylistViewModel Create(PlaylistData modelData, NavigateToMediaCommand navigationCommand, IDialogService dialogService)
        {
            return new PlaylistViewModel(modelData, modelData.PlaylistItems.Select(md => CreateMediaViewModel(md, modelData.ID)), navigationCommand, dialogService);
        }

        private static PlaylistMediaViewModel CreateMediaViewModel(MediaLink mediaLink, String playListID)
        {
            return new PlaylistMediaViewModel(mediaLink, playListID);
        }

        internal static List<PlaylistData> ConvertToModelData(PlaylistsViewModel playlistsViewModel)
        {
            return playlistsViewModel
                        .PlayLists
                        .Where(plvm => plvm.ModelData.IsEditable)
                        .Select(plvm =>
                                 {
                                     var modelToUpdate = plvm.ModelData;
                                     modelToUpdate.Name = plvm.Name;
                                     modelToUpdate.PlaylistItems = plvm.PlayListItems.Select(plit => plit.Media).ToList();
                                     return modelToUpdate;
                                 })
                        .ToList();
        }

        internal static PlaylistViewModel CreateNew(string name, NavigateToMediaCommand navigationCommand, IDialogService dialogService)
        {
            var newModelData = new PlaylistData() { ID = null, Name = name, IsEditable = true, PlaylistItems = new List<MediaLink>() };
            return  Create(newModelData, navigationCommand, dialogService);
        }
    }
}
