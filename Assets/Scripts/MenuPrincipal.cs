using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    void OnGUI()
    {
        float largura = 300f;
        float altura = 200f;
        float x = (Screen.width - largura) / 2f;
        float y = (Screen.height - altura) / 2f;

        GUI.Box(new Rect(x, y, largura, altura), "");

        GUI.Label(new Rect(x, y + 20, largura, 40),
            "<size=24><b>TI Service Simulator</b></size>");

        if (GUI.Button(new Rect(x + 75, y + 80, 150, 40), "Jogar"))
        {
            SceneManager.LoadScene("SampleScene");
        }

        if (GUI.Button(new Rect(x + 75, y + 135, 150, 40), "Sair"))
        {
            Application.Quit();
        }
    }
}
