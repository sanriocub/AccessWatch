using System.ComponentModel.DataAnnotations;

namespace AccessWatch.ViewModels
{
    public class SubmitReportViewModel
    {
        [Required, StringLength(1000)]
        public string Description { get; set; }

        [Required, StringLength(300)]
        public string Location { get; set; }

        public IFormFile? Image { get; set; }
    }
}