using System;
using BepInEx.Configuration;
using PotionCraft.LocalizationSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AlchAssV3
{
    public static class UIWindow
    {
        private static string ActiveHelpTooltip = string.Empty;

        #region 主体渲染
        /// <summary>
        /// 生成样式
        /// </summary>
        public static void InitStyles()
        {
            Variable.WindowTexture = new(1, 1);
            Variable.WindowTexture.SetPixel(0, 0, new(0.12f, 0.12f, 0.12f, 0.95f));
            Variable.WindowTexture.Apply();
            Variable.HelpMarkerTexture = CreateCircleTexture(18, new(0.15f, 0.45f, 0.60f, 1f), new(0.65f, 0.95f, 1f, 1f));
            Variable.TooltipTexture = CreateSolidTexture(new(0.06f, 0.08f, 0.09f, 0.98f));
            Variable.TooltipBorderTexture = CreateSolidTexture(new(0.50f, 0.90f, 1.0f, 1f));

            Variable.WindowStyle = new(GUI.skin.window);
            Variable.WindowStyle.normal.background = Variable.WindowTexture;
            Variable.WindowStyle.onNormal.background = Variable.WindowTexture;
            Variable.WindowStyle.normal.textColor = Color.cyan;
            Variable.WindowStyle.onNormal.textColor = Color.cyan;
            Variable.WindowStyle.font = Variable.Font;
            Variable.WindowStyle.fontSize = Variable.UIFontSize + 4;

            Variable.CategoryStyle = new(GUI.skin.button);
            Variable.CategoryStyle.normal.textColor = new(1.0f, 0.8f, 0.3f);
            Variable.CategoryStyle.hover.textColor = new(1.0f, 0.8f, 0.3f);
            Variable.CategoryStyle.font = Variable.Font;
            Variable.CategoryStyle.fontSize = Variable.UIFontSize + 2;
            Variable.CategoryStyle.alignment = TextAnchor.MiddleLeft;
            Variable.CategoryStyle.padding = new(10, 10, 5, 5);

            Variable.ButtonStyle = new(GUI.skin.button);
            Variable.ButtonStyle.hover.textColor = Color.green;
            Variable.ButtonStyle.font = Variable.Font;
            Variable.ButtonStyle.fontSize = Variable.UIFontSize;
            Variable.ButtonStyle.margin = new(30, 30, 5, 5);
            Variable.ButtonStyle.padding = new(10, 10, 5, 5);

            Variable.HelpMarkerStyle = new(GUI.skin.label);
            Variable.HelpMarkerStyle.normal.background = Variable.HelpMarkerTexture;
            Variable.HelpMarkerStyle.normal.textColor = Color.white;
            Variable.HelpMarkerStyle.hover.textColor = new(1.0f, 0.95f, 0.4f);
            Variable.HelpMarkerStyle.font = Variable.Font;
            Variable.HelpMarkerStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 3);
            Variable.HelpMarkerStyle.fontStyle = FontStyle.Bold;
            Variable.HelpMarkerStyle.alignment = TextAnchor.MiddleCenter;
            Variable.HelpMarkerStyle.margin = new(30, 6, 7, 0);
            Variable.HelpMarkerStyle.padding = new(0, 0, 0, 2);
            Variable.HelpMarkerStyle.fixedWidth = 18;
            Variable.HelpMarkerStyle.fixedHeight = 18;

            Variable.TooltipStyle = new(GUI.skin.box);
            Variable.TooltipStyle.normal.background = Variable.TooltipTexture;
            Variable.TooltipStyle.normal.textColor = new(0.85f, 1.0f, 1.0f);
            Variable.TooltipStyle.font = Variable.Font;
            Variable.TooltipStyle.fontSize = Variable.HelpTooltipFontSize;
            Variable.TooltipStyle.alignment = TextAnchor.UpperLeft;
            Variable.TooltipStyle.wordWrap = true;
            Variable.TooltipStyle.padding = new(10, 10, 7, 7);

            Variable.TooltipBorderStyle = new(GUI.skin.box);
            Variable.TooltipBorderStyle.normal.background = Variable.TooltipBorderTexture;
            Variable.TooltipBorderStyle.margin = new(0, 0, 0, 0);
            Variable.TooltipBorderStyle.padding = new(0, 0, 0, 0);

            Variable.DeleteButtonStyle = new(GUI.skin.button);
            Variable.DeleteButtonStyle.normal.textColor = Color.red;
            Variable.DeleteButtonStyle.hover.textColor = Color.red;
            Variable.DeleteButtonStyle.font = Variable.Font;
            Variable.DeleteButtonStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 2);
            Variable.DeleteButtonStyle.margin = new(0, 30, 5, 0);
            Variable.DeleteButtonStyle.padding = new(10, 10, 6, 4);
            Variable.DeleteButtonStyle.fixedWidth = 50;

            Variable.ToggleStyle = new(GUI.skin.toggle);
            Variable.ToggleStyle.onNormal.textColor = new(0.4f, 0.8f, 1.0f);
            Variable.ToggleStyle.onHover.textColor = new(0.4f, 0.8f, 1.0f);
            Variable.ToggleStyle.font = Variable.Font;
            Variable.ToggleStyle.fontSize = Variable.UIFontSize;
            Variable.ToggleStyle.margin = new(0, 30, 6, 4);
            Variable.ToggleStyle.padding = new(25, 0, -1, 0);

            Variable.LabelStyle = new(GUI.skin.label)
            {
                font = Variable.Font,
                fontSize = Variable.UIFontSize,
                margin = new(30, 10, 5, 5)
            };

            Variable.SliderStyle = new(GUI.skin.horizontalSlider)
            {
                margin = new(0, 5, 14, 0)
            };

            Variable.TextFieldStyle = new(GUI.skin.textField);
            Variable.TextFieldStyle.normal.textColor = Color.green;
            Variable.TextFieldStyle.font = Variable.Font;
            Variable.TextFieldStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 2);
            Variable.TextFieldStyle.margin = new(0, 10, 7, 0);
            Variable.TextFieldStyle.fixedWidth = 70;

            Variable.TextFieldErrorStyle = new(GUI.skin.textField);
            Variable.TextFieldErrorStyle.normal.textColor = Color.red;
            Variable.TextFieldErrorStyle.font = Variable.Font;
            Variable.TextFieldErrorStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 2);
            Variable.TextFieldErrorStyle.margin = new(0, 10, 7, 0);
            Variable.TextFieldErrorStyle.fixedWidth = 70;
        }

        private static void ApplyFontSizes()
        {
            Variable.WindowStyle.fontSize = Variable.UIFontSize + 4;
            Variable.CategoryStyle.fontSize = Variable.UIFontSize + 2;
            Variable.ButtonStyle.fontSize = Variable.UIFontSize;
            Variable.HelpMarkerStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 3);
            Variable.ToggleStyle.fontSize = Variable.UIFontSize;
            Variable.LabelStyle.fontSize = Variable.UIFontSize;
            Variable.DeleteButtonStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 2);
            Variable.TextFieldStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 2);
            Variable.TextFieldErrorStyle.fontSize = Mathf.Max(10, Variable.UIFontSize - 2);
            Variable.TooltipStyle.fontSize = Variable.HelpTooltipFontSize;
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
            ActiveHelpTooltip = string.Empty;
            ApplyFontSizes();
            GUILayout.Space(8);
            Variable.ScrollPosition = GUILayout.BeginScrollView(Variable.ScrollPosition);

            DrawEnables();
            DrawDisplays();
            DrawActions();
            DrawCustomList();
            DrawExpansion();

            GUILayout.EndScrollView();
            DrawHelpTooltip();
            GUILayout.Space(5);
            GUI.DragWindow(new Rect(0, 0, Variable.WindowRect.width, 30));
        }
        #endregion

        #region 元素渲染
        private static Texture2D CreateSolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateCircleTexture(int size, Color fillColor, Color borderColor)
        {
            var texture = new Texture2D(size, size);
            var center = (size - 1) / 2f;
            var outerRadius = size / 2f - 0.5f;
            var innerRadius = outerRadius - 1.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (distance <= innerRadius)
                        texture.SetPixel(x, y, fillColor);
                    else if (distance <= outerRadius)
                        texture.SetPixel(x, y, borderColor);
                    else
                        texture.SetPixel(x, y, Color.clear);
                }
            }
            texture.Apply();
            return texture;
        }

        private static bool DrawHelpToggle(bool value, string textKey, KeyboardShortcut shortcut, string helpKey)
        {
            var text = GetLayoutTextString(textKey, shortcut);

            GUILayout.BeginHorizontal();
            GUILayout.Label("?", Variable.HelpMarkerStyle);
            var markerRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint && markerRect.Contains(Event.current.mousePosition))
                ActiveHelpTooltip = LocalizationManager.GetText(helpKey);
            value = GUILayout.Toggle(value, text, Variable.ToggleStyle, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return value;
        }


        private static void DrawHelpTooltip()
        {
            if (string.IsNullOrEmpty(ActiveHelpTooltip))
                return;

            Variable.TooltipStyle.fontSize = Variable.HelpTooltipFontSize;
            var maxWidth = Mathf.Clamp(Variable.WindowRect.width - 60f, 180f, 520f);
            var content = new GUIContent(ActiveHelpTooltip);
            var height = Variable.TooltipStyle.CalcHeight(content, maxWidth);
            var mousePos = Event.current.mousePosition;
            var x = Mathf.Min(mousePos.x + 14f, Variable.WindowRect.width - maxWidth - 10f);
            var y = Mathf.Min(mousePos.y + 18f, Variable.WindowRect.height - height - 10f);
            var contentRect = new Rect(Mathf.Max(10f, x), Mathf.Max(35f, y), maxWidth, height);
            GUI.Box(new Rect(contentRect.x - 2f, contentRect.y - 2f, contentRect.width + 4f, contentRect.height + 4f), GUIContent.none, Variable.TooltipBorderStyle);
            GUI.Box(contentRect, content, Variable.TooltipStyle);
        }

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
            var icon = $"{(Variable.EnableExpand ? "▼" : "▲")} {LocalizationManager.GetText("button_rendering_options")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.EnableExpand = !Variable.EnableExpand;

            if (Variable.EnableExpand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                Variable.EnablePathLine = DrawHelpToggle(Variable.EnablePathLine, "button_path_line", Variable.KeyEnablePathLine.Value, "help_path_line");
                Variable.EnableLadleLine = DrawHelpToggle(Variable.EnableLadleLine, "button_ladle_line", Variable.KeyEnableLadleLine.Value, "help_ladle_line");
                Variable.EnableEffectLine = DrawHelpToggle(Variable.EnableEffectLine, "button_effect_line", Variable.KeyEnableEffectLine.Value, "help_effect_line");
                Variable.EnableVortexLine = DrawHelpToggle(Variable.EnableVortexLine, "button_vortex_line", Variable.KeyEnableVortexLine.Value, "help_vortex_line");
                Variable.EnableTangentLine = DrawHelpToggle(Variable.EnableTangentLine, "button_vortex_tangent", Variable.KeyEnableTangentLine.Value, "help_vortex_tangent");
                Variable.EnableCustomLine = DrawHelpToggle(Variable.EnableCustomLine, "button_custom_line", Variable.KeyEnableCustomLine.Value, "help_custom_line");
                Variable.EnablePathCurve = DrawHelpToggle(Variable.EnablePathCurve, "button_path_curve", Variable.KeyEnablePathCurve.Value, "help_path_curve");
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                Variable.EnableVortexCurve = DrawHelpToggle(Variable.EnableVortexCurve, "button_vortex_curve", Variable.KeyEnableVortexCurve.Value, "help_vortex_curve");
                Variable.EnableEffectRange = DrawHelpToggle(Variable.EnableEffectRange, "button_effect_range", Variable.KeyEnableEffectRange.Value, "help_effect_range");
                Variable.EnableVortexRange = DrawHelpToggle(Variable.EnableVortexRange, "button_vortex_range", Variable.KeyEnableVortexRange.Value, "help_vortex_range");
                Variable.EnableDangerSimulation = DrawHelpToggle(Variable.EnableDangerSimulation, "button_danger_simulation", Variable.KeyEnableDangerSimulation.Value, "help_danger_simulation");
                Variable.EnableSwampSimulation = DrawHelpToggle(Variable.EnableSwampSimulation, "button_swamp_simulation", Variable.KeyEnableSwampSimulation.Value, "help_swamp_simulation");
                Variable.EnableTransparency = DrawHelpToggle(Variable.EnableTransparency, "button_transparency", Variable.KeyEnableTransparency.Value, "help_transparency");
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
            var icon = $"{(Variable.DisplayExpand ? "▼" : "▲")} {LocalizationManager.GetText("button_display_options")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.DisplayExpand = !Variable.DisplayExpand;

            if (Variable.DisplayExpand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                Variable.DisplaySalt = DrawHelpToggle(Variable.DisplaySalt, "button_salt_data", Variable.KeyToggleDisplaySalt.Value, "help_salt_data");
                Variable.DisplayStage = DrawHelpToggle(Variable.DisplayStage, "button_stir_phase", Variable.KeyToggleDisplayStage.Value, "help_stir_phase");
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                Variable.DisplayPolar = DrawHelpToggle(Variable.DisplayPolar, "button_polar_coordinates", Variable.KeyToggleDisplayPolar.Value, "help_polar_coordinates");
                Variable.OffsetCorrection = DrawHelpToggle(Variable.OffsetCorrection, "button_collider_correction", Variable.KeyToggleDisplayOffset.Value, "help_collider_correction");
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
            var icon = $"{(Variable.ActionExpand ? "▼" : "▲")} {LocalizationManager.GetText("button_window_management")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.ActionExpand = !Variable.ActionExpand;

            if (Variable.ActionExpand)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(LocalizationManager.GetText("button_restore_all_windows"), Variable.ButtonStyle))
                    Function.RestoreDebugWindows();
                if (GUILayout.Button(LocalizationManager.GetText("button_save_window_layout"), Variable.ButtonStyle))
                    Function.SaveDebugWindowPos();
                GUILayout.EndHorizontal();
                DrawFontSizeSlider("label_ui_text_size", ref Variable.UIFontSize);
                DrawFontSizeSlider("label_help_text_size", ref Variable.HelpTooltipFontSize);
            }
            GUILayout.Space(10);
        }

        private static void DrawFontSizeSlider(string labelKey, ref int value)
        {
            var labelWidth = Mathf.Clamp(Variable.WindowRect.width * 0.36f, 120f, 220f);
            var sliderWidth = Mathf.Max(100f, Variable.WindowRect.width - labelWidth - 90f);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{LocalizationManager.GetText(labelKey)}: {value}", Variable.LabelStyle, GUILayout.Width(labelWidth));
            var sliderValue = GUILayout.HorizontalSlider(value, 10f, 30f, Variable.SliderStyle, new(GUI.skin.horizontalSliderThumb), GUILayout.Width(sliderWidth));
            var newValue = Mathf.RoundToInt(sliderValue);
            if (newValue != value)
            {
                value = newValue;
                ApplyFontSizes();
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制自定义方向线管理
        /// </summary>
        public static void DrawCustomList()
        {
            var icon = $"{(Variable.CustomListExpand ? "▼" : "▲")} {LocalizationManager.GetText("button_custom_lines_management")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                Variable.CustomListExpand = !Variable.CustomListExpand;

            if (Variable.CustomListExpand)
            {
                if (GUILayout.Button($"+ {LocalizationManager.GetText("button_add_lines")}", Variable.ButtonStyle))
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

                    if (GUILayout.Button(LocalizationManager.GetText("button_delete"), Variable.DeleteButtonStyle))
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
