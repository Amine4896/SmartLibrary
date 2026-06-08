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
}