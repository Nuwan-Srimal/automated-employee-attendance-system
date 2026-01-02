using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Automated_Employee_Attendance_System
{
    public partial class DashboardWindow : UserControl
    {
        public SeriesCollection AttendanceSeries { get; set; }

        private SerialPort _serialPort;
        public string[] Labels { get; set; }
        public string SystemStatusText { get; set; }
        public string SystemStatusSubText { get; set; }
        public Brush SystemStatusColor { get; set; }

        public DashboardWindow()
        {
            InitializeComponent();
            SystemStatusText = "No Device Configured";
            SystemStatusSubText = "Connect Arduino to enable real-time sync";
            SystemStatusColor = Brushes.Orange;

            DataContext = this;

            Labels = new[]
            {
        "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"
    };

            AttendanceSeries = new SeriesCollection
{
    new LineSeries
    {
        Title = "Attendance",
        Values = new ChartValues<int>
        {
            15, 20, 30, 25, 35, 20, 28
        },
        StrokeThickness = 3,
        PointGeometry = null,    
        LineSmoothness = 1,
        Stroke = Brushes.DodgerBlue,
        Fill = new SolidColorBrush(Color.FromArgb(40, 30, 144, 255))
    }
};

            DataContext = this;

            //InitSerial();
        }

        private void InitSerial()
        {
            _serialPort = new SerialPort("COM3", 9600); // ⚠️ COM port change
            _serialPort.DataReceived += Serial_DataReceived;
            _serialPort.Open();
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = _serialPort.ReadLine();

            Dispatcher.Invoke(() =>
            {
                if (data.StartsWith("COUNT:"))
                {
                    int count = int.Parse(data.Replace("COUNT:", ""));

                    var values = AttendanceSeries[0].Values as ChartValues<int>;

                    if (values.Count >= 8)
                        values.RemoveAt(0);

                    values.Add(count);
                }
            });
        }
    }
}