# Raylib 3D Game Build Notes

## Status

✅ **Core TinyECS + Bevy + Hierarchy**: Working perfectly
- `example_hierarchy.c` - All hierarchy tests pass
- `example_bevy_hierarchy.c` - Full Bevy + hierarchy demo works
- Ships with turrets and shields as children
- System ordering, queries, transforms all functional

✅ **Game Code**: Complete and ready
- `example_bevy_raylib_3d.c` - 3D space shooter (1076 lines)
- Player controls (WASD/QE movement, arrows rotation, SPACE fire)
- Enemy AI with hierarchy (turrets + shields as children)
- Collision detection, particle effects, camera system
- Full ECS architecture with multiple stages

❌ **Compilation**: Blocked by toolchain mismatch

## Problem

The raylib library in `raylib-5.5_win64_msvc16/` is:
- Built for **MSVC 64-bit** (x86-64)
- Located at: `src-c/raylib-5.5_win64_msvc16/`

But the system compiler is:
- **mingw32 32-bit** (i686-w64-mingw32)
- Path: `C:/mingw32/bin/gcc.exe`

Result: Undefined references to all raylib functions (`IsKeyDown`, `BeginDrawing`, etc.)

## Solutions

### Option 1: Download MinGW-w64 Raylib (Recommended)

Download the MinGW-w64 version of Raylib 5.5:

```bash
# Download from https://github.com/raysan5/raylib/releases/tag/5.5
# Get: raylib-5.5_win64_mingw-w64.zip
# Extract to src-c/raylib-5.5_mingw-w64/
```

Then compile:
```bash
cd src-c
gcc -o example_bevy_raylib_3d.exe example_bevy_raylib_3d.c \
    -I./raylib-5.5_mingw-w64/include \
    -L./raylib-5.5_mingw-w64/lib \
    -lraylib -lopengl32 -lgdi32 -lwinmm \
    -std=c11 -Wno-unused-parameter
```

### Option 2: Use MSVC Compiler

If you have Visual Studio installed:

```cmd
cd src-c
cl /I"raylib-5.5_win64_msvc16\include" example_bevy_raylib_3d.c ^
   raylib-5.5_win64_msvc16\lib\raylib.lib ^
   /link /SUBSYSTEM:WINDOWS
```

### Option 3: Build Raylib from Source with Zig

```bash
cd /c/dev/raylib
zig build -Doptimize=ReleaseFast
# Then use the built library from zig-out/lib/
```

(Note: Currently failing due to old zig version - needs zig >= 0.13.0)

### Option 4: Use Existing Raylib Source

The raylib source is available at `/c/dev/raylib/src/`. You could compile it inline:

```bash
cd src-c
gcc -o example_bevy_raylib_3d.exe \
    example_bevy_raylib_3d.c \
    /c/dev/raylib/src/rcore.c \
    /c/dev/raylib/src/rshapes.c \
    /c/dev/raylib/src/rtextures.c \
    /c/dev/raylib/src/rtext.c \
    /c/dev/raylib/src/rmodels.c \
    /c/dev/raylib/src/raudio.c \
    /c/dev/raylib/src/rglfw.c \
    -I/c/dev/raylib/src \
    -I/c/dev/raylib/src/external/glfw/include \
    -lopengl32 -lgdi32 -lwinmm -std=c11
```

## Current File State

The game source (`example_bevy_raylib_3d.c`) includes:
- `#define TINYECS_IMPLEMENTATION`
- `#define TINYECS_BEVY_IMPLEMENTATION`
- `#define RAYMATH_IMPLEMENTATION`
- `#include <raylib.h>`
- `#include <raymath.h>`
- `#include <rlgl.h>`

All necessary headers are included and macro definitions are correct.

## Game Features

Once compiled, the game will demonstrate:

1. **3D Rendering**
   - Camera following player with smooth lerp
   - 3D grid
   - Multiple mesh types (cubes, spheres, cylinders)

2. **Entity Hierarchy**
   - Enemies have turret and shield children
   - Children follow parent transforms
   - Proper 3D transform composition

3. **Player Controls**
   - WASD/QE - 3D movement (X, Y, Z axes)
   - Arrow keys - Rotation (pitch/yaw)
   - SPACE - Fire bullets in forward direction

4. **Enemy AI**
   - Spawn system (max 10 enemies)
   - Chase behavior toward player
   - Health system

5. **Combat**
   - Bullet entities with velocity
   - 3D collision detection (Vector3Distance)
   - Particle explosion effects on death
   - Score tracking

6. **ECS Architecture**
   - 4 stages: FIRST, UPDATE, POST_UPDATE, LAST
   - 12 systems with proper ordering
   - Resources: Time, Camera, GameStats
   - Components: Transform3D, Velocity3D, MeshRenderer, Player, Enemy, Bullet, Particle, Health

## Next Steps

1. Download MinGW-w64 raylib build
2. Update include/library paths
3. Compile and run
4. Enjoy the 3D space shooter!

## Verification

To verify the ECS + Bevy + hierarchy works without Raylib:

```bash
cd src-c
./example_bevy_hierarchy.exe
```

This demonstrates all the ECS features the 3D game uses.
