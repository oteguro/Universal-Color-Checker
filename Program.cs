using System;

namespace ColorChecker
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            NativeMethods.SetProcessDPIAware();
            using var window = new AppWindow("Color Checker");
            window.Show();
        }
    }

} // namespace ColorChecker 
