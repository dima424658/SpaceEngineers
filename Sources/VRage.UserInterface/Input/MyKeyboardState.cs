using EmptyKeys.UserInterface.Input;
using VRage.Input;

namespace VRage.UserInterface.Input
{
    public class MyKeyboardState : KeyboardStateBase
    {
        public override bool IsKeyPressed(KeyCode keyCode) => MyInput.Static.IsKeyPress((MyKeys)keyCode);

        public override void Update() { }
    }
}
