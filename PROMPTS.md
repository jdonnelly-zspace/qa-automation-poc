# Claude Code Prompts for QA Automation

Copy/paste these prompts into Claude Code to run QA automation.

---

## Single App

```
Run QA automation for this zSpace Unity app: https://github.com/zspace/apps.studioa3

Use run_qa.py in the qa-automation-poc project to run the full pipeline. Open the HTML report when done.
```

---

## Multiple Apps

```
Run QA automation for these zSpace Unity apps:
- https://github.com/zspace/apps.studioa3
- https://github.com/zspace/apps.franklinslab-a3

Use run_qa.py in the qa-automation-poc project to run the full pipeline for each. Open the HTML reports when done.
```

---

## Local Repo (Already Cloned)

```
Run QA automation for the local Unity project at ../apps.studioa3

Use run_qa.py in the qa-automation-poc project to run the full pipeline. Open the HTML report when done.
```

---

## What These Prompts Do

Each prompt triggers the full pipeline:
1. Clones the repo (if URL) or uses the local path
2. Runs 4 scans: source code, localization, assets, test coverage
3. Includes Unity unit test results if available
4. Generates an HTML dashboard in `output/reports/`

No flags, configs, or extra setup needed.

---

## Create Jira Issues from Scan Failures

```
Run the QA scan for Studio A3, then create Jira issues from any failures:

1. python run_qa.py ../apps.studioa3
2. python 03-test-management/create_jira_issues.py --results-file output/reports/full_scan_studio-a3_*.json --config configs/studio-a3.json --dry-run
3. Review the preview, then create each issue using the Jira MCP tools.
```

### Dry Run Only (Preview)

```
Preview what Jira issues would be created from the latest scan:
  python 03-test-management/create_jira_issues.py --results-file output/reports/full_scan_studio-a3_*.json --config configs/studio-a3.json --dry-run --skip-dedup
```

### Franklin's Lab A3

```
python 03-test-management/create_jira_issues.py --results-file output/reports/full_scan_franklins-lab-a3_*.json --config configs/franklins-lab-a3.json --dry-run
```
