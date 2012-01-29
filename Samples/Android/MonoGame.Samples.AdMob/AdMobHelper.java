package MonoGame;

import android.view.View;
import android.app.Activity;
import com.google.ads.AdView;
import com.google.ads.AdRequest;
import com.google.ads.AdSize;
import java.lang.String;

public class AdMobHelper
{
  private AdMobHelper() { }
  
  public static void addTestDevice(View view,String deviceid)
  {
    AdRequest request = new AdRequest();
    request.addTestDevice(AdRequest.TEST_EMULATOR);
    request.addTestDevice(deviceid); 
    ((AdView)view).loadAd(request);  
  }

  public static View createAdView(Activity activity, String appid)
  {
    // Create the adView
    AdView view = new AdView(activity, AdSize.BANNER, appid);    
    return view;
  }

  public static void requestFreshAd(View view)
  {
    ((AdView)view).loadAd(new AdRequest());
  }
}