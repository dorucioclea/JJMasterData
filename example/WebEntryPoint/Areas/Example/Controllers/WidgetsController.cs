using Microsoft.AspNetCore.Mvc;

namespace JJMasterData.WebEntryPoint.Areas.Example.Controllers;


public class WidgetsController : ExampleController
{
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult Title()
    {
        return View();
    }
    public IActionResult Alert()
    {
        return View();
    }
}