using System;
using System.Collections.Generic;
using System.Linq;
<<<<<<< HEAD
=======
using System.Net.NetworkInformation;
>>>>>>> ui
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
<<<<<<< HEAD
=======
using Automated_Employee_Attendance_System.Services;
>>>>>>> ui

namespace Automated_Employee_Attendance_System
{
    /// <summary>
    /// Interaction logic for DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : UserControl
    {
<<<<<<< HEAD
        public DashboardWindow()
        {
            InitializeComponent();
=======
        private readonly ESP_Services _esp = new();

        public DashboardWindow()
        {
            InitializeComponent();
            Loaded += async (_, _) => await _esp.DetectESP();
            _esp.OnStatusChanged = SetStatus;
        }

        public void SetStatus(string text)
        {
            Dispatcher.Invoke(() => Status.Text = text);
>>>>>>> ui
        }
    }
}
