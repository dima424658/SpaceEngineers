using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Input;
using VRage.Input;

namespace VRage.UserInterface.Input
{
    public class MyGamePadState : GamePadStateBase
    {
        public override PointF DPad
        {
            get
            {
                if (MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.JDUp))
                    return new PointF(0.0f, 1f);
                else if (MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.JDDown))
                    return new PointF(0.0f, -1f);
                else if (MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.JDLeft))
                    return new PointF(-1f, 0.0f);
                else
                    return MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.JDRight) ? new PointF(1f, 0.0f) : new PointF();
            }
        }

        public override bool IsAButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J01);

        public override bool IsBButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J02);

        public override bool IsCButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J03);

        public override bool IsDButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J04);

        public override bool IsLeftShoulderButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J05);

        public override bool IsLeftStickButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J09);

        public override bool IsRightShoulderButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J06);

        public override bool IsRightStickButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J10);

        public override bool IsSelectButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J08);

        public override bool IsStartButtonPressed => MyInput.Static.IsJoystickButtonPressed(MyJoystickButtonsEnum.J07);

        public override PointF LeftThumbStick
        {
            get => new PointF(
                    MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Xpos) - MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Xneg),
                    MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Yneg) - MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Ypos)
                    );
        }

        public override float LeftTrigger => MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Zpos);

        public override int PlayerNumber => 0;

        public override PointF RightThumbStick
        {
            get => new PointF(
                MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.RotationXpos) - MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.RotationXneg),
                MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.RotationYneg) - MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.RotationYpos)
                );
        }

        public override float RightTrigger => MyInput.Static.GetJoystickAxisStateForGameplay(MyJoystickAxesEnum.Zneg);

        public override void Update(int gamePadIndex) { }
    }
}
