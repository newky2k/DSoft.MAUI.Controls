using Microsoft.Maui.Controls.Platform;

namespace DSoft.Maui.Controls.TouchTracking
{
	// Intentionally empty: this platform already lets a child gesture recognizer win
	// over an enclosing scrolling container, so nothing has to be done here. The type
	// exists so ScrollCaptureEffect can be registered from shared code.
	internal class ScrollCapturePlatformEffect : PlatformEffect
	{
		protected override void OnAttached()
		{
		}

		protected override void OnDetached()
		{
		}
	}
}
