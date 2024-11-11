using Microsoft.AspNetCore.Mvc;
using OrderPayment.Models;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OrderPayment.Controllers
{
    public class AdminController : Controller
    {
        private readonly OrderPaymentDbContext _context;

        public AdminController(OrderPaymentDbContext context)
        {
            _context = context;
        }

        // Admin Paneli Sayfası
        public IActionResult AdminPanel()
        {
            var products = _context.products.ToList(); // Ürünleri veritabanından al
            return View(products); // Veritabanından alınan ürünleri view'a gönder
        }

        // Yeni Ürün Ekle Sayfası
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile image)
        {
            if (ModelState.IsValid)
            {
                // Resmi base64 formatına çevir
                if (image != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await image.CopyToAsync(memoryStream);
                        product.Image = Convert.ToBase64String(memoryStream.ToArray());
                    }
                }

                // Ürünü veritabanına kaydet
                _context.products.Add(product);
                await _context.SaveChangesAsync();

                // Ürün ekleme sonrası AdminPanel'e yönlendir
                return RedirectToAction("AdminPanel");
            }

            return View(product);
        }


    }
}
