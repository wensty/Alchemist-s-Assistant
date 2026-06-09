#!/usr/bin/env python3
"""Generate an AlchAssV3 zone .bin file from a runtime collider dump.

The generated binary uses the same layout read by Function.LoadZoneFromBin:

    int32 line_count
    int32 arc_count
    int32 node_count
    line_count * (double x1, y1, x2, y2)
    arc_count * (double x, y, r, start_angle, end_angle)
    node_count * BVH node

This tool intentionally works from the dumped map-space collider geometry.  It
does not need Unity assemblies.

The default generator builds analytic offset boundaries for each selected
collider independently. It is kept as a lightweight debugging helper; the
preferred production path is Tools/CgalBinBuilder.
"""

from __future__ import annotations

import argparse
import json
import math
import struct
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable

TAU = math.pi * 2.0
EPS = 1e-9
MAX_LEAF_ITEMS = 4
ARC_TRAVERSAL_WEIGHT = 1.2


@dataclass(frozen=True)
class Point:
    x: float
    y: float

    def __add__(self, other: "Point") -> "Point":
        return Point(self.x + other.x, self.y + other.y)

    def __sub__(self, other: "Point") -> "Point":
        return Point(self.x - other.x, self.y - other.y)

    def __mul__(self, scalar: float) -> "Point":
        return Point(self.x * scalar, self.y * scalar)


@dataclass(frozen=True)
class Line:
    x1: float
    y1: float
    x2: float
    y2: float

    @property
    def bounds(self) -> tuple[float, float, float, float]:
        return (
            min(self.x1, self.x2),
            min(self.y1, self.y2),
            max(self.x1, self.x2),
            max(self.y1, self.y2),
        )

    @property
    def center(self) -> Point:
        return Point((self.x1 + self.x2) * 0.5, (self.y1 + self.y2) * 0.5)


@dataclass(frozen=True)
class Arc:
    x: float
    y: float
    r: float
    start: float
    end: float

    @property
    def bounds(self) -> tuple[float, float, float, float]:
        # Conservative full-circle bounds.  This keeps BVH generation simple and
        # correct even for wrapped arcs.
        return (self.x - self.r, self.y - self.r, self.x + self.r, self.y + self.r)

    @property
    def center(self) -> Point:
        return Point(self.x, self.y)


Primitive = Line | Arc


@dataclass(frozen=True)
class BvhNode:
    bounds: tuple[float, float, float, float]
    left: int | None = None
    right: int | None = None
    items: tuple[int, ...] = ()

    @property
    def is_leaf(self) -> bool:
        return bool(self.items)


def vec_from_json(value: dict[str, Any]) -> Point:
    return Point(float(value["x"]), float(value["y"]))


def length(v: Point) -> float:
    return math.hypot(v.x, v.y)


def normalize(v: Point) -> Point:
    d = length(v)
    if d <= EPS:
        return Point(0.0, 0.0)
    return Point(v.x / d, v.y / d)


def signed_area(points: list[Point]) -> float:
    area = 0.0
    for p0, p1 in zip(points, points[1:] + points[:1]):
        area += p0.x * p1.y - p1.x * p0.y
    return area * 0.5


def norm_angle(angle: float) -> float:
    angle %= TAU
    if angle < 0:
        angle += TAU
    return angle


def angle_of(v: Point) -> float:
    return norm_angle(math.atan2(v.y, v.x))


def merge_bounds(bounds: Iterable[tuple[float, float, float, float]]) -> tuple[float, float, float, float]:
    items = list(bounds)
    return (
        min(b[0] for b in items),
        min(b[1] for b in items),
        max(b[2] for b in items),
        max(b[3] for b in items),
    )


def bounds_area(bounds: tuple[float, float, float, float]) -> float:
    return max(0.0, bounds[2] - bounds[0]) * max(0.0, bounds[3] - bounds[1])


def outward_normals(points: list[Point]) -> list[Point]:
    if signed_area(points) < 0:
        points.reverse()

    normals: list[Point] = []
    for p0, p1 in zip(points, points[1:] + points[:1]):
        edge = p1 - p0
        normals.append(normalize(Point(edge.y, -edge.x)))
    return normals


def add_offset_polygon(points: list[Point], radius: float, lines: list[Line], arcs: list[Arc]) -> None:
    points = [p for p in points]
    if len(points) < 2:
        return
    if len(points) == 2:
        add_offset_segment(points[0], points[1], radius, lines, arcs)
        return

    normals = outward_normals(points)
    count = len(points)

    for i, (p0, p1) in enumerate(zip(points, points[1:] + points[:1])):
        n = normals[i]
        q0 = p0 + n * radius
        q1 = p1 + n * radius
        if length(q1 - q0) > EPS:
            lines.append(Line(q0.x, q0.y, q1.x, q1.y))

    for i, point in enumerate(points):
        prev_normal = normals[(i - 1) % count]
        next_normal = normals[i]
        # For a counter-clockwise source polygon, the outward normal rotates
        # counter-clockwise around convex corners from previous to next.
        arcs.append(Arc(point.x, point.y, radius, angle_of(prev_normal), angle_of(next_normal)))


def add_offset_segment(p0: Point, p1: Point, radius: float, lines: list[Line], arcs: list[Arc]) -> None:
    direction = normalize(p1 - p0)
    if length(direction) <= EPS:
        arcs.append(Arc(p0.x, p0.y, radius, 0.0, TAU))
        return

    normal = Point(direction.y, -direction.x)
    a0 = p0 + normal * radius
    a1 = p1 + normal * radius
    b0 = p0 - normal * radius
    b1 = p1 - normal * radius
    lines.append(Line(a0.x, a0.y, a1.x, a1.y))
    lines.append(Line(b1.x, b1.y, b0.x, b0.y))

    n0 = angle_of(normal)
    n1 = angle_of(Point(-normal.x, -normal.y))
    arcs.append(Arc(p1.x, p1.y, radius, n0, n1))
    arcs.append(Arc(p0.x, p0.y, radius, n1, n0))


def add_collider(collider: dict[str, Any], radius: float, lines: list[Line], arcs: list[Arc]) -> str | None:
    shape = collider.get("shape") or {}
    collider_type = str(shape.get("colliderType") or collider.get("type") or "")

    if "CircleCollider2D" in collider_type:
        center = vec_from_json(shape["mapCenter"])
        source_radius = float(shape.get("mapRadiusByLossyScaleMax", shape.get("radius", 0.0)))
        arcs.append(Arc(center.x, center.y, source_radius + radius, 0.0, TAU))
        return None

    if "BoxCollider2D" in collider_type:
        points = [vec_from_json(p) for p in shape.get("mapCorners", [])]
        add_offset_polygon(points, radius, lines, arcs)
        return None

    if "PolygonCollider2D" in collider_type:
        paths = shape.get("mapPaths", [])
        if not paths:
            return "PolygonCollider2D has no mapPaths"
        for path in paths:
            add_offset_polygon([vec_from_json(p) for p in path], radius, lines, arcs)
        return None

    if "EdgeCollider2D" in collider_type:
        points = [vec_from_json(p) for p in shape.get("mapPoints", [])]
        for p0, p1 in zip(points, points[1:]):
            add_offset_segment(p0, p1, radius, lines, arcs)
        return None

    if "CapsuleCollider2D" in collider_type:
        # The dump already provides mapBoxCorners.  Treating it as a rounded box
        # approximation is useful for debugging, but it is not an exact capsule
        # offset unless the source dump is extended with the capsule centerline.
        points = [vec_from_json(p) for p in shape.get("mapBoxCorners", [])]
        add_offset_polygon(points, radius, lines, arcs)
        return "CapsuleCollider2D approximated from mapBoxCorners"

    return f"Unsupported collider type: {collider_type}"


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


def build_bvh(primitives: list[Primitive]) -> list[BvhNode]:
    nodes: list[BvhNode] = []

    def primitive_weight(index: int) -> float:
        return 1.0 if isinstance(primitives[index], Line) else ARC_TRAVERSAL_WEIGHT

    def primitive_max_axis(index: int, split_x: bool) -> float:
        return primitives[index].bounds[2] if split_x else primitives[index].bounds[3]

    def choose_sah_split(indices: list[int], bounds: tuple[float, float, float, float]) -> tuple[bool, float] | None:
        best: tuple[float, bool, float] | None = None
        parent_area = max(bounds_area(bounds), EPS)
        for split_x in (True, False):
            for plane in sorted(set(primitive_max_axis(index, split_x) for index in indices)):
                left = [index for index in indices if primitive_max_axis(index, split_x) <= plane + EPS]
                right = [index for index in indices if primitive_max_axis(index, split_x) > plane + EPS]
                if not left or not right:
                    continue
                left_bounds = merge_bounds(primitives[index].bounds for index in left)
                right_bounds = merge_bounds(primitives[index].bounds for index in right)
                left_weight = sum(primitive_weight(index) for index in left)
                right_weight = sum(primitive_weight(index) for index in right)
                cost = (bounds_area(left_bounds) * left_weight + bounds_area(right_bounds) * right_weight) / parent_area
                if best is None or cost < best[0]:
                    best = (cost, split_x, plane)
        if best is None:
            return None
        return best[1], best[2]

    def add_node(indices: list[int]) -> int:
        bounds = merge_bounds(primitives[i].bounds for i in indices)
        node_index = len(nodes)
        nodes.append(BvhNode(bounds))

        if len(indices) <= MAX_LEAF_ITEMS:
            nodes[node_index] = BvhNode(bounds, items=tuple(indices))
            return node_index

        split = choose_sah_split(indices, bounds)
        if split is None:
            min_x, min_y, max_x, max_y = bounds
            split_x = (max_x - min_x) >= (max_y - min_y)
            indices.sort(key=lambda i: (primitives[i].center.x if split_x else primitives[i].center.y, i))
            mid = len(indices) // 2
            left_indices = indices[:mid]
            right_indices = indices[mid:]
        else:
            split_x, plane = split
            left_indices = [index for index in indices if primitive_max_axis(index, split_x) <= plane + EPS]
            right_indices = [index for index in indices if primitive_max_axis(index, split_x) > plane + EPS]

        if not left_indices or not right_indices:
            min_x, min_y, max_x, max_y = bounds
            split_x = (max_x - min_x) >= (max_y - min_y)
            indices.sort(key=lambda i: (primitives[i].center.x if split_x else primitives[i].center.y, i))
            mid = len(indices) // 2
            left_indices = indices[:mid]
            right_indices = indices[mid:]

        left = add_node(left_indices)
        right = add_node(right_indices)
        nodes[node_index] = BvhNode(bounds, left=left, right=right)
        return node_index

    if primitives:
        root = add_node(list(range(len(primitives))))
        if root != 0:
            raise RuntimeError("BVH root was not written at index 0")
    return nodes


def write_bin(path: Path, lines: list[Line], arcs: list[Arc], nodes: list[BvhNode]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("wb") as stream:
        stream.write(struct.pack("<iii", len(lines), len(arcs), len(nodes)))

        for line in lines:
            stream.write(struct.pack("<dddd", line.x1, line.y1, line.x2, line.y2))
        for arc in arcs:
            stream.write(struct.pack("<ddddd", arc.x, arc.y, arc.r, arc.start, arc.end))

        for node in nodes:
            stream.write(struct.pack("<?7xdddd", node.is_leaf, *node.bounds))
            if node.is_leaf:
                stream.write(struct.pack("<i", len(node.items)))
                for item in node.items:
                    stream.write(struct.pack("<i", int(item)))
            else:
                # supress pylance error.
                if node.left is None or node.right is None:
                    raise RuntimeError("Internal BVH node is missing child indices")
                stream.write(struct.pack("<ii", node.left, node.right))


def write_summary(
    path: Path,
    dump_path: Path,
    output_path: Path,
    radius: float,
    selected_count: int,
    lines: list[Line],
    arcs: list[Arc],
    nodes: list[BvhNode],
    warnings: list[str],
) -> None:
    path.write_text(
        json.dumps(
            {
                "sourceDump": str(dump_path),
                "outputBin": str(output_path),
                "indicatorRadius": radius,
                "selectedColliderCount": selected_count,
                "lineCount": len(lines),
                "arcCount": len(arcs),
                "nodeCount": len(nodes),
                "warnings": warnings[:200],
                "warningCount": len(warnings),
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate an AlchAssV3 zone bin from a runtime collider dump.")
    parser.add_argument("dump", type=Path, help="RuntimeDumps/map_colliders_*.json")
    parser.add_argument("output", type=Path, help="Output .bin path")
    parser.add_argument(
        "--include",
        action="append",
        default=None,
        help="Only include colliders whose path contains this text. May be repeated. Default: StrongDangerZoneContainer",
    )
    parser.add_argument("--exclude", action="append", default=[], help="Exclude colliders whose path contains this text.")
    parser.add_argument("--radius", type=float, default=None, help="Potion/indicator radius. Default: dump.indicatorCollider.radius")
    parser.add_argument("--include-inactive", action="store_true", help="Include inactive/disabled colliders.")
    parser.add_argument("--summary", type=Path, default=None, help="Optional JSON summary path.")
    args = parser.parse_args()

    dump = json.loads(args.dump.read_text(encoding="utf-8-sig"))
    radius = args.radius
    if radius is None:
        radius = float(dump.get("indicatorCollider", {}).get("radius", 0.74))

    includes = args.include if args.include is not None else ["StrongDangerZoneContainer"]
    colliders = selected_colliders(dump, includes, args.exclude, args.include_inactive)

    lines: list[Line] = []
    arcs: list[Arc] = []
    warnings: list[str] = []

    for collider in colliders:
        warning = add_collider(collider, radius, lines, arcs)
        if warning is not None:
            warnings.append(f"{collider.get('path', '<unknown>')}: {warning}")

    primitives: list[Primitive] = [*lines, *arcs]
    nodes = build_bvh(primitives)
    write_bin(args.output, lines, arcs, nodes)

    summary_path = args.summary or args.output.with_suffix(args.output.suffix + ".summary.json")
    write_summary(
        summary_path,
        args.dump,
        args.output,
        radius,
        len(colliders),
        lines,
        arcs,
        nodes,
        warnings,
    )

    print(f"Wrote {args.output}")
    print(f"Selected colliders: {len(colliders)}")
    print(f"Lines: {len(lines)}")
    print(f"Arcs: {len(arcs)}")
    print(f"BVH nodes: {len(nodes)}")
    print(f"Summary: {summary_path}")
    if warnings:
        print(f"Warnings: {len(warnings)}; see summary for details.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
