using EmptyKeys.UserInterface.Input;

namespace VRage.UserInterface.Input
{
    public class MyTouchState : TouchStateBase
    {
        public override int Id => 0;

        public override TouchAction Action => TouchAction.None;

        public override TouchGestures Gesture => TouchGestures.None;

        public override bool HasGesture => false;

        public override bool IsTouched => false;

        public override float MoveX => 0.0f;

        public override float MoveY => 0.0f;

        public override float NormalizedX => 0.0f;

        public override float NormalizedY => 0.0f;

        public override void Update() { }
    }
}
