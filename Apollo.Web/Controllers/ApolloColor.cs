using System.ComponentModel.DataAnnotations;

namespace Apollo.Web.Controllers
{
    public class ApolloColor
    {
        [Required]
        [Range(0, 255)]
        public int R { get; set; }

        [Required]
        [Range(0, 255)]
        public int G { get; set; }

        [Required]
        [Range(0, 255)]
        public int B { get; set; }

        [Required]
        [Range(0, 255)]
        public int Alpha { get; set; }
    }
}