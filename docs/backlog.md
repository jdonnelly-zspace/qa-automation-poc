# QA Automation Backlog

Prioritized list of future automation items beyond the 4 POC prototypes. Items are ranked by impact and feasibility.

## Priority Order

| # | Item | Description | Estimated Effort | Prerequisites |
|---|------|-------------|-----------------|---------------|
| 5 | Play-Mode Integration Tests | Launch the app in Unity Play mode and test scene loading, navigation, context menus. Validates end-to-end behavior within the editor. | 3-4 weeks | Prototype #2 (unit tests) provides the foundation |
| 6 | WebGL Smoke Tests | Use Playwright or Selenium to load the WebGL build in a browser, verify it renders, and click through basic interactions. | 2-3 weeks | Needs a hosted WebGL build URL |
| 7 | Sentry Error Regression Alerts | Automate Sentry monitoring to flag new error types introduced per build. Create a script that compares Sentry issues before and after a release. | 1-2 weeks | Sentry API access |
| 8 | Addressable Asset Integrity | Deep validation that all Unity Addressable asset groups resolve correctly and bundles are not corrupted. Extends Prototype #1. | 1-2 weeks | Prototype #1 provides the framework |
| 9 | Performance Benchmarking | Automated FPS and memory profiling across all scenes. Track metrics over time to catch performance regressions. | 3-4 weeks | Unity Profiler API, baseline metrics |
| 10 | Stylus Input Simulation | Build a hardware abstraction layer to mock zSpace stylus events, enabling automated testing of input handling code paths. | 4-6 weeks | Requires significant architecture work |
| 11 | Stereoscopy/Head Tracking Validation | Automated checks for stereo rendering and head tracking accuracy. The 15 test cases in this category are currently all manual. | 4-6 weeks | Requires physical zSpace hardware |
| 12 | zView Integration Tests | Test presenter/viewer mode switching, object masking, and background handling in zView. | 3-4 weeks | Requires zSpace hardware + zView |
| 13 | Code Signing Verification | Validate the GlobalSign certificate chain on built executables. Low risk — manual check takes under a minute. | 0.5 weeks | Partially covered by Prototype #1 |
| 14 | Activity Pack Version Sync | Detect content drift between the two activity-pack repos (production vs source). | 1 week | Git diff tooling |

## Decision Criteria

Items were ranked using these factors:
- **No hardware required** items rank higher (can run on any machine)
- **Reusable across all zSpace apps** items rank higher
- **Builds on existing work** (extends a prototype) ranks higher
- **Hardware-dependent** items are lowest priority (require physical zSpace display)

## Recommended Next Phase

After the 4 prototypes are validated, the dev team should tackle items **5-8** as Phase 2. These build directly on the prototype foundation and still don't require zSpace hardware. Items 9-14 form Phase 3 and require either significant architecture work or physical hardware.
