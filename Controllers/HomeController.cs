using ContactAdder.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using ClosedXML.Excel;


namespace ContactAdder.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View(new VCardUploadViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = 20000)]
        public async Task<IActionResult> PreviewContacts(VCardUploadViewModel model)
        {
            if (model.ExcelFile == null || model.ExcelFile.Length == 0)
            {
                ModelState.AddModelError("", "Zəhmət olmasa bir Excel faylı (.xlsx) seçin.");
                return View("Index", model);
            }

            var contactList = new List<ContactItemViewModel>();

            using (var memoryStream = new MemoryStream())
            {
                await model.ExcelFile.CopyToAsync(memoryStream);
                using (var workbook = new XLWorkbook(memoryStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var range = worksheet.RangeUsed();

                    if (range == null)
                    {
                        ModelState.AddModelError("", "Excel faylında məlumat tapılmadı.");
                        return View("Index", model);
                    }

                    // 1-ci sətir başlıqdırsa ötürülür
                    var rows = range.RowsUsed().Skip(1);

                    foreach (var row in rows)
                    {
                        string ad = row.Cell(1).GetFormattedString().Trim();    // Sütun A: Ad
                        string soyad = row.Cell(2).GetFormattedString().Trim(); // Sütun B: Soyad
                        string rawPhone = row.Cell(3).GetFormattedString().Trim(); // Sütun C: Nömrə

                        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(rawPhone))
                            continue;

                        string cleanedPhone = CleanPhoneNumber(rawPhone);
                        if (string.IsNullOrEmpty(cleanedPhone))
                            continue;

                        string suffix = model.Suffix?.Trim() ?? string.Empty;
                        string baseName = string.IsNullOrWhiteSpace(soyad) ? ad : $"{ad} {soyad}";
                        string tamAd = string.IsNullOrEmpty(suffix) ? baseName : $"{baseName} {suffix}";

                        contactList.Add(new ContactItemViewModel
                        {
                            IsSelected = true,
                            FirstName = ad,
                            LastName = soyad,
                            FullName = tamAd,
                            PhoneNumber = cleanedPhone
                        });
                    }
                }
            }

            if (contactList.Count == 0)
            {
                ModelState.AddModelError("", "Excel faylından heç bir kontakt oxuna bilmədi. Sütunların ardıcıllığını yoxlayın: A=Ad, B=Soyad, C=Nömrə.");
                return View("Index", model);
            }

            model.Contacts = contactList;
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = 20000)]
        public IActionResult ExportSelected(VCardUploadViewModel model)
        {
            var selectedContacts = model.Contacts.Where(c => c.IsSelected).ToList();

            if (selectedContacts.Count == 0)
            {
                ModelState.AddModelError("", "Zəhmət olmasa ən azı bir kontakt seçin.");
                return View("Index", model);
            }

            string suffix = model.Suffix?.Trim() ?? string.Empty;
            var sb = new StringBuilder();

            foreach (var contact in selectedContacts)
            {
                sb.AppendLine("BEGIN:VCARD");
                sb.AppendLine("VERSION:3.0");
                sb.AppendLine($"FN;CHARSET=UTF-8:{contact.FullName}");
                sb.AppendLine($"N;CHARSET=UTF-8:{contact.LastName};{contact.FirstName};;;{suffix}");
                sb.AppendLine($"TEL;TYPE=CELL:{contact.PhoneNumber}");
                sb.AppendLine("END:VCARD");
            }

            byte[] fileBytes = Encoding.UTF8.GetBytes(sb.ToString());
            string fileName = BuildFileName(model.Suffix);
            return File(fileBytes, "text/vcard; charset=utf-8", fileName);
        }

        private static string BuildFileName(string? suffix)
        {
            suffix = suffix?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(suffix))
                return "kontaktlar.vcf";

            var invalidChars = Path.GetInvalidFileNameChars();
            string safeSuffix = new string(suffix.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray())
                .Replace(' ', '_');

            return $"kontaktlar_{safeSuffix}.vcf";
        }

        private static string CleanPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return string.Empty;

            var digits = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

            if (string.IsNullOrWhiteSpace(digits) || digits == "+")
                return string.Empty;

            if (digits.StartsWith("0"))
                digits = "+994" + digits[1..];
            else if (digits.StartsWith("994"))
                digits = "+" + digits;
            else if (!digits.StartsWith("+"))
                digits = "+994" + digits;

            if (digits.Count(char.IsDigit) < 9)
                return string.Empty;

            return digits;
        }
    }




}
