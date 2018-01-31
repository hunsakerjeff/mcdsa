namespace DSA.Model.Dto
{
    public class ContactDTO
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public AccountDTO Account { get; set; }

        public string AccountName
        {
            get
            {
                var res = string.Empty;

                if (this.Account != null)
                    res = Account.Name;

                return res;
            }
            set
            {
                var ac = Account ?? new AccountDTO();
                ac.Name = value;
            }
        }

        public class AccountDTO
        {
            public string Name { get; set; }
        }
    }
}
