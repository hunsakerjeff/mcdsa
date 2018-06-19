using DSA.Model.Enums;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DSA.Shell.Commands
{
    public class RelatedItemSelectedCommand : ICommand
    {
        // CTOR
        public RelatedItemSelectedCommand(){ }

        // Event Handlers
        public event EventHandler CanExecuteChanged;

        // Implementation - Public Methods
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var mediaContainer = parameter as IHaveMedia;
            if (mediaContainer == null)
            {
                return;
            }

            // Send mediaLink to MediaContentViewModel
            Messenger.Default.Send(mediaContainer.Media);
        }
    }
}
