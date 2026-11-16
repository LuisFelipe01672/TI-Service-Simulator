using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// Close de câmera numa peça do equipamento (Etapa 2.4): ao "olhar" uma peça, a câmera desliza até ela,
// fecha o campo de visão e desfoca o fundo (profundidade de campo só nos closes). A pista fica na peça
// (visor, LEDs, poeira); E, Esc, espaço, andar, clicar ou mexer o mouse voltam para o jogador.
public class CloseDePeca : MonoBehaviour
{
    public static bool Ativo { get; private set; }
    public static string Legenda { get; private set; }

    static CloseDePeca inst;

    Camera cam;
    PlayerController controle;
    Vector3 posLocal;
    Quaternion rotLocal;
    float fovOriginal;
    PostProcessVolume volumeDof;
    DepthOfField dof;
    float liberaEm;
    bool voltando;

    const float Fov = 42f, Ida = 0.45f, Volta = 0.35f;

    // 'peca' = colisor da peça mirada; 'legenda' = o que aparece embaixo durante o close
    public static void Abrir(PecaEquipamento peca, string legenda)
    {
        if (peca == null || Ativo) return;
        var jogador = GameObject.FindWithTag("Player");
        if (jogador == null) return;
        if (inst == null) inst = new GameObject("CloseDePeca").AddComponent<CloseDePeca>();
        inst.Comecar(jogador, peca, legenda);
    }

    void Comecar(GameObject jogador, PecaEquipamento peca, string legenda)
    {
        cam = jogador.GetComponentInChildren<Camera>();
        controle = jogador.GetComponent<PlayerController>();
        if (cam == null) return;

        var col = peca.GetComponent<Collider>();
        Bounds b = col != null ? col.bounds : new Bounds(peca.transform.position, Vector3.one * 0.2f);
        Vector3 alvo = b.center;

        // Vem do lado de onde o jogador está olhando, um pouco de cima (como quem se abaixa para ver)
        Vector3 dir = cam.transform.position - alvo;
        dir.y = Mathf.Max(dir.y, 0.15f * dir.magnitude);
        dir.Normalize();
        float dist = Mathf.Clamp(b.extents.magnitude * 2.8f, 0.32f, 0.85f);

        // Não atravessa parede nem móvel: para antes do que estiver no caminho (fora o próprio equipamento)
        var hits = Physics.RaycastAll(alvo, dir, dist + 0.1f, ~0, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<Equipamento>() != null || h.collider.CompareTag("Player") || h.collider is CharacterController) continue;
            dist = Mathf.Min(dist, Mathf.Max(0.2f, h.distance - 0.06f));
        }
        Vector3 pos = alvo + dir * dist;
        Quaternion rot = Quaternion.LookRotation(alvo - pos);

        Ativo = true;
        voltando = false;
        Legenda = legenda;
        liberaEm = Time.time + Ida + 0.15f;
        posLocal = cam.transform.localPosition;
        rotLocal = cam.transform.localRotation;
        fovOriginal = cam.fieldOfView;
        if (controle != null) controle.enabled = false;

        Tween.StopAll(cam.transform);
        var seq = Sequence.Create().Group(Tween.Position(cam.transform, pos, Ida, Ease.InOutSine));
        if (Quaternion.Angle(cam.transform.rotation, rot) > 0.01f) seq.Group(Tween.Rotation(cam.transform, rot, Ida, Ease.InOutSine));
        if (Mathf.Abs(cam.fieldOfView - Fov) > 0.01f) seq.Group(Tween.CameraFieldOfView(cam, Fov, Ida, Ease.InOutSine));
        // Foco na face da peça que está de frente (não no centro: numa peça grande o centro fica atrás)
        LigarDesfoque(Mathf.Max(0.15f, Vector3.Distance(pos, b.ClosestPoint(pos))));
    }

    void LigarDesfoque(float distancia)
    {
        if (cam.GetComponent<PostProcessLayer>() == null || PostProcessManager.instance == null) return;
        if (dof == null)
        {
            dof = ScriptableObject.CreateInstance<DepthOfField>();
            dof.enabled.Override(true);
            dof.kernelSize.Override(KernelSize.Medium);
        }
        dof.focusDistance.Override(distancia);
        dof.aperture.Override(5.6f);    // fundo desfocado, peça inteira nítida
        dof.focalLength.Override(50f);
        if (volumeDof == null)
        {
            volumeDof = PostProcessManager.instance.QuickVolume(0, 50f, dof);   // camada Default, acima do volume do cenário
            volumeDof.transform.SetParent(transform, false);
        }
        volumeDof.weight = 0f;
        Tween.Custom(0f, 1f, Ida, v => { if (volumeDof != null) volumeDof.weight = v; });
    }

    void Update()
    {
        if (!Ativo || voltando || cam == null) return;
        bool aberto = GameUI.Instance != null && GameUI.Instance.PainelAberto;
        if (Time.time < liberaEm && !aberto) return;
        bool mexeu = Mathf.Abs(Input.GetAxis("Mouse X")) + Mathf.Abs(Input.GetAxis("Mouse Y")) > 1.2f;
        bool tecla = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)
                     || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)
                     || Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        if (aberto || mexeu || tecla) Voltar();
    }

    public static void Fechar() { if (inst != null && Ativo) inst.Voltar(); }

    void Voltar()
    {
        voltando = true;
        Tween.StopAll(cam.transform);
        var seq = Sequence.Create();
        // (só anima o que mudou: o PrimeTween avisa quando o valor final já é o atual)
        if ((cam.transform.localPosition - posLocal).sqrMagnitude > 1e-8f) seq.Group(Tween.LocalPosition(cam.transform, posLocal, Volta, Ease.InOutSine));
        if (Quaternion.Angle(cam.transform.localRotation, rotLocal) > 0.01f) seq.Group(Tween.LocalRotation(cam.transform, rotLocal, Volta, Ease.InOutSine));
        if (Mathf.Abs(cam.fieldOfView - fovOriginal) > 0.01f) seq.Group(Tween.CameraFieldOfView(cam, fovOriginal, Volta, Ease.InOutSine));
        seq.ChainDelay(0.01f)
            .ChainCallback(() =>
            {
                Ativo = false;
                voltando = false;
                Legenda = null;
                if (controle != null) controle.enabled = true;
            });
        if (volumeDof != null && volumeDof.weight > 0.001f) Tween.Custom(volumeDof.weight, 0f, Volta, v => { if (volumeDof != null) volumeDof.weight = v; });
    }

    void OnDestroy()
    {
        if (volumeDof != null) RuntimeUtilities.DestroyVolume(volumeDof, true, true);
        if (inst == this) { inst = null; Ativo = false; }
    }
}
