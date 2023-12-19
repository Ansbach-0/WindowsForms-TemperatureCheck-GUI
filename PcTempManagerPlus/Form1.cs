using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenHardwareMonitor.Hardware;
using System.Security.Principal;
using System.Windows.Forms.DataVisualization.Charting;

namespace PcTempManagerPlus
{
    public partial class form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        //create new computer for monitoring
        private Computer computer = new Computer()
        {
            CPUEnabled = true,
            GPUEnabled = true
        };

        Chart chartCPU = new Chart();
        ChartArea areaCPU = new ChartArea();
        Series seriesCPU = new Series();

        Chart chartGPU = new Chart();
        ChartArea areaGPU = new ChartArea();
        Series seriesGPU = new Series();

        public form1()
        {
            if (!IsAdministrator())
            {
                // Ask the user to run as admin and restart
                RunAsAdmin();
                // Close the current instance
                this.Close();
            }

            chartCPU.Dock = DockStyle.Fill;
            Controls.Add(chartCPU);
            chartCPU.ChartAreas.Add(areaCPU);
            chartCPU.Series.Add(seriesCPU);
            chartCPU.BackColor = Color.FromArgb(255, 136, 48, 78);
            chartCPU.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.White;
            chartCPU.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.White;
            seriesCPU.ChartType = SeriesChartType.Line;
            seriesCPU.Color = Color.FromArgb(255, 136, 48, 78);

            chartGPU.Dock = DockStyle.Fill;
            Controls.Add(chartGPU);
            chartGPU.ChartAreas.Add(areaGPU);
            chartGPU.Series.Add(seriesGPU);
            chartGPU.BackColor = Color.FromArgb(255, 136, 48, 78);
            chartGPU.ChartAreas[0].AxisX.LabelStyle.ForeColor = Color.White;
            chartGPU.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.White;
            seriesGPU.ChartType = SeriesChartType.Line;
            seriesGPU.Color = Color.FromArgb(255, 136, 48, 78);

            InitializeComponent();
            computer.Open();

            // Create a timer to update the label every second
            Timer timer = new Timer();
            timer.Interval = 2000;
            timer.Tick += Timer_Tick;
            timer.Start();

            // if the user forcibly closes the window, the computer still goes off and doesn't run in the background.
            this.FormClosing += new FormClosingEventHandler(form1_FormClosing);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Update the label with the CPU and GPU temperatures
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            // Initialize the strings for the CPU and GPU temperatures
            string cpuTempString = "";
            string gpuTempString = "";
            float cpuTemp = 0.0f;
            float gpuTemp = 0.0f;

            // Loop through the hardware devices
            foreach (IHardware hardware in computer.Hardware)
            {
                // Update the hardware data
                hardware.Update();

                // Check if the hardware is CPU or GPU
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    // Loop through the sensors
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            // store
                            cpuTempString = $"CPU: {sensor.Value.GetValueOrDefault()} °C";
                            cpuTemp = sensor.Value.GetValueOrDefault();
                        }
                    }
                }
                else if (hardware.HardwareType == HardwareType.GpuNvidia ||
                         hardware.HardwareType == HardwareType.GpuAti)
                {
                    // Loop through the sensors
                    foreach (ISensor sensor in hardware.Sensors)
                    {
                        // Check if the sensor is temperature
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            // store
                            gpuTempString = $"GPU: {sensor.Value.GetValueOrDefault()} °C";
                            gpuTemp = sensor.Value.GetValueOrDefault();
                        }
                    }
                }
            }

            // Concatenate the CPU and GPU temperatures and assign to the label
            resultadoCpu.Text = cpuTempString;
            resultadoGpu.Text = gpuTempString;
            seriesCPU.Points.AddY(cpuTemp);
            seriesGPU.Points.AddY(gpuTemp);
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            computer.Close();
            this.Close();
        }

        private void minimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void topPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void label1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void form1_Load(object sender, EventArgs e)
        {
            cpuGraphPanel.Controls.Add(chartCPU);
            gpuGraphPanel.Controls.Add(chartGPU);
        }

        private void form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            computer.Close();
        }

        static void RunAsAdmin()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Verb = "runas"; // This will prompt for elevation
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // The user refused to elevate
                MessageBox.Show(ex.Message);
            }
        }
        static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

    }
}
