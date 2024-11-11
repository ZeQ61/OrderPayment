using Microsoft.AspNetCore.Mvc;

namespace OrderPayment.Controllers
{
    public class AdminController : Controller
    {
        // Admin Paneli Sayfası//
        public IActionResult AdminPanel()
        {
            return View();  // Admin Paneli görünümüne yönlendirir
        }

        // Ürün Ekle Sayfası
        public IActionResult AddProduct()
        {
            return View();  // Ürün Ekle görünümüne yönlendirir
        }

        // Ürün Düzenle Sayfası
        public IActionResult EditProduct()
        {
            return View();  // Ürün Düzenle görünümüne yönlendirir
        }
    }
}
