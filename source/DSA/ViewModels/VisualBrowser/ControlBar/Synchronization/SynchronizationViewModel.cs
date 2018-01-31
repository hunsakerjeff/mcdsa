using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model;
using DSA.Model.Enums;
using DSA.Model.Messages;
using DSA.Sfdc.Sync;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using GalaSoft.MvvmLight.Views;
using Salesforce.SDK.Adaptation;
using Salesforce.SDK.Utilities;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar.Synchronization
{
    public class SynchronizationViewModel : ViewModelBase
    {
        private readonly ISettingsDataService _settingsDataService;
        private readonly ISyncLogService _syncLogService;
        private readonly IDialogService _dialogService;

        private RelayCommand _cancelSynchronizationCommand;
        private CancellationTokenSource _tokenSource;
        private bool _isPopupOpen;
        private string _header;
        private string _closeCancelButtonContent;
        private string _resultsMessage;
        private Task _currentTask;
        private bool _isDownloading;
        private decimal _currentDownloadedBytes = 0m;
        private decimal _totalToDownloadBytes;
        private double _percentageDownloaded;


        public SynchronizationViewModel(
             IDialogService dialogService,
             ISettingsDataService settingsDataService,
             ISyncLogService syncLogService)
        {
            _dialogService = dialogService;
            _settingsDataService = settingsDataService;
            _syncLogService = syncLogService;
            QueueingStep = new SynchronizationStepViewModel("Queueing");
            ConfigurationStep = new SynchronizationStepViewModel("Configuration");
            ContentStep = new SynchronizationStepViewModel("Content");
            FinishingStep = new SynchronizationStepViewModel("Finishing");
            ResetSteps();
            ResetProgress();
            AttachMessages();
        }

        private void AttachMessages()
        {
            Messenger.Default.Register<SynchronizationClosePopup>(this, (m) =>
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                IsPopupOpen = false;
            }));

            Messenger.Default.Register<SynchronizationProgressMessage>(this, (m) =>
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (_currentDownloadedBytes < _totalToDownloadBytes)
                {
                    _currentDownloadedBytes += m.Current;
                }
                if (m.Current == 0 && m.Total > 0)
                {
                    IsDownloading = true;
                    TotalToDownloadBytes = m.Total;
                }
                if (TotalToDownloadBytes > 0m)
                {
                    PercentageDownloaded = (double)((_currentDownloadedBytes / TotalToDownloadBytes) * 100m);
                } else
                {
                    PercentageDownloaded = 0d;
                }
                Debug.WriteLine("***Downloading " + _currentDownloadedBytes + " of " + _totalToDownloadBytes);
            }));

            Messenger.Default.Register<SynchronizationCancelMessage>(this, (m) =>
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                if (_settingsDataService.InSynchronizationInProgress)
                {
                    if (!CancelTokenSource())
                    {
                        return;
                    }
                    if (m.Exception != null)
                    {
                        var aggregateException = m.Exception as AggregateException;
                        if (aggregateException == null)
                        {
                            aggregateException = new AggregateException(new List<Exception> {m.Exception});
                        }
                        _syncLogService.SynchronizationFailed(aggregateException);
                        ShowErrorMessageBoxIfNeeded(aggregateException);
                    }
                    FinishSync(false);
                }
            }));
        }

        public bool IsPopupOpen
        {
            get{ return _isPopupOpen; }
            set
            {
                Set(ref _isPopupOpen, value);
                if (value == false)
                {
                    return;
                }

                ResetSteps();
                ResetProgress();
                Task.Factory.StartNew(StartSynchronization);
            }
        }

        public async Task StartSynchronization()
        {
            _settingsDataService.InSynchronizationInProgress = true;

            _tokenSource = new CancellationTokenSource();
            _currentTask = await Task.Factory.StartNew(async () =>
            {
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    _tokenSource.Token.ThrowIfCancellationRequested();
                }
                
                _syncLogService.StartSynchronization(Mode);

                var syncTask = Mode == SynchronizationMode.Full || Mode == SynchronizationMode.Initial
                    ? ProcessFullSynchronizationSteps(_tokenSource.Token)
                    : ProcessDeltaSynchronizationSteps(_tokenSource.Token);
                var isSucess = await syncTask.ContinueWith(
                        (task) =>
                        {
                            var result = task.Exception?.InnerExceptions == null && task.Result;
                            if (result)
                            {
                                _syncLogService.SynchronizationCompleted();
                            }
                            else
                            {
                                if (task.Exception?.InnerException?.Message == "Token Expired")//hide popup on token expired error
                                {
                                    Messenger.Default.Send(new SynchronizationClosePopup());
                                }
                                _syncLogService.SynchronizationFailed(task.Exception);
                                ShowErrorMessageBoxIfNeeded(task.Exception);
                            }
                            return result;  
                        },
                        _tokenSource.Token);

                FinishSync(isSucess);

            }, _tokenSource.Token);
        }

        private void FinishSync(bool isSucess)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ResultsMessage = isSucess
                    ? "Synchronization completed successfully"
                    : "Synchronization failed";

                CloseCancelButtonContent = "Close";
            });

            if (isSucess)
            {
                Messenger.Default.Send(new SynchronizationFinished() { Mode = Mode });
                Messenger.Default.Unregister(this);
            }

            _settingsDataService.InSynchronizationInProgress = false;
            DisposeTokenSource();
        }

        private bool CancelTokenSource()
        {
            if (_tokenSource != null && !_tokenSource.IsCancellationRequested)
            {
                try
                {
                    _tokenSource.Cancel();
                    return true;
                }
                catch (Exception)
                {
                    // TODO log exception
                }
            }
            return false;
        }

        private void DisposeTokenSource()
        {
            if (_tokenSource != null)
            {
                try
                {
                    _tokenSource.Dispose();
                }
                catch (ObjectDisposedException e)
                {
                    PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                    Debug.WriteLine("SynchronizationViewModel DisposeTokenSource ObjectDisposedException");
                }
            }
        }

        private void ShowErrorMessageBoxIfNeeded(AggregateException exception)
        {
            var messagesToDisplay = exception.InnerExceptions.Where(e => e != null).Select(e => e.Message).ToList();
            if(messagesToDisplay.Any() == false)
            {
                return;
            }

            var message = string.Join(Environment.NewLine, messagesToDisplay);
            if(exception.InnerExceptions.Any(e => e is ErrorResponseException))
            {
                var messageHeader = $"You do not have correct permission set for DSA app, report the problem to email address {SfdcConfig.SynchronizationFailedSupportEmail}";
                message = messageHeader + Environment.NewLine + Environment.NewLine + message;
            }

            if (!ObjectSyncDispatcher.HasInternetConnection())
            {
                message = ObjectSyncDispatcher.NoInternetConnectionMessage;
            }
            
            DispatcherHelper.CheckBeginInvokeOnUI(() => { _dialogService.ShowMessage(message, "Synchronization failed"); });
        }

        private async Task<bool> ProcessDeltaSynchronizationSteps(CancellationToken token)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                QueueingStep.State = StepState.Progress;
                Header = "Step 1 - Queueing";
            });
            var currentUser = await ObjectSyncDispatcher.Instance.QueueingDeltaSyncAsync((text) => Debug.WriteLine(text), token);

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                QueueingStep.State = StepState.Done;
                ConfigurationStep.State = StepState.Progress;
                Header = "Step 2 - Configuration";
            });

            await ObjectSyncDispatcher.Instance.ConfigurationDeltaSyncAsync((text) => Debug.WriteLine(text), currentUser, token);

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ConfigurationStep.State = StepState.Done;
                ContentStep.State = StepState.Progress;
                Header = "Step 3 - Content";
            });

            await ObjectSyncDispatcher.Instance.ContentDeltaSyncAsync((text) => Debug.WriteLine(text), currentUser, token);

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ContentStep.State = StepState.Done;
                FinishingStep.State = StepState.Progress;
                Header = "Step 4 - Finishing";
            });

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                FinishingStep.State = StepState.Done;
                Header = "Synchronization finished";
            });

            return true;
        }

        private async Task<bool> ProcessFullSynchronizationSteps(CancellationToken token)
        {
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                QueueingStep.State = StepState.Progress;
                Header = "Step 1 - Queueing";
            });
            var currentUser = await ObjectSyncDispatcher.Instance.QueueingFullSyncAsync((text) => Debug.WriteLine(text), token);

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                QueueingStep.State = StepState.Done;
                ConfigurationStep.State = StepState.Progress;
                Header = "Step 2 - Configuration";
            });

            await ObjectSyncDispatcher.Instance.ConfigurationFullSyncAsync((text) => Debug.WriteLine(text), currentUser, token);

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ConfigurationStep.State = StepState.Done;
                ContentStep.State = StepState.Progress;
                Header = "Step 3 - Content";
            });

            await ObjectSyncDispatcher.Instance.ContentFullSyncAsync((text) => Debug.WriteLine(text), currentUser, token);

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                ContentStep.State = StepState.Done;
                FinishingStep.State = StepState.Progress;
                Header = "Step 4 - Finishing";
            });

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                FinishingStep.State = StepState.Done;
                Header = "Synchronization finished";
            });

            return true;
        }

        private void ResetSteps()
        {
            Header = "Preparing to Synchronize";
            CloseCancelButtonContent = "Cancel";
            ResultsMessage = string.Empty;
            QueueingStep.State = StepState.Waiting;
            ConfigurationStep.State = StepState.Waiting;
            ContentStep.State = StepState.Waiting;
            FinishingStep.State = StepState.Waiting;
        }

        private void ResetProgress()
        {
            IsDownloading = false;
            _currentDownloadedBytes = 0m;
            TotalToDownloadBytes = 0m;
            PercentageDownloaded = 0d;
        }

        public string Header
        {
            get { return _header; }
            set { Set(ref _header, value); }
        }

        public SynchronizationStepViewModel QueueingStep
        {
            get;
            set;
        }

        public SynchronizationStepViewModel ConfigurationStep
        {
            get; set;
        }

        public SynchronizationStepViewModel ContentStep
        {
            get; set;
        }

        public SynchronizationStepViewModel FinishingStep
        {
            get; set;
        }

        public string ResultsMessage
        {
            get { return _resultsMessage; }
            set { Set(ref _resultsMessage, value); }
        }

        public bool IsDownloading
        {
            get { return _isDownloading; }
            set { Set(ref _isDownloading, value); }
        }

        public decimal TotalToDownloadBytes
        {
            get { return _totalToDownloadBytes; }
            set { Set(ref _totalToDownloadBytes, value); }
        }

        public double PercentageDownloaded
        {
            get { return _percentageDownloaded; }
            set { Set(ref _percentageDownloaded, value); }
        }

        public string CloseCancelButtonContent
        {
            get{ return _closeCancelButtonContent; }
            set { Set(ref _closeCancelButtonContent, value); }
        }
        
        public RelayCommand CancelSynchronizationCommand
        {
            get
            {
                return _cancelSynchronizationCommand ?? (_cancelSynchronizationCommand = new RelayCommand(
                    () =>
                    {
                        CancelTokenSource();
                        _currentTask.ContinueWith((task) =>
                        {
                            if (!task.IsCanceled && task.Exception?.InnerException != null)
                            {
                                throw task.Exception.InnerException;
                            }
                            if (task.IsCanceled)
                                _syncLogService.SynchronizationCanceled();
                        }).Wait();
                        ResetSteps();
                        IsPopupOpen = false;
                        _settingsDataService.InSynchronizationInProgress = false;
                        Messenger.Default.Send(new SynchronizationCompleteMessage());
                        DisposeTokenSource();
                    }));
            }
        }

        internal SynchronizationMode Mode { get; set; }
    }
}