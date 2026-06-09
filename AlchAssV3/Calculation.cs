using HarmonyLib;
using PotionCraft.LocalizationSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Mortar;
using PotionCraft.ObjectBased.RecipeMap.RecipeMapItem.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AlchAssV3
{
    public static class Calculation
    {
        #region 窗口信息计算
        /// <summary>
        /// 计算路径信息
        /// </summary>
        public static string CalculatePath()
        {
            string devTotText = LocalizationManager.GetText("label_unavailable");
            string devPosText = LocalizationManager.GetText("label_unavailable");
            string closestDirText = LocalizationManager.GetText("label_unavailable");
            string deltaAngleText = LocalizationManager.GetText("label_unavailable");
            string lifeSaltText = LocalizationManager.GetText("label_unavailable");
            string swampDisText = LocalizationManager.GetText("label_unavailable");

            if (!float.IsNaN(Variable.ClosestPositions[0].x))
            {
                Vector2 targetPos = Variable.TargetEffect.transform.localPosition;
                var targetRot = Variable.TargetEffect.transform.localEulerAngles.z;
                var devPos = Vector2.Distance(targetPos, Variable.ClosestPositions[0]) * 1800f;
                var devRot = Mathf.Abs(Mathf.DeltaAngle(Managers.RecipeMap.indicatorRotation.Value, targetRot)) / 3f * 25f;
                var devTot = devPos + devRot;

                var lvlPos = devPos <= 100f ? 3 : devPos <= 600f ? 2 : devPos <= 2754f ? 1 : 0;
                var lvlTot = devTot <= 100f ? 3 : devTot <= 600f ? 2 : devPos <= 2754f ? 1 : 0;
                var closestDir = Vector2.SignedAngle(Vector2.right, Variable.ClosestPositions[0] - targetPos);

                devPosText = $"<color=red>L{lvlPos}</color> {devPos}%";
                devTotText = $"<color=red>L{lvlTot}</color> {devTot}%";
                closestDirText = $"{closestDir}°";
            }

            if (!double.IsNaN(Variable.LineDirections[0]) && !double.IsNaN(Variable.LineDirections[2]))
            {
                var deltaAng = Mathf.DeltaAngle((float)Variable.LineDirections[0], (float)Variable.LineDirections[2]);
                deltaAngleText = $"{deltaAng}°";
            }
            if (!double.IsNaN(Variable.DangerDistancePath))
                lifeSaltText = Function.FormatLifeSalt(Variable.DangerDistancePath);
            if (!double.IsNaN(Variable.DistanceSwamp))
                swampDisText = $"{(float)Variable.DistanceSwamp}";
            return $"""
                {LocalizationManager.GetText("label_total_deviation")}: {devTotText}
                {LocalizationManager.GetText("label_position_deviation")}: {devPosText}
                {LocalizationManager.GetText("label_proximity_direction")}: {closestDirText}
                {LocalizationManager.GetText("label_effect_angle")}: {deltaAngleText}
                {LocalizationManager.GetText("label_healing_requirement")}: {lifeSaltText}
                {LocalizationManager.GetText("label_swamp_length")}: {swampDisText}
                """;
        }

        /// <summary>
        /// 计算加水信息
        /// </summary>
        public static string CalculateLadle()
        {
            string devTotText = LocalizationManager.GetText("label_unavailable");
            string devPosText = LocalizationManager.GetText("label_unavailable");
            string closestDirText = LocalizationManager.GetText("label_unavailable");
            string deltaAngleText = LocalizationManager.GetText("label_unavailable");
            string lifeSaltText = LocalizationManager.GetText("label_unavailable");

            if (!float.IsNaN(Variable.ClosestPositions[2].x))
            {
                Vector2 targetPos = Variable.TargetEffect.transform.localPosition;
                var targetRot = Variable.TargetEffect.transform.localEulerAngles.z;
                var devPos = Vector2.Distance(targetPos, Variable.ClosestPositions[2]) * 1800f;
                var devRot = Mathf.Abs(Mathf.DeltaAngle(Managers.RecipeMap.indicatorRotation.Value, targetRot)) / 3f * 25f;
                var devTot = devPos + devRot;

                var lvlPos = devPos <= 100f ? 3 : devPos <= 600f ? 2 : devPos <= 2754f ? 1 : 0;
                var lvlTot = devTot <= 100f ? 3 : devTot <= 600f ? 2 : devPos <= 2754f ? 1 : 0;
                var closestDir = Vector2.SignedAngle(Vector2.right, Variable.ClosestPositions[2] - targetPos);

                devPosText = $"<color=red>L{lvlPos}</color> {devPos}%";
                devTotText = $"<color=red>L{lvlTot}</color> {devTot}%";
                closestDirText = $"{closestDir}°";
            }

            if (!double.IsNaN(Variable.LineDirections[1]) && !double.IsNaN(Variable.LineDirections[2]))
            {
                var deltaAng = Mathf.DeltaAngle((float)Variable.LineDirections[1], (float)Variable.LineDirections[2]);
                deltaAngleText = $"{deltaAng}°";
            }
            if (!double.IsNaN(Variable.DangerDistanceLadle))
                lifeSaltText = Function.FormatLifeSalt(Variable.DangerDistanceLadle);
            return $"""
                {LocalizationManager.GetText("label_total_deviation")}: {devTotText}
                {LocalizationManager.GetText("label_position_deviation")}: {devPosText}
                {LocalizationManager.GetText("label_proximity_direction")}: {closestDirText}
                {LocalizationManager.GetText("label_effect_angle")}: {deltaAngleText}
                {LocalizationManager.GetText("label_healing_requirement")}: {lifeSaltText}
                """;
        }

        /// <summary>
        /// 计算移动信息
        /// </summary>
        public static string CalculateMove()
        {
            var phase = Managers.RecipeMap.path.deletedGraphicsSegments;
            var progress = Managers.RecipeMap.path.segmentLengthToDeletePhysics;
            var pathDir = double.IsNaN(Variable.LineDirections[0]) ? LocalizationManager.GetText("label_unavailable") : $"{(float)Variable.LineDirections[0]}°";
            var ladleDir = double.IsNaN(Variable.LineDirections[1]) ? LocalizationManager.GetText("label_unavailable") : $"{(float)Variable.LineDirections[1]}°";
            var vortexText = LocalizationManager.GetText("label_unavailable");
            if (Managers.RecipeMap.CurrentVortexMapItem != null)
            {
                Vector2 vortexPos = Managers.RecipeMap.CurrentVortexMapItem.thisTransform.localPosition;
                var p = vortexPos - GetPotionBasePosition();
                var r = ((CircleCollider2D)Traverse.Create(Managers.RecipeMap.CurrentVortexMapItem).Field("vortexCollider").GetValue()).radius + 0.74f;
                var a = (float)Variable.VortexA;
                var b = r * r / (p.magnitude * Mathf.Sqrt(r * r + a * a));
                var vortexDir = (Mathf.Atan2(p.y, p.x) + Mathf.Asin(b)) * Mathf.Rad2Deg;
                vortexText = $"{(vortexDir > 0f ? vortexDir - 180f : vortexDir + 180f)}°";
            }
            if (Variable.DisplayStage)
                return $"""
                {LocalizationManager.GetText("label_stir_phase")}: {phase}
                {LocalizationManager.GetText("label_phase_progress")}: {progress}
                {LocalizationManager.GetText("label_path_direction")}: {pathDir}
                {LocalizationManager.GetText("label_ladle_direction")}: {ladleDir}
                {LocalizationManager.GetText("label_vortex_tangency")}: {vortexText}
                """;
            return $"""
                {LocalizationManager.GetText("label_stir_progress")}: {phase + progress}
                {LocalizationManager.GetText("label_path_direction")}: {pathDir}
                {LocalizationManager.GetText("label_ladle_direction")}: {ladleDir}
                {LocalizationManager.GetText("label_vortex_tangency")}: {vortexText}
                """;
        }

        /// <summary>
        /// 计算目标效果
        /// </summary>
        public static string CalculateEffect()
        {
            if (Variable.TargetEffect == null)
                return "";

            var targetId = Variable.TargetEffect.Effect.GetLocalizedTitle();
            Vector2 targetPos = Variable.TargetEffect.transform.localPosition;
            var targetRot = Mathf.DeltaAngle(Variable.TargetEffect.transform.localEulerAngles.z, 0f) / 9f * 25f;
            var posText = Function.FormatPosition(targetPos);
            var rotText = Function.FormatMoonSalt(targetRot);
            var dirText = double.IsNaN(Variable.LineDirections[2]) ? LocalizationManager.GetText("label_unavailable") : $"{(float)Variable.LineDirections[2]}°";
            return $"""
                {LocalizationManager.GetText("label_target_effect")}: {targetId}
                {LocalizationManager.GetText("label_position")}: {posText}
                {LocalizationManager.GetText("label_rotation")}: {rotText}
                {LocalizationManager.GetText("label_effect_direction")}: {dirText}
                """;
        }

        /// <summary>
        /// 计算酿造信息
        /// </summary>
        public static string CalculateBrewing(float health)
        {
            var indPos = GetIndicatorMapCheckPosition();
            var offPos = GetIndicatorLocalOffset();
            var indRot = Managers.RecipeMap.indicatorRotation.Value;
            var posText = Function.FormatPosition(indPos);
            var offText = Function.FormatPosition(offPos);
            var rotText = Function.FormatMoonSalt(indRot);
            return $"""
                {LocalizationManager.GetText("label_position")}: {posText}
                {LocalizationManager.GetText("label_offset")}: {offText}
                {LocalizationManager.GetText("label_rotation")}: {rotText}
                {LocalizationManager.GetText("label_health")}: {health * 100f}%
                """;
        }

        /// <summary>
        /// 计算效果偏离
        /// </summary>
        public static string CalculateDeviation()
        {
            if (Variable.TargetEffect == null)
                return "";

            Vector2 targetPos = Variable.TargetEffect.transform.localPosition;
            var targetRot = Variable.TargetEffect.transform.localEulerAngles.z;
            var indPos = GetIndicatorMapCheckPosition();
            var indRot = Managers.RecipeMap.indicatorRotation.Value;

            var devPos = Vector2.Distance(targetPos, indPos) * 1800f;
            var devRot = Mathf.Abs(Mathf.DeltaAngle(indRot, targetRot)) / 3f * 25f;
            var devTot = devPos + devRot;

            var lvlPos = devPos <= 100f ? 3 : devPos <= 600f ? 2 : devPos <= 2754f ? 1 : 0;
            var lvlRot = devRot <= 100f ? 3 : devRot <= 600f ? 2 : 1;
            var lvlTot = devTot <= 100f ? 3 : devTot <= 600f ? 2 : devPos <= 2754f ? 1 : 0;
            return $"""
                {LocalizationManager.GetText("label_total_deviation")}: <color=red>L{lvlTot}</color> {devTot}%
                {LocalizationManager.GetText("label_position_deviation")}: <color=red>L{lvlPos}</color> {devPos}%
                {LocalizationManager.GetText("label_rotation_deviation")}: <color=red>L{lvlRot}</color> {devRot}%
                """;
        }

        /// <summary>
        /// 计算活跃漩涡
        /// </summary>
        public static string CalculateVortex()
        {
            if (Managers.RecipeMap.CurrentVortexMapItem == null)
                return "";

            var indPos = GetIndicatorLogicPosition();
            Vector2 vortexPos = Managers.RecipeMap.CurrentVortexMapItem.thisTransform.localPosition;

            var disText = $"{Vector2.Distance(vortexPos, indPos)}";
            var maxText = $"{((CircleCollider2D)Traverse.Create(Managers.RecipeMap.CurrentVortexMapItem).Field("vortexCollider").GetValue()).radius + 0.74f}";
            var dirText = $"{Vector2.SignedAngle(Vector2.right, vortexPos - indPos)}°";
            var tanText = double.IsNaN(Variable.LineDirections[4]) ? LocalizationManager.GetText("label_unavailable") : $"{(float)Variable.LineDirections[4]}°";
            var lfsText = double.IsNaN(Variable.DangerDistanceVortex) ? LocalizationManager.GetText("label_unavailable") : Function.FormatLifeSalt(Variable.DangerDistanceVortex);
            return $"""
                {LocalizationManager.GetText("label_vortex_distance")}: {disText}
                {LocalizationManager.GetText("label_maximum_distance")}: {maxText}
                {LocalizationManager.GetText("label_vortex_direction")}: {dirText}
                {LocalizationManager.GetText("label_vortex_tangent")}: {tanText}
                {LocalizationManager.GetText("label_healing_requirement")}: {lfsText}
                """;
        }

        /// <summary>
        /// 计算目标漩涡
        /// </summary>
        public static string CalculateTargetVortex()
        {
            var mapindex = Variable.MapId[Managers.RecipeMap.currentMap.potionBase.name];
            if (mapindex == 2 || Variable.VortexIndex[mapindex] < 0)
                return "";

            var indPos = GetIndicatorMapCheckPosition();
            var selVortex = Variable.Vortexs[mapindex][Variable.VortexIndex[mapindex]];
            var vortexPos = new Vector2((float)selVortex.x, (float)selVortex.y);

            var disText = $"{Vector2.Distance(vortexPos, indPos)}";
            var maxText = $"{(float)selVortex.r}";
            var dirText = double.IsNaN(Variable.LineDirections[3]) ? LocalizationManager.GetText("label_unavailable") : $"{(float)Variable.LineDirections[3]}°";
            var strText = float.IsNaN(Variable.ClosestPositions[1].x) ? LocalizationManager.GetText("label_unavailable") : $"{Vector2.Distance(vortexPos, Variable.ClosestPositions[1])}";
            var ldlText = float.IsNaN(Variable.ClosestPositions[3].x) ? LocalizationManager.GetText("label_unavailable") : $"{Vector2.Distance(vortexPos, Variable.ClosestPositions[3])}";
            return $"""
                {LocalizationManager.GetText("label_vortex_distance")}: {disText}
                {LocalizationManager.GetText("label_path_proximity")}: {strText}
                {LocalizationManager.GetText("label_ladle_proximity")}: {ldlText}
                {LocalizationManager.GetText("label_maximum_distance")}: {maxText}
                {LocalizationManager.GetText("label_vortex_direction")}: {dirText}
                """;
        }

        /// <summary>
        /// 计算研磨信息
        /// </summary>
        public static string CalculateGrind(Mortar mortar)
        {
            if (mortar.ContainedStack == null)
                return "";
            return $"{LocalizationManager.GetText("label_grind_progress")}: {mortar.ContainedStack.overallGrindStatus * 100f}%";
        }
        #endregion

        #region 渲染信息计算
        /// <summary>
        /// 指示器逻辑位置。游戏加水和漩涡移动主要使用这个容器位置。
        /// </summary>
        public static Vector2 GetIndicatorLogicPosition()
        {
            return Managers.RecipeMap.recipeMapObject.indicatorContainer.localPosition;
        }

        /// <summary>
        /// 指示器子物体局部偏移。用于调试显示和 collider 轨迹推导。
        /// </summary>
        public static Vector2 GetIndicatorLocalOffset()
        {
            return Managers.RecipeMap.indicator.thisTransform.localPosition;
        }

        /// <summary>
        /// 指示器 collider 当前随子物体偏移后的地图坐标。
        /// </summary>
        public static Vector2 GetIndicatorColliderPosition()
        {
            return GetIndicatorLogicPosition() + GetIndicatorLocalOffset();
        }

        /// <summary>
        /// 指示器 collider transform 的实际地图坐标，用于校验 Unity 触发器碰撞位置。
        /// </summary>
        public static Vector2 GetIndicatorColliderMapPosition()
        {
            var mapTrans = Managers.RecipeMap.currentMap.referencesContainer.transform;
            return mapTrans.InverseTransformPoint(Managers.RecipeMap.indicator.circleCollider.transform.position);
        }

        /// <summary>
        /// 指示器 collider 实际地图坐标相对容器坐标的偏移。
        /// </summary>
        public static Vector2 GetIndicatorColliderMapOffset()
        {
            return GetIndicatorColliderMapPosition() - GetIndicatorLogicPosition();
        }

        /// <summary>
        /// 游戏的效果等级距离、溶剂方向和显式地图物体距离检查均使用容器位置。
        /// </summary>
        public static Vector2 GetIndicatorMapCheckPosition()
        {
            return GetIndicatorLogicPosition();
        }

        /// <summary>
        /// 溶剂会把指示器推向当前药剂基底的地图位置。
        /// </summary>
        public static Vector2 GetPotionBasePosition()
        {
            return Managers.RecipeMap.currentMap.referencesContainer.potionBaseMapItem.transform.localPosition;
        }

        /// <summary>
        /// 缓存指示器子物体局部偏移，仅用于信息窗口显示。
        /// </summary>
        public static void UpdateOffset()
        {
            Variable.Offset = GetIndicatorLocalOffset();
        }

        /// <summary>
        /// 加水过程中的碰撞轨迹终点。
        /// </summary>
        public static Vector2 GetLadleCollisionTargetPosition()
        {
            return GetIndicatorColliderPosition() + (GetPotionBasePosition() - GetIndicatorLogicPosition());
        }

        /// <summary>
        /// 计算路径方向
        /// </summary>
        public static void GetPathLineDirection()
        {
            if (!Variable.DoLines[0] || Variable.PathPhysical.Count < 2)
                Variable.LineDirections[0] = double.NaN;
            else
                Variable.LineDirections[0] = Vector2.SignedAngle(Vector2.right, Variable.PathPhysical[1].Item1 - Variable.PathPhysical[0].Item1);
        }

        /// <summary>
        /// 计算加水方向
        /// </summary>
        public static void GetLadleLineDirection()
        {
            if (!Variable.DoLines[1])
                Variable.LineDirections[1] = double.NaN;
            else
                Variable.LineDirections[1] = Vector2.SignedAngle(Vector2.right, GetPotionBasePosition() - GetIndicatorLogicPosition());
        }

        /// <summary>
        /// 计算目标方向
        /// </summary>
        public static void GetTargetLineDirection()
        {
            if (!Variable.DoLines[2] || Variable.TargetEffect == null)
                Variable.LineDirections[2] = double.NaN;
            else
            {
                Vector2 targetPos = Variable.TargetEffect.transform.localPosition;
                var indPos = GetIndicatorMapCheckPosition();
                Variable.LineDirections[2] = Vector2.SignedAngle(Vector2.right, targetPos - indPos);
            }
        }

        /// <summary>
        /// 计算漩涡方向
        /// </summary>
        public static void GetVortexLineDirection()
        {
            if (!Variable.DoLines[3])
                Variable.LineDirections[3] = double.NaN;
            else
            {
                var mapindex = Variable.MapId[Managers.RecipeMap.currentMap.potionBase.name];
                if (mapindex == 2 || Variable.VortexIndex[mapindex] < 0)
                    Variable.LineDirections[3] = double.NaN;
                else
                {
                    var selVortex = Variable.Vortexs[mapindex][Variable.VortexIndex[mapindex]];
                    var vortexPos = new Vector2((float)selVortex.x, (float)selVortex.y);
                    var indPos = GetIndicatorMapCheckPosition();
                    Variable.LineDirections[3] = Vector2.SignedAngle(Vector2.right, vortexPos - indPos);
                }
            }
        }

        /// <summary>
        /// 计算漩涡切线
        /// </summary>
        public static void GetTangentLineDirection()
        {
            if (!Variable.DoLines[4] || double.IsNaN(Variable.VortexX))
                Variable.LineDirections[4] = double.NaN;
            else
            {
                var dir = Variable.VortexMaxAngle + Variable.VortexRotation + Math.Atan(Variable.VortexMaxAngle);
                var dirdeg = dir * 180 / Math.PI;
                Variable.LineDirections[4] = dirdeg % 360 - 180;
            }
        }

        /// <summary>
        /// 生成直线
        /// </summary>
        public static void InitLine(double theta, out Vector3[] Points)
        {
            Points = [];
            if (double.IsNaN(theta))
                return;

            var rad = theta * Math.PI / 180;
            var dx = Math.Cos(rad);
            var dy = Math.Sin(rad);
            var mapTrans = Managers.RecipeMap.currentMap.referencesContainer.transform;
            var indPos = GetIndicatorMapCheckPosition();
            List<Vector3> points = [];

            if (Math.Abs(dx) > 1e-5)
            {
                var t = (-80 - indPos.x) / dx;
                var y = indPos.y + t * dy;
                if (y >= -80 && y <= 80)
                    points.Add(mapTrans.TransformPoint(new(-80, (float)y)));
                t = (80 - indPos.x) / dx;
                y = indPos.y + t * dy;
                if (y >= -80 && y <= 80)
                    points.Add(mapTrans.TransformPoint(new(80, (float)y)));
            }
            if (Math.Abs(dy) > 1e-5)
            {
                var t = (-80 - indPos.y) / dy;
                var x = indPos.x + t * dx;
                if (x > -80 && x < 80)
                    points.Add(mapTrans.TransformPoint(new((float)x, -80)));
                t = (80 - indPos.y) / dy;
                x = indPos.x + t * dx;
                if (x > -80 && x < 80)
                    points.Add(mapTrans.TransformPoint(new((float)x, 80)));
            }
            Points = [.. points];
        }

        /// <summary>
        /// 生成路径曲线（整列和散列）
        /// </summary>
        public static void InitPathCurve()
        {
            Variable.PathPhysical = []; Variable.PathGraphical = []; Variable.SwampPositions = []; Variable.DistanceSwamp = double.NaN;
            if (!Variable.DoPathCurve)
                return;

            var pathHints = Managers.RecipeMap.path.fixedPathHints;
            if (pathHints.Count == 0)
                return;

            var mapTrans = Managers.RecipeMap.currentMap.referencesContainer.transform;
            var pathTrans = Managers.RecipeMap.path.thisTransform;
            var stIn = ZonePart.GetZonesActivePartsCount(typeof(SwampZonePart)) > 0;
            var stSet = Vector3.zero;
            var indPos = GetIndicatorLogicPosition();
            var mapId = Managers.RecipeMap.currentMap.potionBase.name;
            var lineIn = stIn;
            List<(Vector2, int, double)> swampPos = [];

            Variable.PathPhysical.Add((indPos, false));
            for (int i = 0; i < pathHints.Count; i++)
            {
                var hint = pathHints[i];
                var isTp = hint.GetType().Name == "TeleportationFixedHint";
                var points = hint.evenlySpacedPointsFixedPhysics.points.Select(point => mapTrans.InverseTransformPoint(pathTrans.TransformPoint(point))).ToList();
                var graphicalPoints = hint.evenlySpacedPointsFixedGraphics.points.Select(point => mapTrans.InverseTransformPoint(pathTrans.TransformPoint(point))).ToList();
                if (points.Count() < 2 || graphicalPoints.Count() < 2) continue;
                if (i == 0)
                {
                    points[0] = indPos;
                    graphicalPoints[0] = indPos;
                }
                if (isTp)
                {
                    points = [points[0], points[points.Count - 1]];
                    graphicalPoints = [graphicalPoints[0], graphicalPoints[graphicalPoints.Count - 1]];
                }
                if (Variable.DoSwampPoint && mapId == "Oil")
                {
                    Geometry.ScalePath(points, stIn, stSet, Variable.PathPhysical.Count - 1, isTp, out var pointsSc, out var pos, out var edIn, out var edSet);
                    stIn = edIn; stSet = edSet; points = pointsSc;
                    swampPos.AddRange(pos);
                }

                Variable.PathPhysical.AddRange(points.Skip(1).Select(point => (point, isTp)));
                Variable.PathGraphical.Add(([.. graphicalPoints.Select(point => mapTrans.TransformPoint(point))], isTp));
            }
            Variable.SwampPositions.AddRange(swampPos.Select(x => x.Item1));
            if (Variable.DoSwampPoint && mapId == "Oil")
                Geometry.SwampLine(Variable.PathPhysical, swampPos, lineIn, out Variable.DistanceSwamp);
        }

        /// <summary>
        /// 生成漩涡曲线（参数和散列）
        /// </summary>
        public static void InitVortexCurve()
        {
            Variable.VortexX = double.NaN; Variable.VortexY = double.NaN; Variable.VortexRotation = double.NaN;
            Variable.VortexMaxAngle = double.NaN; Variable.VortexMinAngle = double.NaN; Variable.VortexGraphical = [];
            if (!Variable.DoVortexCurve)
                return;

            var curVortex = Managers.RecipeMap.CurrentVortexMapItem;
            if (curVortex == null)
                return;

            Vector2 vortexPos = curVortex.thisTransform.localPosition;
            var indPos = GetIndicatorLogicPosition();
            var maxDis = ((CircleCollider2D)Traverse.Create(curVortex).Field("vortexCollider").GetValue()).radius + 0.74;
            var distance = Vector2.Distance(vortexPos, indPos);
            if (distance > maxDis + 1e-5)
                return;

            var v = indPos - vortexPos;
            var maxAng = v.magnitude / Variable.VortexA;
            var rot = (Math.Atan2(v.y, v.x) - maxAng) % (2 * Math.PI);
            Variable.VortexMinAngle = (maxDis - 1.55) / Variable.VortexA;
            Variable.VortexX = vortexPos.x;
            Variable.VortexY = vortexPos.y;
            Variable.VortexRotation = rot;
            Variable.VortexMaxAngle = maxAng;

            var mapTrans = Managers.RecipeMap.currentMap.referencesContainer.transform;
            Variable.VortexGraphical = new Vector3[Math.Max(10, (int)(distance * 250))];
            for (int i = 0; i < Variable.VortexGraphical.Length; i++)
            {
                var t = i / (double)Variable.VortexGraphical.Length;
                var angle = t * maxAng;
                var radius = Variable.VortexA * angle;
                var x = radius * Math.Cos(angle);
                var y = radius * Math.Sin(angle);
                var x_rot = x * Math.Cos(rot) - y * Math.Sin(rot) + vortexPos.x;
                var y_rot = x * Math.Sin(rot) + y * Math.Cos(rot) + vortexPos.y;
                Variable.VortexGraphical[i] = mapTrans.TransformPoint(new((float)x_rot, (float)y_rot));
            }
            Variable.VortexGraphical = [.. Variable.VortexGraphical.AddItem(mapTrans.TransformPoint(indPos))];
        }

        /// <summary>
        /// 生成边界圈
        /// </summary>
        public static void InitRange(double rad, double cx, double cy, out Vector3[] Points)
        {
            var r = rad - Variable.LineWidth.Value * 0.5;
            Points = new Vector3[Math.Max(10, (int)(r * 250))];
            var mapTrans = Managers.RecipeMap.currentMap.referencesContainer.transform;
            for (int i = 0; i < Points.Length; i++)
            {
                var t = i / (double)Points.Length;
                var angle = t * 2 * Math.PI;
                var x = r * Math.Cos(angle) + cx;
                var y = r * Math.Sin(angle) + cy;
                Points[i] = mapTrans.TransformPoint(new((float)x, (float)y));
            }
        }

        /// <summary>
        /// 生成关键点
        /// </summary>
        public static void InitPoints(double health)
        {
            Variable.ClosestPositions = [new Vector2(float.NaN, float.NaN), new Vector2(float.NaN, float.NaN), new Vector2(float.NaN, float.NaN), new Vector2(float.NaN, float.NaN)];
            Variable.DefeatPositions = [new Vector2(float.NaN, float.NaN), new Vector2(float.NaN, float.NaN), new Vector2(float.NaN, float.NaN)];
            Variable.DangerPositions = [[], [], []];
            Variable.IntersectionPositions = [[], [], [], []];
            Variable.DangerDistancePath = double.NaN;
            Variable.DangerDistanceLadle = double.NaN;
            Variable.DangerDistanceVortex = double.NaN;

            var indPos = GetIndicatorMapCheckPosition();
            var ladleColliderPos = GetIndicatorColliderPosition();
            var ladleTargetPos = GetLadleCollisionTargetPosition();
            List<(Vector3, bool)> pathLadle = [(ladleColliderPos, false), (ladleTargetPos, false)];
            bool[] inDanger = [
                ZonePart.GetZonesActivePartsCount(typeof(StrongDangerZonePart)) > 0,
                ZonePart.GetZonesActivePartsCount(typeof(WeakDangerZonePart)) > 0,
                ZonePart.GetZonesActivePartsCount(typeof(HealZonePart)) > 0];

            bool effectVaild = new(); bool vortexVaild = new();
            Vector2 effectPos = new(); Vector2 vortexPos = new();
            double vortexRad = new();
            var mapId = Managers.RecipeMap.currentMap.potionBase.name;
            if (Variable.TargetEffect != null)
            {
                effectPos = Variable.TargetEffect.transform.localPosition;
                effectVaild = true;
            }
            if (mapId != "Wine")
            {
                var mapindex = Variable.MapId[mapId];
                if (Variable.VortexIndex[mapindex] >= 0)
                {
                    var selVortex = Variable.Vortexs[mapindex][Variable.VortexIndex[mapindex]];
                    vortexPos = new Vector2((float)selVortex.x, (float)selVortex.y);
                    vortexRad = selVortex.r;
                    vortexVaild = true;
                }
            }
            var vortexIn = Variable.VortexMaxAngle > Variable.VortexMinAngle;

            var closeEPathEn = Variable.DoPathCurve && effectVaild;
            var closeELadleEn = Variable.DoLines[1] && effectVaild;
            var closeVPathEn = Variable.DoPathCurve && vortexVaild;
            var closeVLadleEn = Variable.DoLines[1] && vortexVaild;
            var effectPathEn = Variable.DoPathEffectPoint && effectVaild;
            var effectLadleEn = Variable.DoLadleEffectPoint && effectVaild;
            var vortexPathEn = Variable.DoPathVortexPoint && vortexVaild;
            var vortexLadleEn = Variable.DoLadleVortexPoint && vortexVaild;
            var dangerPathEn = Variable.DoPathDangerPoint;
            var dangerLadleEn = Variable.DoLadleDangerPoint;
            var dangerVortexEn = Variable.DoVortexDangerPoint && vortexIn;

            var lenPath = Variable.PathPhysical.Count() - 1;
            if (lenPath > 0)
            {
                if (closeEPathEn || closeVPathEn)
                {
                    var closeEPathMin = double.MaxValue;
                    var closeVPathMin = double.MaxValue;

                    for (var i = 0; i < lenPath; i++)
                    {
                        Vector2 p0 = Variable.PathPhysical[i].Item1;
                        Vector2 p1 = Variable.PathPhysical[i + 1].Item1;
                        var isTp = Variable.PathPhysical[i + 1].Item2;

                        if (closeEPathEn)
                        {
                            Geometry.SqrDisToPoint(p0, p1, effectPos, isTp, out var closeEPathDis, out var closeEPathPos);
                            if (closeEPathDis < closeEPathMin)
                            {
                                closeEPathMin = closeEPathDis;
                                Variable.ClosestPositions[0] = closeEPathPos;
                            }
                        }

                        if (closeVPathEn)
                        {
                            Geometry.SqrDisToPoint(p0, p1, vortexPos, isTp, out var closeVPathDis, out var closeVPathPos);
                            if (closeVPathDis < closeVPathMin)
                            {
                                closeVPathMin = closeVPathDis;
                                Variable.ClosestPositions[1] = closeVPathPos;
                            }
                        }
                    }
                }

                if (effectPathEn || vortexPathEn || dangerPathEn)
                {
                    List<(Vector2, int, double, int)> dangerPathSum = [];

                    for (var i = 0; i < lenPath; i += 100)
                    {
                        var lt = Math.Min(lenPath, i + 100);
                        var minx = -double.MaxValue; var maxx = double.MaxValue;
                        var miny = -double.MaxValue; var maxy = double.MaxValue;

                        for (var j = i; j <= lt; j++)
                        {
                            var x = Variable.PathPhysical[j].Item1.x;
                            var y = Variable.PathPhysical[j].Item1.y;
                            minx = Math.Min(minx, x); maxx = Math.Max(maxx, x);
                            miny = Math.Min(miny, y); maxy = Math.Max(maxy, y);
                        }

                        var effectPathEnC = effectPathEn && Geometry.RangeAABB(minx, miny, maxx, maxy, effectPos, 1.53);
                        var vortexPathEnC = vortexPathEn && Geometry.RangeAABB(minx, miny, maxx, maxy, vortexPos, vortexRad);
                        var dangerPathEnC = dangerPathEn && Geometry.DangerAABB(minx, miny, maxx, maxy, mapId);

                        for (var j = i; j < lt; j++)
                        {
                            Vector2 p0 = Variable.PathPhysical[j].Item1;
                            Vector2 p1 = Variable.PathPhysical[j + 1].Item1;
                            var isTp = Variable.PathPhysical[j + 1].Item2;

                            if (effectPathEnC)
                            {
                                Geometry.TargetRange(p0, p1, effectPos, isTp, out var effectPath);
                                Variable.IntersectionPositions[0].AddRange(effectPath);
                            }
                            if (vortexPathEnC)
                            {
                                Geometry.VortexRange(p0, p1, vortexPos, vortexRad, isTp, out var vortexPath);
                                Variable.IntersectionPositions[2].AddRange(vortexPath);
                            }
                            if (dangerPathEnC)
                            {
                                Geometry.DangerLine(p0, p1, mapId, j, isTp, out var dangerPath);
                                dangerPathSum.AddRange(dangerPath);
                            }
                        }
                    }

                    if (dangerPathEn)
                    {
                        Geometry.DefeatLine(Variable.PathPhysical, dangerPathSum, health, inDanger, mapId,
                            out Variable.DefeatPositions[0], out Variable.DangerDistancePath);
                        Variable.DangerPositions[0].AddRange(dangerPathSum.Select(x => x.Item1));
                    }
                }
            }

            if (closeELadleEn)
                Geometry.SqrDisToPoint(ladleColliderPos, ladleTargetPos, effectPos, false, out _, out Variable.ClosestPositions[2]);
            if (closeVLadleEn)
                Geometry.SqrDisToPoint(ladleColliderPos, ladleTargetPos, vortexPos, false, out _, out Variable.ClosestPositions[3]);
            if (effectLadleEn)
                Geometry.TargetRange(ladleColliderPos, ladleTargetPos, effectPos, false, out Variable.IntersectionPositions[1]);
            if (vortexLadleEn)
                Geometry.VortexRange(ladleColliderPos, ladleTargetPos, vortexPos, vortexRad, false, out Variable.IntersectionPositions[3]);
            if (dangerLadleEn)
            {
                Geometry.DangerLine(ladleColliderPos, ladleTargetPos, mapId, 0, false, out var dangerLadle);
                Geometry.DefeatLine(pathLadle, dangerLadle, health, inDanger, mapId, out Variable.DefeatPositions[1], out Variable.DangerDistanceLadle);
                Variable.DangerPositions[1].AddRange(dangerLadle.Select(x => x.Item1));
            }

            if (dangerVortexEn)
            {
                Geometry.DangerSpiral(Variable.VortexX, Variable.VortexY, Variable.VortexRotation,
                    Variable.VortexMaxAngle, Variable.VortexMinAngle, mapId, out var dangerVortex);
                Geometry.DefeatSpiral(Variable.VortexX, Variable.VortexY, Variable.VortexRotation, Variable.VortexMaxAngle,
                    dangerVortex, health, inDanger[0], out Variable.DefeatPositions[2], out Variable.DangerDistanceVortex);
                Variable.DangerPositions[2].AddRange(dangerVortex.Select(x => x.Item1));
            }
        }
        #endregion
    }
}
