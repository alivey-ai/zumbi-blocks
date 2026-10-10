# Player profiles and cloud save

Players can come back on any browser or computer, also after clearing the browser's data, and get their progress back. Optional: guest play (no profile, everything in the browser's PlayerPrefs) is unchanged.

Free plan only: D1 (`zumbi-blocks`, tables `accounts`, `sessions`, migration `server/migrations/0002_accounts.sql`), the Lobby Durable Object for rate limit counters, WebCrypto for hashing.

## How a player uses it
Main menu > **Profile** (a new row below Quit; shows "Profile: NAME" when logged in).
* **Create profile**: profile name + password (twice). The name is the nickname (the game's rule: 2-10 letters / digits / space `_ - .`, not "player"), unique without regard to case. This browser's current save (money, items, look, settings) moves into the new profile. A **recovery code** (`XXXX-XXXX-XXXX-XXXX`) is shown once, with a Copy button: it is the only way to reset a forgotten password, there is no e-mail.
* **Log in** on another browser: name + password. If that browser has a guest save of its own (money or items differ from a new player's), the game first says so and offers **Keep profile save** (default) or Cancel. The profile's save is loaded, nothing is merged; the guest save is kept aside and comes back on log out.
* **Forgot password**: name + recovery code + new password. Logs this browser in, logs every other browser out, shows a new recovery code (the old one is used up).
* **Log out**: uploads the last changes, ends the session (server side too), and brings back this browser's guest save (a fresh guest after the first signup, whose save moved into the profile).
* **Delete profile**: needs the password. Removes the profile, its save and stats and every session; the name is free again. Leaderboard results stay as nickname results (they are public results, removable by hand, docs/LEADERBOARD.md).
* The profile screen shows **best wave, total kills, games played** (counted by the server: multiplayer matches by the relay, single player through `/api/score`, any rules as long as the result passes the plausibility checks).

While logged in, the nickname is always the profile name (the Appearance name field cannot change it: the next save puts it back).

## Cloud save
Everything `sc_gamecontrol.save()` writes (`ZB_v001_*`: nick, beard, beard colour, skin, shirt, auto aim, auto reload, money, volume, sensitivity, the inventory slots) plus this port's own settings (FOV, invert Y, graphics, Waves Timed/Classic, Loot More/Classic) as one JSON (< 1 KB, limit 4 KB) in `accounts.data`.
* **Upload**: `save()` calls `ZbProfile.OnSave` (one hook line). When logged in it marks the save dirty and uploads it at most every **10 s** (the first one about 1 s after the save; a match end saves the banked money, so it goes up within 10 s too). One upload = 1 session read + 1 `UPDATE` (one row per profile).
* **Page closed / reloaded**: the latest not yet uploaded save is staged in the page (`ZbAccount.jslib`) and sent with `navigator.sendBeacon` on `pagehide` (it carries the cookie and survives the unload).
* **Download**: on login (and on every page load while logged in, `GET /api/account/me`) the server copy replaces the local one. Exception: the local save has changes that were never uploaded and is based on the server's current revision (the upload simply did not happen yet): then it is uploaded instead.
* **Stale writes**: every upload sends the revision it is based on (`{rev, data}`); the server writes only if it still has that revision (`UPDATE ... WHERE rev = ?`) and answers 409 with its newer copy otherwise. The old tab then loads that copy ("This profile was saved from another tab or browser; its newer save was loaded"). So the newest write wins and an old tab never overwrites it; changes an old tab made before refreshing are lost on purpose (no merge).
* **First signup** sends this browser's save along (migration). Logging in to an existing profile never merges (see above).

## Sessions
* A random 32 byte token in an **httpOnly cookie** `zbs` (`Path=/; Max-Age=90 days; HttpOnly; SameSite=Lax`, `Secure` on https). The game never sees it: Unity's `UnityWebRequest`, the relay WebSocket upgrade and `sendBeacon` are all same-origin, so the browser attaches it. Chosen over a bearer token in PlayerPrefs because it is not readable by page scripts and authenticates the WebSocket without putting a secret in a URL.
* Only `SHA-256(token)` is stored (`sessions`). Sliding expiry: renewed to 90 days at most once a day. Revocable: log out deletes the row, password recovery and Delete profile delete all of the profile's rows.
* CSRF: `SameSite=Lax` plus every POST with a foreign `Origin` header is refused.

## Passwords and recovery
* **PBKDF2-SHA256**, 16 byte random salt per profile, **50,000 iterations** (stored per profile in `pw_iter`; raising `PBKDF2_ITER` rehashes on the next login). Measured: 3.9 ms per hash in node on an Apple M-series core (`accounts.test.mjs --unit` prints it); in local workerd a login with a wrong password (lobby counter + D1 read + PBKDF2) answers in about 7 ms wall time against about 3 ms for a request that stops before the hash. That keeps a request inside the Workers Free plan's 10 ms CPU; 100,000 iterations (workerd's maximum) would not. This is weaker than the OWASP 600,000 recommendation; the rate limits below are what makes online guessing impractical. A stolen database would allow offline guessing of weak passwords at a few hundred per second per core and guess.
* Passwords 6-72 characters, anything allowed. Logging in with an unknown name costs the same PBKDF2 as a known one.
* **Recovery code**: 16 symbols of a 32 letter alphabet (80 bits), stored as SHA-256 only (high entropy, no slow hash needed), shown once, replaced on every use. Typing is forgiving (case, spaces and dashes are ignored).

## Rate limits (Lobby DO counters, shared by all isolates; on top of the 240/min `/api/*` bucket)
| what | limit |
|---|---|
| signup | 10 per hour per IP |
| login | 30 per 10 min per IP, 10 per 10 min per name (any IP) |
| forgot password | 10 per hour per IP, 5 per hour per name |
| delete | 10 per 10 min per IP and per profile |
| cloud save | 30 per minute per profile |

Known trade-off: somebody can keep a profile's name in the "too many tries" state by spamming wrong logins (10 per 10 min); the owner then waits or uses a browser that is still logged in.

## Leaderboard names
* A registered profile name belongs to that profile. **Solo**: `/api/score` checks the request's session cookie; anybody else posting that name is recorded as **"name (guest)"** (a separate row; the owner's row is untouched). **Multiplayer**: the Worker checks the session cookie of every WebSocket that joins (host or client) and passes `acct=id:name` to the Room (never taken from the client); at the end of the match the relay looks up which reported names are registered and renames the ones whose owner did not join this room with a session to "name (guest)". The verified profiles get the match in their stats.
* Guests choose nicknames as before; a name that later gets registered by somebody else shows as "(guest)" from then on. A profile created with a name that already has (guest) leaderboard rows from before simply continues them (same board key).
* Host leaves and the match continues (level B+): the first part is recorded when the host leaves (the old team, including the host); the continued match is recorded at its end as its own run (the remaining players, a different team key, so it is a separate row; the board keeps the best row per team). The continuation is timed from the original start so the "wave vs real time" check stays correct, and its run id carries the room epoch so it is not mistaken for the first part.

## API (`/api/account/*`, JSON, `cache-control: no-store`)
| | request | answer |
|---|---|---|
| `POST signup` | `{name, password, data?}` | `{ok, name, rev, data, stats, recovery}` + cookie; 409 taken, 400 name/password, 413 save too big |
| `POST login` | `{name, password}` | profile + cookie; 401 |
| `GET me` | (cookie) | `{ok, name, rev, data, stats: {bestWave, kills, games}}`; 401 |
| `POST save` | `{rev, data}` | `{ok, rev}`; 409 `{rev, data, ...}` (stale: server copy); 401 |
| `POST recover` | `{name, code, password}` | profile + new `recovery` + cookie; 401 |
| `POST logout` | | clears the cookie, deletes the session |
| `POST delete` | `{password}` | deletes profile + sessions; 401 wrong password |

## Privacy
Stored per profile: the name, the password's salted PBKDF2 hash (never the password), the recovery code's SHA-256, the game save JSON, best wave / total kills / games played, creation and last save time, and per login session a hashed token with its expiry. No e-mail, no IP address. One cookie (`zbs`) that only keeps the player logged in. Stated on the game page and in the profile screen; Delete profile removes all of it.

## Test profiles
Names starting with `zbt-` are flagged `hidden`. Every automated test deletes the profiles it made (also when a check fails). Leftovers on the live database can be removed with
`npx wrangler d1 execute zumbi-blocks --remote --command "DELETE FROM sessions WHERE aid IN (SELECT id FROM accounts WHERE hidden = 1); DELETE FROM accounts WHERE hidden = 1"`.

## Tests
* `server/test/accounts.test.mjs` (in `npm test`): unit (hashing, recovery code, name protection) and integration against a running Worker: signup cookie + recovery code, case-insensitive uniqueness and input rules, foreign Origin refused, login / logout / revocation of a copied cookie, revision check (stale write 409 with the server copy), recovery (new code, other sessions logged out, old password and old code dead), rate limits (login per name, signup per IP), solo and relay name protection with stats, hidden test rooms, delete.
* `server/test/e2e/profile.mjs`: the real game: guest save -> Create profile (migration, recovery code), debounced upload, a fresh browser context (no cookies, no storage) with its own guest save logs in ("Keep profile save") and gets everything back, stale write from the old tab, reload keeps the session, log out restores the guest save, guest vs owner on the solo leaderboard, stats on the profile screen, forgot password, delete profile.

## Code
`server/src/accounts.js` (all of the above), `server/src/index.js` (`/api/account/*`, the session check for `/api/score` and WebSocket joins), `server/src/room.js` (`noteAcct`, `recordMatch`), `game/Assets/Scripts/ZbProfile.cs` (screens 50-56, upload / download, hooks), `game/Assets/Plugins/WebGL/ZbAccount.jslib` (sendBeacon, clipboard).
