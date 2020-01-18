using System.ComponentModel.DataAnnotations;

namespace MyLeasing.Web.Data.Entities
{
    public class PropertyImage
    {
        public int Id { get; set; }

        [Display(Name = "Image")]
        //[Required(ErrorMessage = "The field {0} is mandatory.")]
        //Con la anterior DataA se revienta la vista de agregar una imagen
        public string ImageUrl { get; set; }

        public Property Property { get; set; }

        // TODO: Change the path when publish
        public string ImageFullPath => string.IsNullOrEmpty(ImageUrl) 
            ? null 
            :$"https://TBD.azurewebsites.net{ImageUrl.Substring(1)}";
    }
}
