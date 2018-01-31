namespace DSA.Model.Messages
{
    public class MobileConfigurationChangedMessage
    {
        public string SelectedConfiguration { private set; get; }

        public MobileConfigurationChangedMessage(string selectedConfiguration)
        {
            SelectedConfiguration = selectedConfiguration;
        }
    }
}
