using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace web.app.Controllers;

[Authorize]
public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Setup(string email, string pwd)
    {
        if (System.IO.File.Exists("users.json")) {
            return Redirect($"/?_={Environment.TickCount64:x}");
        }
        if (Request.Method == "GET") {
            return View();
        }
        try {
            if (!System.IO.File.Exists("users.json")) {
                List<User> users = [];
                users.Add(new User {
                    Email = email,
                    Pwd = pwd,
                    Role = "root",
                });
                System.IO.File.WriteAllText("users.json", JsonSerializer.Serialize(new {
                    User = users
                }, App._json_serializer_options));
            }
            return Ok(new {
                Code = 200,
                Message = $"<ol>" +
                $"<li>��ʼ���ɹ��������·�����վ��</li>" +
                $"</ol>"
            });
        } catch (Exception) {
            return Ok(new {
                Code = 200,
                Message = "����������"
            });
        }
    }

    public IActionResult Index(string group, int page = 1, int size = 20)
    {
        var values = WorkerController._session.Values;
        ViewBag.Count = (values.Count(x => x.Group == group || string.IsNullOrWhiteSpace(group)) / size) + 1;
        ViewBag.Page = page;
        ViewBag.Group = group;
        var groups = values.GroupBy(x => x.Group).Select(x => x.Key).Distinct().ToList();
        if (!groups.Contains("Ĭ�Ϸ���")) {
            groups.Add("Ĭ�Ϸ���");
        }
        ViewBag.Groups = groups
            .OrderByDescending(x => x == "Ĭ�Ϸ���")
            .ThenBy(g => g == "����վ")
            .ThenBy(g => g);
        return View();
    }
}