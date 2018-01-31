using System.Collections.Generic;

namespace DSA.Model.Dto
{
    public class PlaylistData
    {
        public virtual string ID { get; set; }

        public virtual long __localId { get; set; }

        public virtual string Name { get; set; }

        public virtual bool IsEditable { get; set; }

        public virtual string OwnerId { get; set; }

        public List<MediaLink> PlaylistItems { get; set; }
    }

    public class PersonalPlaylistData : PlaylistData
    {
        public override string ID
        {
            get
            {
                return "__personal__";
            }
            set { }
        }

        public override string Name
        {
            get { return string.Empty; }
            set { }
        }

        public override bool IsEditable
        {
            get { return false; }
            set { }
        }

    }
}
