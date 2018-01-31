using System;
using System.Collections.Generic;
using System.Linq;

namespace DSA.Model.Dto
{
    public class PlayListCollection : List<MediaLink>
    {
        public Option<MediaLink> GetNext(MediaLink current)
        {
            if(this.Any() == false || current == null)
            {
                return Option<MediaLink>.None();
            }

            var currentIndex = FindIndex(md => md.ID == current.ID);
            if(currentIndex == -1)
            {
                return Option<MediaLink>.None();
            }

            return (currentIndex + 1) >= this.Count 
                    ? Option<MediaLink>.None()
                    : Option<MediaLink>.Some(this[currentIndex + 1]);
        }

        public Option<MediaLink> GetPrevious(MediaLink current)
        {
            if (this.Any() == false || current == null)
            {
                return Option<MediaLink>.None();
            }

            var currentIndex = FindIndex(md => md.ID == current.ID);

            return (currentIndex - 1) < 0
                    ? Option<MediaLink>.None()
                    : Option<MediaLink>.Some(this[currentIndex - 1]);
        }

    }
}
