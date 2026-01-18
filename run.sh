#!/bin/bash
# Development環境で.NET 8.0アプリケーションを起動するスクリプト
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
dotnet run
