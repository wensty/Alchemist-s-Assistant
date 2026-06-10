#!/usr/bin/env python3
"""Generate an AlchAssV3 Vortex_*.bin circle-list file from a runtime collider dump."""

from __future__ import annotations

import argparse
import json
import re
import struct
from pathlib import Path
from typing import Any


def natural_key(value: str) -> list[Any]:
    return [int(part) if part.isdigit() else part.lower() for part in re.split(r"(\d+)", value)]


def point(value: dict[str, Any]) -> tuple[float, float]:
    return (float(value["x"]), float(value["y"]))


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate a compact Vortex circle-list bin from dumped EntryPoint colliders.")
    parser.add_argument("dump", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--indicator-radius", type=float, default=None)
    parser.add_argument("--include-inactive", action="store_true")
    parser.add_argument("--path-contains", default="Vortex")
    parser.add_argument("--entrypoint-contains", default="EntryPoint")
    args = parser.parse_args()

    dump = json.loads(args.dump.read_text(encoding="utf-8-sig"))
    indicator_radius = args.indicator_radius
    if indicator_radius is None:
        indicator_radius = float((dump.get("indicatorCollider") or {}).get("radius", 0.74))

    rows: list[tuple[str, float, float, float]] = []
    for collider in dump.get("colliders", []):
        path = str(collider.get("path", ""))
        if args.path_contains not in path or args.entrypoint_contains not in path:
            continue
        if not args.include_inactive and (not collider.get("enabled", False) or not collider.get("activeInHierarchy", False)):
            continue
        shape = collider.get("shape") or {}
        collider_type = str(shape.get("colliderType") or collider.get("type") or "")
        if "CircleCollider2D" not in collider_type:
            continue
        cx, cy = point(shape["mapCenter"])
        radius = float(shape.get("mapRadiusByLossyScaleMax", shape.get("radius", 0.0))) + indicator_radius
        rows.append((path, cx, cy, radius))

    rows.sort(key=lambda item: natural_key(item[0]))

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("wb") as stream:
        stream.write(struct.pack("<i", len(rows)))
        for _, x, y, radius in rows:
            stream.write(struct.pack("<ddd", x, y, radius))

    print(f"Wrote {args.output}")
    print(f"Vortex count: {len(rows)}")
    if rows:
        print(f"First: x={rows[0][1]:.8g} y={rows[0][2]:.8g} r={rows[0][3]:.8g}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
