using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DocMd.Site.Models.SetupViewModels
{
    public class HookViewModel
    {
        [Required]
        [Url]
        [Display(Name = "Repository Url")]
        public string RepositoryUrl { get; set; }

        [Required]
        [Display(Name = "Personal Access Token")]
        public string PersonalAccessToken { get; set; }

        [Required]
        public string Secret { get; set; }
    }
}
