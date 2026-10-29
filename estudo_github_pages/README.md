# estudo — Blazor WebAssembly para GitHub Pages

Este projeto foi adaptado de **Blazor Web App com Interactive Server** para **Blazor WebAssembly Standalone**. Assim, os componentes C# e os eventos interativos são executados no navegador, e o site pode ser hospedado como arquivos estáticos no GitHub Pages.

## Abrir no Visual Studio 2026

1. Extraia o ZIP.
2. Abra `estudo.csproj` no Visual Studio.
3. Aguarde a restauração dos pacotes NuGet.
4. Pressione `F5` para executar localmente.

É necessário ter o workload **ASP.NET e desenvolvimento Web** e o .NET 10 SDK instalados.

## Publicar no GitHub Pages

1. Crie um repositório no GitHub. O nome pode ser `estudo` ou outro nome.
2. Envie **o conteúdo desta pasta** para a raiz do repositório (não envie apenas o arquivo ZIP e não deixe uma pasta extra envolvendo todo o projeto).
3. Confirme que o branch principal se chama `main` ou `master`.
4. No GitHub, abra **Settings → Pages** e defina **Build and deployment → Source → GitHub Actions**.
5. Abra a aba **Actions** e confira a execução de `Publicar Blazor WebAssembly no GitHub Pages`.
6. Quando o fluxo terminar com sucesso, o endereço será `https://SEU-USUARIO.github.io/NOME-DO-REPOSITORIO/`.

O workflow compila o projeto automaticamente, ajusta o `<base href>` de acordo com o nome do repositório, prepara os arquivos para GitHub Pages e publica o resultado. Não é necessário compilar o projeto no seu computador para fazer o deploy.

## Observações

- O fluxo usa .NET 10 e publica o conteúdo de `wwwroot`.
- A compressão prévia de recursos Blazor está desativada para facilitar a hospedagem no GitHub Pages.
- As páginas `Home`, `Counter` e `Weather` são exemplos de aprendizagem.
- O GitHub Pages hospeda arquivos estáticos; este projeto não possui um backend ASP.NET Core, banco de dados de servidor ou autenticação de servidor.
