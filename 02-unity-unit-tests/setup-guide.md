# Unity Edit Mode Tests -- Setup Guide

## What Are Edit Mode Tests?

Edit Mode tests run inside the Unity Editor **without entering Play Mode**. They execute like traditional unit tests -- fast, isolated, and deterministic. Because they skip the full game loop (physics, rendering, scene loading), they complete in milliseconds per test.

**Why this matters for zSpace projects:**
- Tests run in 2-5 seconds total, not minutes
- They work in CI/Jenkins without a GPU or display
- No scenes need to be loaded, so tests are independent of scene configuration
- They catch logic bugs, content errors, and data issues before anyone presses Play


## How to Add These Tests to Your Unity Project

### Step 1: Create the test folder

Inside your Unity project, create:

```
Assets/
  Tests/
    EditModeTests/
```

### Step 2: Copy the files

Copy these four files into `Assets/Tests/EditModeTests/`:

- `EditModeTests.asmdef` (assembly definition -- Unity needs this to find the tests)
- `ActivityGalleryTests.cs`
- `InventoryTests.cs`
- `ContentValidationTests.cs`

### Step 3: Let Unity recompile

After copying, switch back to Unity and wait for the progress bar to finish. Unity will detect the new `.asmdef` and compile the test assembly automatically.

### Step 4: Wire up your actual classes

Each `.cs` file contains `TODO` markers showing exactly where to:
1. Add `using` directives for your real namespaces
2. Replace stub classes with references to real classes
3. Update method names to match your actual API

Search for `TODO` across all files to find every adaptation point.

### Step 5: Add assembly references (important)

The `EditModeTests.asmdef` file needs to reference any assembly that contains the classes you are testing. To do this:

1. Open `EditModeTests.asmdef` in the Unity Inspector
2. Under **Assembly Definition References**, click the `+` button
3. Add a reference to the assembly that contains your classes (e.g., your project's main `.asmdef`)

If your project does not use assembly definitions for its main code, the default `Assembly-CSharp` assembly is referenced automatically and you can skip this step.

In the JSON, it looks like this:

```json
{
    "references": [
        "YourProject.MainAssembly"
    ]
}
```


## Running Tests in the Unity Editor

1. Open the Test Runner: **Window > General > Test Runner**
2. Click the **EditMode** tab at the top
3. You should see the test classes listed in a tree view
4. Click **Run All** to execute every test
5. Green checkmarks = passed, red X = failed, yellow = skipped

**Tip:** You can right-click a single test and choose **Run** to execute just that one.

**Tip:** Double-click a failed test to jump to the assertion that failed.


## Running Tests from the Command Line (Jenkins / CI)

Use this command to run Edit Mode tests in batch mode (no UI, no GPU required):

```bash
"C:\Program Files\Unity\Hub\Editor\2019.4.41f1\Editor\Unity.exe" \
    -batchmode \
    -nographics \
    -projectPath "C:\path\to\your\unity\project" \
    -runTests \
    -testPlatform EditMode \
    -testResults "C:\output\test-results.xml" \
    -logFile "C:\output\unity-test.log"
```

**Parameter breakdown:**

| Parameter | Purpose |
|-----------|---------|
| `-batchmode` | Runs Unity without opening the editor UI |
| `-nographics` | Skips GPU initialization (essential for headless CI) |
| `-projectPath` | Absolute path to the Unity project root |
| `-runTests` | Tells Unity to execute tests and exit |
| `-testPlatform EditMode` | Run Edit Mode tests specifically |
| `-testResults` | Where to write the NUnit XML results file |
| `-logFile` | Where to write the Unity Editor log |

The test results XML file can be consumed by Jenkins (NUnit plugin), Azure DevOps, or any CI system that reads NUnit format.

**Jenkins integration example:**

```groovy
// In your Jenkinsfile
stage('Unit Tests') {
    steps {
        bat """
            "C:\\Program Files\\Unity\\Hub\\Editor\\2019.4.41f1\\Editor\\Unity.exe" ^
                -batchmode ^
                -nographics ^
                -projectPath "%WORKSPACE%" ^
                -runTests ^
                -testPlatform EditMode ^
                -testResults "%WORKSPACE%\\test-results.xml" ^
                -logFile "%WORKSPACE%\\unity-test.log"
        """
    }
    post {
        always {
            nunit testResultsPattern: 'test-results.xml'
        }
    }
}
```


## Common Pitfalls and Troubleshooting

### "No tests found in Test Runner"

- Make sure the `.asmdef` file is in the same folder as your test `.cs` files
- Check that the `.asmdef` has `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`
- Check that the `.asmdef` has `"includePlatforms": ["Editor"]`
- Verify that `com.unity.test-framework` is listed in your project's `Packages/manifest.json`
- Try closing and re-opening the Test Runner window

### "Assembly reference errors" or "type not found"

- Your test code references classes from another assembly but the `.asmdef` doesn't list that assembly in its `references` array
- Open the `.asmdef` in the Inspector, add the missing assembly reference, and let Unity recompile

### "Tests pass locally but fail in CI"

- Edit Mode tests should not depend on scene state, loaded assets, or user input
- If a test uses `AssetDatabase.LoadAssetAtPath`, make sure the asset exists in the project checked into source control
- Check that the Unity version in CI matches the version used locally (2019.4.41)

### "NullReferenceException in SetUp"

- Your SetUp method is probably trying to instantiate a MonoBehaviour with `new`. MonoBehaviours must be created with `new GameObject().AddComponent<T>()` instead
- Remember to clean up any GameObjects you create in TearDown using `Object.DestroyImmediate()`

### "Tests take a long time"

- Edit Mode tests should be very fast (milliseconds each). If they are slow, check for:
  - Accidental file I/O on large directories
  - Loading heavy assets in SetUp that could be loaded once in a `[OneTimeSetUp]` method
  - Nested loops with large datasets

### "Cannot test MonoBehaviour methods"

Edit Mode tests CAN test MonoBehaviours, but you need to create them on GameObjects:

```csharp
[SetUp]
public void SetUp()
{
    var go = new GameObject("TestObject");
    _myComponent = go.AddComponent<MyComponent>();
}

[TearDown]
public void TearDown()
{
    Object.DestroyImmediate(_myComponent.gameObject);
}
```

This creates a real component in the Editor without entering Play Mode.


## Required Package

Make sure your `Packages/manifest.json` includes the Unity Test Framework:

```json
{
    "dependencies": {
        "com.unity.test-framework": "1.1.31"
    }
}
```

If it is missing, add it and Unity will download it automatically.


## Next Steps

After getting these template tests running:

1. **Replace the stubs** with your actual classes (search for `TODO` in each file)
2. **Add more test files** following the same patterns for CameraNav, Labels, etc.
3. **Wire into Jenkins** using the command-line instructions above
4. **Set a coverage goal** -- even 20-30% coverage on critical paths catches most regressions
