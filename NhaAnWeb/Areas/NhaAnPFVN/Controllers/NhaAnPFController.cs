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
using ClosedXML.Excel;
using System.IO;
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

                // Lấy ngày hiện tại
                var today = DateTime.Today;
                var now = DateTime.Now;

                // Biến thời gian và số lượng suất ăn
                TimeSpan startTime = TimeSpan.Zero;
                TimeSpan endTime = TimeSpan.Zero;
                int mealCount = 0;

                // Bữa sáng: 05:00 - 08:00
                if (now.TimeOfDay >= new TimeSpan(5, 0, 0) && now.TimeOfDay < new TimeSpan(8, 0, 0))
                {
                    startTime = new TimeSpan(5, 0, 0);
                    endTime = new TimeSpan(8, 0, 0);

                    mealCount = _context.AttLogs.Count(tr =>
                        tr.DeviceName == selectedGate.GName &&
                        tr.AuthDateTime.HasValue &&
                        tr.AuthDateTime.Value.Date == today &&
                        tr.AuthDateTime.Value.TimeOfDay >= startTime &&
                        tr.AuthDateTime.Value.TimeOfDay <= endTime);
                }
                // Bữa trưa: 11:00 - 13:00
                else if (now.TimeOfDay >= new TimeSpan(11, 0, 0) && now.TimeOfDay < new TimeSpan(13, 0, 0))
                {
                    startTime = new TimeSpan(11, 0, 0);
                    endTime = new TimeSpan(13, 0, 0);

                    mealCount = _context.AttLogs.Count(tr =>
                        tr.DeviceName == selectedGate.GName &&
                        tr.AuthDateTime.HasValue &&
                        tr.AuthDateTime.Value.Date == today &&
                        tr.AuthDateTime.Value.TimeOfDay >= startTime &&
                        tr.AuthDateTime.Value.TimeOfDay <= endTime);
                }
                // Bữa tối: 16:30 - 19:00
                else if (now.TimeOfDay >= new TimeSpan(16, 30, 0) && now.TimeOfDay < new TimeSpan(19, 0, 0))
                {
                    startTime = new TimeSpan(16, 30, 0);
                    endTime = new TimeSpan(19, 0, 0);

                    mealCount = _context.AttLogs.Count(tr =>
                        tr.DeviceName == selectedGate.GName &&
                        tr.AuthDateTime.HasValue &&
                        tr.AuthDateTime.Value.Date == today &&
                        tr.AuthDateTime.Value.TimeOfDay >= startTime &&
                        tr.AuthDateTime.Value.TimeOfDay <= endTime);
                }
                // Bữa đêm: 23:30 - 01:00 (qua ngày mới)
                else if (now.TimeOfDay >= new TimeSpan(23, 30, 0) || now.TimeOfDay < new TimeSpan(1, 0, 0))
                {
                    startTime = new TimeSpan(23, 30, 0);
                    endTime = new TimeSpan(1, 0, 0);

                    DateTime startDate = now.TimeOfDay >= new TimeSpan(23, 30, 0) ? today : today.AddDays(-1);
                    DateTime endDate = startDate.AddDays(1);

                    mealCount = _context.AttLogs.Count(tr =>
                        tr.DeviceName == selectedGate.GName &&
                        tr.AuthDateTime.HasValue &&
                        (
                            // Trường hợp trong cùng ngày (23:30 - 23:59)
                            (tr.AuthDateTime.Value.Date == startDate && tr.AuthDateTime.Value.TimeOfDay >= startTime) ||
                            // Trường hợp sang ngày hôm sau (00:00 - 01:00)
                            (tr.AuthDateTime.Value.Date == endDate && tr.AuthDateTime.Value.TimeOfDay <= endTime)
                        ));
                }
                else
                {
                    return Json(new { count = 0, message = "Không phải giờ ăn" });
                }

                return Json(new
                {
                    count = mealCount,
                    mealTime = $"{startTime:hh\\:mm} - {endTime:hh\\:mm}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { count = 0, message = "Đã có lỗi xảy ra", error = ex.Message });
            }
        }

        [HttpGet("/PF/GetAttLogsByGate")]
        public IActionResult GetAttLogsByGate(int idgate)
        {
            try
            {
                // Kiểm tra đăng nhập
                var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
                if (authenticatedUser == null)
                {
                    return RedirectToAction("Login", "NhaAnPF");
                }

                // Kiểm tra cổng hợp lệ
                var gate = _context.GatePfs.FirstOrDefault(g => g.Id == idgate);
                if (gate == null)
                {
                    return Json(new { message = "Cổng không hợp lệ", data = new List<object>() });
                }

                DateTime today = DateTime.Today;
                DateTime now = DateTime.Now;
                TimeSpan currentTime = now.TimeOfDay;

                // Khai báo biến thời gian bắt đầu / kết thúc
                TimeSpan startTime = TimeSpan.Zero;
                TimeSpan endTime = TimeSpan.Zero;
                string mealName = "";
                DateTime startDate = today;
                DateTime endDate = today;

                // Xác định ca ăn hiện tại
                if (currentTime >= new TimeSpan(5, 0, 0) && currentTime < new TimeSpan(8, 0, 0))
                {
                    mealName = "Sáng";
                    startTime = new TimeSpan(5, 0, 0);
                    endTime = new TimeSpan(8, 0, 0);
                }
                else if (currentTime >= new TimeSpan(11, 0, 0) && currentTime < new TimeSpan(13, 0, 0))
                {
                    mealName = "Trưa";
                    startTime = new TimeSpan(11, 0, 0);
                    endTime = new TimeSpan(13, 0, 0);
                }
                else if (currentTime >= new TimeSpan(16, 30, 0) && currentTime < new TimeSpan(19, 0, 0))
                {
                    mealName = "Tối";
                    startTime = new TimeSpan(16, 30, 0);
                    endTime = new TimeSpan(19, 0, 0);
                }
                else if (currentTime >= new TimeSpan(23, 30, 0) || currentTime < new TimeSpan(1, 0, 0))
                {
                    mealName = "Đêm";
                    startTime = new TimeSpan(23, 30, 0);
                    endTime = new TimeSpan(1, 0, 0);

                    // Xử lý đặc biệt: ca đêm qua 2 ngày
                    if (currentTime < new TimeSpan(1, 0, 0))
                    {
                        startDate = today.AddDays(-1);
                        endDate = today;
                    }
                    else
                    {
                        startDate = today;
                        endDate = today.AddDays(1);
                    }
                }
                else
                {
                    return Json(new { message = "Hiện tại không nằm trong khung giờ ăn", data = new List<object>() });
                }

                // Lấy dữ liệu log trong khoảng thời gian ca hiện tại
                var logs = _context.AttLogs
                    .Where(a => a.DeviceName == gate.GName && a.AuthDateTime.HasValue)
                    .Where(a =>
                        // Trường hợp bình thường (không qua ngày)
                        (startDate == endDate && a.AuthDateTime.Value.Date == today &&
                         a.AuthDateTime.Value.TimeOfDay >= startTime &&
                         a.AuthDateTime.Value.TimeOfDay <= endTime)
                        ||
                        // Trường hợp ca đêm (qua ngày)
                        (startDate != endDate &&
                            ((a.AuthDateTime.Value.Date == startDate && a.AuthDateTime.Value.TimeOfDay >= startTime) ||
                             (a.AuthDateTime.Value.Date == endDate && a.AuthDateTime.Value.TimeOfDay <= endTime)))
                    )
                    .OrderByDescending(a => a.AuthDateTime)
                    .Select(a => new
                    {
                        a.EmployeeId,
                        a.PersonName,
                        AuthDateTime = a.AuthDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                        a.Direction,
                        a.DeviceName
                    })
                    .ToList();

                // Trả về kết quả
                return Json(new
                {
                    gateId = gate.Id,
                    gateName = gate.GName,
                    meal = mealName,
                    mealTime = $"{startTime:hh\\:mm} - {endTime:hh\\:mm}",
                    total = logs.Count,
                    data = logs
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    message = "Đã có lỗi xảy ra khi truy vấn dữ liệu",
                    error = ex.Message,
                    data = new List<object>()
                });
            }
        }

        [HttpGet("/PF/ExportAttLogsToExcel")]
        public IActionResult ExportAttLogsToExcel(DateTime start, DateTime end)
        {
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
            if (authenticatedUser == null)
                return Json(new { message = "Chưa đăng nhập" });

            // Tính khoảng thời gian: từ 01:00 ngày bắt đầu -> 01:00 ngày sau ngày kết thúc
            DateTime startDateTime = start.Date.AddHours(1);
            DateTime endDateTime = end.Date.AddDays(1).AddHours(1);

            // Lấy dữ liệu
            var logs = _context.AttLogs
                .Where(a => a.AuthDateTime.HasValue &&
                            a.AuthDateTime.Value >= startDateTime &&
                            a.AuthDateTime.Value < endDateTime)
                .OrderBy(a => a.AuthDateTime)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("AttLogs");

                // Header
                ws.Cell(1, 1).Value = "EmployeeId";
                ws.Cell(1, 2).Value = "PersonName";
                ws.Cell(1, 3).Value = "AuthDateTime";
                ws.Cell(1, 4).Value = "Direction";
                ws.Cell(1, 5).Value = "DeviceName";
                ws.Cell(1, 6).Value = "Meal";

                int row = 2;
                foreach (var log in logs)
                {
                    var time = log.AuthDateTime.Value.TimeOfDay;
                    string meal;

                    // Xác định ca ăn theo giờ, ngoài giờ là "0"
                    if (time >= new TimeSpan(5, 0, 0) && time < new TimeSpan(8, 0, 0))
                        meal = "Sáng";
                    else if (time >= new TimeSpan(11, 0, 0) && time < new TimeSpan(13, 0, 0))
                        meal = "Trưa";
                    else if (time >= new TimeSpan(16, 30, 0) && time < new TimeSpan(19, 0, 0))
                        meal = "Tối";
                    else if (time >= new TimeSpan(23, 30, 0) || time < new TimeSpan(1, 0, 0))
                        meal = "Đêm";
                    else
                        meal = "0"; // Ngoài giờ

                    ws.Cell(row, 1).Value = log.EmployeeId;
                    ws.Cell(row, 2).Value = log.PersonName;
                    ws.Cell(row, 3).Value = log.AuthDateTime?.ToString("yyyy-MM-dd HH:mm:ss");
                    ws.Cell(row, 4).Value = log.Direction;
                    ws.Cell(row, 5).Value = log.DeviceName;
                    ws.Cell(row, 6).Value = meal;

                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    string fileName = $"AttLogs_{start:yyyyMMdd}_to_{end:yyyyMMdd}.xlsx";

                    return File(stream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                fileName);
                }
            }
        }

        #endregion
    }
}
