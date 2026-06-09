#include <CGAL/Exact_predicates_exact_constructions_kernel.h>

#include <boost/property_tree/json_parser.hpp>
#include <boost/property_tree/ptree.hpp>

#include <algorithm>
#include <array>
#include <cmath>
#include <cstdint>
#include <filesystem>
#include <fstream>
#include <iostream>
#include <limits>
#include <optional>
#include <stdexcept>
#include <string>
#include <string_view>
#include <variant>
#include <vector>

namespace fs = std::filesystem;
namespace pt = boost::property_tree;

using Kernel = CGAL::Exact_predicates_exact_constructions_kernel;

constexpr double Pi = 3.141592653589793238462643383279502884;
constexpr double Tau = Pi * 2.0;
constexpr double Eps = 1e-10;
constexpr std::size_t MaxLeafItems = 4;
constexpr double ArcTraversalWeight = 1.2;

struct Point
{
    double x = 0.0;
    double y = 0.0;
};

struct Line
{
    double x1 = 0.0;
    double y1 = 0.0;
    double x2 = 0.0;
    double y2 = 0.0;
};

struct Arc
{
    double x = 0.0;
    double y = 0.0;
    double r = 0.0;
    double start = 0.0;
    double end = 0.0;
};

struct Bounds
{
    double minX = std::numeric_limits<double>::infinity();
    double minY = std::numeric_limits<double>::infinity();
    double maxX = -std::numeric_limits<double>::infinity();
    double maxY = -std::numeric_limits<double>::infinity();
};

struct CandidateLine
{
    Line line;
    int owner = -1;
};

struct CandidateArc
{
    Arc arc;
    int owner = -1;
};

enum class InflatedShapeKind
{
    Disk,
    Polygon,
    Segment,
};

struct InflatedShape
{
    InflatedShapeKind kind = InflatedShapeKind::Disk;
    int owner = -1;
    Point center;
    double radius = 0.0;
    Point p0;
    Point p1;
    std::vector<Point> polygon;
    Bounds bounds;
};

struct PrimitiveRef
{
    bool isLine = true;
    int index = 0;
    Bounds bounds;
    Point center;
};

struct BvhNode
{
    bool isLeaf = false;
    Bounds bounds;
    int left = -1;
    int right = -1;
    std::vector<int> items;
};

struct Options
{
    fs::path dumpPath;
    fs::path outputPath;
    fs::path rebuildInputPath;
    std::vector<std::string> includes = { "StrongDangerZoneContainer" };
    std::vector<std::string> excludes;
    double radius = std::numeric_limits<double>::quiet_NaN();
    bool includeInactive = false;
    bool rebuildExistingBin = false;
    std::string mode = "per-collider";
    double boundaryStep = 0.05;
    double coverageEpsilon = 1e-7;
    double nonCircleExtraOffset = 0.0;
    std::size_t maxLeafItems = MaxLeafItems;
};

Point operator+(Point a, Point b) { return { a.x + b.x, a.y + b.y }; }
Point operator-(Point a, Point b) { return { a.x - b.x, a.y - b.y }; }
Point operator*(Point a, double s) { return { a.x * s, a.y * s }; }

double length(Point p)
{
    return std::hypot(p.x, p.y);
}

Point normalize(Point p)
{
    const double len = length(p);
    if (len <= Eps)
        return {};
    return { p.x / len, p.y / len };
}

double normalizeAngle(double angle)
{
    angle = std::fmod(angle, Tau);
    if (angle < 0)
        angle += Tau;
    return angle;
}

double angleOf(Point p)
{
    return normalizeAngle(std::atan2(p.y, p.x));
}

Bounds merge(Bounds a, Bounds b)
{
    return {
        std::min(a.minX, b.minX),
        std::min(a.minY, b.minY),
        std::max(a.maxX, b.maxX),
        std::max(a.maxY, b.maxY),
    };
}

double boundsArea(Bounds bounds)
{
    const double width = std::max(0.0, bounds.maxX - bounds.minX);
    const double height = std::max(0.0, bounds.maxY - bounds.minY);
    return width * height;
}

Bounds lineBounds(Line line)
{
    return {
        std::min(line.x1, line.x2),
        std::min(line.y1, line.y2),
        std::max(line.x1, line.x2),
        std::max(line.y1, line.y2),
    };
}

Bounds arcBounds(Arc arc)
{
    // Conservative full-circle bounds. Runtime intersection checks remain exact.
    return { arc.x - arc.r, arc.y - arc.r, arc.x + arc.r, arc.y + arc.r };
}

Bounds pointBounds(Point point, double padding)
{
    return { point.x - padding, point.y - padding, point.x + padding, point.y + padding };
}

Bounds segmentBounds(Point p0, Point p1, double padding)
{
    return {
        std::min(p0.x, p1.x) - padding,
        std::min(p0.y, p1.y) - padding,
        std::max(p0.x, p1.x) + padding,
        std::max(p0.y, p1.y) + padding,
    };
}

Bounds polygonBounds(const std::vector<Point>& points, double padding)
{
    Bounds bounds;
    for (Point point : points)
        bounds = merge(bounds, pointBounds(point, padding));
    return bounds;
}

bool boundsContains(Bounds bounds, Point point, double padding)
{
    return point.x >= bounds.minX - padding &&
        point.x <= bounds.maxX + padding &&
        point.y >= bounds.minY - padding &&
        point.y <= bounds.maxY + padding;
}

Point lineCenter(Line line)
{
    return { (line.x1 + line.x2) * 0.5, (line.y1 + line.y2) * 0.5 };
}

Point arcCenter(Arc arc)
{
    return { arc.x, arc.y };
}

Point readPoint(const pt::ptree& tree)
{
    return { tree.get<double>("x"), tree.get<double>("y") };
}

bool containsAny(std::string_view text, const std::vector<std::string>& needles)
{
    return std::ranges::any_of(needles, [&](const std::string& needle) {
        return text.find(needle) != std::string_view::npos;
    });
}

double signedArea(std::vector<Point> points)
{
    double area = 0.0;
    for (std::size_t i = 0; i < points.size(); ++i)
    {
        const Point a = points[i];
        const Point b = points[(i + 1) % points.size()];
        area += a.x * b.y - b.x * a.y;
    }
    return area * 0.5;
}

double distanceToSegment(Point point, Point p0, Point p1)
{
    const Point edge = p1 - p0;
    const double len2 = edge.x * edge.x + edge.y * edge.y;
    if (len2 <= Eps)
        return length(point - p0);

    const double t = std::clamp(((point.x - p0.x) * edge.x + (point.y - p0.y) * edge.y) / len2, 0.0, 1.0);
    const Point projected = p0 + edge * t;
    return length(point - projected);
}

bool pointInPolygon(Point point, const std::vector<Point>& polygon)
{
    if (polygon.size() < 3)
        return false;

    bool inside = false;
    for (std::size_t i = 0, j = polygon.size() - 1; i < polygon.size(); j = i++)
    {
        const Point pi = polygon[i];
        const Point pj = polygon[j];
        const bool crosses = ((pi.y > point.y) != (pj.y > point.y)) &&
            (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + 0.0) + pi.x);
        if (crosses)
            inside = !inside;
    }
    return inside;
}

bool containsInflatedShape(const InflatedShape& shape, Point point, double epsilon)
{
    if (!boundsContains(shape.bounds, point, epsilon))
        return false;

    switch (shape.kind)
    {
    case InflatedShapeKind::Disk:
        return length(point - shape.center) <= shape.radius + epsilon;
    case InflatedShapeKind::Segment:
        return distanceToSegment(point, shape.p0, shape.p1) <= shape.radius + epsilon;
    case InflatedShapeKind::Polygon:
        if (pointInPolygon(point, shape.polygon))
            return true;
        for (std::size_t i = 0; i < shape.polygon.size(); ++i)
        {
            const Point p0 = shape.polygon[i];
            const Point p1 = shape.polygon[(i + 1) % shape.polygon.size()];
            if (distanceToSegment(point, p0, p1) <= shape.radius + epsilon)
                return true;
        }
        return false;
    }
    return false;
}

bool coveredByOtherShape(Point point, int owner, const std::vector<InflatedShape>& shapes, double epsilon)
{
    for (const InflatedShape& shape : shapes)
    {
        if (shape.owner == owner)
            continue;
        if (containsInflatedShape(shape, point, epsilon))
            return true;
    }
    return false;
}

std::vector<Point> outwardNormals(std::vector<Point>& points)
{
    if (signedArea(points) < 0)
        std::ranges::reverse(points);

    std::vector<Point> normals;
    normals.reserve(points.size());
    for (std::size_t i = 0; i < points.size(); ++i)
    {
        const Point p0 = points[i];
        const Point p1 = points[(i + 1) % points.size()];
        const Point edge = p1 - p0;
        normals.push_back(normalize({ edge.y, -edge.x }));
    }
    return normals;
}

void addOffsetSegment(Point p0, Point p1, double radius, std::vector<Line>& lines, std::vector<Arc>& arcs)
{
    const Point direction = normalize(p1 - p0);
    if (length(direction) <= Eps)
    {
        arcs.push_back({ p0.x, p0.y, radius, 0.0, Tau });
        return;
    }

    const Point normal = { direction.y, -direction.x };
    const Point a0 = p0 + normal * radius;
    const Point a1 = p1 + normal * radius;
    const Point b0 = p0 - normal * radius;
    const Point b1 = p1 - normal * radius;

    lines.push_back({ a0.x, a0.y, a1.x, a1.y });
    lines.push_back({ b1.x, b1.y, b0.x, b0.y });

    const double n0 = angleOf(normal);
    const double n1 = angleOf({ -normal.x, -normal.y });
    arcs.push_back({ p1.x, p1.y, radius, n0, n1 });
    arcs.push_back({ p0.x, p0.y, radius, n1, n0 });
}

void addOffsetPolygon(std::vector<Point> points, double radius, std::vector<Line>& lines, std::vector<Arc>& arcs)
{
    if (points.size() < 2)
        return;
    if (points.size() == 2)
    {
        addOffsetSegment(points[0], points[1], radius, lines, arcs);
        return;
    }

    const std::vector<Point> normals = outwardNormals(points);
    const std::size_t count = points.size();
    for (std::size_t i = 0; i < count; ++i)
    {
        const Point p0 = points[i];
        const Point p1 = points[(i + 1) % count];
        const Point n = normals[i];
        const Point q0 = p0 + n * radius;
        const Point q1 = p1 + n * radius;
        if (length(q1 - q0) > Eps)
            lines.push_back({ q0.x, q0.y, q1.x, q1.y });
    }

    for (std::size_t i = 0; i < count; ++i)
    {
        const Point prevNormal = normals[(i + count - 1) % count];
        const Point nextNormal = normals[i];
        const Point p = points[i];
        arcs.push_back({ p.x, p.y, radius, angleOf(prevNormal), angleOf(nextNormal) });
    }
}

std::vector<Point> readPointArray(const pt::ptree& array)
{
    std::vector<Point> result;
    for (const auto& item : array)
        result.push_back(readPoint(item.second));
    return result;
}

std::optional<std::string> addColliderBoundary(const pt::ptree& collider, double radius, double nonCircleExtraOffset, std::vector<Line>& lines, std::vector<Arc>& arcs)
{
    const auto& shape = collider.get_child("shape");
    const std::string colliderType = shape.get<std::string>("colliderType", collider.get<std::string>("type", ""));
    const double nonCircleRadius = radius + nonCircleExtraOffset;

    if (colliderType.find("CircleCollider2D") != std::string::npos)
    {
        const Point center = readPoint(shape.get_child("mapCenter"));
        const double sourceRadius = shape.get<double>("mapRadiusByLossyScaleMax", shape.get<double>("radius", 0.0));
        arcs.push_back({ center.x, center.y, sourceRadius + radius, 0.0, Tau });
        return std::nullopt;
    }

    if (colliderType.find("BoxCollider2D") != std::string::npos)
    {
        addOffsetPolygon(readPointArray(shape.get_child("mapCorners")), nonCircleRadius, lines, arcs);
        return std::nullopt;
    }

    if (colliderType.find("PolygonCollider2D") != std::string::npos)
    {
        const auto child = shape.get_child_optional("mapPaths");
        if (!child)
            return "PolygonCollider2D has no mapPaths";
        for (const auto& path : *child)
            addOffsetPolygon(readPointArray(path.second), nonCircleRadius, lines, arcs);
        return std::nullopt;
    }

    if (colliderType.find("EdgeCollider2D") != std::string::npos)
    {
        const auto points = readPointArray(shape.get_child("mapPoints"));
        for (std::size_t i = 1; i < points.size(); ++i)
            addOffsetSegment(points[i - 1], points[i], nonCircleRadius, lines, arcs);
        return std::nullopt;
    }

    if (colliderType.find("CapsuleCollider2D") != std::string::npos)
    {
        const auto child = shape.get_child_optional("mapBoxCorners");
        if (!child)
            return "CapsuleCollider2D has no mapBoxCorners";
        addOffsetPolygon(readPointArray(*child), nonCircleRadius, lines, arcs);
        return "CapsuleCollider2D approximated from mapBoxCorners";
    }

    return "Unsupported collider type: " + colliderType;
}

void appendCandidates(int owner, const std::vector<Line>& tempLines, const std::vector<Arc>& tempArcs, std::vector<CandidateLine>& lines, std::vector<CandidateArc>& arcs)
{
    for (Line line : tempLines)
        lines.push_back({ line, owner });
    for (Arc arc : tempArcs)
        arcs.push_back({ arc, owner });
}

std::optional<std::string> addColliderAnalysis(
    const pt::ptree& collider,
    int owner,
    double radius,
    double nonCircleExtraOffset,
    std::vector<InflatedShape>& shapes,
    std::vector<CandidateLine>& candidateLines,
    std::vector<CandidateArc>& candidateArcs)
{
    const auto& shape = collider.get_child("shape");
    const std::string colliderType = shape.get<std::string>("colliderType", collider.get<std::string>("type", ""));
    const double nonCircleRadius = radius + nonCircleExtraOffset;
    std::vector<Line> tempLines;
    std::vector<Arc> tempArcs;

    if (colliderType.find("CircleCollider2D") != std::string::npos)
    {
        const Point center = readPoint(shape.get_child("mapCenter"));
        const double sourceRadius = shape.get<double>("mapRadiusByLossyScaleMax", shape.get<double>("radius", 0.0));
        const double inflatedRadius = sourceRadius + radius;
        shapes.push_back({ InflatedShapeKind::Disk, owner, center, inflatedRadius, {}, {}, {}, pointBounds(center, inflatedRadius) });
        tempArcs.push_back({ center.x, center.y, inflatedRadius, 0.0, Tau });
        appendCandidates(owner, tempLines, tempArcs, candidateLines, candidateArcs);
        return std::nullopt;
    }

    if (colliderType.find("BoxCollider2D") != std::string::npos)
    {
        std::vector<Point> points = readPointArray(shape.get_child("mapCorners"));
        shapes.push_back({ InflatedShapeKind::Polygon, owner, {}, nonCircleRadius, {}, {}, points, polygonBounds(points, nonCircleRadius) });
        addOffsetPolygon(std::move(points), nonCircleRadius, tempLines, tempArcs);
        appendCandidates(owner, tempLines, tempArcs, candidateLines, candidateArcs);
        return std::nullopt;
    }

    if (colliderType.find("PolygonCollider2D") != std::string::npos)
    {
        const auto child = shape.get_child_optional("mapPaths");
        if (!child)
            return "PolygonCollider2D has no mapPaths";
        for (const auto& path : *child)
        {
            std::vector<Point> points = readPointArray(path.second);
            shapes.push_back({ InflatedShapeKind::Polygon, owner, {}, nonCircleRadius, {}, {}, points, polygonBounds(points, nonCircleRadius) });
            addOffsetPolygon(std::move(points), nonCircleRadius, tempLines, tempArcs);
        }
        appendCandidates(owner, tempLines, tempArcs, candidateLines, candidateArcs);
        return std::nullopt;
    }

    if (colliderType.find("EdgeCollider2D") != std::string::npos)
    {
        const std::vector<Point> points = readPointArray(shape.get_child("mapPoints"));
        for (std::size_t i = 1; i < points.size(); ++i)
        {
            shapes.push_back({ InflatedShapeKind::Segment, owner, {}, nonCircleRadius, points[i - 1], points[i], {}, segmentBounds(points[i - 1], points[i], nonCircleRadius) });
            addOffsetSegment(points[i - 1], points[i], nonCircleRadius, tempLines, tempArcs);
        }
        appendCandidates(owner, tempLines, tempArcs, candidateLines, candidateArcs);
        return std::nullopt;
    }

    if (colliderType.find("CapsuleCollider2D") != std::string::npos)
    {
        const auto child = shape.get_child_optional("mapBoxCorners");
        if (!child)
            return "CapsuleCollider2D has no mapBoxCorners";
        std::vector<Point> points = readPointArray(*child);
        shapes.push_back({ InflatedShapeKind::Polygon, owner, {}, nonCircleRadius, {}, {}, points, polygonBounds(points, nonCircleRadius) });
        addOffsetPolygon(std::move(points), nonCircleRadius, tempLines, tempArcs);
        appendCandidates(owner, tempLines, tempArcs, candidateLines, candidateArcs);
        return "CapsuleCollider2D approximated from mapBoxCorners";
    }

    return "Unsupported collider type: " + colliderType;
}

Point pointOnLine(Line line, double t)
{
    return {
        line.x1 + (line.x2 - line.x1) * t,
        line.y1 + (line.y2 - line.y1) * t,
    };
}

double arcSpan(Arc arc)
{
    if (std::abs(arc.end - Tau) <= Eps && std::abs(arc.start) <= Eps)
        return Tau;
    return arc.end >= arc.start ? arc.end - arc.start : arc.end + Tau - arc.start;
}

double angleAt(Arc arc, double t)
{
    return arc.start + arcSpan(arc) * t;
}

Point pointOnArc(Arc arc, double t)
{
    const double angle = angleAt(arc, t);
    return { arc.x + std::cos(angle) * arc.r, arc.y + std::sin(angle) * arc.r };
}

Arc arcInterval(Arc arc, double t0, double t1)
{
    const double rawStart = angleAt(arc, t0);
    const double rawEnd = angleAt(arc, t1);
    if (rawEnd - rawStart >= Tau - 1e-8)
        return { arc.x, arc.y, arc.r, 0.0, Tau };

    const double start = normalizeAngle(rawStart);
    const double end = normalizeAngle(rawEnd);
    return { arc.x, arc.y, arc.r, start, end };
}

void filterCandidateLine(const CandidateLine& candidate, const std::vector<InflatedShape>& shapes, const Options& options, std::vector<Line>& output)
{
    const double len = length({ candidate.line.x2 - candidate.line.x1, candidate.line.y2 - candidate.line.y1 });
    const int pieces = std::max(1, static_cast<int>(std::ceil(len / options.boundaryStep)));
    std::optional<double> visibleStart;

    for (int i = 0; i < pieces; ++i)
    {
        const double t0 = static_cast<double>(i) / pieces;
        const double t1 = static_cast<double>(i + 1) / pieces;
        const Point mid = pointOnLine(candidate.line, (t0 + t1) * 0.5);
        const bool visible = !coveredByOtherShape(mid, candidate.owner, shapes, options.coverageEpsilon);

        if (visible && !visibleStart)
            visibleStart = t0;
        if ((!visible || i == pieces - 1) && visibleStart)
        {
            const double end = visible ? t1 : t0;
            if (end > *visibleStart + Eps)
            {
                const Point p0 = pointOnLine(candidate.line, *visibleStart);
                const Point p1 = pointOnLine(candidate.line, end);
                output.push_back({ p0.x, p0.y, p1.x, p1.y });
            }
            visibleStart.reset();
        }
    }
}

void filterCandidateArc(const CandidateArc& candidate, const std::vector<InflatedShape>& shapes, const Options& options, std::vector<Arc>& output)
{
    const double span = arcSpan(candidate.arc);
    const double len = std::abs(span * candidate.arc.r);
    const int pieces = std::max(1, static_cast<int>(std::ceil(len / options.boundaryStep)));
    std::optional<double> visibleStart;

    for (int i = 0; i < pieces; ++i)
    {
        const double t0 = static_cast<double>(i) / pieces;
        const double t1 = static_cast<double>(i + 1) / pieces;
        const Point mid = pointOnArc(candidate.arc, (t0 + t1) * 0.5);
        const bool visible = !coveredByOtherShape(mid, candidate.owner, shapes, options.coverageEpsilon);

        if (visible && !visibleStart)
            visibleStart = t0;
        if ((!visible || i == pieces - 1) && visibleStart)
        {
            const double end = visible ? t1 : t0;
            if (end > *visibleStart + Eps)
                output.push_back(arcInterval(candidate.arc, *visibleStart, end));
            visibleStart.reset();
        }
    }
}

void filterBoundaryCandidates(
    const std::vector<CandidateLine>& candidateLines,
    const std::vector<CandidateArc>& candidateArcs,
    const std::vector<InflatedShape>& shapes,
    const Options& options,
    std::vector<Line>& lines,
    std::vector<Arc>& arcs)
{
    for (const CandidateLine& line : candidateLines)
        filterCandidateLine(line, shapes, options, lines);
    for (const CandidateArc& arc : candidateArcs)
        filterCandidateArc(arc, shapes, options, arcs);
}

struct SplitChoice
{
    bool valid = false;
    bool splitX = true;
    double plane = 0.0;
    double cost = std::numeric_limits<double>::infinity();
};

double primitiveWeight(const PrimitiveRef& primitive)
{
    return primitive.isLine ? 1.0 : ArcTraversalWeight;
}

double primitiveMaxAxis(const PrimitiveRef& primitive, bool splitX)
{
    return splitX ? primitive.bounds.maxX : primitive.bounds.maxY;
}

SplitChoice chooseSahSplit(const std::vector<PrimitiveRef>& primitives, const std::vector<int>& indices, Bounds nodeBounds)
{
    SplitChoice best;

    auto evaluateAxis = [&](bool splitX) {
        std::vector<int> sorted = indices;
        std::ranges::sort(sorted, [&](int a, int b) {
            const double ca = primitiveMaxAxis(primitives[a], splitX);
            const double cb = primitiveMaxAxis(primitives[b], splitX);
            if (std::abs(ca - cb) > Eps)
                return ca < cb;
            return a < b;
        });

        const std::size_t count = sorted.size();
        std::vector<Bounds> prefix(count);
        std::vector<Bounds> suffix(count);
        std::vector<double> prefixWeight(count);
        std::vector<double> suffixWeight(count);
        prefix[0] = primitives[sorted[0]].bounds;
        prefixWeight[0] = primitiveWeight(primitives[sorted[0]]);
        for (std::size_t i = 1; i < count; ++i)
        {
            prefix[i] = merge(prefix[i - 1], primitives[sorted[i]].bounds);
            prefixWeight[i] = prefixWeight[i - 1] + primitiveWeight(primitives[sorted[i]]);
        }
        suffix[count - 1] = primitives[sorted[count - 1]].bounds;
        suffixWeight[count - 1] = primitiveWeight(primitives[sorted[count - 1]]);
        for (std::size_t i = count - 1; i-- > 0;)
        {
            suffix[i] = merge(suffix[i + 1], primitives[sorted[i]].bounds);
            suffixWeight[i] = suffixWeight[i + 1] + primitiveWeight(primitives[sorted[i]]);
        }

        const double parentArea = std::max(boundsArea(nodeBounds), Eps);
        for (std::size_t split = 1; split < count; ++split)
        {
            const double plane = primitiveMaxAxis(primitives[sorted[split - 1]], splitX);
            const double nextPlane = primitiveMaxAxis(primitives[sorted[split]], splitX);
            if (std::abs(plane - nextPlane) <= Eps)
                continue;
            const double cost = (boundsArea(prefix[split - 1]) * prefixWeight[split - 1] + boundsArea(suffix[split]) * suffixWeight[split]) / parentArea;
            if (cost < best.cost)
                best = { true, splitX, plane, cost };
        }
    };

    evaluateAxis(true);
    evaluateAxis(false);
    return best;
}

int addBvhNode(std::vector<BvhNode>& nodes, const std::vector<PrimitiveRef>& primitives, std::vector<int> indices, std::size_t maxLeafItems)
{
    Bounds bounds = primitives[indices[0]].bounds;
    for (std::size_t i = 1; i < indices.size(); ++i)
        bounds = merge(bounds, primitives[indices[i]].bounds);

    const int nodeIndex = static_cast<int>(nodes.size());
    nodes.push_back({});

    if (indices.size() <= maxLeafItems)
    {
        nodes[nodeIndex] = { true, bounds, -1, -1, indices };
        return nodeIndex;
    }

    const SplitChoice split = chooseSahSplit(primitives, indices, bounds);
    std::vector<int> leftIndices;
    std::vector<int> rightIndices;

    if (split.valid)
    {
        leftIndices.reserve(indices.size());
        rightIndices.reserve(indices.size());
        for (int index : indices)
        {
            if (primitiveMaxAxis(primitives[index], split.splitX) <= split.plane + Eps)
                leftIndices.push_back(index);
            else
                rightIndices.push_back(index);
        }
    }

    if (leftIndices.empty() || rightIndices.empty())
    {
        const bool splitX = (bounds.maxX - bounds.minX) >= (bounds.maxY - bounds.minY);
        std::ranges::sort(indices, [&](int a, int b) {
            const double ca = splitX ? primitives[a].center.x : primitives[a].center.y;
            const double cb = splitX ? primitives[b].center.x : primitives[b].center.y;
            if (std::abs(ca - cb) > Eps)
                return ca < cb;
            return a < b;
        });
        const auto mid = indices.begin() + static_cast<std::ptrdiff_t>(indices.size() / 2);
        leftIndices = { indices.begin(), mid };
        rightIndices = { mid, indices.end() };
    }

    const int left = addBvhNode(nodes, primitives, std::move(leftIndices), maxLeafItems);
    const int right = addBvhNode(nodes, primitives, std::move(rightIndices), maxLeafItems);
    nodes[nodeIndex] = { false, bounds, left, right, {} };
    return nodeIndex;
}

std::vector<BvhNode> buildBvh(const std::vector<Line>& lines, const std::vector<Arc>& arcs, std::size_t maxLeafItems)
{
    std::vector<PrimitiveRef> primitives;
    primitives.reserve(lines.size() + arcs.size());
    for (std::size_t i = 0; i < lines.size(); ++i)
        primitives.push_back({ true, static_cast<int>(i), lineBounds(lines[i]), lineCenter(lines[i]) });
    for (std::size_t i = 0; i < arcs.size(); ++i)
        primitives.push_back({ false, static_cast<int>(i), arcBounds(arcs[i]), arcCenter(arcs[i]) });

    std::vector<BvhNode> nodes;
    if (primitives.empty())
        return nodes;

    std::vector<int> indices(primitives.size());
    for (std::size_t i = 0; i < indices.size(); ++i)
        indices[i] = static_cast<int>(i);
    addBvhNode(nodes, primitives, std::move(indices), maxLeafItems);
    return nodes;
}

template <typename T>
void writeValue(std::ofstream& output, T value)
{
    output.write(reinterpret_cast<const char*>(&value), sizeof(T));
}

void writeBin(const fs::path& path, const std::vector<Line>& lines, const std::vector<Arc>& arcs, const std::vector<BvhNode>& nodes)
{
    fs::create_directories(path.parent_path());
    std::ofstream output(path, std::ios::binary);
    if (!output)
        throw std::runtime_error("Failed to open output file: " + path.string());

    writeValue<std::int32_t>(output, static_cast<std::int32_t>(lines.size()));
    writeValue<std::int32_t>(output, static_cast<std::int32_t>(arcs.size()));
    writeValue<std::int32_t>(output, static_cast<std::int32_t>(nodes.size()));

    for (const Line& line : lines)
    {
        writeValue(output, line.x1);
        writeValue(output, line.y1);
        writeValue(output, line.x2);
        writeValue(output, line.y2);
    }
    for (const Arc& arc : arcs)
    {
        writeValue(output, arc.x);
        writeValue(output, arc.y);
        writeValue(output, arc.r);
        writeValue(output, arc.start);
        writeValue(output, arc.end);
    }

    for (const BvhNode& node : nodes)
    {
        writeValue<std::uint8_t>(output, node.isLeaf ? 1 : 0);
        const std::array<std::uint8_t, 7> padding{};
        output.write(reinterpret_cast<const char*>(padding.data()), static_cast<std::streamsize>(padding.size()));
        writeValue(output, node.bounds.minX);
        writeValue(output, node.bounds.minY);
        writeValue(output, node.bounds.maxX);
        writeValue(output, node.bounds.maxY);
        if (node.isLeaf)
        {
            writeValue<std::int32_t>(output, static_cast<std::int32_t>(node.items.size()));
            for (int item : node.items)
                writeValue<std::int32_t>(output, item);
        }
        else
        {
            writeValue<std::int32_t>(output, node.left);
            writeValue<std::int32_t>(output, node.right);
        }
    }
}

template <typename T>
T readValue(std::ifstream& input)
{
    T value{};
    input.read(reinterpret_cast<char*>(&value), sizeof(T));
    if (!input)
        throw std::runtime_error("Unexpected end of file while reading bin");
    return value;
}

void readBinPrimitives(const fs::path& path, std::vector<Line>& lines, std::vector<Arc>& arcs)
{
    std::ifstream input(path, std::ios::binary);
    if (!input)
        throw std::runtime_error("Failed to open input bin: " + path.string());

    const auto lineCount = readValue<std::int32_t>(input);
    const auto arcCount = readValue<std::int32_t>(input);
    const auto nodeCount = readValue<std::int32_t>(input);
    if (lineCount < 0 || arcCount < 0 || nodeCount < 0)
        throw std::runtime_error("Invalid negative count in bin: " + path.string());

    lines.reserve(static_cast<std::size_t>(lineCount));
    arcs.reserve(static_cast<std::size_t>(arcCount));
    for (std::int32_t i = 0; i < lineCount; ++i)
    {
        Line line;
        line.x1 = readValue<double>(input);
        line.y1 = readValue<double>(input);
        line.x2 = readValue<double>(input);
        line.y2 = readValue<double>(input);
        lines.push_back(line);
    }
    for (std::int32_t i = 0; i < arcCount; ++i)
    {
        Arc arc;
        arc.x = readValue<double>(input);
        arc.y = readValue<double>(input);
        arc.r = readValue<double>(input);
        arc.start = readValue<double>(input);
        arc.end = readValue<double>(input);
        arcs.push_back(arc);
    }
}

Options parseArgs(int argc, char** argv)
{
    if (argc < 3)
        throw std::runtime_error("Usage: CgalBinBuilder <dump.json> <output.bin> [...] OR CgalBinBuilder --rebuild-bin <input.bin> <output.bin> [--max-leaf-items n]");

    Options options;
    int firstOption = 3;
    if (std::string(argv[1]) == "--rebuild-bin")
    {
        if (argc < 4)
            throw std::runtime_error("Usage: CgalBinBuilder --rebuild-bin <input.bin> <output.bin> [--max-leaf-items n]");
        options.rebuildExistingBin = true;
        options.rebuildInputPath = argv[2];
        options.outputPath = argv[3];
        firstOption = 4;
    }
    else
    {
        options.dumpPath = argv[1];
        options.outputPath = argv[2];
    }

    bool includeOverridden = false;
    for (int i = firstOption; i < argc; ++i)
    {
        const std::string arg = argv[i];
        auto requireValue = [&](std::string_view name) -> std::string {
            if (i + 1 >= argc)
                throw std::runtime_error("Missing value for " + std::string(name));
            return argv[++i];
        };

        if (arg == "--include")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--include is not valid with --rebuild-bin");
            if (!includeOverridden)
            {
                options.includes.clear();
                includeOverridden = true;
            }
            options.includes.push_back(requireValue("--include"));
        }
        else if (arg == "--exclude")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--exclude is not valid with --rebuild-bin");
            options.excludes.push_back(requireValue("--exclude"));
        }
        else if (arg == "--radius")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--radius is not valid with --rebuild-bin");
            options.radius = std::stod(requireValue("--radius"));
        }
        else if (arg == "--include-inactive")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--include-inactive is not valid with --rebuild-bin");
            options.includeInactive = true;
        }
        else if (arg == "--mode")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--mode is not valid with --rebuild-bin");
            options.mode = requireValue("--mode");
            if (options.mode != "per-collider" && options.mode != "boundary-filter" && options.mode != "cgal-union")
                throw std::runtime_error("Unsupported mode: " + options.mode);
        }
        else if (arg == "--boundary-step")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--boundary-step is not valid with --rebuild-bin");
            options.boundaryStep = std::stod(requireValue("--boundary-step"));
            if (options.boundaryStep <= 0)
                throw std::runtime_error("--boundary-step must be positive");
        }
        else if (arg == "--coverage-epsilon")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--coverage-epsilon is not valid with --rebuild-bin");
            options.coverageEpsilon = std::stod(requireValue("--coverage-epsilon"));
            if (options.coverageEpsilon < 0)
                throw std::runtime_error("--coverage-epsilon must be non-negative");
        }
        else if (arg == "--non-circle-extra-offset")
        {
            if (options.rebuildExistingBin)
                throw std::runtime_error("--non-circle-extra-offset is not valid with --rebuild-bin");
            options.nonCircleExtraOffset = std::stod(requireValue("--non-circle-extra-offset"));
            if (options.nonCircleExtraOffset < 0)
                throw std::runtime_error("--non-circle-extra-offset must be non-negative");
        }
        else if (arg == "--max-leaf-items")
        {
            const int value = std::stoi(requireValue("--max-leaf-items"));
            if (value <= 0)
                throw std::runtime_error("--max-leaf-items must be positive");
            options.maxLeafItems = static_cast<std::size_t>(value);
        }
        else
        {
            throw std::runtime_error("Unknown argument: " + arg);
        }
    }
    return options;
}

int main(int argc, char** argv)
{
    try
    {
        const Options options = parseArgs(argc, argv);

        if (options.rebuildExistingBin)
        {
            std::vector<Line> lines;
            std::vector<Arc> arcs;
            readBinPrimitives(options.rebuildInputPath, lines, arcs);
            const std::vector<BvhNode> nodes = buildBvh(lines, arcs, options.maxLeafItems);
            writeBin(options.outputPath, lines, arcs, nodes);

            std::cout << "Wrote " << options.outputPath << '\n';
            std::cout << "Mode: rebuild-bin\n";
            std::cout << "Max leaf items: " << options.maxLeafItems << '\n';
            std::cout << "Lines: " << lines.size() << '\n';
            std::cout << "Arcs: " << arcs.size() << '\n';
            std::cout << "BVH nodes: " << nodes.size() << '\n';
            return 0;
        }

        pt::ptree dump;
        pt::read_json(options.dumpPath.string(), dump);

        double radius = options.radius;
        if (std::isnan(radius))
            radius = dump.get<double>("indicatorCollider.radius", 0.74);

        std::vector<Line> lines;
        std::vector<Arc> arcs;
        std::vector<InflatedShape> shapes;
        std::vector<CandidateLine> candidateLines;
        std::vector<CandidateArc> candidateArcs;
        std::vector<std::string> warnings;
        int selectedCount = 0;

        for (const auto& item : dump.get_child("colliders"))
        {
            const pt::ptree& collider = item.second;
            const std::string path = collider.get<std::string>("path", "");
            if (!options.includes.empty() && !containsAny(path, options.includes))
                continue;
            if (!options.excludes.empty() && containsAny(path, options.excludes))
                continue;
            if (!options.includeInactive)
            {
                if (!collider.get<bool>("enabled", false) || !collider.get<bool>("activeInHierarchy", false))
                    continue;
            }

            ++selectedCount;
            if (options.mode == "boundary-filter")
            {
                if (auto warning = addColliderAnalysis(collider, selectedCount - 1, radius, options.nonCircleExtraOffset, shapes, candidateLines, candidateArcs))
                    warnings.push_back(path + ": " + *warning);
            }
            else
            {
                if (auto warning = addColliderBoundary(collider, radius, options.nonCircleExtraOffset, lines, arcs))
                    warnings.push_back(path + ": " + *warning);
            }
        }

        if (options.mode == "cgal-union")
        {
            throw std::runtime_error(
                "cgal-union mode is not implemented yet. "
                "The installed CGAL provides Gps_circle_segment_traits_2 and "
                "General_polygon_set_2, so the next step is wiring these "
                "per-collider analytic boundaries into a regularized union pass."
            );
        }

        if (options.mode == "boundary-filter")
        {
            filterBoundaryCandidates(candidateLines, candidateArcs, shapes, options, lines, arcs);
        }

        const std::vector<BvhNode> nodes = buildBvh(lines, arcs, options.maxLeafItems);
        writeBin(options.outputPath, lines, arcs, nodes);

        std::cout << "Wrote " << options.outputPath << '\n';
        std::cout << "Mode: " << options.mode << '\n';
        std::cout << "Non-circle extra offset: " << options.nonCircleExtraOffset << '\n';
        std::cout << "Max leaf items: " << options.maxLeafItems << '\n';
        std::cout << "Selected colliders: " << selectedCount << '\n';
        if (options.mode == "boundary-filter")
        {
            std::cout << "Inflated shapes: " << shapes.size() << '\n';
            std::cout << "Candidate lines: " << candidateLines.size() << '\n';
            std::cout << "Candidate arcs: " << candidateArcs.size() << '\n';
            std::cout << "Boundary step: " << options.boundaryStep << '\n';
        }
        std::cout << "Lines: " << lines.size() << '\n';
        std::cout << "Arcs: " << arcs.size() << '\n';
        std::cout << "BVH nodes: " << nodes.size() << '\n';
        if (!warnings.empty())
            std::cout << "Warnings: " << warnings.size() << '\n';

        return 0;
    }
    catch (const std::exception& ex)
    {
        std::cerr << "Error: " << ex.what() << '\n';
        return 1;
    }
}
