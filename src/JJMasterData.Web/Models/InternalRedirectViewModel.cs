namespace JJMasterData.Web.Models;

public class InternalRedirectViewModel
{
    public string HtmlContent { get; set; }
    public bool ShowToolBar { get; set; }

    public InternalRedirectViewModel(string htmlContent, bool showToolBar)
    {
        HtmlContent = htmlContent;
        ShowToolBar = showToolBar;
    }
}