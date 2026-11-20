# Test fixtures

Everything in this folder is **fictional test data** for the application id `ht-fixture-app`.
The identifiers use the reserved year `0000` (`HTSA-0000-NNNN`), so they can never collide with a real
advisory. No fixture carries a CVE or GHSA identifier.

To regenerate the fixtures, run `python -I security/validation/generate_fixtures.py`.

| Path | Purpose |
|---|---|
| `advisories/` | Valid fixtures covering every status (Fixed, Affected, Under Review, Unknown, Not Affected), plus backports, platform rules, channel rules and an advisory with no fix |
| `invalid/` | Advisories that must be rejected: missing metadata, a malformed CVE, unknown fields |
| `expected-statuses.json` | Conformance table that every client implementation (.NET, web, Android) must reproduce |
| `release-feed.json` | Fixture release feed for the Software Update Service. It is unsigned; tests sign it with a throwaway key |

Fixtures are never signed with a production key. The tests and the screenshot harness generate a throwaway key at run time.
