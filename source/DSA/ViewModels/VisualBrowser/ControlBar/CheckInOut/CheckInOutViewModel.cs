using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Dto;
using DSA.Model.Messages;
using DSA.Shell.ViewModels.Abstract;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using WinRTXamlToolkit.Tools;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar.CheckInOut
{
    public class CheckInOutViewModel : DSAViewModelBase
    {
        private readonly IContactsService _contactsService;
        private readonly IPresentationDataService _presentationDataService;

        private List<ContactViewModel> _contacts;
        private List<ContactViewModel> _allContacts;
        private List<ContactViewModel> _recentContacts;

        private bool _isFlyoutOpen;

        private RelayCommand _chooseAtCheckOutCommand;
        private RelayCommand _cancelCommand;
        private RelayCommand<object> _selectContactCommand;
        private RelayCommand _checkInOutOpenCommand;
        private RelayCommand _finishCheckOutCommand;
        private RelayCommand _goToChooseContactCommand;
        private RelayCommand _goToContentReviewCommand;

        private string _query;
        private string _notes;
        private bool _isChooseAtCheckOutVisible;
        private bool _isContactsListVisible;
        private ObservableCollection<ContentReviewViewModel> _contentsReviews;
        private bool _isChooseAtCheckOutFlow;
        private ContactTypeFilter _contactTypeFilter;

        public CheckInOutViewModel(
            IContactsService contactsService,
            IPresentationDataService presentationDataService,
            ISettingsDataService settingsDataService) : base(settingsDataService)
        {
            _presentationDataService = presentationDataService;
            _contactsService = contactsService;
            DispatcherHelper.CheckBeginInvokeOnUI(async () => await Initialize());
        }

        protected override async Task Initialize()
        {
            try
            {
                ResetState();
                var allContacts = await _contactsService.GetAllContacts();
                _allContacts = allContacts.Select(dto => new ContactViewModel(dto)).ToList();
                var recetntContacts = await _contactsService.GetRecentContacts();
                _recentContacts = recetntContacts.Select(dto => new ContactViewModel(dto)).ToList();
                FilterContacts(String.Empty, ContactTypeFilter.All);
                ChooseAtCheckOutCommand.RaiseCanExecuteChanged();
            }
            catch
            {

            }
        }

        private void ResetState()
        {
            DispatcherHelper.CheckBeginInvokeOnUI(
                () =>
                {
                    Query = string.Empty;
                    ContactTypeFilter = ContactTypeFilter.All;

                    IsChooseAtCheckOutVisible = true;
                    IsContactsListVisible = true;
                    IsChooseAtCheckOutFlow = false;
                    Notes = string.Empty;
                });

        }

        public List<ContactViewModel> Contacts
        {
            get { return _contacts; }
            set { Set(ref _contacts, value); }
        }

        public bool IsFlyoutOpen
        {
            get
            {
                return _isFlyoutOpen;
            }
            set
            {
                Set(ref _isFlyoutOpen, value);
               
                Query = string.Empty;
                ContactTypeFilter = ContactTypeFilter.All;
                Messenger.Default.Send(new IsCheckInFlyoutOpenMessage(value));
            }
        }

        private async Task ProcessCheckOut()
        {
            var reviews = _presentationDataService.GetContentReviews();
            if (reviews.Any())
            {
                IsContactsListVisible = false;
                if(IsChooseAtCheckOutFlow)
                {
                    IsChooseAtCheckOutVisible = false;
                }
                if (ContentsReviews != null)
                {
                    ContentsReviews.CollectionChanged -= OnContentsReviewsCollectionChanged;
                }

                ContentsReviews = new ObservableCollection<ContentReviewViewModel>(reviews.Select(cri => new ContentReviewViewModel(cri)));
                ContentsReviews.CollectionChanged += OnContentsReviewsCollectionChanged;
                ContentsReviews.ForEach(cr => cr.PropertyChanged += 
                (s, e) =>
                {
                    GoToChooseContactCommand.RaiseCanExecuteChanged();
                    FinishCheckOutCommand.RaiseCanExecuteChanged();
                });

                GoToChooseContactCommand.RaiseCanExecuteChanged();
                FinishCheckOutCommand.RaiseCanExecuteChanged();
            }
            else
            {
                ResetState();
                await _presentationDataService.FinishPresentation(string.Empty, new List<ContentReviewInfo>());
                Messenger.Default.Send(new CheckInOutChangedMessage());
            }
        }

        private void OnContentsReviewsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            FinishCheckOutCommand.RaiseCanExecuteChanged();
        }

        public RelayCommand ChooseAtCheckOutCommand
        {
            get
            {
                return _chooseAtCheckOutCommand ?? (_chooseAtCheckOutCommand = new RelayCommand(
                    () =>
                    {
                        _presentationDataService.StartPresentationChooseAtCheckOut();
                        IsChooseAtCheckOutFlow = true;
                        IsContactsListVisible = false;
                        Messenger.Default.Send(new CheckInOutChangedMessage());
                        IsFlyoutOpen = false;
                    },
                    () =>
                    {
                        return _allContacts != null && _allContacts.Any();
                    }));
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                return _cancelCommand ?? (_cancelCommand = new RelayCommand(
                    () =>
                    {
                        IsFlyoutOpen = false;
                    }));
            }
        }

        public RelayCommand<object> SelectContactCommand
        {
            get
            {
                return _selectContactCommand ?? (_selectContactCommand = new RelayCommand<object>(
                    async (contactObj) =>
                    {
                        var contact = contactObj as ContactViewModel;
                        if (contact == null)
                        {
                            return;
                        }

                        if(_presentationDataService.IsPresentationStarted() && this.IsChooseAtCheckOutFlow)
                        {
                            
                            await _presentationDataService.FinishPresentation(contact.Contact, Notes, this.ContentsReviews.Select(vm => vm.ReviewInfo).ToList());
                            DispatcherHelper.CheckBeginInvokeOnUI(() =>
                            {
                                Messenger.Default.Send(new CheckInOutChangedMessage());
                                IsFlyoutOpen = false;
                                IsChooseAtCheckOutFlow = false;
                                ResetState();
                            });
                        }
                        else
                        {
                            _presentationDataService.StartPresentation(contact.Contact);
                            await _contactsService.AddRecentContact(contact.Contact);
                            AddRecentContact(contact);
                            Messenger.Default.Send(new CheckInOutChangedMessage());
                            IsFlyoutOpen = false;
                        }
                    }));
            }
        }

        private void AddRecentContact(ContactViewModel contact)
        {
            if (_recentContacts.Any(c => c.Contact.Id == contact.Contact.Id))
            {
                return;
            }

            _recentContacts.Add(contact);
            FilterContacts(String.Empty, ContactTypeFilter);
        }

        public ContactTypeFilter ContactTypeFilter
        {
            get
            {
                return _contactTypeFilter;
            }
            set
            {
                Set(ref _contactTypeFilter, value);
                FilterContacts(Query, value);
            }
        }

        public RelayCommand CheckInOutOpenCommand
        {
            get
            {
                return _checkInOutOpenCommand ?? (_checkInOutOpenCommand = new RelayCommand(
              async () =>
              {
                  if (_presentationDataService.IsPresentationStarted())
                  {
                      await ProcessCheckOut();
                  }
                  else
                  {
                      ResetState();
                  }
              }));
            }
        }

        public string Query
        {
            get { return _query; }
            set
            {
                Set(ref _query, value);
                FilterContacts(value, ContactTypeFilter);
            }
        }

        private void FilterContacts(string query, ContactTypeFilter type)
        {
            if (_allContacts == null || _recentContacts == null)
            {
                return;
            }

            var listToFilter = type == ContactTypeFilter.All
                                    ? _allContacts
                                    : _recentContacts;

            Contacts = listToFilter.Where(c => IsMatch(c, query)).ToList();
        }

        private bool IsMatch(ContactViewModel c, string query)
        {
            if (String.IsNullOrEmpty(query))
            {
                return true;
            }
            var stringsToSearch = new[] { c.Contact.FirstName, c.Contact.LastName, c.Contact.AccountName, c.Contact.Email };

            return stringsToSearch
                        .Where(s => String.IsNullOrEmpty(s) == false)
                        .Any(s => s.IndexOf(query, 0, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public bool IsChooseAtCheckOutVisible
        {
            get
            {
                return  _isChooseAtCheckOutVisible;
            }
            set { Set(ref _isChooseAtCheckOutVisible, value); }
        }

        public bool IsContactsListVisible
        {
            get { return _isContactsListVisible; }
            set { Set(ref _isContactsListVisible, value); }
        }

        public RelayCommand FinishCheckOutCommand
        {
            get
            {
                return _finishCheckOutCommand ?? (_finishCheckOutCommand = new RelayCommand(
                 async () =>
                 {
                     await _presentationDataService.FinishPresentation(Notes, this.ContentsReviews.Select(vm => vm.ReviewInfo).ToList());
                     DispatcherHelper.CheckBeginInvokeOnUI(() =>
                     {
                         Messenger.Default.Send(new CheckInOutChangedMessage());
                         IsFlyoutOpen = false;
                         IsChooseAtCheckOutFlow = false;
                         ResetState();
                     });
                 },
                 () =>
                 {
                     return AllMaterialAreReviewed();
                 }
                 ));
            }
        }

        private bool AllMaterialAreReviewed()
        {
            if (SfdcConfig.RequireContentReviewRatingsAtCheckout == false)
            {
                return true;
            }

            if (ContentsReviews == null)
            {
                return false;
            }

            return ContentsReviews.All(c => c.Rating != 0);
        }

        public string Notes
        {
            get { return _notes; }
            set { Set(ref _notes, value); }
        }

        public ObservableCollection<ContentReviewViewModel> ContentsReviews
        {
            get { return _contentsReviews; }
            set { Set(ref _contentsReviews, value); }
        }

        public bool IsChooseAtCheckOutFlow
        {
            get
            {
                return _isChooseAtCheckOutFlow;
            }
            set
            {
                Set(ref _isChooseAtCheckOutFlow, value);
            }
        }

        public RelayCommand GoToChooseContactCommand
        {
            get
            {
                return _goToChooseContactCommand ?? (_goToChooseContactCommand = new RelayCommand(
                    () =>
                    {
                        IsContactsListVisible = true;
                    },
                    () =>
                    {
                        return AllMaterialAreReviewed();
                    }));
            }
        }

        public RelayCommand GoToContentReviewCommand
        {
            get
            {
                return _goToContentReviewCommand ?? (_goToContentReviewCommand = new RelayCommand(
                    () =>
                    {
                        IsContactsListVisible = false;
                    }));
            }
        }
    }

    public enum ContactTypeFilter
    {
        All,
        Recent
    }
}