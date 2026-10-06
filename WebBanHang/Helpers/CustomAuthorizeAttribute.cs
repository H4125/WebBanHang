using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace WebBanHang.Helpers
{
    public class CustomAuthorizeAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        private readonly int[] _allowedRoles;

        // Cho phép truyền vào nhiều Role cùng lúc (VD: [CustomAuthorize(1, 2)])
        public CustomAuthorizeAttribute(params int[] roles)
        {
            _allowedRoles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Lấy mã quyền từ Session hiện tại
            int? userRole = context.HttpContext.Session.GetInt32("Role");

            if (userRole == null)
            {
                // Chưa đăng nhập -> Đá về trang Đăng nhập
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Nếu Action có cấu hình giới hạn quyền truy cập
            if (_allowedRoles != null && _allowedRoles.Length > 0)
            {
                // Nếu Role của user không nằm trong danh sách được phép
                if (!_allowedRoles.Contains(userRole.Value))
                {
                    // Đá về trang chủ (hoặc bạn có thể tạo một trang AccessDenied riêng)
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                }
            }
        }
    }
}