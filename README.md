# f-type-parts-manual-generator
Scrape a Jag parts website and construct a PDF for offline reference

## Running Playwright

```powershell
pwsh bin\Debug\net6.0\playwright.ps1 install
```

## Build

From App.Console directory, run:

```powershell
dotnet build -c release
```

## Run scrape

From App.Console directory, run:

```powershell
.\bin\Release\net6.0\App.Console.exe scrape
```

## Run PDF builder

From App.Console directory, run:

```powershell
.\bin\Release\net6.0\App.Console.exe build-pdf --data-file C:\Temp\f-type-parts-scraper.20220804_210355\results.json
```