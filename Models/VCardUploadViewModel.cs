using System.ComponentModel.DataAnnotations;

namespace ContactAdder.Models
{
    public class VCardUploadViewModel
    {
        [Required(ErrorMessage = "Zəhmət olmasa Excel faylını seçin.")]
        public IFormFile ExcelFile { get; set; } = null!;
        public List<ContactItemViewModel> Contacts { get; set; } = new();
        public string Suffix { get; set; } = "Ww EVS";
    }
}
