using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using NhaAnWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Packaging;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using NhaAnWeb.Context;
namespace KhoEST.Areas.NhaAnPFVN.Controllers
{
    [Area("NhaAnPFVN")]
    public class NhaAnPFController : Controller
    {

        #region khai bao
        public DBIVMS4200Context _context;
        public NhaAnPFController()
        {
            _context = new DBIVMS4200Context();
        }
        #endregion

        #region dang nhap 1
        //nhà ăn best
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login2(User user)
        {
            var authenticatedUser = _context.NhanViens.FirstOrDefault(u => u.TenDangNhap == user.Username && u.MatKhau == user.Password && u.TrangThai == true);

            if (authenticatedUser != null)
            {
                // Serialize đối tượng nhân viên thành chuỗi JSON
                string serializedUser = JsonConvert.SerializeObject(authenticatedUser);

                // Lưu chuỗi JSON và thời gian đăng nhập vào Session
                HttpContext.Session.SetString("NhaAnPF", serializedUser);
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());

                // Lấy ChucVu, nếu null thì gán 0
                int chucVuValue = authenticatedUser.ChucVu ?? 0;
                HttpContext.Session.SetInt32("UserChucVu", chucVuValue);

                // Đăng nhập thành công
                return RedirectToAction("Index", "NhaAnPF", new { area = "NhaAnPFVN" });
            }
            else
            {
                // Đăng nhập không thành công
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View("Login", user);
            }
        }

        private bool IsSessionExpired()
        {
            var loginTimeString = HttpContext.Session.GetString("LoginTime");
            if (string.IsNullOrEmpty(loginTimeString))
            {
                return true; // Chưa đăng nhập hoặc Session không tồn tại
            }

            var loginTime = DateTime.Parse(loginTimeString);
            var currentTime = DateTime.Now;

            // Kiểm tra nếu thời gian đăng nhập đã vượt quá 3 tiếng
            return (currentTime - loginTime).TotalHours > 3;
        }

        public class User
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpGet("/PF/GetSessionRemainingTime")]
        public JsonResult GetSessionRemainingTime()
        {
            var loginTimeStr = HttpContext.Session.GetString("LoginTime");

            if (string.IsNullOrEmpty(loginTimeStr))
            {
                return Json(new
                {
                    success = false,
                    message = "Chưa đăng nhập hoặc phiên đã hết.",
                    hours = 0,
                    minutes = 0,
                    seconds = 0
                });
            }

            if (!DateTime.TryParse(loginTimeStr, out DateTime loginTime))
            {
                return Json(new
                {
                    success = false,
                    message = "Dữ liệu thời gian đăng nhập không hợp lệ.",
                    hours = 0,
                    minutes = 0,
                    seconds = 0
                });
            }

            DateTime expireTime = loginTime.AddHours(3);
            TimeSpan remaining = expireTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Phiên đăng nhập đã hết hạn.",
                    hours = 0,
                    minutes = 0,
                    seconds = 0
                });
            }

            return Json(new
            {
                success = true,
                hours = remaining.Hours,
                minutes = remaining.Minutes,
                seconds = remaining.Seconds
            });
        }

        #endregion

        #region Nhà ăn PF
        [HttpGet("/PF/An")]
        public IActionResult Index()
        {
            // Kiểm tra nếu Session hết hạn
            if (IsSessionExpired())
            {
                HttpContext.Session.Clear(); // Xóa Session
                return RedirectToAction("Login", "NhaAnPF", new { area = "NhaAnPFVN" });
            }
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
            if (authenticatedUser == null)
            {
                return RedirectToAction("Index", "NhaAnPF", new { area = "NhaAnPFVN" });
            }
            return View();
        }

        [HttpGet("/PF/Gate")]
        public IActionResult GetGates()
        {
            var gates = _context.GatePfs
                .Select(g => new
                {
                    g.Id,       // ID cổng
                    g.GName     // Tên cổng
                })
                .ToList();

            return Ok(gates); // Trả về danh sách cổng dạng JSON
        }
        [HttpGet("/PF/TotalMeals")]
        public JsonResult TotalMeals()
        {
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");

            if (authenticatedUser == null)
            {
                // User is not authenticated, redirect to login
                return Json(new { error = "Not authenticated" });
            }
            else
            {
                // Assume you have a method to get the total number of meals from the database
                int totalMeals = GetTotalMealsFromDatabase();
                return Json(totalMeals);
            }
        }
        private int GetTotalMealsFromDatabase()
        {
            DateTime today = DateTime.Today;

            // Query using LINQ to EF Core
            int todayRecordCount = _context.AttLogs
                .Count();

            return todayRecordCount;
        }
    

        [HttpGet("/PF/GetMealCountByGate")]
        public IActionResult GetMealCountByGate(int idgate)
        {
            try
            {
                var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
                if (authenticatedUser == null)
                {
                    return RedirectToAction("Login", "NhaAnPF");
                }
                // Kiểm tra cổng hợp lệ
                var selectedGate = _context.GatePfs.FirstOrDefault(g => g.Id == idgate);
                if (selectedGate == null)
                {
                    return Json(new { count = 0, message = "Cổng không hợp lệ" });
                }

                // Lấy ngày hôm nay và thời gian hiện tại
                var today = DateTime.Today;
                var now = DateTime.Now;

                // Xác định khoảng thời gian bữa ăn dựa trên thời gian hiện tại
                TimeSpan startTime = TimeSpan.Zero;
                TimeSpan endTime = TimeSpan.Zero;
                int mealCount = 0;

                if (now.Hour >= 6 && now.Hour < 9) // Bữa sáng
                {
                    startTime = new TimeSpan(6, 0, 0);
                    endTime = new TimeSpan(9, 0, 0);
                    mealCount = _context.AttLogs.Count(tr =>
                        tr.DeviceName == selectedGate.GName &&
                        tr.AuthDateTime == today.Date &&
                        tr.AuthDateTime.Value.TimeOfDay >= startTime &&
                        tr.AuthDateTime.Value.TimeOfDay <= endTime);
                }
                else if (now.Hour >= 11 && now.Hour < 14) // Bữa trưa
                {
                    startTime = new TimeSpan(11, 0, 0);
                    endTime = new TimeSpan(14, 0, 0);
                    mealCount = _context.AttLogs.Count(tr =>
                         tr.DeviceName == selectedGate.GName &&
                         tr.AuthDateTime == today.Date &&
                         tr.AuthDateTime.Value.TimeOfDay >= startTime &&
                         tr.AuthDateTime.Value.TimeOfDay <= endTime);
                }
                else if (now.Hour >= 17 && now.Hour < 20) // Bữa tối
                {
                    startTime = new TimeSpan(17, 0, 0);
                    endTime = new TimeSpan(20, 0, 0);
                    mealCount = _context.AttLogs.Count(tr =>
                        tr.DeviceName == selectedGate.GName &&
                        tr.AuthDateTime == today.Date &&
                        tr.AuthDateTime.Value.TimeOfDay >= startTime &&
                        tr.AuthDateTime.Value.TimeOfDay <= endTime);
                }
                else if (now.Hour >= 23 || now.Hour < 2) // Bữa đêm (23:00 - 01:30)
                {
                    startTime = new TimeSpan(23, 0, 0);
                    endTime = new TimeSpan(1, 30, 0);

                    // Ngày hôm trước và hôm nay
                    var previousDay = today.AddDays(-1);

                    mealCount = _context.AttLogs.Count(tr =>
                        tr.DeviceName == selectedGate.GName &&
                        tr.AuthDateTime == today.Date &&
                        tr.AuthDateTime.Value.TimeOfDay >= startTime &&
                        tr.AuthDateTime.Value.TimeOfDay <= endTime);
                }
                else
                {
                    return Json(new { count = 0, message = "Không phải giờ ăn" });
                }

                return Json(new { count = mealCount, mealTime = $"{startTime} - {endTime}" });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ví dụ: ghi log lỗi)
                return Json(new { count = 0, message = "Đã có lỗi xảy ra", error = ex.Message });
            }
        }
        [HttpGet("/PF/GetAttLogsByGate")]
        public IActionResult GetAttLogsByGate(int idgate)
        {
            var gate = _context.GatePfs.FirstOrDefault(g => g.Id == idgate);
            if (gate == null)
                return Json(new List<object>()); // Không có cổng

            var logs = _context.AttLogs
                .Where(a => a.DeviceName == gate.GName)
                .OrderByDescending(a => a.AuthDateTime)
                .Select(a => new
                {
                    a.EmployeeId,
                    a.PersonName,
                    AuthDateTime = a.AuthDateTime.HasValue ? a.AuthDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    a.Direction,
                    a.DeviceName
                })
                .ToList();

            return Json(logs);
        }

        #endregion
    }
}
