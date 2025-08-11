using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace web.app.Controllers;

public class IdentityController(IConfiguration cfg, IMemoryCache cache) : Controller
{
    public async Task<IActionResult> SignOut(string ReturnUrl)
    {
        await HttpContext.SignOutAsync();
        return Redirect(ReturnUrl);
    }

    public async Task<IActionResult> SignIn(string email, string pwd)
    {
        if (Request.Method == "GET") {
            if (!System.IO.File.Exists("users.json")) {
                return Redirect($"/home/setup?cid={HttpContext.Connection.Id.ToLower()}");
            }
            return View();
        }
        if (cache.TryGetValue("@lock", out int failCount) && failCount >= 3) {
            return Ok(new {
                Code = 200,
                Message = "��¼ʧ�ܴ������࣬��15���Ӻ����ԡ�"
            });
        }
        List<User> users = [];
        cfg.Bind("User", users);
        if (users.SingleOrDefault(x => x.Email == email && x.Pwd == pwd) is User entry) {
            cache.Remove("@lock");
            List<Claim> claims = [new Claim(ClaimTypes.Name, entry.Email), new Claim(ClaimTypes.Role, entry.Role)];
            var identity = new ClaimsIdentity(claims, CustomAuthorizeHandler.AuthenticationScheme);
            await HttpContext.SignInAsync(new ClaimsPrincipal(identity));
            return Ok(new {
                Code = 200,
                Url = "/"
            });
        }
        var newFailCount = cache.Set("@lock", failCount + 1, TimeSpan.FromMinutes(15));
        if (newFailCount >= 3) {
            return Ok(new {
                Code = 200,
                Message = "�������������࣬�˻�����ʱ��������15���Ӻ����ԡ�"
            });
        }
        return Ok(new {
            Code = 200,
            Message = $"�û�����������󣬻��� {3 - newFailCount} �λ��ᡣ"
        });
    }
}