using System;

namespace Managers
{
    public static class EventsManager
    {
        #region KeyEvents
        
        public static Action MoveForward;
        public static Action MoveLeft;
        public static Action MoveBackwards;
        public static Action MoveRight;
        public static Action PressJump;
        
        public static Action StopMovingForward;
        public static Action StopMovingLeft;
        public static Action StopMovingBackwards;
        public static Action StopMovingRight;
        public static Action ReleaseJump;
        
        #endregion

        #region MouseButtonsEvents
        
        public static Action PressMouseButton0;
        public static Action PressMouseButton1;
        public static Action PressMouseButton2;
        
        public static Action ReleaseMouseButton0;
        public static Action ReleaseMouseButton1;
        public static Action ReleaseMouseButton2;        

        #endregion

        #region ScrollWheelEvents
    
        public static Action ScrollUp;
        public static Action ScrollDown;        

        #endregion

    }
}
