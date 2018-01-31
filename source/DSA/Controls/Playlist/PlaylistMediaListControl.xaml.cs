using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DSA.Shell.ViewModels.Playlist;

namespace DSA.Shell.Controls.Playlist
{
    public sealed partial class PlaylistMediaListControl
    {
        public PlaylistMediaListControl()
        {
            InitializeComponent();
            playlistListView.LostFocus += (s, e) => playlistListView.SelectedItem = null;
        }

        private void playlistListView_Drop(object sender, DragEventArgs e)
        {
            var droppedIndex = GetDropedItemIndex(e);
            e.Data.Properties.Add(PlaylistViewModel.DroppedIndexKey, droppedIndex);
        }

        private int GetDropedItemIndex(DragEventArgs e)
        {
            Point pos = e.GetPosition(this.playlistListView.ItemsPanelRoot);
            ListViewItem lvi = (ListViewItem)playlistListView.ContainerFromIndex(0);
            if(lvi == null)
            {
                return 0;
            }

            double itemWidth = lvi.ActualWidth + lvi.Margin.Left + lvi.Margin.Right;
            int index = Math.Min(playlistListView.Items.Count - 1, (int)(pos.X / itemWidth));

            ListViewItem targetItem = (ListViewItem)playlistListView.ContainerFromIndex(index);

            Point positionInItem = e.GetPosition(targetItem);
            if (positionInItem.X > itemWidth / 2)
            {
                index++;
            }

            return Math.Min(playlistListView.Items.Count, index);
        }
    }
}
