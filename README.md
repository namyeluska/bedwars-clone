# Bedwars Clone (OpenTK, C#)

A small Minecraft-inspired Bedwars-style prototype built with OpenTK and .NET.
This project focuses on voxel rendering, basic interaction (break/place), inventory,
first‑person/third‑person camera modes, and simple UI.

> **Note:** There is **no license** for this repository. You can do whatever you want with it.

---

## Gameplay

[![Gameplay](https://img.youtube.com/vi/umZxgjTeuqU/hqdefault.jpg)](https://www.youtube.com/watch?v=umZxgjTeuqU)

---

## Features

- Chunked voxel world rendering
- Block selection, breaking, and placement
- Hotbar UI + item rendering
- First‑person hand + sword rendering
- Third‑person camera modes (F5)
- Player model render with skin texture (3rd person)
- Basic movement, sprint, sneak, jump
- Optional sound system (steps, break, place)

---

## Controls

- **WASD** — Move  
- **Space** — Jump  
- **Left Shift** — Sneak  
- **Left Ctrl** — Sprint  
- **Mouse Move** — Look  
- **Left Click** — Break  
- **Right Click** — Place  
- **1–9** — Hotbar slots  
- **F5** — Camera mode (First → Third Back → Third Front)

---

## Known Issues

- **F5 third‑person POVs are not fully correct** (camera positioning and model behavior are still being tuned).
- Player model animation in third‑person is still work‑in‑progress.

---

## How to Run

### Requirements
- **.NET 8 SDK**
- **OpenGL 3.3+** compatible GPU/driver

### Run
```bash
dotnet run
```

---

## Project Structure

**Core**
- `Program.cs` — Entry point
- `Game.cs` — Main game loop, rendering, input, interaction, camera, UI
- `World.cs` — World/chunk storage, raycast, block access
- `Chunk.cs` — Chunk data + mesh generation
- `Block.cs` — Block types, texture mapping
- `Player.cs` — Movement, physics, collisions, camera
- `Inventory.cs` — Hotbar inventory logic

**Rendering**
- `WorldShader.cs` — 3D world shader (texture array)
- `Shader.cs` — Generic textured shader (UI, items, hand, player model)
- `TextureManager.cs` — Loads textures (blocks, items, UI, skin)

**Audio (optional)**
- `AudioManager.cs` — Loads and plays OGG sounds (steps/break/place)
- `sound_list.txt` — List of expected sound file names

**Assets**
- `Resources/` — Textures, models, sounds
- `Resources/skin.png` — Skin used for third‑person player model
- `Resources/hand_display.json` — Hand transform tuning (first‑person)
- `Resources/Old_Default_1.13.2/` — Minecraft‑style texture pack

---

## Sounds

Sounds are optional.  
Place files under:
```
Resources/sounds/
```
File names expected are listed in `sound_list.txt`.

---

## Notes for Contributors

This is a learning project and is still evolving.  
Feel free to fork, modify, and publish improvements.

---

## License

There is **no license**.  
You can do whatever you want with this code.
