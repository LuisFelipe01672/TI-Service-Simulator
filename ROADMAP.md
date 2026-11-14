# Roadmap — TI Service Simulator

Plano de longo prazo. Cada fase termina **testada na Unity e com build**. Regras que valem sempre:
realismo (animação em toda ação, pista visual para todo problema), marcas fictícias, valores calibrados com o dono
(sem publicar números exatos), push só com pedido. Skills do projeto: `testar-na-unity`, `baixar-modelo-3d`,
`publicar-ti-service`.

Legenda: ✅ feito · 🔜 próxima · ⬜ a fazer · ❓ decisão do dono

## Visão
Simulador de técnico de TI brasileiro, do MEI em casa até uma empresa com sede, equipe e contratos. O prazer vem de
quatro coisas (inspiração em simuladores de trabalho do gênero):
1. **Trabalho físico satisfatório** — cada tarefa é feita com as mãos: abrir caixa, crimpar, tirar poeira, trocar
   peça, organizar rack (antes/depois).
2. **Diagnóstico de verdade** — o cliente descreve o problema de forma vaga ("tá lento", "a internet caiu"); você
   pergunta, mede, testa e descobre a causa.
3. **Crescimento visível** — o dinheiro vira coisas que você vê: ferramentas, carro, sede maior, prateleiras cheias,
   funcionários fazendo o que antes era você.
4. **Você melhora como técnico** — experiência por tipo de serviço, certificações, reputação na cidade.

E o tempero da **TI real**: "não mexi em nada", senha no post-it, backup nunca testado, toner acabando sexta às 17h,
certificado digital vencido, domínio vencido derrubando o e-mail, operadora que não resolve, cliente que não paga.

## Sistemas transversais (servem a todas as fases)
Construídos uma vez; cada equipamento novo só pluga as suas peças, causas e ações.

**Pilar gráfico** — o mais realista possível sem travar PCs médios
- ⬜ Post-processing: tonemapping ACES, bloom leve, oclusão de ambiente (SSAO), gradação de cor, profundidade de
      campo só nos closes
- ⬜ Iluminação baked (lightmaps) nos interiores + reflection probes + light probes para personagens
- ⬜ Materiais PBR (ambientCG/Poly Haven) em todo piso, parede, metal e plástico; sujeira/desgaste onde faz sentido
- ⬜ Modelos realistas (Poly Haven CC0 / Sketchfab CC-BY) substituindo as caixas, cômodo por cômodo
- ⬜ Opções gráficas (baixo/médio/alto/ultra)

**Sistema de diagnóstico**
- ⬜ Chamado vago → lista de causas possíveis com peso; cada causa deixa pistas (LED, bipe, mensagem, cheiro de
      queimado, poeira, código na placa POST, leitura do multímetro)
- ⬜ **Conversa de triagem**: por ligação/mensagem você escolhe perguntas ("o que mudou?", "aparece alguma
      mensagem?", "só nesse computador?"); o cliente responde mal, esquece ou mente ("não mexi em nada")
- ⬜ **Saúde das peças** em % (HD 34%, bateria do nobreak 10%, cilindro 80%) vista com a ferramenta certa;
      desgaste com o tempo de uso
- ⬜ Ferramentas compradas liberam ou aceleram pistas (ver Fases 4 e 5)

**Ordem de serviço (OS) e checklist**
- ⬜ Todo atendimento abre uma **OS**: problema relatado, diagnóstico, o que foi feito, peças, horas, deslocamento
- ⬜ **Checklist do serviço** na prancheta/celular (ex.: "testar todos os pontos", "etiquetar", "limpar o local");
      o cliente confere no fim — item esquecido vira retrabalho ou estrela a menos
- ⬜ **Cliente assina a OS**; sem assinatura ele pode contestar a cobrança
- ⬜ **Teste de prova** no fim (temperatura e desempenho antes/depois, página de teste, teste de cabo) anexado à OS
- ⬜ **Fotos de antes/depois** anexadas à OS vão para o **portfólio** no celular e atraem clientes

**O técnico (personagem)**
- ⬜ **Energia/cansaço**: trabalhar até tarde rende hoje e cobra amanhã (mais lento, mais erro); dormir e comer
      recuperam; café dá um empurrão curto
- ⬜ **Experiência por tipo de serviço** (cabeamento, impressoras, computadores, redes, CFTV): mais rápido e menos
      erro com a prática; ligada às certificações
- ⬜ **Reputação na cidade** além das estrelas: clientes grandes só chamam quem tem nome
- ⬜ **Pressa tem risco**: subir a escada correndo, mexer na fonte sem desligar, crimpar sem conferir a ordem

**Celular e PC de casa**
- ⬜ Celular: mensagens fictícias com clientes (pedidos, fotos, cobrança), ligações de triagem, orçamentos, OS,
      GPS, banco, portfólio, alertas de monitoramento
- ⬜ PC de casa: sistema de chamados, loja, banco, **atendimento remoto** e **painel de monitoramento**

---

## Fase 0 — Fundação (próxima sessão)
- 🔜 Abrir a Unity pelo Hub → instalar glTFast e importar os 11 modelos de `Assets/Models/Sketchfab` (commitar os .meta)
- ✅ Commit e push dos modelos + `CREDITOS.md` + skills + este roadmap (1f03f7f)
- ⬜ `CLAUDE.md` na raiz com as convenções do projeto (português, UI por código, managers por evento, saves)
- ⬜ Tela de **Créditos** no menu principal (autores do `CREDITOS.md`; exigido pelo CC-BY)
- ⬜ Corrigir a marcação falsa de "cena modificada" ao gerar o executável
- ⬜ **Teste gráfico decisivo**: montar um cômodo de prova (home office) com os mesmos modelos e texturas no
      Built-in + post-processing e numa cópia do projeto em **HDRP** (e/ou URP); comparar visual e FPS
- ❓ Escolher o pipeline gráfico definitivo **antes** de construir a casa (trocar depois obriga a refazer
      materiais e luz)

## Fase 1A — Casa jogável (quarto + home office)
O jogo passa a **começar em casa** (acordando na cama), não no escritório.
- ⬜ Quarto realista: cama de casal, criado-mudo, abajur, guarda-roupa, cortina, tapete, luz baked
- ⬜ Home office: mesa, cadeira, **PC do jogador**, prateleira de ferramentas, mochila física
- ⬜ Interruptores que acendem luzes, relógio de parede com a hora do jogo, sol pela janela
- ⬜ **Atendimento remoto** pelo PC de casa (programa fictício): resolver de longe senha, impressora padrão, driver,
      e-mail — mais rápido e barato, mas nem tudo dá; o cliente precisa estar no computador e aceitar a conexão
- ⬜ **Celular do técnico** (primeira versão: mensagens, ligações de triagem, orçamentos, GPS)
- ⬜ **Energia do técnico** + dormir/comer (primeira versão do personagem)
- ⬜ Rotina da manhã: acordar → café (bônus de energia) → ver chamados no celular/PC → sair de casa

## Fase 1B — Casa completa (cenário)
- ⬜ Sala (sofá, TV, rack, quadros), cozinha (geladeira, fogão, pia, micro-ondas, cafeteira) e banheiro realistas
- ⬜ Pisos/azulejos/paredes PBR em todos os cômodos
- ⬜ Comer de verdade na cozinha (geladeira com compras do mercado) — liga com a energia
- ❓ Fontes: Poly Haven (CC0) primeiro; Sketchfab CC-BY para o que faltar

## Fase 2 — Compras físicas, estoque e fornecedores
- ⬜ Tudo que é comprado chega como **caixa na porta** (casa ou sede), no horário de entrega; pegar e levar
- ⬜ **Caixa de cabo de rede 305 m** realista (marca fictícia, "Cat6 · 305 m"), com o cabo saindo pelo furo;
      na bancada o cabo sai da caixa ao fazer um cabo
- ⬜ Saquinho de RJ45, caixa de toner, ferramentas e equipamentos de locação como objetos
- ⬜ Cabos prontos enrolados (com o tamanho escrito) na mochila/bancada
- ⬜ **Prateleiras de estoque** na sede: guardar cada item e ver o que está acabando (ligado ao estoque do sistema)
- ⬜ Fornecedores com preço, prazo e confiabilidade diferentes: **entrega atrasada**, **peça errada**, peça com defeito
- ⬜ **Preço de mercado oscilando** (dólar, falta de chip, promoção): decidir quando comprar estoque
- ⬜ **Garantia/RMA**: peça defeituosa volta ao fabricante e demora (ou você paga outra)
- ⬜ **Peças usadas e recondicionadas**: lote de equipamentos de empresa que fechou (leilão) e equipamentos
      descartados para testar, recuperar e revender ou colocar em locação
- ⬜ **Descarte correto**: apagar dados de verdade (LGPD) antes de revender; lixo eletrônico para o ponto de coleta

## Fase 3 — Impressora completa e locação  (detalhe em `PLANO_FASE_IMPRESSORA.md`)
- ✅ Peças com mira + E, tampa animada, crimpagem animada em 3D
- ⬜ Modelo realista (Impressora_MFP_Compacta / MFP_Escritorio) com logotipo fictício e visor com texto real
- ⬜ Mão/item segurado + close de câmera
- ⬜ Toner completo (tirar, olhar, recolocar o mesmo, novo, chacoalhar, lacre, encaixar; toner usado na mochila)
- ⬜ Traseira: plugue RJ45 animado, LEDs, testador com luzes 1–8
- ⬜ Página de teste saindo com o resultado (ok, falhada, branca, listras)
- ⬜ Problemas: atolamento, sem papel, cilindro riscado, IP mudou, tampa, driver, fila travada — via sistema de
      diagnóstico ("a impressora não imprime" pode ser qualquer um)
- ⬜ **Franquia de páginas + excedente**: todo mês ler o contador (no visor ou pelo sistema) e cobrar o excedente
- ⬜ **Peças por tempo de uso**: cilindro, fusor e rolos duram X mil páginas; trocar antes evita chamado, tarde
      demais gera chamado e cliente bravo
- ⬜ **Toner reserva no cliente** (estoque deixado lá) para evitar visita urgente; controlar quanto sobrou

## Fase 4 — Computador completo
- ⬜ Modelo realista (Computador_Mesa) + gabinete que abre (tampa lateral)
- ⬜ Peças internas animáveis: memória, SSD/HD, fonte, cooler, placa de vídeo, bateria da placa-mãe
- ⬜ **Limpeza satisfatória**: poeira saindo com ar comprimido e pincel (antes/depois); cooler sujo = superaquecimento
- ⬜ **Pasta térmica** (tirar a velha, aplicar a nova) e **organização de cabos** dentro do gabinete
- ⬜ Cabos atrás: rede, energia, vídeo (monitor sem imagem por cabo solto)
- ⬜ Monitor mostrando o "Windows" do jogo na própria tela 3D (o computador simulado que já existe)
- ⬜ **Ferramentas de diagnóstico**: testador de fonte, multímetro, **placa POST**, pendrive de diagnóstico (teste
      de memória, saúde do disco, antivírus), testador de cabo
- ⬜ **Check-up completo** (serviço vendável): inspeção de todas as peças com laudo de saúde em %
- ⬜ **Compatibilidade de peças**: memória do tipo errado não encaixa, fonte fraca desliga sob carga
- ⬜ Problemas: lentidão, vírus/ransomware, Windows não inicia, memória ruim (bipes), fonte queimada, HD com setor
      ruim (backup antes de trocar), atualização do Windows quebrou o sistema da contabilidade
- ⬜ Peças trocadas fisicamente; peça velha volta para a mochila (sucata ou RMA)
- ⬜ Montagem de PC sob encomenda (orçamento → peças → montagem → teste de desempenho)
- ⬜ **Notebook**: tela, teclado, bateria estufada, dobradiça, limpeza

## Fase 5 — Rede, infraestrutura, CFTV e energia
- ⬜ Roteador realista (luzes reais por estado, reset com clipe, painel web já existente)
- ⬜ Switch de mesa e de rack, rack com patch panel (modelos já baixados)
- ⬜ **Projeto de cabeamento jogável em 3D**: passar cabo por canaleta, crimpar, conectar no patch panel, testar
      ponto a ponto, identificar com etiqueta
- ⬜ **Trabalho em altura e forro**: escada, abrir placa de forro, poeira; risco de cair se for com pressa
- ⬜ **Organizar rack bagunçado** (antes/depois) — serviço vendável
- ⬜ Wi-Fi: posicionar access point, sinal fraco nos cantos (mapa de calor), **rede de visitantes separada**
- ⬜ Firewall em comodato no rack do cliente; **VPN** para quem trabalha de casa
- ⬜ **Servidor/NAS** no escritório do cliente; **backup 3-2-1** (HD externo + NAS + nuvem) e **teste de restauração**
- ⬜ **Queda do link da operadora**: provar que não é sua culpa, abrir chamado com a operadora; vender link de backup
- ⬜ **CFTV**: câmeras, DVR/NVR, cabo, fonte; posicionar vendo a área certa, acesso pelo celular do cliente
- ⬜ **Nobreak**: bateria morta, apitando, dimensionar e instalar; queda de energia e **raio** queimando
      switch/fonte de quem não tem proteção
- ⬜ Ferramentas: alicate de crimpar, testador de cabo, punch-down, escada, furadeira, passa-fio, detector de tensão
- ⬜ **Fiscalização** em obras de cabeamento maiores (documentação, ART/responsável técnico) ❓ calibrar com o dono

## Fase 6 — Cidade e deslocamento (dirigindo de verdade)
Decisão do dono: **dirigir de verdade** (sem viagem rápida por mapa).
- ⬜ Carro do técnico dirigível (entrar/sair, volante, câmera em 1ª pessoa, física simples com WheelCollider)
- ⬜ Cidade com ruas ligando casa, sede, fornecedores, ponto de coleta de lixo eletrônico e clientes; GPS no celular
- ⬜ Combustível (posto), estacionar na frente do cliente, carregar caixas/equipamentos/escada no porta-malas
- ⬜ Vários endereços (clínica, contabilidade, loja, escola, prefeitura, condomínio) — cada cliente com sala própria
- ⬜ Trânsito simples (carros NPC, semáforos); **clima**: chuva atrasa e impede serviço externo, temporal traz raios
      e chamados
- ⬜ Cobrança de deslocamento/km no orçamento
- ⬜ Mais tarde: trocar de veículo (moto → carro → van da empresa adesivada)

## Fase 7 — Clientes e pessoas vivas
- ✅ Clientes Mixamo animados (parado / conversando)
- ⬜ Funcionários do cliente sentados digitando, andando, atendendo telefone (animações já baixadas)
- ⬜ Cliente leva você até o equipamento; reage ao seu trabalho (impaciência, alívio)
- ⬜ **Personalidades**: o que jura que "não mexeu em nada", o da senha no post-it, o apressado, o que pechincha, o
      que **não paga**, o fiel que indica outros
- ⬜ **Horário comercial do cliente**: certos serviços só com o escritório vazio (parar servidor/rede) — fim de
      tarde, noite ou fim de semana
- ⬜ **Avaliação por estrelas** depois de cada atendimento; **retrabalho/garantia** quando o serviço foi mal feito
- ⬜ **Golpes e engenharia social** nos clientes (ligação do "banco", e-mail falso, phishing): limpar o estrago e
      vender treinamento
- ⬜ Mais variedade de personagens (Mixamo) e escritórios com cara própria por tipo de cliente
- ⬜ Seus funcionários visíveis na sede trabalhando e saindo para atender

## Fase 8 — Empresa, contratos e progressão
- ⬜ Terminar testes: vencer licitação, alertas de firewall/ERP na Central, serviços mensais, equipe e folha,
      virar ME, produto próprio (ERP/bot)
- ⬜ **Orçamento negociável** (peças + horas + deslocamento): cliente aprova, pechincha ou some
- ⬜ **Você define seus preços** (visita, hora técnica, formatação, contrato): caro demais = cliente vai embora;
      barato demais = você quebra
- ⬜ **Contrato mensal de suporte** (pacote de horas) com **prioridade e prazo (SLA)**: "servidor parado" é urgente,
      "trocar mouse" pode esperar; atraso gera multa
- ⬜ **Monitoramento remoto** dos clientes de contrato (agente instalado): alertas de disco cheio, backup que falhou,
      nobreak na bateria, antivírus desatualizado — resolver antes do cliente ligar vale estrelas
- ⬜ **Visita preventiva mensal** (limpeza, atualização, conferência de backup, leitura de contadores)
- ⬜ **Funcionário novo no cliente**: criar e-mail/conta/licença, preparar notebook, configurar impressora
- ⬜ **Funcionário que sai**: bloquear acessos, trocar senhas, recolher equipamento — esquecer gera problema
      (ex-funcionário apagando arquivos)
- ⬜ **Domínio, e-mail e site** do cliente: domínio vencido derruba o e-mail da empresa inteira; renovação, DNS,
      migração de e-mail
- ⬜ **Documentação da rede do cliente** (IPs, senhas, mapa de pontos, equipamentos com **etiqueta de patrimônio**):
      documentado = chamado resolvido mais rápido
- ⬜ **Certificações/cursos** que liberam linhas de serviço (CFTV, redes avançadas, firewall, servidores, backup)
- ⬜ **Funcionários com especialidade** (auxiliar, técnico, comercial, limpeza) assumem tarefas que eram suas —
      você vê eles fazendo
- ⬜ **Contas do mês**: aluguel, luz, internet, contador, impostos (DAS/Simples), combustível, folha
- ⬜ Problemas brasileiros: **certificado digital A1/A3 vencido** (emissor de NF-e parado), maquininha/Pix sem
      internet, sistema da contabilidade, LGPD (vazamento por senha fraca)
- ⬜ **Plantão e serviço fora do horário** (trocar servidor no domingo) pago mais caro; sobreaviso
- ⬜ **Sazonalidade**: volta às aulas, Imposto de Renda e fim de ano (contadores lotados), Black Friday (lojas),
      começo do ano (licitações)
- ⬜ **Concorrência visível**: concorrente que baixa o preço e tenta roubar cliente (já existe no sistema; mostrar
      no mundo e nas mensagens)
- ⬜ **Escolhas tentadoras**: programa pirata (rápido e barato, mas risco de fiscalização e de perder clientes
      grandes), "esquecer" de emitir nota
- ⬜ **Sede física** evoluindo (sala → sala maior → galpão), com móveis comprados aparecendo; **sede suja ou
      bagunçada** derruba a impressão de quem te visita
- ⬜ Eventos com cena: cliente furioso na porta, queda de energia, rato no forro roendo cabo, ransomware num cliente
      sem backup, backup "que nunca foi testado"
- ⬜ Balanceamento com o dono (valores calibrados com as notas privadas, fora do repositório; M365, ERP e controle
      de impressão a confirmar ❓)

## Fase 9 — Polimento
- ⬜ Sons gravados (Kenney Audio CC0 / Freesound CC0) no lugar dos gerados; música ambiente
- ⬜ Tutorial guiado no primeiro dia (com os objetivos "Primeiros passos")
- ⬜ Configurações gráficas completas, otimização (texturas 4K do Mixamo → 2K, LOD, oclusão)
- ⬜ Vários saves (slots), autosave
- ⬜ Menu principal 3D (câmera passeando pela casa/escritório)
- ⬜ Conquistas ligadas a situações reais ("Salvou a contabilidade em pleno IR", "Rack impecável", "Backup salvou o dia")
- ⬜ Revisão de textos, acentuação e glifos

## Fase 10 — Lançamento
- ⬜ Build estável sem console de debug por padrão
- ⬜ Página (itch.io / Steam), capturas e trailer
- ⬜ Revisão de licenças (CREDITOS.md completo, sem marcas reais, Mixamo embutido só no build)

---

### Ideias soltas (avaliar depois)
- Modo "dia livre" sem pressão de prazo
- Modo carreira com metas (ex.: "ter 10 contratos mensais em 1 ano")

### Como executar
Uma fase por vez, na ordem; os sistemas transversais nascem na primeira fase que precisa deles e crescem depois.
Em cada sessão: pegar o próximo ⬜, implementar, testar na Unity (skill `testar-na-unity`), marcar ✅ aqui,
commit local; push quando o dono pedir. Itens ❓ precisam da resposta do dono antes de começar.
