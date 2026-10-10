-- Phase 5: leaderboards and game counts (docs/LEADERBOARD.md). Few rows per match: 1 run + 2 best upserts + 1 stats upsert.

-- One row per recorded match (default rules only). id = relay match id ("r:CODE:startms") or the solo start token's nonce ("s:..."),
-- so the same match / token can never be recorded twice.
CREATE TABLE runs (
  id TEXT PRIMARY KEY,
  created INTEGER NOT NULL,       -- unix seconds (end of the match)
  day TEXT NOT NULL,              -- UTC YYYY-MM-DD of `created`
  mode TEXT NOT NULL,             -- 'solo' | 'team'
  names TEXT NOT NULL,            -- display: "Ana, Bo"
  nkey TEXT NOT NULL,             -- lower case, sorted, comma joined (the board key)
  players INTEGER NOT NULL,
  wave INTEGER NOT NULL,
  kills INTEGER NOT NULL,         -- solo: own kills, team: sum of the players' kills
  secs INTEGER NOT NULL,          -- match time
  detail TEXT,                    -- team: JSON [[name, kills], ...]
  source TEXT NOT NULL,           -- 'relay' (Room DO, multiplayer) | 'client' (solo POST /api/score)
  hidden INTEGER NOT NULL DEFAULT 0 -- test rows (a name starting with "zbt-"): never on the public boards
);
CREATE INDEX runs_day ON runs(day);

-- Best result per board key: period = 'all' or a UTC day. Best wave (ties: more kills) and most kills are kept separately.
CREATE TABLE best (
  mode TEXT NOT NULL,
  period TEXT NOT NULL,
  nkey TEXT NOT NULL,
  names TEXT NOT NULL,
  hidden INTEGER NOT NULL DEFAULT 0,
  wave INTEGER NOT NULL,          -- best wave
  wave_kills INTEGER NOT NULL,    -- kills of that run
  wave_at INTEGER NOT NULL,
  kills INTEGER NOT NULL,         -- most kills
  kills_wave INTEGER NOT NULL,    -- wave of that run
  kills_at INTEGER NOT NULL,
  PRIMARY KEY (mode, period, nkey)
);
CREATE INDEX best_wave ON best(mode, period, hidden, wave DESC, wave_kills DESC);
CREATE INDEX best_kills ON best(mode, period, hidden, kills DESC, kills_wave DESC);

-- Games per UTC day, any rules: mode 'single' (offline single player) | 'online' (relay rooms, any player count).
-- started = matches started, finished = matches that ended or were left after starting, players = player slots of finished matches.
CREATE TABLE stats (
  day TEXT NOT NULL,
  mode TEXT NOT NULL,
  started INTEGER NOT NULL DEFAULT 0,
  finished INTEGER NOT NULL DEFAULT 0,
  players INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (day, mode)
);
