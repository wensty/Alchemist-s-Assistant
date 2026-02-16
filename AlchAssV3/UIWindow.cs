using System;
using BepInEx.Configuration;
using PotionCraft.LocalizationSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlchAssV3
{
    public static class UIWindow
    {
        #region 主体渲染
        /// <summary>
        /// 生成样式
        /// </summary>
        public static void InitStyles()
        {
            Variable.WindowTexture = new(1, 1);
            Variable.WindowTexture.SetPixel(0, 0, new(0.12f, 0.12f, 0.12f, 0.95f));
            Variable.WindowTexture.Apply();

            Variable.WindowStyle = new(GUI.skin.window);
            Variable.WindowStyle.normal.background = Variable.WindowTexture;
            Variable.WindowStyle.onNormal.background = Variable.WindowTexture;
            Variable.WindowStyle.normal.textColor = Color.cyan;
            Variable.WindowStyle.onNormal.textColor = Color.cyan;
            Variable.WindowStyle.font = Variable.Font;
            Variable.WindowStyle.fontSize = 20;

            Variable.CategoryStyle = new(GUI.skin.button);
            Variable.CategoryStyle.normal.textColor = new(1.0f, 0.8f, 0.3f);
            Variable.CategoryStyle.hover.textColor = new(1.0f, 0.8f, 0.3f);
            Variable.CategoryStyle.font = Variable.Font;
            Variable.CategoryStyle.fontSize = 18;
            Variable.CategoryStyle.alignment = TextAnchor.MiddleLeft;
            Variable.CategoryStyle.padding = new(10, 10, 5, 5);

            Variable.ButtonStyle = new(GUI.skin.button);
            Variable.ButtonStyle.hover.textColor = Color.green;
            Variable.ButtonStyle.font = Variable.Font;
            Variable.ButtonStyle.margin = new(30, 30, 5, 5);
            Variable.ButtonStyle.padding = new(10, 10, 5, 5);

            Variable.DeleteButtonStyle = new(GUI.skin.button);
            Variable.DeleteButtonStyle.normal.textColor = Color.red;
            Variable.DeleteButtonStyle.hover.textColor = Color.red;
            Variable.DeleteButtonStyle.font = Variable.Font;
            Variable.DeleteButtonStyle.fontSize = 14;
            Variable.DeleteButtonStyle.margin = new(0, 30, 5, 0);
            Variable.DeleteButtonStyle.padding = new(10, 10, 6, 4);
            Variable.DeleteButtonStyle.fixedWidth = 50;

            Variable.ToggleStyle = new(GUI.skin.toggle);
            Variable.ToggleStyle.onNormal.textColor = new(0.4f, 0.8f, 1.0f);
            Variable.ToggleStyle.onHover.textColor = new(0.4f, 0.8f, 1.0f);
            Variable.ToggleStyle.font = Variable.Font;
            Variable.ToggleStyle.margin = new(30, 30, 6, 4);
            Variable.ToggleStyle.padding = new(25, 0, -1, 0);

            Variable.LabelStyle = new(GUI.skin.label)
            {
                font = Variable.Font,
                margin = new(30, 10, 5, 5)
            };

            Variable.SliderStyle = new(GUI.skin.horizontalSlider)
            {
                margin = new(0, 5, 14, 0)
            };

            Variable.TextFieldStyle = new(GUI.skin.textField);
            Variable.TextFieldStyle.normal.textColor = Color.green;
            Variable.TextFieldStyle.font = Variable.Font;
            Variable.TextFieldStyle.fontSize = 14;
            Variable.TextFieldStyle.margin = new(0, 10, 7, 0);
            Variable.TextFieldStyle.fixedWidth = 70;

            Variable.TextFieldErrorStyle = new(GUI.skin.textField);
            Variable.TextFieldErrorStyle.normal.textColor = Color.red;
            Variable.TextFieldErrorStyle.font = Variable.Font;
            Variable.TextFieldErrorStyle.fontSize = 14;
            Variable.TextFieldErrorStyle.margin = new(0, 10, 7, 0);
            Variable.TextFieldErrorStyle.fixedWidth = 70;
        }

        /// <summary>
        /// 设置窗口大小
        /// </summary>
        public static void Resizing()
        {
            var ResizeRect = new Rect(Variable.WindowRect.xMax - 25, Variable.WindowRect.yMax - 25, 25, 25);
            var mousePos = Mouse.current.position.ReadValue();
            mousePos.y = Screen.height - mousePos.y;

            if (Mouse.current.leftButton.wasPressedThisFrame && ResizeRect.Contains(mousePos))
            {
                Variable.IsResizing = true;
                Variable.LastMousePosition = mousePos;
            }
            if (Variable.IsResizing)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    var delta = mousePos - Variable.LastMousePosition;
                    Variable.WindowRect.width = Mathf.Max(400, Variable.WindowRect.width + delta.x);
                    Variable.WindowRect.height = Mathf.Max(400, Variable.WindowRect.height + delta.y);
                    Variable.LastMousePosition = mousePos;
                }
                else
                    Variable.IsResizing = false;
            }
        }

        /// <summary>
        /// 绘制窗口
        /// </summary>
        public static void DrawWindow(int _)
        {
            GUILayout.Space(8);
            Variable.ScrollPosition = GUILayout.BeginScrollView(Variable.ScrollPosition);

            DrawEnables();
            DrawDisplays();
            DrawActions();
            DrawCustomList();
            DrawExpansion();

            GUILayout.EndScrollView();
            GUILayout.Space(5);
            GUI.DragWindow(new Rect(0, 0, Variable.WindowRect.width, 30));
        }
        #endregion

        #region 元素渲染
        /// <summary>
        /// 辅助函数：获取文本（快捷键或开关功能）
        /// </summary>
        /// <param name="textKey"></param>
        /// <param name="shortcut"></param>
        /// <returns></returns>
        public static string GetLayoutTextString(string textKey, KeyboardShortcut shortcut)
        {
            if (Keyboard.current.backslashKey.ReadValue() == 1)
            {
                return shortcut.Serialize();
            }
            return LocalizationManager.GetText(textKey);
            // return string.Format("{0}({1})", LocalizationManager.GetText(textKey), shortcut.Serialize());
        }

        /// <summary>
        /// 绘制功能选项
        /// </summary>
        public static void DrawEnables()
        {
            var icon = $"{(Variable.EnableExpand ? "▼" : "▲")} {LocalizationManager.GetText("渲染选项")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.EnableExpand = !Variable.EnableExpand;

            if (Variable.EnableExpand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                Variable.EnablePathLine = GUILayout.Toggle(Variable.EnablePathLine, GetLayoutTextString("路径切向线", Variable.KeyEnablePathLine.Value), Variable.ToggleStyle);
                Variable.EnableLadleLine = GUILayout.Toggle(Variable.EnableLadleLine, GetLayoutTextString("加水方向线", Variable.KeyEnableLadleLine.Value), Variable.ToggleStyle);
                Variable.EnableEffectLine = GUILayout.Toggle(Variable.EnableEffectLine, GetLayoutTextString("效果径向线", Variable.KeyEnableEffectLine.Value), Variable.ToggleStyle);
                Variable.EnableVortexLine = GUILayout.Toggle(Variable.EnableVortexLine, GetLayoutTextString("漩涡径向线", Variable.KeyEnableVortexLine.Value), Variable.ToggleStyle);
                Variable.EnableTangentLine = GUILayout.Toggle(Variable.EnableTangentLine, GetLayoutTextString("漩涡切向线", Variable.KeyEnableTangentLine.Value), Variable.ToggleStyle);
                Variable.EnableCustomLine = GUILayout.Toggle(Variable.EnableCustomLine, GetLayoutTextString("自定义方向线", Variable.KeyEnableCustomLine.Value), Variable.ToggleStyle);
                Variable.EnablePathCurve = GUILayout.Toggle(Variable.EnablePathCurve, GetLayoutTextString("路径曲线", Variable.KeyEnablePathCurve.Value), Variable.ToggleStyle);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                Variable.EnableVortexCurve = GUILayout.Toggle(Variable.EnableVortexCurve, GetLayoutTextString("漩涡曲线", Variable.KeyEnableVortexCurve.Value), Variable.ToggleStyle);
                Variable.EnableEffectRange = GUILayout.Toggle(Variable.EnableEffectRange, GetLayoutTextString("效果范围", Variable.KeyEnableEffectRange.Value), Variable.ToggleStyle);
                Variable.EnableVortexRange = GUILayout.Toggle(Variable.EnableVortexRange, GetLayoutTextString("漩涡范围", Variable.KeyEnableVortexRange.Value), Variable.ToggleStyle);
                Variable.EnableDangerSimulation = GUILayout.Toggle(Variable.EnableDangerSimulation, GetLayoutTextString("骷髅区模拟", Variable.KeyEnableDangerSimulation.Value), Variable.ToggleStyle);
                Variable.EnableSwampSimulation = GUILayout.Toggle(Variable.EnableSwampSimulation, GetLayoutTextString("沼泽区模拟", Variable.KeyEnableSwampSimulation.Value), Variable.ToggleStyle);
                Variable.EnableTransparency = GUILayout.Toggle(Variable.EnableTransparency, GetLayoutTextString("透明瓶身", Variable.KeyEnableTransparency.Value), Variable.ToggleStyle);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                Function.UpdateDoFromEnable();
            }
            GUILayout.Space(10);
        }

        /// <summary>
        /// 绘制显示选项
        /// </summary>
        public static void DrawDisplays()
        {
            var icon = $"{(Variable.DisplayExpand ? "▼" : "▲")} {LocalizationManager.GetText("显示选项")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.DisplayExpand = !Variable.DisplayExpand;

            if (Variable.DisplayExpand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                Variable.DisplaySalt = GUILayout.Toggle(Variable.DisplaySalt, GetLayoutTextString("盐量数据", Variable.KeyToggleDisplaySalt.Value), Variable.ToggleStyle);
                Variable.DisplayStage = GUILayout.Toggle(Variable.DisplayStage, GetLayoutTextString("搅拌阶段", Variable.KeyToggleDisplayStage.Value), Variable.ToggleStyle);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                Variable.DisplayPolar = GUILayout.Toggle(Variable.DisplayPolar, GetLayoutTextString("极坐标", Variable.KeyToggleDisplayPolar.Value), Variable.ToggleStyle);
                Variable.OffsetCorrection = GUILayout.Toggle(Variable.OffsetCorrection, GetLayoutTextString("碰撞体修正", Variable.KeyToggleDisplayOffset.Value), Variable.ToggleStyle);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
        }

        /// <summary>
        /// 绘制即时功能
        /// </summary>
        public static void DrawActions()
        {
            var icon = $"{(Variable.ActionExpand ? "▼" : "▲")} {LocalizationManager.GetText("窗口管理")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.ActionExpand = !Variable.ActionExpand;

            if (Variable.ActionExpand)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(LocalizationManager.GetText("恢复所有窗口"), Variable.ButtonStyle))
                    Function.RestoreDebugWindows();
                if (GUILayout.Button(LocalizationManager.GetText("保存窗口布局"), Variable.ButtonStyle))
                    Function.SaveDebugWindowPos();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);
        }

        /// <summary>
        /// 绘制自定义方向线管理
        /// </summary>
        public static void DrawCustomList()
        {
            var icon = $"{(Variable.CustomListExpand ? "▼" : "▲")} {LocalizationManager.GetText("自定义方向线管理")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.CustomListExpand = !Variable.CustomListExpand;

            if (Variable.CustomListExpand)
            {
                if (GUILayout.Button($"+ {LocalizationManager.GetText("添加方向线")}", Variable.ButtonStyle))
                {
                    Variable.CustomLineDirections.Add(0f);
                    Variable.CustomLineHovers.Add(false);
                    Variable.Inputs.Add(("0", false));

                    var cnt = (int)Mathf.Ceil(Mathf.Log10(Variable.CustomLineDirections.Count + 1));
                    Variable.LabelWidth = Localization.GetLabelWidth([new('0', cnt)], false);
                }

                for (int i = 0; i < Variable.CustomLineDirections.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{i + 1}.", Variable.LabelStyle, GUILayout.Width(Variable.LabelWidth));

                    var slideValue = GUILayout.HorizontalSlider(Variable.CustomLineDirections[i], 0f, 360f, Variable.SliderStyle, new(GUI.skin.horizontalSliderThumb));
                    if (slideValue != Variable.CustomLineDirections[i])
                    {
                        Variable.CustomLineDirections[i] = slideValue;
                        Variable.Inputs[i] = ($"{slideValue}", false);
                    }

                    var style = Variable.Inputs[i].Item2 ? Variable.TextFieldErrorStyle : Variable.TextFieldStyle;
                    var inputValue = GUILayout.TextField(Variable.Inputs[i].Item1, style);
                    if (inputValue != Variable.Inputs[i].Item1)
                    {
                        if (float.TryParse(inputValue, out var parsedValue))
                        {
                            Variable.CustomLineDirections[i] = Mathf.Clamp(parsedValue, 0f, 360f);
                            if (parsedValue < 0f || parsedValue > 360f)
                                Variable.Inputs[i] = ($"{Variable.CustomLineDirections[i]}", true);
                            else
                                Variable.Inputs[i] = (inputValue, false);
                        }
                        else
                            Variable.Inputs[i] = (inputValue, true);
                    }

                    if (GUILayout.Button(LocalizationManager.GetText("删除"), Variable.DeleteButtonStyle))
                    {
                        Variable.CustomLineDirections.RemoveAt(i);
                        Variable.CustomLineHovers.RemoveAt(i);
                        Variable.Inputs.RemoveAt(i);
                        if (Variable.TargetLineIndex == i) Variable.TargetLineIndex = -1;
                        else if (Variable.TargetLineIndex > i) Variable.TargetLineIndex--;
                        i--;

                        var cnt = (int)Mathf.Ceil(Mathf.Log10(Variable.CustomLineDirections.Count + 1));
                        Variable.LabelWidth = Localization.GetLabelWidth([new('0', cnt)], false);
                    }
                    GUILayout.EndHorizontal();

                    if (Event.current.type == EventType.Repaint)
                        Variable.CustomLineHovers[i] = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);
                }
            }
        }

        /// <summary>
        /// 留待扩展的接口
        /// </summary>
        public static void DrawExpansion() { }
        #endregion
    }
}
