#if ANDROID
using Android.Views;

namespace StokBarangMAUI.Platforms.Android
{
    // Tells the nearest scrollable parent (ScrollView, RecyclerView, etc.)
    // NOT to intercept touch events while the user is interacting with this view.
    // Without this, parent ScrollView consumes vertical drag before MAUI's
    // PanGestureRecognizer can react, making in-box pan/zoom impossible.
    public static class TouchInterceptor
    {
        public static void Attach(VisualElement view)
        {
            void DoAttach()
            {
                if (view.Handler?.PlatformView is global::Android.Views.View nv)
                {
                    nv.Touch += OnTouch;
                }
            }
            // Hook ke Loaded supaya pasti dipanggil setelah handler tersedia.
            // Kalau sudah loaded, langsung attach.
            if (view.Handler != null) DoAttach();
            view.Loaded            += (_, _) => DoAttach();
            view.HandlerChanged    += (_, _) => DoAttach();
        }

        private static void OnTouch(object? s, global::Android.Views.View.TouchEventArgs e)
        {
            if (e.Event == null) { e.Handled = false; return; }
            if (s is not global::Android.Views.View nv) { e.Handled = false; return; }

            // Walk up parent chain — request disallow on ALL ancestors so any scrollable
            // ancestor (ScrollView, RefreshLayout, etc.) won't intercept.
            switch (e.Event.Action & MotionEventActions.Mask)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Move:
                case MotionEventActions.Pointer1Down:
                case MotionEventActions.Pointer2Down:
                case MotionEventActions.Pointer3Down:
                    DisallowAncestors(nv, true);
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    DisallowAncestors(nv, false);
                    break;
            }
            e.Handled = false;
        }

        private static void DisallowAncestors(global::Android.Views.View view, bool disallow)
        {
            var p = view.Parent;
            while (p != null)
            {
                p.RequestDisallowInterceptTouchEvent(disallow);
                p = p.Parent;
            }
        }
    }
}
#endif
