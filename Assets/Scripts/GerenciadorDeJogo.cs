using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GerenciadorDeJogo : MonoBehaviour
{
    public enum EstadoDoJogo { Menu, Introducao, Gameplay, EventoAtivo, GameOver }
    
    [Header("Controle de Fluxo")]
    public EstadoDoJogo estadoAtual;
    public int diaAtual = 1;
    
    private int problemaAtualID;
    private int varianteAtualID;
    private int paginaIntroducaoAtual = 0;
    private string[] paginasIntroducao = new string[5];

    [Header("Componentes de Interface (UI)")]
    public TextMeshProUGUI componenteTextoIntroducao; // Texto exclusivo para a introdução (Tela Preta)
    public TextMeshProUGUI componenteTextoDoMonitor;  // Texto exclusivo para os Eventos Diários (Monitor Azul)
    [TextArea(5, 10)] public string textoExibidoNoMonitor;
    
    [Header("🆕 Textos das Opções Resumidas (Botões)")]
    public TextMeshProUGUI textoBotaoOpcao1;          // Componente de texto do Botão 1
    public TextMeshProUGUI textoBotaoOpcao2;          // Componente de texto do Botão 2
    public TextMeshProUGUI textoBotaoOpcao3;          // Componente de texto do Botão 3

    [Header("Configurações do Efeito de Escrita")]
    public float velocidadEscrita = 0.03f;
    private Coroutine coroutineDigitacaoAtiva;

    [Header("Paineis de UI Originais")]
    public GameObject painelMenuInicial;     
    public GameObject painelCabineProvisorio;

    [Header("🆕 Sistema de Telas do Monitor Interativo")]
    public GameObject painelTerminalComputador; // Tela principal com os 2 botões
    public GameObject painelStatusRecursos;     // Tela com os tanques, escudos, caixas e pilhas
    public GameObject painelJanelaDeEventos;
    public ControladorTelaStatus controladorStatus;    // Tela onde aparece o texto da máquina de escrever e opções

    [Header("Configurações de Áudio Coletivas")]
    public AudioSource audioTrilhaMenu;        
    public AudioSource emissorCorreria;       

    [Header("Recursos do Submarino (0-100)")]
    public int oxigenio = 100;
    public int integridadeDoCasco = 100; 
    public int energiaEletrica = 100;
    public int suprimentos = 50; 

    [Header("Referências dos Tripulantes Oficiais")]
    public Tripulante capitaoAugusto;
    public Tripulante engenheiraHelena;
    public Tripulante biologoRafael;
    public Tripulante tecnicoLucas;
    public Tripulante segurancaMaya;

    [Header("Sistema de Fim de Jogo")]
    public CanvasGroup telaEscura;          
    public GameObject arteVitoria;          
    public GameObject arteDerrota;          
    public float velocidadFade = 1.5f;     

    [Header("🆕 Sistema de Diálogos dos Tripulantes (Telas Completas)")]
    [SerializeField] private GameObject painelDialogoCompleto; // O painel pai que engloba o sistema
    [SerializeField] private List<GameObject> telasDeDialogoPersonagens; // Elemento 0 = Augusto, 1 = Helena, 2 = Maya, 3 = Rafael, 4 = Lucas
    [SerializeField] private TextMeshProUGUI textoNomeUI; // O texto do Nome (se ainda for usar por cima da arte)
    [SerializeField] private TextMeshProUGUI textoFalaUI; // O texto da Fala que fica dentro da caixa verde
    private PersonagemClicavel personagemQueEstaFalando;

    [Header("🆕 Sistema de Passagem de Dia")]
    [SerializeField] private GameObject objetoPassagemDia;

    private void Start()
    {
        // Busca automática de segurança do Controlador de Status
        if (controladorStatus == null)
        {
            controladorStatus = Resources.FindObjectsOfTypeAll<ControladorTelaStatus>()[0];
        }

        if (controladorStatus != null)
        {
            controladorStatus.ConfigurarGerenciadorManualmente(this);
        }

        if (SceneManager.GetActiveScene().name == "Creditos")
        {
            StartCoroutine(ContagemRegressivaCreditos());
            return;
        }

        estadoAtual = EstadoDoJogo.Menu;
        ConfigurarTextosIntroducao();
        FecharTodasAsTelasDoMonitor();

        if (telaEscura != null) telaEscura.alpha = 0f;
        if (arteVitoria != null) arteVitoria.SetActive(false);
        if (arteDerrota != null) arteDerrota.SetActive(false);

        if (audioTrilhaMenu != null)
        {
            audioTrilhaMenu.loop = true;
            audioTrilhaMenu.Play();
        }

        // 🆕 MUDANÇA: No Dia 1, todo mundo acorda pronto para falar!
        AtivarFalasDeTodosOsTripulantes();
    }

    public void FinalizarDia()
    {
        oxigenio -= 10;
        suprimentos -= 5;

        oxigenio = Mathf.Clamp(oxigenio, 0, 100);
        integridadeDoCasco = Mathf.Clamp(integridadeDoCasco, 0, 100);
        energiaEletrica = Mathf.Clamp(energiaEletrica, 0, 100);
        suprimentos = Mathf.Clamp(suprimentos, 0, 100);

        if (controladorStatus != null) controladorStatus.AtualizarInterface();

        FecharTodasAsTelasDoMonitor();

        if (integridadeDoCasco <= 0 || oxigenio <= 0)
        {
            estadoAtual = EstadoDoJogo.GameOver;
            if (painelJanelaDeEventos != null) painelJanelaDeEventos.SetActive(true);
            
            if(textoBotaoOpcao1 != null) textoBotaoOpcao1.transform.parent.gameObject.SetActive(false);

            textoExibidoNoMonitor = "GAME OVER\nO submarino colapsou.";
            AtualizarTextoComEfeito(textoExibidoNoMonitor, componenteTextoDoMonitor);
            
            StartCoroutine(EfeitoFadeEIrParaCreditos(false));
            return;
        }

        diaAtual++;

        if (diaAtual > 7)
        {
            estadoAtual = EstadoDoJogo.GameOver;
            if (painelJanelaDeEventos != null) painelJanelaDeEventos.SetActive(true);
            
            if(textoBotaoOpcao1 != null) textoBotaoOpcao1.transform.parent.gameObject.SetActive(false);

            textoExibidoNoMonitor = "VITÓRIA!\nSobreviveram aos 7 dias!";
            AtualizarTextoComEfeito(textoExibidoNoMonitor, componenteTextoDoMonitor);
            
            StartCoroutine(EfeitoFadeEIrParaCreditos(true));
        }
        else
        {
            estadoAtual = EstadoDoJogo.Gameplay;
            if (componenteTextoDoMonitor != null) componenteTextoDoMonitor.text = "";
            
            if (painelCabineProvisorio != null) painelCabineProvisorio.SetActive(true);

            StartCoroutine(EfeitoPassagemDia(diaAtual));

            // 🆕 MUDANÇA: Ativa as exclamações e falas de absolutamente todos os personagens para o novo dia
            AtivarFalasDeTodosOsTripulantes();
        }
    }

    private void AtivarFalasDeTodosOsTripulantes()
    {
        PersonagemClicavel[] todosOsTripulantes = FindObjectsByType<PersonagemClicavel>(FindObjectsSortMode.None);
        
        foreach (PersonagemClicavel tripulante in todosOsTripulantes)
        {
            if (tripulante != null)
            {
                // Carrega a fala indexada do dia atual para cada um deles
                tripulante.AtualizarFalaDoDia(diaAtual);
            }
        }
    }

    public void BotaoComecarJogo()
    {
        estadoAtual = EstadoDoJogo.Introducao;
        painelMenuInicial.SetActive(false);
        paginaIntroducaoAtual = 0;

        if (componenteTextoIntroducao != null) 
            componenteTextoIntroducao.gameObject.SetActive(true);

        if (audioTrilhaMenu != null) audioTrilhaMenu.Stop(); 
        if (emissorCorreria != null)
        {
            emissorCorreria.loop = true; 
            emissorCorreria.Play(); 
        }

        StartCoroutine(GerenciarTempoIntroducao());
    }

    void ConfigurarTextosIntroducao()
    {
        paginasIntroducao[0] = "[SISTEMA DE GRAVAÇÃO DE ÁUDIO – CABINE DE COMANDO]\n03:14 AM – COMANDANTE: \"Não me importam os alertas! Estamos quase lá. Esqueçam o protocolo, força máxima nos motores de descida! Nós vamos alcançar aquela jazida mineral antes de amanhecer!\"\n\n[ALERTA CRÍTICO DE SISTEMA]\nPRESSÃO HIDROSTÁTICA EXCEDIDA EM 140%.\nFALHA ESTRUTURAL IMINENTE NO SETOR DE PROPULSÃO.";
        paginasIntroducao[1] = "[REGISTRO DE DANOS EM TEMPO REAL]\n03:16 AM – Motores principais implodiram. Perda total de aceleração e controle de direção.\n03:17 AM – Estação em queda livre na Fossa das Marianas. Profundidade atual: 5.200 metros... 5.800 metros...\n03:18 AM – Curto-circuito geral na fiação da Ala B. Fogo detectado na cozinha e nos dormitórios secundários.\n03:19 AM – O cabo de fibra óptica que nos ligava à superfície arrebentou. Conexão perdida com o posto avançado da Guarda Costeira. Estamos completamente cegos e isolados.";
        paginasIntroducao[2] = "[COLISÃO DETECTADA]\nO submarino atingiu um paredão de rochas durante a descida. Choque severo na ala habitacional.\n\nDescompressão em 75% dos setores. Protocolo de isolamento a vácuo ativado. Apenas 5 tripulantes respondem aos sinais vitais na Ala Central. Os demais assentos não registram atividade.";
        paginasIntroducao[3] = "[IMPACTO FINAL]\nAbertura de amortecedores de emergência falhou. A estação colidiu contra o leito oceânico profundo.\nProfundidade final: 7.000 metros.\n\n[SISTEMA REINICIALIZADO EM MODO DE SOBREVIVÊNCIA]\nEnergia gerada por baterias reserva. Suporte à via estimado para exatamente <color=#FF3333>7 dias</color>.";
        paginasIntroducao[4] = "INICIAR DIA 1...";
    }

    IEnumerator GerenciarTempoIntroducao()
    {
        if (this == null || componenteTextoIntroducao == null) yield break;
        float tamanhoFonteOriginal = componenteTextoIntroducao.fontSize;

        while (paginaIntroducaoAtual < paginasIntroducao.Length)
        {
            if (this == null || componenteTextoIntroducao == null) yield break;
            textoExibidoNoMonitor = paginasIntroducao[paginaIntroducaoAtual];
            
            if (paginaIntroducaoAtual == 4)
            {
                componenteTextoIntroducao.fontSize = tamanhoFonteOriginal * 1.6f; 
                componenteTextoIntroducao.alignment = TextAlignmentOptions.Center; 
            }
            else
            {
                componenteTextoIntroducao.fontSize = tamanhoFonteOriginal;
                componenteTextoIntroducao.alignment = TextAlignmentOptions.TopLeft; 
            }

            AtualizarTextoComEfeito(textoExibidoNoMonitor, componenteTextoIntroducao);
            
            if (paginaIntroducaoAtual == 4) yield return new WaitForSeconds(6f); 
            else yield return new WaitForSeconds(22f); 

            paginaIntroducaoAtual++;
        }

        if (this != null && componenteTextoIntroducao != null)
        {
            componenteTextoIntroducao.fontSize = tamanhoFonteOriginal;
            componenteTextoIntroducao.alignment = TextAlignmentOptions.TopLeft;
        }

        IniciarGameplay();
    }

    void IniciarGameplay()
    {
        if (emissorCorreria != null)
        {
            emissorCorreria.Stop(); 
            emissorCorreria.loop = false; 
        }

        if (audioTrilhaMenu != null)
        {
            audioTrilhaMenu.loop = true; 
            audioTrilhaMenu.Play();      
        }

        if (componenteTextoIntroducao != null) 
        {
            componenteTextoIntroducao.text = ""; 
            componenteTextoIntroducao.gameObject.SetActive(false);
        }

        if (painelCabineProvisorio != null) painelCabineProvisorio.SetActive(true); 

        estadoAtual = EstadoDoJogo.Gameplay;
    }

    public void ClicarNoMonitor()
    {
        Debug.Log("Física funcionando! O clique chegou no script. Estado atual do jogo: " + estadoAtual);

        if (estadoAtual == EstadoDoJogo.Gameplay)
        {
            AbrirMenuPrincipalDoTerminal();
        }
        else
        {
            Debug.LogWarning("O monitor não abriu porque o estado do jogo é " + estadoAtual + " e deveria ser Gameplay!");
        }
    }

    public void AbrirMenuPrincipalDoTerminal()
    {
        FecharTodasAsTelasDoMonitor();
        if (painelCabineProvisorio != null) painelCabineProvisorio.SetActive(false);
        if (painelTerminalComputador != null) painelTerminalComputador.SetActive(true);
    }

    public void BotaoAcessarStatusRecursos()
    {
        FecharTodasAsTelasDoMonitor();
        if (painelCabineProvisorio != null) painelCabineProvisorio.SetActive(false);
        if (painelStatusRecursos != null) painelStatusRecursos.SetActive(true);

        if (controladorStatus != null) controladorStatus.AtualizarInterface();
    }

    public void BotaoAcessarJanelaDeEventos()
    {
        FecharTodasAsTelasDoMonitor();
        if (painelCabineProvisorio != null) painelCabineProvisorio.SetActive(false);
        if (painelJanelaDeEventos != null) painelJanelaDeEventos.SetActive(true);

        if (estadoAtual == EstadoDoJogo.Gameplay)
        {
            GerarEventoDoDia();
        }
    }

    public void BotaoFecharTerminal()
    {
        FecharTodasAsTelasDoMonitor();
        if (painelCabineProvisorio != null) painelCabineProvisorio.SetActive(true);
        estadoAtual = EstadoDoJogo.Gameplay;
    }

    public void FecharTodasAsTelasDoMonitor()
    {
        if (painelTerminalComputador != null) painelTerminalComputador.SetActive(false);
        if (painelStatusRecursos != null) painelStatusRecursos.SetActive(false);
        if (painelJanelaDeEventos != null) painelJanelaDeEventos.SetActive(false);
    }

    void GerarEventoDoDia()
    {
        estadoAtual = EstadoDoJogo.EventoAtivo;

        problemaAtualID = Random.Range(1, 7); 
        varianteAtualID = Random.Range(1, 3);

        #region Dicionário de Textos e Opções Resumidas
        if (problemaAtualID == 1) 
        {
            if (varianteAtualID == 1)
            {
                textoExibidoNoMonitor = "A FRESTA NO CASCO\nUm rangido ensurdecedor ecoou pelo setor de dormitórios. A pressão absurda da fossa abissal encontrou uma microfissura na solida externa do casco e abriu uma fresta. Um jato de água congelante e ultrapreciso está perfurando a sala como uma lâmina, inundando o local e ameaçando comprometer a estrutura inteira.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Selar via Sistema\n(Energia -20)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Mandar Engenheiro conter\n(Engenheiro Pânico +)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Ignorar e isolar a sala\n(Ruptura ++ / Casco -25)";
            }
            else
            {
                textoExibidoNoMonitor = "RUPTURA DA TUBULAÇÃO INTERNA\nA tubulação secundária de resfriamento do reator não aguentou o impacto com as rochas e rachou. Água fervente e vapor altamente pressurizado estão inundando o corredor principal. O calor está insuportável e o chão está virando uma piscina.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Redirecionar Computador\n(Casco -10)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Fechar Válvula Manual\n(Proximidade -2 / Ferido)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Desligar fluxo pelo monitor\n(Energia -20)";
            }
        }
        else if (problemaAtualID == 2) 
        {
            if (varianteAtualID == 1)
            {
                textoExibidoNoMonitor = "CURTO NO GERADOR PRINCIPAL\nAs lâmpadas do teto piscaram três vezes e se apagaram com um estalo estático. O gerador principal entrou em pane protetiva e o submarino mergulhou em uma escuridão total, restando apenas os bipes azuis deste monitor. O silêncio e o breu estão cobrando o seu preço psicológico.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Bateria de Emergência\n(Energia -15)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Forçar Reincialização\n(Casco -10)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Deixar no escuro\n(Chance de Pânico)";
            }
            else
            {
                textoExibidoNoMonitor = "SOBRECARGA NOS FUSÍVEIS DA ALA HABITACIONAL\nUma explosão de faíscas queimou os fusíveis da Ala Central. Estamos sem iluminação nos alojamentos e na cozinha. Os tripulantes recusam-se a andar pelo setor escuro, alegando estarem ouvindo barulhos estranhos vindos do metal do casco.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Mandar dois investigarem\n(Proximidade +2)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Mandar um sozinho\n(Status Irracional +)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Ignorar por hoje\n(Fome amanhã + / Comida -3)";
            }
        }
        else if (problemaAtualID == 3) 
        {
            if (varianteAtualID == 1)
            {
                textoExibidoNoMonitor = "FUMAÇA TÓXICA NA VENTILAÇÃO\nO sistema de ventilação começou a soprar uma fumaça cinzenta e com cheiro de plástico queimado vinda de um curto-circuito interno. O ar está ficando denso, asfixiante e os tripulantes não conseguem parar de tossir. O nível de oxigênio está caindo rápido.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Purificador Emergência\n(Oxigênio -20)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Sobrecarga de Exaustão\n(Energia -15)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Forçar a aguentar\n(O2 Consumo Dobra / Catatônico +)";
            }
            else
            {
                textoExibidoNoMonitor = "OBSTRUÇÃO POR ALGAS ABISSAIS\nO duto externo de captação foi obstruído por resíduos e lodo denso da fossa durante o impacto. Os exaustores estão sobrecarregando e fazendo um barulho agonizante. O ar está ficando abafado e quente.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Inverter exaustores\n(Energia -10)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Cozinheiro limpa duto\n(Cozinheiro Exausto +)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Não fazer nada\n(Oxigênio -25)";
            }
        }
        else if (problemaAtualID == 4) 
        {
            if (varianteAtualID == 1)
            {
                textoExibidoNoMonitor = "FOGO TÉCNICO NO PAINEL ELÉTRICO\nALERTA DE INCÊNDIO: Superaquecimento nos cabos de alta tensão atrás das paredes do painel de controle. Fumaça preta e tóxica começa a sair pelas frestas dos botões. Se o fogo atingir o computador central, perderemos o monitor para sempre.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Supressão Química\n(Energia -10 / Oxigênio -10)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Selo de Vácuo Manual\n(Casco -15)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Apagar com panos\n(Tripulante Ferido +)";
            }
            else
            {
                textoExibidoNoMonitor = "INCÊNDIO NOS ALOJAMENTOS\nUm curto-circuito em uma tomada antiga incendiou um dos colchões nos alojamentos. O fogo está se espalhando pelas roupas de dry-fit e pertences pessoais da tripulação. O pânico está instaurado.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Salvar pertences\n(Proximidade do Tripulante Maximizada)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Selar as portas e queimar\n(Proximidade Geral -3 / Casco -15)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Inundar Setor Habitacional\n(Comida -5 / Energia -10)";
            }
        }
        else if (problemaAtualID == 5) 
        {
            if (varianteAtualID == 1)
            {
                textoExibidoNoMonitor = "PANE NO COMPRESSOR TÉRMICO\nO visor da câmara de resfriamento de alimentos apagou. O compressor térmico queimou devido às oscilações de energia. Se a temperatura subir mais 3 graus, as rações hidrolisadas vão azedar e mofar antes do amanhecer.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Gambiarra Elétrica\n(Energia -15)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Transferir para área inundada\n(Sanidade -)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Não consertar\n(Comida reduzida pela metade)";
            }
            else
            {
                textoExibidoNoMonitor = "INVASÃO DE PRAGAS / CONTAMINAÇÃO\nA umidade do acidente fez com que fungos da fossa abissal começassem a brotar nas caixas de suprimentos da cozinha. Duas caixas de ração já estão cobertas por uma gosma esverdeada estranha.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Purga por Aquecimento\n(Energia -10)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Jogar caixas fora\n(Comida -3)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Forçar a tripulação a comer\n(Chance de Doença/Infecção)";
            }
        }
        else if (problemaAtualID == 6) 
        {
            if (varianteAtualID == 1)
            {
                textoExibidoNoMonitor = "O RANGIDO PROFUNDO\nA estação foi arrastada por uma correnteza abissal contra uma parede de rocha pontiaguda. Um rangido agudo de metal sofrendo torção ecoa pelos tetos. As vigas de sustentação estão entortando visivelmente sob o peso do oceano.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Calibragem Estabilizadores\n(Energia -15)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Inundar um tanque\n(Energia -10)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Ignorar perigo\n(Casco -30 / Risco Game Over)";
            }
            else
            {
                textoExibidoNoMonitor = "CHUVA DE DETRITOS ROCHOSOS\nUm pequeno desabamento na parede da fossa jogou pedras colossais contra o teto do submarino. O impacto amassou o teto do laboratório central e estourou várias lâmpadas. A estrutura está sob estresse máximo.";
                if(textoBotaoOpcao1 != null) textoBotaoOpcao1.text = "Mandar Eng. e Pragmático\n(Proximidade +2)";
                if(textoBotaoOpcao2 != null) textoBotaoOpcao2.text = "Abandonar laboratório\n(Casco -20 / Inutilizável)";
                if(textoBotaoOpcao3 != null) textoBotaoOpcao3.text = "Escoramento Mecânico\n(Energia -20)";
            }
        }
        #endregion

        AtualizarTextoComEfeito(textoExibidoNoMonitor, componenteTextoDoMonitor);
    }

    void AtualizarTextoComEfeito(string textoCompleto, TextMeshProUGUI campoTextoTarget)
    {
        if (coroutineDigitacaoAtiva != null) StopCoroutine(coroutineDigitacaoAtiva);
        coroutineDigitacaoAtiva = StartCoroutine(EfeitoMaquinaDeEscrever(textoCompleto, campoTextoTarget));
    }

    IEnumerator EfeitoMaquinaDeEscrever(string textoCompleto, TextMeshProUGUI campoTextoTarget)
    {
        if (campoTextoTarget != null)
        {
            campoTextoTarget.text = "";
            foreach (char letra in textoCompleto.ToCharArray())
            {
                if (this == null || campoTextoTarget == null) yield break;
                campoTextoTarget.text += letra;
                yield return new WaitForSeconds(velocidadEscrita);
            }
        }
        coroutineDigitacaoAtiva = null;
    }

    public void SelecionarOpcao1()
    {
        if (estadoAtual != EstadoDoJogo.EventoAtivo) return;

        if (problemaAtualID == 1) 
        {
            if (varianteAtualID == 1) { energiaEletrica -= 20; }
            else { integridadeDoCasco -= 10; }
        }
        else if (problemaAtualID == 2) 
        {
            if (varianteAtualID == 1) { energiaEletrica -= 15; }
            else { if(engenheiraHelena != null) engenheiraHelena.AlterarProximidade(2); }
        }
        else if (problemaAtualID == 3) 
        {
            if (varianteAtualID == 1) { oxigenio -= 20; }
            else { energiaEletrica -= 10; }
        }
        else if (problemaAtualID == 4) 
        {
            if (varianteAtualID == 1) { energiaEletrica -= 10; oxigenio -= 10; }
            else { if(segurancaMaya != null) segurancaMaya.AlterarProximidade(4); }
        }
        else if (problemaAtualID == 5) 
        {
            if (varianteAtualID == 1) { energiaEletrica -= 15; }
            else { energiaEletrica -= 10; }
        }
        else if (problemaAtualID == 6) 
        {
            if (varianteAtualID == 1) { energiaEletrica -= 15; }
            else { if(engenheiraHelena != null) engenheiraHelena.AlterarProximidade(2); }
        }

        FinalizarDia();
    }

    public void SelecionarOpcao2()
    {
        if (estadoAtual != EstadoDoJogo.EventoAtivo) return;

        if (problemaAtualID == 1)
        {
            if (varianteAtualID == 1) { if(engenheiraHelena != null) engenheiraHelena.estaEmPanico = true; }
            else { Debug.Log("Saiu ferido."); }
        }
        else if (problemaAtualID == 2)
        {
            if (varianteAtualID == 1) { energiaEletrica -= 10; }
            else { if(tecnicoLucas != null) tecnicoLucas.estaIrracional = true; }
        }
        else if (problemaAtualID == 3)
        {
            if (varianteAtualID == 1) { energiaEletrica -= 15; }
            else { if(biologoRafael != null) biologoRafael.estaEmPanico = true; }
        }
        else if (problemaAtualID == 4)
        {
            if (varianteAtualID == 1) { integridadeDoCasco -= 15; }
            else { integridadeDoCasco -= 15; if(capitaoAugusto != null) capitaoAugusto.AlterarProximidade(-3); }
        }
        else if (problemaAtualID == 5)
        {
            if (varianteAtualID == 1) { Debug.Log("Água fria."); }
            else { suprimentos -= 3; }
        }
        else if (problemaAtualID == 6)
        {
            if (varianteAtualID == 1) { energiaEletrica -= 10; }
            else { integridadeDoCasco -= 20; }
        }

        FinalizarDia();
    }

    public void SelecionarOpcao3()
    {
        if (estadoAtual != EstadoDoJogo.EventoAtivo) return;

        if (problemaAtualID == 1)
        {
            if (varianteAtualID == 1) { integridadeDoCasco -= 25; }
            else { energiaEletrica -= 20; }
        }
        else if (problemaAtualID == 2) 
        {
            if (varianteAtualID == 2) { suprimentos -= 3; }
        }
        else if (problemaAtualID == 3)
        {
            if (varianteAtualID == 1) { oxigenio -= 30; if(engenheiraHelena != null) engenheiraHelena.estaCatatonico = true; }
            else { oxigenio -= 25; }
        }
        else if (problemaAtualID == 4)
        {
            if (varianteAtualID != 1) { suprimentos -= 5; energiaEletrica -= 10; }
        }
        else if (problemaAtualID == 5)
        {
            if (varianteAtualID == 1) { suprimentos /= 2; }
        }
        else if (problemaAtualID == 6)
        {
            if (varianteAtualID == 1) { integridadeDoCasco -= 30; }
            else { energiaEletrica -= 20; }
        }

        FinalizarDia();
    }

    public void AbrirPainelDialogo(string nome, string fala, Sprite retrato, PersonagemClicavel personagem)
    {
        // 1. Trava o estado do jogo para evitar cliques repetidos na gameplay
        estadoAtual = EstadoDoJogo.EventoAtivo; 
        personagemQueEstaFalando = personagem;

        // 2. DESLIGA todas as telas de fundo de personagem para não ficarem sobrepostas
        foreach (GameObject tela in telasDeDialogoPersonagens)
        {
            if (tela != null) tela.SetActive(false);
        }

        // 3. LIGA apenas a tela cujo nome combine com o personagem clicado
        bool telaEncontrada = false;
        string nomeFiltro = nome.Split(' ')[0].ToLower(); // Pega apenas o primeiro nome

        for (int i = 0; i < telasDeDialogoPersonagens.Count; i++)
        {
            if (telasDeDialogoPersonagens[i] != null)
            {
                string nomeDaTela = telasDeDialogoPersonagens[i].name.ToLower();
                
                if (nomeDaTela.Contains(nomeFiltro))
                {
                    GameObject telaAtiva = telasDeDialogoPersonagens[i];
                    telaAtiva.SetActive(true);
                    telaEncontrada = true;
                    Debug.Log("Tela ativada com sucesso para: " + nome);

                    // 🆕 MUDANÇA: Procura os componentes de texto especificamente dentro desta tela ativada!
                    TextMeshProUGUI[] textosDaTela = telaAtiva.GetComponentsInChildren<TextMeshProUGUI>(true);
                    
                    // Se você tiver 2 textos lá dentro (um pro nome e outro pra fala)
                    if (textosDaTela.Length >= 2)
                    {
                        // O primeiro costuma ser o Nome e o segundo a Fala (ou vice-versa na ordem da Hierarquia)
                        textosDaTela[0].text = nome;
                        textosDaTela[1].text = fala;
                    }
                    else if (textosDaTela.Length == 1)
                    {
                        // Se colocou só um campo grande de texto para tudo
                        textosDaTela[0].text = "<b>" + nome + "</b>\n" + fala;
                    }

                    break;
                }
            }
        }

        if (!telaEncontrada)
        {
            Debug.LogWarning("Alerta: Nenhuma das 5 telas da lista do GameManager contém o nome: " + nome);
            estadoAtual = EstadoDoJogo.Gameplay; 
            return;
        }

        // 5. Liga o painel pai mestre que desenha o sistema na tela
        if (painelDialogoCompleto != null) painelDialogoCompleto.SetActive(true);
    }

    public void BotaoFecharDialogo()
    {
        if (painelDialogoCompleto != null) painelDialogoCompleto.SetActive(false);

        // Desliga todas as telas de fundo para limpar o Canvas
        foreach (GameObject tela in telasDeDialogoPersonagens)
        {
            if (tela != null) tela.SetActive(false);
        }

        if (personagemQueEstaFalando != null)
        {
            personagemQueEstaFalando.falaAtual = "";
            personagemQueEstaFalando.AtualizarIndicador();
        }

        estadoAtual = EstadoDoJogo.Gameplay; 
    }

    IEnumerator EfeitoPassagemDia(int proximoDia)
    {
        if (objetoPassagemDia != null)
        {
            TextMeshProUGUI textoComponente = objetoPassagemDia.GetComponent<TextMeshProUGUI>();
            if (textoComponente != null)
            {
                textoComponente.text = "DIA " + proximoDia;
            }

            CanvasGroup canvasGroupDia = objetoPassagemDia.GetComponent<CanvasGroup>();
            if (canvasGroupDia != null)
            {
                canvasGroupDia.alpha = 0f;
                objetoPassagemDia.SetActive(true);

                while (canvasGroupDia.alpha < 1f)
                {
                    canvasGroupDia.alpha += Time.deltaTime * 2f;
                    yield return null;
                }
                canvasGroupDia.alpha = 1f;

                yield return new WaitForSeconds(1.8f);

                while (canvasGroupDia.alpha > 0f)
                {
                    canvasGroupDia.alpha -= Time.deltaTime * 2f;
                    yield return null;
                }
                canvasGroupDia.alpha = 0f;
                
                objetoPassagemDia.SetActive(false);
            }
        }
    }

    IEnumerator EfeitoFadeEIrParaCreditos(bool jogadorVenceu)
    {
        FecharTodasAsTelasDoMonitor();

        GameObject arteAtiva = jogadorVenceu ? arteVitoria : arteDerrota;

        if (arteAtiva != null)
        {
            arteAtiva.SetActive(true);
            CanvasGroup canvasGroupArte = arteAtiva.GetComponent<CanvasGroup>();
            
            if (canvasGroupArte != null)
            {
                canvasGroupArte.alpha = 0f;
                while (canvasGroupArte.alpha < 1f)
                {
                    canvasGroupArte.alpha += Time.deltaTime * 0.5f; 
                    yield return null;
                }
            }
        }

        yield return new WaitForSeconds(5f);

        if (telaEscura != null)
        {
            while (telaEscura.alpha < 1f)
            {
                telaEscura.alpha += Time.deltaTime * velocidadFade;
                yield return null;
            }
            yield return new WaitForSeconds(1f);
        }

        SceneManager.LoadScene("Creditos");
    }

    public void VoltarParaOMenu()
    {
        SceneManager.LoadScene("SampleScene"); 
    }

    IEnumerator ContagemRegressivaCreditos()
    {
        yield return new WaitForSeconds(13f);
        VoltarParaOMenu();
    }

    private void Update()
    {
        if (this == null || !gameObject.activeInHierarchy) return;

        if (estadoAtual == EstadoDoJogo.Introducao && Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            paginaIntroducaoAtual++;
            
            if (paginaIntroducaoAtual < paginasIntroducao.Length)
            {
                if (componenteTextoIntroducao != null) StartCoroutine(GerenciarTempoIntroducao());
            }
            else
            {
                if (emissorCorreria != null)
                {
                    emissorCorreria.Stop();
                    emissorCorreria.loop = false;
                }
                IniciarGameplay(); 
            }
        }
    }
}