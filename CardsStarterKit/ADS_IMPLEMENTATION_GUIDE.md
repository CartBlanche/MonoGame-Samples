# MonoGame Cross-Platform Ads Implementation Guide

## Overview

This guide outlines how to implement ads across Android, iOS, and Desktop platforms in the MonoGame CardsStarterKit project, including a "Pay to Remove Ads" feature.

---

## Platform Compatibility

### ✅ Fully Supported
- **Android**: Google AdMob (recommended) or Facebook Audience Network
- **iOS**: Google AdMob or Apple's Ad Network

### ⚠️ Limited Support
- **Desktop (Windows/macOS/Linux)**: No native mobile ad SDKs
  - Can display promotional banners for your own products
  - Use web-based ads (complex, poor UX)
  - **Recommended**: Skip ads entirely on desktop

---

## Recommended Architecture

The project already follows excellent patterns (see `2-Core/Misc/AudioManager.cs:45-59` and `1-Framework/Utils/UIUtility.cs:19-24`) that can be extended for ads.

### File Structure

```
1-Framework/Services/Ads/
├── IAdService.cs (interface)
├── AdPlacement.cs (enum: Banner, Interstitial, Rewarded)
└── AdConfiguration.cs (settings)

3-Games/Blackjack/Core/Services/
└── AdManager.cs (singleton, similar to AudioManager pattern)

3-Games/Blackjack/Platforms/Android/Services/
└── GoogleAdMobService.cs (Android implementation)

3-Games/Blackjack/Platforms/iOS/Services/
└── GoogleAdMobiOSService.cs (iOS implementation)

3-Games/Blackjack/Platforms/{Windows,Desktop}/Services/
└── NoAdService.cs (desktop stub implementation)
```

---

## Core Implementation

### 1. Ad Service Interface

**File**: `1-Framework/Services/Ads/IAdService.cs`

```csharp
namespace CardsFramework.Services.Ads
{
    public interface IAdService
    {
        bool IsInitialized { get; }
        bool AdsEnabled { get; set; } // Based on purchase status

        void Initialize(string appId);
        void LoadBanner(AdPlacement placement);
        void ShowBanner();
        void HideBanner();
        void LoadInterstitial();
        void ShowInterstitial(Action onClosed, Action onFailed);
        void LoadRewardedAd();
        void ShowRewardedAd(Action<bool> onResult); // bool = watched completely
    }
}
```

### 2. Ad Placement Enum

**File**: `1-Framework/Services/Ads/AdPlacement.cs`

```csharp
namespace CardsFramework.Services.Ads
{
    public enum AdPlacement
    {
        TopBanner,
        BottomBanner,
        Interstitial,
        RewardedVideo
    }
}
```

### 3. Ad Manager (Singleton Pattern)

**File**: `3-Games/Blackjack/Core/Services/AdManager.cs`

```csharp
using System;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using CardsFramework.Services.Ads;

namespace Blackjack.Core.Services
{
    public class AdManager : GameComponent
    {
        private static AdManager _instance;
        private readonly IAdService _adService;
        private readonly ConcurrentQueue<Action> _pendingCallbacks = new();

        public static AdManager Instance => _instance;
        public bool AdsEnabled => _adService?.AdsEnabled ?? false;

        private AdManager(Game game, IAdService adService) : base(game)
        {
            _adService = adService;
        }

        public static void Initialize(Game game, IAdService adService)
        {
            if (_instance != null)
                return;

            _instance = new AdManager(game, adService);
            game.Components.Add(_instance);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Process ad callbacks on game thread
            while (_pendingCallbacks.TryDequeue(out var callback))
            {
                callback?.Invoke();
            }
        }

        public void ShowInterstitialAd(Action onClosed = null, Action onFailed = null)
        {
            if (!AdsEnabled || _adService == null)
            {
                onClosed?.Invoke();
                return;
            }

            _adService.ShowInterstitial(
                () => _pendingCallbacks.Enqueue(() => onClosed?.Invoke()),
                () => _pendingCallbacks.Enqueue(() => onFailed?.Invoke())
            );
        }

        public void ShowBanner()
        {
            if (AdsEnabled && _adService != null)
                _adService.ShowBanner();
        }

        public void HideBanner()
        {
            _adService?.HideBanner();
        }
    }
}
```

### 4. Extend GameSettings

**File**: `3-Games/Blackjack/Core/GameSettings.cs`

Add this property to the existing `GameSettings` class:

```csharp
public class GameSettings
{
    // Add new property
    public bool AdFreeVersion { get; set; } = false;

    // Existing Save/Load methods handle persistence automatically
}
```

---

## Platform-Specific Implementation

### Android (Google AdMob)

#### Step 1: Add NuGet Package

**File**: `3-Games/Blackjack/Platforms/Android/BlackJack.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Xamarin.Google.Android.Play.Services.Ads" Version="23.0.0" />
</ItemGroup>
```

#### Step 2: Update AndroidManifest.xml

**File**: `3-Games/Blackjack/Platforms/Android/AndroidManifest.xml`

Add inside `<application>` tag:

```xml
<meta-data
    android:name="com.google.android.gms.ads.APPLICATION_ID"
    android:value="ca-app-pub-YOUR_ID~YOUR_APP_ID"/>
```

#### Step 3: Create GoogleAdMobService

**File**: `3-Games/Blackjack/Platforms/Android/Services/GoogleAdMobService.cs`

```csharp
using Android.Gms.Ads;
using Android.Gms.Ads.Interstitial;
using CardsFramework.Services.Ads;
using System;

namespace Blackjack.Android.Services
{
    public class GoogleAdMobService : IAdService
    {
        private InterstitialAd _interstitialAd;
        private string _interstitialAdUnitId;
        private Action _onInterstitialClosed;
        private Action _onInterstitialFailed;

        public bool IsInitialized { get; private set; }
        public bool AdsEnabled { get; set; } = true;

        public void Initialize(string appId)
        {
            MobileAds.Initialize(MainActivity.Instance, initStatus =>
            {
                IsInitialized = true;
            });
        }

        public void LoadInterstitial()
        {
            if (!AdsEnabled) return;

            var adRequest = new AdRequest.Builder().Build();
            InterstitialAd.Load(
                MainActivity.Instance,
                _interstitialAdUnitId,
                adRequest,
                new InterstitialAdLoadCallback(this)
            );
        }

        public void ShowInterstitial(Action onClosed, Action onFailed)
        {
            if (!AdsEnabled || _interstitialAd == null)
            {
                onClosed?.Invoke();
                return;
            }

            _onInterstitialClosed = onClosed;
            _onInterstitialFailed = onFailed;

            _interstitialAd.FullScreenContentCallback = new FullScreenContentCallback(this);
            _interstitialAd.Show(MainActivity.Instance);
        }

        // Implement other interface methods (Banner, Rewarded, etc.)
        public void LoadBanner(AdPlacement placement) { }
        public void ShowBanner() { }
        public void HideBanner() { }
        public void LoadRewardedAd() { }
        public void ShowRewardedAd(Action<bool> onResult) { }

        // Callback classes
        private class InterstitialAdLoadCallback : Android.Gms.Ads.Interstitial.InterstitialAdLoadCallback
        {
            private readonly GoogleAdMobService _service;

            public InterstitialAdLoadCallback(GoogleAdMobService service)
            {
                _service = service;
            }

            public override void OnAdLoaded(InterstitialAd ad)
            {
                _service._interstitialAd = ad;
            }

            public override void OnAdFailedToLoad(LoadAdError error)
            {
                _service._interstitialAd = null;
            }
        }

        private class FullScreenContentCallback : Android.Gms.Ads.FullScreenContentCallback
        {
            private readonly GoogleAdMobService _service;

            public FullScreenContentCallback(GoogleAdMobService service)
            {
                _service = service;
            }

            public override void OnAdDismissedFullScreenContent()
            {
                _service._interstitialAd = null;
                _service._onInterstitialClosed?.Invoke();
                _service.LoadInterstitial(); // Preload next ad
            }

            public override void OnAdFailedToShowFullScreenContent(AdError error)
            {
                _service._interstitialAd = null;
                _service._onInterstitialFailed?.Invoke();
            }
        }
    }
}
```

#### Step 4: Initialize in MainActivity

**File**: `3-Games/Blackjack/Platforms/Android/MainActivity.cs`

```csharp
using Blackjack.Android.Services;
using Blackjack.Core.Services;

protected override void OnCreate(Bundle savedInstanceState)
{
    base.OnCreate(savedInstanceState);

    // Initialize ad service
    var adService = new GoogleAdMobService();
    adService.Initialize("ca-app-pub-YOUR_ID~YOUR_APP_ID");

    // Create game with ad service
    _game = new BlackjackGame();
    AdManager.Initialize(_game, adService);

    // Check if user purchased ad removal
    var settings = GameSettings.LoadSettings();
    adService.AdsEnabled = !settings.AdFreeVersion;

    // Preload first interstitial
    if (adService.AdsEnabled)
        adService.LoadInterstitial();

    // Existing code...
    SetContentView(_game.Services.GetService(typeof(View)) as View);
    _game.Run();
}
```

---

### iOS (Google AdMob)

#### Step 1: Add NuGet Package

**File**: `3-Games/Blackjack/Platforms/iOS/BlackJack.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Xamarin.Google.iOS.MobileAds" Version="11.0.0" />
</ItemGroup>
```

#### Step 2: Update Info.plist

**File**: `3-Games/Blackjack/Platforms/iOS/Info.plist`

```xml
<key>GADApplicationIdentifier</key>
<string>ca-app-pub-YOUR_ID~YOUR_APP_ID</string>

<!-- For iOS 14+ App Tracking Transparency -->
<key>NSUserTrackingUsageDescription</key>
<string>This allows us to show you relevant ads</string>

<key>SKAdNetworkItems</key>
<array>
  <dict>
    <key>SKAdNetworkIdentifier</key>
    <string>cstr6suwn9.skadnetwork</string>
  </dict>
  <!-- Add more SKAdNetwork IDs as needed -->
</array>
```

#### Step 3: Create GoogleAdMobiOSService

**File**: `3-Games/Blackjack/Platforms/iOS/Services/GoogleAdMobiOSService.cs`

```csharp
using Google.MobileAds;
using CardsFramework.Services.Ads;
using System;
using Foundation;
using UIKit;

namespace Blackjack.iOS.Services
{
    public class GoogleAdMobiOSService : IAdService
    {
        private Interstitial _interstitialAd;
        private string _interstitialAdUnitId;
        private Action _onInterstitialClosed;
        private Action _onInterstitialFailed;

        public bool IsInitialized { get; private set; }
        public bool AdsEnabled { get; set; } = true;

        public void Initialize(string appId)
        {
            MobileAds.SharedInstance.Start((status) =>
            {
                IsInitialized = true;
            });

            // Request tracking permission (iOS 14+)
            RequestTrackingPermission();
        }

        private void RequestTrackingPermission()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            {
                AppTrackingTransparency.ATTrackingManager.RequestTrackingAuthorization(
                    (status) =>
                    {
                        // Permission granted or denied
                    }
                );
            }
        }

        public void LoadInterstitial()
        {
            if (!AdsEnabled) return;

            var request = Request.GetDefaultRequest();
            Interstitial.Load(_interstitialAdUnitId, request, (ad, error) =>
            {
                if (error != null)
                {
                    _interstitialAd = null;
                    return;
                }

                _interstitialAd = ad;
                _interstitialAd.FullScreenContentDelegate = new InterstitialDelegate(this);
            });
        }

        public void ShowInterstitial(Action onClosed, Action onFailed)
        {
            if (!AdsEnabled || _interstitialAd == null)
            {
                onClosed?.Invoke();
                return;
            }

            _onInterstitialClosed = onClosed;
            _onInterstitialFailed = onFailed;

            var rootViewController = UIApplication.SharedApplication.KeyWindow.RootViewController;
            _interstitialAd.Present(rootViewController);
        }

        // Implement other interface methods
        public void LoadBanner(AdPlacement placement) { }
        public void ShowBanner() { }
        public void HideBanner() { }
        public void LoadRewardedAd() { }
        public void ShowRewardedAd(Action<bool> onResult) { }

        private class InterstitialDelegate : FullScreenContentDelegate
        {
            private readonly GoogleAdMobiOSService _service;

            public InterstitialDelegate(GoogleAdMobiOSService service)
            {
                _service = service;
            }

            public override void DidDismissFullScreenContent(NSObject ad)
            {
                _service._interstitialAd = null;
                _service._onInterstitialClosed?.Invoke();
                _service.LoadInterstitial(); // Preload next ad
            }

            public override void DidFailToPresentFullScreenContent(NSObject ad, NSError error)
            {
                _service._interstitialAd = null;
                _service._onInterstitialFailed?.Invoke();
            }
        }
    }
}
```

#### Step 4: Initialize in Program.cs

**File**: `3-Games/Blackjack/Platforms/iOS/Program.cs`

```csharp
using Blackjack.iOS.Services;
using Blackjack.Core.Services;

static void Main(string[] args)
{
    UIApplication.Main(args, null, typeof(AppDelegate));
}

public class AppDelegate : UIApplicationDelegate
{
    private BlackjackGame _game;

    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        // Initialize ad service
        var adService = new GoogleAdMobiOSService();
        adService.Initialize("ca-app-pub-YOUR_ID~YOUR_APP_ID");

        // Create game
        _game = new BlackjackGame();
        AdManager.Initialize(_game, adService);

        // Check if user purchased ad removal
        var settings = GameSettings.LoadSettings();
        adService.AdsEnabled = !settings.AdFreeVersion;

        // Preload first interstitial
        if (adService.AdsEnabled)
            adService.LoadInterstitial();

        _game.Run();
        return true;
    }
}
```

---

### Desktop/Windows (No Ads)

#### Create NoAdService

**File**: `3-Games/Blackjack/Platforms/Desktop/Services/NoAdService.cs`

```csharp
using CardsFramework.Services.Ads;
using System;

namespace Blackjack.Desktop.Services
{
    public class NoAdService : IAdService
    {
        public bool IsInitialized => true;
        public bool AdsEnabled { get; set; } = false;

        public void Initialize(string appId) { }
        public void LoadBanner(AdPlacement placement) { }
        public void ShowBanner() { }
        public void HideBanner() { }
        public void LoadInterstitial() { }
        public void ShowInterstitial(Action onClosed, Action onFailed)
        {
            onClosed?.Invoke(); // Immediately callback
        }
        public void LoadRewardedAd() { }
        public void ShowRewardedAd(Action<bool> onResult)
        {
            onResult?.Invoke(false); // No reward
        }
    }
}
```

#### Initialize in Program.cs

**File**: `3-Games/Blackjack/Platforms/Desktop/Program.cs`

```csharp
using Blackjack.Desktop.Services;
using Blackjack.Core.Services;

static void Main()
{
    var adService = new NoAdService();

    using (var game = new BlackjackGame())
    {
        AdManager.Initialize(game, adService);
        game.Run();
    }
}
```

---

## Pay-to-Remove Ads Implementation

### Option A: Platform-Specific In-App Purchases (Recommended)

#### Purchase Service Interface

**File**: `1-Framework/Services/Purchase/IPurchaseService.cs`

```csharp
using System;
using System.Threading.Tasks;

namespace CardsFramework.Services.Purchase
{
    public interface IPurchaseService
    {
        Task<bool> InitializeAsync();
        Task<PurchaseResult> PurchaseAdRemovalAsync(string productId);
        Task<bool> RestorePurchasesAsync();
        bool IsAdRemovalPurchased();
    }

    public class PurchaseResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string TransactionId { get; set; }
    }
}
```

#### Purchase Manager

**File**: `3-Games/Blackjack/Core/Services/PurchaseManager.cs`

```csharp
using System.Threading.Tasks;
using CardsFramework.Services.Purchase;

namespace Blackjack.Core.Services
{
    public class PurchaseManager
    {
        private static PurchaseManager _instance;
        private readonly IPurchaseService _purchaseService;

        public static PurchaseManager Instance => _instance;

        private PurchaseManager(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        public static void Initialize(IPurchaseService purchaseService)
        {
            _instance = new PurchaseManager(purchaseService);
        }

        public async Task<bool> PurchaseAdRemovalAsync()
        {
            var result = await _purchaseService.PurchaseAdRemovalAsync("com.yourcompany.blackjack.removeads");

            if (result.Success)
            {
                // Save to settings
                var settings = GameSettings.LoadSettings();
                settings.AdFreeVersion = true;
                settings.SaveSettings();

                // Disable ads
                if (AdManager.Instance != null)
                {
                    AdManager.Instance.HideBanner();
                }

                return true;
            }

            return false;
        }

        public async Task<bool> RestorePurchasesAsync()
        {
            var restored = await _purchaseService.RestorePurchasesAsync();

            if (restored)
            {
                var settings = GameSettings.LoadSettings();
                settings.AdFreeVersion = true;
                settings.SaveSettings();

                if (AdManager.Instance != null)
                {
                    AdManager.Instance.HideBanner();
                }
            }

            return restored;
        }
    }
}
```

#### Android: Google Play Billing

**NuGet Package**: Add `Plugin.InAppBilling` to Android project

**File**: `3-Games/Blackjack/Platforms/Android/Services/GooglePlayPurchaseService.cs`

```csharp
using System;
using System.Threading.Tasks;
using Plugin.InAppBilling;
using CardsFramework.Services.Purchase;

namespace Blackjack.Android.Services
{
    public class GooglePlayPurchaseService : IPurchaseService
    {
        private const string AD_REMOVAL_PRODUCT_ID = "com.yourcompany.blackjack.removeads";

        public async Task<bool> InitializeAsync()
        {
            return await CrossInAppBilling.Current.ConnectAsync();
        }

        public async Task<PurchaseResult> PurchaseAdRemovalAsync(string productId)
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase);

                if (purchase != null)
                {
                    return new PurchaseResult
                    {
                        Success = true,
                        TransactionId = purchase.Id
                    };
                }

                return new PurchaseResult { Success = false, Error = "Purchase cancelled" };
            }
            catch (Exception ex)
            {
                return new PurchaseResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<bool> RestorePurchasesAsync()
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);

                foreach (var purchase in purchases)
                {
                    if (purchase.ProductId == AD_REMOVAL_PRODUCT_ID)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsAdRemovalPurchased()
        {
            var settings = GameSettings.LoadSettings();
            return settings.AdFreeVersion;
        }
    }
}
```

#### iOS: Apple StoreKit

**File**: `3-Games/Blackjack/Platforms/iOS/Services/AppleStorePurchaseService.cs`

```csharp
using System;
using System.Threading.Tasks;
using Plugin.InAppBilling;
using CardsFramework.Services.Purchase;

namespace Blackjack.iOS.Services
{
    public class AppleStorePurchaseService : IPurchaseService
    {
        private const string AD_REMOVAL_PRODUCT_ID = "com.yourcompany.blackjack.removeads";

        public async Task<bool> InitializeAsync()
        {
            return await CrossInAppBilling.Current.ConnectAsync();
        }

        public async Task<PurchaseResult> PurchaseAdRemovalAsync(string productId)
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase);

                if (purchase != null)
                {
                    return new PurchaseResult
                    {
                        Success = true,
                        TransactionId = purchase.Id
                    };
                }

                return new PurchaseResult { Success = false, Error = "Purchase cancelled" };
            }
            catch (Exception ex)
            {
                return new PurchaseResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<bool> RestorePurchasesAsync()
        {
            try
            {
                var billing = CrossInAppBilling.Current;
                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);

                foreach (var purchase in purchases)
                {
                    if (purchase.ProductId == AD_REMOVAL_PRODUCT_ID)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsAdRemovalPurchased()
        {
            var settings = GameSettings.LoadSettings();
            return settings.AdFreeVersion;
        }
    }
}
```

---

## Game Integration

### Show Ads at Appropriate Times

**File**: `3-Games/Blackjack/Core/BlackjackCardGame.cs`

Add interstitial ads between game rounds:

```csharp
private int _gamesPlayedSinceAd = 0;
private const int GAMES_BETWEEN_ADS = 3; // Show ad every 3 games

private void OnGameEnded()
{
    _gamesPlayedSinceAd++;

    // Show interstitial ad every N games
    if (_gamesPlayedSinceAd >= GAMES_BETWEEN_ADS)
    {
        _gamesPlayedSinceAd = 0;

        AdManager.Instance?.ShowInterstitialAd(
            onClosed: () =>
            {
                // Continue to next game
                StartNewGame();
            },
            onFailed: () =>
            {
                // Ad failed, continue anyway
                StartNewGame();
            }
        );
    }
    else
    {
        StartNewGame();
    }
}
```

### Pause Game During Ads

```csharp
public override void Update(GameTime gameTime)
{
    // Don't update game logic if ad is showing
    if (AdManager.Instance?.IsShowingAd ?? false)
        return;

    base.Update(gameTime);
    // ... rest of update logic
}
```

---

## Best Practices

### Ad Placement Strategy

**DO:**
- Show interstitial ads **between game rounds** (after hand completes)
- Show banner at bottom during menu screens only
- Offer rewarded ads for bonus chips/currency (optional)
- Space ads at least 2-3 minutes apart

**DON'T:**
- Show ads during active gameplay
- Show ads more than once every 2-3 minutes
- Block game controls with banner ads
- Show ads immediately after app launch

### Testing

1. **Use Test Ad Units** during development:
   - Android Test Interstitial: `ca-app-pub-3940256099942544/1033173712`
   - iOS Test Interstitial: `ca-app-pub-3940256099942544/4411468910`

2. **Test Purchase Flow**:
   - Android: Use "license testing" accounts in Google Play Console
   - iOS: Use sandbox testing accounts in App Store Connect

3. **Test Scenarios**:
   - First launch (no purchases)
   - Purchase ad removal
   - Reinstall app (restore purchases)
   - Ad load failures
   - Network offline

### Revenue Optimization

**Pricing Strategy:**
- $2.99-$4.99 USD for ad removal (one-time purchase)
- Lower price = higher conversion rate
- Consider regional pricing

**Ad Frequency:**
- 1 interstitial every 3-5 game rounds
- Banner always visible in menus (if not purchased)
- Never show ads to paying users

---

## Implementation Roadmap

### Phase 1: Foundation (2-3 hours)
- [ ] Create `IAdService` interface in `1-Framework`
- [ ] Create `AdManager.cs` in `Blackjack/Core`
- [ ] Extend `GameSettings.cs` to include `AdFreeVersion` property
- [ ] Modify `BlackjackGame.cs` constructor to initialize `AdManager`

### Phase 2: Android Implementation (4-5 hours)
- [ ] Add Google AdMob NuGet package
- [ ] Create `GoogleAdMobService.cs`
- [ ] Modify `MainActivity.cs` to initialize ads
- [ ] Update `AndroidManifest.xml` with AdMob app ID
- [ ] Test ad display and callbacks

### Phase 3: iOS Implementation (4-5 hours)
- [ ] Add Google AdMob iOS NuGet package
- [ ] Create `GoogleAdMobiOSService.cs`
- [ ] Update `Info.plist` with AdMob app ID and ATT
- [ ] Modify `Program.cs` to initialize ads
- [ ] Test ad display and callbacks

### Phase 4: Desktop Handling (1 hour)
- [ ] Create `NoAdService.cs` stub
- [ ] Initialize in Desktop `Program.cs`
- [ ] Optional: Add "Support the Developer" link

### Phase 5: In-App Purchase System (6-8 hours)
- [ ] Create `IPurchaseService` interface
- [ ] Create `PurchaseManager.cs`
- [ ] Implement `GooglePlayPurchaseService` (Android)
- [ ] Implement `AppleStorePurchaseService` (iOS)
- [ ] Add "Remove Ads" button to settings screen
- [ ] Implement restore purchases flow

### Phase 6: Game Integration (2-3 hours)
- [ ] Add ad calls in `BlackjackCardGame.cs`
- [ ] Implement game pause during ads
- [ ] Test ad frequency and timing
- [ ] Polish UI feedback

### Phase 7: Testing & Polish (4-6 hours)
- [ ] Test all ad scenarios (load, show, fail)
- [ ] Test purchase flow end-to-end
- [ ] Test restore purchases on new device
- [ ] Test offline behavior
- [ ] Performance testing
- [ ] Submit for review (Google Play, App Store)

**Total Estimated Time**: 23-31 hours

---

## Required Credentials

### Google AdMob

1. Create account at https://admob.google.com
2. Create App entries for Android and iOS
3. Create Ad Units for each app:
   - Interstitial ad unit
   - Banner ad unit (optional)
   - Rewarded video ad unit (optional)
4. Get Application IDs:
   - Format: `ca-app-pub-XXXXXXXXXXXXXXXX~XXXXXXXXXX`
   - One for Android, one for iOS

### Google Play Console (Android IAP)

1. Create app in Play Console
2. Navigate to Monetization → In-app products
3. Create managed product:
   - Product ID: `com.yourcompany.blackjack.removeads`
   - Price: $2.99 (or your choice)
4. Add license testing accounts

### Apple App Store Connect (iOS IAP)

1. Create app in App Store Connect
2. Navigate to In-App Purchases
3. Create non-consumable product:
   - Product ID: `com.yourcompany.blackjack.removeads`
   - Price: $2.99 (or your choice)
4. Create sandbox test users

---

## Troubleshooting

### Ads Not Showing

1. Check if using correct Ad Unit IDs (not Application ID)
2. Verify internet connectivity
3. Check AdMob account status (suspended?)
4. Look for initialization errors in logs
5. Ensure test device is registered if using test mode

### Purchase Failed

1. Check if product ID matches exactly
2. Verify app is published (or use test accounts)
3. Check billing permissions in AndroidManifest
4. Verify app is configured for IAP in store console
5. Check sandbox/test account credentials

### Threading Issues

Ad callbacks happen on UI thread, not game thread. Always queue callbacks:

```csharp
private ConcurrentQueue<Action> _pendingCallbacks = new();

// In ad callback
_pendingCallbacks.Enqueue(() => onClosed?.Invoke());

// In Update()
while (_pendingCallbacks.TryDequeue(out var callback))
    callback?.Invoke();
```

---

## Additional Resources

- [Google AdMob Documentation](https://developers.google.com/admob)
- [Google Play Billing Documentation](https://developer.android.com/google/play/billing)
- [Apple StoreKit Documentation](https://developer.apple.com/storekit/)
- [Plugin.InAppBilling GitHub](https://github.com/jamesmontemagno/InAppBillingPlugin)
- [MonoGame Documentation](https://docs.monogame.net/)

---

## Notes

- This guide assumes familiarity with C#, MonoGame, and mobile development
- Ad IDs shown are test IDs; replace with your actual IDs from AdMob
- IAP product IDs must match exactly in code and store console
- Always test purchases with sandbox/test accounts before production
- Consider GDPR/CCPA compliance for EU/CA users (consent management)
- Keep `GameSettings.cs` simple; it already handles persistence correctly

---

**Last Updated**: 2026-01-05
**MonoGame Version**: 3.8+
**Target Framework**: .NET 9.0
