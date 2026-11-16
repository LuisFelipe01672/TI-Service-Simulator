using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Roda os testes EditMode e escreve o resumo no Console (dá para chamar pelo Temp/comando_unity.txt)
public static class RodarTestes
{
    [MenuItem("TI Service/Testes/Rodar testes automáticos", false, 400)]
    static void Rodar()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[Testes] Pare o Play antes de rodar os testes."); return; }
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Relatorio());
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
    }

    class Relatorio : ICallbacks
    {
        int falhas;
        public void RunStarted(ITestAdaptor testsToRun) { falhas = 0; Debug.Log("[Testes] Rodando..."); }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor r)
        {
            if (r.HasChildren || r.TestStatus != TestStatus.Failed) return;
            falhas++;
            Debug.LogError($"[Testes] FALHOU {r.Name}: {r.Message}");
        }
        public void RunFinished(ITestResultAdaptor r) =>
            Debug.Log($"[Testes] Fim: {r.PassCount} passaram, {r.FailCount} falharam, {r.SkipCount} pulados.");
    }
}
