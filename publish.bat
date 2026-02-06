@echo off
echo Building Perceptron Simulator release...
dotnet publish -p:PublishProfile=SingleFileRelease
echo.
echo Done! Output is in c:\perceptron_release\
echo Creating installer:
call c:\scripts\CreateInstall c:\perceptron_release\PerceptronSimulator.exe

