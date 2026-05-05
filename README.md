# BlindHunter

> An asymmetric horror multiplayer game built in s&box, inspired by Lurking (DigiPen 2013).
> The Killer is blind. The Survivors must escape.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Status](https://img.shields.io/badge/status-in%20development-orange.svg)
![Engine](https://img.shields.io/badge/engine-s%26box-red.svg)

---

## 🎮 Concept

BlindHunter is an **asymmetric multiplayer horror** game where one player controls a **blind Killer** who can only "see" the world through sound waves, while the other players are **Survivors** trying to escape by completing quests.

### The Killer
- **Completely blind by default** — black screen
- Each sound emitted by survivors creates a 3D wave that propagates through the world
- When the wave hits an object, only its edges are revealed in white
- The Killer has to listen and "feel" the world through echolocation

### The Survivors
- See the world normally with limited flashlight visibility
- Must complete a chain of quests to escape:
  1. Power up generators (multi-step quest)
  2. Find the Keycard
  3. Use the Keycard on the door reader
  4. Escape through the unlocked exit
- **Make noise = get spotted.** Crouch and walk to stay invisible.
- Throw stones to create distractions / fake noises

---

## 🛠️ Technical features

### Custom HLSL post-process shader
- **Depth-based edge detection** (Sobel filter on linear depth, ignores texture noise)
- **3D wave propagation** with up to 8 simultaneous echo sources
- **Reveal mask** — edges only visible when touched by a sound wave
- **Visible ring** — the wave front itself is drawn on surfaces (Lurking-style)
- **Adaptive threshold** for cleaner edge detection at varying distances
- **Exponential distance attenuation** — sounds don't pass through distant walls

### Networked multiplayer
- Built on s&box networking (host-authoritative)
- Synced state for player roles, alive status, inventory, flashlight, eye rotation
- RPC-based events for noise emission, attacks, deaths
- Auto-cleanup of expired noise entries

### Gameplay systems
- **Inventory** with 3 slots (Stone × N, Keycard)
- **Flashlight** with view-model + world-model pattern (FPS-style first-person + visible to others)
- **Stamina** for survivors (running) and killer (attacks)
- **Multi-step quest groups** (e.g. "activate 3 generators to validate the quest")
- **Spectator mode** (dead survivors only watch other survivors, not the killer)
- **Heartbeat HUD** indicating killer proximity

---

## 🎨 Visual showcase

*(screenshots coming soon)*

The custom Lurking-style shader makes BlindHunter visually unique on sbox.game:
- White edges on pure black background
- Visible echo rings sliding across walls and floor
- Wave propagation creates suspense and tactical depth

---

## 🚀 Run the project

### Requirements
- Windows 10/11
- Steam account
- s&box installed (via Steam, currently in development)

### Setup
```bash
git clone https://github.com/klibato/BlindHunter.git
cd BlindHunter
```

Then in s&box:
1. Open the editor
2. File → Open Project → select `blindhunter.sbproj`
3. Open `Assets/scenes/main.scene`
4. Click Play

### Controls

**Survivor**
- WASD : Move
- Shift : Run (consumes stamina)
- Ctrl : Crouch (silent movement)
- E : Interact / Pickup
- F : Toggle flashlight
- 1/2/3 : Switch inventory slot
- Mouse Wheel : Cycle inventory
- Click : Use active item (throw stone, use keycard)

**Killer**
- WASD : Move
- Click : Attack (consumes stamina)

---

## 📁 Project structure

```
BlindHunter/
├── Assets/
│   ├── prefabs/
│   │   ├── Player.prefab
│   │   ├── Stone.prefab
│   │   └── Generic_Interactable.prefab
│   ├── scenes/
│   │   ├── mainmenu.scene
│   │   └── main.scene
│   ├── shaders/
│   │   ├── pp_test_white.shader (debug)
│   │   ├── pp_grayscale.shader (debug)
│   │   └── pp_edge_detect.shader (Lurking shader, the main one)
│   └── sounds/
│       └── (gameplay audio events)
├── Code/
│   ├── Game/
│   │   ├── GameManager.cs
│   │   ├── GameStateManager.cs
│   │   ├── QuestManager.cs
│   │   └── SpawnPoint.cs
│   ├── Player/
│   │   ├── PlayerSetup.cs
│   │   ├── PlayerInteractor.cs
│   │   ├── PlayerInventory.cs
│   │   ├── PlayerStamina.cs
│   │   ├── PlayerRole.cs
│   │   ├── Flashlight.cs
│   │   ├── KillerAttack.cs
│   │   ├── KillerVisionEffect.cs
│   │   └── SpectatorCamera.cs
│   ├── Items/
│   │   ├── ItemType.cs
│   │   ├── Pickable.cs
│   │   └── KeycardReader.cs
│   ├── World/
│   │   ├── Interactable.cs
│   │   ├── QuestGroup.cs
│   │   ├── ExitDoor.cs
│   │   ├── NoiseVisualizer.cs
│   │   └── ThrowableTracker.cs
│   └── UI/
│       ├── RoleDisplay.razor + .scss
│       ├── InteractionPrompt.razor + .scss
│       ├── QuestProgress.razor + .scss
│       ├── GameOverScreen.razor + .scss
│       ├── InventoryUI.razor + .scss
│       ├── StaminaBar.razor + .scss
│       ├── HeartbeatHUD.razor + .scss
│       ├── SpectatorHUD.razor + .scss
│       ├── MainMenuUI.razor + .scss
│       ├── PauseMenu.razor + .scss
│       └── Scoreboard.razor + .scss
└── blindhunter.sbproj
```

---

## 🗺️ Roadmap

- [x] M1-8 : Core gameplay (movement, roles, networking, basic interactions)
- [x] M9 : Inventory system + Keycard quest + Flashlight (view+world model)
- [x] M10 : Custom Lurking shader (edge detection + wave propagation)
- [x] M11.1 : Spawn points dedicated by role
- [ ] M11.2 : Spectator mode (survivors only)
- [ ] M11.3 : Audio sounds for gameplay events
- [ ] M11.4 : Citizen animations
- [ ] M11.5 : Killer proximity HUD (heartbeat)
- [ ] M12 : Main menu + Lobby system
- [ ] M13 : Pause menu + Scoreboard
- [ ] M14 : Tests + balancing (1 week of playtests)
- [ ] M15 : **Public release on sbox.game**

---

## 🙏 Credits

- **Map** : `despawn/fracture` from sbox.game (used via playfund system)
- **Inspiration** : [Lurking](http://lurking-game.com/) by Dexter Chng (DigiPen Singapore, 2013)
- **Engine** : [s&box](https://sbox.game/) by Facepunch Studios

---

## 📜 License

MIT — see [LICENSE](LICENSE) for details.

---

## 👤 Author

**Hamza** — DevOps engineer turned indie gamedev
- GitHub: [@klibato](https://github.com/klibato)
- Steam: Klibato

Built late at night with way too much coffee. ☕
