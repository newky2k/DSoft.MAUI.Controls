using Android.Views;
using Microsoft.Maui.Controls.Platform;
using AView = Android.Views.View;

namespace DSoft.Maui.Controls.TouchTracking
{
	// All the code in this file is only included on Android.
	internal class ScrollCapturePlatformEffect : PlatformEffect
	{
		private AView _view;

		protected override void OnAttached()
		{
			_view = Control ?? Container;

			if (_view != null)
				_view.Touch += OnTouch;
		}

		protected override void OnDetached()
		{
			if (_view == null)
				return;

			// Make sure a detach mid-gesture doesn't leave the parent locked out.
			ReleaseParent();

			_view.Touch -= OnTouch;
			_view = null;
		}

		private void OnTouch(object sender, AView.TouchEventArgs e)
		{
			// Never set e.Handled here. MAUI's own gesture manager subscribes to the
			// same Touch event to drive PanGestureRecognizer, and the last value
			// written wins — this effect only needs to observe the stream.
			switch (e.Event?.ActionMasked)
			{
				case MotionEventActions.Down:
					// The parent ScrollView still sees the down event; this tells it not
					// to intercept the moves that follow, which is what would otherwise
					// cancel the child's pan gesture.
					_view?.Parent?.RequestDisallowInterceptTouchEvent(true);
					break;

				case MotionEventActions.Up:
				case MotionEventActions.Cancel:
					ReleaseParent();
					break;
			}
		}

		private void ReleaseParent()
		{
			try
			{
				_view?.Parent?.RequestDisallowInterceptTouchEvent(false);
			}
			catch (ObjectDisposedException)
			{
				// The native view went away underneath us; nothing to release.
			}
		}
	}
}
