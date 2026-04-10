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
