# 🎯 Auto Missão Discord

[![Licença: CC BY-NC 4.0](https://img.shields.io/badge/Licença-CC_BY--NC_4.0-lightgrey.svg)](LICENSE)
[![Plataforma](https://img.shields.io/badge/Plataforma-Windows-0078D6.svg)](https://microsoft.com/windows)
[![Uso](https://img.shields.io/badge/Uso-Gratuito%20%7C%20Venda%20Proibida-red.svg)](LICENSE)
[![Status](https://img.shields.io/badge/Status-100%25%20Funcional-success.svg)]()

Aplicativo executável nativo do Windows para simular jogos e completar **Discord Quests** (missões de transmissão de 15 minutos) de forma leve, rápida e sem precisar baixar dezenas de gigabytes de jogos nem consumir memória ou placa de vídeo.

---

## ⚠️ AVISO LEGAL: VENDA ESTRITAMENTE PROIBIDA

> **Este é um projeto 100% de código aberto, gratuito e de uso pessoal/educacional.**  
> É **terminantemente proibido vender** este programa, cobrar pelo seu download/instalação ou comercializá-lo em pacotes pagos. Se alguém te cobrou por este software, você foi enganado.

---

## 🌟 Principais Recursos

- **Banco Oficial do Discord:** Mais de **11.100 jogos** catalogados diretamente da API oficial de detecção do Discord (`detectable applications`).
- **Busca Rápida em Tempo Real:** Campo de pesquisa instantâneo que filtra simultaneamente por **nome do jogo** ou **nome do executável (.exe)**.
- **Jogos em Destaque (⭐):** Os jogos mais populares e comuns em missões do Discord ficam fixados no topo da lista.
- **Suporte a Subpastas do Discord:** Criação automática de árvores de diretórios exigidas pelo Discord (ex: `cod23/cod23-cod.exe`, `ea sports fc 25/fc25.exe`, `bin/helldivers2.exe`).
- **Sincronização Online:** Botão **`🔄 Sincronizar Discord`** para atualizar o catálogo sempre que o Discord adicionar novos jogos, sem precisar mexer no código.
- **Janela de Renderização Contínua:** Renderização leve (~15 FPS) com cronômetro em tempo real, garantindo que o gancho de captura do Discord não congele nem considere a transmissão inativa.
- **Super Leve (~35 KB):** Binário nativo em C# (.NET Framework), consome quase 0% de CPU e menos de 15 MB de RAM. Não requer Python, Node.js nem instalação.

---

## 🚀 Como Usar

### 1. Iniciar a Simulação
1. Abra o arquivo `AutoMissaoDiscord.exe`.
2. No campo **`🔍 Buscar jogo ou executável`**, digite o nome do jogo da sua missão (ex: `Modern Warfare 4`, `Roblox`, `FC 25`, `Nekome`, `Genshin`).
3. Selecione o jogo na lista e clique no botão verde **`▶ INICIAR MISSÃO`**.
4. A janela escura simulada do jogo abrirá com o contador regressivo de 15 minutos.

### 2. Transmitir no Discord
1. No seu Discord, entre em um canal de voz com um amigo ou com uma conta secundária.
2. Clique no botão **"Compartilhar sua Tela" / "Transmitir"**.
3. Na aba **Aplicativos**, selecione especificamente a **janela do jogo simulado** (não compartilhe a tela inteira).

### 3. Assistir na Conta Secundária (Importante!)
> 💡 **Requisito Obrigatório do Discord:**  
> O Discord exige que pelo menos **1 pessoa esteja assistindo ativamente** ao stream para computar o progresso da missão.
- Na conta secundária, clique em **"Assistir à transmissão"** e dê dois cliques para deixá-la em **modo foco/tela cheia**.
- **Não minimize a janela** do espectador na barra de tarefas (navegadores modernos pausam a reprodução do vídeo ao serem minimizados, o que pausa a contagem da missão).

### 4. Conclusão
- O programa avisará com som e notificação assim que os 15 minutos terminarem.
- Resgate sua recompensa na aba de Presentes/Missões do Discord!

---

## 📁 Estrutura do Repositório

```
auto_missao_discord/
├── .gitignore               # Arquivos ignorados pelo Git (fakes/, temporários)
├── LICENSE                  # Licença de Uso Não Comercial (CC BY-NC 4.0)
├── README.md                # Documentação e instruções de uso
├── build.bat                # Script de compilação em 1 clique (nativo Windows)
├── games.json               # Banco de dados com 11.100+ jogos do Discord
└── src/
    └── Program.cs           # Código-fonte completo em C# (GUI + Simulador)
```

---

## 🛠️ Como Compilar a Partir do Código Fonte

O projeto utiliza o compilador nativo do Windows (`csc.exe` do .NET Framework 4.8), dispensando instalações de IDEs pesadas:

1. Dê um duplo clique no arquivo `build.bat` (ou execute-o pelo Prompt de Comando).
2. O executável `AutoMissaoDiscord.exe` será gerado automaticamente na raiz do projeto.

---

## ⚖️ Licença

Este projeto está licenciado sob a licença [Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)](LICENSE).

- **Permitido:** Copiar, redistribuir, modificar e criar trabalhos derivados para fins pessoais e não comerciais.
- **Proibido:** Venda, comercialização, inclusão em produtos ou serviços pagos de qualquer natureza.
