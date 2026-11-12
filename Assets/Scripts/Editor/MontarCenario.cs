using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Monta o cenário da Fase 1 com os modelos do Kenney Furniture Kit (CC0):
// o escritório inicial do jogador e o escritório de um cliente, com os equipamentos separados.
// Pode ser rodado de novo: apaga o "Cenario" antigo e monta outro.
public static class MontarCenario
{
    const string PastaModelos = "Assets/Models/Kenney/Furniture/";
    const string PastaMateriais = "Assets/Materials/Cenario";
    const float Escala = 2f;   // o kit do Kenney é pequeno (mesa com 38 cm): 2x deixa no tamanho do jogador
    const float Placa = 2f;    // cada peça de piso/parede ocupa 2 m depois da escala

    enum Colisao { Nenhuma, Caixa, Malha }

    static float correcaoFbx = 1f;
    static Transform raiz;

    [MenuItem("TI Service/3. Montar cenário (escritório + cliente)")]
    public static void Montar()
    {
        if (GameObject.FindWithTag("Player") == null || Object.FindAnyObjectByType<MissionManager>() == null)
        {
            EditorUtility.DisplayDialog("Montar cenário", "Abra a SampleScene antes de montar o cenário.", "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PastaModelos + "wall.fbx") == null)
        {
            EditorUtility.DisplayDialog("Montar cenário", "Modelos do Kenney não encontrados em " + PastaModelos, "OK");
            return;
        }

        CalibrarEscalaFbx();

        var antigo = GameObject.Find("Cenario");
        if (antigo != null) Undo.DestroyObjectImmediate(antigo);
        raiz = new GameObject("Cenario").transform;
        Undo.RegisterCreatedObjectUndo(raiz.gameObject, "Montar cenário");

        // Esconde o cenário antigo (não apaga, para poder voltar se quiser)
        foreach (var nome in new[] { "Parede_Frente", "Parede_Tras", "Parede_Esquerda", "Parede_Direita", "Mesa", "Pé_Mesa" })
        {
            var go = GameObject.Find(nome);
            if (go != null) { Undo.RecordObject(go, "Esconder cenário antigo"); go.SetActive(false); }
        }

        var spawn = MontarEscritorio();
        MontarCliente(spawn);
        AjustarJogador(spawn);

        var cena = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(cena);
        EditorSceneManager.SaveScene(cena);
        AssetDatabase.SaveAssets();
        Debug.Log("[Cenário] Escritório e cliente montados. Cena salva.");
    }

    // ---------------- Escritório do jogador ----------------

    static Transform MontarEscritorio()
    {
        // Quartinho de 6 x 6 m com porta ao sul e janelas ao norte
        var sala = Sala("Escritorio", new Vector3(0, 0, 0), 3, 3, portaSul: 1, janelasNorte: new[] { 0, 2 }, janelaLateral: true);

        Modelo("rugRectangle", sala, new Vector3(0, 0.012f, 0.2f), 0, Colisao.Nenhuma);
        var mesa = Modelo("desk", sala, new Vector3(0, 0, 2.35f), 180);
        float alturaMesa = Altura(mesa);
        Modelo("chairDesk", sala, new Vector3(0, 0, 1.45f), 180);
        Modelo("plantSmall1", sala, new Vector3(0.55f, alturaMesa, 2.45f), 0, Colisao.Nenhuma);
        Modelo("trashcan", sala, new Vector3(1.05f, 0, 2.5f), 0);

        PosicionarEquipamento("Notebook", sala, new Vector3(-0.1f, alturaMesa, 2.3f), 180,
            v => Modelo("laptop", v, Vector3.zero, 0, Colisao.Nenhuma));

        var estante = Modelo("bookcaseOpen", sala, new Vector3(-2.7f, 0, 0.6f), 90);
        Modelo("books", sala, new Vector3(-2.7f, Altura(estante), 0.6f), 90, Colisao.Nenhuma);
        Modelo("lampSquareFloor", sala, new Vector3(-2.5f, 0, 2.5f), 0);

        var caixa = Modelo("cardboardBoxClosed", sala, new Vector3(2.45f, 0, 2.45f), 10);
        Modelo("cardboardBoxClosed", sala, new Vector3(2.4f, Altura(caixa), 2.45f), -15);
        Modelo("cardboardBoxOpen", sala, new Vector3(1.85f, 0, 2.55f), 25);

        var aparador = Modelo("sideTable", sala, new Vector3(2.7f, 0, 0f), -90);
        Modelo("kitchenCoffeeMachine", sala, new Vector3(2.7f, Altura(aparador), 0f), -90, Colisao.Nenhuma);
        Modelo("pottedPlant", sala, new Vector3(2.45f, 0, -2.45f), 0);
        Modelo("coatRackStanding", sala, new Vector3(-2.45f, 0, -2.45f), 0);

        return Ponto("SpawnEscritorio", sala, new Vector3(0, 1f, -1.2f), 0);
    }

    // ---------------- Escritório do cliente ----------------

    static void MontarCliente(Transform spawnEscritorio)
    {
        // Escritório de 10 x 8 m longe do quartinho, porta ao sul
        var sala = Sala("Cliente", new Vector3(40, 0, 0), 5, 4, portaSul: 2, janelasNorte: new[] { 1, 3 }, janelaLateral: true);

        Modelo("rugRectangle", sala, new Vector3(0, 0.012f, 0.5f), 90, Colisao.Nenhuma);

        // Estação 1: o Computador do chamado
        var mesa1 = Modelo("desk", sala, new Vector3(-2.8f, 0, 3.35f), 180);
        float h = Altura(mesa1);
        Modelo("chairDesk", sala, new Vector3(-2.8f, 0, 2.45f), 180);
        PosicionarEquipamento("Computador", sala, new Vector3(-2.8f, h, 3.45f), 180,
            v => Modelo("computerScreen", v, Vector3.zero, 0, Colisao.Nenhuma));
        Modelo("computerKeyboard", sala, new Vector3(-2.8f, h, 3.05f), 180, Colisao.Nenhuma);
        Modelo("computerMouse", sala, new Vector3(-2.35f, h, 3.05f), 180, Colisao.Nenhuma);

        // Estação 2: só decoração
        var mesa2 = Modelo("desk", sala, new Vector3(1.2f, 0, 3.35f), 180);
        Modelo("chairDesk", sala, new Vector3(1.2f, 0, 2.45f), 200);
        Modelo("computerScreen", sala, new Vector3(1.2f, Altura(mesa2), 3.45f), 180, Colisao.Nenhuma);
        Modelo("computerKeyboard", sala, new Vector3(1.2f, Altura(mesa2), 3.05f), 180, Colisao.Nenhuma);

        // Impressora em cima de um armário, na parede leste
        var armario = Modelo("kitchenCabinet", sala, new Vector3(4.5f, 0, 0.8f), -90);
        PosicionarEquipamento("Impressora", sala, new Vector3(4.45f, Altura(armario), 0.8f), -90, VisualImpressora);

        // Roteador em cima de um aparador, na parede oeste
        var aparador = Modelo("sideTable", sala, new Vector3(-4.55f, 0, -1.6f), 90);
        PosicionarEquipamento("Roteador", sala, new Vector3(-4.5f, Altura(aparador), -1.6f), 90, VisualRoteador);

        // Decoração
        Modelo("bookcaseClosedWide", sala, new Vector3(-4.6f, 0, 1.4f), 90);
        Modelo("loungeSofa", sala, new Vector3(2.6f, 0, -3.45f), 0);
        Modelo("tableCoffee", sala, new Vector3(2.6f, 0, -2.4f), 0);
        Modelo("pottedPlant", sala, new Vector3(4.45f, 0, -3.45f), 0);
        Modelo("pottedPlant", sala, new Vector3(-4.45f, 0, 3.45f), 0);
        Modelo("lampSquareFloor", sala, new Vector3(4.5f, 0, 3.5f), 0);
        Modelo("trashcan", sala, new Vector3(-1.5f, 0, 3.5f), 0);
        Modelo("coatRackStanding", sala, new Vector3(-4.45f, 0, -3.45f), 0);

        // Onde o jogador chega ao aceitar um chamado (objeto já usado pelo MissionManager)
        var chegada = GameObject.Find("ClienteLocation");
        if (chegada != null)
        {
            Undo.RecordObject(chegada.transform, "Mover chegada");
            chegada.transform.position = sala.TransformPoint(new Vector3(0, 1f, -2.6f));
            chegada.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Porta de volta para o escritório
        var porta = new GameObject("Porta_VoltarEscritorio");
        porta.transform.SetParent(sala, false);
        porta.transform.localPosition = new Vector3(0, 1f, -3.75f);
        var tp = porta.AddComponent<Teleporte>();
        tp.destino = spawnEscritorio;
        tp.rotulo = "Voltar ao escritório";
        tp.distanciaInteracao = 1.8f;
    }

    // ---------------- Equipamentos montados com formas simples ----------------

    static void VisualImpressora(Transform v)
    {
        var corpo = Mat("Impressora_Corpo", new Color(0.86f, 0.87f, 0.89f), 0.35f);
        var escuro = Mat("Impressora_Escuro", new Color(0.18f, 0.2f, 0.23f), 0.4f);
        var papel = Mat("Papel", Color.white, 0.1f);
        var tela = Mat("LED_Verde", new Color(0.2f, 0.9f, 0.45f), 0.6f);
        Cubo(v, "Corpo", new Vector3(0, 0.15f, 0), new Vector3(0.52f, 0.3f, 0.42f), corpo);
        Cubo(v, "Tampa", new Vector3(0, 0.31f, 0.05f), new Vector3(0.52f, 0.02f, 0.32f), escuro);
        Cubo(v, "PapelEntrada", new Vector3(0, 0.36f, 0.14f), new Vector3(0.3f, 0.08f, 0.02f), papel);
        Cubo(v, "Bandeja", new Vector3(0, 0.12f, -0.27f), new Vector3(0.36f, 0.015f, 0.14f), escuro);
        Cubo(v, "PapelSaida", new Vector3(0, 0.13f, -0.27f), new Vector3(0.28f, 0.01f, 0.12f), papel);
        Cubo(v, "Painel", new Vector3(0.18f, 0.305f, -0.14f), new Vector3(0.1f, 0.012f, 0.06f), tela);
    }

    static void VisualRoteador(Transform v)
    {
        var preto = Mat("Roteador_Preto", new Color(0.08f, 0.09f, 0.11f), 0.55f);
        var led = Mat("LED_Verde", new Color(0.2f, 0.9f, 0.45f), 0.6f);
        Cubo(v, "Corpo", new Vector3(0, 0.035f, 0), new Vector3(0.34f, 0.07f, 0.22f), preto);
        Cubo(v, "AntenaEsq", new Vector3(-0.13f, 0.19f, 0.09f), new Vector3(0.02f, 0.26f, 0.02f), preto);
        Cubo(v, "AntenaDir", new Vector3(0.13f, 0.19f, 0.09f), new Vector3(0.02f, 0.26f, 0.02f), preto);
        for (int i = 0; i < 4; i++)
            Cubo(v, "LED" + i, new Vector3(-0.09f + i * 0.06f, 0.045f, -0.112f), new Vector3(0.022f, 0.012f, 0.006f), led);
    }

    // ---------------- Peças de construção ----------------

    // Sala de nx por nz placas (2 m cada), com piso, quatro paredes, porta ao sul e janelas
    static Transform Sala(string nome, Vector3 centro, int nx, int nz, int portaSul, int[] janelasNorte, bool janelaLateral)
    {
        var sala = new GameObject(nome).transform;
        sala.SetParent(raiz, false);
        sala.position = centro;

        float x0 = -nx * Placa / 2f, z0 = -nz * Placa / 2f;
        var janelas = new HashSet<int>(janelasNorte ?? Array.Empty<int>());

        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
                Modelo("floorFull", sala, new Vector3(x0 + Placa * (i + 0.5f), -0.09f, z0 + Placa * (j + 0.5f)), 0, Colisao.Nenhuma);

        for (int i = 0; i < nx; i++)
        {
            float x = x0 + Placa * (i + 0.5f);
            Modelo(janelas.Contains(i) ? "wallWindow" : "wall", sala, new Vector3(x, 0, -z0), 0, Colisao.Malha);
            Modelo(i == portaSul ? "wallDoorway" : "wall", sala, new Vector3(x, 0, z0), 180, Colisao.Malha);
        }
        for (int j = 0; j < nz; j++)
        {
            float z = z0 + Placa * (j + 0.5f);
            bool janela = janelaLateral && j == nz / 2;
            Modelo(janela ? "wallWindow" : "wall", sala, new Vector3(-x0, 0, z), 90, Colisao.Malha);
            Modelo(janela ? "wallWindow" : "wall", sala, new Vector3(x0, 0, z), -90, Colisao.Malha);
        }
        return sala;
    }

    // Coloca um modelo do Kenney com a base apoiada em 'pos' (coordenadas locais do pai) e centralizado
    static GameObject Modelo(string nome, Transform pai, Vector3 pos, float rotY = 0, Colisao colisao = Colisao.Caixa)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PastaModelos + nome + ".fbx");
        if (prefab == null) { Debug.LogError("[Cenário] Modelo não encontrado: " + nome); return null; }

        var suporte = new GameObject(nome).transform;  // começa na origem para medir o modelo
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        modelo.transform.SetParent(suporte, false);
        modelo.transform.localScale *= Escala * correcaoFbx;

        var b = Limites(modelo);
        modelo.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);

        if (colisao == Colisao.Caixa)
        {
            var bc = suporte.gameObject.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, b.size.y / 2f, 0);
            bc.size = b.size;
        }
        else if (colisao == Colisao.Malha)
        {
            foreach (var mf in modelo.GetComponentsInChildren<MeshFilter>())
                mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        suporte.SetParent(pai, false);
        suporte.localPosition = pos;
        suporte.localRotation = Quaternion.Euler(0, rotY, 0);
        return suporte.gameObject;
    }

    // Move um objeto que já existe na cena (com Equipamento/Notebook) e troca o cubo pelo visual novo
    static void PosicionarEquipamento(string nome, Transform pai, Vector3 posLocal, float rotY, Action<Transform> montarVisual)
    {
        var go = GameObject.Find(nome);
        if (go == null) { Debug.LogWarning("[Cenário] Objeto não encontrado na cena: " + nome); return; }
        Undo.RecordObject(go.transform, "Posicionar " + nome);

        var t = go.transform;
        t.position = Vector3.zero;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) { Undo.RecordObject(mr, "Esconder cubo"); mr.enabled = false; }

        var antigo = t.Find("Visual");
        if (antigo != null) Object.DestroyImmediate(antigo.gameObject);
        var visual = new GameObject("Visual").transform;
        visual.SetParent(t, false);
        montarVisual(visual);

        var b = Limites(visual.gameObject);
        var bc = go.GetComponent<BoxCollider>();
        if (bc == null) bc = Undo.AddComponent<BoxCollider>(go);
        Undo.RecordObject(bc, "Ajustar colisor");
        bc.center = b.center;
        bc.size = b.size;

        t.position = pai.TransformPoint(posLocal);
        t.rotation = Quaternion.Euler(0, pai.eulerAngles.y + rotY, 0);
    }

    static Transform Ponto(string nome, Transform pai, Vector3 posLocal, float rotY)
    {
        var p = new GameObject(nome).transform;
        p.SetParent(pai, false);
        p.localPosition = posLocal;
        p.localRotation = Quaternion.Euler(0, rotY, 0);
        return p;
    }

    static void AjustarJogador(Transform spawn)
    {
        var jogador = GameObject.FindWithTag("Player");
        Undo.RecordObject(jogador.transform, "Mover jogador");
        jogador.transform.position = spawn.position;
        jogador.transform.rotation = spawn.rotation;

        var cc = jogador.GetComponent<CharacterController>();
        if (cc != null)
        {
            Undo.RecordObject(cc, "Ajustar jogador");
            cc.radius = 0.35f; // cabe entre os móveis
        }
    }

    static GameObject Cubo(Transform pai, string nome, Vector3 posLocal, Vector3 tamanho, Material mat)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = nome;
        Object.DestroyImmediate(c.GetComponent<Collider>());
        c.transform.SetParent(pai, false);
        c.transform.localPosition = posLocal;
        c.transform.localScale = tamanho;
        c.GetComponent<Renderer>().sharedMaterial = mat;
        return c;
    }

    static Material Mat(string nome, Color cor, float brilho)
    {
        if (!AssetDatabase.IsValidFolder(PastaMateriais))
            AssetDatabase.CreateFolder("Assets/Materials", "Cenario");
        string caminho = PastaMateriais + "/" + nome + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
        if (m == null)
        {
            m = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, caminho);
        }
        m.color = cor;
        m.SetFloat("_Glossiness", brilho);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Os FBX podem vir em centímetros ou metros: mede a parede (1 unidade de largura) e corrige
    static void CalibrarEscalaFbx()
    {
        correcaoFbx = 1f;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PastaModelos + "wall.fbx");
        var tmp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var b = Limites(tmp);
        float largura = Mathf.Max(b.size.x, b.size.z);
        Object.DestroyImmediate(tmp);
        if (largura > 0.0001f) correcaoFbx = 1f / largura;
        Debug.Log($"[Cenário] Largura da parede no FBX: {largura:F3} → correção {correcaoFbx:F3}");
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static float Altura(GameObject go) => go == null ? 0f : Limites(go).max.y - go.transform.position.y;
}
