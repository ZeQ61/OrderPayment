using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrderPayment.Models;
using Microsoft.AspNetCore.Http;
using System;

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
    public IActionResult SendSms(User user)
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
        var message = $"Merhaba {user.FirstName} {user.LastName}, doğrulama kodunuz: {verificationCode}";
        bool smsSent = _smsService.SendSms(user.PhoneNumber, message);

        if (smsSent)
        {
            user.VerificationCode = verificationCode;
            HttpContext.Session.SetString("User", JsonConvert.SerializeObject(user));
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

        if (user.VerificationCode == verificationCode)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return Json(new { success = true, message = "Kullanıcı başarıyla kaydedildi." });
        }
        else
        {
            return Json(new { success = false, message = "Geçersiz doğrulama kodu!" });
        }
    }

    private string GenerateVerificationCode()
    {
        return new Random().Next(100000, 999999).ToString();
    }
}
