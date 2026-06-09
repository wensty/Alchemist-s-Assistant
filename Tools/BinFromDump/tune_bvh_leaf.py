#!/usr/bin/env python3
"""Tune BVH leaf sizes by delegating BVH construction to CgalBinBuilder."""

from __future__ import annotations

import argparse
import math
import shutil
import struct
import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path

ARC_WEIGHT = 1.2
NODE_WEIGHT = 0.15


@dataclass(frozen=True)
class Point:
    x: float
    y: float


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
    def weight(self) -> float:
        return 1.0


@dataclass(frozen=True)
class Arc:
    x: float
    y: float
    r: float
    start: float
    end: float

    @property
    def bounds(self) -> tuple[float, float, float, float]:
        return (self.x - self.r, self.y - self.r, self.x + self.r, self.y + self.r)

    @property
    def weight(self) -> float:
        return ARC_WEIGHT


@dataclass(frozen=True)
class Node:
    bounds: tuple[float, float, float, float]
    left: int | None = None
    right: int | None = None
    items: tuple[int, ...] = ()

    @property
    def is_leaf(self) -> bool:
        return bool(self.items)


def read_bin(path: Path) -> tuple[list[Line], list[Arc], list[Node]]:
    data = path.read_bytes()
    offset = 0
    line_count, arc_count, node_count = struct.unpack_from("<iii", data, offset)
    offset += 12

    lines: list[Line] = []
    arcs: list[Arc] = []
    nodes: list[Node] = []

    for _ in range(line_count):
        lines.append(Line(*struct.unpack_from("<dddd", data, offset)))
        offset += 32
    for _ in range(arc_count):
        arcs.append(Arc(*struct.unpack_from("<ddddd", data, offset)))
        offset += 40

    for _ in range(node_count):
        is_leaf, min_x, min_y, max_x, max_y = struct.unpack_from("<?7xdddd", data, offset)
        offset += 40
        bounds = (min_x, min_y, max_x, max_y)
        if is_leaf:
            item_count = struct.unpack_from("<i", data, offset)[0]
            offset += 4
            items = struct.unpack_from(f"<{item_count}i", data, offset)
            offset += item_count * 4
            nodes.append(Node(bounds, items=tuple(items)))
        else:
            left, right = struct.unpack_from("<ii", data, offset)
            offset += 8
            nodes.append(Node(bounds, left=left, right=right))

    return lines, arcs, nodes


def line_aabb(p0: Point, p1: Point, bounds: tuple[float, float, float, float]) -> bool:
    dx = p1.x - p0.x
    dy = p1.y - p0.y
    tmin = 0.0
    tmax = 1.0
    for p, d, lo, hi in ((p0.x, dx, bounds[0], bounds[2]), (p0.y, dy, bounds[1], bounds[3])):
        if abs(d) < 1e-9:
            if p < lo or p > hi:
                return False
            continue
        inv = 1.0 / d
        t1 = (lo - p) * inv
        t2 = (hi - p) * inv
        if t1 > t2:
            t1, t2 = t2, t1
        tmin = max(tmin, t1)
        tmax = min(tmax, t2)
        if tmin > tmax:
            return False
    return True


def circle_aabb(center: Point, radius: float, bounds: tuple[float, float, float, float]) -> bool:
    cx = max(bounds[0], min(center.x, bounds[2]))
    cy = max(bounds[1], min(center.y, bounds[3]))
    dx = center.x - cx
    dy = center.y - cy
    return dx * dx + dy * dy <= radius * radius


def query_samples(root_bounds: tuple[float, float, float, float]):
    min_x, min_y, max_x, max_y = root_bounds
    width = max_x - min_x
    height = max_y - min_y
    diag = math.hypot(width, height)

    for i in range(17):
        t = i / 16
        y = min_y + height * t
        x = min_x + width * t
        yield ("line", Point(min_x - width * 0.05, y), Point(max_x + width * 0.05, y))
        yield ("line", Point(x, min_y - height * 0.05), Point(x, max_y + height * 0.05))

    for i in range(9):
        t = i / 8
        yield ("line", Point(min_x, min_y + height * t), Point(max_x, max_y - height * t))

    for ix in range(5):
        for iy in range(5):
            center = Point(min_x + width * ix / 4, min_y + height * iy / 4)
            for radius in (diag * 0.03, diag * 0.08, diag * 0.18):
                yield ("circle", center, radius)


def estimate_query_cost(primitives: list[Line | Arc], nodes: list[Node]) -> tuple[float, float, float]:
    if not nodes:
        return 0.0, 0.0, 0.0

    total_nodes = 0
    total_items = 0.0
    queries = list(query_samples(nodes[0].bounds))

    for query in queries:
        stack = [0]
        while stack:
            node = nodes[stack.pop()]
            if query[0] == "line":
                hit = line_aabb(query[1], query[2], node.bounds)
            else:
                hit = circle_aabb(query[1], query[2], node.bounds)
            if not hit:
                continue

            total_nodes += 1
            if node.is_leaf:
                total_items += sum(primitives[index].weight for index in node.items)
            else:
                if node.left is None or node.right is None:
                    raise RuntimeError("Internal BVH node is missing child indices")
                stack.append(node.left)
                stack.append(node.right)

    count = len(queries)
    avg_nodes = total_nodes / count
    avg_items = total_items / count
    return avg_nodes * NODE_WEIGHT + avg_items, avg_nodes, avg_items


def rebuild_with_cpp(builder: Path, source: Path, output: Path, leaf: int) -> None:
    subprocess.run(
        [
            str(builder),
            "--rebuild-bin",
            str(source),
            str(output),
            "--max-leaf-items",
            str(leaf),
        ],
        check=True,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.PIPE,
        text=True,
    )


def tune_one(builder: Path, path: Path, leaves: list[int], apply: bool) -> None:
    rows = []
    with tempfile.TemporaryDirectory(prefix="alchass_bvh_") as temp_dir_name:
        temp_dir = Path(temp_dir_name)
        for leaf in leaves:
            candidate = temp_dir / f"{path.stem}_leaf{leaf}.bin"
            rebuild_with_cpp(builder, path, candidate, leaf)
            lines, arcs, nodes = read_bin(candidate)
            primitives: list[Line | Arc] = [*lines, *arcs]
            score, avg_nodes, avg_items = estimate_query_cost(primitives, nodes)
            rows.append((score, leaf, candidate, len(nodes), avg_nodes, avg_items, candidate.stat().st_size))

        best = min(rows, key=lambda row: (row[0], row[6]))
        print(path)
        for score, leaf, _, node_count, avg_nodes, avg_items, byte_count in rows:
            mark = "*" if leaf == best[1] else " "
            print(
                f" {mark} leaf={leaf:<2} score={score:9.3f} "
                f"avg_nodes={avg_nodes:8.2f} avg_items={avg_items:8.2f} "
                f"nodes={node_count:6} bytes={byte_count}"
            )

        if apply:
            shutil.copyfile(best[2], path)
            print(f" applied leaf={best[1]}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Tune BVH leaf sizes for existing zone .bin files.")
    parser.add_argument("bins", nargs="+", type=Path)
    parser.add_argument("--leaf", nargs="+", type=int, default=[1, 2, 3, 4, 5, 6, 8, 10, 12, 16])
    parser.add_argument("--apply", action="store_true")
    parser.add_argument(
        "--builder",
        type=Path,
        default=Path("build/CgalBinBuilder/Release/CgalBinBuilder.exe"),
    )
    args = parser.parse_args()

    for leaf in args.leaf:
        if leaf <= 0:
            parser.error("--leaf values must be positive")
    if not args.builder.exists():
        parser.error(f"CgalBinBuilder not found: {args.builder}")

    for path in args.bins:
        tune_one(args.builder, path, args.leaf, args.apply)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
