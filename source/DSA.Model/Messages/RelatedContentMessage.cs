using DSA.Model.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DSA.Model.Messages
{
    public class RelatedContentMessage
    {
        public RelatedContentMessage(List<MediaLink> mediaLinks)
        {
            MediaLinks = mediaLinks;
        }

        public List<MediaLink> MediaLinks { get; private set; }
    }
}
