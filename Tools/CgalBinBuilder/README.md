# CgalBinBuilder

High-quality offline `.bin` builder for `AlchAssV3`.

This tool is intended to become the final preprocessing path for danger/swamp
zone geometry:

```text
runtime collider dump
-> analytic line/arc offset boundaries
-> exact/regularized union of each danger cluster
-> exterior line/arc contours
-> runtime-compatible BVH .bin
```

The important design goal is to preserve circular arcs during preprocessing.
Runtime cost is not a concern because the mod only loads the generated
`Line`/`Arc` primitives and BVH.

## Build

CGAL is expected to be installed through vcpkg. In a Visual Studio Developer
PowerShell, or a terminal launched from a Visual Studio environment, `cmake`,
the MSVC toolchain, and vcpkg integration tools are usually already available
on `PATH`.

If vcpkg is integrated globally, CMake may find CGAL without an explicit
toolchain path:

```powershell
cmake -S Tools\CgalBinBuilder -B build\CgalBinBuilder `
  -DVCPKG_TARGET_TRIPLET=x64-windows

cmake --build build\CgalBinBuilder --config Release
```

If CMake cannot find CGAL, pass the vcpkg toolchain file explicitly:

```powershell
cmake -S Tools\CgalBinBuilder -B build\CgalBinBuilder `
  -DCMAKE_TOOLCHAIN_FILE=<vcpkg-root>\scripts\buildsystems\vcpkg.cmake `
  -DVCPKG_TARGET_TRIPLET=x64-windows
```

If `cmake` is not on `PATH`, use Visual Studio's Developer PowerShell, install
the Visual Studio C++ CMake tools component, or use a terminal that inherits the
Visual Studio build environment.

## Run

Example:

```powershell
build\CgalBinBuilder\Release\CgalBinBuilder.exe `
  RuntimeDumps\map_colliders_Water_20260608_233506.json `
  RuntimeDumps\Generated_Strong_Water_cgal.bin
```

Options:

```text
--include <text>       Include collider paths containing text. May repeat.
                       Default: StrongDangerZoneContainer
--exclude <text>       Exclude collider paths containing text. May repeat.
--radius <value>       Override indicator radius. Default: dump indicator radius.
--include-inactive     Include disabled/inactive colliders.
--mode <mode>          per-collider, boundary-filter, or cgal-union.
                       Default: per-collider
--boundary-step <n>    Sampling interval used by boundary-filter.
                       Default: 0.05
--coverage-epsilon <n> Coverage tolerance used by boundary-filter.
                       Default: 1e-7
--non-circle-extra-offset <n>
                       Extra buffer added only to non-circle colliders.
                       Useful for testing Unity's non-circle contact buffer.
                       Default: 0
--max-leaf-items <n>   Maximum primitive count stored in one BVH leaf.
                       Default: 4
```

The current best practical mode is `boundary-filter`:

```powershell
build\CgalBinBuilder\Release\CgalBinBuilder.exe `
  RuntimeDumps\map_colliders_Water_20260608_233506.json `
  RuntimeDumps\Generated_Strong_Water_cgal_boundary_filter.bin `
  --mode boundary-filter --boundary-step 0.02 `
  --non-circle-extra-offset 0.01 --max-leaf-items 4
```

## Current State

The current version already provides:

- dump JSON parsing via Boost.PropertyTree;
- analytic per-collider offset generation as `Line` and `Arc`;
- `boundary-filter` mode that removes candidate boundary pieces covered by
  other inflated colliders while preserving `Line`/`Arc` primitives;
- output format compatible with `Function.LoadZoneFromBin`;
- AABB BVH built with a surface-area heuristic split to match the original
  preprocessing strategy more closely. Split planes enumerate primitive AABB
  right/top edges, arc primitives count as `1.2` in the SAH cost, and leaves
  hold up to `--max-leaf-items` primitives;
- a CMake/vcpkg project linked against CGAL.

The remaining core work is the high-quality CGAL union step:

```text
per-collider Line/Arc offset boundaries
-> CGAL arrangement / regularized-union pass over the offset line/arc curves
-> only exterior cluster boundaries
```

The installed CGAL package provides the API family needed for this:

- `CGAL/Gps_circle_segment_traits_2.h`
- `CGAL/Arr_circle_segment_traits_2.h`
- `CGAL/General_polygon_set_2.h`

`--mode cgal-union` is reserved for that implementation and currently fails
explicitly. Until that step is implemented, use `--mode boundary-filter` for
the closest practical result. `--mode per-collider` remains useful as a raw
debugging baseline and will still include internal boundaries.
