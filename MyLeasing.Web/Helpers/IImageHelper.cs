using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyLeasing.Web.Helpers
{
    public interface IImageHelper
    {
        //Le pasamos el atributo de el modelo y devuelve
        //un string de la ruta de como quedo guardada
        Task<string> UploadImageAsync(IFormFile imageFile);

    }
}
