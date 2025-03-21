using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace RaspiLDR2MQTT;

using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;

public class MQTTService : IAsyncDisposable
{
    private readonly ILogger<MQTTService> logger;

    private readonly IConfiguration Configuration;

    private readonly IMqttClient mqttClient;

    private MqttClientOptions mqttClientOptions;

    private string TOPIC;

    public MQTTService(ILogger<MQTTService> logger, IConfiguration configuration)
    {
        this.logger = logger;
        var mqttFactory = new MqttFactory();
        this.mqttClient = mqttFactory.CreateMqttClient();

        this.Configuration = configuration;
        var mqttConfig = configuration.GetSection("MQTT");

        this.TOPIC = mqttConfig.GetValue<string>("Topic");

        this.mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttConfig.GetValue<string>("Server"), mqttConfig.GetValue<int>("Port"))
            .WithClientId(mqttConfig.GetValue<string>("ClientId"))
            .WithCredentials(mqttConfig.GetValue<string>("User"), mqttConfig.GetValue<string>("Password"))
            .Build();
    }

    private async Task Connect()
    {
        await this.mqttClient.ConnectAsync(this.mqttClientOptions, CancellationToken.None);

        if (this.Configuration.GetSection("AutoDiscovery").GetValue<bool>("Enabled"))
        {
            // Always send autodiscovery on (re-)connect
            await this.SendAutodiscovery();
        }
    }

    private async Task Disconnect()
    {
        await this.mqttClient.DisconnectAsync();
    }

    public async Task SendToHassAsync(string payload)
    {
        if (!this.mqttClient.IsConnected)
        {
            this.logger.LogInformation("Not connected, trying to connect to mqtt...");
            await this.Connect();
            this.logger.LogInformation("Connected to mqtt");
        }

        var applicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic(this.TOPIC)
            .WithPayload(payload)
            .Build();

        await this.mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

        this.logger.LogInformation("Published paylout: {Payload}", payload);
    }

    private async Task SendAutodiscovery()
    {
        var autoDiscoveryConfig = this.Configuration.GetSection("AutoDiscovery");

        var configPackage = this.GetSectionAsObject(autoDiscoveryConfig.GetSection("ConfigPackage"));

        var json = JsonSerializer.Serialize(configPackage);
        await this.mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
            .WithTopic(autoDiscoveryConfig.GetSection("Topic").Value)
            .WithPayload(json)
            .WithRetainFlag(true)
            .Build());
        this.logger.LogInformation("Published autodiscovery: {Json}", json);
    }

    object GetSectionAsObject(IConfigurationSection section)
    {
        var children = section.GetChildren();

        if (!children.Any())
            return section.Value;

        // Handle arrays
        if (children.All(c => int.TryParse(c.Key, out _)))
        {
            return children.Select(this.GetSectionAsObject).ToList();
        }

        // Handle nested objects
        var dict = new Dictionary<string, object>();
        foreach (var child in children)
        {
            dict[child.Key] = this.GetSectionAsObject(child);
        }

        return dict;
    }

    public async ValueTask DisposeAsync()
    {
        await this.Disconnect();
    }
}