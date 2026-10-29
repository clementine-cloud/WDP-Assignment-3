using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspNetCoreMvcWebSite.Models
{
    // The model class supplied in the assessment brief. It is used by Entity Framework
    // to create the AIImage table and by the AIImagesController / views.
    public class AIImage
    {
        public AIImage()
        {
            Prompt = "";
            ImageGenerator = "";
            Filename = "";
        }

        [Required]
        public int Id { get; set; }

        [Required]
        public string Prompt { get; set; }

        [Required]
        [Display(Name = "Image Generator")]
        public string ImageGenerator { get; set; }

        [Display(Name = "Upload Date")]
        public DateTime UploadDate { get; set; }

        public string Filename { get; set; }

        public int Like { get; set; }

        // CHANGE FROM THE BRIEF: [NotMapped] added. This flag is worked out on every request
        // (has this logged-in user already liked this block?) so it must NOT become a database column.
        [NotMapped]
        public bool canIncreaseLike { get; set; }

        // EXTRA HELPER: the URL of the picture. If the user did not upload a file, the default
        // placeholder is shown. It has no setter, so EF Core ignores it; [NotMapped] makes that explicit.
        [NotMapped]
        public string ImageUrl =>
            string.IsNullOrWhiteSpace(Filename) ? "~/images/default.jpg" : "~/uploads/" + Filename;
    }
}
