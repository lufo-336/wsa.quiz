#!/usr/bin/env bash
# Step 1 cleanup: rimuove i vecchi file della struttura monolitica QuizFdP.
# Da lanciare DENTRO la cartella wsa.quiz/ DOPO aver estratto i file di step1.
set -e

echo "==> Rimuovo i vecchi file con git rm (preserva lo storico)..."

# Vecchio csproj monolitico (sostituito da wsa.quiz.console/Wsa.Quiz.Console.csproj)
git rm -f QuizFdP.csproj 2>/dev/null || true

# Vecchio Program.cs (spostato in wsa.quiz.console/Program.cs)
git rm -f Program.cs 2>/dev/null || true

# Vecchie cartelle Models/ e Services/ (i .cs ora vivono in wsa.quiz.core/ e wsa.quiz.console/)
git rm -rf Models 2>/dev/null || true
git rm -rf Services 2>/dev/null || true

# Vecchia cartella WPF (sostituita da wsa.quiz.app/ in Avalonia)
git rm -rf wsa.quiz.Wpf 2>/dev/null || true

echo "==> Pulisco le cartelle di build (bin/ e obj/) ai vari livelli..."
rm -rf bin obj
rm -rf wsa.quiz.core/bin wsa.quiz.core/obj
rm -rf wsa.quiz.console/bin wsa.quiz.console/obj
rm -rf wsa.quiz.app/bin wsa.quiz.app/obj

echo ""
echo "Fatto. Ora prova:"
echo "    dotnet build Wsa.Quiz.sln"
echo "    dotnet run --project wsa.quiz.console/Wsa.Quiz.Console.csproj"
echo "    dotnet run --project wsa.quiz.app/Wsa.Quiz.App.csproj"
