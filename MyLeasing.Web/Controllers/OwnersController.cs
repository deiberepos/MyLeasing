using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyLeasing.Web.Data;
using MyLeasing.Web.Data.Entities;
using MyLeasing.Web.Helpers;
using MyLeasing.Web.Models;

namespace MyLeasing.Web.Controllers
{
    [Authorize(Roles = "Manager")]
    public class OwnersController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IUserHelper _userHelper;
        private readonly ICombosHelper _combosHelper;
        private readonly IConverterHelper _converterHeper;
        private readonly IImageHelper _imageHelper;

        public OwnersController(
            DataContext dataContext,
            IUserHelper userHelper,
            ICombosHelper combosHelper,
            IConverterHelper converterHeper,
            IImageHelper imageHelper)
        {
            _dataContext = dataContext;
            _userHelper = userHelper;
            _combosHelper = combosHelper;
            _converterHeper = converterHeper;
            _imageHelper = imageHelper;
        }

        // GET: Owners
        public IActionResult Index()
        {
            return View(_dataContext.Owners
                .Include(z => z.User)
                .Include(f => f.Properties)
                .Include(o => o.Contracts));
        }

        // GET: Owners/Details/5
        public async Task<IActionResult> Details(int? id)//el ? quiere decir que puede llegar nulo
        {
            if (id == null)
            {
                return NotFound();
            }

            Owner owner = await _dataContext.Owners
                .Include(o => o.User)
                .Include(o => o.Properties)
                .ThenInclude(p => p.PropertyImages)
                .Include(o => o.Contracts)
                .ThenInclude(c => c.Lessee)
                .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (owner == null)
            {
                return NotFound();
            }

            return View(owner);
        }

        // GET: Owners/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Owners/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                //1. Creamos el metodo para crear un usuario
                //Pasando los datos que estan el modelo, para luego crear el propietario
                User user = await createUserAsync(model);
                //8. Validamos si lo pudo crear o no
                if (user != null)
                {
                    //10. Si llegó bien el usuario debemos matricularlo a la coleccion de propietarios (Owners)
                    Owner owner = new Owner
                    {
                        Properties = new List<Property>(),
                        Contracts = new List<Contract>(),
                        User = user,
                    };
                    _dataContext.Owners.Add(owner);//11. Guardarlo en base de datos
                    await _dataContext.SaveChangesAsync();//12. Confirmacion de guardado
                    return RedirectToAction(nameof(Index));//13. Redireccionar al index

                }
                //9.Mensaje de error si no pudo crear el usuario
                ModelState.AddModelError(string.Empty, "Ya existe un usuario con este email");
            }
            return View(model);
        }

        private async Task<User> createUserAsync(AddUserViewModel model)//Debe retornar el usuario
        {
            //2. Creamos el objeto user con los atributos capturados del modelo
            User user = new User
            {
                Address = model.Address,
                Document = model.Document,
                Email = model.Username,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                UserName = model.Username
            };
            //3. Creamos el usuario usando el userHelper y el user que acabamos de crear
            Microsoft.AspNetCore.Identity.IdentityResult result = await _userHelper.AddUserAsync(user, model.Password);
            if (result.Succeeded)//4. Si lo pudo crear lo traemos de nuevo en nuestra variable user
            {
                user = await _userHelper.GetUserByEmailAsync(model.Username);
                //5. Ahora le agregamos el rol a este usuario
                await _userHelper.AddUserToRoleAsync(user, "Owner");
                //6. retornamos el usuario
                return user;
            }
            //7. si falla retornamos null
            return null;
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Owner owner = await _dataContext.Owners
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id.Value);
            if (owner == null)
            {
                return NotFound();
            }

            EditUserViewModel model = new EditUserViewModel
            {
                Address = owner.User.Address,
                Document = owner.User.Document,
                FirstName = owner.User.FirstName,
                Id = owner.Id,
                LastName = owner.User.LastName,
                PhoneNumber = owner.User.PhoneNumber
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                Owner owner = await _dataContext.Owners
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == model.Id);

                owner.User.Document = model.Document;
                owner.User.FirstName = model.FirstName;
                owner.User.LastName = model.LastName;
                owner.User.Address = model.Address;
                owner.User.PhoneNumber = model.PhoneNumber;

                await _userHelper.UpdateUserAsync(owner.User);
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }


        // GET: Owners/Delete/5
        //Como usamos javascript no necesitamos una acción Post
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Owner owner = await _dataContext.Owners
                .Include(o => o.User)
                .Include(o => o.Properties)//Para validar que no se pueda borrar si tiene propiedades
                .FirstOrDefaultAsync(m => m.Id == id);
            if (owner == null)
            {
                return NotFound();
            }
            if (owner.Properties.Count != 0)
            {
                ModelState.AddModelError(string.Empty, "Este propietario no se puede borrar porque tiene propiedades creadas");
                return RedirectToAction(nameof(Index));
            }

            //Borramos el propietario
            _dataContext.Owners.Remove(owner);
            await _dataContext.SaveChangesAsync();
            //Borramos el usuario
            await _userHelper.DeleteUserAsync(owner.User.Email);
            return RedirectToAction(nameof(Index));
        }


        private bool OwnerExists(int id)
        {
            return _dataContext.Owners.Any(e => e.Id == id);
        }

        public async Task<IActionResult> AddProperty(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Owner owner = await _dataContext.Owners.FindAsync(id);
            if (owner == null)
            {
                return NotFound();
            }
            PropertyViewModel model = new PropertyViewModel
            {
                OwnerId = owner.Id,
                PropertyTypes = _combosHelper.GetComboPropertyTypes()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddProperty(PropertyViewModel model)
        {
            if (ModelState.IsValid)
            {
                Property property = await _converterHeper.ToPropertyAsync(model, true);
                _dataContext.Properties.Add(property);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"Details/{model.OwnerId}");
            }

            //S el modelo no es valido llena nuevamente el combde tipos de propiedades
            model.PropertyTypes = _combosHelper.GetComboPropertyTypes();
            return View(model);
        }

        public async Task<IActionResult> EditProperty(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Property property = await _dataContext.Properties
                .Include(p => p.Owner)
                .Include(p => p.PropertyType)
                .FirstOrDefaultAsync(p => p.Id == id.Value);
            if (property == null)
            {
                return NotFound();
            }

            PropertyViewModel model = _converterHeper.ToPropertyViewModel(property);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProperty(PropertyViewModel model)
        {
            if (ModelState.IsValid)
            {
                Property property = await _converterHeper.ToPropertyAsync(model, false);
                _dataContext.Properties.Update(property);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"Details/{model.OwnerId}");
            }
            return View(model);
        }

        public async Task<IActionResult> DetailsProperty(int? id)
        {
            if (id == null)//Validamos el id de la propiedad
            {
                return NotFound();
            }

            Property property = await _dataContext.Properties//Consulta relacionada que trae muchos valores
                .Include(o => o.Owner) //de las relaciones, gracias a Linq son inner joins
                .ThenInclude(o => o.User)
                .Include(o => o.Contracts)
                .ThenInclude(c => c.Lessee)
                .ThenInclude(l => l.User)
                .Include(o => o.PropertyType)
                .Include(p => p.PropertyImages)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (property == null)
            {
                return NotFound();
            }
            //Le enviamos el objeto property a la vista (ni tabla ni relación, es un objeto)
            return View(property);
        }

        public async Task<IActionResult> AddImage(int? id)
        {
            //Validamos el id de la propiedad
            if (id == null)
            {
                return NotFound();
            }

            //Buscamos la propiedad
            Property property = await _dataContext.Properties.FindAsync(id.Value);
            if (property == null)
            {
                return NotFound();
            }
            //Creamos el modelo para enviar a la vista
            PropertyImageViewModel model = new PropertyImageViewModel
            {
                Id = property.Id
            };

            return View(model);
            //En la vista el usuario selecciona la imagen y vuelve al post
        }

        [HttpPost]
        public async Task<IActionResult> AddImage(PropertyImageViewModel model)
        {
            //Validamos si el modelo es válido
            if (ModelState.IsValid)
            {
                //creamos la variable path asumiendo que adiciono la imagen
                string path = string.Empty;

                //Imagefile=Iformfile donde se captura la imagen
                //Si hay una imagen
                if (model.ImageFile != null)
                {
                    //Llamamos al método que creamos en la interface para subir la imagen
                    //nos devuelve la ruta de como se va ha guradar en la bd
                    path = await _imageHelper.UploadImageAsync(model.ImageFile);
                }

                //Creamos el objeto PropertyImage 
                PropertyImage propertyImage = new PropertyImage
                {
                    ImageUrl = path,//Es la ruta que nos devovlio del método en la interface
                    //buscamos el objeto con el id, ya que desde el get no lo podemos enviar
                    Property = await _dataContext.Properties.FindAsync(model.Id)
                };

                //Finalmente guardamos en la coleccion de imagenes la propertyImage
                //retornamos al detailsProperty con el id de la propiedad
                _dataContext.PropertyImages.Add(propertyImage);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsProperty)}/{model.Id}");
            }

            return View(model);
        }

        public async Task<IActionResult> AddContract(int? id)
        {
            if (id == null)//Valida si el id de la propiedad no es nulo
            {
                return NotFound();
            }

            //mediante una consulta relacionada busca el objeto owner
            // usando el id de la propiedad
            Property property = await _dataContext.Properties
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == id.Value);
            if (property == null)
            {
                return NotFound();
            }

            ContractViewModel model = new ContractViewModel
            {
                //gracias a la consulta relacionada ya podemos completar el modelo
                //sacando los id que necesitamos enviarle
                OwnerId = property.Owner.Id,
                PropertyId = property.Id,
                //Es la funcion que se debe armar en el combosHelper para tener 
                //el combo box con los arrendatarios
                Lessees = _combosHelper.GetComboLessees(),
                Price = property.Price,//dejamos por defecto el precio inicial
                StartDate = DateTime.Today,//la fecha inicial será la fecha del sistema
                EndDate = DateTime.Today.AddYears(1)//la fecha final le sumamos 1 año
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddContract(ContractViewModel model)
        {
            if (ModelState.IsValid)
            {
                Contract contract = await _converterHeper.ToContractAsync(model, true);
                _dataContext.Contracts.Add(contract);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsProperty)}/{model.PropertyId}");
            }
            //Se debe llenar el modelo nuevamente cuando el modelo no es valido
            model.Lessees = _combosHelper.GetComboLessees();
            return View(model);
        }

        public async Task<IActionResult> EditContract(int? id)
        {
            if (id == null)//Se valida el id de los contratos
            {
                return NotFound();
            }

            //Hacemos la consulta relacionada y le enviamos los objetos que necesitamos para sacar los id
            Contract contract = await _dataContext.Contracts
                .Include(p => p.Owner)
                .Include(p => p.Lessee)
                .Include(p => p.Property)
                .FirstOrDefaultAsync(p => p.Id == id.Value);
            if (contract == null)
            {
                return NotFound();
            }

            // return View(_converterHeper.ToContractViewModel(contract));
            ContractViewModel model = _converterHeper.ToContractViewModel(contract);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditContract(ContractViewModel view)
        {
            if (ModelState.IsValid)
            {
                Contract contract = await _converterHeper.ToContractAsync(view, false);
                _dataContext.Contracts.Update(contract);
                await _dataContext.SaveChangesAsync();
                return RedirectToAction($"{nameof(DetailsProperty)}/{view.PropertyId}");
            }

            return View(view);
        }

        public async Task<IActionResult> DeleteImage(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            PropertyImage propertyImage = await _dataContext.PropertyImages
                .Include(pi => pi.Property)
                .FirstOrDefaultAsync(pi => pi.Id == id.Value);
            if (propertyImage == null)
            {
                return NotFound();
            }

            _dataContext.PropertyImages.Remove(propertyImage);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction($"{nameof(DetailsProperty)}/{propertyImage.Property.Id}");
        }

        public async Task<IActionResult> DeleteContract(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Contract contract = await _dataContext.Contracts
                .Include(c => c.Property)
                .FirstOrDefaultAsync(c => c.Id == id.Value);
            if (contract == null)
            {
                return NotFound();
            }

            _dataContext.Contracts.Remove(contract);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction($"{nameof(DetailsProperty)}/{contract.Property.Id}");
        }

        public async Task<IActionResult> DeleteProperty(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Property property = await _dataContext.Properties
                .Include(p => p.Owner)
                .Include(p => p.PropertyImages)
                .Include(p => p.Contracts)
                .FirstOrDefaultAsync(pi => pi.Id == id.Value);
            if (property == null)
            {
                return NotFound();
            }

            if (property.Contracts.Count != 0)
            {
                ModelState.AddModelError(string.Empty, "La propiedad no puede borrarse porque tiene contratos");
                return RedirectToAction($"{nameof(Details)}/{property.Owner.Id}");
            }

            _dataContext.PropertyImages.RemoveRange(property.PropertyImages);
            _dataContext.Properties.Remove(property);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction($"{nameof(Details)}/{property.Owner.Id}");
        }

        public async Task<IActionResult> DetailsContract(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _dataContext.Contracts
                .Include(c => c.Owner)
                .ThenInclude(o => o.User)
                .Include(c => c.Lessee)
                .ThenInclude(o => o.User)
                .Include(c => c.Property)
                .ThenInclude(p => p.PropertyType)
                .FirstOrDefaultAsync(pi => pi.Id == id.Value);
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }



    }
}
