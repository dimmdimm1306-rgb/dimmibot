package crc64f81407cc16c20524;


public class OAuthCallbackActivity
	extends crc6468b6408a11370c2f.WebAuthenticatorCallbackActivity
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("StokBarangMAUI.Platforms.Android.OAuthCallbackActivity, StokBarangMAUI", OAuthCallbackActivity.class, __md_methods);
	}

	public OAuthCallbackActivity ()
	{
		super ();
		if (getClass () == OAuthCallbackActivity.class) {
			mono.android.TypeManager.Activate ("StokBarangMAUI.Platforms.Android.OAuthCallbackActivity, StokBarangMAUI", "", this, new java.lang.Object[] {  });
		}
	}

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
