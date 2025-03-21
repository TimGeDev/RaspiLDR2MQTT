# 🔔 Raspberry Pi LDR Blink Detector with MQTT (C# / Mono)

This project detects light pulses (e.g. from a blinking doorbell LED) using a **light-dependent resistor (LDR)** connected to a **GPIO pin** on a Raspberry Pi.  
When a light pulse is detected, a message is sent via **MQTT**.

It runs as a background service using **C# with Mono**, as .NET (Core) isn't supported on Raspberry Pi 1 / Zero (armv6).

---

## ⚙️ Requirements

- Raspberry Pi (Zero, Zero 2 W, 3, 4, etc.)
- Raspbian / Raspberry Pi OS (Debian-based)
- Mono (runtime for C#)
- LDR (light-dependent resistor)
- MQTT broker (local or remote)

---

## 📦 Installation (Mono Runtime)

Install Mono:

```bash
sudo apt update
sudo apt install mono-complete
```

> `mono-complete` includes the full Mono runtime and development tools.

---

## 🔌 Hardware Wiring

| LDR Pin | Raspberry Pi Pin        |
|---------|--------------------------|
| One side | GPIO18 (BCM) – Physical Pin 12 |
| Other side | GND (e.g. Pin 6)          |

You can use the internal pull-up resistor in software — no extra hardware required.

---

## 📁 Configuration (`appsettings.json`)

```json
{
  "Gpio": {
    "LdrPin": 18
  },
  "MQTT": {
    "Server": "mqtt.example.com",
    "Port": 1883,
    "ClientId": "ldr-sensor-01",
    "User": "myMqttUser",
    "Value": "myMqttPassword",
    "Topic": "sensors/ldr/blink"
  }
}
```

---

## 🧠 What It Does

- Initializes a GPIO pin as input with pull-up
- Monitors the pin for a light-triggered LOW signal
- When light is detected, sends an MQTT message to the configured topic

---

## 🚀 How to Run (via Mono)

1. Build your C# project targeting `.NET Framework 4.8` (or similar)
2. Copy it to the Raspberry Pi
3. Run the app:

```bash
sudo mono MyApp.exe
```

> `sudo` is required for GPIO access.

---

## 📌 Notes

- Make sure your user has access to `/dev/gpiomem` or run with `sudo`
- Debounce logic is included to avoid spamming messages
- You can expand this to detect longer pulses or multiple brightness levels

---

## 📄 License

MIT – free to use and modify.
