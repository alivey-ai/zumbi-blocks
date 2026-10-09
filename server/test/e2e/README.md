Browser end-to-end test: 4 headless Chrome pages (host, list joiner, code joiner, late joiner) play against a running server.

```
cd server && npm install --no-save playwright-core
npx wrangler dev                                  # or use the deployed URL
cd test/e2e
CH=chrome node e2e.mjs                            # BASE=https://zumbi-blocks.vito-senic.workers.dev/ CH=chrome node e2e.mjs
```
`CH=chrome` uses the installed Google Chrome with GPU rendering (needed for a playable frame rate; software GL is too slow and
the game's remote-player interpolation diverges at very low fps). Clicks use fixed canvas coordinates of the original menus.
Zombies can kill idle test players, so rarely a check is flaky; rerun.
