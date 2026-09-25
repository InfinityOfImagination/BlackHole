# Where the specification landed

Section-by-section mapping from *Void Mart: Technical & Game Design Specification v1.0.0* to the
code, plus an honest list of the places the implementation deviates or stops short.

| Spec | Requirement | Where it lives | State |
|------|-------------|----------------|-------|
| 1 | ScriptableObject-event architecture + service locator | `Core/GameEventBus.cs`, `Core/GameEventChannels.cs`, `Core/ServiceLocator.cs` | Done |
| 1 | Unity 6 / URP target | Project is Unity 6000.3.17f1 with URP 17.3 | Done |
| 1 | Zero runtime allocation in gameplay loops | `Core/ObjectPool.cs`, `PoolService`, `NonAlloc` physics queries, `NumberFormat` | Done |
| 1 | Pre-allocated pools for props, currency drops and particles | `ServiceInstaller.RegisterCommonPools`, `CityGenerator.RegisterPools` | Done |
| 2 | Clay-shaded low-poly, saturated palette | `Shaders/ClayLit.shader`, `MeshLibrary`, `ThemeConfig` | Done |
| 2 | Desaturated street / pastel store split | `ThemeConfig` street vs store colours, `WorldTextureLibrary` | Done |
| 2 | Fixed tilt, 35° FOV, 55° downward | `CameraConfig.fieldOfView = 35`, `pitch = 55`, `CameraRig` | Done |
| 2 | Stencil hole: mask quad writes 1, ground `!= 1`, pit `== 1` | `HoleMask.shader`, `ClayLit` stencil block, `PitInterior.shader` | Done |
| 2 | Props kinematic while idle; wake + `SubGround` layer on consumption | `Gameplay/Swallowable.cs` | Done |
| 2 | `Scale(t) = lerp(1.0, 0.0, t^2.5)` | `Swallowable.Update`, exponent exposed as `swallowScalePower` | Done |
| 3 | Music shifts between exploration and interior | `AudioService.PlayMusicForArea`, crossfade, `AreaVolume` | Done |
| 3 | +0.03 semitones per successive swallow, cap +12, reset after 1.5 s | `AudioService.PlaySwallowPop` | Done |
| 3 | `sfx_suction_pop` — crisp plop, ±5% pitch variance | `AudioLibrary.SuctionPop`, `suctionPitchVariance` | Done |
| 3 | `sfx_unload` — continuous loop, volume tied to rate | `AudioLibrary.Unload`, `AudioService.SetUnloadRate`, `Hopper` | Done |
| 3 | `sfx_cash` — paper + register, max 6 voices | `AudioLibrary.Cash`, `cashMaxVoices` voice limiter | Done |
| 4 | Boot → splash/auth → core loop | `Scenes/Boot.unity`, `UI/Bootstrapper.cs` | Done |
| 4 | Slide-up EaseOutBack, pop EaseOutElastic, fade EaseInOutQuad | `UI/ModalPanel.cs`, `Core/Easing.cs` | Done |
| 4 | HUD: level+XP top-left, currencies top-right, capacity gauge centre-bottom | `UIFactory.BuildGameCanvas`, `HUDController` | Done |
| 4 | Iris wipe `R_max * (1 - t^3)` centred on the hole | `UI/IrisWipe.cs`, `Shaders/IrisWipe.shader` | Done |
| 5 | Socket transforms with trigger zones | `FurnishingNodeData.localPosition/zoneSize`, `FurnishingZone` | Done |
| 5 | Cash drains at an exponential tick rate while standing in a zone | `FurnishingZone.Drain`, `purchaseDrainExponent` | Done |
| 5 | Bills pop out of the player along parabolic Bezier paths | `Store/FlyingItem.cs`, `Easing.Bezier` | Done |
| 5 | `FurnishingNodeData` data model | `Data/FurnishingNodeData.cs` — spec fields keep their published names | Done |
| 5 | Trigger disabled, particle poof, 1.0 → 1.25 → 1.0 snap-in | `FurnishingZone.Complete`, `StoreBuilder.SnapIn` | Done |
| 6 | `TweakableAttribute(category, min, max)` | `Tweaker/TweakableAttribute.cs` (spec signature, plus optional `Label`) | Done |
| 6 | Reflection-driven registry, `RuntimeTweakerService.Register(this)` | `Tweaker/RuntimeTweakerService.cs`, called from `PlayerHoleController.Awake` | Done |
| 6 | 3-finger double tap in debug builds | `Tweaker/RuntimeTweakerOverlay.cs` (plus F1 in the editor) | Done |
| 7 | Ads behind a mediation-agnostic interface | `Monetization/AdInterfaces.cs`, `MockAdService` | Seam + stand-in |
| 7 | `rv_fever_mode` — 30 s of 3× size and auto-suction | `GameManager.RequestFever`, `HoleConfig.fever*`, HUD bolt button | Done |
| 7 | `rv_double_offline` — doubles offline profit on return | `GameManager.OfferDoubleOffline`, HUD "2X" button | Done |
| 7 | `int_level_clear` — on factory upgrade, 180 s cooldown, skipped for IAP users | `GameManager.OnNodePurchased`, `MockAdService.InterstitialAllowed` | Done |
| 7 | `iap_no_ads` — disables non-rewarded ads permanently | `MockIapService`, `profile.noAdsActive`, Settings panel | Done |
| 7 | `iap_starter_pack` — 5 000 gems + exclusive skin | `MockIapService.Purchase`, `starterPackGems`, `starterPackSkin` | Entitlement only |
| 8 | AES-256-GCM, PBKDF2 device key, atomic replace | `Services/SecureSaveEngine.cs` | Done, with deviations below |
| 8 | Save on suspension, on purchases, every 60 s | `SaveService` (`OnApplicationPause/Focus/Quit`, `autosaveSeconds`) | Done |
| 9 | Google Play Games Services v2, silent auth, save-conflict rule | `Monetization/LocalPlatformService.cs` | Seam only |
| 10 | Exact JSON schema | `Data/SaveModel.cs` — field-for-field, plus additive blocks | Done |

## Deviations, and why

**AES-GCM construction.** The spec's snippet calls `new AesGcm(key, 16)`. That two-argument
constructor only exists on .NET 8; Unity's .NET Standard 2.1 profile exposes `AesGcm(byte[])`.
`SecureSaveEngine` resolves whichever constructor is present by reflection, so the same file
compiles and runs on both.

**AES-CBC + HMAC fallback.** Not every Unity scripting backend ships a working `AesGcm` (WebGL
and some Mono builds throw `PlatformNotSupportedException`). Rather than lose saves on those
targets, the engine falls back to AES-256-CBC with HMAC-SHA256 in encrypt-then-MAC order, which
gives the same authenticated-encryption guarantee. A format byte in the header records which
path wrote the file, so saves stay readable across a platform switch.

**Google Play Games Services.** Integrating GPGS v2 requires its package and a configured
Play Console app, neither of which exist in this repository. `IPlatformService` defines the
surface, `LocalPlatformService` implements the silent-auth contract locally, and the cloud-save
conflict rule from section 9 (prefer the payload with more lifetime progress, then the newer
timestamp) is implemented and testable as `LocalPlatformService.ResolveConflict`.

**Ads and IAP.** Same shape: the interfaces are the integration point, and the mock services
reproduce the timing contract (a blocking playback, a cooldown, an entitlement that suppresses
interstitials) so the loop can be balanced before an SDK is added. No real mediation adaptor is
wired.

**Starter pack offer.** Purchasing grants the gems and the skin id, and the offer delay is
configured (`starterPackOfferAfterSeconds`), but there is no offer popup — and no skin art
behind `equippedSkin` yet, so the id is stored and not rendered.

**Frame target.** The project is built for the spec's 60 FPS mid-tier Android target — one
material for the world, single-quad ground, pooling everywhere, no per-frame allocation in the
hot paths — but it has not been profiled on device, so that is a design intent, not a measurement.

**Godot.** The spec allowed Unity or Godot; this is the Unity implementation.
