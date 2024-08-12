# Jifer - Прототип на социална мрежа

**Jifer** е прототип на социална мрежа, която предлага основни функции като регистрация на потребители, покани, приятелства и публикации с различни нива на видимост.

## Автор
- **Име:** Алекс
- **Имейл:** [rlgalexbgto@gmail.com](mailto:rlgalexbgto@gmail.com)

## **Настройки:**
1. **За промяна на настройките на приложението**
    appsettings.json, production.json и development.json

### Инсталация.1
1. **Клонирайте репозитория:**
    git clone https://github.com/your-repository-url.git
2. **Навигирайте до директорията на проекта:**
    cd Jifer
3. **Възстановете зависимостите:**
    dotnet restore
4. **Изградете проекта:**
    dotnet build
5. **Създаване на локаланат база данни**
   dotnet ef database update
6. **Стартирайте проекта:**
    dotnet run

### Инсталация.2
1. **Клонирайте репозитория:**
    git clone https://github.com/your-repository-url.git
2. **Отваряне на проекта**
    Стартиране на Jifer.sln в IDE като VisualStudio
3. **Изтегляне и промяна на базата дании**
    При не наличие на подходящи версии, инсталацията им през конзола или SolutionExplorer>Manage NuGet Packages for Solution...
4. **Създаване на локаланат база данни**
   View->Other Windows->Package Manager Console->Update-Database
5. **Стартиране на прокета**
    Ctrl+F5, конзола или с помощ на IDE-то

### Пререквизити
- .NET 8
- SQL Server или друга съвместима база данни


