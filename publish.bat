@echo off

echo "Local FULL publish for each platform, with testing script-engine (not xunit) by 1testrunner and archive artifacts"

call rmdir distrs /s /q

call dotnet publish -c Release -p:DebugType=None -p:CopyOutputSymbolsToPublishDirectory=false -f netcoreapp3.1 .\src\oscript\oscript.csproj -o distrs\net31\win-x64\bin

cd .\distrs\net31\win-x64\

echo "Testing run opm - install libs from hub"

dir /w

call .\bin\opm.bat install asserts
call .\bin\opm.bat install tempfiles
call .\bin\opm install delegate
call .\bin\opm install fs
call .\bin\opm install 1testrunner
call .\bin\opm install 1bdd

cd bin
call 1testrunner -runall ..\..\..\..\tests\

cd ..\..\..\..\
