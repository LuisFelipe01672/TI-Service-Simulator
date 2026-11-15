using PrimeTween;
using UnityEngine;

// Testador de cabo na mão do técnico: aparece na frente da câmera a cada teste e acende as luzes 1 a 8
// em sequência, como o aparelho de verdade. Fio que não passa sinal fica apagado (a pista está nas luzes).
public partial class EquipamentoPecas
{
    static EstadoEquipamentos testadorEstado;
    static int testadorVistos;
    static Transform testador;
    static Renderer[] testadorLeds;
    static float testadorInicio;
    static DefeitoCabo testadorDefeito;
    static Material mTestador, mTestadorTela, mLedOff;
    const float TestadorPasso = 0.3f, TestadorDura = 6f;

    // Chamado por todos os equipamentos, mas só existe um testador (estado estático)
    static void AtualizarTestador(EstadoEquipamentos e)
    {
        if (e != testadorEstado)
        {
            testadorEstado = e;
            testadorVistos = e != null ? e.testesDeCabo : 0;
        }
        var cam = Camera.main;
        if (e != null && cam != null && e.testesDeCabo > testadorVistos)
        {
            testadorVistos = e.testesDeCabo;
            testadorDefeito = e.ultimoTesteCabo;
            MostrarTestador(cam);
        }
        if (testador == null) return;

        float t = Time.time - testadorInicio - 0.6f;   // espera o aparelho chegar na mão
        int passo = t < 0f ? -1 : Mathf.FloorToInt(t / TestadorPasso) % 8;
        for (int i = 0; i < 8; i++)
        {
            bool passa = testadorDefeito == DefeitoCabo.Nenhum || (testadorDefeito == DefeitoCabo.Conector && i != 2 && i != 5);
            Pintar(testadorLeds[i], i == passo && passa ? mVerde : mLedOff);
            // a unidade remota (na outra ponta) acende junto
            Pintar(testadorLeds[8 + i], i == passo && passa ? mVerde : mLedOff);
        }
        if (Time.time - testadorInicio > TestadorDura)
        {
            var x = testador;
            testador = null;
            Tween.LocalPosition(x, x.localPosition + new Vector3(0f, -0.5f, 0f), 0.4f, Ease.InCubic).OnComplete(x, o => Destroy(o.gameObject));
        }
    }

    static void MostrarTestador(Camera cam)
    {
        if (testador != null) Destroy(testador.gameObject);
        mTestador ??= Mat(new Color(0.95f, 0.75f, 0.1f));
        mTestadorTela ??= Mat(new Color(0.08f, 0.08f, 0.09f));
        mLedOff ??= Mat(new Color(0.3f, 0.36f, 0.3f));
        var mCabo = mCaboAzul != null ? mCaboAzul : mTestadorTela;
        var mPonta = mPlugue != null ? mPlugue : mTestadorTela;
        testador = new GameObject("TestadorDeCabo").transform;
        testador.SetParent(cam.transform, false);
        testadorLeds = new Renderer[16];
        // unidade principal (esquerda) e unidade remota (direita), ligadas pelo cabo que está sendo testado
        for (int u = 0; u < 2; u++)
        {
            float x = u == 0 ? -0.07f : 0.07f;
            Cubo(testador, u == 0 ? "Principal" : "Remoto", new Vector3(x, 0f, 0f), new Vector3(0.06f, u == 0 ? 0.13f : 0.1f, 0.025f), mTestador);
            Cubo(testador, "Painel", new Vector3(x, 0f, -0.013f), new Vector3(0.022f, 0.1f, 0.002f), mTestadorTela);
            for (int i = 0; i < 8; i++)
                testadorLeds[u * 8 + i] = Cubo(testador, "Led" + (i + 1), new Vector3(x, 0.042f - i * 0.012f, -0.015f),
                    new Vector3(0.014f, 0.008f, 0.002f), mLedOff).GetComponent<Renderer>();
            Cubo(testador, "Plugue", new Vector3(x, u == 0 ? 0.07f : 0.055f, 0f), new Vector3(0.014f, 0.012f, 0.014f), mPonta);
        }
        Cubo(testador, "Cabo", new Vector3(0f, 0.085f, 0f), new Vector3(0.146f, 0.006f, 0.006f), mCabo);
        Cubo(testador, "CaboE", new Vector3(-0.07f, 0.08f, 0f), new Vector3(0.006f, 0.014f, 0.006f), mCabo);
        Cubo(testador, "CaboD", new Vector3(0.07f, 0.072f, 0f), new Vector3(0.006f, 0.03f, 0.006f), mCabo);
        testador.localPosition = new Vector3(0f, -0.5f, 0.42f);
        testador.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        Tween.LocalPosition(testador, new Vector3(0f, -0.08f, 0.42f), 0.55f, Ease.OutCubic);
        testadorInicio = Time.time;
        SomManager.TocarEm("mexer", cam.transform.position, 0.6f, 1.2f);
    }
}
