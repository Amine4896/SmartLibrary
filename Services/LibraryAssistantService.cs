using SmartLibrary.Services;

namespace SmartLibrary.Services;

public class LibraryAssistantService
{
    private readonly BookService _bookService;
    private readonly GroqService _groqService;

    public LibraryAssistantService(
        BookService bookService,
        GroqService groqService)
    {
        _bookService = bookService;
        _groqService = groqService;
    }

    public async Task<string> AskAsync(string question)
    {
        question = question.ToLower();

        if (question.Contains("disponible"))
        {
            var books = await _bookService.GetAvailableBooks();

            var data = string.Join("\n",
                books.Select(b =>
                    $"- {b.Title} (Stock: {b.StockQuantity})"));

            var prompt = $"""
Tu es l'assistant de la bibliothèque.

Livres disponibles :

{data}

Réponds à la question :
{question}
""";

            return await _groqService.AskAsync(prompt);
        }

        return await _groqService.AskAsync(question);
    }
}