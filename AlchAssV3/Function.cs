using BepInEx;
using PotionCraft.DebugObjects.DebugWindows;
using PotionCraft.InputSystem;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased;
using PotionCraft.ObjectBased.InteractiveItem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlchAssV3
{
    public static class Function
    {
        #region 窗口相关
        /// <summary>
        /// 生成调试窗口
        /// </summary>
        public static void InitDebugWindow(int index, Room room)
        {
            var Window = DebugWindow.Init(LocalizationManager.GetText(Variable.WindowTags[index]), true);
            Window.ToForeground();
            Window.transform.SetParent(room.transform, false);
            Window.transform.localPosition = Variable.WindowPositions[index].Value;
            Window.transform.localScale *= Variable.WindowScale.Value;
            Variable.DebugWindows[index] = Window;
        }

        /// <summary>
        /// 恢复调试窗口
        /// </summary>
        public static void RestoreDebugWindows()
        {
            for (var i = 0; i < Variable.DebugWindows.Length; i++)
            {
                Variable.DebugWindows[i].Visible = true;
                Variable.DebugWindows[i].transform.localPosition = Variable.WindowPositions[i].Value;
            }
            Variable.WindowRect = Variable.WindowRectConfig.Value;
        }

        /// <summary>
        /// 保存调试窗口位置
        /// </summary>
        public static void SaveDebugWindowPos()
        {
            for (var i = 0; i < Variable.DebugWindows.Length; i++)
                Variable.WindowPositions[i].Value = Variable.DebugWindows[i].transform.localPosition;
            Variable.WindowRectConfig.Value = Variable.WindowRect;
            Variable.UIFontSizeConfig.Value = Variable.UIFontSize;
            Variable.HelpTooltipFontSizeConfig.Value = Variable.HelpTooltipFontSize;
        }

        public static void SetDebugWindowTitle()
        {
            for (var i = 0; i < Variable.DebugWindows.Length; i++)
                Variable.DebugWindows[i]?.captionText.text = LocalizationManager.GetText($"{Variable.WindowTags[i]}");
        }
        #endregion

        #region 快捷键功能
        /// <summary>
        /// 游戏输入框选中时，避免模组快捷键和文本输入互相干扰。
        /// </summary>
        public static bool IsGameInputFieldSelected()
        {
            return Command.IsInputFieldSelected;
        }

        /// <summary>
        /// 控制面板开关
        /// </summary>
        public static void UpdateWindow()
        {
            if (IsGameInputFieldSelected())
                return;

            if (Variable.KeyWindow.Value.IsDown())
                Variable.ShowWindow = !Variable.ShowWindow;
            if (Variable.ShowWindow)
            {
                Cursor.visible = true;
                UIWindow.Resizing();
            }
        }

        /// <summary>
        /// 更新选择效果
        /// </summary>
        public static void UpdateSelectEffect(InteractiveItem item)
        {
            if (IsGameInputFieldSelected())
                return;

            if (Variable.KeyEffect.Value.IsPressed() && Mouse.current.rightButton.wasPressedThisFrame)
            {
                var name = item.name;
                if (name == null)
                    return;
                Variable.TargetEffect = Managers.RecipeMap.currentMap.referencesContainer.potionEffectsOnMap.FirstOrDefault(item => item.name == name);
            }
        }

        /// <summary>
        /// 更新选择漩涡
        /// </summary>
        public static void UpdateSelectVortex()
        {
            if (IsGameInputFieldSelected())
                return;

            if (Managers.RecipeMap?.currentMap == null || Managers.RecipeMap.currentMap.potionBase.name == "Wine")
                return;

            var mapid = Variable.MapId[Managers.RecipeMap.currentMap.potionBase.name];
            if (Variable.KeyVortex.Value.IsPressed() && Mouse.current.rightButton.wasPressedThisFrame)
            {
                var mousePos = Managers.Cursor.cursor.transform.position;
                if (!Managers.RecipeMap.recipeMapObject.visibilityZoneCollider.OverlapPoint(mousePos))
                    return;

                var worldPos = Managers.RecipeMap.recipeMapObject.transmitterWindow.ViewToCamera(mousePos);
                Vector2 mapPos = Managers.RecipeMap.currentMap.referencesContainer.transform.InverseTransformPoint(worldPos);
                Variable.VortexIndex[mapid] = -1;
                for (var i = 0; i < Variable.Vortexs[mapid].Count; i++)
                {
                    var vortex = Variable.Vortexs[mapid][i];
                    var dx = mapPos.x - vortex.x;
                    var dy = mapPos.y - vortex.y;
                    if (dx * dx + dy * dy <= vortex.r * vortex.r)
                    {
                        Variable.VortexIndex[mapid] = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 更新自定义直线
        /// </summary>
        public static void UpdateCustomLines()
        {
            if (IsGameInputFieldSelected())
            {
                Variable.TargetLineIndex = -1;
                return;
            }

            if (Variable.KeyCustom.Value.IsPressed() && Variable.DoCustomLine)
            {
                var mousePos = Managers.Cursor.cursor.transform.position;
                if (!Managers.RecipeMap.recipeMapObject.visibilityZoneCollider.OverlapPoint(mousePos))
                    return;

                var worldPos = Managers.RecipeMap.recipeMapObject.transmitterWindow.ViewToCamera(mousePos);
                Vector2 mapPos = Managers.RecipeMap.currentMap.referencesContainer.transform.InverseTransformPoint(worldPos);
                var indPos = Calculation.GetIndicatorMapCheckPosition();
                var delta = mapPos - indPos;

                if (Mouse.current.middleButton.isPressed)
                {
                    if (Variable.TargetLineIndex < 0)
                        return;

                    var dir = Vector2.SignedAngle(Vector2.right, delta);
                    dir = dir < 0 ? dir + 360 : dir;
                    Variable.CustomLineDirections[Variable.TargetLineIndex] = dir;
                    Variable.Inputs[Variable.TargetLineIndex] = (dir.ToString(), false);
                }
                else
                {
                    var target = -1;
                    var minDis = double.MaxValue;
                    for (var i = 0; i < Variable.CustomLineDirections.Count; i++)
                    {
                        var theta = Variable.CustomLineDirections[i] / 180 * Math.PI;
                        var dis = Math.Abs(delta.x * Math.Sin(theta) - delta.y * Math.Cos(theta));
                        if (dis < minDis)
                        {
                            minDis = dis;
                            target = i;
                        }
                    }
                    if (minDis < 0.3)
                        Variable.TargetLineIndex = target;
                    else
                        Variable.TargetLineIndex = -1;

                    if (Mouse.current.rightButton.wasPressedThisFrame)
                    {
                        if (Variable.TargetLineIndex < 0)
                        {
                            var dir = Vector2.SignedAngle(Vector2.right, delta);
                            dir = dir < 0 ? dir + 360 : dir;
                            Variable.CustomLineDirections.Add(dir);
                            Variable.CustomLineHovers.Add(false);
                            Variable.Inputs.Add((dir.ToString(), false));
                            Variable.TargetLineIndex = Variable.CustomLineDirections.Count - 1;
                        }
                        else
                        {
                            Variable.CustomLineDirections.RemoveAt(Variable.TargetLineIndex);
                            Variable.CustomLineHovers.RemoveAt(Variable.TargetLineIndex);
                            Variable.Inputs.RemoveAt(Variable.TargetLineIndex);
                            Variable.TargetLineIndex = -1;
                        }
                    }
                }
            }
            else
                Variable.TargetLineIndex = -1;
        }
        #endregion

        #region 功能开关
        /// <summary>
        /// 从快捷键更新功能开关
        /// </summary>
        public static void UpdateEnable()
        {
            if (IsGameInputFieldSelected())
                return;

            if (Variable.KeyEnablePathLine.Value.IsDown())
                Variable.EnablePathLine = !Variable.EnablePathLine;
            if (Variable.KeyEnableLadleLine.Value.IsDown())
                Variable.EnableLadleLine = !Variable.EnableLadleLine;
            if (Variable.KeyEnableEffectLine.Value.IsDown())
                Variable.EnableEffectLine = !Variable.EnableEffectLine;
            if (Variable.KeyEnableVortexLine.Value.IsDown())
                Variable.EnableVortexLine = !Variable.EnableVortexLine;
            if (Variable.KeyEnableTangentLine.Value.IsDown())
                Variable.EnableTangentLine = !Variable.EnableTangentLine;
            if (Variable.KeyEnableCustomLine.Value.IsDown())
                Variable.EnableCustomLine = !Variable.EnableCustomLine;
            if (Variable.KeyEnablePathCurve.Value.IsDown())
                Variable.EnablePathCurve = !Variable.EnablePathCurve;
            if (Variable.KeyEnableVortexCurve.Value.IsDown())
                Variable.EnableVortexCurve = !Variable.EnableVortexCurve;
            if (Variable.KeyEnableEffectRange.Value.IsDown())
                Variable.EnableEffectRange = !Variable.EnableEffectRange;
            if (Variable.KeyEnableVortexRange.Value.IsDown())
                Variable.EnableVortexRange = !Variable.EnableVortexRange;
            if (Variable.KeyEnableDangerSimulation.Value.IsDown())
                Variable.EnableDangerSimulation = !Variable.EnableDangerSimulation;
            if (Variable.KeyEnableSwampSimulation.Value.IsDown())
                Variable.EnableSwampSimulation = !Variable.EnableSwampSimulation;
            if (Variable.KeyEnableTransparency.Value.IsDown())
                Variable.EnableTransparency = !Variable.EnableTransparency;
            if (Variable.KeyEnableColliderAttachment.Value.IsDown())
                Variable.EnableColliderAttachment = !Variable.EnableColliderAttachment;
            if (Variable.KeyToggleDisplaySalt.Value.IsDown())
                Variable.DisplaySalt = !Variable.DisplaySalt;
            if (Variable.KeyToggleDisplayStage.Value.IsDown())
                Variable.DisplayStage = !Variable.DisplayStage;
            if (Variable.KeyToggleDisplayPolar.Value.IsDown())
                Variable.DisplayPolar = !Variable.DisplayPolar;
#if DEBUG
            if (Keyboard.current?.f12Key.wasPressedThisFrame == true)
                DumpMapColliders();
#endif
        }

        /// <summary>
        /// 渲染元素开关
        /// </summary>
        public static void UpdateDoFromEnable()
        {
            Variable.DoCustomLine = Variable.EnableCustomLine;
            Variable.DoPathCurve = Variable.EnablePathCurve;
            Variable.DoVortexCurve = Variable.EnableVortexCurve;
            Variable.DoEffectRange = Variable.EnableEffectRange;
            Variable.DoVortexRange = Variable.EnableVortexRange;
            Variable.DoTransparency = Variable.EnableTransparency;
            Variable.DoColliderAttachment = Variable.EnableColliderAttachment;
            Variable.DoPathEffectPoint = Variable.EnablePathCurve && Variable.EnableEffectRange;
            Variable.DoLadleEffectPoint = Variable.EnableLadleLine && Variable.EnableEffectRange;
            Variable.DoPathVortexPoint = Variable.EnablePathCurve && Variable.EnableVortexRange;
            Variable.DoLadleVortexPoint = Variable.EnableLadleLine && Variable.EnableVortexRange;
            Variable.DoPathDangerPoint = Variable.EnablePathCurve && Variable.EnableDangerSimulation;
            Variable.DoLadleDangerPoint = Variable.EnableLadleLine && Variable.EnableDangerSimulation;
            Variable.DoVortexDangerPoint = Variable.EnableVortexCurve && Variable.EnableDangerSimulation;
            Variable.DoSwampPoint = Variable.EnablePathCurve && Variable.EnableSwampSimulation;
            Variable.DoLines[0] = Variable.EnablePathLine && Variable.EnablePathCurve;
            Variable.DoLines[1] = Variable.EnableLadleLine;
            Variable.DoLines[2] = Variable.EnableEffectLine;
            Variable.DoLines[3] = Variable.EnableVortexLine;
            Variable.DoLines[4] = Variable.EnableTangentLine && Variable.EnableVortexCurve;
        }
        #endregion

#if DEBUG
        #region 调试导出
        /// <summary>
        /// 导出当前地图和指示器相关的 Collider2D 运行时参数。
        /// </summary>
        public static void DumpMapColliders()
        {
            if (Managers.RecipeMap?.currentMap?.referencesContainer == null)
                return;

            var dumpDirectory = Path.Combine(Paths.ConfigPath, "AlchAssV3", "RuntimeDumps");
            Directory.CreateDirectory(dumpDirectory);

            var mapName = Managers.RecipeMap.currentMap.potionBase?.name ?? "UnknownMap";
            var fileName = $"map_colliders_{SanitizeFileName(mapName)}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(dumpDirectory, fileName);
            var mapTransform = Managers.RecipeMap.currentMap.referencesContainer.transform;
            var indicatorCollider = Managers.RecipeMap.indicator.circleCollider;

            var seenColliderIds = new HashSet<int>();
            var colliders = GetMapColliderDumpTargets()
                .Where(collider => seenColliderIds.Add(collider.GetInstanceID()))
                .OrderBy(collider => GetHierarchyPath(collider.transform))
                .ToList();

            var sb = new StringBuilder(1024 * Math.Max(colliders.Count, 1));
            sb.AppendLine("{");
            AppendJsonProperty(sb, 1, "map", mapName, comma: true);
            AppendJsonProperty(sb, 1, "generatedAt", DateTime.Now.ToString("O"), comma: true);
            AppendJsonProperty(sb, 1, "physics2DDefaultContactOffset", Physics2D.defaultContactOffset, comma: true);
            AppendJsonProperty(sb, 1, "collider2DHasPerColliderContactOffset", false, comma: true);
            AppendJsonProperty(sb, 1, "contactOffsetModel", "Collider2D exposes no per-collider contactOffset in this Unity API; dumped colliders use Physics2D.defaultContactOffset.", comma: true);
            AppendJsonProperty(sb, 1, "colliderCount", colliders.Count, comma: true);
            AppendIndicatorColliderJson(sb, indicatorCollider, mapTransform, 1, comma: true);
            AppendNearIndicatorCollidersJson(sb, colliders, indicatorCollider, mapTransform, 1, comma: true);
            sb.AppendLine(Indent(1) + "\"colliders\": [");
            for (var i = 0; i < colliders.Count; i++)
            {
                AppendColliderJson(sb, colliders[i], mapTransform, 2);
                sb.AppendLine(i == colliders.Count - 1 ? "" : ",");
            }
            sb.AppendLine(Indent(1) + "]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[AlchAssV3] Map colliders dumped to config/AlchAssV3/RuntimeDumps/{fileName}");
        }

        private static IEnumerable<Collider2D> GetMapColliderDumpTargets()
        {
            var roots = new List<Transform>
            {
                Managers.RecipeMap.currentMap.referencesContainer.transform,
                Managers.RecipeMap.recipeMapObject.indicatorContainer,
                Managers.RecipeMap.recipeMapObject.transform
            };

            foreach (var root in roots.Where(root => root != null))
                foreach (var collider in root.GetComponentsInChildren<Collider2D>(true))
                    yield return collider;
        }

        private static void AppendColliderJson(StringBuilder sb, Collider2D collider, Transform mapTransform, int level)
        {
            var transform = collider.transform;
            sb.AppendLine(Indent(level) + "{");
            AppendJsonProperty(sb, level + 1, "instanceId", collider.GetInstanceID(), comma: true);
            AppendJsonProperty(sb, level + 1, "name", collider.name, comma: true);
            AppendJsonProperty(sb, level + 1, "path", GetHierarchyPath(transform), comma: true);
            AppendJsonProperty(sb, level + 1, "type", collider.GetType().FullName, comma: true);
            AppendJsonProperty(sb, level + 1, "enabled", collider.enabled, comma: true);
            AppendJsonProperty(sb, level + 1, "activeInHierarchy", collider.gameObject.activeInHierarchy, comma: true);
            AppendJsonProperty(sb, level + 1, "isTrigger", collider.isTrigger, comma: true);
            AppendJsonProperty(sb, level + 1, "compositeOperation", collider.compositeOperation.ToString(), comma: true);
            AppendJsonProperty(sb, level + 1, "compositeOrder", collider.compositeOrder, comma: true);
            AppendVectorProperty(sb, level + 1, "colliderOffset", collider.offset, comma: true);
            AppendJsonProperty(sb, level + 1, "contactOffset", Physics2D.defaultContactOffset, comma: true);
            AppendJsonProperty(sb, level + 1, "usesDefaultContactOffset", true, comma: true);
            AppendJsonProperty(sb, level + 1, "usedByEffector", collider.usedByEffector, comma: true);
            AppendJsonProperty(sb, level + 1, "contactCaptureLayers", collider.contactCaptureLayers.value, comma: true);
            AppendJsonProperty(sb, level + 1, "layer", collider.gameObject.layer, comma: true);
            AppendJsonProperty(sb, level + 1, "tag", collider.tag, comma: true);
            AppendVectorProperty(sb, level + 1, "localPosition", transform.localPosition, comma: true);
            AppendVectorProperty(sb, level + 1, "worldPosition", transform.position, comma: true);
            AppendVectorProperty(sb, level + 1, "mapPosition", mapTransform.InverseTransformPoint(transform.position), comma: true);
            AppendVectorProperty(sb, level + 1, "localEulerAngles", transform.localEulerAngles, comma: true);
            AppendVectorProperty(sb, level + 1, "worldEulerAngles", transform.eulerAngles, comma: true);
            AppendVectorProperty(sb, level + 1, "localScale", transform.localScale, comma: true);
            AppendVectorProperty(sb, level + 1, "lossyScale", transform.lossyScale, comma: true);
            AppendBoundsProperty(sb, level + 1, "worldBounds", collider.bounds, comma: true);
            AppendShapeJson(sb, collider, mapTransform, level + 1);
            sb.Append(Indent(level) + "}");
        }

        private static void AppendShapeJson(StringBuilder sb, Collider2D collider, Transform mapTransform, int level)
        {
            sb.AppendLine(Indent(level) + "\"shape\": {");
            AppendJsonProperty(sb, level + 1, "colliderType", collider.GetType().Name, comma: true);

            switch (collider)
            {
                case CircleCollider2D circle:
                    AppendVectorProperty(sb, level + 1, "offset", circle.offset, comma: true);
                    AppendJsonProperty(sb, level + 1, "radius", circle.radius, comma: true);
                    AppendVectorProperty(sb, level + 1, "mapCenter", ColliderPointToMap(circle.transform, mapTransform, circle.offset), comma: true);
                    AppendJsonProperty(sb, level + 1, "mapRadiusByLossyScaleMax", circle.radius * Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y)), comma: false);
                    break;
                case BoxCollider2D box:
                    AppendVectorProperty(sb, level + 1, "offset", box.offset, comma: true);
                    AppendVectorProperty(sb, level + 1, "size", box.size, comma: true);
                    AppendJsonProperty(sb, level + 1, "edgeRadius", box.edgeRadius, comma: true);
                    AppendVectorArrayProperty(sb, level + 1, "mapCorners", GetBoxMapCorners(box, mapTransform), comma: false);
                    break;
                case PolygonCollider2D polygon:
                    AppendVectorProperty(sb, level + 1, "offset", polygon.offset, comma: true);
                    AppendJsonProperty(sb, level + 1, "pathCount", polygon.pathCount, comma: true);
                    AppendPolygonPathsProperty(sb, level + 1, "mapPaths", polygon, mapTransform, comma: false);
                    break;
                case EdgeCollider2D edge:
                    AppendVectorProperty(sb, level + 1, "offset", edge.offset, comma: true);
                    AppendJsonProperty(sb, level + 1, "edgeRadius", edge.edgeRadius, comma: true);
                    AppendVectorArrayProperty(sb, level + 1, "mapPoints", edge.points.Select(point => ColliderPointToMap(edge.transform, mapTransform, edge.offset + point)), comma: false);
                    break;
                case CapsuleCollider2D capsule:
                    AppendVectorProperty(sb, level + 1, "offset", capsule.offset, comma: true);
                    AppendVectorProperty(sb, level + 1, "size", capsule.size, comma: true);
                    AppendJsonProperty(sb, level + 1, "direction", capsule.direction.ToString(), comma: true);
                    AppendVectorArrayProperty(sb, level + 1, "mapBoxCorners", GetCapsuleBoxMapCorners(capsule, mapTransform), comma: false);
                    break;
                default:
                    AppendJsonProperty(sb, level + 1, "note", "Unsupported Collider2D subtype; transform and bounds are still dumped.", comma: false);
                    break;
            }

            sb.AppendLine();
            sb.AppendLine(Indent(level) + "}");
        }

        private static void AppendIndicatorColliderJson(StringBuilder sb, CircleCollider2D indicatorCollider, Transform mapTransform, int level, bool comma)
        {
            sb.AppendLine(Indent(level) + "\"indicatorCollider\": {");
            AppendJsonProperty(sb, level + 1, "instanceId", indicatorCollider.GetInstanceID(), comma: true);
            AppendJsonProperty(sb, level + 1, "path", GetHierarchyPath(indicatorCollider.transform), comma: true);
            AppendVectorProperty(sb, level + 1, "colliderOffset", indicatorCollider.offset, comma: true);
            AppendJsonProperty(sb, level + 1, "contactOffset", Physics2D.defaultContactOffset, comma: true);
            AppendJsonProperty(sb, level + 1, "usesDefaultContactOffset", true, comma: true);
            AppendVectorProperty(sb, level + 1, "mapCenter", ColliderPointToMap(indicatorCollider.transform, mapTransform, indicatorCollider.offset), comma: true);
            AppendJsonProperty(sb, level + 1, "radius", indicatorCollider.radius, comma: true);
            AppendJsonProperty(sb, level + 1, "mapRadiusByLossyScaleMax", indicatorCollider.radius * Mathf.Max(Mathf.Abs(indicatorCollider.transform.lossyScale.x), Mathf.Abs(indicatorCollider.transform.lossyScale.y)), comma: false);
            sb.Append(Indent(level)).Append('}');
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendNearIndicatorCollidersJson(StringBuilder sb, List<Collider2D> colliders, Collider2D indicatorCollider, Transform mapTransform, int level, bool comma)
        {
            const float nearDistanceLimit = 5f;
            var nearColliders = colliders
                .Where(collider => collider != null && collider != indicatorCollider && collider.enabled && collider.gameObject.activeInHierarchy)
                .Select(collider => new { Collider = collider, Distance = indicatorCollider.Distance(collider), IsTouching = indicatorCollider.IsTouching(collider) })
                .Where(item => item.IsTouching || item.Distance.isOverlapped || (item.Distance.isValid && item.Distance.distance <= nearDistanceLimit))
                .OrderBy(item => item.Distance.isValid ? item.Distance.distance : float.MaxValue)
                .ToList();

            sb.AppendLine(Indent(level) + "\"nearIndicatorColliders\": {");
            AppendJsonProperty(sb, level + 1, "distanceLimit", nearDistanceLimit, comma: true);
            AppendJsonProperty(sb, level + 1, "count", nearColliders.Count, comma: true);
            sb.AppendLine(Indent(level + 1) + "\"items\": [");
            for (var i = 0; i < nearColliders.Count; i++)
            {
                var item = nearColliders[i];
                AppendIndicatorDistanceJson(sb, item.Collider, item.Distance, item.IsTouching, mapTransform, level + 2);
                sb.AppendLine(i == nearColliders.Count - 1 ? "" : ",");
            }
            sb.AppendLine(Indent(level + 1) + "]");
            sb.Append(Indent(level)).Append('}');
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendIndicatorDistanceJson(StringBuilder sb, Collider2D collider, ColliderDistance2D distance, bool isTouching, Transform mapTransform, int level)
        {
            sb.AppendLine(Indent(level) + "{");
            AppendJsonProperty(sb, level + 1, "instanceId", collider.GetInstanceID(), comma: true);
            AppendJsonProperty(sb, level + 1, "path", GetHierarchyPath(collider.transform), comma: true);
            AppendJsonProperty(sb, level + 1, "type", collider.GetType().FullName, comma: true);
            AppendJsonProperty(sb, level + 1, "isTrigger", collider.isTrigger, comma: true);
            AppendVectorProperty(sb, level + 1, "colliderOffset", collider.offset, comma: true);
            AppendJsonProperty(sb, level + 1, "contactOffset", Physics2D.defaultContactOffset, comma: true);
            AppendJsonProperty(sb, level + 1, "usesDefaultContactOffset", true, comma: true);
            AppendJsonProperty(sb, level + 1, "isTouchingIndicator", isTouching, comma: true);
            AppendJsonProperty(sb, level + 1, "distanceIsValid", distance.isValid, comma: true);
            AppendJsonProperty(sb, level + 1, "distanceIsOverlapped", distance.isOverlapped, comma: true);
            AppendJsonProperty(sb, level + 1, "signedDistance", distance.distance, comma: true);
            AppendVectorProperty(sb, level + 1, "pointOnIndicatorMap", mapTransform.InverseTransformPoint(distance.pointA), comma: true);
            AppendVectorProperty(sb, level + 1, "pointOnColliderMap", mapTransform.InverseTransformPoint(distance.pointB), comma: true);
            AppendShapeSummaryJson(sb, collider, mapTransform, level + 1);
            sb.Append(Indent(level) + "}");
        }

        private static void AppendShapeSummaryJson(StringBuilder sb, Collider2D collider, Transform mapTransform, int level)
        {
            sb.AppendLine(Indent(level) + "\"shape\": {");
            AppendJsonProperty(sb, level + 1, "colliderType", collider.GetType().Name, comma: true);
            switch (collider)
            {
                case CircleCollider2D circle:
                    AppendVectorProperty(sb, level + 1, "mapCenter", ColliderPointToMap(circle.transform, mapTransform, circle.offset), comma: true);
                    AppendJsonProperty(sb, level + 1, "radius", circle.radius, comma: true);
                    AppendJsonProperty(sb, level + 1, "mapRadiusByLossyScaleMax", circle.radius * Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y)), comma: false);
                    break;
                case BoxCollider2D box:
                    AppendVectorProperty(sb, level + 1, "offset", box.offset, comma: true);
                    AppendVectorProperty(sb, level + 1, "size", box.size, comma: true);
                    AppendJsonProperty(sb, level + 1, "edgeRadius", box.edgeRadius, comma: true);
                    AppendVectorArrayProperty(sb, level + 1, "mapCorners", GetBoxMapCorners(box, mapTransform), comma: false);
                    break;
                default:
                    AppendJsonProperty(sb, level + 1, "note", "See full collider entry for detailed shape.", comma: false);
                    break;
            }
            sb.AppendLine();
            sb.AppendLine(Indent(level) + "}");
        }

        private static IEnumerable<Vector2> GetBoxMapCorners(BoxCollider2D box, Transform mapTransform)
        {
            var half = box.size / 2f;
            var offset = box.offset;
            return new[]
            {
                new Vector2(offset.x - half.x, offset.y - half.y),
                new Vector2(offset.x - half.x, offset.y + half.y),
                new Vector2(offset.x + half.x, offset.y + half.y),
                new Vector2(offset.x + half.x, offset.y - half.y)
            }.Select(point => ColliderPointToMap(box.transform, mapTransform, point));
        }

        private static IEnumerable<Vector2> GetCapsuleBoxMapCorners(CapsuleCollider2D capsule, Transform mapTransform)
        {
            var half = capsule.size / 2f;
            var offset = capsule.offset;
            return new[]
            {
                new Vector2(offset.x - half.x, offset.y - half.y),
                new Vector2(offset.x - half.x, offset.y + half.y),
                new Vector2(offset.x + half.x, offset.y + half.y),
                new Vector2(offset.x + half.x, offset.y - half.y)
            }.Select(point => ColliderPointToMap(capsule.transform, mapTransform, point));
        }

        private static Vector2 ColliderPointToMap(Transform colliderTransform, Transform mapTransform, Vector2 localPoint)
        {
            return mapTransform.InverseTransformPoint(colliderTransform.TransformPoint(localPoint));
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
                names.Push(current.name);
            return string.Join("/", names);
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                value = value.Replace(invalidChar, '_');
            return value;
        }

        private static string Indent(int level) => new(' ', level * 2);

        private static string EscapeJson(string value)
        {
            return value?
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") ?? "";
        }

        private static void AppendJsonProperty(StringBuilder sb, int level, string name, string value, bool comma)
        {
            sb.Append(Indent(level)).Append('"').Append(name).Append("\": \"").Append(EscapeJson(value)).Append('"');
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendJsonProperty(StringBuilder sb, int level, string name, int value, bool comma)
        {
            sb.Append(Indent(level)).Append('"').Append(name).Append("\": ").Append(value);
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendJsonProperty(StringBuilder sb, int level, string name, float value, bool comma)
        {
            sb.Append(Indent(level)).Append('"').Append(name).Append("\": ").Append(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendJsonProperty(StringBuilder sb, int level, string name, bool value, bool comma)
        {
            sb.Append(Indent(level)).Append('"').Append(name).Append("\": ").Append(value ? "true" : "false");
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendVectorProperty(StringBuilder sb, int level, string name, Vector2 value, bool comma)
        {
            sb.Append(Indent(level)).Append('"').Append(name).Append("\": ");
            AppendVector(sb, value);
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendVectorProperty(StringBuilder sb, int level, string name, Vector3 value, bool comma)
        {
            sb.Append(Indent(level)).Append('"').Append(name).Append("\": ");
            AppendVector(sb, value);
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendVectorArrayProperty(StringBuilder sb, int level, string name, IEnumerable<Vector2> values, bool comma)
        {
            var array = values.ToArray();
            sb.Append(Indent(level)).Append('"').Append(name).Append("\": [");
            for (var i = 0; i < array.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                AppendVector(sb, array[i]);
            }
            sb.Append(']');
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendPolygonPathsProperty(StringBuilder sb, int level, string name, PolygonCollider2D polygon, Transform mapTransform, bool comma)
        {
            sb.AppendLine(Indent(level) + $"\"{name}\": [");
            for (var pathIndex = 0; pathIndex < polygon.pathCount; pathIndex++)
            {
                var points = polygon.GetPath(pathIndex).Select(point => ColliderPointToMap(polygon.transform, mapTransform, polygon.offset + point)).ToArray();
                sb.Append(Indent(level + 1)).Append('[');
                for (var i = 0; i < points.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    AppendVector(sb, points[i]);
                }
                sb.Append(pathIndex == polygon.pathCount - 1 ? "]" : "],");
                sb.AppendLine();
            }
            sb.Append(Indent(level)).Append(']');
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendBoundsProperty(StringBuilder sb, int level, string name, Bounds bounds, bool comma)
        {
            sb.AppendLine(Indent(level) + $"\"{name}\": {{");
            AppendVectorProperty(sb, level + 1, "center", bounds.center, comma: true);
            AppendVectorProperty(sb, level + 1, "size", bounds.size, comma: true);
            AppendVectorProperty(sb, level + 1, "min", bounds.min, comma: true);
            AppendVectorProperty(sb, level + 1, "max", bounds.max, comma: false);
            sb.Append(Indent(level)).Append('}');
            if (comma) sb.Append(',');
            sb.AppendLine();
        }

        private static void AppendVector(StringBuilder sb, Vector2 value)
        {
            sb.Append("{\"x\": ")
                .Append(value.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append(", \"y\": ")
                .Append(value.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append('}');
        }

        private static void AppendVector(StringBuilder sb, Vector3 value)
        {
            sb.Append("{\"x\": ")
                .Append(value.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append(", \"y\": ")
                .Append(value.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append(", \"z\": ")
                .Append(value.z.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                .Append('}');
        }
        #endregion
#endif

        #region 加载数据
        /// <summary>
        /// 读取二进制文件
        /// </summary>
        public static byte[] ReadBinaryFile(string path)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// 加载漩涡数据
        /// </summary>
        public static void LoadVortexFromBin(string path, out List<Variable.Vortex> list)
        {
            var data = ReadBinaryFile($"AlchAssV3.Bins.{path}.bin");
            var reader = new BinaryReader(new MemoryStream(data));
            var len = reader.ReadInt32();
            list = new List<Variable.Vortex>(len);
            for (var i = 0; i < len; i++)
                list.Add(new Variable.Vortex { x = reader.ReadDouble(), y = reader.ReadDouble(), r = reader.ReadDouble() });
        }

        /// <summary>
        /// 加载区域数据
        /// </summary>
        public static void LoadZoneFromBin(string path, out List<Variable.Shape> listShape, out List<Variable.Node> listNode)
        {
            var data = ReadBinaryFile($"AlchAssV3.Bins.{path}.bin");
            var reader = new BinaryReader(new MemoryStream(data));
            var len_lines = reader.ReadInt32();
            var len_arcs = reader.ReadInt32();
            var len_nodes = reader.ReadInt32();
            listShape = new List<Variable.Shape>(len_lines + len_arcs);
            listNode = new List<Variable.Node>(len_nodes);

            for (var i = 0; i < len_lines; i++)
            {
                var x1 = reader.ReadDouble();
                var y1 = reader.ReadDouble();
                var x2 = reader.ReadDouble();
                var y2 = reader.ReadDouble();
                listShape.Add(new Variable.Shape.Line(x1, y1, x2, y2));
            }
            for (var i = 0; i < len_arcs; i++)
            {
                var x = reader.ReadDouble();
                var y = reader.ReadDouble();
                var r = reader.ReadDouble();
                var start = reader.ReadDouble();
                var end = reader.ReadDouble();
                listShape.Add(new Variable.Shape.Arc(x, y, r, start, end));
            }

            for (var i = 0; i < len_nodes; i++)
            {
                var isLeaf = reader.ReadBoolean();
                reader.ReadBytes(7);
                var minX = reader.ReadDouble();
                var minY = reader.ReadDouble();
                var maxX = reader.ReadDouble();
                var maxY = reader.ReadDouble();

                if (isLeaf)
                {
                    var itemCount = reader.ReadInt32();
                    var items = new int[itemCount];
                    for (var j = 0; j < itemCount; j++)
                        items[j] = reader.ReadInt32();
                    listNode.Add(new Variable.Node.LeafNode(minX, minY, maxX, maxY, items));
                }
                else
                {
                    var left = reader.ReadInt32();
                    var right = reader.ReadInt32();
                    listNode.Add(new Variable.Node.InternalNode(minX, minY, maxX, maxY, left, right));
                }
            }
        }

        /// <summary>
        /// 加载二进制资源
        /// </summary>
        public static void LoadFromBins()
        {
            LoadVortexFromBin("Vortex_Water", out Variable.Vortexs[0]);
            LoadVortexFromBin("Vortex_Oil", out Variable.Vortexs[1]);
            LoadZoneFromBin("Strong_Water", out Variable.Strongs[0], out Variable.StrongBVHs[0]);
            LoadZoneFromBin("Strong_Oil", out Variable.Strongs[1], out Variable.StrongBVHs[1]);
            LoadZoneFromBin("Strong_Wine", out Variable.Strongs[2], out Variable.StrongBVHs[2]);
            LoadZoneFromBin("Weak_Wine", out Variable.WeakWine, out Variable.WeakWineBVH);
            LoadZoneFromBin("Heal_Wine", out Variable.HealWine, out Variable.HealWineBVH);
            LoadZoneFromBin("Swamp_Oil", out Variable.SwampOil, out Variable.SwampOilBVH);
        }
        #endregion

        #region 格式化文本
        /// <summary>
        /// 格式化位置文本
        /// </summary>
        public static string FormatPosition(Vector2 position)
        {
            if (Variable.DisplayPolar)
                return $"{(position.magnitude, Vector2.SignedAngle(Vector2.right, position))}";
            return $"{(position.x, position.y)}";
        }

        /// <summary>
        /// 格式化月盐文本
        /// </summary>
        public static string FormatMoonSalt(float rotation)
        {
            var angle = Mathf.DeltaAngle(rotation, 0f);
            if (!Variable.DisplaySalt)
                return $"{angle}°";
            if (angle < 0)
                return $"<sprite=\"IconsAtlas\" name=\"MoonSalt\"> {-angle / 9f * 25f}";
            return $"<sprite=\"IconsAtlas\" name=\"SunSalt\"> {angle / 9f * 25f}";
        }

        /// <summary>
        /// 格式化血盐文本
        /// </summary>
        public static string FormatLifeSalt(double DangerDistance)
        {
            var hp = (float)DangerDistance * 40f - 100f;
            if (!Variable.DisplaySalt)
                return $"{hp}%";
            return $"<sprite=\"IconsAtlas\" name=\"LifeSalt\"> {hp * 2.5f}";
        }

        /// <summary>
        /// 格式化本地化文本
        /// </summary>
        public static void FormatLocalization()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] tags = ["Label", "Title", "Button", "Help"];
            foreach (var tag in tags)
                Localization.RegisterLocalization($"AlchAssV3.Locs.{tag}.json", assembly);
        }
        #endregion
    }
}
