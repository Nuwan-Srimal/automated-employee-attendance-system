using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Automated_Employee_Attendance_System.Services;
using System.Windows;
using System.Windows.Controls;
using Automated_Employee_Attendance_System.Models; // Add this line if Employee is in Models namespace
using System.Windows.Input;
using System.Text.RegularExpressions;
using XamlAnimatedGif;


namespace Automated_Employee_Attendance_System
{
    /// <summary>
    /// Interaction logic for EmployeeWindow.xaml
    /// </summary>
    public partial class EmployeeWindow : UserControl
    {
        private TextBlock Status;

        HttpClient client = new ESP_Services().client;
        string espBaseUrl = new ESP_Services().espBaseUrl;
        public Action<string>? OnStatusChanged;
        public EmployeeWindow()
        {

            InitializeComponent();
            Status = this.FindName("Status") as TextBlock;
            Loaded += LoadingWindow_Loaded; // window render වෙන විට
            _ = LoadEmployees(); // Fire and forget, since constructors can't be async
        }

        private async void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // GIF load background thread
            await Task.Run(() =>
            {
                string baseFolder = AppDomain.CurrentDomain.BaseDirectory;
                string gifPath = System.IO.Path.Combine(baseFolder, "UI", "Fingerprint_biometric_scan.gif");
                var gifUri = new Uri(gifPath, UriKind.Absolute);

                Dispatcher.Invoke(() =>
                {
                    AnimationBehavior.SetSourceUri(MyGifImage, gifUri);
                    AnimationBehavior.SetRepeatBehavior(MyGifImage, System.Windows.Media.Animation.RepeatBehavior.Forever);
                });
            });

          

         
        }


        private async void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(espBaseUrl))
            {
                CustomMessageBox.Show("ESP not connected");
                SystemServices.Log("Fail To Add Employee (ESP Not Connected)");                
                return;
            }

            var emp = new
            {
                id = EmpId.Text,
                name = EmpName.Text,
                nic = EmpNIC.Text
            };

            var json = JsonSerializer.Serialize(emp);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await client.PostAsync($"{espBaseUrl}/addEmployee", content);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                CustomMessageBox.Show("SD Card Save Failed");
                SystemServices.Log("SD Card Save Failed");
                return;
            }

            using var doc = JsonDocument.Parse(body);
            string status = doc.RootElement.GetProperty("status").GetString();

            if (status == "ok")
            {
                CustomMessageBox.Show("SD Card Save Complete");
                EmpId.Text = "";
                EmpName.Text = "";
                EmpNIC.Text = "";
                SystemServices.Log("SD Card Save Complete");
                EmpId.Text = "";
                EmpName.Text = "";
                EmpNIC.Text = "";
            }

            else
                CustomMessageBox.Show("SD Card Error");
                SystemServices.Log("SD Card Error");

            await LoadEmployees();

        }



        #region EPS Services 

        private async void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            var emp = (sender as Button)?.CommandParameter as Employee;
            if (emp == null)
            {
                CustomMessageBox.Show("No employee selected");
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete employee {emp.name}?",
                "Confirm Delete",
                MessageBoxButton.YesNo);

            if (confirm != MessageBoxResult.Yes)
                return;

            var json = JsonSerializer.Serialize(new { id = emp.id.Trim() });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await client.PostAsync($"{espBaseUrl}/deleteEmployee", content);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                OnStatusChanged?.Invoke(body);
                return;
            }

            using var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.GetProperty("status").GetString();

            if (status == "ok")
            {
                CustomMessageBox.Show("Employee deleted");
                SystemServices.Log($"Delete employee {emp.name}");

                await LoadEmployees();
            }
            else
            {
                OnStatusChanged?.Invoke($"Delete failed: {status}");

                SystemServices.Log($"Delete failed: {status}");
            }
        }




        async Task LoadEmployees()
        {
            if (string.IsNullOrEmpty(espBaseUrl))
                return;

            var res = await client.GetAsync($"{espBaseUrl}/employees");
            if (!res.IsSuccessStatusCode)
            {
                CustomMessageBox.Show("Failed to load employees");
                SystemServices.Log($"Failed to load employees");
                return;
            }

            var json = await res.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<List<Employee>>(json);

            SystemServices.Log($"Load Employee For ESP");


            EmployeeGrid.ItemsSource = list;
        }




        #endregion




    private void EmpId_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // numbers witharai allow
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");

            DataObject.AddPastingHandler(EmpId, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(DataFormats.Text))
                {
                    string text = e.DataObject.GetData(DataFormats.Text) as string;
                    if (!Regex.IsMatch(text ?? "", @"^\d+$"))
                        e.CancelCommand();
                }
                else
                    e.CancelCommand();
            });
        }

    private void EmpId_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // paste block karanna
        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            e.Handled = true;
    }

}


}
