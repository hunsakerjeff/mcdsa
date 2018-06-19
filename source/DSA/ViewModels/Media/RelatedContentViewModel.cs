using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSA.Shell.ViewModels.Abstract;
using DSA.Model.Dto;
using DSA.Shell.Commands;

namespace DSA.Shell.ViewModels.Media
{
    public class RelatedContentViewModel : DSAMediaItemViewModelBase
    {
        // Properties
        public RelatedItemSelectedCommand RelatedItemSelectedCommand { get; private set; }

        public string Description
        {
            get { return _media.Document.Description; }
        }

        public string Title
        {
            get { return _media.Document.Title; }
        }


        // CTOR
        public RelatedContentViewModel(MediaLink media, RelatedItemSelectedCommand relatedItemSelectedCommand)
        {
            _media = media;
            RelatedItemSelectedCommand = relatedItemSelectedCommand;
        }
    }
}
