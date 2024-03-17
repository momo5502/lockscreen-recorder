@echo off
dotnet publish -c Release -p:PublishSingleFile=true --no-self-contained
pause