using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SCEAMS.MVC.ViewModels;

namespace SCEAMS.MVC.Controllers;

[Authorize]
[Route("Dashboard")]
public sealed class DashboardController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var currentRole = User.FindFirstValue(ClaimTypes.Role);

        return string.IsNullOrWhiteSpace(currentRole)
            ? Forbid()
            : RedirectToAction(
                nameof(RoleDashboard),
                new { role = currentRole });
    }

    [HttpGet("{role}")]
    public IActionResult RoleDashboard(string role)
    {
        var currentRole = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(currentRole))
        {
            return Forbid();
        }

        if (!string.Equals(
                role,
                currentRole,
                StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(
                nameof(RoleDashboard),
                new { role = currentRole });
        }

        var content = GetRoleContent(currentRole);
        var model = new DashboardViewModel
        {
            FullName =
                User.FindFirstValue(ClaimTypes.Name) ?? "SCEAMS User",
            Email =
                User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            Role = currentRole,
            Heading = content.Heading,
            Description = content.Description,
            NextStep = content.NextStep,
            PrimaryActionText = content.PrimaryActionText,
            PrimaryController = content.PrimaryController,
            PrimaryAction = content.PrimaryAction
        };

        return View("Index", model);
    }

    private static RoleContent GetRoleContent(string role)
    {
        return role switch
        {
            "Admin" => new(
                Heading: "Trung tâm quản trị",
                Description:
                    "Theo dõi sức khỏe nền tảng và chuẩn bị quản lý tài khoản toàn hệ thống.",
                NextStep:
                    "Các công cụ quản trị người dùng sẽ được mở ở Milestone C.",
                PrimaryActionText: "Kiểm tra hệ thống",
                PrimaryController: "System",
                PrimaryAction: "Health"),
            "Staff" => new(
                Heading: "Không gian Student Affairs",
                Description:
                    "Điều phối hoạt động sinh viên và chuẩn bị quy trình duyệt câu lạc bộ, sự kiện.",
                NextStep:
                    "Luồng duyệt nghiệp vụ sẽ xuất hiện trong các milestone tiếp theo.",
                PrimaryActionText: "Xem trạng thái dữ liệu",
                PrimaryController: "System",
                PrimaryAction: "Health"),
            "Organizer" => new(
                Heading: "Không gian nhà tổ chức",
                Description:
                    "Sẵn sàng quản lý câu lạc bộ, sự kiện và cộng đồng sinh viên của bạn.",
                NextStep:
                    "Chức năng câu lạc bộ và sự kiện đang được triển khai theo roadmap.",
                PrimaryActionText: "Khám phá nền tảng",
                PrimaryController: "Home",
                PrimaryAction: "Index"),
            "Student" => new(
                Heading: "Hành trình sinh viên",
                Description:
                    "Khám phá cơ hội, kết nối câu lạc bộ và theo dõi trải nghiệm ngoại khóa.",
                NextStep:
                    "Hồ sơ cá nhân đã sẵn sàng với dữ liệu mới nhất từ API.",
                PrimaryActionText: "Xem hồ sơ của tôi",
                PrimaryController: "Profile",
                PrimaryAction: "Index"),
            _ => new(
                Heading: "Không gian SCEAMS",
                Description:
                    "Phiên đăng nhập của bạn đã sẵn sàng.",
                NextStep:
                    "Liên hệ quản trị viên nếu vai trò chưa được cấu hình đúng.",
                PrimaryActionText: "Về trang chủ",
                PrimaryController: "Home",
                PrimaryAction: "Index")
        };
    }

    private sealed record RoleContent(
        string Heading,
        string Description,
        string NextStep,
        string PrimaryActionText,
        string PrimaryController,
        string PrimaryAction);
}
