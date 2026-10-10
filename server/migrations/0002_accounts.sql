-- Player profiles (docs/ACCOUNTS.md): profile name + password, one cloud save per profile, small stats. Sessions are revocable bearer
-- secrets kept in an httpOnly cookie; only their SHA-256 is stored.

CREATE TABLE accounts (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,              -- display form, the game's nickname rule (2-10 letters / digits / space _ - .)
  nkey TEXT NOT NULL UNIQUE,       -- lower case name: unique, case-insensitive
  pw_hash TEXT NOT NULL,           -- PBKDF2-SHA256, hex
  pw_salt TEXT NOT NULL,           -- 16 random bytes, hex (per profile)
  pw_iter INTEGER NOT NULL,        -- iterations used for pw_hash (raised later = rehash on the next login)
  rec_hash TEXT NOT NULL,          -- SHA-256 of the recovery code (80 random bits, shown once)
  created INTEGER NOT NULL,        -- unix seconds
  rev INTEGER NOT NULL DEFAULT 0,  -- cloud save revision: a write must name the revision it is based on (stale writes are refused)
  data TEXT,                       -- cloud save: JSON of the game's save (money, inventory, look, settings), <= 4 KB
  updated INTEGER NOT NULL DEFAULT 0,
  best_wave INTEGER NOT NULL DEFAULT 0,
  kills INTEGER NOT NULL DEFAULT 0,
  games INTEGER NOT NULL DEFAULT 0,
  hidden INTEGER NOT NULL DEFAULT 0 -- test profiles (name starting with "zbt-")
);

CREATE TABLE sessions (
  th TEXT PRIMARY KEY,             -- SHA-256 of the session token (hex)
  aid INTEGER NOT NULL,
  created INTEGER NOT NULL,
  expires INTEGER NOT NULL         -- unix seconds; renewed (sliding, 90 days) at most once a day
);
CREATE INDEX sessions_aid ON sessions(aid);
