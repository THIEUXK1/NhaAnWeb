using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------
// Thêm dịch vụ vào container
// ----------------------------
builder.Services.AddControllersWithViews();

// Thêm cấu hình Session trước khi build app
builder.Services.AddDistributedMemoryCache(); // Bắt buộc cho session lưu trong bộ nhớ
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(3); // Session tồn tại tối đa 3 giờ
    options.Cookie.HttpOnly = true; // Bảo mật hơn
    options.Cookie.IsEssential = true; // Cần thiết cho chức năng cơ bản
});

var app = builder.Build();

// ----------------------------
// Cấu hình pipeline HTTP
// ----------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thêm middleware Session trước Authorization
app.UseSession();

// Middleware redirect trang gốc "/" sang Login
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/NhaAnPFVN/NhaAnPF/Login");
        return;
    }
    await next();
});

app.UseAuthorization();

// ----------------------------
// Cấu hình route cho Area
// ----------------------------
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// Route mặc định nếu không có area
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
