using AlchAssV3;
using BepInEx.Configuration;
using PotionCraft.LocalizationSystem;
using UnityEngine;

namespace AlchAssExV3
{
    public static class UIWindowEx
    {
        #region 界面渲染
        /// <summary>
        /// 绘制手动控制开关
        /// </summary>
        public static void DrawManualEnables()
        {
            GUILayout.Space(10);
            var icon = $"{(VariableEx.ManualEnableExpand ? "▼" : "▲")} {LocalizationManager.GetText("button_ex_base_control_options")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                VariableEx.ManualEnableExpand = !VariableEx.ManualEnableExpand;

            if (VariableEx.ManualEnableExpand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                VariableEx.EnableSlowStirring = UIWindow.DrawHelpToggle(VariableEx.EnableSlowStirring, "button_ex_slow_stirring", new KeyboardShortcut(), "help_ex_slow_stirring");
                VariableEx.EnableSlowLadling = UIWindow.DrawHelpToggle(VariableEx.EnableSlowLadling, "button_ex_slow_ladling", new KeyboardShortcut(), "help_ex_slow_ladling");
                VariableEx.EnableSlowGrinding = UIWindow.DrawHelpToggle(VariableEx.EnableSlowGrinding, "button_ex_slow_grinding", new KeyboardShortcut(), "help_ex_slow_grinding");
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                VariableEx.EnableSlowHeating = UIWindow.DrawHelpToggle(VariableEx.EnableSlowHeating, "button_ex_slow_heating", new KeyboardShortcut(), "help_ex_slow_heating");
                VariableEx.EnableBulkBrewing = UIWindow.DrawHelpToggle(VariableEx.EnableBulkBrewing, "button_ex_bulk_brewing", new KeyboardShortcut(), "help_ex_bulk_brewing");
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 绘制手动控制开关
        /// </summary>
        public static void DrawAutoEnables()
        {
            GUILayout.Space(10);
            var icon = $"{(VariableEx.AutoEnableExpand ? "▼" : "▲")} {LocalizationManager.GetText("button_ex_auto_control_options")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                VariableEx.AutoEnableExpand = !VariableEx.AutoEnableExpand;

            if (VariableEx.AutoEnableExpand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                VariableEx.EnableQuantitativeStirring = UIWindow.DrawHelpToggle(VariableEx.EnableQuantitativeStirring, "button_ex_quantitative_stirring", new KeyboardShortcut(), "help_ex_quantitative_stirring");
                VariableEx.EnableQuantitativeLadling = UIWindow.DrawHelpToggle(VariableEx.EnableQuantitativeLadling, "button_ex_quantitative_ladling", new KeyboardShortcut(), "help_ex_quantitative_ladling");
                VariableEx.EnableQuantitativeRestoring = UIWindow.DrawHelpToggle(VariableEx.EnableQuantitativeRestoring, "button_ex_quantitative_restoring", new KeyboardShortcut(), "help_ex_quantitative_restoring");
                VariableEx.EnableQuantitativeGrinding = UIWindow.DrawHelpToggle(VariableEx.EnableQuantitativeGrinding, "button_ex_quantitative_grinding", new KeyboardShortcut(), "help_ex_quantitative_grinding");
                VariableEx.EnableQuantitativeHeating = UIWindow.DrawHelpToggle(VariableEx.EnableQuantitativeHeating, "button_ex_quantitative_heating", new KeyboardShortcut(), "help_ex_quantitative_heating");
                VariableEx.EnableEffectIntersection = UIWindow.DrawHelpToggle(VariableEx.EnableEffectIntersection, "button_ex_effect_intersection", new KeyboardShortcut(), "help_ex_effect_intersection");
                VariableEx.EnableEffectAlignment = UIWindow.DrawHelpToggle(VariableEx.EnableEffectAlignment, "button_ex_effect_alignment", new KeyboardShortcut(), "help_ex_effect_alignment");
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                VariableEx.EnableVortexIntersection = UIWindow.DrawHelpToggle(VariableEx.EnableVortexIntersection, "button_ex_vortex_intersection", new KeyboardShortcut(), "help_ex_vortex_intersection");
                VariableEx.EnableVortexAlignment = UIWindow.DrawHelpToggle(VariableEx.EnableVortexAlignment, "button_ex_vortex_alignment", new KeyboardShortcut(), "help_ex_vortex_alignment");
                VariableEx.EnableDangerIntersection = UIWindow.DrawHelpToggle(VariableEx.EnableDangerIntersection, "button_ex_danger_intersection", new KeyboardShortcut(), "help_ex_danger_intersection");
                VariableEx.EnableDangerAlignment = UIWindow.DrawHelpToggle(VariableEx.EnableDangerAlignment, "button_ex_danger_alignment", new KeyboardShortcut(), "help_ex_danger_alignment");
                VariableEx.EnableSwampIntersection = UIWindow.DrawHelpToggle(VariableEx.EnableSwampIntersection, "button_ex_swamp_intersection", new KeyboardShortcut(), "help_ex_swamp_intersection");
                VariableEx.EnableSwampAlignment = UIWindow.DrawHelpToggle(VariableEx.EnableSwampAlignment, "button_ex_swamp_alignment", new KeyboardShortcut(), "help_ex_swamp_alignment");
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 绘制手动控制选项
        /// </summary>
        public static void DrawManualSettings()
        {
            for (var i = 0; i < 3; i++)
            {
                GUILayout.Space(10);
                var icon = $"{(VariableEx.ManualSettingExpand[i] ? "▼" : "▲")} {LocalizationManager.GetText($"button_ex_manual_control_config_{i + 1}")}";
                if (GUILayout.Button(icon, Variable.CategoryStyle))
                    VariableEx.ManualSettingExpand[i] = !VariableEx.ManualSettingExpand[i];

                if (VariableEx.ManualSettingExpand[i])
                {
                    VariableEx.ConfigStirSpeed[i].Value = DrawFloatSlider("label_ex_stirring_speed", VariableEx.ConfigStirSpeed[i].Value, VariableEx.LabelWidthManuals, 2f, -2f, 0f, 100f, true, ref VariableEx.InputStirSpeed[i]);
                    VariableEx.ConfigLadleSpeed[i].Value = DrawFloatSlider("label_ex_ladling_speed", VariableEx.ConfigLadleSpeed[i].Value, VariableEx.LabelWidthManuals, 2f, -2f, 0f, 100f, true, ref VariableEx.InputLadleSpeed[i]);
                    VariableEx.ConfigGrindSpeed[i].Value = DrawFloatSlider("label_ex_grinding_speed", VariableEx.ConfigGrindSpeed[i].Value, VariableEx.LabelWidthManuals, 2f, -2f, 0f, 100f, true, ref VariableEx.InputGrindSpeed[i]);
                    VariableEx.ConfigHeatSpeed[i].Value = DrawFloatSlider("label_ex_heating_speed", VariableEx.ConfigHeatSpeed[i].Value, VariableEx.LabelWidthManuals, 2f, -2f, 0f, 100f, true, ref VariableEx.InputHeatSpeed[i]);
                    VariableEx.ConfigBrewBulk[i].Value = DrawIntSlider("label_ex_brewing_multiplier", VariableEx.ConfigBrewBulk[i].Value, VariableEx.LabelWidthManuals, 0f, 3f, 1, int.MaxValue, ref VariableEx.InputBrewBulk[i]);
                }
            }
        }

        /// <summary>
        /// 绘制自动控制选项
        /// </summary>
        public static void DrawAutoSettings()
        {
            GUILayout.Space(10);
            var icon = $"{(VariableEx.AutoSettingExpand ? "▼" : "▲")} {LocalizationManager.GetText("button_ex_auto_control_configurations")}";
            if (GUILayout.Button(icon, Variable.CategoryStyle))
                VariableEx.AutoSettingExpand = !VariableEx.AutoSettingExpand;

            if (VariableEx.AutoSettingExpand)
            {
                VariableEx.StirringLength = DrawFloatSlider("label_ex_stirring_length", VariableEx.StirringLength, VariableEx.LabelWidthAutos, 0f, 30f, 0f, float.MaxValue, false, ref VariableEx.InputStirringLength);
                VariableEx.LadlingLength = DrawFloatSlider("label_ex_ladling_length", VariableEx.LadlingLength, VariableEx.LabelWidthAutos, 0f, 30f, 0f, float.MaxValue, false, ref VariableEx.InputLadlingLength);
                VariableEx.RestoringAngle = DrawFloatSlider("label_ex_restoring_angle", VariableEx.RestoringAngle, VariableEx.LabelWidthAutos, 0f, 180f, 0f, 180f, false, ref VariableEx.InputRestoringAngle);
                VariableEx.GrindingTarget = DrawFloatSlider("label_ex_grinding_target", VariableEx.GrindingTarget, VariableEx.LabelWidthManuals, 0f, 100f, 0f, 100f, false, ref VariableEx.InputGrindingTarget);
                VariableEx.HeatingTarget = DrawFloatSlider("label_ex_heating_target", VariableEx.HeatingTarget, VariableEx.LabelWidthManuals, 0f, 100f, 0f, 100f, false, ref VariableEx.InputHeatingTarget);
                VariableEx.HealthThreshold = DrawFloatSlider("label_ex_health_threshold", VariableEx.HealthThreshold, VariableEx.LabelWidthAutos, 0f, 100f, float.MinValue, 100f, false, ref VariableEx.InputHealthThreshold);
                VariableEx.EffectDeviation = DrawPiecewiseSlider("label_ex_effect_deviation", VariableEx.EffectDeviation, VariableEx.LabelWidthAutos, VariableEx.EffectLevels, VariableEx.DeviationRotation, 0f, float.MaxValue, ref VariableEx.InputEffectDeviation);
            }
        }
        #endregion

        #region 滑条渲染
        private static void GetSliderRowWidths(float labelWidth, out float resolvedLabelWidth, out float inputWidth)
        {
            var contentWidth = Mathf.Max(160f, Variable.WindowRect.width - 72f);
            resolvedLabelWidth = Mathf.Clamp(labelWidth, 72f, Mathf.Max(72f, contentWidth * 0.42f));
            inputWidth = Mathf.Clamp(contentWidth * 0.2f, 48f, 88f);
        }

        /// <summary>
        /// 绘制整数滑条
        /// </summary>
        public static int DrawIntSlider(string label, int value, float width, float min, float max, int cmin, int cmax, ref (string, bool) input)
        {
            GetSliderRowWidths(width, out var labelWidth, out var inputWidth);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{LocalizationManager.GetText(label)}:", Variable.LabelStyle, GUILayout.Width(labelWidth));

            var logVal = Mathf.Log10(value);
            var newVal = GUILayout.HorizontalSlider(logVal, min, max, Variable.SliderStyle, new(GUI.skin.horizontalSliderThumb), GUILayout.MinWidth(40f), GUILayout.ExpandWidth(true));
            if (newVal != logVal)
            {
                value = (int)Mathf.Pow(10, newVal);
                input = ($"{value}", false);
            }

            var style = input.Item2 ? Variable.TextFieldErrorStyle : Variable.TextFieldStyle;
            var iptVal = GUILayout.TextField(input.Item1, style, GUILayout.Width(inputWidth));
            if (iptVal != input.Item1)
            {
                if (int.TryParse(iptVal, out var parVal))
                {
                    value = Mathf.Clamp(parVal, cmin, cmax);
                    if (parVal < cmin || parVal > cmax)
                        input = ($"{value}", false);
                    else
                        input = (iptVal, false);
                }
                else
                    input = (iptVal, true);
            }
            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        /// 绘制浮点滑条
        /// </summary>
        public static float DrawFloatSlider(string label, float value, float width, float min, float max, float cmin, float cmax, bool pow, ref (string, bool) input)
        {
            GetSliderRowWidths(width, out var labelWidth, out var inputWidth);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{LocalizationManager.GetText(label)}:", Variable.LabelStyle, GUILayout.Width(labelWidth));

            var logVal = pow ? Mathf.Log10(value) : value;
            var newVal = GUILayout.HorizontalSlider(logVal, min, max, Variable.SliderStyle, new(GUI.skin.horizontalSliderThumb), GUILayout.MinWidth(40f), GUILayout.ExpandWidth(true));
            if (newVal != logVal)
            {
                value = pow ? Mathf.Pow(10, newVal) : newVal;
                input = ($"{value}", false);
            }

            var style = input.Item2 ? Variable.TextFieldErrorStyle : Variable.TextFieldStyle;
            var iptVal = GUILayout.TextField(input.Item1, style, GUILayout.Width(inputWidth));
            if (iptVal != input.Item1)
            {
                if (float.TryParse(iptVal, out var parVal))
                {
                    value = Mathf.Clamp(parVal, cmin, cmax);
                    if (parVal < cmin || parVal > cmax)
                        input = ($"{value}", false);
                    else
                        input = (iptVal, false);
                }
                else
                    input = (iptVal, true);
            }
            GUILayout.EndHorizontal();
            return value;
        }

        /// <summary>
        /// 绘制分段滑条
        /// </summary>
        public static float DrawPiecewiseSlider(string label, float value, float width, float[] pars, float dev, float cmin, float cmax, ref (string, bool) input)
        {
            GetSliderRowWidths(width, out var labelWidth, out var inputWidth);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{LocalizationManager.GetText(label)}:", Variable.LabelStyle, GUILayout.Width(labelWidth));

            var nomVal = FunctionEx.ValueToNormalized(value, pars);
            var newVal = GUILayout.HorizontalSlider(nomVal, 0f, 1f, Variable.SliderStyle, new(GUI.skin.horizontalSliderThumb), GUILayout.MinWidth(40f), GUILayout.ExpandWidth(true));
            if (newVal != nomVal)
            {
                value = FunctionEx.NormalizedToValue(newVal, pars);
                input = ($"{value}", false);
            }

            var style = input.Item2 ? Variable.TextFieldErrorStyle : Variable.TextFieldStyle;
            var iptVal = GUILayout.TextField(input.Item1, style, GUILayout.Width(inputWidth));
            if (iptVal != input.Item1)
            {
                if (float.TryParse(iptVal, out var parVal))
                {
                    value = Mathf.Clamp(parVal, cmin, cmax);
                    if (parVal < cmin || parVal > cmax)
                        input = ($"{value}", false);
                    else
                        input = (iptVal, false);
                }
                else
                    input = (iptVal, true);
            }
            GUILayout.EndHorizontal();

            for (var i = 1; i < pars.Length - 1; i++)
                if (GUILayout.Button($"{LocalizationManager.GetText(label)} L{pars.Length - i}", Variable.ButtonStyle))
                {
                    value = Mathf.Max(0f, pars[i] - dev);
                    input = ($"{value}", false);
                }
            return value;
        }
        #endregion
    }
}
