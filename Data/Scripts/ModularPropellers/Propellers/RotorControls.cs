using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Text;
using System;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace ModularPropellers.Propellers
{
    internal static class RotorControls
    {
        private static bool _hasInited = false;
        private const string IdPrefix = "MP_";

        public static void DoOnce()
        {
            if (_hasInited)
                return;

            CreateControls();
            CreateActions();
            CreateProperties();

            _hasInited = true;
        }

        private static bool HasRotorLogic(IMyTerminalBlock block) => block?.GameLogic?.GetAs<RotorLogic>() != null;

        private static void CreateControls()
        {
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyThrust>(IdPrefix + "OptionsDivider");
                c.Label = MyStringId.GetOrCompute("=== Modular Propellers ===");
                c.SupportsMultipleBlocks = true;
                c.Visible = HasRotorLogic;
                MyAPIGateway.TerminalControls.AddControl<IMyThrust>(c);
            }
            {
                CreateSlider(
                    "BladePitch",
                    "Blade Pitch",
                    "Angle of the blades attached to this rotor.",
                    0f,
                    45f,
                    (b) => MathHelper.ToDegrees(b.GameLogic.GetAs<RotorLogic>().BladeAngle),
                    (b, v) => b.GameLogic.GetAs<RotorLogic>().BladeAngle.Value = MathHelper.ToRadians(v),
                    (b, sb) => sb.Append($"{MathHelper.ToDegrees(b.GameLogic.GetAs<RotorLogic>().BladeAngle):N1} degrees")
                ).SetLimits(
                    b => -MathHelper.ToDegrees(RotorLogic.RotorInfos[b.BlockDefinition.SubtypeName].MaxAngle),
                    b => MathHelper.ToDegrees(RotorLogic.RotorInfos[b.BlockDefinition.SubtypeName].MaxAngle)
                    );
            }
            {
                CreateSlider(
                    "MaxRpm",
                    "Maximum RPM",
                    "Highest speed at which this rotor can spin.",
                    0,
                    float.MaxValue,
                    (b) => b.GameLogic.GetAs<RotorLogic>().MaxRpm,
                    (b, v) => b.GameLogic.GetAs<RotorLogic>().MaxRpm = v,
                    (b, sb) => sb.Append($"{b.GameLogic.GetAs<RotorLogic>().MaxRpm:N0} RPM")
                ).SetLimits(
                    b => 0,
                    b => b.GameLogic.GetAs<RotorLogic>().AbsMaxRpm
                    );
            }
        }

        private static void CreateActions()
        {
            {
                CreateAction(
                    "BladePitch_Inc",
                    "Increase Blade Pitch",
                    b =>
                    {
                        var logic = b.GameLogic.GetAs<RotorLogic>();
                        if (logic.BladeAngle.Value + MathHelper.ToRadians(0.5f) <= logic.Info.MaxAngle)
                            logic.BladeAngle.Value += MathHelper.ToRadians(0.5f);
                        else
                            logic.BladeAngle.Value = logic.Info.MaxAngle;
                    },
                    (b, sb) => sb.Append(
                        $"{MathHelper.ToDegrees(b.GameLogic.GetAs<RotorLogic>().BladeAngle):N1}\u00b0"),
                    @"Textures\GUI\Icons\Actions\Increase.dds"
                );
            }
            {
                CreateAction(
                    "BladePitch_Dec",
                    "Decrease Blade Pitch",
                    b => 
                    {
                        var logic = b.GameLogic.GetAs<RotorLogic>();
                        if (logic.BladeAngle.Value - MathHelper.ToRadians(0.5f) >= -logic.Info.MaxAngle)
                            logic.BladeAngle.Value -= MathHelper.ToRadians(0.5f);
                        else
                            logic.BladeAngle.Value = -logic.Info.MaxAngle;
                    },
                    (b, sb) => sb.Append(
                        $"{MathHelper.ToDegrees(b.GameLogic.GetAs<RotorLogic>().BladeAngle):N1}\u00b0"),
                    @"Textures\GUI\Icons\Actions\Decrease.dds"
                );
            }
        }

        private static void CreateProperties()
        {

        }

        public static IMyTerminalControlOnOffSwitch CreateToggle(string id, string displayName, string toolTip, Func<IMyTerminalBlock, bool> getter, Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible = null)
        {
            var ShootToggle = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyThrust>(IdPrefix + id);
            ShootToggle.Title = MyStringId.GetOrCompute(displayName);
            ShootToggle.Tooltip = MyStringId.GetOrCompute(toolTip);
            ShootToggle.SupportsMultipleBlocks = true; // wether this control should be visible when multiple blocks are selected (as long as they all have this control).
                                                       // callbacks to determine if the control should be visible or not-grayed-out(Enabled) depending on whatever custom condition you want, given a block instance.
                                                       // optional, they both default to true.

            ShootToggle.Visible = HasRotorLogic;
            //c.Enabled = CustomVisibleCondition;
            ShootToggle.OnText = MySpaceTexts.SwitchText_On;
            ShootToggle.OffText = MySpaceTexts.SwitchText_Off;
            //c.OffText = MyStringId.GetOrCompute("Off");
            // setters and getters should both be assigned on all controls that have them, to avoid errors in mods or PB scripts getting exceptions from them.
            ShootToggle.Getter = getter;  // Getting the value
            ShootToggle.Setter = setter; // Setting the value

            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(ShootToggle);

            return ShootToggle;
        }

        private static IMyTerminalControlSlider CreateSlider(string id, string displayName, string toolTip, float min, float max, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter, Action<IMyTerminalBlock, StringBuilder> writer, Func<IMyTerminalBlock, bool> visible = null)
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyThrust>(IdPrefix + id);
            slider.Title = MyStringId.GetOrCompute(displayName);
            slider.Tooltip = MyStringId.GetOrCompute(toolTip);
            slider.SetLimits(min, max); // Set the minimum and maximum values for the slider
            slider.Getter = getter; // Replace with your property
            slider.Setter = setter; // Replace with your property
            slider.Writer = writer; // Replace with your property

            slider.Visible = HasRotorLogic;
            slider.Enabled = (b) => true; // or your custom condition
            slider.SupportsMultipleBlocks = true;

            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(slider);
            return slider;
        }

        private static IMyTerminalAction CreateAction(string id, string displayName, Action<IMyTerminalBlock> action, Action<IMyTerminalBlock, StringBuilder> writer, string icon)
        {
            var cycleControlForwardAction = MyAPIGateway.TerminalControls.CreateAction<IMyThrust>(IdPrefix + id);
            cycleControlForwardAction.Name = new StringBuilder(displayName);
            cycleControlForwardAction.Action = action;
            cycleControlForwardAction.Writer = writer;
            cycleControlForwardAction.Icon = icon;

            cycleControlForwardAction.Enabled = HasRotorLogic;
            MyAPIGateway.TerminalControls.AddAction<IMyThrust>(cycleControlForwardAction);

            return cycleControlForwardAction;
        }

        private static IMyTerminalControlButton CreateButton(string id, string displayName, string toolTip, Action<IMyTerminalBlock> action, Func<IMyTerminalBlock, bool> visible = null)
        {
            var button = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyThrust>(IdPrefix + id);
            button.Title = MyStringId.GetOrCompute(displayName);
            button.Tooltip = MyStringId.GetOrCompute(toolTip);
            button.SupportsMultipleBlocks = true;

            button.Visible = HasRotorLogic;
            button.Action = action;

            MyAPIGateway.TerminalControls.AddControl<IMyThrust>(button);

            return button;
        }
    }
}
