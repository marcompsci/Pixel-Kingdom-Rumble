# Shadow Contracts (mission mode) and the $3.99 unlock

**What it is:** a stealth mission mode on the main menu (purple **SHADOW CONTRACTS** button).

- The board lists 4 main contracts and 2 secret ones.
- Contracts mix two objectives: steal a prize and take down a marked target (a Gearwatch Captain).
- Ancient relics are worth points: 250 each.
- A secret contract appears once you find its cipher scroll in another contract:
  - The Bell Keeper reveals The Cipher Crypt.
  - Harbor of Lanterns reveals Clocktower Shadow.

| Contract | Kind | Objective |
|---|---|---|
| First Light | **Free teaser** | Steal the Dawn Lantern |
| The Bell Keeper | Paid | Take down the Captain (cipher scroll in the back room) |
| Vault of the Brass Kings | Paid | Steal the Brass Crown |
| Harbor of Lanterns | Paid | Take down the smuggler captain (cipher scroll in the boathouse) |
| The Cipher Crypt | Secret, paid | Steal the Cipher Key |
| Clocktower Shadow | Secret, paid | Take down the last captain and steal the Master Key |

**Scoring** (`Core/Save/Missions.cs`, `MissionScore`):

| Part | Points |
|---|---|
| Each objective | +1000 |
| Each relic | +250 |
| Each takedown | +150 |
| Never spotted | +1500 |
| Each time spotted | −200 |
| Time under par | +10 per second |

Best score and relics are saved per contract. The board shows the total score and a title (RECRUIT → MASTER OF SHADOWS).

The maps are text in `Core/Level/ContractLayouts.cs`:

- **V** = target
- **R** = relic
- **Z** = cipher scroll

## The purchase

- **Product:** non-consumable, ID `com.marcompsci.pixelkingdomrumble.shadowcontracts`, price $3.99.
- **What it unlocks:** every contract except First Light.
- **Code:**
  - `Runtime/Store/PurchaseService.cs`
  - `Assets/_Project/IAP/UnityIapBackend.cs` (Unity IAP 4.15, the version in the project).
- **Restore:** RESTORE PURCHASE is on the board, as Apple requires for non-consumables.
- **Tester builds** (the default from `Tools/ios_build.command`) show **TESTER BUILD: UNLOCK ALL** so you can play everything before the App Store side exists. For an App Store build, run the build with `PKR_RELEASE=1` so that button is compiled out.

### What Omari has to do in App Store Connect (only you can)

1. **Agreements, Tax, and Banking:** accept the Paid Apps agreement and add bank and tax details. Purchases don't work until this is done.
2. **Create the app record** for bundle ID `com.marcompsci.pixelkingdomrumble` if it isn't there yet.
3. Under the app, open **In-App Purchases** and press **+**:
   - Type: **Non-Consumable**
   - Reference name: Shadow Contracts
   - Product ID: `com.marcompsci.pixelkingdomrumble.shadowcontracts` (must match exactly)
   - Price: $3.99 (USD)
   - Add a display name, a description and a review screenshot of the board.
4. **Users and Access → Sandbox → Testers:** add a sandbox Apple ID to test buying without being charged.
5. The first time, submit the in-app purchase together with an app version.

### Known limits

- Unity marks IAP 4 as deprecated: "unsupported as of June 8, 2026. IAP 5 is the supported version". It compiles and works today; move to IAP 5 before a public release.
- The purchase itself has only been checked in code and tests. A real sandbox purchase needs the steps above.
