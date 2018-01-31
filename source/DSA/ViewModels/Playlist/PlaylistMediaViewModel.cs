using DSA.Model.Dto;
using DSA.Model.Enums;
using DSA.Shell.ViewModels.Abstract;

namespace DSA.Shell.ViewModels.Playlist
{
    public class PlaylistMediaViewModel : DSAMediaItemViewModelBase, IHavePlaylist
    {
        public PlaylistMediaViewModel(MediaLink media, string playListID)
        {
            _media = media;
            PlaylistID = playListID;
        }

        public string ID 
        {
            get
            {
                return _media.ID;
            }
        }

        public string Name
        {
            get { return _media.Name; }
        }


        public string PlaylistID
        {
            get;
            private set;
        }
    }
}