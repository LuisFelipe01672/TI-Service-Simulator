using System;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tela de créditos (exigida pela licença CC-BY dos modelos). O texto vem de CREDITOS.md, copiado para
// Resources/Creditos.md pelo editor (SincronizarCreditos), e é convertido de Markdown para rich text.
public static class JanelaCreditos
{
    public static void Montar(RectTransform janela, Action aoVoltar)
    {
        UIFactory.Texto(janela, "Créditos", 40, UIFactory.CorTexto, FontStyles.Bold, TextAlignmentOptions.Center);

        var lista = UIFactory.ListaComRolagem(janela, "ListaCreditos");
        var le = lista.parent.parent.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 560;
        le.minHeight = 560;

        UIFactory.Texto(lista, "TI Service Simulator usa os trabalhos abaixo. Obrigado aos autores!",
            20, UIFactory.CorSuave, FontStyles.Italic);

        var arquivo = Resources.Load<TextAsset>("Creditos");
        var corpo = UIFactory.Texto(lista, arquivo != null ? Converter(arquivo.text) : "Arquivo de créditos não encontrado.",
            20, UIFactory.CorTexto);
        corpo.richText = true;

        var espaco = UIFactory.Vazio(janela, "Espaco");
        espaco.gameObject.AddComponent<LayoutElement>().preferredHeight = 6;
        UIFactory.Botao(janela, "Voltar", aoVoltar, UIFactory.Destaque, 300, 54);
    }

    // Markdown simples → TextMeshPro: títulos, listas, tabelas e links (fica só o texto do link)
    public static string Converter(string md)
    {
        var sb = new StringBuilder();
        foreach (var bruta in md.Replace("\r", "").Split('\n'))
        {
            var linha = bruta.Trim();
            if (linha.Length == 0) { sb.AppendLine(); continue; }

            if (linha.StartsWith("# ")) continue; // o título da janela já diz "Créditos"
            if (linha.StartsWith("## "))
            {
                sb.AppendLine($"<size=24><b><color=#3FA9F5>{Limpar(linha.Substring(3))}</color></b></size>");
                continue;
            }
            if (linha.StartsWith("|"))
            {
                var cols = linha.Trim('|').Split('|');
                if (cols.Length < 4 || cols[0].Contains("---") || cols[0].Contains("Arquivo")) continue;
                string modelo = Limpar(cols[1]), autor = Limpar(cols[2]), alt = Limpar(cols[3]);
                sb.Append($"• <b>{modelo}</b> — por {autor}");
                if (alt != "—" && alt.Length > 0) sb.Append($" <color=#9AA6B8>({alt})</color>");
                sb.AppendLine();
                continue;
            }
            if (linha.StartsWith("- ")) { sb.AppendLine("• " + Limpar(linha.Substring(2))); continue; }
            sb.AppendLine(Limpar(linha));
        }
        return sb.ToString().Trim();
    }

    static string Limpar(string s)
    {
        s = Regex.Replace(s, @"\[([^\]]+)\]\([^)]+\)", "$1");   // [texto](url) → texto
        s = Regex.Replace(s, @"\*\*([^*]+)\*\*", "<b>$1</b>");  // **negrito**
        s = s.Replace("`", "");
        return s.Trim();
    }
}
