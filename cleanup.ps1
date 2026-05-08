# Step 1 fix — pulizia residui dalla prima estrazione
# Lanciare DENTRO la cartella che contiene Wsa.Quiz.sln, DOPO aver estratto questo zip.

Write-Host "==> Rimuovo la vecchia cartella wsa.quiz.console (sostituita da wsa.quiz.cli)..."
if (Test-Path .\wsa.quiz.console) {
    Remove-Item -Recurse -Force .\wsa.quiz.console
    Write-Host "    Rimossa."
} else {
    Write-Host "    Gia' assente, ok."
}

Write-Host "==> Pulisco bin/ e obj/ in tutti i progetti..."
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Fatto. Adesso prova:"
Write-Host "    dotnet build Wsa.Quiz.sln"
Write-Host "    dotnet run --project wsa.quiz.cli\Wsa.Quiz.Cli.csproj"
Write-Host "    dotnet run --project wsa.quiz.app\Wsa.Quiz.App.csproj"
