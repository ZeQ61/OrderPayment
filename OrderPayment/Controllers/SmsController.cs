using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrderPayment.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

public class SmsController : Controller
{
    private readonly SmsService _smsService;
    private readonly OrderPaymentDbContext _context;

    public SmsController(SmsService smsService, OrderPaymentDbContext context)
    {
        _smsService = smsService;
        _context = context;
    }

    [HttpGet]
    public IActionResult SendSms()
    {
        return View("SendSms");
    }

    [HttpGet]
    public IActionResult VerifySms()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(User user)
    {
        if (string.IsNullOrEmpty(user.PhoneNumber))
        {
            return Json(new { success = false, message = "Telefon numarası gereklidir." });
        }

        // Kullanıcı var mı kontrolü
        var existingUser = _context.Users.FirstOrDefault(u => u.PhoneNumber == user.PhoneNumber);
        if (existingUser != null)
        {
            return Json(new { success = false, message = "Bu telefon numarasıyla zaten kayıtlı bir kullanıcı var." });
        }

        // Doğrulama kodu oluştur
        var verificationCode = GenerateVerificationCode();

        // SMS gönderimi
        var message = $"Merhaba {user.FirstName},\n\n" +
                      "Hesabınızı güvenli bir şekilde oluşturabilmeniz için, aşağıda yer alan doğrulama kodunu kullanabilirsiniz:\n\n" +
                      $"Doğrulama Kodu: {verificationCode}\n\n" +
                      "Kodunuzun geçerlilik süresi sınırlıdır, lütfen en kısa sürede giriniz.\n\n" +
                      "Teşekkürler!";
        bool smsSent = _smsService.SendSms(user.PhoneNumber, message);

        if (smsSent)
        {
            // Kullanıcıyı veritabanına ekle ve kaydet
            user.IsActive = false; // Kullanıcı aktif değil
            user.CreatedAt= DateTime.Now;
            _context.Users.Add(user);
            _context.SaveChanges();

            // Doğrulama kodu oluştur ve kullanıcıya bağla
            var verification = new VerificationCode
            {
                Code = verificationCode,
                SentAt = DateTime.UtcNow,
                ExpiryInSeconds = 60, // Kod geçerlilik süresi 1 dakika (60 saniye)
                UserId = user.Id // Kullanıcıya bağla
            };

            _context.VerificationCodes.Add(verification); // Doğrulama kodunu veritabanına ekle
            _context.SaveChanges(); // Doğrulama kodunu kaydet

            // Kullanıcıyı session'a kaydet
            HttpContext.Session.SetString("User", JsonConvert.SerializeObject(user,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                }));

            return Json(new { success = true });
        }
        else
        {
            return Json(new { success = false, message = "SMS gönderilemedi." });
        }
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

        // Kullanıcının doğrulama kodunu kontrol et
        var verification = _context.VerificationCodes
            .Where(vc => vc.UserId == user.Id && vc.Code == verificationCode)
            .OrderByDescending(vc => vc.SentAt) // En son gönderilen kodu al
            .FirstOrDefault();

        if (verification != null)
        {
            // Kodun süresinin bitip bitmediğini kontrol et
            if (verification.IsExpired()) // Yeni modelde doğrulama kodu süresi kontrolü
            {
                return Json(new { success = false, message = "Geçersiz veya süresi dolmuş doğrulama kodu!" });
            }

            // Kalan süreyi hesapla
            var remainingTime = verification.GetRemainingTime(); // Kalan süreyi hesapla (saniye cinsinden)

            // Eğer geçerli kodsa, kullanıcıyı aktif yap
            if (!verification.IsExpired())
            {
                user.IsActive = true; // Kullanıcıyı aktif yap
                _context.Users.Update(user);
                _context.SaveChanges();

                return Json(new { success = true, message = "Kullanıcı başarıyla kaydedildi ve aktif edildi.", remainingTime = remainingTime });
            }
        }
        else
        {
            // Kullanıcı daha önce doğrulama kodunu kullanmışsa ve yeni doğrulama kodu alınması isteniyorsa hata mesajı
            return Json(new { success = false, message = "Geçersiz doğrulama kodu veya kodu daha önce kullanmışsınız." });
        }

        return Json(new { success = false, message = "Geçersiz doğrulama kodu!" });
    }




    [HttpPost]
    public IActionResult ResendVerificationCode(User user)
    {
        // Kullanıcının en son doğrulama kodunu al
        var existingVerification = _context.VerificationCodes
            .Where(vc => vc.UserId == user.Id)
            .OrderByDescending(vc => vc.SentAt) // En son gönderilen doğrulama kodu
            .FirstOrDefault();

        // Eğer mevcut doğrulama kodu varsa
        if (existingVerification != null)
        {
            // Süresi dolmuşsa yeni bir doğrulama kodu gönder
            if (existingVerification.IsExpired()) // Yeni modelde doğrulama kodunun süresi kontrolü
            {
                // Yeni doğrulama kodu oluştur
                var verificationCode = GenerateVerificationCode();

                // SMS gönderimi
                var message = $"Merhaba {user.FirstName} {user.LastName}, yeni doğrulama kodunuz: {verificationCode}";
                bool smsSent = _smsService.SendSms(user.PhoneNumber, message);

                if (smsSent)
                {
                    // Yeni doğrulama kodunu veritabanına kaydet
                    existingVerification.Code = verificationCode;
                    existingVerification.SentAt = DateTime.UtcNow; // Kodun gönderilme zamanı güncelleniyor
                    existingVerification.ExpiryInSeconds = 60; // Kod geçerlilik süresi 60 saniye olarak ayarlandı

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
    private int GetRemainingTimeForVerificationCode(VerificationCode verificationCode)
    {
        var expiryTime = verificationCode.SentAt.AddSeconds(verificationCode.ExpiryInSeconds); // Kodun geçerlilik süresi hesaplanıyor

        // Eğer süre dolmuşsa 0 döndür, değilse kalan süreyi döndür
        var remainingTime = (expiryTime - DateTime.UtcNow).TotalSeconds;
        return remainingTime > 0 ? (int)remainingTime : 0;
    }

    private string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }
}
