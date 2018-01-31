using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DSA.Model.Dto;
using DSA.Shell.Commands;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using WinRTXamlToolkit.Tools;

namespace DSA.Shell.ViewModels.Playlist
{
    public class PlaylistViewModel : ViewModelBase
    {
        public const string DroppedIndexKey = "droppedIndex";
        private const string DragedItemKey = "dataItem";
        private const string SourceCollectionKey = "sourceCollection";
        private const string SourceCollectionIsEditableKey = "sourceCollectionIsEditable";

        private bool _isPlaylistEdited;
        private bool _isPlaylistEditMode;
        private string _name;
        private ObservableCollection<PlaylistMediaViewModel> _playListItems;
        private RelayCommand<PlaylistMediaViewModel> _deleteMediaCommand;
        private bool _isInternalMode;
        private List<PlaylistMediaViewModel> _allPlayListItems;
        private readonly NavigateToMediaCommand _navigationCommand;
        private readonly IDialogService _dialogService;

        public PlaylistViewModel(
            PlaylistData modelData, 
            IEnumerable<PlaylistMediaViewModel> playlistItems, 
            NavigateToMediaCommand navigationCommand, 
            IDialogService dialogService
           )
        {
            _navigationCommand = navigationCommand;
            _dialogService = dialogService;
            Name = modelData.Name;
            ModelData = modelData;
            var ordered = playlistItems.OrderBy(p => p.Name);
            AllPlayListItems = new List<PlaylistMediaViewModel>(ordered);
            PlayListItems = new ObservableCollection<PlaylistMediaViewModel>(ordered);
        }

        public PlaylistData ModelData
        {
            get;
            private set;
        }

        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        public bool IsPlaylistEdited
        {
            get
            {
                return _isPlaylistEdited;
            }
            set
            {
                if (ModelData.IsEditable)
                {
                    Set(ref _isPlaylistEdited, value);
                }
            }
        }

        public bool IsPlaylistEditMode
        {
            get
            {
                return _isPlaylistEditMode;
            }
            set
            {
                
              Set(ref _isPlaylistEditMode, value);
              IsPlaylistEdited = value;
            }
        }

        public bool IsInternalMode
        {
            get
            {
                return _isInternalMode;
            }
            set
            {

                Set(ref _isInternalMode, value);
                AllPlayListItems.ForEach(pli => pli.Media.IsInternalMode = value);
                PlayListItems = new ObservableCollection<PlaylistMediaViewModel>(AllPlayListItems.Where(pli => pli.Media.IsVisible));
            }
        }

        public List<PlaylistMediaViewModel> AllPlayListItems
        {
            get { return _allPlayListItems; }
            set { Set(ref _allPlayListItems, value); }
        }

        public ObservableCollection<PlaylistMediaViewModel> PlayListItems
        {
            get { return _playListItems; }
            set { Set(ref _playListItems, value); }
        }

        public NavigateToMediaCommand NavigateToMediaCommand
        {
            get { return _navigationCommand; }
        }


        private RelayCommand<DragEventArgs> _dropItemCommand;
        public RelayCommand<DragEventArgs> DropItemCommand
        {
            get
            {
                return _dropItemCommand ?? (_dropItemCommand = new RelayCommand<DragEventArgs>(
                    async (arg) =>
                    {
                        var droppedVm = arg.Data.Properties.Keys.Contains(DragedItemKey) 
                                                ? arg.Data.Properties[DragedItemKey] as PlaylistMediaViewModel
                                                : null;

                        var sourceCollection = arg.Data.Properties.Keys.Contains(SourceCollectionKey)
                                                ? arg.Data.Properties[SourceCollectionKey] as ObservableCollection<PlaylistMediaViewModel>
                                                : null;

                        var droppedIndex = arg.Data.Properties.Keys.Contains(DroppedIndexKey)
                                                ? (int)arg.Data.Properties[DroppedIndexKey]
                                                : int.MaxValue;

                        var sourceCollectionIsEditable = arg.Data.Properties.Keys.Contains(SourceCollectionIsEditableKey)
                                                ? (bool)arg.Data.Properties[SourceCollectionIsEditableKey]
                                                : false;

                        if (sourceCollection == null || droppedVm == null || PlayListItems.Any(vm => vm.ID == droppedVm.ID))
                        {
                            return;
                        }

                        if (sourceCollectionIsEditable)
                        {
                            await _dialogService.ShowMessage("Would you like to move or copy the file to this shelf?",
                          "Please Confirm",
                           buttonConfirmText: "Move",
                           buttonCancelText: "Copy",
                           afterHideCallback: (move) =>
                           {
                               AddItemToPlaylist(droppedVm, droppedIndex);
                               if (move)
                               {
                                   sourceCollection.Remove(droppedVm);
                               }
                           });
                        }
                        else
                        {
                            AddItemToPlaylist(droppedVm, droppedIndex);
                        }
                    }
                    ));
            }
        }

        private void AddItemToPlaylist(PlaylistMediaViewModel droppedVm, int droppedIndex)
        {
            if (droppedIndex > PlayListItems.Count())
            {
                PlayListItems.Add(droppedVm);
            }
            else
            {
                PlayListItems.Insert(droppedIndex, droppedVm);
            }
        }

        private RelayCommand<DragItemsStartingEventArgs> _dragItemsStartingCommand;
        public RelayCommand<DragItemsStartingEventArgs> DragItemsStartingCommand
        {
            get
            {
                return _dragItemsStartingCommand ?? (_dragItemsStartingCommand = new RelayCommand<DragItemsStartingEventArgs>(
                    (arg) =>
                    {
                        var vm = arg.Items[0] as PlaylistMediaViewModel;
                        arg.Data.Properties.Add(DragedItemKey, vm);
                        arg.Data.Properties.Add(SourceCollectionKey, PlayListItems);
                        arg.Data.Properties.Add(SourceCollectionIsEditableKey, ModelData.IsEditable);
                    }
                    ));
            }
        }

        private RelayCommand _deleteShelfCommand;
        public RelayCommand DeleteShelfCommand
        {
            get
            {
                return _deleteShelfCommand ?? (_deleteShelfCommand = new RelayCommand(
                async () =>
                {

                    await _dialogService.ShowMessage($"Would you like to delete shelf {this.Name}?",
                       "Please Confirm",
                        buttonConfirmText: "Delete",
                        buttonCancelText: "Cancel",
                        afterHideCallback: (confirm) =>
                        {
                            if (confirm)
                            {
                                RemoveMethod();
                            }
                        });
                }));
            }
        }

        public RelayCommand<PlaylistMediaViewModel> DeleteMediaCommand
        {
            get
            {
                return _deleteMediaCommand ?? (_deleteMediaCommand = new RelayCommand<PlaylistMediaViewModel>(
                async (vm) =>
                {
                    await _dialogService.ShowMessage($"Would you like to delete {vm.Name}?",
                       "Please Confirm",
                        buttonConfirmText: "Delete",
                        buttonCancelText: "Cancel",
                        afterHideCallback: (confirm) =>
                        {
                            if (confirm)
                            {
                                PlayListItems.Remove(vm);
                            }
                        });
                }));
            }
        }

        public Action RemoveMethod { get; set; }
    }
}