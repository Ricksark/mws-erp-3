using System.Diagnostics;
using System.Reflection;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MwsErp.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private readonly WebView2 _webView;

    public MainForm()
    {
        Text = "MWS ERP · Purchases & Sales";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        _webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(_webView);

        Load += OnLoadAsync;
    }

    private async void OnLoadAsync(object? sender, EventArgs e)
    {
        try
        {
            var appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MwsErp");
            var wwwrootDir = Path.Combine(appDataRoot, "wwwroot");
            var userDataDir = Path.Combine(appDataRoot, "WebView2");
            Directory.CreateDirectory(wwwrootDir);
            Directory.CreateDirectory(userDataDir);

            ExtractEmbeddedHtml(Path.Combine(wwwrootDir, "index.html"));

            var env = await CoreWebView2Environment.CreateAsync(null, userDataDir);
            await _webView.EnsureCoreWebView2Async(env);

            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "app.mwserp.local",
                wwwrootDir,
                CoreWebView2HostResourceAccessKind.Allow);

            _webView.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Uri))
                {
                    args.Handled = true;
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = args.Uri,
                        UseShellExecute = true
                    });
                }
            };

            _webView.Source = new Uri("https://app.mwserp.local/index.html");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to initialize the application view.\n\nEnsure Microsoft Edge WebView2 Runtime is installed.\n\nDetails: {ex.Message}",
                "MWS ERP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void ExtractEmbeddedHtml(string destinationPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "MwsErp.App.Assets.index.html";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var fileStream = File.Create(destinationPath);
        stream.CopyTo(fileStream);
    }
}
