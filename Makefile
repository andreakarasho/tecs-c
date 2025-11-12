# TinyEcs C Makefile

# Detect OS for cross-platform commands
# On Windows with Unix-like shell (Git Bash, MSYS2), still use Unix commands
ifdef SHELL
    ifneq (,$(findstring sh,$(SHELL)))
        # Unix-like shell (even on Windows)
        RM = rm -f
        RM_RECURSIVE = rm -rf
        MKDIR = mkdir -p
        SEP = /
    else ifeq ($(OS),Windows_NT)
        # Native Windows CMD
        RM = del /Q
        RM_RECURSIVE = del /Q /S
        MKDIR = mkdir
        SEP = \\
    endif
else
    # Unix systems
    RM = rm -f
    RM_RECURSIVE = rm -rf
    MKDIR = mkdir -p
    SEP = /
endif

# Compiler selection (use 'make CC=gcc' to use GCC instead)
CC = zig cc
CFLAGS_DEBUG = -std=c99 -Wall -Wextra -O0 -g
CFLAGS_RELEASE = -std=c99 -Wall -Wextra -O3 -DNDEBUG
CFLAGS_SHARED = -std=c99 -Wall -Wextra -O3 -DNDEBUG -DTINYECS_SHARED_LIBRARY -fPIC
CFLAGS = $(CFLAGS_RELEASE)
LDFLAGS = -lm

# Build output directory
BUILD_DIR = build

# Source files
HEADERS = tinyecs.h tinyecs_bevy.h
DLL_CORE = $(BUILD_DIR)/tinyecs.dll
DLL_BEVY = $(BUILD_DIR)/tinyecs_bevy.dll
LIB_CORE = $(BUILD_DIR)/libtinyecs.a
LIB_BEVY = $(BUILD_DIR)/libtinyecs_bevy.a

# Targets
EXAMPLES = $(BUILD_DIR)/example.exe $(BUILD_DIR)/example_bevy.exe $(BUILD_DIR)/example_performance.exe $(BUILD_DIR)/example_performance_opt.exe $(BUILD_DIR)/example_bevy_performance.exe $(BUILD_DIR)/example_iter_cache.exe $(BUILD_DIR)/example_iter_library_cache.exe

TESTS = $(BUILD_DIR)/test_bevy_query.exe $(BUILD_DIR)/test_bevy_update.exe $(BUILD_DIR)/test_hierarchy_debug.exe $(BUILD_DIR)/test_ids.exe

.PHONY: all clean debug release benchmark dll static test run-tests

# Default: Build all examples in release mode
all: release

# Ensure build directory exists
$(BUILD_DIR):
	$(MKDIR) $(BUILD_DIR)

# Release builds (optimized)
release: CFLAGS = $(CFLAGS_RELEASE)
release: $(BUILD_DIR) $(EXAMPLES)

# Debug builds
debug: CFLAGS = $(CFLAGS_DEBUG)
debug: $(BUILD_DIR) $(EXAMPLES)

# Individual example targets
$(BUILD_DIR)/example.exe: examples/example.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/example_bevy.exe: examples/example_bevy.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/example_performance.exe: examples/example_performance.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/example_performance_opt.exe: examples/example_performance_opt.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/example_bevy_performance.exe: examples/example_bevy_performance.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/example_iter_cache.exe: examples/example_iter_cache.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/example_iter_library_cache.exe: examples/example_iter_library_cache.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

# Test targets
$(BUILD_DIR)/test_bevy_query.exe: tests/test_bevy_query.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/test_bevy_update.exe: tests/test_bevy_update.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/test_hierarchy_debug.exe: tests/test_hierarchy_debug.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

$(BUILD_DIR)/test_ids.exe: tests/test_ids.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS) -I. -o $@ $<

# Build all tests
test: $(BUILD_DIR) $(TESTS)

# Run all tests
run-tests: $(TESTS)
	@echo === Running All Tests ===
	@echo ""
	@echo Running build/test_bevy_query.exe...
	@./build/test_bevy_query.exe
	@echo ""
	@echo Running build/test_bevy_update.exe...
	@./build/test_bevy_update.exe
	@echo ""
	@echo Running build/test_hierarchy_debug.exe...
	@./build/test_hierarchy_debug.exe
	@echo ""
	@echo Running build/test_ids.exe...
	@./build/test_ids.exe
	@echo ""
	@echo === All Tests Passed ===

# Benchmark target - run optimized performance test
benchmark: $(BUILD_DIR)/example_performance_opt.exe
	@echo "=== Running TinyEcs Performance Benchmark (Optimized) ==="
	@echo "Compiler flags: $(CFLAGS_RELEASE)"
	@echo ""
	./$(BUILD_DIR)/example_performance_opt.exe

# Clean all build artifacts
clean:
	$(RM_RECURSIVE) $(BUILD_DIR)
	$(RM) *.o tinyecs_impl.c tinyecs_bevy_impl.c

# Shared library (DLL) builds
dll: $(BUILD_DIR) $(DLL_CORE) $(DLL_BEVY)

tinyecs_impl.c:
	@echo "#define TINYECS_IMPLEMENTATION" > tinyecs_impl.c
	@echo "#include \"tinyecs.h\"" >> tinyecs_impl.c

tinyecs_bevy_impl.c:
	@echo "#define TINYECS_IMPLEMENTATION" > tinyecs_bevy_impl.c
	@echo "#define TINYECS_BEVY_IMPLEMENTATION" >> tinyecs_bevy_impl.c
	@echo "#include \"tinyecs.h\"" >> tinyecs_bevy_impl.c
	@echo "#include \"tinyecs_bevy.h\"" >> tinyecs_bevy_impl.c

$(DLL_CORE): tinyecs_impl.c $(HEADERS) | $(BUILD_DIR)
	$(CC) $(CFLAGS_SHARED) -shared -o $@ tinyecs_impl.c -Wl,--out-implib,$(LIB_CORE)

$(DLL_BEVY): tinyecs_bevy_impl.c $(HEADERS) $(DLL_CORE) | $(BUILD_DIR)
	$(CC) $(CFLAGS_SHARED) -shared -o $@ tinyecs_bevy_impl.c -L$(BUILD_DIR) -ltinyecs -Wl,--out-implib,$(LIB_BEVY)

# Static library builds
static: $(BUILD_DIR) $(LIB_CORE) $(LIB_BEVY)

tinyecs_impl.o: tinyecs_impl.c $(HEADERS)
	$(CC) $(CFLAGS_RELEASE) -c -o $@ tinyecs_impl.c

tinyecs_bevy_impl.o: tinyecs_bevy_impl.c $(HEADERS)
	$(CC) $(CFLAGS_RELEASE) -c -o $@ tinyecs_bevy_impl.c

$(LIB_CORE): tinyecs_impl.o | $(BUILD_DIR)
	ar rcs $@ $<

$(LIB_BEVY): tinyecs_bevy_impl.o | $(BUILD_DIR)
	ar rcs $@ $<

# Run examples
run-example: $(BUILD_DIR)/example.exe
	./$(BUILD_DIR)/example.exe

run-bevy: $(BUILD_DIR)/example_bevy.exe
	./$(BUILD_DIR)/example_bevy.exe

run-performance: $(BUILD_DIR)/example_performance.exe
	./$(BUILD_DIR)/example_performance.exe

# Help
help:
	@echo "TinyEcs C Makefile"
	@echo ""
	@echo "Compiler: $(CC)"
	@echo ""
	@echo "Targets:"
	@echo "  all         - Build all examples (release mode, default)"
	@echo "  release     - Build with optimizations (-O3)"
	@echo "  debug       - Build with debug symbols (-O0 -g)"
	@echo "  test        - Build all test programs"
	@echo "  run-tests   - Build and run all tests"
	@echo "  dll         - Build shared libraries (DLL)"
	@echo "  static      - Build static libraries (.a)"
	@echo "  benchmark   - Run optimized performance benchmark"
	@echo "  clean       - Remove all build artifacts"
	@echo ""
	@echo "Examples:"
	@echo "  make release           # Build optimized binaries"
	@echo "  make debug             # Build debug binaries"
	@echo "  make test              # Build all tests"
	@echo "  make run-tests         # Build and run all tests"
	@echo "  make CC=gcc release    # Use GCC instead of Zig"
	@echo "  make dll               # Build DLL libraries"
	@echo "  make static            # Build static libraries"
	@echo "  make benchmark         # Run performance test"
	@echo "  make run-performance   # Run performance benchmark"
