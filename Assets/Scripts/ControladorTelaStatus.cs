using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ControladorTelaStatus : MonoBehaviour
{
    // 🆕 O método agora está NO LUGAR CERTO: dentro da classe!
    public void ConfigurarGerenciadorManualmente(GerenciadorDeJogo gerenciadorInstancia)
    {
        gerenciador = gerenciadorInstancia;
    }

    [Header("Referências do Gerenciador")]
    [SerializeField] private GerenciadorDeJogo gerenciador;

    [Header("UI de Oxigênio")]
    public Image fillOxigenio;
    public TextMeshProUGUI textoOxigenio;

    [Header("UI de Energia")]
    public Image fillEnergia;
    public TextMeshProUGUI textoEnergia;

    [Header("UI de Casco")]
    public Image fillCasco;
    public TextMeshProUGUI textoCasco;

    [Header("UI de Suprimentos")]
    public Image fillSuprimentos;
    public TextMeshProUGUI textoSuprimentos;

    private void Awake()
    {
        if (gerenciador == null)
        {
            gerenciador = FindFirstObjectByType<GerenciadorDeJogo>();
        }
    }

    private void OnEnable()
    {
        // 🆕 Mudamos para uma Coroutine para dar tempo da Unity processar os componentes ativos
        StartCoroutine(AguardarEAtualizar());
    }

    IEnumerator AguardarEAtualizar()
    {
        yield return new WaitForSecondsRealtime(0.05f);
        AtualizarInterface();
    }

    public void AtivarPainel()
    {
        gameObject.SetActive(true);
        StartCoroutine(AguardarEAtualizar());
    }

    public void AtualizarInterface()
    {
        if (gerenciador == null) 
        {
            Debug.LogError("🚨 ControladorTelaStatus: O Gerenciador de Jogo não foi encontrado na cena!");
            return;
        }

        // Força o preenchimento exato lendo as variáveis
        AtualizarBarra(fillOxigenio, textoOxigenio, gerenciador.oxigenio, "Oxigênio");
        AtualizarBarra(fillEnergia, textoEnergia, gerenciador.energiaEletrica, "Energia");
        AtualizarBarra(fillCasco, textoCasco, gerenciador.integridadeDoCasco, "Casco");
        AtualizarBarra(fillSuprimentos, textoSuprimentos, gerenciador.suprimentos, "Suprimentos");
    }

    private void AtualizarBarra(Image imagemBarra, TextMeshProUGUI campoTexto, int valorAtual, string nomeRecurso)
    {
        if (imagemBarra != null)
        {
            // Força a conversão garantindo que números inteiros virem float corretamente
            float proporcao = (float)valorAtual / 100f;
            
            // Força o clamp para garantir que fique entre 0 e 1
            imagemBarra.fillAmount = Mathf.Clamp01(proporcao);
        }

        if (campoTexto != null)
        {
            campoTexto.text = $"{valorAtual}%";
        }
    }
}