using Microsoft.AspNetCore.Mvc;
using SmartLibrary.Models;
using SmartLibrary.Services;

public class ChatController : Controller
{
    private readonly LibraryAssistantService _assistant;

    public ChatController(
        LibraryAssistantService assistant)
    {
        _assistant = assistant;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Ask(ChatViewModel model)
    {
        model.Response =
            await _assistant.AskAsync(model.Question);

        return View("Index", model);
    }

    [HttpPost]
    public async Task<IActionResult> AskJson([FromBody] ChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Question))
        {
            return Json(new { response = "Veuillez poser une question." });
        }

        var response = await _assistant.AskAsync(request.Question);
        return Json(new { response });
    }
}

public class ChatRequest
{
    public string Question { get; set; } = string.Empty;
}