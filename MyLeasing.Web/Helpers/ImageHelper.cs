using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyLeasing.Web.Helpers
{
    public class ImageHelper : IImageHelper
    {
        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            //Genera numeros y letras que no se repitan
            //para que no se sobreescriban las fotos con el nombre
            var guid = Guid.NewGuid().ToString();
            //le pegamos el .jpg
            var file = $"{guid}.jpg";

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),//Es la ubicacion donde corro el proyecto
                //principalmente lo usamos para cuando usemos azure
                "wwwroot\\images\\Properties",//Direccion de donde guardamos la imagen
                file);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);//coge el iformfile, lee el archivo en un string y lo sube
            }

            return $"~/images/Properties/{file}";//retorna el string que sera guardado en la base de datos
        }
    }
}
