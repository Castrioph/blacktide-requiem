using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public class RunEditModeTests
{
    private const string ResultPath = "test-results-s402a.txt";

    public static void Execute()
    {
        if (File.Exists(ResultPath))
            File.Delete(ResultPath);

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new ResultWriter());
        api.Execute(new ExecutionSettings(new Filter
        {
            testMode = TestMode.EditMode
        }));
        Debug.Log("[S4-02a] EditMode test run started via TestRunnerApi");
    }

    private class ResultWriter : ICallbacks
    {
        private readonly StringBuilder _failures = new StringBuilder();

        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RESULT: " + result.TestStatus);
            sb.AppendLine("PASSED: " + result.PassCount);
            sb.AppendLine("FAILED: " + result.FailCount);
            sb.AppendLine("SKIPPED: " + result.SkipCount);
            if (_failures.Length > 0)
            {
                sb.AppendLine("--- FAILURES ---");
                sb.Append(_failures);
            }
            File.WriteAllText(ResultPath, sb.ToString());
            Debug.Log("[S4-02a] EditMode test run finished: " + result.TestStatus);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (!result.Test.IsSuite && result.TestStatus == TestStatus.Failed)
            {
                _failures.AppendLine(result.FullName);
                _failures.AppendLine("  " + result.Message);
            }
        }
    }
}
