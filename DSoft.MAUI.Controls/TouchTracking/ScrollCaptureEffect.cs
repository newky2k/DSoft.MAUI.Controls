using Microsoft.Maui.Controls;

namespace DSoft.Maui.Controls.TouchTracking
{
	/// <summary>
	/// Attach to a view that handles its own drag gestures to stop a parent scrolling
	/// container from stealing the touch stream once the drag passes the scroll slop.
	/// </summary>
	/// <remarks>
	/// Only Android needs this. There, a parent <c>ScrollView</c> intercepts the touch
	/// stream as soon as a vertical drag is detected, which cancels any gesture the
	/// child was tracking. The Android implementation calls
	/// <c>RequestDisallowInterceptTouchEvent</c> for the duration of the gesture.
	/// The implementations on the other platforms are deliberately empty — iOS and
	/// Windows already give the child gesture precedence.
	/// <para>
	/// Requires <c>UseDSoftControls()</c> to have been called on the
	/// <see cref="MauiAppBuilder"/>; without it the effect resolves to a no-op.
	/// </para>
	/// </remarks>
	public class ScrollCaptureEffect : RoutingEffect
	{
		public ScrollCaptureEffect() : base() { }
	}
}
