using System.Collections.Generic;
using System.Threading.Tasks;
using DSA.Model.Dto;

namespace DSA.Data.Interfaces
{
    public interface IContactsService
    {
        Task<List<ContactDTO>> GetAllContacts();

        Task AddRecentContact(ContactDTO contact);

        Task<List<ContactDTO>> GetRecentContacts();
    }
}
