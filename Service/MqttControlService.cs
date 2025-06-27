using System;
using System.Text.Json;
using System.Threading.Tasks;
using MQTT;

namespace LoginApp.Service
{
    public class MQTTControlService
    {
        private readonly MQTTDeviceClient mqttClient;
        private readonly string controlTopic;

        public event Action<string>? OnCommandSent;
        public event Action<string>? OnError;

        public bool IsConnected { get; private set; } = false;

        public MQTTControlService(
            string userId = "1",
            string brokerAddress = "broker.emqx.io",
            int brokerPort = 8883,
            string clientId = "maui-control-client")
        {
            controlTopic = $"{Globals.GlobalUserId}/device";

            mqttClient = new MQTTDeviceClient(
                brokerAddress: brokerAddress,
                brokerPort: brokerPort,
                topic: controlTopic,
                clientId: clientId
            );
        }

        public async Task StartAsync(string? username = null, string? password = null)
        {
            try
            {
                await mqttClient.ConnectAsync(username, password);
                IsConnected = true;
                Console.WriteLine("[MQTTControlService] Đã kết nối tới MQTT broker.");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                OnError?.Invoke($"Lỗi khi kết nối MQTT: {ex.Message}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (IsConnected)
                {
                    await mqttClient.DisconnectAsync();
                    IsConnected = false;
                    Console.WriteLine("[MQTTControlService] Đã ngắt kết nối MQTT broker.");
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Lỗi khi ngắt kết nối MQTT: {ex.Message}");
            }
        }

        // Gửi lệnh đến thiết bị với đủ 5 trường
        public async Task SendDeviceCommandAsync(string cmd, bool state, string action = "", string time = "", string status = "")
        {
            if (!IsConnected)
                throw new InvalidOperationException("Chưa kết nối MQTT broker.");

            try
            {
                string value = state ? "on" : "off";
                await mqttClient.SendCommandAsync(cmd.ToUpper(), value, action, time, status);

                string json = JsonSerializer.Serialize(new { cmd = cmd.ToUpper(), value = value, action, time, status });
                Console.WriteLine($"[MQTTControlService] Đã gửi lệnh: {json}");
                OnCommandSent?.Invoke(json);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Lỗi khi gửi lệnh MQTT: {ex.Message}");
            }
        }
    }
}