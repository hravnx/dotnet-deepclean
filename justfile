
build:
    dotnet build -c Release

pack: build
    dotnet pack src/app/Ravn.Dotnet.DeepClean.fsproj

install: pack
    dotnet tool install --global --add-source src/app/bin/Release Ravn.Dotnet.DeepClean

uninstall:
    dotnet tool uninstall --global ravn.dotnet.deepclean
