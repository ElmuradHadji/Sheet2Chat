namespace ContactAdder.Models
{
    public class ContactItemViewModel
    {
        public bool IsSelected { get; set; } = true;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }


}
