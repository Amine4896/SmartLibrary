using Microsoft.Extensions.Configuration;
using SmartLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartLibrary.Services
{
    public class LibraryAssistantService
    {
        private readonly BookService _bookService;
        private readonly GroqService _groqService;
        private readonly IConfiguration _configuration;

        public LibraryAssistantService(
            BookService bookService,
            GroqService groqService,
            IConfiguration configuration)
        {
            _bookService = bookService;
            _groqService = groqService;
            _configuration = configuration;
        }

        public async Task<string> AskAsync(string? question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Veuillez poser une question valide.";
            }

            var questionLower = question.ToLower().Trim();
            var apiKey = _configuration["Groq:ApiKey"];
            var isOffline = string.IsNullOrWhiteSpace(apiKey);

            if (isOffline)
            {
                if (questionLower.Contains("disponible"))
                {
                    var books = await _bookService.GetAvailableBooks();
                    if (books.Any())
                    {
                        return "### 📚 Livres Disponibles Actuellement\n\nVoici la liste des ouvrages en stock :\n\n" + 
                            string.Join("\n", books.Select(b => $"- **{b.Title}** de *{b.Author}* (Stock : `{b.StockQuantity}`)"));
                    }
                    return "Désolé, aucun livre n'est disponible en stock pour le moment.";
                }

                if (questionLower.Contains("recommandation") || questionLower.Contains("recommande") || questionLower.Contains("propose"))
                {
                    var books = await _bookService.GetAvailableBooks();
                    if (books.Any())
                    {
                        var rand = new Random();
                        var b = books[rand.Next(books.Count)];
                        return $"### 💡 Recommandation de Lecture\n\nJe vous suggère de lire **{b.Title}** écrit par *{b.Author}*. Ce livre est actuellement disponible en stock (`{b.StockQuantity}` exemplaires) !";
                    }
                    return "Aucun livre n'est disponible en stock pour une recommandation pour le moment.";
                }

                if (questionLower.Contains("rupture") || questionLower.Contains("stock") || questionLower.Contains("épuisé"))
                {
                    var allBooks = await _bookService.GetAllBooks();
                    var outOfStock = allBooks.Where(b => b.StockQuantity <= 0).ToList();
                    if (outOfStock.Any())
                    {
                        return "### ⚠️ Livres en Rupture de Stock\n\nVoici la liste des ouvrages actuellement indisponibles :\n\n" + 
                            string.Join("\n", outOfStock.Select(b => $"- **{b.Title}** de *{b.Author}*"));
                    }
                    return "Excellente nouvelle ! Aucun livre n'est actuellement en rupture de stock.";
                }

                if (questionLower.Contains("conseil") || questionLower.Contains("organiser") || questionLower.Contains("lecture"))
                {
                    return "### 📖 Conseils pour Organiser votre Lecture\n\n" +
                        "1. **Fixez-vous un objectif quotidien** (ex: 15 à 20 pages par jour).\n" +
                        "2. **Créez un rituel de lecture** (lire le matin au réveil ou au calme avant de dormir).\n" +
                        "3. **Éliminez les distractions** (mettez votre smartphone en mode avion/silencieux).\n" +
                        "4. **Tenez un journal de lecture** pour noter vos citations et vos impressions.";
                }

                return "### 🤖 Assistant IA (Mode Hors-ligne)\n\n" +
                    "Je suis en mode local car aucune clé API Groq n'est configurée dans `appsettings.json`.\n\n" +
                    "Vous pouvez me poser ces questions spécifiques :\n" +
                    "- *Quels livres de programmation sont disponibles ?*\n" +
                    "- *Propose-moi une recommandation de livre.*\n" +
                    "- *Quels livres sont en rupture de stock ?*\n" +
                    "- *Conseils pour mieux organiser ma lecture.*";
            }

            if (questionLower.Contains("disponible"))
            {
                var books = await _bookService.GetAvailableBooks();
                var data = string.Join("\n", books.Select(b => $"- {b.Title} de {b.Author} (Stock: {b.StockQuantity})"));

                var prompt = $"""
Tu es l'assistant intelligent de la bibliothèque SmartLibrary.
Réponds de manière chaleureuse, polie, structurée et professionnelle en français.
Utilise le formatage Markdown.

Livres actuellement disponibles en stock :
{data}

Réponds à la question suivante en te basant sur ces données :
"{question}"
""";

                return await _groqService.AskAsync(prompt);
            }

            var genericPrompt = $"""
Tu es l'assistant intelligent de la bibliothèque SmartLibrary.
Réponds de manière chaleureuse, polie, structurée et professionnelle en français.
Utilise le formatage Markdown.

Réponds à la question suivante :
"{question}"
""";
            return await _groqService.AskAsync(genericPrompt);
        }
    }
}