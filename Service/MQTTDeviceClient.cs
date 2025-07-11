using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MQTT
{
    public class MQTTDeviceClient
    {
        private TcpClient client;
        private SslStream sslStream;
        private string clientId;
        private string topic;
        private string broker;
        private int port;

        public event Action<double, double, double> OnSensorDataReceived;

        public MQTTDeviceClient(string brokerAddress, int brokerPort, string topic, string clientId)
        {
            this.broker = brokerAddress;
            this.port = brokerPort;
            this.topic = topic;
            this.clientId = clientId;
        }

        public async Task ConnectAsync(string username = null, string password = null)
        {
            client = new TcpClient();
            await client.ConnectAsync(broker, port);

            var networkStream = client.GetStream();

            sslStream = new SslStream(networkStream, false,
                (sender, certificate, chain, sslPolicyErrors) => true);

            await sslStream.AuthenticateAsClientAsync(broker);

            var connectPacket = Packet.Connect(clientId, username, password, 60);
            await SendPacketAsync(connectPacket);

            await ReadResponseAsync();

            var subscribePacket = Packet.Subscribe(topic, 0);
            await SendPacketAsync(subscribePacket);

            await ReadResponseAsync();

            _ = Task.Run(ListenForMessagesAsync);
        }

        public async Task DisconnectAsync()
        {
            var disconnectPacket = Packet.Disconnect();
            await SendPacketAsync(disconnectPacket);

            sslStream.Close();
            client.Close();
        }

        private async Task SendPacketAsync(Packet packet)
        {
            var data = packet.ToBytes();
            await sslStream.WriteAsync(data, 0, data.Length);
            await sslStream.FlushAsync();
        }

        private async Task ReadResponseAsync()
        {
            byte[] header = new byte[2];
            await ReadExactAsync(sslStream, header, 0, 2);
            int length = header[1];
            byte[] payload = new byte[length];
            await ReadExactAsync(sslStream, payload, 0, length);
        }

        private async Task ReadExactAsync(SslStream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    throw new Exception("Stream closed unexpectedly.");
                totalRead += bytesRead;
            }
        }

        private async Task ListenForMessagesAsync()
        {
            try
            {
                while (true)
                {
                    byte[] fixedHeader = new byte[2];
                    await ReadExactAsync(sslStream, fixedHeader, 0, 2);

                    byte packetType = (byte)(fixedHeader[0] >> 4);
                    int remainingLength = fixedHeader[1];

                    byte[] payload = new byte[remainingLength];
                    await ReadExactAsync(sslStream, payload, 0, remainingLength);

                    if (packetType == 3) // PUBLISH
                    {
                        int topicLength = (payload[0] << 8) + payload[1];
                        string topicReceived = Encoding.UTF8.GetString(payload, 2, topicLength);

                        int messageStartIndex = 2 + topicLength;
                        int messageLength = remainingLength - messageStartIndex;

                        string messagePayload = Encoding.UTF8.GetString(payload, messageStartIndex, messageLength);

                        Console.WriteLine($"[MQTT] Message received on topic '{topicReceived}': {messagePayload}");

                        if (!string.IsNullOrWhiteSpace(messagePayload) && messagePayload.Contains("{"))
                        {
                            try
                            {
                                var json = JsonDocument.Parse(messagePayload);

                                double temp = json.RootElement.GetProperty("Temp").GetDouble();
                                double hum = json.RootElement.GetProperty("humidity").GetDouble();
                                double waterPercent = 0.0;
                                if (json.RootElement.TryGetProperty("water_percent", out var waterProp))
                                {
                                    waterPercent = waterProp.GetDouble();
                                }

                                OnSensorDataReceived?.Invoke(temp, hum, waterPercent);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Lỗi parse JSON: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[MQTT] Packet type {packetType} received (ignored).");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi nhận message MQTT: {ex.Message}");
            }
        }

        // Gửi lệnh điều khiển với đủ 5 trường
        public async Task SendCommandAsync(string cmd, string value, string action = "", string time = "", string status = "")
        {
            var command = new
            {
                cmd = cmd.ToUpper(),
                value = value.ToLower(),
                action = action,
                time = time,
                status = status
            };
            string json = JsonSerializer.Serialize(command);
            var publishPacket = Packet.Publish(topic, Encoding.UTF8.GetBytes(json), 0, false);
            await SendPacketAsync(publishPacket);
        }
    }
}