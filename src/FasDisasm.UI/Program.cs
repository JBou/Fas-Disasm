namespace FasDisasm.UI;

static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        var mainForm = new MainForm();

        // Handle command line arguments
        if (args.Length > 0 && File.Exists(args[0]))
        {
            mainForm.LoadFileOnStart(args[0]);
        }

        Application.Run(mainForm);
    }
}
