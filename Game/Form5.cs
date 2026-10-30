
using System;
using System.Collections.Generic;
using System.Drawing;



namespace movimento
{
    public partial class Form5 : Form
    {


        private readonly GameTimer timerJogo;
        private readonly HashSet<Keys> teclas = new HashSet<Keys>();

        private enum TipoPersonagem
        {
            ItadoriYuji, YutaOkkotsu, Choso, TojiFushiguro, NaoyaZenin, GojoSatoru, RyomenSukuna,
            MegumiFushiguro
        }


        // Uma sombra invocada funciona como uma unidade independente: tem vida,
        // velocidade, dano, tempo de permanência e intervalo entre ataques.
        private sealed class SombraInvocada
        {
            public bool Ativa;
            public int Indice;
            public int Dono;
            public float X;
            public float Y;
            public int Vida = 55;
            public int VidaMaxima = 55;
            public int TicksRestantes = 270;
            public int CooldownAtaque = 14;
            public int QuadrosAtaque = 0;
            public int Direcao = 2;
            public float Velocidade = 3f;
            public int Dano = 8;
            public int IntervaloAtaque = 30;
        }

        private SombraInvocada sombraJogador1;
        private SombraInvocada sombraJogador2;

        private const int TotalPersonagens = 8;
        private const int LarguraBase = 1000;
        private const int AlturaBase = 650;

        // A batalha só começa depois que os dois jogadores confirmarem.
        private bool telaSelecao = true;
        private int escolha1 = (int)TipoPersonagem.YutaOkkotsu;
        private int escolha2 = (int)TipoPersonagem.ItadoriYuji;
        private bool confirmado1 = false;
        private bool confirmado2 = false;

        private TipoPersonagem personagem1 = TipoPersonagem.YutaOkkotsu;
        private TipoPersonagem personagem2 = TipoPersonagem.ItadoriYuji;

        private float x = 100, y = 150;
        private float x2 = 300, y2 = 150;

        private int vida1 = 100, vida2 = 100;
        private int direcao1 = 2, direcao2 = 1;

        private int quadroAnimacao = 0, quadroAnimacao2 = 0;
        private int contadorAnimacao = 0, contadorAnimacao2 = 0;

        private bool andando = false, andando2 = false;

        private int cooldown1 = 0, cooldown2 = 0;
        private int ataque1 = 0, ataque2 = 0;

        private int congelado1 = 0, congelado2 = 0;
        private int cooldownPoder1 = 0, cooldownPoder2 = 0;
        private int dashVisual1 = 0, dashVisual2 = 0;

        // Efeitos desenhados no Canvas e uma segunda batida para o punho divergente.
        private sealed class EfeitoGolpe
        {
            public TipoPersonagem Tipo;
            public float OrigemX, OrigemY, AlvoX, AlvoY;
            public int Direcao, Idade, Duracao = 17;
            public bool Acertou;
        }
        private readonly List<EfeitoGolpe> efeitosGolpe = new List<EfeitoGolpe>();
        private int ecoItadori1, ecoItadori2;
        private int tremorQuadros;
        private int tempoArena;

        // Passivas automáticas. Os estados são individuais para os dois jogadores.
        private int comboItadori1, comboItadori2;
        private int cargaNaoya1, cargaNaoya2;
        private int recargaInfinito1, recargaInfinito2;
        private int avisoPassiva1, avisoPassiva2;
        private string textoPassiva1 = "", textoPassiva2 = "";

        private void MostrarPassiva(int jogador, string mensagem)
        {
            if (jogador == 1)
            {
                textoPassiva1 = mensagem;
                avisoPassiva1 = 50; // 1,5 segundo
            }
            else
            {
                textoPassiva2 = mensagem;
                avisoPassiva2 = 50;
            }
        }

        private string NomePassiva(TipoPersonagem tipo)
        {
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji: return "Ritmo Divergente";
                case TipoPersonagem.YutaOkkotsu: return "Energia Reversa";
                case TipoPersonagem.Choso: return "Sangue Concentrado";
                case TipoPersonagem.TojiFushiguro: return "Restrição Celestial";
                case TipoPersonagem.NaoyaZenin: return "Movimento Projetado";
                case TipoPersonagem.GojoSatoru: return "Infinito";
                case TipoPersonagem.RyomenSukuna: return "Instinto do Rei";
                case TipoPersonagem.MegumiFushiguro: return "Vínculo das Sombras";
                default: return "Sem passiva";
            }
        }

        private string DetalhePassiva(TipoPersonagem tipo)
        {
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji: return "Cada 3 golpes acertados: +7 dano.";
                case TipoPersonagem.YutaOkkotsu: return "Golpes acertados restauram 3 de vida.";
                case TipoPersonagem.Choso: return "Com 50 ou menos de vida: +5 dano.";
                case TipoPersonagem.TojiFushiguro: return "Movimento 25% mais rápido.";
                case TipoPersonagem.NaoyaZenin: return "Mova-se por 20 quadros: +4 no golpe.";
                case TipoPersonagem.GojoSatoru: return "Bloqueia 1 golpe básico a cada 8s.";
                case TipoPersonagem.RyomenSukuna: return "Contra rivais com até 40 vida: +5 dano.";
                case TipoPersonagem.MegumiFushiguro: return "Sombras: +10 de vida e +2 de dano.";
                default: return "";
            }
        }

        private string EstadoPassiva(int jogador, TipoPersonagem tipo)
        {
            int hp = jogador == 1 ? vida1 : vida2;
            int hpRival = jogador == 1 ? vida2 : vida1;
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji:
                    return "COMBO " + (jogador == 1 ? comboItadori1 : comboItadori2) + "/3";
                case TipoPersonagem.YutaOkkotsu: return "CURA +3 NO ACERTO";
                case TipoPersonagem.Choso: return hp <= 50 ? "ATIVADA (+5)" : "ATIVA COM VIDA <= 50";
                case TipoPersonagem.TojiFushiguro: return "VELOCIDADE +25%";
                case TipoPersonagem.NaoyaZenin:
                    return "CARGA " + Math.Min(20, jogador == 1 ? cargaNaoya1 : cargaNaoya2) + "/20";
                case TipoPersonagem.GojoSatoru:
                    int espera = jogador == 1 ? recargaInfinito1 : recargaInfinito2;
                    return espera == 0 ? "ESCUDO PRONTO" : "ESCUDO EM " + ((espera * 30 + 999) / 1000) + "s";
                case TipoPersonagem.RyomenSukuna:
                    return hpRival <= 40 ? "EXECUÇÃO ATIVA (+5)" : "ATIVA COM RIVAL <= 40";
                case TipoPersonagem.MegumiFushiguro:
                    return "SOMBRAS REFORÇADAS";
                default: return "";
            }
        }

        // Cutscene unificada para os quatro novos feiticeiros.
        // Etapas: preparação, técnica especial e impacto final.
        private bool cutsceneQuartetoAtiva = false;
        private int quartetoJogador = 0, quartetoEtapa = 0, quartetoQuadro = 0;
        private TipoPersonagem quartetoTipo = TipoPersonagem.ItadoriYuji;
        private float quartetoInicioX, quartetoInicioY, quartetoAlvoX, quartetoAlvoY;
        private bool quartetoAcertou = false, quartetoImpactoAplicado = false;
        private const int QuadrosPreparacaoQuarteto = 14;
        private const int QuadrosGolpeQuarteto = 18;
        private const int QuadrosFinalQuarteto = 16;

        // Estado da cutscene da Técnica de Projeção do Naoya.
        // Etapas: 0 = preparação, 1 = avanço rápido, 2 = impacto/saída.
        private bool cutsceneNaoyaAtiva = false;
        private int cutsceneNaoyaJogador = 0;
        private int cutsceneNaoyaEtapa = 0;
        private int cutsceneNaoyaQuadro = 0;
        private float naoyaInicioX, naoyaInicioY, naoyaFimX, naoyaFimY;
        private float naoyaAlvoX, naoyaAlvoY;
        private bool naoyaAcertou = false;
        private bool naoyaImpactoAplicado = false;
        private const int QuadrosPreparacaoNaoya = 12;
        private const int QuadrosAvancoNaoya = 10;
        private const int QuadrosImpactoNaoya = 20;

        // Cutscene do Gojo: preparação, disparo do Roxo e impacto.
        private bool cutsceneGojoAtiva = false;
        private int gojoJogador = 0, gojoEtapa = 0, gojoQuadro = 0;
        private float gojoInicioX, gojoInicioY, gojoAlvoX, gojoAlvoY;
        private bool gojoAcertou = false, gojoImpactoAplicado = false;
        private const int QuadrosPreparacaoGojo = 16;
        private const int QuadrosDisparoGojo = 18;
        private const int QuadrosImpactoGojo = 18;

        // Cutscene do Sukuna: abertura do domínio e sequência de cortes.
        private bool cutsceneSukunaAtiva = false;
        private int sukunaJogador = 0, sukunaEtapa = 0, sukunaQuadro = 0;
        private float sukunaInicioX, sukunaInicioY, sukunaAlvoX, sukunaAlvoY;
        private bool sukunaAcertou = false, sukunaImpactoAplicado = false;
        private const int QuadrosPreparacaoSukuna = 16;
        private const int QuadrosCortesSukuna = 28;
        private const int QuadrosFimSukuna = 20;

        // Megumi Fushiguro: cutscene da Técnica das Dez Sombras.
        // Etapas: invocação, aparição sequencial dos shikigami e Jardim das Sombras.
        private bool cutsceneMegumiAtiva = false;
        private int megumiJogador = 0, megumiEtapa = 0, megumiQuadro = 0;
        private float megumiInicioX, megumiInicioY, megumiAlvoX, megumiAlvoY;
        private bool megumiAcertou = false, megumiImpactoAplicado = false;
        private const int QuadrosPreparacaoMegumi = 16;
        private const int QuadrosInvocacaoMegumi = 50;
        private const int QuadrosDominioMegumi = 22;
        private int megumiSombraIndice = 0;
        private static readonly Random SorteioSombrasMegumi = new Random();
        private static readonly string[] NomesSombras = new string[]
        {
            "Cães Divinos", "Nue", "Sapo", "Serpente", "Max Elefante",
            "Fuga dos Coelhos", "Veado Redondo", "Boi Perfurante",
            "Tigre Funeral", "Mahoraga"
        };

        private bool ataqueYuta1 = false, ataqueYuta2 = false;

        private bool combateTerminado = false;
        private string vencedor = "";

        private const float Velocidade = 4f;
        private const int Dano = 10;
        private const float Alcance = 65f;

        public Form5()
        {
            InitializeComponent();

            Text = "Meu RPG 2D - Combate";
            ClientSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            DoubleBuffered = true;

            KeyDown += Form5_KeyDown;
            KeyUp += Form5_KeyUp;

            timerJogo = new GameTimer();
            timerJogo.Interval = 30;
            timerJogo.Tick += TimerJogo_Tick;
            timerJogo.Start();
        }

        private void Form5_Load(object sender, EventArgs e) { }

        private void Form5_KeyDown(object sender, KeyEventArgs e)
        {
            if (!teclas.Add(e.KeyCode))
                return;

            // Na tela inicial, as teclas servem para escolher os personagens.
            if (telaSelecao)
            {
                if (!confirmado1)
                {
                    if (e.KeyCode == Keys.A)
                        escolha1 = (escolha1 + TotalPersonagens - 1) % TotalPersonagens;
                    else if (e.KeyCode == Keys.D)
                        escolha1 = (escolha1 + 1) % TotalPersonagens;
                    else if (e.KeyCode == Keys.F)
                        confirmado1 = true;
                }

                if (!confirmado2)
                {
                    if (e.KeyCode == Keys.Left)
                        escolha2 = (escolha2 + TotalPersonagens - 1) % TotalPersonagens;
                    else if (e.KeyCode == Keys.Right)
                        escolha2 = (escolha2 + 1) % TotalPersonagens;
                    else if (e.KeyCode == Keys.Enter)
                        confirmado2 = true;
                }

                personagem1 = (TipoPersonagem)escolha1;
                personagem2 = (TipoPersonagem)escolha2;

                if (confirmado1 && confirmado2)
                {
                    telaSelecao = false;
                    teclas.Clear();
                }

                Invalidate();
                return;
            }

            // Durante a cutscene, os controles ficam pausados para destacar a técnica.
            if (cutsceneQuartetoAtiva || cutsceneNaoyaAtiva || cutsceneGojoAtiva || cutsceneSukunaAtiva || cutsceneMegumiAtiva)
            {
                Invalidate();
                return;
            }

            // Controles normais durante a batalha.
            if (e.KeyCode == Keys.F) Atacar(1);
            if (e.KeyCode == Keys.Enter) Atacar(2);
            if (e.KeyCode == Keys.Q) UsarPoder(1);

            if (e.KeyCode == Keys.ShiftKey)
                UsarPoder(2);
        }

        private void Form5_KeyUp(object sender, KeyEventArgs e)
        {
            teclas.Remove(e.KeyCode);
        }

        private void Atacar(int jogador)
        {
            if (combateTerminado || telaSelecao ||
                cutsceneQuartetoAtiva || cutsceneNaoyaAtiva || cutsceneGojoAtiva ||
                cutsceneSukunaAtiva || cutsceneMegumiAtiva) return;
            if ((jogador == 1 ? cooldown1 : cooldown2) > 0) return;

            TipoPersonagem tipo = jogador == 1 ? personagem1 : personagem2;
            int direcao = jogador == 1 ? direcao1 : direcao2;
            float ax = jogador == 1 ? x : x2;
            float ay = jogador == 1 ? y : y2;

            // Cada golpe básico tem identidade própria, além do poder (Q/Shift).
            int dano = 10, recarga = 15, atordoamento = 0, repulsao = 0;
            float alcance = 65f;
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji:   // Punho Divergente: eco posterior ao impacto
                    dano = 9; alcance = 69f; recarga = 17; break;
                case TipoPersonagem.YutaOkkotsu:   // Katana: alcance maior e bônus de Rika
                    dano = 12; alcance = 96f; recarga = 19; break;
                case TipoPersonagem.Choso:         // Disparo sanguíneo: distância longa
                    dano = 8; alcance = 260f; recarga = 24; break;
                case TipoPersonagem.TojiFushiguro: // Investida curta com a lança
                    dano = 14; alcance = 93f; recarga = 23; break;
                case TipoPersonagem.NaoyaZenin:    // Golpe veloz que interrompe movimento
                    dano = 7; alcance = 70f; recarga = 10; atordoamento = 5; break;
                case TipoPersonagem.GojoSatoru:    // Pulso azul repele o adversário
                    dano = 8; alcance = 90f; recarga = 19; repulsao = 29; break;
                case TipoPersonagem.RyomenSukuna:  // Corte com alcance ampliado
                    dano = 13; alcance = 115f; recarga = 21; break;
                case TipoPersonagem.MegumiFushiguro: // Garras sombrias + apoio do shikigami
                    dano = 9; alcance = 103f; recarga = 18; atordoamento = 7; break;
            }

            // Toji avança na direção do golpe antes de verificar o alcance.
            if (tipo == TipoPersonagem.TojiFushiguro)
            {
                float passoX = direcao == 2 ? 25f : direcao == 1 ? -25f : 0f;
                float passoY = direcao == 0 ? 25f : direcao == 3 ? -25f : 0f;
                if (jogador == 1) { x += passoX; y += passoY; }
                else { x2 += passoX; y2 += passoY; }
                LimitarPosicoes();
                ax = jogador == 1 ? x : x2;
                ay = jogador == 1 ? y : y2;
            }

            bool rika = jogador == 1 ? ataqueYuta1 : ataqueYuta2;
            if (tipo == TipoPersonagem.YutaOkkotsu && rika)
                dano += 12;

            if (tipo == TipoPersonagem.MegumiFushiguro)
            {
                SombraInvocada aliada = jogador == 1 ? sombraJogador1 : sombraJogador2;
                if (aliada != null && aliada.Ativa) dano += 5;
            }
            // Bônus condicionais de Choso e Sukuna aplicam-se ao dano básico.
            bool sangueConcentrado = tipo == TipoPersonagem.Choso &&
                (jogador == 1 ? vida1 : vida2) <= 50;
            bool instintoDoRei = tipo == TipoPersonagem.RyomenSukuna &&
                (jogador == 1 ? vida2 : vida1) <= 40;
            if (sangueConcentrado || instintoDoRei) dano += 5;

            if (jogador == 1) { cooldown1 = recarga; ataque1 = 10; ataqueYuta1 = false; }
            else { cooldown2 = recarga; ataque2 = 10; ataqueYuta2 = false; }

            float cx = ax + 16f, cy = ay + 22f;
            float tx = jogador == 1 ? x2 + 16f : x + 16f;
            float ty = jogador == 1 ? y2 + 22f : y + 22f;
            bool acertou = false;
            float impactoX = cx + (direcao == 2 ? alcance : direcao == 1 ? -alcance : 0f);
            float impactoY = cy + (direcao == 0 ? alcance : direcao == 3 ? -alcance : 0f);

            // Sombras adversárias interceptam os golpes, inclusive os disparos.
            SombraInvocada sombra = jogador == 1 ? sombraJogador2 : sombraJogador1;
            if (sombra != null && sombra.Ativa)
            {
                float sx = sombra.X + 16f, sy = sombra.Y + 22f;
                float dxs = sx - cx, dys = sy - cy;
                if (Math.Sqrt(dxs * dxs + dys * dys) <= alcance &&
                    EstaNaFrente(direcao, dxs, dys))
                {
                    sombra.Vida = Math.Max(0, sombra.Vida - dano);
                    sombra.QuadrosAtaque = 8;
                    if (sombra.Vida <= 0) sombra.Ativa = false;
                    acertou = true;
                    impactoX = sx; impactoY = sy;
                }
            }

            if (!acertou)
            {
                float dx = tx - cx, dy = ty - cy;
                if (Math.Sqrt(dx * dx + dy * dy) <= alcance &&
                    EstaNaFrente(direcao, dx, dy))
                {
                    int rival = jogador == 1 ? 2 : 1;
                    TipoPersonagem tipoRival = rival == 1 ? personagem1 : personagem2;
                    int recargaEscudo = rival == 1 ? recargaInfinito1 : recargaInfinito2;
                    bool bloqueado = tipoRival == TipoPersonagem.GojoSatoru && recargaEscudo == 0;

                    // Infinito de Gojo impede dano, controle e segundo impacto
                    // de um único golpe BÁSICO; especiais não são bloqueados.
                    if (bloqueado)
                    {
                        if (rival == 1) recargaInfinito1 = 266; // ~8 segundos
                        else recargaInfinito2 = 266;
                        MostrarPassiva(rival, "INFINITO! GOLPE BLOQUEADO");
                        tremorQuadros = Math.Max(tremorQuadros, 4);
                    }
                    else
                    {
                        if (tipo == TipoPersonagem.ItadoriYuji)
                        {
                            int combo = jogador == 1 ? ++comboItadori1 : ++comboItadori2;
                            if (combo >= 3)
                            {
                                dano += 7;
                                if (jogador == 1) comboItadori1 = 0;
                                else comboItadori2 = 0;
                                MostrarPassiva(jogador, "RITMO DIVERGENTE! +7");
                            }
                        }
                        if (tipo == TipoPersonagem.NaoyaZenin &&
                            (jogador == 1 ? cargaNaoya1 : cargaNaoya2) >= 20)
                        {
                            dano += 4;
                            if (jogador == 1) cargaNaoya1 = 0;
                            else cargaNaoya2 = 0;
                            MostrarPassiva(jogador, "MOVIMENTO PROJETADO! +4");
                        }

                        if (jogador == 1)
                        {
                            vida2 = Math.Max(0, vida2 - dano);
                            congelado2 = Math.Max(congelado2, atordoamento);
                            if (repulsao > 0)
                            {
                                x2 += (direcao == 2 ? 1 : direcao == 1 ? -1 : 0) * repulsao;
                                y2 += (direcao == 0 ? 1 : direcao == 3 ? -1 : 0) * repulsao;
                            }
                            if (tipo == TipoPersonagem.ItadoriYuji) ecoItadori1 = 7;
                        }
                        else
                        {
                            vida1 = Math.Max(0, vida1 - dano);
                            congelado1 = Math.Max(congelado1, atordoamento);
                            if (repulsao > 0)
                            {
                                x += (direcao == 2 ? 1 : direcao == 1 ? -1 : 0) * repulsao;
                                y += (direcao == 0 ? 1 : direcao == 3 ? -1 : 0) * repulsao;
                            }
                            if (tipo == TipoPersonagem.ItadoriYuji) ecoItadori2 = 7;
                        }

                        if (tipo == TipoPersonagem.YutaOkkotsu)
                        {
                            int antes = jogador == 1 ? vida1 : vida2;
                            if (jogador == 1) vida1 = Math.Min(100, vida1 + 3);
                            else vida2 = Math.Min(100, vida2 + 3);
                            int recuperado = (jogador == 1 ? vida1 : vida2) - antes;
                            if (recuperado > 0)
                                MostrarPassiva(jogador, "ENERGIA REVERSA! +" + recuperado + " VIDA");
                        }
                        if (sangueConcentrado)
                            MostrarPassiva(jogador, "SANGUE CONCENTRADO! +5");
                        if (instintoDoRei)
                            MostrarPassiva(jogador, "INSTINTO DO REI! +5");
                        tremorQuadros = Math.Max(tremorQuadros,
                            tipo == TipoPersonagem.TojiFushiguro ? 6 : 4);
                        LimitarPosicoes();
                        VerificarVencedor();
                    }
                    acertou = true;
                    impactoX = tx; impactoY = ty;
                }
            }

            efeitosGolpe.Add(new EfeitoGolpe
            {
                Tipo = tipo, OrigemX = cx, OrigemY = cy,
                AlvoX = impactoX, AlvoY = impactoY,
                Direcao = direcao, Acertou = acertou
            });
            // Limite defensivo para partidas longas.
            if (efeitosGolpe.Count > 24) efeitosGolpe.RemoveAt(0);
            Invalidate();
        }

        private bool EstaNaFrente(int direcao, float dx, float dy)
        {
            return
                (direcao == 0 && dy > 0 && Math.Abs(dy) >= Math.Abs(dx)) ||
                (direcao == 3 && dy < 0 && Math.Abs(dy) >= Math.Abs(dx)) ||
                (direcao == 1 && dx < 0 && Math.Abs(dx) >= Math.Abs(dy)) ||
                (direcao == 2 && dx > 0 && Math.Abs(dx) >= Math.Abs(dy));
        }

        private void VerificarVencedor()
        {
            if (vida1 <= 0)
            {
                vida1 = 0;
                combateTerminado = true;
                vencedor = "Jogador 2 venceu!";
            }
            else if (vida2 <= 0)
            {
                vida2 = 0;
                combateTerminado = true;
                vencedor = "Jogador 1 venceu!";
            }
        }

        private void UsarPoder(int jogador)
        {
            if (combateTerminado) return;
            if (jogador == 1 && cooldownPoder1 > 0) return;
            if (jogador == 2 && cooldownPoder2 > 0) return;

            TipoPersonagem tipo =
                jogador == 1 ? personagem1 : personagem2;

            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji:
                case TipoPersonagem.YutaOkkotsu:
                case TipoPersonagem.Choso:
                case TipoPersonagem.TojiFushiguro:
                    UsarTecnicaQuarteto(jogador, tipo);
                    break;

                case TipoPersonagem.NaoyaZenin:
                    UsarTecnicaProjecao(jogador);
                    break;

                case TipoPersonagem.GojoSatoru:
                    UsarRoxoGojo(jogador);
                    break;

                case TipoPersonagem.RyomenSukuna:
                    UsarDominioSukuna(jogador);
                    break;

                case TipoPersonagem.MegumiFushiguro:
                    UsarDezSombrasMegumi(jogador);
                    break;
            }

            VerificarVencedor();

            if (jogador == 1)
                cooldownPoder1 = ObterCooldownPoder(personagem1);
            else
                cooldownPoder2 = ObterCooldownPoder(personagem2);

            Invalidate();
        }

        // Os quatro personagens possuem técnicas próprias com duração, alcance,
        // impacto, visual e cooldown, sem depender de bibliotecas Windows.
        private void UsarTecnicaQuarteto(int jogador, TipoPersonagem tipo)
        {
            cutsceneQuartetoAtiva = true;
            quartetoJogador = jogador;
            quartetoTipo = tipo;
            quartetoEtapa = 0;
            quartetoQuadro = 0;
            quartetoImpactoAplicado = false;
            quartetoInicioX = jogador == 1 ? x : x2;
            quartetoInicioY = jogador == 1 ? y : y2;
            quartetoAlvoX = jogador == 1 ? x2 : x;
            quartetoAlvoY = jogador == 1 ? y2 : y;

            float dx = quartetoAlvoX - quartetoInicioX;
            float dy = quartetoAlvoY - quartetoInicioY;
            float distancia = (float)Math.Sqrt(dx * dx + dy * dy);

            float alcance = tipo switch
            {
                TipoPersonagem.ItadoriYuji => 200f,  // Black Flash
                TipoPersonagem.YutaOkkotsu => 365f,  // Rika e energia amaldiçoada
                TipoPersonagem.Choso => 480f,  // Sangue Perfurante: projétil de longo alcance
                TipoPersonagem.TojiFushiguro => 275f, // Investida com a Lança Invertida do Céu
                _ => 0f
            };
            quartetoAcertou = distancia <= alcance;

            if (distancia > 0.01f)
                AtualizarDirecao((int)Math.Round(dx), (int)Math.Round(dy), jogador);
        }

        private void AtualizarCutsceneQuarteto()
        {
            quartetoQuadro++;
            if (quartetoEtapa == 0)
            {
                if (quartetoQuadro >= QuadrosPreparacaoQuarteto)
                {
                    quartetoEtapa = 1;
                    quartetoQuadro = 0;
                }
                return;
            }

            if (quartetoEtapa == 1)
            {
                if (quartetoQuadro >= QuadrosGolpeQuarteto)
                {
                    if (!quartetoImpactoAplicado)
                    {
                        AplicarTecnicaQuarteto();
                        quartetoImpactoAplicado = true;
                    }
                    quartetoEtapa = 2;
                    quartetoQuadro = 0;
                }
                return;
            }

            if (quartetoQuadro >= QuadrosFinalQuarteto)
            {
                cutsceneQuartetoAtiva = false;
                quartetoEtapa = 0;
                quartetoQuadro = 0;
                LimitarPosicoes();
            }
        }

        private void AplicarTecnicaQuarteto()
        {
            if (!quartetoAcertou)
                return;

            int dano = 0;
            int atordoamento = 0;
            switch (quartetoTipo)
            {
                case TipoPersonagem.ItadoriYuji:
                    dano = 25;
                    atordoamento = 14;
                    break;

                case TipoPersonagem.YutaOkkotsu:
                    dano = 19;
                    atordoamento = 8;
                    // Rika fortalece o próximo golpe físico de Yuta.
                    if (quartetoJogador == 1)
                    {
                        vida1 = Math.Min(100, vida1 + 8);
                        ataqueYuta1 = true;
                    }
                    else
                    {
                        vida2 = Math.Min(100, vida2 + 8);
                        ataqueYuta2 = true;
                    }
                    break;

                case TipoPersonagem.Choso:
                    dano = 23;
                    atordoamento = 18;
                    break;

                case TipoPersonagem.TojiFushiguro:
                    dano = 29;
                    atordoamento = 10;
                    break;
            }

            // Itadori avança no Black Flash; Toji se aproxima para atacar com a lança.
            if (quartetoTipo == TipoPersonagem.ItadoriYuji ||
                quartetoTipo == TipoPersonagem.TojiFushiguro)
            {
                float alvoX = quartetoJogador == 1 ? x2 : x;
                float alvoY = quartetoJogador == 1 ? y2 : y;
                float origemX = quartetoJogador == 1 ? x : x2;
                float lado = origemX <= alvoX ? -1f : 1f;
                float novaX = alvoX + lado * 45f;
                if (quartetoJogador == 1)
                {
                    x = novaX;
                    y = alvoY;
                    direcao1 = lado < 0 ? 2 : 1;
                    ataque1 = 10;
                }
                else
                {
                    x2 = novaX;
                    y2 = alvoY;
                    direcao2 = lado < 0 ? 2 : 1;
                    ataque2 = 10;
                }
            }
            else if (quartetoJogador == 1)
            {
                ataque1 = 10;
            }
            else
            {
                ataque2 = 10;
            }

            if (quartetoJogador == 1)
            {
                vida2 = Math.Max(0, vida2 - dano);
                congelado2 = Math.Max(congelado2, atordoamento);
            }
            else
            {
                vida1 = Math.Max(0, vida1 - dano);
                congelado1 = Math.Max(congelado1, atordoamento);
            }

            LimitarPosicoes();
            tremorQuadros = Math.Max(tremorQuadros, 10);
            VerificarVencedor();
        }

        // Inicia uma sequência cinematográfica para a Técnica de Projeção.
        // A movimentação é animada pelo TimerJogo_Tick em vez de acontecer instantaneamente.
        private void UsarTecnicaProjecao(int jogador)
        {
            cutsceneNaoyaAtiva = true;
            cutsceneNaoyaJogador = jogador;
            cutsceneNaoyaEtapa = 0;
            cutsceneNaoyaQuadro = 0;
            naoyaImpactoAplicado = false;

            naoyaInicioX = jogador == 1 ? x : x2;
            naoyaInicioY = jogador == 1 ? y : y2;
            naoyaAlvoX = jogador == 1 ? x2 : x;
            naoyaAlvoY = jogador == 1 ? y2 : y;

            float dx = naoyaAlvoX - naoyaInicioX;
            float dy = naoyaAlvoY - naoyaInicioY;
            float distancia = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distancia > 0.01f)
            {
                AtualizarDirecao((int)Math.Round(dx), (int)Math.Round(dy), jogador);
                // Naoya tenta parar perto do rival, sem atravessar o personagem.
                float deslocamento = Math.Min(180f, Math.Max(0f, distancia - 44f));
                naoyaFimX = naoyaInicioX + dx / distancia * deslocamento;
                naoyaFimY = naoyaInicioY + dy / distancia * deslocamento;
            }
            else
            {
                naoyaFimX = naoyaInicioX;
                naoyaFimY = naoyaInicioY;
            }

            naoyaFimX = Math.Max(0, Math.Min(naoyaFimX, ClientSize.Width - 32));
            naoyaFimY = Math.Max(100, Math.Min(naoyaFimY, ClientSize.Height - 44));

            float distanciaFinalX = naoyaAlvoX - naoyaFimX;
            float distanciaFinalY = naoyaAlvoY - naoyaFimY;
            float distanciaFinal = (float)Math.Sqrt(
                distanciaFinalX * distanciaFinalX + distanciaFinalY * distanciaFinalY);
            naoyaAcertou = distanciaFinal <= 68f;

            // Liga o rastro visual durante a preparação.
            if (jogador == 1) dashVisual1 = 8;
            else dashVisual2 = 8;
        }

        private void AtualizarCutsceneNaoya()
        {
            // Mantém a aura visível enquanto a sequência está em andamento.
            if (cutsceneNaoyaJogador == 1) dashVisual1 = 6;
            else dashVisual2 = 6;

            cutsceneNaoyaQuadro++;

            if (cutsceneNaoyaEtapa == 0)
            {
                // Pequena preparação antes da arrancada.
                if (cutsceneNaoyaQuadro >= QuadrosPreparacaoNaoya)
                {
                    cutsceneNaoyaEtapa = 1;
                    cutsceneNaoyaQuadro = 0;
                }
                return;
            }

            if (cutsceneNaoyaEtapa == 1)
            {
                // Curva de aceleração: começa controlado e termina muito rápido.
                float t = Math.Min(1f, (float)cutsceneNaoyaQuadro / QuadrosAvancoNaoya);
                float progresso = 1f - (float)Math.Pow(1f - t, 3);
                float novoX = naoyaInicioX + (naoyaFimX - naoyaInicioX) * progresso;
                float novoY = naoyaInicioY + (naoyaFimY - naoyaInicioY) * progresso;

                if (cutsceneNaoyaJogador == 1)
                {
                    x = novoX;
                    y = novoY;
                }
                else
                {
                    x2 = novoX;
                    y2 = novoY;
                }

                if (cutsceneNaoyaQuadro >= QuadrosAvancoNaoya)
                {
                    if (cutsceneNaoyaJogador == 1)
                    {
                        x = naoyaFimX;
                        y = naoyaFimY;
                        ataque1 = 10;
                    }
                    else
                    {
                        x2 = naoyaFimX;
                        y2 = naoyaFimY;
                        ataque2 = 10;
                    }

                    // O impacto acontece uma única vez, no fim da animação de avanço.
                    if (naoyaAcertou && !naoyaImpactoAplicado)
                    {
                        if (cutsceneNaoyaJogador == 1)
                        {
                            vida2 = Math.Max(0, vida2 - 18);
                            congelado2 = Math.Max(congelado2, 33);
                        }
                        else
                        {
                            vida1 = Math.Max(0, vida1 - 18);
                            congelado1 = Math.Max(congelado1, 33);
                        }
                        naoyaImpactoAplicado = true;
                        VerificarVencedor();
                    }

                    cutsceneNaoyaEtapa = 2;
                    cutsceneNaoyaQuadro = 0;
                }
                return;
            }

            if (cutsceneNaoyaEtapa == 2 && cutsceneNaoyaQuadro >= QuadrosImpactoNaoya)
            {
                cutsceneNaoyaAtiva = false;
                cutsceneNaoyaEtapa = 0;
                cutsceneNaoyaQuadro = 0;
                dashVisual1 = 0;
                dashVisual2 = 0;
                LimitarPosicoes();
            }
        }

        // Gojo dispara uma versão lúdica do Hollow Purple (Roxo).
        // A animação continua mesmo após o dano, para concluir a cutscene.
        private void UsarRoxoGojo(int jogador)
        {
            cutsceneGojoAtiva = true;
            gojoJogador = jogador;
            gojoEtapa = 0;
            gojoQuadro = 0;
            gojoImpactoAplicado = false;

            gojoInicioX = jogador == 1 ? x : x2;
            gojoInicioY = jogador == 1 ? y : y2;
            gojoAlvoX = jogador == 1 ? x2 : x;
            gojoAlvoY = jogador == 1 ? y2 : y;

            float dx = gojoAlvoX - gojoInicioX;
            float dy = gojoAlvoY - gojoInicioY;
            float distancia = (float)Math.Sqrt(dx * dx + dy * dy);
            gojoAcertou = distancia <= 520f;

            if (distancia > 0.01f)
                AtualizarDirecao((int)Math.Round(dx), (int)Math.Round(dy), jogador);
        }

        private void AtualizarCutsceneGojo()
        {
            gojoQuadro++;

            if (gojoEtapa == 0)
            {
                if (gojoQuadro >= QuadrosPreparacaoGojo)
                {
                    gojoEtapa = 1;
                    gojoQuadro = 0;
                }
                return;
            }

            if (gojoEtapa == 1)
            {
                if (gojoQuadro >= QuadrosDisparoGojo)
                {
                    if (gojoAcertou && !gojoImpactoAplicado)
                    {
                        if (gojoJogador == 1)
                        {
                            vida2 = Math.Max(0, vida2 - 28);
                            congelado2 = Math.Max(congelado2, 20);
                            x2 = Math.Max(0, Math.Min(ClientSize.Width - 32,
                                x2 + (direcao1 == 2 ? 42 : direcao1 == 1 ? -42 : 0)));
                            y2 = Math.Max(100, Math.Min(ClientSize.Height - 44,
                                y2 + (direcao1 == 0 ? 42 : direcao1 == 3 ? -42 : 0)));
                        }
                        else
                        {
                            vida1 = Math.Max(0, vida1 - 28);
                            congelado1 = Math.Max(congelado1, 20);
                            x = Math.Max(0, Math.Min(ClientSize.Width - 32,
                                x + (direcao2 == 2 ? 42 : direcao2 == 1 ? -42 : 0)));
                            y = Math.Max(100, Math.Min(ClientSize.Height - 44,
                                y + (direcao2 == 0 ? 42 : direcao2 == 3 ? -42 : 0)));
                        }
                        gojoImpactoAplicado = true;
                        VerificarVencedor();
                    }
                    gojoEtapa = 2;
                    gojoQuadro = 0;
                }
                return;
            }

            if (gojoEtapa == 2 && gojoQuadro >= QuadrosImpactoGojo)
            {
                cutsceneGojoAtiva = false;
                gojoEtapa = 0;
                gojoQuadro = 0;
                LimitarPosicoes();
            }
        }

        // Sukuna expande o Santuário Malevolente e atinge inimigos próximos
        // com cortes sucessivos. O alcance da área é de 300 pixels.
        private void UsarDominioSukuna(int jogador)
        {
            cutsceneSukunaAtiva = true;
            sukunaJogador = jogador;
            sukunaEtapa = 0;
            sukunaQuadro = 0;
            sukunaImpactoAplicado = false;

            sukunaInicioX = jogador == 1 ? x : x2;
            sukunaInicioY = jogador == 1 ? y : y2;
            sukunaAlvoX = jogador == 1 ? x2 : x;
            sukunaAlvoY = jogador == 1 ? y2 : y;

            float dx = sukunaAlvoX - sukunaInicioX;
            float dy = sukunaAlvoY - sukunaInicioY;
            sukunaAcertou = Math.Sqrt(dx * dx + dy * dy) <= 300f;
        }

        private void AtualizarCutsceneSukuna()
        {
            sukunaQuadro++;

            if (sukunaEtapa == 0)
            {
                if (sukunaQuadro >= QuadrosPreparacaoSukuna)
                {
                    sukunaEtapa = 1;
                    sukunaQuadro = 0;
                }
                return;
            }

            if (sukunaEtapa == 1)
            {
                // O dano ocorre durante a sequência de cortes, uma única vez.
                if (sukunaQuadro >= 10 && !sukunaImpactoAplicado)
                {
                    if (sukunaAcertou)
                    {
                        if (sukunaJogador == 1)
                        {
                            vida2 = Math.Max(0, vida2 - 30);
                            congelado2 = Math.Max(congelado2, 25);
                        }
                        else
                        {
                            vida1 = Math.Max(0, vida1 - 30);
                            congelado1 = Math.Max(congelado1, 25);
                        }
                        VerificarVencedor();
                    }
                    sukunaImpactoAplicado = true;
                }

                if (sukunaQuadro >= QuadrosCortesSukuna)
                {
                    sukunaEtapa = 2;
                    sukunaQuadro = 0;
                }
                return;
            }

            if (sukunaEtapa == 2 && sukunaQuadro >= QuadrosFimSukuna)
            {
                cutsceneSukunaAtiva = false;
                sukunaEtapa = 0;
                sukunaQuadro = 0;
            }
        }

        // Megumi sorteia uma única sombra a cada ativação da Técnica das Dez Sombras.
        private void UsarDezSombrasMegumi(int jogador)
        {
            megumiSombraIndice = SorteioSombrasMegumi.Next(NomesSombras.Length);
            cutsceneMegumiAtiva = true;
            megumiJogador = jogador;
            megumiEtapa = 0;
            megumiQuadro = 0;
            megumiImpactoAplicado = false;

            megumiInicioX = jogador == 1 ? x : x2;
            megumiInicioY = jogador == 1 ? y : y2;
            megumiAlvoX = jogador == 1 ? x2 : x;
            megumiAlvoY = jogador == 1 ? y2 : y;

            float dx = megumiAlvoX - megumiInicioX;
            float dy = megumiAlvoY - megumiInicioY;
            float distancia = (float)Math.Sqrt(dx * dx + dy * dy);

            // A sombra invocada passa a perseguir o oponente depois da cutscene,
            // portanto o alcance inicial não impede a invocação.
            megumiAcertou = true;

            if (distancia > 0.01f)
                AtualizarDirecao((int)Math.Round(dx), (int)Math.Round(dy), jogador);
        }

        private void AtualizarCutsceneMegumi()
        {
            megumiQuadro++;

            if (megumiEtapa == 0)
            {
                if (megumiQuadro >= QuadrosPreparacaoMegumi)
                {
                    megumiEtapa = 1;
                    megumiQuadro = 0;
                }
                return;
            }

            if (megumiEtapa == 1)
            {
                // O efeito da sombra sorteada acontece uma única vez no momento do impacto.
                if (megumiQuadro >= 28 && !megumiImpactoAplicado)
                {
                    AplicarEfeitoSombraMegumi();
                    megumiImpactoAplicado = true;
                }

                if (megumiQuadro >= QuadrosInvocacaoMegumi)
                {
                    megumiEtapa = 2;
                    megumiQuadro = 0;
                }
                return;
            }

            if (megumiEtapa == 2 && megumiQuadro >= QuadrosDominioMegumi)
            {
                cutsceneMegumiAtiva = false;
                megumiEtapa = 0;
                megumiQuadro = 0;
            }
        }

        // A cutscene invoca o shikigami; ele permanece na arena e luta por conta própria.
        private void AplicarEfeitoSombraMegumi()
        {
            float donoX = megumiJogador == 1 ? x : x2;
            float donoY = megumiJogador == 1 ? y : y2;
            float alvoX = megumiJogador == 1 ? x2 : x;

            SombraInvocada nova = new SombraInvocada();
            nova.Ativa = true;
            nova.Indice = megumiSombraIndice;
            nova.Dono = megumiJogador;
            nova.Vida = 20;
            nova.VidaMaxima = 20;
            nova.TicksRestantes = 270; // cerca de 8,1 segundos após a cutscene
            nova.CooldownAtaque = 14;
            nova.X = donoX + (alvoX >= donoX ? -42f : 42f);
            nova.Y = donoY + 4f;
            nova.X = Math.Max(4f, Math.Min(nova.X, ClientSize.Width - 42f));
            nova.Y = Math.Max(102f, Math.Min(nova.Y, ClientSize.Height - 48f));

            // Cada shikigami tem um perfil diferente, mas todos perseguem e atacam.
            switch (nova.Indice)
            {
                case 0: nova.Velocidade = 3.8f; nova.Dano = 8; nova.IntervaloAtaque = 28; break; // Cães Divinos
                case 1: nova.Velocidade = 3.2f; nova.Dano = 7; nova.IntervaloAtaque = 32; break; // Nue
                case 2: nova.Velocidade = 2.8f; nova.Dano = 6; nova.IntervaloAtaque = 30; break; // Sapo
                case 3: nova.Velocidade = 3.4f; nova.Dano = 8; nova.IntervaloAtaque = 31; break; // Serpente
                case 4: nova.Velocidade = 2.0f; nova.Dano = 12; nova.IntervaloAtaque = 43; break; // Max Elefante
                case 5: nova.Velocidade = 4.2f; nova.Dano = 4; nova.IntervaloAtaque = 17; break; // Coelhos
                case 6: nova.Velocidade = 3.0f; nova.Dano = 5; nova.IntervaloAtaque = 28; break; // Veado Redondo
                case 7: nova.Velocidade = 4.5f; nova.Dano = 14; nova.IntervaloAtaque = 42; break; // Boi Perfurante
                case 8: nova.Velocidade = 3.6f; nova.Dano = 10; nova.IntervaloAtaque = 29; break; // Tigre Funeral
                case 9: nova.Velocidade = 3.1f; nova.Dano = 12; nova.IntervaloAtaque = 34; break; // Mahoraga
            }

            // Passiva de Megumi: todos os shikigamis nascem mais resistentes
            // e causam mais dano, independentemente da espécie sorteada.
            nova.VidaMaxima += 10;
            nova.Vida = nova.VidaMaxima;
            nova.Dano += 2;
            MostrarPassiva(megumiJogador, "VÍNCULO DAS SOMBRAS! +VIDA/+DANO");

            if (megumiJogador == 1)
                sombraJogador1 = nova;
            else
                sombraJogador2 = nova;
        }

        private void AtualizarSombrasInvocadas()
        {
            AtualizarSombraInvocada(sombraJogador1);
            AtualizarSombraInvocada(sombraJogador2);
        }

        private void AtualizarSombraInvocada(SombraInvocada sombra)
        {
            if (sombra == null || !sombra.Ativa)
                return;

            sombra.TicksRestantes--;
            if (sombra.TicksRestantes <= 0 || sombra.Vida <= 0)
            {
                sombra.Ativa = false;
                return;
            }

            if (sombra.CooldownAtaque > 0) sombra.CooldownAtaque--;
            if (sombra.QuadrosAtaque > 0) sombra.QuadrosAtaque--;

            float alvoX = sombra.Dono == 1 ? x2 : x;
            float alvoY = sombra.Dono == 1 ? y2 : y;
            float dx = (alvoX + 16f) - (sombra.X + 16f);
            float dy = (alvoY + 22f) - (sombra.Y + 22f);
            float distancia = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distancia > 44f)
            {
                float divisor = Math.Max(0.001f, distancia);
                sombra.X += dx / divisor * sombra.Velocidade;
                sombra.Y += dy / divisor * sombra.Velocidade;

                if (Math.Abs(dx) >= Math.Abs(dy))
                    sombra.Direcao = dx < 0 ? 1 : 2;
                else
                    sombra.Direcao = dy < 0 ? 3 : 0;
            }
            else if (sombra.CooldownAtaque <= 0)
            {
                sombra.QuadrosAtaque = 8;
                sombra.CooldownAtaque = sombra.IntervaloAtaque;

                if (sombra.Dono == 1)
                {
                    vida2 = Math.Max(0, vida2 - sombra.Dano);
                    if (sombra.Indice == 1) congelado2 = Math.Max(congelado2, 8);  // Nue
                    if (sombra.Indice == 2) congelado2 = Math.Max(congelado2, 12); // Sapo
                    if (sombra.Indice == 8) congelado2 = Math.Max(congelado2, 8);  // Tigre
                    if (sombra.Indice == 9) congelado2 = Math.Max(congelado2, 10); // Mahoraga
                    if (sombra.Indice == 6) vida1 = Math.Min(100, vida1 + 3); // Veado: ataque e pequeno suporte
                }
                else
                {
                    vida1 = Math.Max(0, vida1 - sombra.Dano);
                    if (sombra.Indice == 1) congelado1 = Math.Max(congelado1, 8);
                    if (sombra.Indice == 2) congelado1 = Math.Max(congelado1, 12);
                    if (sombra.Indice == 8) congelado1 = Math.Max(congelado1, 8);
                    if (sombra.Indice == 9) congelado1 = Math.Max(congelado1, 10);
                    if (sombra.Indice == 6) vida2 = Math.Min(100, vida2 + 3);
                }

                VerificarVencedor();
            }

            sombra.X = Math.Max(0f, Math.Min(sombra.X, ClientSize.Width - 42f));
            sombra.Y = Math.Max(101f, Math.Min(sombra.Y, ClientSize.Height - 48f));
        }

        private void DesenharSombraInvocada(Graphics g, SombraInvocada sombra)
        {
            if (sombra == null || !sombra.Ativa)
                return;

            float cx = sombra.X + 16f;
            float cy = sombra.Y + 22f;
            Color corDono = sombra.Dono == 1 ? Color.DeepSkyBlue : Color.LightCoral;

            using (Brush sombraChao = new SolidBrush(Color.FromArgb(110, 7, 6, 19)))
                g.FillEllipse(sombraChao, cx - 21f, cy + 13f, 42f, 10f);

            using (Pen aura = new Pen(Color.FromArgb(205, corDono), 2f))
                g.DrawEllipse(aura, cx - 25f, cy - 25f, 50f, 50f);

            if (sombra.QuadrosAtaque > 0)
            {
                using (Pen golpe = new Pen(Color.FromArgb(225, 208, 198, 255), 3f))
                {
                    if (sombra.Direcao == 1 || sombra.Direcao == 2)
                    {
                        int sinal = sombra.Direcao == 2 ? 1 : -1;
                        g.DrawLine(golpe, cx + sinal * 9f, cy - 3f, cx + sinal * 31f, cy - 13f);
                        g.DrawLine(golpe, cx + sinal * 10f, cy + 5f, cx + sinal * 35f, cy + 7f);
                    }
                    else
                    {
                        int sinal = sombra.Direcao == 0 ? 1 : -1;
                        g.DrawLine(golpe, cx - 8f, cy + sinal * 5f, cx - 17f, cy + sinal * 28f);
                        g.DrawLine(golpe, cx + 8f, cy + sinal * 5f, cx + 18f, cy + sinal * 28f);
                    }
                }
            }

            DesenharSimboloSombra(g, sombra.Indice, cx, cy, 48f, 245);

            // Barra de vida própria, como uma unidade independente da batalha.
            const float larguraBarra = 42f;
            float barraX = cx - larguraBarra / 2f;
            float barraY = sombra.Y - 12f;
            using (Brush fundo = new SolidBrush(Color.FromArgb(220, 25, 12, 30)))
                g.FillRectangle(fundo, barraX, barraY, larguraBarra, 5f);
            float preenchimento = larguraBarra * Math.Max(0, sombra.Vida) / Math.Max(1, sombra.VidaMaxima);
            using (Brush vida = new SolidBrush(corDono))
                g.FillRectangle(vida, barraX, barraY, preenchimento, 5f);
            g.DrawRectangle(Pens.White, barraX, barraY, larguraBarra, 5f);
        }

        private int ObterCooldownPoder(TipoPersonagem tipo)
        {
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji: return 175; // 5,25 segundos
                case TipoPersonagem.YutaOkkotsu: return 220; // 6,6 segundos
                case TipoPersonagem.Choso: return 190; // 5,7 segundos
                case TipoPersonagem.TojiFushiguro: return 185; // 5,55 segundos
                case TipoPersonagem.NaoyaZenin: return 180; // 5,4 segundos
                case TipoPersonagem.GojoSatoru: return 230; // 6,9 segundos
                case TipoPersonagem.RyomenSukuna: return 210; // 6,3 segundos
                case TipoPersonagem.MegumiFushiguro: return 280; // 8,4 segundos
                default: return 100;
            }
        }

        private void TimerJogo_Tick(object sender, EventArgs e)
        {
            if (telaSelecao)
            {
                Invalidate();
                return;
            }

            tempoArena++;
            if (recargaInfinito1 > 0) recargaInfinito1--;
            if (recargaInfinito2 > 0) recargaInfinito2--;
            if (avisoPassiva1 > 0) avisoPassiva1--;
            if (avisoPassiva2 > 0) avisoPassiva2--;
            if (tremorQuadros > 0) tremorQuadros--;
            for (int i = efeitosGolpe.Count - 1; i >= 0; i--)
            {
                efeitosGolpe[i].Idade++;
                if (efeitosGolpe[i].Idade >= efeitosGolpe[i].Duracao)
                    efeitosGolpe.RemoveAt(i);
            }

            // O segundo impacto de Itadori acontece alguns quadros após o primeiro.
            if (ecoItadori1 > 0 && --ecoItadori1 == 0 && !combateTerminado)
            {
                vida2 = Math.Max(0, vida2 - 5);
                tremorQuadros = Math.Max(tremorQuadros, 5);
                VerificarVencedor();
            }
            if (ecoItadori2 > 0 && --ecoItadori2 == 0 && !combateTerminado)
            {
                vida1 = Math.Max(0, vida1 - 5);
                tremorQuadros = Math.Max(tremorQuadros, 5);
                VerificarVencedor();
            }

            if (cooldown1 > 0) cooldown1--;
            if (cooldown2 > 0) cooldown2--;
            if (ataque1 > 0) ataque1--;
            if (ataque2 > 0) ataque2--;
            if (cooldownPoder1 > 0) cooldownPoder1--;
            if (cooldownPoder2 > 0) cooldownPoder2--;
            if (congelado1 > 0) congelado1--;
            if (congelado2 > 0) congelado2--;
            if (dashVisual1 > 0) dashVisual1--;
            if (dashVisual2 > 0) dashVisual2--;

            if (cutsceneQuartetoAtiva)
            {
                AtualizarCutsceneQuarteto();
                Invalidate();
                return;
            }

            if (cutsceneNaoyaAtiva)
            {
                AtualizarCutsceneNaoya();
                Invalidate();
                return;
            }

            if (cutsceneGojoAtiva)
            {
                AtualizarCutsceneGojo();
                Invalidate();
                return;
            }

            if (cutsceneSukunaAtiva)
            {
                AtualizarCutsceneSukuna();
                Invalidate();
                return;
            }

            if (cutsceneMegumiAtiva)
            {
                AtualizarCutsceneMegumi();
                Invalidate();
                return;
            }

            if (combateTerminado)
            {
                Invalidate();
                return;
            }

            AtualizarSombrasInvocadas();

            int dx = 0, dy = 0;

            if (teclas.Contains(Keys.A)) dx--;
            if (teclas.Contains(Keys.D)) dx++;
            if (teclas.Contains(Keys.W)) dy--;
            if (teclas.Contains(Keys.S)) dy++;

            andando = dx != 0 || dy != 0;

            if (congelado1 > 0)
                andando = false;
            else if (andando)
            {
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                float v1 = personagem1 == TipoPersonagem.TojiFushiguro
                    ? Velocidade * 1.25f : Velocidade;
                x += dx / d * v1;
                y += dy / d * v1;
                if (personagem1 == TipoPersonagem.NaoyaZenin)
                    cargaNaoya1 = Math.Min(20, cargaNaoya1 + 1);

                AtualizarDirecao(dx, dy, 1);

                if (++contadorAnimacao >= 4)
                {
                    quadroAnimacao = (quadroAnimacao + 1) % 2;
                    contadorAnimacao = 0;
                }
            }
            else
            {
                quadroAnimacao = 0;
                contadorAnimacao = 0;
            }

            int dx2 = 0, dy2 = 0;

            if (teclas.Contains(Keys.Left)) dx2--;
            if (teclas.Contains(Keys.Right)) dx2++;
            if (teclas.Contains(Keys.Up)) dy2--;
            if (teclas.Contains(Keys.Down)) dy2++;

            andando2 = dx2 != 0 || dy2 != 0;

            if (congelado2 > 0)
                andando2 = false;
            else if (andando2)
            {
                float d2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                float v2 = personagem2 == TipoPersonagem.TojiFushiguro
                    ? Velocidade * 1.25f : Velocidade;
                x2 += dx2 / d2 * v2;
                y2 += dy2 / d2 * v2;
                if (personagem2 == TipoPersonagem.NaoyaZenin)
                    cargaNaoya2 = Math.Min(20, cargaNaoya2 + 1);

                AtualizarDirecao(dx2, dy2, 2);

                if (++contadorAnimacao2 >= 4)
                {
                    quadroAnimacao2 = (quadroAnimacao2 + 1) % 2;
                    contadorAnimacao2 = 0;
                }
            }
            else
            {
                quadroAnimacao2 = 0;
                contadorAnimacao2 = 0;
            }

            LimitarPosicoes();
            Invalidate();
        }

        private void AtualizarDirecao(int dx, int dy, int jogador)
        {
            int dir;

            if (Math.Abs(dx) >= Math.Abs(dy))
                dir = dx < 0 ? 1 : 2;
            else
                dir = dy < 0 ? 3 : 0;

            if (jogador == 1) direcao1 = dir;
            else direcao2 = dir;
        }

        private void LimitarPosicoes()
        {
            x = Math.Max(0, Math.Min(x, ClientSize.Width - 32));
            y = Math.Max(100, Math.Min(y, ClientSize.Height - 44));
            x2 = Math.Max(0, Math.Min(x2, ClientSize.Width - 32));
            y2 = Math.Max(100, Math.Min(y2, ClientSize.Height - 44));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            if (telaSelecao)
            {
                GraphicsState estado = g.Save();
                float escala = Math.Min((float)ClientSize.Width / LarguraBase,
                    (float)ClientSize.Height / AlturaBase);
                g.TranslateTransform((ClientSize.Width - LarguraBase * escala) / 2f,
                    (ClientSize.Height - AlturaBase * escala) / 2f);
                g.ScaleTransform(escala, escala);
                DesenharTelaSelecao(g);
                g.Restore(estado);
                return;
            }

            GraphicsState mundo = g.Save();
            if (tremorQuadros > 0)
            {
                float intensidade = tremorQuadros * 0.45f;
                g.TranslateTransform((float)Math.Sin(tempoArena * 2.6f) * intensidade,
                    (float)Math.Cos(tempoArena * 3.1f) * intensidade);
            }
            if (cutsceneNaoyaAtiva && cutsceneNaoyaEtapa == 2)
            {
                float intensidade = naoyaAcertou && cutsceneNaoyaQuadro < 8 ? 6f : 2f;
                g.TranslateTransform((float)Math.Sin(cutsceneNaoyaQuadro * 2.7) * intensidade,
                    (float)Math.Cos(cutsceneNaoyaQuadro * 3.1) * intensidade * 0.6f);
            }

            DesenharArena(g);
            g.SmoothingMode = SmoothingMode.None;

            // Personagens e unidades independentes ficam acima do piso.
            DesenharPersonagem(g, x, y, personagem1,
                andando, quadroAnimacao, direcao1, ataque1, congelado1, ataqueYuta1, dashVisual1);
            DesenharPersonagem(g, x2, y2, personagem2,
                andando2, quadroAnimacao2, direcao2, ataque2, congelado2, ataqueYuta2, dashVisual2);
            DesenharSombraInvocada(g, sombraJogador1);
            DesenharSombraInvocada(g, sombraJogador2);
            DesenharAuraPassiva(g, x, y, 1, personagem1);
            DesenharAuraPassiva(g, x2, y2, 2, personagem2);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            DesenharEfeitosGolpe(g);
            g.Restore(mundo);

            DesenharHudCombate(g, 16, 14, 1, personagem1, vida1, cooldownPoder1);
            DesenharHudCombate(g, ClientSize.Width - 306, 14, 2,
                personagem2, vida2, cooldownPoder2);

            using (Font titulo = new Font("Segoe UI", 13f, FontStyle.Bold))
            using (Font micro = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (StringFormat centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (Brush branco = new SolidBrush(Color.FromArgb(230, 235, 244, 255)))
            using (Brush opaco = new SolidBrush(Color.FromArgb(170, 171, 188, 212)))
            {
                g.DrawString("VS", titulo, branco,
                    new RectangleF(ClientSize.Width / 2f - 23, 22, 46, 34), centro);
                g.DrawString("JUJUTSU · ARENA 01", micro, opaco,
                    new RectangleF(ClientSize.Width / 2f - 92, 56, 184, 21), centro);
            }

            using (Brush fundo = new SolidBrush(Color.FromArgb(212, 10, 16, 30)))
            using (Pen borda = new Pen(Color.FromArgb(110, 87, 116, 154), 1f))
            {
                g.FillRectangle(fundo, 14, ClientSize.Height - 45, 310, 29);
                g.FillRectangle(fundo, ClientSize.Width - 339, ClientSize.Height - 45, 325, 29);
                g.DrawRectangle(borda, 14, ClientSize.Height - 45, 310, 29);
                g.DrawRectangle(borda, ClientSize.Width - 339, ClientSize.Height - 45, 325, 29);
            }
            using (Font comandos = new Font("Segoe UI", 9f, FontStyle.Bold))
            {
                g.DrawString("P1   WASD · MOVER     F · GOLPE     Q · ESPECIAL",
                    comandos, Brushes.White, 22, ClientSize.Height - 40);
                g.DrawString("P2   SETAS · MOVER    ENTER · GOLPE    SHIFT · ESPECIAL",
                    comandos, Brushes.White, ClientSize.Width - 331, ClientSize.Height - 40);
            }

            if (combateTerminado) DesenharTelaVitoria(g);
            if (cutsceneQuartetoAtiva) DesenharCutsceneQuarteto(g);
            if (cutsceneNaoyaAtiva) DesenharCutsceneNaoya(g);
            if (cutsceneGojoAtiva) DesenharCutsceneGojo(g);
            if (cutsceneSukunaAtiva) DesenharCutsceneSukuna(g);
            if (cutsceneMegumiAtiva) DesenharCutsceneMegumi(g);
        }

        private void DesenharArena(Graphics g)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            g.Clear(Color.FromArgb(12, 20, 34));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Chão de pedra escura com faixas luminosas e recortes metálicos.
            using (Brush centro = new SolidBrush(Color.FromArgb(19, 32, 50)))
                g.FillRectangle(centro, 18, 105, w - 36, h - 157);

            using (Pen grade = new Pen(Color.FromArgb(48, 80, 113, 144), 1f))
            using (Pen inter = new Pen(Color.FromArgb(24, 128, 154, 181), 1f))
            {
                for (int xx = 18; xx < w; xx += 64)
                {
                    g.DrawLine(grade, xx, 108, xx, h - 52);
                    g.DrawLine(inter, xx + 5, 108, xx + 5, h - 52);
                }
                for (int yy = 108; yy < h - 52; yy += 64)
                {
                    g.DrawLine(grade, 18, yy, w - 18, yy);
                    g.DrawLine(inter, 18, yy + 5, w - 18, yy + 5);
                }
            }

            // Símbolo central do duelo, círculos e linhas de energia.
            float cx = w * 0.5f, cy = h * 0.54f;
            using (Pen externo = new Pen(Color.FromArgb(82, 90, 170, 192), 3f))
            using (Pen interno = new Pen(Color.FromArgb(70, 185, 107, 167), 2f))
            using (Pen divisao = new Pen(Color.FromArgb(60, 105, 140, 191), 1.5f))
            {
                g.DrawEllipse(externo, cx - 162, cy - 162, 324, 324);
                g.DrawEllipse(interno, cx - 112, cy - 112, 224, 224);
                g.DrawEllipse(divisao, cx - 58, cy - 58, 116, 116);
                g.DrawLine(divisao, cx - 185, cy, cx + 185, cy);
                g.DrawLine(divisao, cx, cy - 185, cx, cy + 185);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4 + tempoArena * 0.0025;
                    float ux = (float)Math.Cos(a), uy = (float)Math.Sin(a);
                    g.DrawLine(interno, cx + ux * 130, cy + uy * 130,
                        cx + ux * 152, cy + uy * 152);
                }
            }
            using (Brush nucleo = new SolidBrush(Color.FromArgb(42, 170, 213, 228)))
                g.FillEllipse(nucleo, cx - 14, cy - 14, 28, 28);

            // Partículas ambiente determinísticas, sem alocação aleatória a cada quadro.
            for (int i = 0; i < 29; i++)
            {
                float px = 26 + ((i * 163 + 29) % Math.Max(1, w - 52));
                float py = 128 + ((i * 113 + 57) % Math.Max(1, h - 213));
                float oscilacao = (float)Math.Sin(tempoArena * 0.045 + i * 1.7) * 5f;
                using (Brush brilho = new SolidBrush(Color.FromArgb(35 + i % 4 * 17, 145, 219, 242)))
                    g.FillEllipse(brilho, px, py + oscilacao, 2f + i % 3, 2f + i % 3);
            }

            using (Brush topo = new SolidBrush(Color.FromArgb(235, 7, 12, 24)))
            using (Brush rodape = new SolidBrush(Color.FromArgb(233, 8, 14, 26)))
            {
                g.FillRectangle(topo, 0, 0, w, 104);
                g.FillRectangle(rodape, 0, h - 57, w, 57);
            }
            using (Pen moldura = new Pen(Color.FromArgb(130, 70, 115, 150), 2f))
            using (Pen detalhe = new Pen(Color.FromArgb(85, 188, 113, 155), 2f))
            {
                g.DrawRectangle(moldura, 9, 104, w - 18, h - 160);
                g.DrawLine(detalhe, 22, 104, 205, 104);
                g.DrawLine(detalhe, w - 205, 104, w - 22, 104);
                g.DrawLine(detalhe, w * .5f - 80, h - 57, w * .5f + 80, h - 57);
            }
        }

        private void DesenharHudCombate(Graphics g, float bx, float by,
            int jogador, TipoPersonagem tipo, int vida, int recarga)
        {
            const int largura = 290;
            Color cor = CorPersonagem(tipo);
            float progresso = Math.Max(0f, Math.Min(1f, vida / 100f));
            float prontidao = Math.Max(0f, Math.Min(1f,
                1f - recarga / (float)Math.Max(1, ObterCooldownPoder(tipo))));

            using (Brush fundo = new SolidBrush(Color.FromArgb(247, 18, 25, 41)))
            using (Pen borda = new Pen(Color.FromArgb(132, cor), 1.5f))
            using (Brush linha = new SolidBrush(cor))
            using (Brush vazio = new SolidBrush(Color.FromArgb(68, 74, 89)))
            using (Brush poder = new SolidBrush(Color.FromArgb(165, 202, 225, 252)))
            {
                g.FillRectangle(fundo, bx, by, largura, 83);
                g.DrawRectangle(borda, bx, by, largura, 83);
                g.FillRectangle(linha, bx, by, 5, 83);
                g.FillRectangle(vazio, bx + 14, by + 40, largura - 28, 13);
                g.FillRectangle(linha, bx + 14, by + 40, (largura - 28) * progresso, 13);
                g.FillRectangle(vazio, bx + 14, by + 65, largura - 28, 5);
                g.FillRectangle(poder, bx + 14, by + 65, (largura - 28) * prontidao, 5);
            }
            using (Font nome = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (Font miudo = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (Brush branco = new SolidBrush(Color.FromArgb(242, 244, 251, 255)))
            using (Brush cinza = new SolidBrush(Color.FromArgb(198, 176, 193, 212)))
            {
                g.DrawString("P" + jogador + "  " + NomePersonagem(tipo), nome, branco, bx + 14, by + 8);
                g.DrawString(vida + " / 100", miudo, branco, bx + largura - 65, by + 25);
                g.DrawString(recarga == 0 ? "ESPECIAL PRONTO" : "ENERGIA AMALDIÇOADA",
                    miudo, cinza, bx + 14, by + 54);
            }

            // Barra pequena abaixo do HUD com o estado da passiva.
            int aviso = jogador == 1 ? avisoPassiva1 : avisoPassiva2;
            string mensagem = jogador == 1 ? textoPassiva1 : textoPassiva2;
            bool ativacao = aviso > 0;
            string estado = ativacao ? mensagem :
                NomePassiva(tipo).ToUpperInvariant() + " · " + EstadoPassiva(jogador, tipo);
            using (Brush fundoPassiva = new SolidBrush(Color.FromArgb(234, 12, 21, 36)))
            using (Pen bordaPassiva = new Pen(Color.FromArgb(ativacao ? 210 : 115, cor), 1f))
            using (Brush textoPassiva = new SolidBrush(
                ativacao ? Color.FromArgb(255, 251, 220, 128) : Color.FromArgb(240, 205, 226, 242)))
            using (Font fontePassiva = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (StringFormat alinhamento = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                RectangleF placa = new RectangleF(bx, by + 86, largura, 23);
                g.FillRectangle(fundoPassiva, placa);
                g.DrawRectangle(bordaPassiva, placa.X, placa.Y, placa.Width, placa.Height);
                g.DrawString(estado, fontePassiva, textoPassiva,
                    new RectangleF(bx + 4, by + 87, largura - 8, 21), alinhamento);
            }
        }

        // Sinal visual discreto quando a passiva está pronta ou fortalecida.
        private void DesenharAuraPassiva(Graphics g, float px, float py,
            int jogador, TipoPersonagem tipo)
        {
            bool ativa = (jogador == 1 ? avisoPassiva1 : avisoPassiva2) > 0;
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji:
                    ativa |= (jogador == 1 ? comboItadori1 : comboItadori2) >= 2;
                    break;
                case TipoPersonagem.Choso:
                    ativa |= (jogador == 1 ? vida1 : vida2) <= 50;
                    break;
                case TipoPersonagem.TojiFushiguro:
                    ativa |= (jogador == 1 ? andando : andando2);
                    break;
                case TipoPersonagem.NaoyaZenin:
                    ativa |= (jogador == 1 ? cargaNaoya1 : cargaNaoya2) >= 20;
                    break;
                case TipoPersonagem.GojoSatoru:
                    ativa |= (jogador == 1 ? recargaInfinito1 : recargaInfinito2) == 0;
                    break;
                case TipoPersonagem.RyomenSukuna:
                    ativa |= (jogador == 1 ? vida2 : vida1) <= 40;
                    break;
                case TipoPersonagem.MegumiFushiguro:
                    SombraInvocada sombra = jogador == 1 ? sombraJogador1 : sombraJogador2;
                    ativa |= sombra != null && sombra.Ativa;
                    break;
            }
            if (!ativa) return;

            Color cor = CorPersonagem(tipo);
            float brilho = 3f + (float)Math.Sin(tempoArena * 0.16) * 2f;
            using (Pen anel = new Pen(Color.FromArgb(180, cor), 2f))
            using (Pen segundo = new Pen(Color.FromArgb(80, cor), 1f))
            {
                g.DrawEllipse(anel, px - 7f - brilho, py + 24f - brilho * 0.4f,
                    47f + brilho * 2f, 13f + brilho);
                g.DrawEllipse(segundo, px - 12f - brilho, py - 5f - brilho,
                    56f + brilho * 2f, 52f + brilho * 2f);
            }
        }

        private void DesenharEfeitosGolpe(Graphics g)
        {
            foreach (EfeitoGolpe e in efeitosGolpe)
            {
                float t = e.Idade / (float)Math.Max(1, e.Duracao);
                float progresso = Math.Min(1f, t * 2.4f);
                float ex = e.OrigemX + (e.AlvoX - e.OrigemX) * progresso;
                float ey = e.OrigemY + (e.AlvoY - e.OrigemY) * progresso;
                Color cor = CorPersonagem(e.Tipo);
                int alpha = Math.Max(0, Math.Min(245, (int)(220 * (1f - t))));
                float raio = 9f + 35f * t;

                using (Pen brilho = new Pen(Color.FromArgb(alpha, cor), 4f))
                using (Pen fino = new Pen(Color.FromArgb(alpha, 235, 244, 255), 2f))
                {
                    brilho.StartCap = LineCap.Round;
                    brilho.EndCap = LineCap.Round;
                    switch (e.Tipo)
                    {
                        case TipoPersonagem.ItadoriYuji:
                            // Punho divergente: dois anéis defasados.
                            g.DrawEllipse(brilho, ex - raio, ey - raio, 2 * raio, 2 * raio);
                            if (t > .23f)
                                g.DrawEllipse(fino, ex - raio * .6f, ey - raio * .6f, raio * 1.2f, raio * 1.2f);
                            break;
                        case TipoPersonagem.YutaOkkotsu:
                            // Katana: corte em X envolvido por energia azul.
                            g.DrawLine(brilho, ex - raio, ey + raio, ex + raio, ey - raio);
                            g.DrawLine(fino, ex - raio * .7f, ey - raio * .8f,
                                ex + raio * .65f, ey + raio * .7f);
                            break;
                        case TipoPersonagem.Choso:
                            // Jato carmesim percorre o espaço até o alvo.
                            g.DrawLine(brilho, e.OrigemX, e.OrigemY, ex, ey);
                            g.DrawLine(fino, e.OrigemX, e.OrigemY, ex, ey);
                            g.DrawEllipse(brilho, ex - 5, ey - 5, 10, 10);
                            break;
                        case TipoPersonagem.TojiFushiguro:
                            // Lança: estocada longa com um gancho transversal.
                            g.DrawLine(brilho, e.OrigemX, e.OrigemY, ex, ey);
                            g.DrawLine(fino, ex - 12, ey - 13, ex + 12, ey + 13);
                            g.DrawLine(fino, ex + 12, ey - 13, ex - 12, ey + 13);
                            break;
                        case TipoPersonagem.NaoyaZenin:
                            // Técnica de projeção: três cortes instantâneos em sequência.
                            for (int i = 0; i < 3; i++)
                                g.DrawLine(brilho, ex - raio + i * 12, ey + 12 + i * 5,
                                    ex + raio + i * 12, ey - 12 + i * 5);
                            break;
                        case TipoPersonagem.GojoSatoru:
                            // Infinito: ondas concêntricas de repulsão.
                            g.DrawEllipse(brilho, ex - raio * 1.4f, ey - raio * 1.4f, raio * 2.8f, raio * 2.8f);
                            g.DrawEllipse(fino, ex - raio * .75f, ey - raio * .75f, raio * 1.5f, raio * 1.5f);
                            break;
                        case TipoPersonagem.RyomenSukuna:
                            // Desmantelar: duas linhas cortantes cruzadas.
                            g.DrawLine(brilho, ex - raio * 1.7f, ey - raio, ex + raio * 1.7f, ey + raio);
                            g.DrawLine(fino, ex - raio * 1.3f, ey + raio, ex + raio * 1.3f, ey - raio);
                            break;
                        case TipoPersonagem.MegumiFushiguro:
                            // Garras de sombra: rastros paralelos.
                            for (int i = -1; i <= 1; i++)
                                g.DrawLine(brilho, ex - raio + i * 9, ey - raio,
                                    ex + raio + i * 9, ey + raio);
                            break;
                    }
                }
                if (e.Acertou && t < .55f)
                {
                    using (Pen impacto = new Pen(Color.FromArgb(Math.Max(0, 195 - e.Idade * 17), 255, 255, 255), 1.7f))
                        g.DrawEllipse(impacto, e.AlvoX - raio * .75f, e.AlvoY - raio * .75f,
                            raio * 1.5f, raio * 1.5f);
                }
            }
        }

        // Cinemática em Canvas para Itadori, Yuta, Choso e Toji.
        private void DesenharCutsceneQuarteto(Graphics g)
        {
            Color energia = quartetoTipo switch
            {
                TipoPersonagem.ItadoriYuji => Color.FromArgb(245, 60, 96),
                TipoPersonagem.YutaOkkotsu => Color.FromArgb(118, 205, 255),
                TipoPersonagem.Choso => Color.FromArgb(224, 55, 85),
                TipoPersonagem.TojiFushiguro => Color.FromArgb(96, 225, 161),
                _ => Color.White
            };

            int quadro = quartetoQuadro;
            int faixa = Math.Max(36, ClientSize.Height / 13);
            using (Brush fundo = new SolidBrush(Color.FromArgb(126, 8, 9, 24)))
                g.FillRectangle(fundo, ClientRectangle);
            using (Brush barra = new SolidBrush(Color.FromArgb(232, 8, 10, 22)))
            {
                g.FillRectangle(barra, 0, 0, ClientSize.Width, faixa);
                g.FillRectangle(barra, 0, ClientSize.Height - faixa, ClientSize.Width, faixa);
            }

            float sx = quartetoInicioX + 16f;
            float sy = quartetoInicioY + 19f;
            float tx = quartetoAlvoX + 16f;
            float ty = quartetoAlvoY + 19f;

            if (quartetoEtapa == 0)
            {
                // Preparação: aura pulsando ao redor do feiticeiro.
                float raio = 22f + quadro * 3.2f;
                using (Pen aura = new Pen(Color.FromArgb(225, energia), 4f))
                using (Pen auraInterior = new Pen(Color.FromArgb(130, 245, 245, 255), 2f))
                {
                    g.DrawEllipse(aura, sx - raio, sy - raio, raio * 2f, raio * 2f);
                    g.DrawEllipse(auraInterior, sx - raio * 0.56f, sy - raio * 0.56f,
                        raio * 1.12f, raio * 1.12f);
                }
                if (quartetoTipo == TipoPersonagem.Choso)
                {
                    // Choso concentra o Sangue Perfurante em um único ponto.
                    using (Pen espiral = new Pen(Color.FromArgb(225, 207, 50, 74), 3f))
                    using (Brush nucleo = new SolidBrush(Color.FromArgb(240, 245, 145, 164)))
                    {
                        g.DrawEllipse(espiral, sx - 18, sy - 18, 36, 36);
                        g.DrawEllipse(espiral, sx - 31, sy - 31, 62, 62);
                        g.FillEllipse(nucleo, sx - 6, sy - 6, 12, 12);
                    }
                }
                else if (quartetoTipo == TipoPersonagem.TojiFushiguro)
                {
                    // Toji se prepara para uma investida veloz com sua arma.
                    using (Pen rastro = new Pen(Color.FromArgb(190, 95, 228, 165), 3f))
                    {
                        g.DrawLine(rastro, sx - 42, sy + 16, sx + 39, sy - 37);
                        g.DrawLine(rastro, sx - 33, sy + 32, sx + 23, sy - 10);
                    }
                }
            }
            else if (quartetoEtapa == 1)
            {
                float t = Math.Min(1f, (float)quadro / QuadrosGolpeQuarteto);
                float fx = sx + (tx - sx) * t;
                float fy = sy + (ty - sy) * t;
                float largura = quartetoTipo == TipoPersonagem.YutaOkkotsu ? 16f : 9f;

                using (Pen rastro = new Pen(Color.FromArgb(135, energia), largura))
                using (Pen centro = new Pen(Color.FromArgb(240, 246, 244, 255), 3f))
                {
                    rastro.StartCap = LineCap.Round;
                    rastro.EndCap = LineCap.Round;
                    g.DrawLine(rastro, sx, sy, fx, fy);
                    g.DrawLine(centro, sx, sy, fx, fy);
                }

                if (quartetoTipo == TipoPersonagem.ItadoriYuji)
                {
                    // Duplo impacto característico do Black Flash.
                    using (Pen relampago = new Pen(Color.FromArgb(240, 240, 49, 93), 4f))
                    {
                        g.DrawLine(relampago, fx - 28, fy - 35, fx + 4, fy - 12);
                        g.DrawLine(relampago, fx + 4, fy - 12, fx - 14, fy + 22);
                        g.DrawLine(relampago, fx - 14, fy + 22, fx + 29, fy + 5);
                    }
                }
                else if (quartetoTipo == TipoPersonagem.YutaOkkotsu)
                {
                    // Silhueta de energia representando Rika atrás de Yuta.
                    using (Brush rika = new SolidBrush(Color.FromArgb(100, 154, 166, 255)))
                    using (Pen contorno = new Pen(Color.FromArgb(215, 185, 235, 255), 3f))
                    {
                        g.FillEllipse(rika, fx - 40, fy - 69, 80, 97);
                        g.DrawEllipse(contorno, fx - 40, fy - 69, 80, 97);
                        g.DrawLine(contorno, fx - 33, fy + 8, fx - 52, fy + 32);
                        g.DrawLine(contorno, fx + 33, fy + 8, fx + 52, fy + 32);
                    }
                }
                else if (quartetoTipo == TipoPersonagem.Choso)
                {
                    // Feixe compacto e veloz do Sangue Perfurante (efeito visual estilizado).
                    using (Pen feixe = new Pen(Color.FromArgb(225, 178, 27, 55), 11f))
                    using (Pen nucleo = new Pen(Color.FromArgb(255, 255, 198, 210), 3f))
                    using (Brush ponta = new SolidBrush(Color.FromArgb(240, 244, 95, 116)))
                    {
                        feixe.StartCap = LineCap.Round;
                        feixe.EndCap = LineCap.Round;
                        g.DrawLine(feixe, sx, sy, fx, fy);
                        g.DrawLine(nucleo, sx, sy, fx, fy);
                        g.FillEllipse(ponta, fx - 8f, fy - 8f, 16f, 16f);
                    }
                    using (Pen ondas = new Pen(Color.FromArgb(165, 229, 85, 112), 2f))
                    {
                        g.DrawEllipse(ondas, fx - 22, fy - 22, 44, 44);
                        g.DrawEllipse(ondas, fx - 36, fy - 36, 72, 72);
                    }
                }
                else if (quartetoTipo == TipoPersonagem.TojiFushiguro)
                {
                    // Investida verde e cortes cruzados da Lança Invertida do Céu.
                    using (Pen trilha = new Pen(Color.FromArgb(210, 56, 170, 132), 8f))
                    using (Pen lamina = new Pen(Color.FromArgb(250, 206, 247, 223), 4f))
                    {
                        trilha.StartCap = LineCap.Round;
                        trilha.EndCap = LineCap.Round;
                        g.DrawLine(trilha, sx, sy, fx, fy);
                        g.DrawLine(lamina, fx - 43, fy + 31, fx + 34, fy - 35);
                        g.DrawLine(lamina, fx - 28, fy - 35, fx + 39, fy + 25);
                    }
                }
            }
            else
            {
                float raio = 25f + quadro * 12f;
                int transparencia = Math.Max(0, 225 - quadro * 14);
                using (Pen impacto = new Pen(Color.FromArgb(transparencia, energia), 5f))
                using (Pen flash = new Pen(Color.FromArgb(transparencia / 2, Color.White), 2f))
                {
                    g.DrawEllipse(impacto, tx - raio, ty - raio * 0.7f,
                        raio * 2f, raio * 1.4f);
                    g.DrawEllipse(flash, tx - raio * 0.7f, ty - raio * 0.49f,
                        raio * 1.4f, raio * 0.98f);
                }
            }

            string titulo = quartetoTipo switch
            {
                TipoPersonagem.ItadoriYuji => "YUJI ITADORI",
                TipoPersonagem.YutaOkkotsu => "YUTA OKKOTSU",
                TipoPersonagem.Choso => "CHOSO",
                TipoPersonagem.TojiFushiguro => "TOJI FUSHIGURO",
                _ => "TÉCNICA AMALDIÇOADA"
            };
            string tecnica = quartetoTipo switch
            {
                TipoPersonagem.ItadoriYuji => "BLACK FLASH",
                TipoPersonagem.YutaOkkotsu => "RIKA — CORTE AMALDIÇOADO",
                TipoPersonagem.Choso => "SANGUE PERFURANTE",
                TipoPersonagem.TojiFushiguro => "LANÇA INVERTIDA DO CÉU",
                _ => "TÉCNICA ESPECIAL"
            };
            string subtitulo = quartetoEtapa == 0
                ? quartetoTipo == TipoPersonagem.Choso
                    ? "CONCENTRANDO MANIPULAÇÃO DE SANGUE"
                    : quartetoTipo == TipoPersonagem.TojiFushiguro
                        ? "VELOCIDADE FÍSICA E ARMA ESPECIAL"
                        : "CONCENTRANDO ENERGIA AMALDIÇOADA"
                : quartetoEtapa == 1 ? tecnica
                : quartetoAcertou ? "GOLPE ESPECIAL CONCLUÍDO!" : "O ALVO ESTAVA FORA DO ALCANCE";
            DesenharTextoCinematografico(g, quartetoEtapa == 0 ? titulo : tecnica,
                subtitulo, energia, faixa);
        }

        private void DesenharCutsceneGojo(Graphics g)
        {
            int quadro = gojoQuadro;
            int alpha = gojoEtapa == 2 ? (gojoAcertou && quadro < 5 ? 78 : 118) : 90;
            using (Brush sombra = new SolidBrush(Color.FromArgb(alpha, 7, 5, 26)))
                g.FillRectangle(sombra, ClientRectangle);

            int faixa = Math.Max(34, ClientSize.Height / 13);
            using (Brush barra = new SolidBrush(Color.FromArgb(238, 4, 5, 18)))
            {
                g.FillRectangle(barra, 0, 0, ClientSize.Width, faixa);
                g.FillRectangle(barra, 0, ClientSize.Height - faixa, ClientSize.Width, faixa);
            }

            float sx = gojoInicioX + 16f;
            float sy = gojoInicioY + 20f;
            float tx = gojoAlvoX + 16f;
            float ty = gojoAlvoY + 20f;

            if (gojoEtapa == 0)
            {
                float pulso = 20f + (quadro % 6) * 4f;
                using (Pen azul = new Pen(Color.FromArgb(220, 80, 175, 255), 3f))
                using (Pen vermelho = new Pen(Color.FromArgb(210, 255, 75, 95), 2f))
                {
                    g.DrawEllipse(azul, sx - pulso, sy - pulso, pulso * 2, pulso * 2);
                    g.DrawEllipse(vermelho, sx - pulso * 0.65f, sy - pulso * 0.65f,
                        pulso * 1.3f, pulso * 1.3f);
                }
            }
            else if (gojoEtapa == 1)
            {
                float t = Math.Min(1f, (float)quadro / QuadrosDisparoGojo);
                float px = sx + (tx - sx) * t;
                float py = sy + (ty - sy) * t;
                using (Pen rastro = new Pen(Color.FromArgb(150, 140, 100, 255), 18f))
                using (Pen roxo = new Pen(Color.FromArgb(230, 190, 145, 255), 8f))
                using (Brush nucleo = new SolidBrush(Color.FromArgb(255, 230, 220, 255)))
                {
                    rastro.StartCap = LineCap.Round; rastro.EndCap = LineCap.Round;
                    roxo.StartCap = LineCap.Round; roxo.EndCap = LineCap.Round;
                    g.DrawLine(rastro, sx, sy, px, py);
                    g.DrawLine(roxo, sx, sy, px, py);
                    g.FillEllipse(nucleo, px - 13, py - 13, 26, 26);
                }
                using (Pen riscos = new Pen(Color.FromArgb(150, 170, 200, 255), 2f))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float yy = (i * 53 + quadro * 29) % Math.Max(1, ClientSize.Height);
                        float xx = (i * 71 + quadro * 43) % Math.Max(1, ClientSize.Width);
                        g.DrawLine(riscos, xx, yy, Math.Min(ClientSize.Width, xx + 55 + i * 7), yy - 12);
                    }
                }
            }
            else
            {
                float raio = 30f + quadro * 12f;
                int brilho = Math.Max(0, 230 - quadro * 12);
                using (Pen anel = new Pen(Color.FromArgb(brilho, 180, 125, 255), 5f))
                using (Pen anel2 = new Pen(Color.FromArgb(brilho, 100, 185, 255), 2f))
                {
                    g.DrawEllipse(anel, tx - raio, ty - raio * 0.65f, raio * 2f, raio * 1.3f);
                    g.DrawEllipse(anel2, tx - raio * 0.7f, ty - raio * 0.45f,
                        raio * 1.4f, raio * 0.9f);
                }
            }

            string titulo = gojoEtapa == 0 ? "GOJO SATORU" :
                            gojoEtapa == 1 ? "HOLLOW PURPLE — ROXO" :
                            (gojoAcertou ? "IMPACTO DO ROXO!" : "TÉCNICA LANÇADA");
            string subtitulo = gojoEtapa == 0 ? "CONVERGÊNCIA DE ENERGIA" :
                               gojoEtapa == 1 ? "AZUL + VERMELHO" :
                               (gojoAcertou ? "A ENERGIA IMAGINÁRIA ATINGIU O ALVO" : "O OPONENTE ESTAVA FORA DO ALCANCE");
            DesenharTextoCinematografico(g, titulo, subtitulo,
                Color.FromArgb(235, 190, 165, 255), faixa);
        }

        private void DesenharCutsceneSukuna(Graphics g)
        {
            int quadro = sukunaQuadro;
            int alpha = sukunaEtapa == 2 && sukunaAcertou && quadro < 4 ? 65 : 112;
            using (Brush sombra = new SolidBrush(Color.FromArgb(alpha, 24, 2, 5)))
                g.FillRectangle(sombra, ClientRectangle);

            int faixa = Math.Max(34, ClientSize.Height / 13);
            using (Brush barra = new SolidBrush(Color.FromArgb(240, 12, 3, 8)))
            {
                g.FillRectangle(barra, 0, 0, ClientSize.Width, faixa);
                g.FillRectangle(barra, 0, ClientSize.Height - faixa, ClientSize.Width, faixa);
            }

            float cx = sukunaInicioX + 16f;
            float cy = sukunaInicioY + 21f;
            if (sukunaEtapa == 0)
            {
                float pulso = 80f + (quadro % 5) * 8f;
                using (Pen vermelho = new Pen(Color.FromArgb(230, 195, 30, 48), 3f))
                using (Pen ouro = new Pen(Color.FromArgb(180, 255, 105, 60), 2f))
                {
                    g.DrawEllipse(vermelho, cx - pulso, cy - pulso * 0.65f,
                        pulso * 2f, pulso * 1.3f);
                    g.DrawLine(vermelho, cx - 42, cy - 72, cx - 42, cy + 72);
                    g.DrawLine(vermelho, cx + 42, cy - 72, cx + 42, cy + 72);
                    g.DrawLine(ouro, cx - 58, cy - 45, cx, cy - 95);
                    g.DrawLine(ouro, cx, cy - 95, cx + 58, cy - 45);
                }
            }
            else if (sukunaEtapa == 1)
            {
                using (Pen corte = new Pen(Color.FromArgb(210, 255, 45, 60), 3f))
                using (Pen corteClaro = new Pen(Color.FromArgb(150, 255, 200, 180), 1.5f))
                {
                    for (int i = 0; i < 18; i++)
                    {
                        int xx = (i * 97 + quadro * 47) % Math.Max(1, ClientSize.Width);
                        int yy = (i * 61 + quadro * 37) % Math.Max(1, ClientSize.Height);
                        int comprimento = 35 + ((i * 19 + quadro * 13) % 105);
                        g.DrawLine(corte, xx, yy, Math.Min(ClientSize.Width, xx + comprimento), yy - 22);
                        if (i % 2 == 0)
                            g.DrawLine(corteClaro, xx + 8, yy + 8,
                                Math.Min(ClientSize.Width, xx + comprimento - 5), yy - 10);
                    }
                }
                using (Pen circulo = new Pen(Color.FromArgb(170, 200, 20, 40), 2f))
                    g.DrawEllipse(circulo, cx - 280, cy - 180, 560, 360);
            }
            else
            {
                float raio = 45 + quadro * 10f;
                using (Pen corte = new Pen(Color.FromArgb(Math.Max(0, 220 - quadro * 10), 255, 40, 50), 4f))
                {
                    g.DrawEllipse(corte, cx - raio, cy - raio * 0.55f, raio * 2, raio * 1.1f);
                    g.DrawLine(corte, cx - raio, cy - raio * 0.5f, cx + raio, cy + raio * 0.5f);
                    g.DrawLine(corte, cx - raio * 0.8f, cy + raio * 0.5f, cx + raio * 0.8f, cy - raio * 0.5f);
                }
            }

            string titulo = sukunaEtapa == 0 ? "RYOMEN SUKUNA" :
                            sukunaEtapa == 1 ? "SANTUÁRIO MALEVOLENTE" :
                            (sukunaAcertou ? "CORTES!" : "DOMÍNIO EXPANDIDO");
            string subtitulo = sukunaEtapa == 0 ? "EXPANSÃO DE DOMÍNIO" :
                               sukunaEtapa == 1 ? "DESMANTELAR — CORTES EM ÁREA" :
                               (sukunaAcertou ? "NINGUÉM ESCAPA DO SANTUÁRIO" : "O OPONENTE ESTAVA FORA DA ÁREA");
            DesenharTextoCinematografico(g, titulo, subtitulo,
                Color.FromArgb(255, 255, 90, 85), faixa);
        }

        private void DesenharCutsceneMegumi(Graphics g)
        {
            int quadro = megumiQuadro;
            int alphaFundo = megumiEtapa == 2 && megumiAcertou && quadro < 5 ? 48 : 128;
            using (Brush sombraFundo = new SolidBrush(Color.FromArgb(alphaFundo, 5, 7, 18)))
                g.FillRectangle(sombraFundo, ClientRectangle);

            int faixa = Math.Max(36, ClientSize.Height / 13);
            using (Brush barra = new SolidBrush(Color.FromArgb(242, 3, 5, 12)))
            {
                g.FillRectangle(barra, 0, 0, ClientSize.Width, faixa);
                g.FillRectangle(barra, 0, ClientSize.Height - faixa, ClientSize.Width, faixa);
            }

            float sx = megumiInicioX + 16f;
            float sy = megumiInicioY + 22f;
            float tx = megumiAlvoX + 16f;
            float ty = megumiAlvoY + 22f;
            string nomeSombra = NomesSombras[megumiSombraIndice];

            if (megumiEtapa == 0)
            {
                // A sombra se abre sob Megumi enquanto ele prepara a invocação.
                float progresso = (float)quadro / QuadrosPreparacaoMegumi;
                float raio = 28f + progresso * 92f;
                int a = Math.Max(0, 220 - quadro * 7);
                using (Pen anel = new Pen(Color.FromArgb(a, 95, 83, 155), 4f))
                using (Pen anelInterno = new Pen(Color.FromArgb(a, 155, 150, 205), 2f))
                {
                    g.DrawEllipse(anel, sx - raio, sy - raio * 0.42f, raio * 2f, raio * 0.84f);
                    g.DrawEllipse(anelInterno, sx - raio * 0.62f, sy - raio * 0.28f,
                        raio * 1.24f, raio * 0.56f);
                }
                using (Pen linhas = new Pen(Color.FromArgb(170, 120, 115, 190), 2f))
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = (float)(i * Math.PI / 4.0 + quadro * 0.08);
                        float x1 = sx + (float)Math.Cos(ang) * 18f;
                        float y1 = sy + (float)Math.Sin(ang) * 18f;
                        float x2 = sx + (float)Math.Cos(ang) * (raio + 10f);
                        float y2 = sy + (float)Math.Sin(ang) * (raio * 0.45f + 10f);
                        g.DrawLine(linhas, x1, y1, x2, y2);
                    }
                }

                // Símbolo da sombra sorteada aparece já durante a preparação.
                float pulso = 48f + (float)Math.Sin(quadro * 0.7f) * 6f;
                DesenharSimboloSombra(g, megumiSombraIndice, sx, sy - 42f, pulso, Math.Min(230, 90 + quadro * 8));
            }
            else if (megumiEtapa == 1)
            {
                // Rastro de sombra e um único shikigami sorteado avançando em direção ao alvo.
                float t = Math.Min(1f, (float)quadro / QuadrosInvocacaoMegumi);
                float fimX = sx + (tx - sx) * t;
                float fimY = sy + (ty - sy) * t;
                using (Pen trilhaEscura = new Pen(Color.FromArgb(205, 20, 19, 42), 36f))
                using (Pen trilhaRoxa = new Pen(Color.FromArgb(180, 95, 78, 150), 5f))
                {
                    trilhaEscura.StartCap = LineCap.Round;
                    trilhaEscura.EndCap = LineCap.Round;
                    trilhaRoxa.StartCap = LineCap.Round;
                    trilhaRoxa.EndCap = LineCap.Round;
                    g.DrawLine(trilhaEscura, sx, sy, fimX, fimY);
                    g.DrawLine(trilhaRoxa, sx, sy, fimX, fimY);
                }

                using (Pen riscos = new Pen(Color.FromArgb(150, 140, 130, 200), 2f))
                {
                    for (int i = 0; i < 14; i++)
                    {
                        int yy = (i * 43 + quadro * 19) % Math.Max(1, ClientSize.Height);
                        int xx = (i * 79 + quadro * 27) % Math.Max(1, ClientSize.Width);
                        g.DrawLine(riscos, xx, yy, Math.Min(ClientSize.Width, xx + 34 + i * 5), yy - 8);
                    }
                }

                float oscilacao = (float)Math.Sin(quadro * 0.38f) * 13f;
                float tamanho = Math.Min(118f, Math.Max(66f, ClientSize.Width / 14f));
                // Rastros menores do MESMO shikigami, sem exibir os outros nove.
                for (int rastro = 3; rastro >= 1; rastro--)
                {
                    float tRastro = Math.Max(0f, t - rastro * 0.075f);
                    float rx = sx + (tx - sx) * tRastro;
                    float ry = sy + (ty - sy) * tRastro + oscilacao;
                    DesenharSimboloSombra(g, megumiSombraIndice, rx, ry,
                        tamanho * (1f - rastro * 0.13f), Math.Max(22, 105 - rastro * 25));
                }
                DesenharSimboloSombra(g, megumiSombraIndice, fimX, fimY + oscilacao, tamanho,
                    Math.Min(255, 145 + quadro * 4));

                using (Font f = new Font("Segoe UI", Math.Max(12f, Math.Min(25f, ClientSize.Width / 65f)), FontStyle.Bold))
                using (Brush b = new SolidBrush(Color.FromArgb(245, 238, 236, 255)))
                using (StringFormat alinhamento = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    RectangleF areaNome = new RectangleF(20, ClientSize.Height * 0.78f,
                        ClientSize.Width - 40, Math.Max(28, ClientSize.Height * 0.06f));
                    g.DrawString("SOMBRA INVOCADA: " + nomeSombra.ToUpperInvariant(), f, b, areaNome, alinhamento);
                }
            }
            else
            {
                // Impacto da sombra sorteada: uma única figura no centro, com ondas de energia.
                float progresso = Math.Min(1f, (float)quadro / QuadrosDominioMegumi);
                float raio = 45f + progresso * Math.Min(ClientSize.Width, ClientSize.Height) * 0.42f;
                int a = Math.Max(0, 230 - quadro * 10);
                float cx = ClientSize.Width / 2f;
                float cy = ClientSize.Height / 2f;
                using (Pen circulo = new Pen(Color.FromArgb(a, 93, 75, 153), 6f))
                using (Pen circuloFino = new Pen(Color.FromArgb(a, 175, 163, 220), 2f))
                {
                    g.DrawEllipse(circulo, cx - raio, cy - raio * 0.5f, raio * 2f, raio);
                    g.DrawEllipse(circuloFino, cx - raio * 0.72f, cy - raio * 0.36f, raio * 1.44f, raio * 0.72f);
                }
                using (Pen corte = new Pen(Color.FromArgb(a, 175, 160, 230), 3f))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        double ang = i * Math.PI / 5.0 + quadro * 0.12;
                        float px = cx + (float)Math.Cos(ang) * raio * 0.72f;
                        float py = cy + (float)Math.Sin(ang) * raio * 0.35f;
                        g.DrawLine(corte, cx, cy, px, py);
                    }
                }
                float tamanhoSombra = Math.Min(150f, Math.Max(82f, ClientSize.Width / 11f));
                DesenharSimboloSombra(g, megumiSombraIndice, cx, cy - 8f, tamanhoSombra,
                    Math.Max(30, 235 - quadro * 7));

                if (megumiAcertou && quadro < 8)
                {
                    using (Pen flash = new Pen(Color.FromArgb(Math.Max(0, 210 - quadro * 25), 218, 218, 255), 9f))
                        g.DrawEllipse(flash, cx - 50 - quadro * 12, cy - 30 - quadro * 6,
                            100 + quadro * 24, 60 + quadro * 12);
                }
            }

            string titulo;
            string subtitulo;
            if (megumiEtapa == 0)
            {
                titulo = "MEGUMI FUSHIGURO";
                subtitulo = "TÉCNICA DAS DEZ SOMBRAS — INVOCANDO " + nomeSombra.ToUpperInvariant();
            }
            else if (megumiEtapa == 1)
            {
                titulo = "TÉCNICA DAS DEZ SOMBRAS";
                subtitulo = "SHIKIGAMI: " + nomeSombra.ToUpperInvariant();
            }
            else
            {
                titulo = nomeSombra.ToUpperInvariant() + (megumiAcertou ? " — ATAQUE!" : " — INVOCAÇÃO");
                if (megumiSombraIndice == 6)
                    subtitulo = "VEADO REDONDO — MEGUMI RECUPERA 25 PONTOS DE VIDA";
                else if (megumiAcertou)
                    subtitulo = nomeSombra.ToUpperInvariant() + " ATINGIU O ADVERSÁRIO";
                else
                    subtitulo = "O ADVERSÁRIO ESTAVA FORA DO ALCANCE";
            }

            DesenharTextoCinematografico(g, titulo, subtitulo,
                Color.FromArgb(235, 188, 178, 255), faixa);
        }

        private void DesenharSimboloSombra(Graphics g, int indice, float cx, float cy,
            float tamanho, int alpha)
        {
            float r = tamanho * 0.34f;
            using (Brush aura = new SolidBrush(Color.FromArgb(Math.Max(0, alpha / 4), 115, 95, 180)))
                g.FillEllipse(aura, cx - r * 1.45f, cy - r * 1.45f, r * 2.9f, r * 2.9f);
            using (Brush b = new SolidBrush(Color.FromArgb(alpha, 18, 17, 32)))
            using (Pen p = new Pen(Color.FromArgb(alpha, 185, 177, 224), 2f))
            {
                switch (indice)
                {
                    case 0: // Cães Divinos
                        g.FillEllipse(b, cx - r, cy - r * 0.25f, r * 2f, r * 1.45f);
                        g.FillPolygon(b, new PointF[] { new PointF(cx - r * 0.7f, cy - r * 0.1f), new PointF(cx - r * 0.85f, cy - r * 1.0f), new PointF(cx - r * 0.2f, cy - r * 0.45f) });
                        g.FillPolygon(b, new PointF[] { new PointF(cx + r * 0.25f, cy - r * 0.4f), new PointF(cx + r * 0.85f, cy - r * 0.95f), new PointF(cx + r * 0.8f, cy - r * 0.05f) });
                        g.DrawEllipse(p, cx + r * 0.25f, cy + r * 0.1f, r * 0.16f, r * 0.16f);
                        break;
                    case 1: // Nue
                        g.FillEllipse(b, cx - r * 0.38f, cy - r * 0.1f, r * 0.76f, r * 1.15f);
                        g.FillPolygon(b, new PointF[] { new PointF(cx - r * 0.25f, cy), new PointF(cx - r * 1.35f, cy - r * 0.75f), new PointF(cx - r * 0.95f, cy + r * 0.55f) });
                        g.FillPolygon(b, new PointF[] { new PointF(cx + r * 0.25f, cy), new PointF(cx + r * 1.35f, cy - r * 0.75f), new PointF(cx + r * 0.95f, cy + r * 0.55f) });
                        g.FillEllipse(b, cx - r * 0.25f, cy - r * 0.9f, r * 0.5f, r * 0.65f);
                        break;
                    case 2: // Sapo
                        g.FillEllipse(b, cx - r, cy - r * 0.2f, r * 2f, r * 1.2f);
                        g.FillEllipse(b, cx - r * 0.65f, cy - r * 0.65f, r * 0.55f, r * 0.55f);
                        g.FillEllipse(b, cx + r * 0.1f, cy - r * 0.65f, r * 0.55f, r * 0.55f);
                        g.DrawLine(p, cx + r * 0.5f, cy + r * 0.35f, cx + r * 1.25f, cy + r * 0.65f);
                        break;
                    case 3: // Serpente
                        g.DrawLines(p, new PointF[] { new PointF(cx - r, cy + r * 0.55f), new PointF(cx - r * 0.55f, cy - r * 0.45f), new PointF(cx, cy + r * 0.3f), new PointF(cx + r * 0.5f, cy - r * 0.45f), new PointF(cx + r, cy + r * 0.35f) });
                        g.FillEllipse(b, cx + r * 0.45f, cy - r * 0.65f, r * 0.8f, r * 0.55f);
                        break;
                    case 4: // Max Elefante
                        g.FillEllipse(b, cx - r, cy - r * 0.5f, r * 1.8f, r * 1.5f);
                        g.FillEllipse(b, cx + r * 0.25f, cy - r * 0.55f, r * 0.65f, r * 0.65f);
                        g.DrawArc(p, cx + r * 0.35f, cy - r * 0.05f, r * 0.7f, r * 1.2f, 20, 150);
                        g.DrawLine(p, cx - r * 0.55f, cy + r * 0.65f, cx - r * 0.55f, cy + r * 1.0f);
                        g.DrawLine(p, cx + r * 0.25f, cy + r * 0.65f, cx + r * 0.25f, cy + r * 1.0f);
                        break;
                    case 5: // Coelhos
                        g.FillEllipse(b, cx - r * 0.8f, cy, r * 0.8f, r * 0.8f);
                        g.FillEllipse(b, cx + r * 0.05f, cy, r * 0.8f, r * 0.8f);
                        g.FillEllipse(b, cx - r * 0.65f, cy - r * 1.3f, r * 0.3f, r * 1.1f);
                        g.FillEllipse(b, cx - r * 0.15f, cy - r * 1.3f, r * 0.3f, r * 1.1f);
                        g.FillEllipse(b, cx + r * 0.2f, cy - r * 1.3f, r * 0.3f, r * 1.1f);
                        g.FillEllipse(b, cx + r * 0.7f, cy - r * 1.3f, r * 0.3f, r * 1.1f);
                        break;
                    case 6: // Veado Redondo
                        g.FillEllipse(b, cx - r * 0.7f, cy - r * 0.1f, r * 1.4f, r * 1.0f);
                        g.DrawLine(p, cx - r * 0.25f, cy - r * 0.2f, cx - r * 0.65f, cy - r * 1.1f);
                        g.DrawLine(p, cx - r * 0.65f, cy - r * 1.1f, cx - r * 1.0f, cy - r * 0.8f);
                        g.DrawLine(p, cx + r * 0.25f, cy - r * 0.2f, cx + r * 0.65f, cy - r * 1.1f);
                        g.DrawLine(p, cx + r * 0.65f, cy - r * 1.1f, cx + r * 1.0f, cy - r * 0.8f);
                        break;
                    case 7: // Boi Perfurante
                        g.FillEllipse(b, cx - r, cy - r * 0.35f, r * 2f, r * 1.2f);
                        g.FillPolygon(b, new PointF[] { new PointF(cx - r * 0.65f, cy - r * 0.2f), new PointF(cx - r * 1.4f, cy - r * 0.9f), new PointF(cx - r * 0.8f, cy + r * 0.1f) });
                        g.FillPolygon(b, new PointF[] { new PointF(cx + r * 0.65f, cy - r * 0.2f), new PointF(cx + r * 1.4f, cy - r * 0.9f), new PointF(cx + r * 0.8f, cy + r * 0.1f) });
                        break;
                    case 8: // Tigre Funeral
                        g.FillEllipse(b, cx - r, cy - r * 0.25f, r * 2f, r * 1.2f);
                        for (int i = -1; i <= 1; i++)
                            g.DrawLine(p, cx + i * r * 0.5f, cy - r * 0.15f, cx + i * r * 0.25f, cy + r * 0.55f);
                        g.FillEllipse(b, cx - r * 0.35f, cy - r * 0.85f, r * 0.8f, r * 0.75f);
                        break;
                    default: // Mahoraga: figura e roda
                        g.FillEllipse(b, cx - r * 0.38f, cy - r * 0.55f, r * 0.76f, r * 0.6f);
                        g.FillRectangle(b, cx - r * 0.5f, cy + r * 0.05f, r, r * 1.2f);
                        g.DrawEllipse(p, cx - r * 1.1f, cy - r * 1.45f, r * 2.2f, r * 0.75f);
                        g.DrawLine(p, cx, cy - r * 1.45f, cx, cy - r * 0.7f);
                        g.DrawLine(p, cx - r * 0.75f, cy - r * 1.25f, cx + r * 0.75f, cy - r * 0.9f);
                        break;
                }
            }
        }

        private void DesenharTextoCinematografico(Graphics g, string titulo,
            string subtitulo, Color corTitulo, int alturaBarra)
        {
            using (Font fonteTitulo = new Font("Arial", Math.Max(18, ClientSize.Width / 38), FontStyle.Bold))
            using (Font fonteSub = new Font("Arial", Math.Max(10, ClientSize.Width / 100), FontStyle.Bold))
            using (Brush pincelTitulo = new SolidBrush(corTitulo))
            using (Brush branco = new SolidBrush(Color.White))
            {
                SizeF t = g.MeasureString(titulo, fonteTitulo);
                SizeF st = g.MeasureString(subtitulo, fonteSub);
                float yTitulo = ClientSize.Height - alturaBarra - t.Height - 10;
                if (yTitulo < alturaBarra + 4) yTitulo = alturaBarra + 4;
                g.DrawString(titulo, fonteTitulo, pincelTitulo,
                    (ClientSize.Width - t.Width) / 2f, yTitulo);
                g.DrawString(subtitulo, fonteSub, branco,
                    (ClientSize.Width - st.Width) / 2f, ClientSize.Height - alturaBarra + 5);
            }
        }

        private void DesenharCutsceneNaoya(Graphics g)
        {
            int quadro = cutsceneNaoyaQuadro;
            int escurecer = cutsceneNaoyaEtapa == 2 ? 78 : 95;
            if (cutsceneNaoyaEtapa == 2 && naoyaAcertou && quadro < 5)
                escurecer = 45; // clarão rápido no momento do impacto

            using (Brush sombra = new SolidBrush(Color.FromArgb(escurecer, 4, 7, 18)))
                g.FillRectangle(sombra, ClientRectangle);

            int alturaBarra = Math.Max(38, ClientSize.Height / 12);
            using (Brush barra = new SolidBrush(Color.FromArgb(235, 8, 10, 20)))
            {
                g.FillRectangle(barra, 0, 0, ClientSize.Width, alturaBarra);
                g.FillRectangle(barra, 0, ClientSize.Height - alturaBarra,
                    ClientSize.Width, alturaBarra);
            }

            // Linhas de velocidade animadas durante o avanço.
            if (cutsceneNaoyaEtapa == 1)
            {
                using (Pen linha = new Pen(Color.FromArgb(175, 226, 210, 125), 2f))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        int yLinha = (i * 47 + quadro * 31) % Math.Max(1, ClientSize.Height);
                        int inicio = (i * 83 + quadro * 53) % Math.Max(1, ClientSize.Width);
                        int comprimento = 45 + ((i * 17 + quadro * 11) % 130);
                        g.DrawLine(linha, inicio, yLinha,
                            Math.Min(ClientSize.Width, inicio + comprimento), yLinha - 9);
                    }
                }
            }

            // Anéis de energia no impacto.
            if (cutsceneNaoyaEtapa == 2 && naoyaAcertou)
            {
                float progresso = Math.Min(1f, (float)quadro / QuadrosImpactoNaoya);
                float raio = 35f + progresso * Math.Min(ClientSize.Width, ClientSize.Height) * 0.45f;
                int alpha = Math.Max(0, 220 - quadro * 11);
                using (Pen anel = new Pen(Color.FromArgb(alpha, 239, 220, 138), 4f))
                {
                    float cx = ClientSize.Width / 2f;
                    float cy = ClientSize.Height / 2f;
                    g.DrawEllipse(anel, cx - raio, cy - raio * 0.42f,
                        raio * 2f, raio * 0.84f);
                }
            }

            string titulo;
            string subtitulo;
            if (cutsceneNaoyaEtapa == 0)
            {
                titulo = "NAOYA ZENIN";
                subtitulo = "TÉCNICA DE PROJEÇÃO";
            }
            else if (cutsceneNaoyaEtapa == 1)
            {
                titulo = "TÉCNICA DE PROJEÇÃO";
                subtitulo = "ACELERAÇÃO — 24 QUADROS";
            }
            else
            {
                titulo = naoyaAcertou ? "IMPACTO!" : "AVANÇO CONCLUÍDO";
                subtitulo = naoyaAcertou ? "O OPONENTE FOI ATORDOADO" : "O OPONENTE ESTAVA FORA DE ALCANCE";
            }

            using (Font fonteTitulo = new Font("Arial", Math.Max(18, ClientSize.Width / 36), FontStyle.Bold))
            using (Font fonteSub = new Font("Arial", Math.Max(10, ClientSize.Width / 95), FontStyle.Bold))
            using (Brush ouro = new SolidBrush(Color.FromArgb(255, 238, 218, 133)))
            using (Brush branco = new SolidBrush(Color.White))
            {
                SizeF tamTitulo = g.MeasureString(titulo, fonteTitulo);
                SizeF tamSub = g.MeasureString(subtitulo, fonteSub);
                float yTitulo = ClientSize.Height - alturaBarra - tamTitulo.Height - 12;
                float ySub = ClientSize.Height - alturaBarra + 7;
                if (yTitulo < alturaBarra + 5) yTitulo = alturaBarra + 5;
                g.DrawString(titulo, fonteTitulo, ouro,
                    (ClientSize.Width - tamTitulo.Width) / 2f, yTitulo);
                g.DrawString(subtitulo, fonteSub, branco,
                    (ClientSize.Width - tamSub.Width) / 2f, ySub);
            }
        }

        private void DesenharTelaSelecao(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(9, 14, 28));

            // Fundo da seleção: duas equipes com luzes opostas e um selo central.
            using (Brush azul = new SolidBrush(Color.FromArgb(58, 31, 72, 124)))
                g.FillRectangle(azul, 0, 0, LarguraBase / 2, AlturaBase);
            using (Brush vinho = new SolidBrush(Color.FromArgb(55, 108, 32, 77)))
                g.FillRectangle(vinho, LarguraBase / 2, 0, LarguraBase / 2, AlturaBase);
            using (Pen grade = new Pen(Color.FromArgb(34, 124, 152, 193), 1f))
            using (Pen circulo = new Pen(Color.FromArgb(87, 182, 160, 215), 2f))
            {
                for (int i = 0; i < LarguraBase; i += 55)
                    g.DrawLine(grade, i, 0, i, AlturaBase);
                for (int i = 0; i < AlturaBase; i += 55)
                    g.DrawLine(grade, 0, i, LarguraBase, i);
                g.DrawEllipse(circulo, 400, 200, 200, 200);
                g.DrawEllipse(circulo, 445, 245, 110, 110);
            }
            using (Brush topo = new SolidBrush(Color.FromArgb(225, 7, 12, 26)))
                g.FillRectangle(topo, 0, 0, LarguraBase, 73);
            using (Pen linha = new Pen(Color.FromArgb(165, 151, 116, 202), 2f))
                g.DrawLine(linha, 0, 72, LarguraBase, 72);

            using (Font titulo = new Font("Segoe UI", 25, FontStyle.Bold))
            using (Font subtitulo = new Font("Segoe UI", 10, FontStyle.Regular))
            using (Brush branco = new SolidBrush(Color.White))
            using (Brush dourado = new SolidBrush(Color.FromArgb(255, 240, 207, 112)))
            using (StringFormat centralizado = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                string textoTitulo = "JUJUTSU ARENA  /  ESCOLHA SEU LUTADOR";
                g.DrawString(textoTitulo, titulo, dourado,
                    new RectangleF(0, 8, LarguraBase, 38), centralizado);

                string subt = "8 LUTADORES  ·  GOLPES ÚNICOS  ·  2 JOGADORES LOCAIS";
                g.DrawString(subt, subtitulo, branco,
                    new RectangleF(0, 45, LarguraBase, 24), centralizado);
            }

            const float painelY = 80f;
            const float painelAltura = 438f;
            const float painelLargura = 465f;
            const float painel1X = 22f;
            const float painel2X = 513f;
            DesenharPainelSelecao(g, new RectangleF(painel1X, painelY, painelLargura, painelAltura),
                "JOGADOR 1", Color.DeepSkyBlue);
            DesenharPainelSelecao(g, new RectangleF(painel2X, painelY, painelLargura, painelAltura),
                "JOGADOR 2", Color.LightCoral);

            DesenharCartoesSelecao(g, painel1X, painelLargura, escolha1, confirmado1);
            DesenharCartoesSelecao(g, painel2X, painelLargura, escolha2, confirmado2);

            using (Font instrucoes = new Font("Segoe UI", 10, FontStyle.Bold))
            using (Font rodape = new Font("Segoe UI", 9, FontStyle.Regular))
            using (StringFormat centralizado = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("A / D: trocar personagem     F: confirmar",
                    instrucoes, Brushes.DeepSkyBlue, new RectangleF(painel1X, 524, painelLargura, 24), centralizado);
                g.DrawString("← / →: trocar personagem     ENTER: confirmar",
                    instrucoes, Brushes.LightCoral, new RectangleF(painel2X, 524, painelLargura, 24), centralizado);

                string status1 = confirmado1 ? "ESCOLHA CONFIRMADA" : "ESCOLHA UM PERSONAGEM";
                string status2 = confirmado2 ? "ESCOLHA CONFIRMADA" : "ESCOLHA UM PERSONAGEM";
                g.DrawString(status1, rodape, confirmado1 ? Brushes.LightGreen : Brushes.White,
                    new RectangleF(painel1X, 550, painelLargura, 20), centralizado);
                g.DrawString(status2, rodape, confirmado2 ? Brushes.LightGreen : Brushes.White,
                    new RectangleF(painel2X, 550, painelLargura, 20), centralizado);

                string aviso = confirmado1 && confirmado2
                    ? "Preparando a batalha..."
                    : "A batalha começa quando os dois jogadores confirmarem.";
                g.DrawString(aviso, rodape, Brushes.Gold,
                    new RectangleF(0, 586, LarguraBase, 22), centralizado);
                g.DrawString("GOLPE: " + DescricaoGolpeBasico((TipoPersonagem)escolha1),
                    rodape, Brushes.DeepSkyBlue,
                    new RectangleF(painel1X, 608, painelLargura, 20), centralizado);
                g.DrawString("GOLPE: " + DescricaoGolpeBasico((TipoPersonagem)escolha2),
                    rodape, Brushes.LightCoral,
                    new RectangleF(painel2X, 608, painelLargura, 20), centralizado);
                g.DrawString("PASSIVA - " + NomePassiva((TipoPersonagem)escolha1) + ": " +
                    DetalhePassiva((TipoPersonagem)escolha1),
                    rodape, Brushes.Gold,
                    new RectangleF(painel1X, 628, painelLargura, 20), centralizado);
                g.DrawString("PASSIVA - " + NomePassiva((TipoPersonagem)escolha2) + ": " +
                    DetalhePassiva((TipoPersonagem)escolha2),
                    rodape, Brushes.Gold,
                    new RectangleF(painel2X, 628, painelLargura, 20), centralizado);
            }
        }

        private void DesenharPainelSelecao(Graphics g, RectangleF painel, string titulo, Color cor)
        {
            using (Brush fundo = new SolidBrush(Color.FromArgb(45, 8, 12, 25)))
                g.FillRectangle(fundo, painel);
            using (Pen borda = new Pen(Color.FromArgb(120, cor), 1.5f))
                g.DrawRectangle(borda, painel.X, painel.Y, painel.Width, painel.Height);
            using (Font fonte = new Font("Segoe UI", 13, FontStyle.Bold))
            using (Brush pincel = new SolidBrush(cor))
            using (StringFormat centralizado = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(titulo, fonte, pincel,
                    new RectangleF(painel.X, painel.Y + 3, painel.Width, 27), centralizado);
        }

        private void DesenharCartoesSelecao(Graphics g, float painelX, float painelLargura,
            int escolha, bool confirmado)
        {
            TipoPersonagem[] tipos = new TipoPersonagem[]
            {
                TipoPersonagem.ItadoriYuji,
                TipoPersonagem.YutaOkkotsu,
                TipoPersonagem.Choso,
                TipoPersonagem.TojiFushiguro,
                TipoPersonagem.NaoyaZenin,
                TipoPersonagem.GojoSatoru,
                TipoPersonagem.RyomenSukuna,
                TipoPersonagem.MegumiFushiguro
            };

            const float margemInterna = 12f;
            const float espacamentoColunas = 10f;
            const float topoCartoes = 114f;
            const float alturaCartao = 70f;
            const float espacamentoLinhas = 8f;
            float larguraCartao = (painelLargura - margemInterna * 2f - espacamentoColunas) / 2f;

            using (Font inicial = new Font("Segoe UI", 12, FontStyle.Bold))
            using (Font fonteNome = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font fonteDescricao = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (Font fonteEstado = new Font("Segoe UI", 6.8f, FontStyle.Bold))
            using (StringFormat centralizado = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (StringFormat textoEsquerda = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                for (int i = 0; i < tipos.Length; i++)
                {
                    int coluna = i % 2;
                    int linha = i / 2;
                    float cardX = painelX + margemInterna + coluna * (larguraCartao + espacamentoColunas);
                    float cardY = topoCartoes + linha * (alturaCartao + espacamentoLinhas);
                    RectangleF cartao = new RectangleF(cardX, cardY, larguraCartao, alturaCartao);
                    Color cor = CorPersonagem(tipos[i]);
                    bool selecionado = escolha == i;

                    using (Brush fundo = new SolidBrush(selecionado
                        ? Color.FromArgb(100, cor)
                        : Color.FromArgb(48, 235, 239, 255)))
                        g.FillRectangle(fundo, cartao);

                    using (Pen borda = new Pen(selecionado
                        ? (confirmado ? Color.LimeGreen : Color.White)
                        : Color.FromArgb(85, 180, 190, 210), selecionado ? 2.5f : 1f))
                        g.DrawRectangle(borda, cartao.X, cartao.Y, cartao.Width, cartao.Height);

                    // Miniatura real em pixel art, com a mesma rotina usada no combate.
                    using (Brush placa = new SolidBrush(Color.FromArgb(135, 9, 15, 29)))
                        g.FillRectangle(placa, cardX + 6f, cardY + 5f, 46f, 60f);
                    GraphicsState miniatura = g.Save();
                    g.TranslateTransform(cardX + 11f, cardY + 11f);
                    g.ScaleTransform(1.03f, 1.03f);
                    DesenharPersonagem(g, 0f, 0f, tipos[i], false, 0, 2, 0, 0, false, 0);
                    g.Restore(miniatura);

                    float textoX = cardX + 56f;
                    float textoLargura = larguraCartao - 64f;
                    g.DrawString(NomePersonagem(tipos[i]), fonteNome, Brushes.White,
                        new RectangleF(textoX, cardY + 8f, textoLargura, 20f), textoEsquerda);
                    g.DrawString(DescricaoPersonagem(tipos[i]), fonteDescricao, Brushes.Gainsboro,
                        new RectangleF(textoX, cardY + 29f, textoLargura, 16f), textoEsquerda);

                    if (selecionado)
                    {
                        string legenda = confirmado ? "CONFIRMADO" : "SELECIONADO";
                        using (Brush destaque = new SolidBrush(confirmado ? Color.LightGreen : Color.Gold))
                            g.DrawString(legenda, fonteEstado, destaque,
                                new RectangleF(textoX, cardY + 48f, textoLargura, 13f), textoEsquerda);
                    }
                }
            }
        }

        private string DescricaoGolpeBasico(TipoPersonagem tipo)
        {
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji: return "Punho divergente · 2 impactos";
                case TipoPersonagem.YutaOkkotsu: return "Katana amaldiçoada · alcance médio";
                case TipoPersonagem.Choso: return "Rajada carmesim · longo alcance";
                case TipoPersonagem.TojiFushiguro: return "Estocada veloz · avanço curto";
                case TipoPersonagem.NaoyaZenin: return "Impacto projetado · rápido";
                case TipoPersonagem.GojoSatoru: return "Pulso azul · empurra o rival";
                case TipoPersonagem.RyomenSukuna: return "Corte desmantelar · arco amplo";
                case TipoPersonagem.MegumiFushiguro: return "Garras sombrias · apoio de sombra";
                default: return "Ataque básico";
            }
        }

        private Color CorPersonagem(TipoPersonagem tipo)
        {
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji: return Color.FromArgb(235, 97, 127);
                case TipoPersonagem.YutaOkkotsu: return Color.FromArgb(130, 190, 255);
                case TipoPersonagem.Choso: return Color.FromArgb(186, 55, 80);
                case TipoPersonagem.TojiFushiguro: return Color.FromArgb(58, 160, 130);
                case TipoPersonagem.NaoyaZenin: return Color.DarkKhaki;
                case TipoPersonagem.GojoSatoru: return Color.DodgerBlue;
                case TipoPersonagem.RyomenSukuna: return Color.Crimson;
                case TipoPersonagem.MegumiFushiguro: return Color.MediumSlateBlue;
                default: return Color.DarkKhaki;
            }
        }

        private string NomePersonagem(TipoPersonagem tipo)
        {
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji: return "Yuji Itadori";
                case TipoPersonagem.YutaOkkotsu: return "Yuta Okkotsu";
                case TipoPersonagem.Choso: return "Choso";
                case TipoPersonagem.TojiFushiguro: return "Toji Fushiguro";
                case TipoPersonagem.NaoyaZenin: return "Naoya Zenin";
                case TipoPersonagem.GojoSatoru: return "Gojo Satoru";
                case TipoPersonagem.RyomenSukuna: return "Ryomen Sukuna";
                case TipoPersonagem.MegumiFushiguro: return "Megumi Fushiguro";
                default: return "Personagem";
            }
        }

        private string DescricaoPersonagem(TipoPersonagem tipo)
        {
            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji: return "Black Flash";
                case TipoPersonagem.YutaOkkotsu: return "Rika e corte amaldiçoado";
                case TipoPersonagem.Choso: return "Sangue Perfurante";
                case TipoPersonagem.TojiFushiguro: return "Lança Invertida do Céu";
                case TipoPersonagem.NaoyaZenin: return "Avanço veloz e atordoa";
                case TipoPersonagem.GojoSatoru: return "Disparo Roxo";
                case TipoPersonagem.RyomenSukuna: return "Cortes em área";
                case TipoPersonagem.MegumiFushiguro: return "Invoca 1 sombra aleatória";
                default: return "Habilidade especial.";
            }
        }

        private void DesenharBarraVida(Graphics g, int bx, int by,
            int vida, Color cor, string nome)
        {
            using (Font f = new Font("Arial", 10, FontStyle.Bold))
                g.DrawString(nome + " " + vida + "/100",
                    f, Brushes.White, bx, by);

            using (Brush fundo = new SolidBrush(Color.DarkRed))
                g.FillRectangle(fundo, bx, by + 23, 220, 18);

            int largura = (int)(220 * vida / 100.0);

            if (largura > 0)
            {
                using (Brush b = new SolidBrush(cor))
                    g.FillRectangle(b, bx, by + 23, largura, 18);
            }

            g.DrawRectangle(Pens.White, bx, by + 23, 220, 18);
        }

        private void DesenharPersonagem(Graphics g, float px, float py,
            TipoPersonagem tipo, bool andandoLocal, int quadro,
            int direcao, int ataque, int congelado, bool energiaYutaAtiva, int dashVisual)
        {
            int passo = andandoLocal ? quadro * 3 : 0;

            Color roupa, cabelo, pele = Color.PeachPuff;

            switch (tipo)
            {
                case TipoPersonagem.ItadoriYuji:
                    roupa = Color.FromArgb(32, 35, 58); // uniforme escuro
                    cabelo = Color.FromArgb(236, 134, 161); // cabelo rosa
                    pele = Color.FromArgb(242, 204, 184);
                    break;
                case TipoPersonagem.YutaOkkotsu:
                    roupa = Color.FromArgb(229, 232, 238); // jaqueta clara
                    cabelo = Color.FromArgb(29, 31, 44); // cabelo preto
                    pele = Color.FromArgb(242, 210, 195);
                    break;
                case TipoPersonagem.Choso:
                    roupa = Color.FromArgb(221, 216, 204); // roupa clara e gola roxa
                    cabelo = Color.FromArgb(28, 24, 35); // cabelo escuro preso
                    pele = Color.FromArgb(235, 202, 182);
                    break;
                case TipoPersonagem.TojiFushiguro:
                    roupa = Color.FromArgb(29, 51, 66); // camiseta azul-escura
                    cabelo = Color.FromArgb(21, 25, 30); // cabelo preto espetado
                    pele = Color.FromArgb(227, 186, 160);
                    break;
                case TipoPersonagem.GojoSatoru:
                    roupa = Color.FromArgb(25, 29, 42);
                    cabelo = Color.FromArgb(235, 239, 250);
                    pele = Color.PeachPuff;
                    break;
                case TipoPersonagem.RyomenSukuna:
                    roupa = Color.FromArgb(125, 35, 48);
                    cabelo = Color.FromArgb(220, 145, 155);
                    pele = Color.FromArgb(245, 205, 183);
                    break;
                case TipoPersonagem.MegumiFushiguro:
                    roupa = Color.FromArgb(25, 31, 50);
                    cabelo = Color.FromArgb(25, 25, 34);
                    pele = Color.FromArgb(238, 204, 181);
                    break;
                default: // Naoya Zenin: uniforme escuro e cabelo loiro-esverdeado.
                    roupa = Color.FromArgb(35, 38, 48);
                    cabelo = Color.FromArgb(190, 184, 112);
                    pele = Color.PeachPuff;
                    break;
            }

            using (Brush sombra = new SolidBrush(Color.FromArgb(150, 2, 7, 18)))
                g.FillEllipse(sombra, px - 2, py + 35, 38, 11);
            if (ataque > 0 || dashVisual > 0 || energiaYutaAtiva)
            {
                Color auraCor = CorPersonagem(tipo);
                using (Pen aura = new Pen(Color.FromArgb(155, auraCor), 2f))
                    g.DrawEllipse(aura, px - 8, py - 9, 48, 57);
            }

            if (tipo == TipoPersonagem.NaoyaZenin && dashVisual > 0)
            {
                int pulsacao = (dashVisual % 3) * 2;
                using (Pen aura = new Pen(Color.FromArgb(170, 239, 220, 138), 2f))
                using (Pen auraInterna = new Pen(Color.FromArgb(110, 245, 245, 210), 1f))
                {
                    g.DrawEllipse(aura, px - 7 - pulsacao, py - 7 - pulsacao,
                        46 + pulsacao * 2, 54 + pulsacao * 2);
                    g.DrawEllipse(auraInterna, px - 3, py - 3, 38, 46);
                }
            }

            if (dashVisual > 0)
            {
                using (Pen rastro = new Pen(Color.FromArgb(190, Color.Khaki), 2f))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float desloc = i * 5;
                        if (direcao == 2)
                            g.DrawLine(rastro, px - 6 - desloc, py + 8 + i * 10, px - 22 - desloc, py + 8 + i * 10);
                        else if (direcao == 1)
                            g.DrawLine(rastro, px + 32 + desloc, py + 8 + i * 10, px + 48 + desloc, py + 8 + i * 10);
                        else if (direcao == 0)
                            g.DrawLine(rastro, px + 7 + i * 9, py - 5 - desloc, px + 7 + i * 9, py - 18 - desloc);
                        else
                            g.DrawLine(rastro, px + 7 + i * 9, py + 42 + desloc, px + 7 + i * 9, py + 55 + desloc);
                    }
                }
            }

            if (tipo == TipoPersonagem.NaoyaZenin && dashVisual > 0)
            {
                using (Pen corte = new Pen(Color.FromArgb(200, 246, 231, 166), 3f))
                {
                    if (direcao == 1 || direcao == 2)
                    {
                        int sinal = direcao == 2 ? -1 : 1;
                        g.DrawLine(corte, px + 8, py + 3, px + 8 + sinal * 31, py - 9);
                        g.DrawLine(corte, px + 8, py + 14, px + 8 + sinal * 37, py + 9);
                        g.DrawLine(corte, px + 8, py + 26, px + 8 + sinal * 27, py + 28);
                    }
                    else
                    {
                        g.DrawLine(corte, px + 2, py + 8, px - 10, py + 8 + (direcao == 0 ? 1 : -1) * 27);
                        g.DrawLine(corte, px + 15, py + 8, px + 15, py + 8 + (direcao == 0 ? 1 : -1) * 34);
                        g.DrawLine(corte, px + 27, py + 8, px + 39, py + 8 + (direcao == 0 ? 1 : -1) * 25);
                    }
                }
            }

            using (Brush b = new SolidBrush(cabelo))
            {
                g.FillRectangle(b, px + 6, py, 20, 10);
                g.FillRectangle(b, px + 3, py + 5, 26, 7);

                if (tipo == TipoPersonagem.GojoSatoru)
                {
                    g.FillPolygon(b, new Point[] {
                        new Point((int)px + 3, (int)py + 8),
                        new Point((int)px + 0, (int)py - 1),
                        new Point((int)px + 10, (int)py + 3),
                        new Point((int)px + 13, (int)py - 5),
                        new Point((int)px + 19, (int)py + 2),
                        new Point((int)px + 27, (int)py - 2),
                        new Point((int)px + 29, (int)py + 9)
                    });
                }
                else if (tipo == TipoPersonagem.RyomenSukuna)
                {
                    g.FillPolygon(b, new Point[] {
                        new Point((int)px + 3, (int)py + 8),
                        new Point((int)px + 1, (int)py - 2),
                        new Point((int)px + 10, (int)py + 2),
                        new Point((int)px + 15, (int)py - 3),
                        new Point((int)px + 22, (int)py + 2),
                        new Point((int)px + 28, (int)py - 1),
                        new Point((int)px + 29, (int)py + 9)
                    });
                }
                else if (tipo == TipoPersonagem.MegumiFushiguro)
                {
                    g.FillPolygon(b, new Point[] {
                        new Point((int)px + 3, (int)py + 9),
                        new Point((int)px - 1, (int)py - 4),
                        new Point((int)px + 7, (int)py + 1),
                        new Point((int)px + 10, (int)py - 10),
                        new Point((int)px + 15, (int)py + 1),
                        new Point((int)px + 21, (int)py - 7),
                        new Point((int)px + 23, (int)py + 2),
                        new Point((int)px + 31, (int)py - 3),
                        new Point((int)px + 28, (int)py + 10)
                    });
                }
                else if (tipo == TipoPersonagem.ItadoriYuji)
                {
                    // Cabelo curto, rosa e pontudo do Itadori.
                    g.FillPolygon(b, new PointF[] {
                        new PointF(px + 3, py + 8), new PointF(px + 1, py - 4),
                        new PointF(px + 9, py + 1), new PointF(px + 15, py - 7),
                        new PointF(px + 19, py + 1), new PointF(px + 27, py - 4),
                        new PointF(px + 31, py + 9)
                    });
                }
                else if (tipo == TipoPersonagem.YutaOkkotsu)
                {
                    // Franja caída e cabelo preto do Yuta.
                    g.FillPolygon(b, new PointF[] {
                        new PointF(px + 2, py + 7), new PointF(px + 4, py - 5),
                        new PointF(px + 22, py - 3), new PointF(px + 31, py + 5),
                        new PointF(px + 24, py + 13), new PointF(px + 19, py + 6),
                        new PointF(px + 12, py + 13), new PointF(px + 8, py + 6)
                    });
                }
                else if (tipo == TipoPersonagem.Choso)
                {
                    // Cabelo repartido com duas mechas presas atrás da cabeça.
                    g.FillPolygon(b, new PointF[] {
                        new PointF(px + 3, py + 9), new PointF(px + 6, py - 5),
                        new PointF(px + 16, py - 2), new PointF(px + 19, py - 6),
                        new PointF(px + 28, py + 2), new PointF(px + 30, py + 11),
                        new PointF(px + 23, py + 8), new PointF(px + 16, py + 3),
                        new PointF(px + 11, py + 9)
                    });
                    g.FillEllipse(b, px - 2, py + 1, 9, 11);
                    g.FillEllipse(b, px + 25, py + 1, 9, 11);
                }
                else if (tipo == TipoPersonagem.TojiFushiguro)
                {
                    // Cabelo preto curto e desalinhado do Toji.
                    g.FillPolygon(b, new PointF[] {
                        new PointF(px + 2, py + 10), new PointF(px + 1, py - 2),
                        new PointF(px + 7, py + 2), new PointF(px + 11, py - 7),
                        new PointF(px + 17, py + 1), new PointF(px + 23, py - 5),
                        new PointF(px + 27, py + 2), new PointF(px + 32, py + 3),
                        new PointF(px + 28, py + 12), new PointF(px + 21, py + 6),
                        new PointF(px + 10, py + 11)
                    });
                }
            }

            using (Brush b = new SolidBrush(pele))
            {
                g.FillRectangle(b, px + 7, py + 8, 18, 13);
                g.FillRectangle(b, px, py + 22 + passo, 4, 8);
                g.FillRectangle(b, px + 28, py + 22 + (3 - passo), 4, 8);
            }

            if (direcao != 3)
            {
                g.FillRectangle(Brushes.Black, px + 10, py + 12, 3, 3);
                g.FillRectangle(Brushes.Black, px + 19, py + 12, 3, 3);
            }

            if (tipo == TipoPersonagem.MegumiFushiguro)
            {
                // Olhos escuros e gola alta do uniforme de Megumi.
                using (Brush gola = new SolidBrush(Color.FromArgb(10, 13, 24)))
                    g.FillRectangle(gola, px + 7, py + 19, 18, 5);
            }
            else if (tipo == TipoPersonagem.GojoSatoru)
            {
                // Faixa escura sobre os olhos, marca visual do Gojo nesta versão pixel-art.
                using (Brush venda = new SolidBrush(Color.FromArgb(18, 20, 27)))
                    g.FillRectangle(venda, px + 5, py + 10, 22, 5);
                using (Pen brilho = new Pen(Color.FromArgb(100, 120, 185, 255), 1f))
                    g.DrawLine(brilho, px + 6, py + 11, px + 26, py + 11);
            }
            else if (tipo == TipoPersonagem.RyomenSukuna)
            {
                using (Pen marca = new Pen(Color.FromArgb(190, 45, 20, 28), 2f))
                {
                    g.DrawLine(marca, px + 8, py + 10, px + 13, py + 15);
                    g.DrawLine(marca, px + 13, py + 10, px + 8, py + 15);
                    g.DrawLine(marca, px + 20, py + 10, px + 25, py + 15);
                    g.DrawLine(marca, px + 25, py + 10, px + 20, py + 15);
                    g.DrawLine(marca, px + 13, py + 19, px + 21, py + 19);
                }
            }
            else if (tipo == TipoPersonagem.ItadoriYuji)
            {
                using (Brush cicatriz = new SolidBrush(Color.FromArgb(190, 154, 63, 83)))
                {
                    g.FillRectangle(cicatriz, px + 9, py + 16, 5, 1);
                    g.FillRectangle(cicatriz, px + 19, py + 16, 5, 1);
                }
            }
            else if (tipo == TipoPersonagem.YutaOkkotsu)
            {
                // Olheiras características do Yuta.
                using (Pen olheira = new Pen(Color.FromArgb(145, 93, 84, 101), 1f))
                {
                    g.DrawLine(olheira, px + 9, py + 16, px + 15, py + 17);
                    g.DrawLine(olheira, px + 18, py + 17, px + 24, py + 16);
                }
            }
            else if (tipo == TipoPersonagem.Choso)
            {
                // Faixa escura horizontal cruzando o nariz (estilo pixel art).
                using (Brush marca = new SolidBrush(Color.FromArgb(135, 70, 65)))
                    g.FillRectangle(marca, px + 8, py + 16, 16, 3);
                using (Brush fita = new SolidBrush(Color.FromArgb(128, 54, 86)))
                {
                    g.FillRectangle(fita, px - 1, py + 6, 5, 3);
                    g.FillRectangle(fita, px + 29, py + 6, 5, 3);
                }
            }
            else if (tipo == TipoPersonagem.TojiFushiguro)
            {
                // Cicatriz discreta no canto da boca.
                using (Pen cicatriz = new Pen(Color.FromArgb(165, 93, 74), 1f))
                {
                    g.DrawLine(cicatriz, px + 21, py + 19, px + 25, py + 21);
                    g.DrawLine(cicatriz, px + 25, py + 21, px + 28, py + 19);
                }
            }

            using (Brush b = new SolidBrush(roupa))
                g.FillRectangle(b, px + 4, py + 21, 24, 12);

            if (tipo == TipoPersonagem.ItadoriYuji)
            {
                // Capuz e gola vermelhos sobre o uniforme preto.
                using (Brush capuz = new SolidBrush(Color.FromArgb(180, 52, 65)))
                {
                    g.FillRectangle(capuz, px + 7, py + 21, 18, 4);
                    g.FillRectangle(capuz, px + 13, py + 25, 6, 7);
                }
            }
            else if (tipo == TipoPersonagem.YutaOkkotsu)
            {
                // Gola alta e fecho da jaqueta branca.
                using (Brush gola = new SolidBrush(Color.FromArgb(249, 249, 250)))
                using (Pen costura = new Pen(Color.FromArgb(100, 112, 132), 1f))
                {
                    g.FillRectangle(gola, px + 9, py + 18, 15, 8);
                    g.DrawLine(costura, px + 15, py + 25, px + 15, py + 33);
                    g.DrawLine(costura, px + 10, py + 25, px + 20, py + 25);
                }
            }
            else if (tipo == TipoPersonagem.Choso)
            {
                // Gola roxa e faixa vermelho-vinho sobre o traje claro.
                using (Brush gola = new SolidBrush(Color.FromArgb(82, 47, 85)))
                using (Brush faixa = new SolidBrush(Color.FromArgb(149, 43, 66)))
                {
                    g.FillRectangle(gola, px + 8, py + 21, 16, 4);
                    g.FillRectangle(faixa, px + 13, py + 25, 7, 8);
                    g.FillRectangle(gola, px + 5, py + 26, 3, 7);
                    g.FillRectangle(gola, px + 25, py + 26, 3, 7);
                }
            }
            else if (tipo == TipoPersonagem.TojiFushiguro)
            {
                // Manga curta, braços à mostra e cintura da calça clara.
                using (Brush cintura = new SolidBrush(Color.FromArgb(179, 171, 157)))
                using (Brush braço = new SolidBrush(pele))
                {
                    g.FillRectangle(cintura, px + 6, py + 31, 20, 4);
                    g.FillRectangle(braço, px + 1, py + 26, 4, 5);
                    g.FillRectangle(braço, px + 27, py + 26, 4, 5);
                }
            }

            Color corCalca = tipo == TipoPersonagem.TojiFushiguro
                ? Color.FromArgb(201, 196, 182)
                : tipo == TipoPersonagem.Choso
                    ? Color.FromArgb(55, 43, 63)
                    : Color.DarkSlateBlue;
            using (Brush b = new SolidBrush(corCalca))
            {
                g.FillRectangle(b, px + 7, py + 33, 7, 7 - passo);
                g.FillRectangle(b, px + 18, py + 33, 7, 4 + passo);
            }

            using (Brush bota = new SolidBrush(tipo == TipoPersonagem.TojiFushiguro
                ? Color.FromArgb(35, 39, 38) : Color.SaddleBrown))
            {
                g.FillRectangle(bota, px + 6, py + 38, 8, 4);
                g.FillRectangle(bota, px + 18, py + 38, 8, 4);
            }

            // Armas: Yuta usa a katana; Toji usa a Lança Invertida do Céu.
            // Choso projeta energia vermelha em seu ataque comum.
            if (tipo == TipoPersonagem.YutaOkkotsu)
            {
                float cx = px + 16f;
                float cy = py + 25f;
                float comprimento = ataque > 0 ? 38f : 27f;
                float dxArma = direcao == 1 ? -comprimento :
                               direcao == 2 ? comprimento : 8f;
                float dyArma = direcao == 0 ? comprimento :
                               direcao == 3 ? -comprimento : 4f;

                using (Pen cabo = new Pen(Color.FromArgb(55, 52, 66), 4f))
                using (Pen lamina = new Pen(Color.FromArgb(214, 233, 248), 3f))
                {
                    cabo.StartCap = LineCap.Round;
                    cabo.EndCap = LineCap.Round;
                    lamina.StartCap = LineCap.Round;
                    lamina.EndCap = LineCap.Round;
                    g.DrawLine(cabo, cx, cy, cx + dxArma * 0.3f, cy + dyArma * 0.3f);
                    g.DrawLine(lamina, cx + dxArma * 0.3f, cy + dyArma * 0.3f,
                        cx + dxArma, cy + dyArma);
                }
                if (energiaYutaAtiva)
                {
                    using (Pen energia = new Pen(Color.FromArgb(205, 101, 193, 255), 4f))
                        g.DrawLine(energia, cx + dxArma * 0.3f, cy + dyArma * 0.3f,
                            cx + dxArma, cy + dyArma);
                }
            }
            else if (tipo == TipoPersonagem.TojiFushiguro)
            {
                float cx = px + 16f, cy = py + 25f;
                float comprimento = ataque > 0 ? 43f : 31f;
                float dxArma = direcao == 1 ? -comprimento :
                               direcao == 2 ? comprimento : 7f;
                float dyArma = direcao == 0 ? comprimento :
                               direcao == 3 ? -comprimento : -5f;

                using (Pen empunhadura = new Pen(Color.FromArgb(67, 52, 57), 5f))
                using (Pen lamina = new Pen(Color.FromArgb(174, 204, 194), 4f))
                using (Pen fio = new Pen(Color.FromArgb(110, 242, 186), 1.5f))
                {
                    empunhadura.StartCap = LineCap.Round;
                    lamina.EndCap = LineCap.Round;
                    g.DrawLine(empunhadura, cx, cy,
                        cx + dxArma * 0.45f, cy + dyArma * 0.45f);
                    g.DrawLine(lamina, cx + dxArma * 0.45f, cy + dyArma * 0.45f,
                        cx + dxArma, cy + dyArma);
                    g.DrawLine(fio, cx + dxArma * 0.65f, cy + dyArma * 0.65f,
                        cx + dxArma, cy + dyArma);
                }
                // A lâmina em formato de punhal tem um dente lateral.
                using (Pen gancho = new Pen(Color.FromArgb(175, 214, 203), 3f))
                    g.DrawLine(gancho, cx + dxArma * 0.69f, cy + dyArma * 0.69f,
                        cx + dxArma * 0.69f - dyArma * 0.18f,
                        cy + dyArma * 0.69f + dxArma * 0.18f);
            }
            else if (tipo == TipoPersonagem.NaoyaZenin)
            {
                int comprimento = ataque > 0 ? 34 : 22;
                using (Pen bastao = new Pen(Color.SaddleBrown, 5))
                {
                    bastao.StartCap = LineCap.Round;
                    bastao.EndCap = LineCap.Round;
                    float cx = px + 16f;
                    float cy = py + 25f;
                    if (direcao == 0)
                        g.DrawLine(bastao, cx + 9, cy, cx + 9, cy + comprimento);
                    else if (direcao == 3)
                        g.DrawLine(bastao, cx - 9, cy, cx - 9, cy - comprimento);
                    else if (direcao == 1)
                        g.DrawLine(bastao, cx, cy + 5, cx - comprimento, cy + 5);
                    else
                        g.DrawLine(bastao, cx, cy + 5, cx + comprimento, cy + 5);
                }
            }
            else if (tipo == TipoPersonagem.ItadoriYuji && ataque > 0)
            {
                // Punho coberto de energia vermelha.
                float maoX = direcao == 1 ? px - 8f : px + 31f;
                using (Pen flash = new Pen(Color.FromArgb(220, 235, 73, 110), 4f))
                    g.DrawEllipse(flash, maoX - 8f, py + 17f, 17f, 17f);
            }
            else if (tipo == TipoPersonagem.Choso && ataque > 0)
            {
                // Pequeno disparo estilizado de energia carmesim.
                float sentidoX = direcao == 1 ? -1f : direcao == 2 ? 1f : 0f;
                float sentidoY = direcao == 0 ? 1f : direcao == 3 ? -1f : 0f;
                float cx = px + 16f, cy = py + 24f;
                using (Pen disparo = new Pen(Color.FromArgb(235, 189, 37, 69), 5f))
                using (Pen brilho = new Pen(Color.FromArgb(240, 255, 192, 207), 1.5f))
                {
                    disparo.StartCap = LineCap.Round;
                    disparo.EndCap = LineCap.Round;
                    g.DrawLine(disparo, cx, cy, cx + sentidoX * 33f, cy + sentidoY * 33f);
                    g.DrawLine(brilho, cx, cy, cx + sentidoX * 33f, cy + sentidoY * 33f);
                }
            }

            if (congelado > 0)
            {
                using (Pen p = new Pen(Color.Cyan, 3))
                {
                    g.DrawRectangle(p, px - 4, py - 4, 40, 49);
                    g.DrawLine(p, px, py + 5, px + 28, py + 35);
                    g.DrawLine(p, px + 28, py + 5, px, py + 35);
                }
            }

            if (energiaYutaAtiva && tipo == TipoPersonagem.YutaOkkotsu)
            {
                using (Pen p = new Pen(Color.FromArgb(125, 194, 255), 3f))
                    g.DrawRectangle(p, px - 3, py - 3, 38, 46);
            }
        }

        private void DesenharTelaVitoria(Graphics g)
        {
            TipoPersonagem campeao = vida1 <= 0 ? personagem2 : personagem1;
            Color cor = CorPersonagem(campeao);
            float cx = ClientSize.Width / 2f, cy = ClientSize.Height / 2f;
            using (Brush sombra = new SolidBrush(Color.FromArgb(220, 3, 6, 17)))
                g.FillRectangle(sombra, ClientRectangle);
            using (Pen anel = new Pen(Color.FromArgb(110, cor), 4f))
            using (Pen anel2 = new Pen(Color.FromArgb(75, 218, 230, 253), 2f))
            {
                g.DrawEllipse(anel, cx - 195, cy - 195, 390, 390);
                g.DrawEllipse(anel2, cx - 155, cy - 155, 310, 310);
            }
            using (Brush painel = new SolidBrush(Color.FromArgb(246, 18, 23, 40)))
            using (Pen borda = new Pen(Color.FromArgb(205, cor), 2f))
            {
                g.FillRectangle(painel, cx - 278, cy - 135, 556, 267);
                g.DrawRectangle(borda, cx - 278, cy - 135, 556, 267);
            }
            using (Font pequeno = new Font("Segoe UI", 12, FontStyle.Bold))
            using (Font titulo = new Font("Segoe UI", 30, FontStyle.Bold))
            using (Font nome = new Font("Segoe UI", 19, FontStyle.Bold))
            using (Brush destaque = new SolidBrush(cor))
            using (StringFormat centro = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("FIM DE COMBATE  ·  ARENA JUJUTSU", pequeno, destaque,
                    new RectangleF(cx - 265, cy - 110, 530, 28), centro);
                g.DrawString(vida1 <= 0 ? "JOGADOR 2 VENCEU" : "JOGADOR 1 VENCEU", titulo,
                    Brushes.White, new RectangleF(cx - 260, cy - 58, 520, 64), centro);
                g.DrawString(NomePersonagem(campeao).ToUpperInvariant(), nome, destaque,
                    new RectangleF(cx - 260, cy + 17, 520, 40), centro);
                g.DrawString("USE REINICIAR ACIMA PARA UMA REVANCHE", pequeno, Brushes.Gainsboro,
                    new RectangleF(cx - 250, cy + 79, 500, 30), centro);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timerJogo.Stop();
            timerJogo.Dispose();
            base.OnFormClosed(e);
        }
        // Ponte entre a lógica original do WinForms e o loop do Blazor.
        public override void StepFrame() => timerJogo.Fire();

    }
}
