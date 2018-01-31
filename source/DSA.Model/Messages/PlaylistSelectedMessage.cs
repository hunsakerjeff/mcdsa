namespace DSA.Model.Messages
{
    public class PlaylistSelectedMessage
    {
        public PlaylistSelectedMessage(string playlistID)
        {
            PlaylistID = playlistID;
        }

        public string PlaylistID { get; private set; }
    }
}
