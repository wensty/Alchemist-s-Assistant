#!/usr/bin/env python3
"""Render a CgalBinBuilder .bin boundary file as an SVG diagram."""

from __future__ import annotations

import argparse
import json
import math
import struct
from pathlib import Path
from typing import Any

TAU = math.pi * 2.0


def point(value: dict[str, Any]) -> tuple[float, float]:
    return (float(value["x"]), float(value["y"]))


def selected_colliders(dump: dict[str, Any], includes: list[str], excludes: list[str], include_inactive: bool) -> list[dict[str, Any]]:
    result = []
    for collider in dump.get("colliders", []):
        path = str(collider.get("path", ""))
        if includes and not any(part in path for part in includes):
            continue
        if excludes and any(part in path for part in excludes):
            continue
        if not include_inactive and (not collider.get("enabled", False) or not collider.get("activeInHierarchy", False)):
            continue
        result.append(collider)
    return result


def read_bin(path: Path):
    with path.open("rb") as stream:
        line_count, arc_count, node_count = struct.unpack("<iii", stream.read(12))
        lines = [struct.unpack("<dddd", stream.read(32)) for _ in range(line_count)]
        arcs = [struct.unpack("<ddddd", stream.read(40)) for _ in range(arc_count)]
    return lines, arcs, node_count


def expand_bounds(bounds: list[float], x: float, y: float) -> None:
    bounds[0] = min(bounds[0], x)
    bounds[1] = min(bounds[1], y)
    bounds[2] = max(bounds[2], x)
    bounds[3] = max(bounds[3], y)


def angle_in_span(angle: float, start: float, end: float) -> bool:
    angle %= TAU
    start %= TAU
    end %= TAU
    if end < start:
        end += TAU
    if angle < start:
        angle += TAU
    return start <= angle <= end


def add_bin_bounds(bounds: list[float], lines, arcs) -> None:
    for x1, y1, x2, y2 in lines:
        expand_bounds(bounds, x1, y1)
        expand_bounds(bounds, x2, y2)

    for cx, cy, radius, start, end in arcs:
        if abs(start) < 1e-8 and abs(end - TAU) < 1e-8:
            expand_bounds(bounds, cx - radius, cy - radius)
            expand_bounds(bounds, cx + radius, cy + radius)
            continue

        span = end - start if end >= start else end + TAU - start
        for angle in (start, start + span):
            expand_bounds(bounds, cx + math.cos(angle) * radius, cy + math.sin(angle) * radius)
        for angle in (0.0, math.pi / 2.0, math.pi, math.pi * 1.5):
            if angle_in_span(angle, start, start + span):
                expand_bounds(bounds, cx + math.cos(angle) * radius, cy + math.sin(angle) * radius)


def add_collider_bounds(bounds: list[float], colliders: list[dict[str, Any]]) -> None:
    for collider in colliders:
        shape = collider.get("shape") or {}
        collider_type = str(shape.get("colliderType") or collider.get("type") or "")
        if "CircleCollider2D" in collider_type:
            cx, cy = point(shape["mapCenter"])
            radius = float(shape.get("mapRadiusByLossyScaleMax", shape.get("radius", 0.0)))
            expand_bounds(bounds, cx - radius, cy - radius)
            expand_bounds(bounds, cx + radius, cy + radius)
        elif "BoxCollider2D" in collider_type:
            for coord in [point(p) for p in shape.get("mapCorners", [])]:
                expand_bounds(bounds, *coord)
        elif "PolygonCollider2D" in collider_type:
            for path in shape.get("mapPaths", []):
                for coord in [point(p) for p in path]:
                    expand_bounds(bounds, *coord)
        elif "EdgeCollider2D" in collider_type:
            for coord in [point(p) for p in shape.get("mapPoints", [])]:
                expand_bounds(bounds, *coord)
        elif "CapsuleCollider2D" in collider_type:
            for coord in [point(p) for p in shape.get("mapBoxCorners", [])]:
                expand_bounds(bounds, *coord)


def raw_collider_paths(colliders: list[dict[str, Any]], tx) -> str:
    paths: list[str] = []
    for collider in colliders:
        shape = collider.get("shape") or {}
        collider_type = str(shape.get("colliderType") or collider.get("type") or "")
        if "CircleCollider2D" in collider_type:
            cx, cy = point(shape["mapCenter"])
            radius = float(shape.get("mapRadiusByLossyScaleMax", shape.get("radius", 0.0)))
            x0, y0 = tx((cx - radius, cy))
            x1, y1 = tx((cx + radius, cy))
            r = abs(x1 - x0) / 2
            paths.append(f"M {x0:.3f} {y0:.3f} A {r:.3f} {r:.3f} 0 1 0 {x1:.3f} {y1:.3f} A {r:.3f} {r:.3f} 0 1 0 {x0:.3f} {y0:.3f} Z")
        elif "BoxCollider2D" in collider_type:
            coords = [point(p) for p in shape.get("mapCorners", [])]
            paths.append(polyline_path(coords, tx, closed=True))
        elif "PolygonCollider2D" in collider_type:
            for path in shape.get("mapPaths", []):
                paths.append(polyline_path([point(p) for p in path], tx, closed=True))
        elif "EdgeCollider2D" in collider_type:
            paths.append(polyline_path([point(p) for p in shape.get("mapPoints", [])], tx, closed=False))
        elif "CapsuleCollider2D" in collider_type:
            paths.append(polyline_path([point(p) for p in shape.get("mapBoxCorners", [])], tx, closed=True))
    return " ".join(path for path in paths if path)


def polyline_path(coords: list[tuple[float, float]], tx, closed: bool) -> str:
    if not coords:
        return ""
    x, y = tx(coords[0])
    parts = [f"M {x:.3f} {y:.3f}"]
    for coord in coords[1:]:
        x, y = tx(coord)
        parts.append(f"L {x:.3f} {y:.3f}")
    if closed:
        parts.append("Z")
    return " ".join(parts)


def boundary_path(lines, arcs, tx) -> str:
    parts: list[str] = []
    for x1, y1, x2, y2 in lines:
        sx, sy = tx((x1, y1))
        ex, ey = tx((x2, y2))
        parts.append(f"M {sx:.3f} {sy:.3f} L {ex:.3f} {ey:.3f}")

    for cx, cy, radius, start, end in arcs:
        span = end - start if end >= start else end + TAU - start
        if abs(start) < 1e-8 and abs(end - TAU) < 1e-8:
            left = tx((cx - radius, cy))
            right = tx((cx + radius, cy))
            r_px = abs(right[0] - left[0]) / 2
            parts.append(
                f"M {left[0]:.3f} {left[1]:.3f} "
                f"A {r_px:.3f} {r_px:.3f} 0 1 0 {right[0]:.3f} {right[1]:.3f} "
                f"A {r_px:.3f} {r_px:.3f} 0 1 0 {left[0]:.3f} {left[1]:.3f}"
            )
            continue

        sx = cx + math.cos(start) * radius
        sy = cy + math.sin(start) * radius
        ex = cx + math.cos(start + span) * radius
        ey = cy + math.sin(start + span) * radius
        start_px = tx((sx, sy))
        end_px = tx((ex, ey))
        center_px = tx((cx, cy))
        radius_px = abs(tx((cx + radius, cy))[0] - center_px[0])
        large_arc = 1 if span > math.pi else 0
        # SVG y-axis is inverted by tx(), so a CCW map arc becomes clockwise on screen.
        sweep = 0
        parts.append(f"M {start_px[0]:.3f} {start_px[1]:.3f} " f"A {radius_px:.3f} {radius_px:.3f} 0 {large_arc} {sweep} {end_px[0]:.3f} {end_px[1]:.3f}")
    return " ".join(parts)


def indicator_path(dump: dict[str, Any], tx) -> str:
    indicator = dump.get("indicatorCollider", {})
    center = indicator.get("mapCenter", {"x": 0, "y": 0})
    cx, cy = point(center)
    radius = float(indicator.get("radius", 0.74))
    left = tx((cx - radius, cy))
    right = tx((cx + radius, cy))
    r = abs(right[0] - left[0]) / 2
    return f"M {left[0]:.3f} {left[1]:.3f} A {r:.3f} {r:.3f} 0 1 0 {right[0]:.3f} {right[1]:.3f} A {r:.3f} {r:.3f} 0 1 0 {left[0]:.3f} {left[1]:.3f} Z"


def main() -> int:
    parser = argparse.ArgumentParser(description="Render a CGAL-generated bin boundary SVG diagram.")
    parser.add_argument("dump", type=Path)
    parser.add_argument("bin", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--include", action="append", default=None)
    parser.add_argument("--exclude", action="append", default=[])
    parser.add_argument("--include-inactive", action="store_true")
    parser.add_argument("--center-x", type=float)
    parser.add_argument("--center-y", type=float)
    parser.add_argument("--width", type=float)
    parser.add_argument("--height", type=float, default=None)
    parser.add_argument("--padding", type=float, default=2.0)
    parser.add_argument("--include-indicator", action="store_true")
    parser.add_argument("--hide-raw-colliders", action="store_true")
    parser.add_argument("--svg-width", type=int, default=1600)
    parser.add_argument("--svg-height", type=int, default=1000)
    args = parser.parse_args()

    dump = json.loads(args.dump.read_text(encoding="utf-8-sig"))
    lines, arcs, node_count = read_bin(args.bin)
    includes = args.include if args.include is not None else ["StrongDangerZoneContainer"]
    colliders = selected_colliders(dump, includes, args.exclude, args.include_inactive)

    manual_view = args.center_x is not None or args.center_y is not None or args.width is not None
    if manual_view:
        if args.center_x is None or args.center_y is None or args.width is None:
            parser.error("--center-x, --center-y, and --width must be provided together")
        viewport_w = args.width
        viewport_h = args.height if args.height is not None else args.width * args.svg_height / args.svg_width
        min_x = args.center_x - viewport_w / 2
        max_x = args.center_x + viewport_w / 2
        min_y = args.center_y - viewport_h / 2
        max_y = args.center_y + viewport_h / 2
    else:
        bounds = [float("inf"), float("inf"), float("-inf"), float("-inf")]
        add_bin_bounds(bounds, lines, arcs)
        if not args.hide_raw_colliders:
            add_collider_bounds(bounds, colliders)
        if not math.isfinite(bounds[0]):
            bounds = [-1.0, -1.0, 1.0, 1.0]
        min_x, min_y, max_x, max_y = bounds
        min_x -= args.padding
        min_y -= args.padding
        max_x += args.padding
        max_y += args.padding
        data_w = max(max_x - min_x, 1e-6)
        data_h = max(max_y - min_y, 1e-6)
        target_aspect = args.svg_width / args.svg_height
        data_aspect = data_w / data_h
        if data_aspect > target_aspect:
            extra = data_w / target_aspect - data_h
            min_y -= extra / 2.0
            max_y += extra / 2.0
        else:
            extra = data_h * target_aspect - data_w
            min_x -= extra / 2.0
            max_x += extra / 2.0
        viewport_w = max_x - min_x
        viewport_h = max_y - min_y

    scale = min(args.svg_width / viewport_w, args.svg_height / viewport_h)
    offset_x = (args.svg_width - viewport_w * scale) / 2
    offset_y = (args.svg_height - viewport_h * scale) / 2

    def tx(coord: tuple[float, float]) -> tuple[float, float]:
        x, y = coord
        return (offset_x + (x - min_x) * scale, offset_y + (max_y - y) * scale)

    raw_path = "" if args.hide_raw_colliders else raw_collider_paths(colliders, tx)
    cgal_path = boundary_path(lines, arcs, tx)
    ind_path = indicator_path(dump, tx) if args.include_indicator else ""
    indicator_svg = f'  <path d="{ind_path}" fill="#3e73d8" fill-opacity="0.18" stroke="#244a9b" stroke-width="2.0"/>\n' if ind_path else ""
    legend = "red=CGAL bin Line/Arc boundary"
    if raw_path:
        legend += ", brown=raw selected colliders"
    if ind_path:
        legend += ", blue=indicator"

    svg = f"""<svg xmlns="http://www.w3.org/2000/svg" width="{args.svg_width}" height="{args.svg_height}" viewBox="0 0 {args.svg_width} {args.svg_height}">
  <rect width="100%" height="100%" fill="#f6ead2"/>
  <path d="{raw_path}" fill="#6d4a24" fill-opacity="0.16" stroke="#5d3b1b" stroke-width="0.8" stroke-dasharray="5 4"/>
  <path d="{cgal_path}" fill="none" stroke="#c22b2b" stroke-width="2.4" stroke-linecap="round"/>
{indicator_svg.rstrip()}
  <text x="18" y="28" font-family="Consolas, monospace" font-size="18" fill="#3b2a18">CGAL boundary-filter bin diagram</text>
  <text x="18" y="52" font-family="Consolas, monospace" font-size="14" fill="#3b2a18">lines={len(lines)} arcs={len(arcs)} nodes={node_count} colliders={len(colliders)}</text>
  <text x="18" y="74" font-family="Consolas, monospace" font-size="14" fill="#3b2a18">{legend}</text>
</svg>
"""
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(svg, encoding="utf-8")
    print(f"Wrote {args.output}")
    print(f"Lines: {len(lines)}")
    print(f"Arcs: {len(arcs)}")
    print(f"Nodes: {node_count}")
    print(f"Bounds: {(min_x, min_y, max_x, max_y)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
