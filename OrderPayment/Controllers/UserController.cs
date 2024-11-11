using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrderPayment.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using BCrypt.Net;

using Microsoft.EntityFrameworkCore;
using System.Web.Helpers;

public class UserController : Controller
{
    private readonly SmsService _smsService;
    private readonly OrderPaymentDbContext _context;

    public UserController(SmsService smsService, OrderPaymentDbContext context)
    {
        _smsService = smsService;
        _context = context;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpGet]
    public IActionResult VerifyCode()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(User user)
    {
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return Json(new { success = false, message = "Telefon numarası gereklidir." });
        }

        if (string.IsNullOrWhiteSpace(user.Password) || user.Password.Length < 6)
        {
            return Json(new { success = false, message = "Şifre en az 6 karakter uzunluğunda olmalıdır." });
        }

        // Telefon numarasına göre kullanıcı kontrolü
        if (_context.Users.Any(u => u.PhoneNumber == user.PhoneNumber))
        {
            return Json(new { success = false, message = "Bu telefon numarasıyla zaten kayıtlı bir kullanıcı var." });
        }

        // Şifre hashleme
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);
        user.Password = user.Password; // Düz şifreyi saklamıyoruz

        // Doğrulama kodu oluştur
        var verificationCode = GenerateVerificationCode();

        // SMS mesajı
        var message = $"Merhaba {user.FirstName},\n\n" +
                      "Hesabınızı güvenli bir şekilde oluşturabilmeniz için aşağıdaki doğrulama kodunu kullanabilirsiniz:\n\n" +
                      $"Doğrulama Kodu: {verificationCode}\n\n" +
                      "Kodunuzun geçerlilik süresi sınırlıdır, lütfen en kısa sürede giriniz.\n\n" +
                      "Teşekkürler!";

        // SMS gönderimi
        if (!_smsService.SendSms(user.PhoneNumber, message))
        {
            return Json(new { success = false, message = "SMS gönderilemedi. Lütfen daha sonra tekrar deneyin." });
        }

        // Kullanıcı bilgilerini kaydet
        user.IsActive = false;
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        _context.SaveChanges();

        // Doğrulama kodunu kaydet
        var verification = new VerificationCode
        {
            Code = verificationCode,
            SentAt = DateTime.UtcNow,
            ExpiryInSeconds = 20, // 3 dakika
            UserId = user.Id
        };

        _context.VerificationCodes.Add(verification);
        _context.SaveChanges();

        // Session'a kullanıcıyı kaydet
        HttpContext.Session.SetString("User", JsonConvert.SerializeObject(user, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        }));

        return Json(new { success = true, message = "Kayıt başarılı! Doğrulama kodu gönderildi." });
    }




    [HttpPost]
    public IActionResult VerifyCode(string verificationCode)
    {
        var userJson = HttpContext.Session.GetString("User");
        if (string.IsNullOrEmpty(userJson))
        {
            return Json(new { success = false, message = "Geçersiz kullanıcı verisi!" });
        }

        var user = JsonConvert.DeserializeObject<User>(userJson);
        user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == user.Id);

        var verification = _context.VerificationCodes
            .AsNoTracking()
            .Where(vc => vc.UserId == user.Id && vc.Code == verificationCode)
            .OrderByDescending(vc => vc.SentAt)
            .FirstOrDefault();

        if (verification != null)
        {
            if (verification.IsExpired())
            {
                return Json(new { success = false, message = "Geçersiz veya süresi dolmuş doğrulama kodu!" });
            }

            user.IsActive = true;
            _context.Users.Attach(user);
            _context.SaveChanges();

            return Json(new { success = true, message = "Kullanıcı başarıyla kaydedildi ve aktif edildi." });
        }
        else
        {
            return Json(new { success = false, message = "Geçersiz doğrulama kodu veya kodu daha önce kullanmışsınız." });
        }
    }




    [HttpPost]
    public IActionResult ResendVerificationCode()
    {
        // Kullanıcı verisini session'dan al ve deserialize et
        var userJson = HttpContext.Session.GetString("User");
        if (string.IsNullOrEmpty(userJson))
        {
            return Json(new { success = false, message = "Geçersiz kullanıcı verisi!" });
        }

        var user = JsonConvert.DeserializeObject<User>(userJson);
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı!" });
        }

        // Kullanıcının en son doğrulama kodunu al
        var existingVerification = _context.VerificationCodes
            .Where(vc => vc.UserId == user.Id) // Gelen user nesnesindeki Id'yi kullanıyoruz
            .OrderByDescending(vc => vc.SentAt) // En son gönderilen doğrulama kodu
            .FirstOrDefault();

        // Eğer mevcut doğrulama kodu varsa
        if (existingVerification != null)
        {
            // Süresi dolmuşsa yeni bir doğrulama kodu gönder
            if (existingVerification.IsExpired()) // Kodun süresinin dolup dolmadığını kontrol et
            {
                // Yeni doğrulama kodu oluştur
                var verificationCode = GenerateVerificationCode();

                // SMS gönderimi
                var message = $"Merhaba {user.FirstName} {user.LastName}, yeni doğrulama kodunuz: {verificationCode}";
                bool smsSent = _smsService.SendSms(user.PhoneNumber, message); // Kullanıcıya SMS gönder

                if (smsSent)
                {
                    // Yeni doğrulama kodunu veritabanına kaydet
                    existingVerification.Code = verificationCode;
                    existingVerification.SentAt = DateTime.UtcNow; // Kodun gönderilme zamanı güncelleniyor
                    existingVerification.ExpiryInSeconds = 20; // Kod geçerlilik süresi 60 saniye olarak ayarlandı

                    _context.SaveChanges(); // Güncellenmiş doğrulama kodunu kaydet

                    return Json(new { success = true, message = "Yeni doğrulama kodu gönderildi." });
                }
                else
                {
                    return Json(new { success = false, message = "Yeni doğrulama kodu gönderilemedi." });
                }
            }
            else
            {
                var remainingTime = existingVerification.GetRemainingTime(); // Kalan süreyi hesapla
                return Json(new { success = false, message = $"Doğrulama kodunuz henüz geçerliliğini kaybetmedi. Lütfen {remainingTime} kadar bekleyin." });
            }
        }
        else
        {
            // Eğer kullanıcı hiç doğrulama kodu almadıysa
            return Json(new { success = false, message = "Doğrulama kodu bulunamadı." });
        }
    }





    [HttpPost]
    public IActionResult GetRemainingTime()
    {
        // Session'dan kullanıcı bilgisini al
        var userJson = HttpContext.Session.GetString("User");

        // Eğer session'da kullanıcı bilgisi yoksa hata döndür
        if (string.IsNullOrEmpty(userJson))
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        // JSON verisini User nesnesine dönüştür
        User user;
        try
        {
            user = JsonConvert.DeserializeObject<User>(userJson);
        }
        catch (JsonException ex)
        {
            return Json(new { success = false, message = "Kullanıcı bilgisi hatalı." });
        }

        // Kullanıcının son doğrulama kodunu al
        var verificationCode = _context.VerificationCodes
            .Where(vc => vc.UserId == user.Id)
            .OrderByDescending(vc => vc.SentAt) // En son gönderilen doğrulama kodu
            .FirstOrDefault();

        // Eğer doğrulama kodu yoksa hata döndür
        if (verificationCode == null)
        {
            return Json(new { success = false, message = "Doğrulama kodu bulunamadı." });
        }

        // Kalan süreyi hesapla
        int remainingTimeInSeconds = GetRemainingTimeForVerificationCode(verificationCode);
        var remainingTime = TimeSpan.FromSeconds(remainingTimeInSeconds);

        return Json(new
        {
            success = true,
            verificationCode = verificationCode.Code,  // Son doğrulama kodu
            remainingTime = remainingTime.ToString(@"mm\:ss")  // Kalan süre (dakika:saniye cinsinden)
        });
    }

    // Kalan süreyi hesaplayan metod
    // Kalan süreyi hesaplayan metod
    private int GetRemainingTimeForVerificationCode(VerificationCode verificationCode)
    {
        var expiryTime = verificationCode.SentAt.AddSeconds(verificationCode.ExpiryInSeconds); // Kodun geçerlilik süresi hesaplanıyor

        // Eğer süre dolmuşsa 0 döndür, değilse kalan süreyi döndür
        var remainingTime = (expiryTime - DateTime.UtcNow).TotalSeconds;

        return remainingTime > 0 ? (int)remainingTime : 0; // Süre 0'dan küçükse 0 döndür
    }

    private string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }
}
