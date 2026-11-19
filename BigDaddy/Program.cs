namespace BigDaddy
{
    // Starts the Windows application and opens the main form.
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            // Applies the standard Windows Forms visual settings before any forms are created.
            ApplicationConfiguration.Initialize();
            bool startInBackground = args.Any(argument =>
                string.Equals(argument, "--background", StringComparison.OrdinalIgnoreCase));

            // Windows autostart uses --background so Big Daddy begins from the tray.
            Application.Run(new BigDaddyForm(startInBackground));
        }
    }
}
