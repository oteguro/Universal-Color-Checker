using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ColorChecker
{
    public partial class WindowChooser : Form
    {
        private readonly string[] ignoreProcessesStringArray = { "applicationframehost", "shellexperiencehost", "systemsettings", "winstore.app", "searchui", "pulse", "colorchecker", "custominfo" };
        private List<IntPtr> capturableWindowHandle = new List<IntPtr>();

        private IntPtr SelectedWindow   { get; set; }
        public int SelectedLut          { get; set; }
        public bool ApplyLut            { get; set; }

        public WindowChooser()
        {
            InitializeComponent();
        }

        public IntPtr PickCaptureTarget()
        {
            return SelectedWindow;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var index = listBox1.SelectedIndex;
            if (index >= 0)
            {
                SelectedWindow = capturableWindowHandle[index];
                this.Close();
            }
            var lutindex = comboBox1.SelectedIndex;
            if (lutindex >= 0)
            {
                SelectedLut = lutindex;
            }
            ApplyLut = checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FindWindows();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            FindWindows();
            SelectedLut = comboBox1.SelectedIndex = 0;
            ApplyLut = checkBox1.Checked = false;
        }

        private void FindWindows()
        {
            SelectedWindow = IntPtr.Zero;
            listBox1.Items.Clear();
            capturableWindowHandle.Clear();

            foreach (System.Diagnostics.Process p in System.Diagnostics.Process.GetProcesses())
            {
                if (p.MainWindowTitle.Length != 0)
                {
                    var windowHandle = p.MainWindowHandle;
                    if (!NativeMethods.IsWindowVisible(windowHandle))
                    {
                        continue;
                    }
                    if (ignoreProcessesStringArray.Contains(p.ProcessName.ToLower()))
                    {
                        continue;
                    }
                    var listName = p.ProcessName + " / " + p.MainWindowTitle;
                    listBox1.Items.Add(listName);
                    capturableWindowHandle.Add(windowHandle);
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
        }
    }
}
