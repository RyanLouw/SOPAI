using Microsoft.AspNetCore.Mvc;
using SOPSearch.Web.Models;
using SOPSearch.Web.Services;
using System.Diagnostics;
using System.Reflection;

namespace SOPSearch.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IApiClient _api;

        public HomeController(ILogger<HomeController> logger, IApiClient api)
        {
            _logger = logger;
            _api = api;
        }

        // --- INDEX SELECTION ---
        [HttpGet]
        public async Task<IActionResult> SelectIndex(CancellationToken ct)
        {
            var vm = new SelectIndexViewModel();

            try
            {
                vm.Indexes = await _api.GetAllTags(ct) ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to load search indexes.");
                ModelState.AddModelError(string.Empty, "We could not load the available indexes. Please try again.");
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectIndex(SelectIndexViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    vm.Indexes = await _api.GetAllTags(ct) ?? new List<string>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to load search indexes.");
                    ModelState.AddModelError(string.Empty, "We could not load the available indexes. Please try again.");
                }

                return View(vm);
            }

            return RedirectToAction(nameof(Chat), new { selectedTagSource = vm.SelectedTagSource });
        }

        // --- CHAT ---
        [HttpGet]
        public IActionResult Chat(string? selectedTagSource)
        {
            if (string.IsNullOrWhiteSpace(selectedTagSource))
                return RedirectToAction(nameof(SelectIndex));

            return View(new ChatViewModel { SelectedTagSource = selectedTagSource });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskChat(ChatViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Please enter a question before sending it." });

            try
            {
                string answer = await _api.AskChatAsync(vm.Question, vm.SelectedTagSource, ct);
                if (answer.StartsWith("ERROR"))
                {
                    _logger.LogError(answer);
                    return BadRequest(new { error = answer });
                }

                return Json(new { answer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "We could not get an answer. Please try again." });
            }
        }

        // --- UPLOAD ---
        [HttpGet]
        public IActionResult Upload()
            => View(new UploadViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadViewModel vm, CancellationToken ct)
        {
            const long maxBytes = 4 * 1024 * 1024; // 4 MB

            if (!ModelState.IsValid)
                return View(vm);

            if (vm.File == null || vm.File.Length == 0)
            {
                ModelState.AddModelError(nameof(vm.File), "Please select a file.");
                return View(vm);
            }

            if (vm.File.Length > maxBytes)
            {
                ModelState.AddModelError(nameof(vm.File), "File too large. Maximum allowed is 10 MB.");
                return View(vm);
            }

            var allowedExtensions = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
            if (!allowedExtensions.Contains(vm.File!.ContentType))
            {
                ModelState.AddModelError("File", "Invalid file type.");
                return View(vm);
            }

            vm.Tags = vm.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList();

            try
            {
                vm.Result = await _api.UploadFileAsync(vm, ct);
                if (!vm.Result.StartsWith("ERROR"))
                {
                    _logger.LogError(vm.Result);
                    vm.Error = vm.Result;
                    vm.SOPName = "";
                    vm.Result = "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                vm.Error = ex.Message;
            }

            ModelState.Clear();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ViewAll(CancellationToken ct)
        {
            DocumentViewModel model = new DocumentViewModel();
            try
            {
                model.Documents = await _api.GetAllDocuments(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                model.Error = ex.Message;
            }
            ModelState.Clear();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ViewAll(string sopName, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(sopName))
                return await ViewAll(ct);

            var result = await _api.DeleteDocument(sopName, ct);

            if (result.StartsWith("ERROR"))
            {
                DocumentViewModel model = new DocumentViewModel();
                _logger.LogError(result);
                model.Error = result;
                return View(model);
            }

            await Task.Delay(TimeSpan.FromSeconds(2));

            return RedirectToAction("ViewAll");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
