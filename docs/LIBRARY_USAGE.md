# TinyEcs C - Library Usage Guide

This document explains how to use TinyEcs as a library (shared/static) in your projects.

## Building the Libraries

TinyEcs supports three build modes:

### 1. Header-Only (Default)

The simplest usage - just include the headers with implementation defines:

```c
#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"

// If using Bevy layer:
#define TINYECS_BEVY_IMPLEMENTATION
#include "tinyecs_bevy.h"
```

**Build command:**
```bash
gcc -O3 -march=native -o myapp myapp.c
```

### 2. Shared Library (DLL)

Build and link against shared libraries for smaller executables and shared memory.

**Build libraries:**
```bash
make dll
```

This creates:
- `tinyecs.dll` + `libtinyecs.a` (import library)
- `tinyecs_bevy.dll` + `libtinyecs_bevy.a` (import library)

**Usage in your code:**
```c
// myapp.c
#define TINYECS_SHARED_LIBRARY
#include "tinyecs.h"
#include "tinyecs_bevy.h"

int main(void) {
    tecs_world_t* world = tecs_world_new();
    // ...
}
```

**Build your app:**
```bash
# Windows (MinGW)
gcc -O3 myapp.c -L. -ltinyecs -ltinyecs_bevy -o myapp.exe

# Linux
gcc -O3 myapp.c -L. -ltinyecs -ltinyecs_bevy -o myapp -Wl,-rpath,'$ORIGIN'
```

**Distribution:**
- Include `tinyecs.dll` and `tinyecs_bevy.dll` with your executable
- Windows: Place DLLs in same directory as .exe
- Linux: Use `-Wl,-rpath,'$ORIGIN'` to load from exe directory

### 3. Static Library

Build static libraries for single-binary distribution without DLL dependencies.

**Build libraries:**
```bash
make static
```

This creates:
- `libtinyecs.a`
- `libtinyecs_bevy.a`

**Usage in your code:**
```c
// myapp.c
#include "tinyecs.h"
#include "tinyecs_bevy.h"

int main(void) {
    tecs_world_t* world = tecs_world_new();
    // ...
}
```

**Build your app:**
```bash
gcc -O3 myapp.c -L. -ltinyecs -ltinyecs_bevy -o myapp
```

**Distribution:**
- No DLLs needed - everything is in the executable
- Larger executable size
- Simpler deployment

## API Export Macros

TinyEcs uses platform-specific macros for DLL export/import:

- **`TECS_API`**: Marks core TinyEcs functions
- **`TBEVY_API`**: Marks Bevy layer functions

### How It Works

**When building the DLL (`TINYECS_SHARED_LIBRARY` + `TINYECS_IMPLEMENTATION`):**
```c
// Windows (MinGW GCC)
#define TECS_API __attribute__((dllexport))

// Windows (MSVC)
#define TECS_API __declspec(dllexport)

// Linux/Unix
#define TECS_API __attribute__((visibility("default")))
```

**When using the DLL (`TINYECS_SHARED_LIBRARY` without implementation):**
```c
// Windows (MinGW GCC)
#define TECS_API __attribute__((dllimport))

// Windows (MSVC)
#define TECS_API __declspec(dllimport)
```

**When using header-only (no `TINYECS_SHARED_LIBRARY`):**
```c
#define TECS_API    // Expands to nothing
```

## Example Projects

### Minimal Header-Only Example

```c
// minimal.c
#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"

typedef struct { float x, y; } Position;

int main(void) {
    tecs_world_t* world = tecs_world_new();

    tecs_component_id_t Position_id =
        tecs_register_component(world, "Position", sizeof(Position));

    tecs_entity_t entity = tecs_entity_new(world);
    Position pos = {10.0f, 20.0f};
    tecs_set(world, entity, Position_id, &pos, sizeof(Position));

    Position* p = tecs_get(world, entity, Position_id);
    printf("Position: (%.1f, %.1f)\n", p->x, p->y);

    tecs_world_free(world);
    return 0;
}
```

**Build:**
```bash
gcc -O3 -march=native minimal.c -o minimal
```

### Shared Library Example

```c
// app.c
#define TINYECS_SHARED_LIBRARY
#include "tinyecs.h"
#include "tinyecs_bevy.h"

typedef struct { float x, y; } Position;
typedef struct { float x, y; } Velocity;

static tecs_component_id_t Position_id;
static tecs_component_id_t Velocity_id;

void update_system(tbevy_app_t* app, void* user_data) {
    (void)user_data;

    tecs_query_t* query = tecs_query_new(tbevy_app_world(app));
    tecs_query_with(query, Position_id);
    tecs_query_with(query, Velocity_id);
    tecs_query_build(query);

    tecs_query_iter_t* iter = tecs_query_iter(query);
    while (tecs_query_next(iter)) {
        int count = tecs_iter_count(iter);
        Position* positions = (Position*)tecs_iter_column(iter, 0);
        Velocity* velocities = (Velocity*)tecs_iter_column(iter, 1);

        for (int i = 0; i < count; i++) {
            positions[i].x += velocities[i].x;
            positions[i].y += velocities[i].y;
        }
    }

    tecs_query_iter_free(iter);
    tecs_query_free(query);
}

int main(void) {
    tbevy_app_t* app = tbevy_app_new(TBEVY_THREADING_SINGLE);

    Position_id = tecs_register_component(tbevy_app_world(app),
                                          "Position", sizeof(Position));
    Velocity_id = tecs_register_component(tbevy_app_world(app),
                                          "Velocity", sizeof(Velocity));

    // Spawn entities
    tbevy_commands_t commands;
    tbevy_commands_init(&commands, app);

    for (int i = 0; i < 1000; i++) {
        tbevy_entity_commands_t ec = tbevy_commands_spawn(&commands);
        Position pos = {0.0f, 0.0f};
        Velocity vel = {1.0f, 1.0f};
        tbevy_entity_insert(&ec, Position_id, &pos, sizeof(Position));
        tbevy_entity_insert(&ec, Velocity_id, &vel, sizeof(Velocity));
    }

    tbevy_commands_apply(&commands);
    tbevy_commands_free(&commands);

    // Add update system
    tbevy_system_build(
        tbevy_system_in_stage(
            tbevy_app_add_system(app, update_system, NULL),
            tbevy_stage_default(TBEVY_STAGE_UPDATE)
        )
    );

    // Run 60 frames
    for (int i = 0; i < 60; i++) {
        tbevy_app_update(app);
    }

    printf("Ran 60 frames successfully!\n");

    tbevy_app_free(app);
    return 0;
}
```

**Build:**
```bash
# First build the DLLs
make dll

# Then build your app
gcc -O3 app.c -L. -ltinyecs -ltinyecs_bevy -o app.exe

# Run (DLLs must be in same directory)
./app.exe
```

## Compiler Flags Reference

### Release Build (Recommended)
```bash
-O3                  # Maximum optimization
-march=native        # Use CPU-specific instructions
-flto                # Link-time optimization
-ffast-math          # Relaxed FP math (acceptable for games)
-DNDEBUG            # Disable assertions
```

**Expected performance:** 3,600M entities/sec on modern CPU

### Debug Build
```bash
-O0                  # No optimization (easier debugging)
-g                   # Debug symbols
-Wall -Wextra        # All warnings
```

### Shared Library Build
```bash
-O3 -march=native -ffast-math
-DTINYECS_SHARED_LIBRARY         # Enable DLL import/export
-DTINYECS_IMPLEMENTATION         # When building the DLL
-DTINYECS_BEVY_IMPLEMENTATION    # When building Bevy DLL
-fPIC                            # Position-independent code (Linux)
-shared                          # Create shared library
```

## Platform-Specific Notes

### Windows (MinGW GCC)
- DLLs placed in same directory as .exe automatically found
- Use `__attribute__((dllexport/dllimport))` for symbols
- Import libraries (`.a`) needed at link time

### Windows (MSVC)
- Use `__declspec(dllexport/dllimport)` for symbols
- Import libraries (`.lib`) needed at link time
- May need `/MD` flag for runtime library

### Linux
- Use `-fPIC` when building shared libraries
- Set `LD_LIBRARY_PATH` or use `-Wl,-rpath,'$ORIGIN'`
- Use `-fvisibility=hidden` + `__attribute__((visibility("default")))`

### macOS
- Similar to Linux, use `.dylib` extension
- Use `@rpath` for runtime library paths
- Code sign DLLs: `codesign -s - yourlib.dylib`

## Performance Considerations

### Header-Only
- ✅ Fastest compilation (LTO across all code)
- ✅ Best optimization opportunities
- ❌ Slower incremental builds
- ✅ No DLL dependencies

### Shared Library (DLL)
- ✅ Fast incremental builds
- ✅ Shared memory between processes
- ✅ Smaller executables
- ❌ Slight performance overhead (indirect calls)
- ❌ DLL dependencies to distribute

### Static Library
- ✅ Single binary distribution
- ✅ Fast incremental builds
- ✅ Good optimization with LTO
- ❌ Larger executable size
- ❌ Duplicated code if used by multiple DLLs

### Recommendation

- **Game development**: Header-only or static library (easier distribution)
- **Large projects**: Shared library (faster builds)
- **Plugins/mods**: Shared library (shared state)

## Troubleshooting

### "undefined reference to" errors

**Problem:** Linking fails with undefined symbol errors.

**Solution:** Make sure you:
1. Link both libraries: `-ltinyecs -ltinyecs_bevy`
2. Use `-L.` to specify library search path
3. Define `TINYECS_SHARED_LIBRARY` when using DLLs

### "cannot find -ltinyecs" errors

**Problem:** Linker can't find the library files.

**Solution:**
```bash
# Check libraries exist
ls -l libtinyecs.a tinyecs.dll

# Use explicit path
gcc app.c -L./path/to/libs -ltinyecs -ltinyecs_bevy
```

### DLL not found at runtime

**Problem:** Windows can't find `tinyecs.dll` when running.

**Solution:**
- Copy `tinyecs.dll` and `tinyecs_bevy.dll` to same directory as .exe
- Or add directory to PATH: `set PATH=%PATH%;C:\path\to\dlls`

### "multiple definition" errors

**Problem:** Multiple translation units include the implementation.

**Solution:** Only define `TINYECS_IMPLEMENTATION` once:
```c
// main.c - Define implementation here
#define TINYECS_IMPLEMENTATION
#include "tinyecs.h"

// other.c - Just include header
#include "tinyecs.h"
```

## CMake Integration Example

```cmake
# CMakeLists.txt
cmake_minimum_required(VERSION 3.10)
project(MyGame C)

# Option: Use header-only or build as library
option(TINYECS_BUILD_SHARED "Build TinyEcs as shared library" OFF)

if(TINYECS_BUILD_SHARED)
    # Build as shared library
    add_library(tinyecs SHARED tinyecs_impl.c)
    add_library(tinyecs_bevy SHARED tinyecs_bevy_impl.c)
    target_compile_definitions(tinyecs PUBLIC TINYECS_SHARED_LIBRARY)
    target_compile_definitions(tinyecs_bevy PUBLIC TINYECS_SHARED_LIBRARY)
    target_link_libraries(tinyecs_bevy tinyecs)

    add_executable(myapp main.c)
    target_link_libraries(myapp tinyecs tinyecs_bevy)
else()
    # Header-only
    add_executable(myapp main.c)
    target_compile_definitions(myapp PRIVATE
        TINYECS_IMPLEMENTATION
        TINYECS_BEVY_IMPLEMENTATION)
endif()

target_compile_options(myapp PRIVATE -O3 -march=native -Wall -Wextra)
```

## License

TinyEcs is released under the MIT License. See LICENSE file for details.
