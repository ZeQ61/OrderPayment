using Microsoft.EntityFrameworkCore;
using OrderPayment.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Servisleri ekleyin
builder.Services.AddControllersWithViews();
builder.Services.AddControllers();

// SmsService ba��ml�l���n� ekleyin
builder.Services.AddSingleton<SmsService>();

// SQL Server ba�lant�s� ile DbContext ekleyin
builder.Services.AddDbContext<OrderPaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Session ve MemoryCache ekleyin
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Oturum s�resi
    options.Cookie.HttpOnly = true;                // G�venlik i�in sadece HTTP eri�imi
    options.Cookie.IsEssential = true;             // GDPR uyumlulu�u
});

var app = builder.Build();

// Veritaban�n� her uygulama �al��t�r�ld���nda s�f�rlama i�lemi
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<OrderPaymentDbContext>();

    // Veritaban�n� sil ve yeniden olu�tur
    context.Database.EnsureDeleted();  // Veritaban�n� siler
    context.Database.Migrate();        // Veritaban�n� yeniden olu�turur
}

// Hata i�leme ve g�venlik yap�land�rmalar�
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Session'� etkinle�tir
app.UseSession();

app.UseAuthorization();

// Varsay�lan rota ayar�n� SmsController'daki SendSms action'�na y�nlendirin
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=AdminPanel}/{id?}"
);

app.Run();
