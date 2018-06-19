using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using DSA.Model.Messages;
using DSA.Model.Dto;
using DSA.Shell.ViewModels.Media;
using DSA.Shell.Commands;
using GalaSoft.MvvmLight.Views;
using GalaSoft.MvvmLight.Command;
using System;
using Windows.UI.Xaml.Controls;
using DSA.Shell.Controls.Media;

namespace DSA.ViewModel.Media
{
    public interface IClosable
    {
        void Close();
    }

    public class RelatedContentFlyoutViewModel : ViewModelBase
    {
        // Attributes
        private List<RelatedContentViewModel> _relatedContentViewModels;
        private RelatedItemSelectedCommand _relatedItemSelectedCommand;

        // Properties
        public List<RelatedContentViewModel> RelatedContentItems
        {
            get { return _relatedContentViewModels; }
            private set { Set(ref _relatedContentViewModels, value); }
        }


        // CTOR / DTOR
        public RelatedContentFlyoutViewModel()
        {
            _relatedItemSelectedCommand = new RelatedItemSelectedCommand();

            // Setup the messages
            RegisterMessages();
        }

        private void SetRelatedContent(List<MediaLink> relatedContent)
        {
            // Create a New View Model List
            IEnumerable<RelatedContentViewModel> rcvms = relatedContent.Select(rc => new RelatedContentViewModel(rc, _relatedItemSelectedCommand));

            // Set the property to fire the UI
            RelatedContentItems = rcvms.ToList();
        }

        private void RegisterMessages()
        {
            Messenger.Default.Register<RelatedContentMessage>(
                this,
                msg =>
                {
                    SetRelatedContent(msg.MediaLinks);
                });
        }
    }
}
