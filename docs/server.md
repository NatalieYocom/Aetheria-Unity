## ⚙️ Aetheria Dedicated Server Overview

The **Aetheria Dedicated Server** operates as a standalone, headless cross-platform application built on .NET. Rather than handling heavy graphics rendering, its primary role is acting as an authority and relay hub for synchronization, security, and moderation across networked VR instances.

---

### 📡 Network Traffic & Transport Relay

The server utilizes **LiteNetLib** for low-latency Reliable UDP socket connections between clients. To ensure safe and efficient networking, direct peer connections are encrypted via **ChaCha20-Poly1305**, supported by integrated NAT hole-punching fallback mechanism. Additionally, high-density traffic is compressed using **LZ4/Deflate** to keep bandwidth footprints minimal during large multi-user lobbies.

---

### 🔄 State Replication & World Authority

Core world state and synchronization are maintained directly by the server. It handles real-time replication of **avatar tracking transforms, full-body IK structures, lip-sync, and expression blendshapes**. Beyond players, it synchronizes **dynamic prop transforms, pickup physics, seat anchors, and ownership handoffs**. Communication relies on the server to route spatial **Opus 48kHz audio streams**, manage text chat history, display typing indicators, and trigger server-wide non-spatialized broadcast announcements.

---

### 🛡️ Moderation & Server Administration

Administrative control is tied directly to **UUID-based permission groups** assigned to player accounts. Administrators can enforce server-side **kicks, temporary bans, block lists, and mutes**. On initial launch, an interactive CLI boot setup wizard allows hosts to configure network ports, instance constraints, and server properties via command-line arguments.

---

### 📊 Diagnostic & Telemetry Capture

To assist developers in debugging multi-user environments, the server includes built-in diagnostic logging. Connected clients can send **compressed error logs and exception traces** back to the host, allowing world creators to isolate bugs across different hardware setups.

---

### ⚠️ System Boundaries & Custom Extensions

Out of the box, the server manages active session instances rather than persistent database storage. Supporting **persistent player databases** (e.g., player inventories, economy, progression) or **multi-server master matchmaking** requires integrating custom web services or API endpoints into your backend pipeline.
