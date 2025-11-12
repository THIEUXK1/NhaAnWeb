using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Th那m d?ch v? v角o container
// ----------------------------
builder.Services.AddControllersWithViews();

// Th那m c?u h足nh Session tr??c khi build app
builder.Services.AddDistributedMemoryCache(); // B?t bu?c cho session l?u trong b? nh?
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(3); // Session t?n t?i t?i ?a 3 gi?
    options.Cookie.HttpOnly = true; // B?o m?t h?n
    options.Cookie.IsEssential = true; // C?n thi?t cho ch?c n?ng c? b?n
});

var app = builder.Build();

// ----------------------------
// C?u h足nh pipeline HTTP
// ----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // N?u b?n c車 file t?nh (wwwroot)

app.UseRouting();

// Th那m middleware Session tr??c Authorization
app.UseSession();

app.UseAuthorization();

// ----------------------------
// C?u h足nh route cho Area
// ----------------------------
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// Route m?c ??nh n?u kh?ng c車 area
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
