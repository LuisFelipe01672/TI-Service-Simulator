using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

// Etapa 8: o rack de parede do cliente é criado em código (como o notebook: não está na cena montada, não precisa
// recalcular a luz) e vai para a parede atrás do roteador da sala do chamado, acima dele ou ao lado, num pedaço
// de parede livre (sem janela, quadro ou relógio). Os pontos de rede do projeto de cabeamento ficam na mesma
// parede, a 1–3 m do rack, com a canaleta subindo até 2,25 m e correndo até o rack.
// Coordenadas locais do rack: x ao longo da parede, y do chão, z = 0 na face da parede e -z para dentro da sala.
public static class RackCliente
{
    public const float Base = 1.42f;          // altura do fundo do rack
    public const float Largura = 0.57f, Altura = 0.62f, Profundidade = 0.45f;   // rack de parede 12U
    public const float AlturaCanaleta = 2.25f, AlturaTomada = 0.32f;

    static Equipamento atual;
    // Onde cabe um ponto de rede (x local, ao longo da parede); o visual lê e refaz as peças quando a versão muda
    public static readonly List<float> PontosLocais = new List<float>();
    public static int Versao { get; private set; }

    public static Equipamento Garantir()
    {
        if (atual != null) return atual;
        foreach (var e in Object.FindObjectsByType<Equipamento>())
            if (e.tipoEquipamento == TipoEquipamento.Rack) return atual = e;
        var go = new GameObject("Rack_Cliente");
        var eq = go.AddComponent<Equipamento>();
        eq.tipoEquipamento = TipoEquipamento.Rack;
        eq.distanciaInteracao = 2f;
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, Base + Altura / 2f, -Profundidade / 2f);
        bc.size = new Vector3(Largura, Altura, Profundidade);
        go.AddComponent<EquipamentoPecas>();
        return atual = eq;
    }

    public static Transform Transform => atual != null ? atual.transform : null;

    // Põe o rack na parede atrás do roteador desta sala
    public static void Posicionar(SalaCliente sala)
    {
        var rack = Garantir();
        Equipamento rot = null;
        foreach (var e in Object.FindObjectsByType<Equipamento>())
            if (e.tipoEquipamento == TipoEquipamento.Roteador) { rot = e; break; }
        Transform refT = rot != null ? rot.transform : sala != null ? sala.ancoraRoteador : null;
        if (refT == null) return;

        float chao = sala != null ? sala.Centro.y : 0f;
        Vector3 dir = refT.forward; dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
        dir.Normalize();

        // A parede atrás do roteador, na altura do rack
        Vector3 origem = new Vector3(refT.position.x, chao + 1.7f, refT.position.z) - dir * 0.2f;
        Vector3 naParede = origem + dir * 0.6f, normal = -dir;
        float melhor = float.MaxValue;
        foreach (var h in Physics.RaycastAll(origem, dir, 2.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Ignorar(h.collider, rack.transform) || Vector3.Dot(h.normal, dir) > -0.7f) continue;
            if (h.distance < melhor) { melhor = h.distance; naParede = h.point; normal = h.normal; }
        }
        Vector3 f = -normal; f.y = 0f; f.Normalize();               // para dentro da parede
        Vector3 direita = Vector3.Cross(Vector3.up, f);
        naParede.y = chao;

        var renderers = Object.FindObjectsByType<Renderer>();
        Vector3 pos = naParede;
        bool achou = false;
        foreach (float s in new[] { 0f, 0.45f, -0.45f, 0.8f, -0.8f, 1.2f, -1.2f, 1.6f, -1.6f })
        {
            Vector3 c = naParede + direita * s;
            if (!Parede(c + Vector3.up * 1.5f, f, rack.transform) || !Parede(c + Vector3.up * 2.0f, f, rack.transform) ||
                !Parede(c + Vector3.up * 1.75f + direita * 0.28f, f, rack.transform) || !Parede(c + Vector3.up * 1.75f - direita * 0.28f, f, rack.transform))
                continue;
            var area = new Bounds(c + Vector3.up * (Base + Altura / 2f) - f * (Profundidade / 2f), new Vector3(Largura + 0.1f, Altura + 0.08f, Profundidade));
            if (Ocupado(area, f, renderers, rack.transform)) continue;
            pos = c; achou = true; break;
        }
        if (!achou) Debug.LogWarning("[Rede] Rack: nenhum pedaço de parede livre perto do roteador; ficou atrás dele.");
        rack.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(f, Vector3.up));
        Physics.SyncTransforms();

        // Pontos de rede: na mesma parede, com a parede inteira de baixo até a canaleta e nada na frente
        PontosLocais.Clear();
        foreach (float d in new[] { 1.1f, -1.1f, 1.6f, -1.6f, 2.1f, -2.1f, 2.6f, -2.6f, 3.1f, -3.1f })
        {
            if (PontosLocais.Count >= 4) break;
            Vector3 p = pos + direita * d;
            if (!Parede(p + Vector3.up * AlturaTomada, f, rack.transform) || !Parede(p + Vector3.up * 1.2f, f, rack.transform) ||
                !Parede(p + Vector3.up * AlturaCanaleta, f, rack.transform)) continue;
            bool caminho = true;
            float passo = Mathf.Sign(d) * 0.3f;
            for (float x = Mathf.Sign(d) * 0.35f; Mathf.Abs(x) < Mathf.Abs(d); x += passo)
                if (!Parede(pos + direita * x + Vector3.up * AlturaCanaleta, f, rack.transform)) { caminho = false; break; }
            if (!caminho) continue;
            var caixa = new Bounds(p + Vector3.up * AlturaTomada - f * 0.15f, new Vector3(0.22f, 0.3f, 0.3f));
            var subida = new Bounds(p + Vector3.up * 1.35f - f * 0.03f, new Vector3(0.06f, 1.9f, 0.06f));
            var corrida = new Bounds(pos + direita * (d / 2f) + Vector3.up * AlturaCanaleta - f * 0.03f, new Vector3(0.06f, 0.06f, 0.06f));
            corrida.Encapsulate(pos + direita * d + Vector3.up * AlturaCanaleta);
            corrida.Encapsulate(pos + direita * (Mathf.Sign(d) * 0.3f) + Vector3.up * AlturaCanaleta);
            if (Ocupado(caixa, f, renderers, rack.transform) || Ocupado(subida, f, renderers, rack.transform) || Ocupado(corrida, f, renderers, rack.transform)) continue;
            PontosLocais.Add(d);
        }
        if (PontosLocais.Count < 4)
        {
            Debug.LogWarning($"[Rede] Só {PontosLocais.Count} lugar(es) livre(s) para ponto de rede nesta parede; o resto fica nos lugares padrão.");
            foreach (float d in new[] { 1.1f, -1.1f, 1.6f, -1.6f })
                if (PontosLocais.Count < 4 && !PontosLocais.Contains(d)) PontosLocais.Add(d);
        }
        Versao++;
        Debug.Log($"[Rede] Rack na sala {sala?.Nome} em {pos} (parede livre: {achou}); pontos em x = {string.Join(", ", PontosLocais)}");
    }

    // Onde fica (no mundo) a tomada do ponto de índice i
    public static Vector3 PosicaoPonto(int i)
    {
        var t = Transform;
        if (t == null) return Vector3.zero;
        float x = i >= 0 && i < PontosLocais.Count ? PontosLocais[i] : 1.1f;
        return t.TransformPoint(new Vector3(x, AlturaTomada, -0.03f));
    }

    static bool Ignorar(Collider c, Transform rack) =>
        c == null || c is CharacterController || c.CompareTag("Player") || c.transform.IsChildOf(rack) || c.GetComponentInParent<Equipamento>() != null;

    // Tem parede logo ali (a 0,5 m de quem olha de frente), sem janela nem porta?
    static bool Parede(Vector3 naParede, Vector3 f, Transform rack)
    {
        Vector3 o = naParede - f * 0.5f;
        float melhor = float.MaxValue;
        bool parede = false;
        foreach (var h in Physics.RaycastAll(o, f, 0.8f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Ignorar(h.collider, rack) || h.distance >= melhor) continue;
            melhor = h.distance;
            parede = Mathf.Abs(h.distance - 0.5f) < 0.07f && Vector3.Dot(h.normal, f) < -0.8f;
        }
        return parede;
    }

    // Algo na frente da parede nesse volume (quadro, relógio, móvel, planta)?
    static bool Ocupado(Bounds area, Vector3 f, Renderer[] renderers, Transform rack)
    {
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled || r.transform.IsChildOf(rack) || r.GetComponentInParent<Sujeira>() != null) continue;
            if (r.GetComponentInParent<PlayerController>() != null) continue;
            if (r is SkinnedMeshRenderer || r.GetComponentInParent<Animator>() != null) continue;   // gente (o contato fica perto do roteador)
            var b = r.bounds;
            if (b.size.y > 2.0f || (b.size.x > 3f || b.size.z > 3f)) continue;   // parede, chão, forro
            if (b.Intersects(area)) return true;
        }
        return false;
    }

    // ---------- Foto do antes/depois ----------

    // Fotografa o rack de frente (flash do celular) e devolve a imagem para a OS
    public static Texture2D Fotografar()
    {
        var t = Transform;
        if (t == null) return null;
        Vector3 centro = t.TransformPoint(new Vector3(0f, Base + Altura / 2f - 0.12f, -Profundidade / 2f));
        Vector3 de = centro - t.forward * 1.25f + Vector3.up * 0.05f;

        var luz = new GameObject("Flash").AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = 3f;
        luz.color = Color.white;
        luz.intensity = 0.8f;   // durante a foto: flash fraco (a câmera da foto não tem a correção de cor do jogo)
        luz.transform.position = de;

        var go = new GameObject("CameraFoto");
        var cam = go.AddComponent<Camera>();
        go.transform.position = de;
        go.transform.LookAt(centro);
        cam.fieldOfView = 50f;
        cam.nearClipPlane = 0.05f;
        cam.allowHDR = false;
        if (Camera.main != null) { cam.cullingMask = Camera.main.cullingMask; cam.backgroundColor = Camera.main.backgroundColor; }
        var rt = RenderTexture.GetTemporary(640, 480, 24);
        cam.targetTexture = rt;
        cam.Render();
        var antes = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(640, 480, TextureFormat.RGB24, false) { name = "FotoRack" };
        tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
        tex.Apply();
        RenderTexture.active = antes;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Object.Destroy(go);

        SomManager.TocarEm("clique", de, 0.7f, 2f);
        luz.intensity = 4f;   // o clarão que o técnico vê
        Tween.Custom(luz, 4f, 0f, 0.25f, (l, v) => l.intensity = v).OnComplete(luz, l => Object.Destroy(l.gameObject));
        return tex;
    }
}
