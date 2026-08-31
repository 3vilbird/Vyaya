Yes, you can absolutely build a serverless, offline peer-to-peer (P2P) app for Android using .NET MAUI without any internet connection. The key is to use Android’s native radios—Wi‑Fi Direct, Wi‑Fi Aware (NAN), Bluetooth/BLE, or a local hotspot—to create a direct link between devices, then run your own lightweight protocol on top. [anexplorer](https://anexplorer.io/transfer/share-without-internet)

## Core options for offline P2P on Android

| Approach | How it works | Pros | Cons / caveats |
|---|---|---|---|
| **Wi‑Fi Direct (Wi‑Fi P2P)** | Devices form a direct Wi‑Fi link without a router; one becomes group owner (acts like an AP). | High throughput (20–50 MB/s), no internet needed, widely supported. | Requires Wi‑Fi hardware & permissions; group-owner negotiation; connection setup UX.  [anexplorer](https://anexplorer.io/transfer/share-without-internet) |
| **Wi‑Fi Aware (NAN)** | Neighbor Awareness Networking lets devices discover/connect directly on Wi‑Fi without AP. | Designed for service discovery + P2P; good for multi-hop/mesh patterns. | Android 8+ (better on 9+); fewer real-world examples; API complexity.  [esp32](https://esp32.com/viewtopic.php?t=47662) |
| **Bluetooth / BLE** | Classic BT for data channels; BLE for low-bandwidth control/signaling. | Works with Wi‑Fi off; good fallback. | Low bandwidth; higher latency; pairing UX.  [igniscor](https://www.igniscor.com/ru/post/bluetooth-in-maui-app) |
| **Local hotspot (one device as “server”)** | One phone enables Mobile Hotspot (no internet needed), others join; you run a local TCP/UDP server on the host. | Simplest mental model (client–server over LAN); easy to implement with .NET sockets. | Host drains battery; only as many clients as hotspot allows; requires hotspot permissions.  [anexplorer](https://anexplorer.io/transfer/share-without-internet) |
| **Google Nearby Connections** | Google’s API abstracts BT + Wi‑Fi P2P to create direct links. | Easy discovery + connections; handles transport selection. | Google Play Services dependency; Android-only; less control over topology.  [offlineprotocol](https://www.offlineprotocol.com/glossary/nearby-connections) |

For your “3 users, one becomes server” pattern, the **local hotspot + local TCP server** or **Wi‑Fi Direct with one group owner** are the most straightforward. [anexplorer](https://anexplorer.io/transfer/share-without-internet)

## Recommended architecture for your MAUI app

### Option A: Hotspot + local TCP server (simplest)
- One device enables **Mobile Hotspot** (no SIM/internet required). Others connect to that SSID. [anexplorer](https://anexplorer.io/transfer/share-without-internet)
- The hotspot device starts a **TCP listener** (e.g., `TcpListener` in .NET) on a fixed port.
- Other devices connect to the host’s **gateway IP** (commonly `192.168.43.1` or similar) and your chosen port.
- You can implement:
  - Simple request/response messaging
  - Pub/sub style chat
  - File transfer over the same socket or a secondary port
- In MAUI, use `System.Net.Sockets` plus MAUI’s `Connectivity` API to check local network status. [learn.microsoft](https://learn.microsoft.com/nl-nl/dotnet/maui/platform-integration/communication/networking?view=net-maui-10.0)

This matches your “one becomes server” idea exactly and avoids complex P2P discovery.

### Option B: Wi‑Fi Direct (true P2P, no hotspot)
- Use Android’s **Wi‑Fi P2P** APIs to discover peers and form a group. One device becomes group owner (acts like an AP). [techno-sage.medium](https://techno-sage.medium.com/how-two-or-more-android-phones-talk-without-internet-wi-fi-network-or-bluetooth-4114a9919102)
- Once connected, you get IP addresses and can open **TCP/UDP sockets** between peers.
- For 3 users, you can:
  - Have the group owner act as coordinator (simplest), or
  - Let all peers talk directly once they know each other’s IPs.
- In .NET MAUI, you’ll need **platform-specific code** (Android) to call Wi‑Fi P2P APIs via dependency injection or MAUI’s platform channels, then hand off IPs/ports to shared .NET socket code.

### Option C: Hybrid (BLE for discovery, Wi‑Fi Direct/hotspot for data)
- Use **BLE** to discover nearby devices and exchange connection metadata (e.g., “I can be hotspot” / “I can be group owner”). [igniscor](https://www.igniscor.com/ru/post/bluetooth-in-maui-app)
- Then establish a higher-bandwidth link via Wi‑Fi Direct or hotspot for actual messaging/file transfer.
- This is how many offline mesh apps work under the hood. [offlineprotocol](https://www.offlineprotocol.com/glossary/nearby-connections)

## .NET MAUI specifics you’ll need

- **Permissions (Android)**:  
  - Hotspot: `CHANGE_WIFI_STATE`, `ACCESS_WIFI_STATE`, `NEARBY_WIFI_DEVICES` (Android 13+), plus runtime prompts. [lifetips.alibaba](https://lifetips.alibaba.com/tech-efficiency/superbeam-shares-files-between-android-devices-without)
  - Wi‑Fi P2P: `CHANGE_WIFI_STATE`, `ACCESS_WIFI_STATE`, `ACCESS_FINE_LOCATION` (for scanning), and on newer Android, nearby device permissions. [techno-sage.medium](https://techno-sage.medium.com/how-two-or-more-android-phones-talk-without-internet-wi-fi-network-or-bluetooth-4114a9919102)
  - BLE (if used): `BLUETOOTH`, `BLUETOOTH_ADMIN`, `BLUETOOTH_CONNECT`, `ACCESS_FINE_LOCATION`. [igniscor](https://www.igniscor.com/ru/post/bluetooth-in-maui-app)
- **MAUI platform integration**:  
  - Use `Connectivity.Current.NetworkAccess` to detect local network vs internet. [learn.microsoft](https://learn.microsoft.com/nl-nl/dotnet/maui/platform-integration/communication/networking?view=net-maui-10.0)
  - Implement Android-specific services for Wi‑Fi P2P / hotspot control via `IPlatformApplication` or dependency injection, then expose events (peers discovered, connected, IPs) to your shared MAUI UI.
- **Networking**:  
  - For local TCP: `System.Net.Sockets.TcpListener` / `TcpClient` works fine on LAN/hotspot/Wi‑Fi Direct once you have IPs.  
  - Design a simple application-layer protocol (e.g., length-prefixed JSON messages) for chat, presence, and file transfer metadata.

## Practical pattern for 3 users

- **Startup**:
  - User A: “Host session” → enables hotspot, starts TCP listener on port e.g. 8888, shows session code.
  - Users B & C: “Join session” → connect to hotspot SSID, then connect to host IP:8888.
- **Messaging**:
  - Host maintains a list of connected clients and relays messages (simplest).
  - Or, after join, host shares peer IPs so B↔C can talk directly (more P2P-like).
- **No internet**: All traffic stays on the local link; mobile data can be off. [anexplorer](https://anexplorer.io/transfer/share-without-internet)

## Existing references you can study

- Offline mesh/file-transfer apps using Wi‑Fi Direct/hotspot (e.g., SuperBeam, WiBChat Mesh, WD Cable) show the UX and constraints. [lifetips.alibaba](https://lifetips.alibaba.com/tech-efficiency/superbeam-shares-files-between-android-devices-without)
- Google’s **Nearby Connections** docs illustrate discovery + P2P patterns you can mimic even if you don’t use the library. [offlineprotocol](https://www.offlineprotocol.com/glossary/nearby-connections)
- BLE-based offline games in MAUI demonstrate how to structure platform-specific Bluetooth code with shared .NET logic. [igniscor](https://www.igniscor.com/ru/post/bluetooth-in-maui-app)

If you tell me your target Android versions and whether you prefer “one host” or “full mesh”, I can sketch a concrete MAUI project structure (interfaces, Android services, and a minimal TCP protocol) tailored to your setup.
