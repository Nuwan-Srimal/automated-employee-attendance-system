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
        // GLOBAL (MainWindow.xaml.cs)
        byte[]? tempFingerprintTemplate = null;

        private ESP_Services _espServices; // Shared instance
        HttpClient client => _espServices.client;
        string espBaseUrl => _espServices.espBaseUrl;
        public Action<string>? OnStatusChanged;
        
        public EmployeeWindow()
        {
            InitializeComponent();
            Status = this.FindName("Status") as TextBlock;
            
            _espServices = new ESP_Services();
            _espServices.OnStatusChanged += (msg) => OnStatusChanged?.Invoke(msg);
            
            
            Loaded += LoadingWindow_Loaded;
            
            // Initialize ESP connection
            _ = InitializeESP();
        }

        private async Task InitializeESP()
        {
            await _espServices.DetectESP();
            await LoadEmployees();
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
        private async void ScanFingerprint_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(espBaseUrl))
            {
                CustomMessageBox.Show("ESP not connected");
                return;
            }

            CustomMessageBox.Show("Place finger on sensor...");

            try
            {
                var res = await client.GetAsync($"{espBaseUrl}/scanFingerprint");

                if (!res.IsSuccessStatusCode)
                {
                    CustomMessageBox.Show("Fingerprint scan failed");
                    return;
                }

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                // 🔴 CHECK if template exists
                if (doc.RootElement.TryGetProperty("template", out JsonElement templateElement))
                {
                    string base64 = templateElement.GetString();
                    tempFingerprintTemplate = Convert.FromBase64String(base64);
                    CustomMessageBox.Show("Fingerprint captured (TEMP)");
                }
                else if (doc.RootElement.TryGetProperty("finger_id", out JsonElement fingerIdElement))
                {
                    // 🟡 FALLBACK: Use finger_id as temporary identifier
                    int fingerId = fingerIdElement.GetInt32();

                    // Store finger_id as bytes (temporary workaround)
                    tempFingerprintTemplate = BitConverter.GetBytes(fingerId);

                    CustomMessageBox.Show($"Fingerprint ID: {fingerId} (TEMP)");
                }
                else
                {
                    CustomMessageBox.Show("Invalid response from ESP");
                    SystemServices.Log($"ESP Response: {json}");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}");
                SystemServices.Log($"Fingerprint scan error: {ex.Message}");
            }
        }


        private async void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (tempFingerprintTemplate == null)
            {
                CustomMessageBox.Show("Scan fingerprint first");
                return;
            }

            var emp = new
            {
                id = EmpId.Text,
                name = EmpName.Text,
                nic = EmpNIC.Text,
                fingerprint = Convert.ToBase64String(tempFingerprintTemplate)
            };

            var json = JsonSerializer.Serialize(emp);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var res = await client.PostAsync($"{espBaseUrl}/addEmployee", content);

            if (!res.IsSuccessStatusCode)
            {
                CustomMessageBox.Show("SD Card Save Failed");
                return;
            }

            CustomMessageBox.Show("Employee + Fingerprint Saved");

            // CLEAR
            EmpId.Text = EmpName.Text = EmpNIC.Text = "";
            tempFingerprintTemplate = null;

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




        public async Task LoadEmployees()
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
