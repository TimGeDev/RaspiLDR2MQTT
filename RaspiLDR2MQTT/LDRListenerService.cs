namespace RaspiLDR2MQTT;

using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class LdrService(ILogger<LdrService> logger, IConfiguration configuration, MQTTService mqttService)
    : IHostedService
{
    private GpioController GPIO;

    private int Pin;

    private CancellationTokenSource Cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting LDR GPIO Service");

        this.Pin = configuration.GetValue<int>("Gpio:LdrPin");
        this.GPIO = new GpioController();
        this.GPIO.OpenPin(this.Pin, PinMode.InputPullUp);

        this.Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task.Run(() => this.MonitorLoop(this.Cts.Token), cancellationToken);

        return Task.CompletedTask;
    }

    private async Task MonitorLoop(CancellationToken token)
    {
        logger.LogInformation("Monitoring GPIO pin {Pin}", this.Pin);

        bool lastState = false;

        while (!token.IsCancellationRequested)
        {
            bool currentState = this.GPIO.Read(this.Pin) == PinValue.Low;

            if (currentState != lastState)
            {
                lastState = currentState;
                logger.LogInformation("Change to {State} detected at {Time}", currentState, DateTime.Now);
                await mqttService.SendToHassAsync(lastState ? "ON" : "OFF");
            }

            await Task.Delay(250, token); // Poll interval
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping GPIO Service...");

        this.Cts?.Cancel();

        if (this.GPIO != null && this.GPIO.IsPinOpen(this.Pin))
        {
            this.GPIO.ClosePin(this.Pin);
            this.GPIO.Dispose();
        }

        return Task.CompletedTask;
    }
}