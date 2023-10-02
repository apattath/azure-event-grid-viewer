using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace viewer.Views.Message
{
    public class ChatModel : PageModel
    {
        public ChatModel()
        {
        }

        [BindProperty]
        public string Message { get; set; }

        [BindProperty]
        public string Environment { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            // Call the Send action method on the MessageController
            return RedirectToPage("/Message/Send", new { message = Message });
        }
    }
}
