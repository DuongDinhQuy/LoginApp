using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using LoginApp.Service;

namespace LoginApp
{
    public partial class MainPage : ContentPage
    {
        private MQTTService _mqttService;

        public MainPage()
        {
            InitializeComponent();
            InitMqtt();
        }

        private async void InitMqtt()
        {
            // Giả sử GlobalUserId được gán sau khi đăng nhập thành công
            _mqttService = new MQTTService(Globals.GlobalUserId);
            _mqttService.SensorDataReceived += MqttService_SensorDataReceived;
            await _mqttService.StartAsync(); // Kết nối MQTT
        }

        private async void MqttService_SensorDataReceived(object? sender, SensorDataEventArgs e)
        {
            // Cập nhật UI từ dữ liệu MQTT
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TemperatureLabel.Text = e.Temperature.HasValue ? e.Temperature.Value.ToString("F1") : "--";
                HumidityLabel.Text = e.Humidity.HasValue ? e.Humidity.Value.ToString("F1") : "--";
                WaterLevelLabel.Text = e.WaterLevel.HasValue ? e.WaterLevel.Value.ToString("F2") : "--";
            });

            // Gửi dữ liệu SensorData lên server
            await PostSensorDataToServer(e);
        }

        private async Task PostSensorDataToServer(SensorDataEventArgs sensor)
        {
            try
            {
                var httpClient = new HttpClient();
                var sensorData = new
                {
                    UserId = Globals.GlobalUserId,
                    Temperature = sensor.Temperature,
                    Humidity = sensor.Humidity,
                    WaterLevel = sensor.WaterLevel,
                    Time = DateTime.UtcNow
                };

                // Log gửi dữ liệu (debug)
                System.Diagnostics.Debug.WriteLine($"[POST] Sending SensorData: UserId={sensorData.UserId}, Temp={sensorData.Temperature}, Humidity={sensorData.Humidity}, Water={sensorData.WaterLevel}, Time={sensorData.Time}");

                var response = await httpClient.PostAsJsonAsync("https://1ab3-2001-ee0-4161-2458-c96a-c69c-38b9-add3.ngrok-free.app/api/SensorData", sensorData);

                if (!response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Application.Current?.MainPage?.DisplayAlert("Lỗi gửi dữ liệu", $"Status: {response.StatusCode}\n{content}", "OK");
                    });
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Application.Current?.MainPage?.DisplayAlert("Lỗi gửi dữ liệu", ex.ToString(), "OK");
                });
            }
        }
    }

    // Định nghĩa EventArgs nếu file MQTTService của bạn chưa có:
    public class SensorDataEventArgs : EventArgs
    {
        public double? Temperature { get; set; }
        public double? Humidity { get; set; }
        public double? WaterLevel { get; set; }
    }
}