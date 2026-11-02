using UnityEngine.XR;

public static class Haptics {
   public static void Pulse(XRNode hand, float amplitude, float seconds) {
        InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
        if (device.isValid && device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse) {
            device.SendHapticImpulse(0, amplitude, seconds); }
   }
}
