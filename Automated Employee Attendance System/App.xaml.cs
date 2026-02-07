using System.ComponentModel;
using System.Configuration;
using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Windows;

namespace Automated_Employee_Attendance_System
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            base.OnStartup(e);
        }


    }

}
