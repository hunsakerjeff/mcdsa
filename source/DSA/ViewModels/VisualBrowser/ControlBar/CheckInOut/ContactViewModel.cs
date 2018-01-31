using System;
using System.Linq;
using DSA.Model.Dto;

namespace DSA.Shell.ViewModels.VisualBrowser.ControlBar.CheckInOut
{
    public class ContactViewModel
    {
        public ContactViewModel(ContactDTO contact)
        {
            Contact = contact;
        }

        public string DisplayName
        {
            get
            {
                var mail = String.IsNullOrEmpty(Contact.Email) == false
                               ? Contact.Email
                               : "no email";
                var infostrings = new[] { Contact.FirstName, Contact.LastName, $"<{ mail }>" };
               
                return String.Join(" ", infostrings.Where(s => String.IsNullOrEmpty(s) == false));
            }
        }

        public string Description
        {
            get
            {
                return Contact.AccountName;
            }
        }

        public ContactDTO Contact { get; private set; }
    }
}
