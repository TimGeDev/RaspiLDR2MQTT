using Microsoft.Extensions.Configuration;

namespace RaspiLDR2MQTT;

using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;

public class MQTTService : IDisposable
{
    private readonly ILogger<MQTTService> logger;

    private readonly IMqttClient mqttClient;

    private MqttClientOptions mqttClientOptions;

    private string TOPIC;

    public MQTTService(ILogger<MQTTService> logger, IConfiguration configuration)
    {
        this.logger = logger;
        var mqttFactory = new MqttFactory();
        this.mqttClient = mqttFactory.CreateMqttClient();

        var mqttConfig = configuration.GetSection("MQTT");

        this.TOPIC = mqttConfig.GetValue<string>("Topic");

        this.mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttConfig.GetValue<string>("Server"), mqttConfig.GetValue<int>("Port"))
            .WithClientId(mqttConfig.GetValue<string>("ClientId"))
            .WithCredentials(mqttConfig.GetValue<string>("User"), mqttConfig.GetValue<string>("Value"))
            .Build();
    }

    private void Connect()
    {
        this.mqttClient.ConnectAsync(this.mqttClientOptions, CancellationToken.None);
    }

    private void Disconnect()
    {
        this.mqttClient.DisconnectAsync();
    }

    public async Task SendToHassAsync(string payload)
    {
        if (!this.mqttClient.IsConnected)
        {
            this.Connect();
        }

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(this.TOPIC)
            .WithPayload(payload)
            .Build();

        await this.mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        this.logger.LogInformation("Published: " + payload);
    }

    public void Dispose()
    {
        this.Disconnect();
    }
}