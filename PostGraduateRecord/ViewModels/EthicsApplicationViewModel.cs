
using System.ComponentModel.DataAnnotations;

namespace PostGraduateRecord.ViewModels
{
    public class EthicsApplicationViewModel
    {
        [Required(ErrorMessage = "Please select Yes or No.")]
        [Display(Name = "Did you submit an ethics application?")]
        public bool? EthicsSubmitted { get; set; }

        [Display(Name = "Ethics Number")]
        public string EthicsNumber { get; set; }
    }
}

